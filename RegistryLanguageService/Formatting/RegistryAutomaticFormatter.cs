using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using RegistryLanguageService.Syntax;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Text.Operations;

namespace RegistryLanguageService.Formatting
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType(RegistryContentTypeNames.Registry)]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal sealed class RegistryAutomaticFormatter : IVsTextViewCreationListener
    {
#pragma warning disable 169

        [Import]
        private IVsEditorAdaptersFactoryService AdaptersFactory;

        [Import]
        private ITextDocumentFactoryService TextDocumentFactoryService;

        [Import]
        private ITextBufferUndoManagerProvider _textBufferUndoManagerProvider;

#pragma warning restore 169


        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            IWpfTextView view = AdaptersFactory.GetWpfTextView(textViewAdapter);
            Debug.Assert(view != null);
            
            // register command filter
            CommandFilter filter = new CommandFilter(view,
                _textBufferUndoManagerProvider.GetTextBufferUndoManager(view.TextBuffer).TextBufferUndoHistory
            );

            IOleCommandTarget next;
            ErrorHandler.ThrowOnFailure(textViewAdapter.AddCommandFilter(filter, out next));
            filter.Next = next;
        }


        private sealed class CommandFilter : IOleCommandTarget
        {
            public CommandFilter(ITextView view, ITextUndoHistory undoHistory)
            {
                _textView = view;
                _undoHistory = undoHistory;
            }

            private readonly ITextView _textView;
            private readonly ITextUndoHistory _undoHistory;


            public void OnCharTyped(char @char)
            {
                // format on ']'
                if (@char == ']')
                {
                    ITextBuffer buffer = _textView.TextBuffer;
                    
                    SyntaxTree syntaxTree = buffer.GetSyntaxTree();
                    RegistryDocumentSyntax root = syntaxTree.Root as RegistryDocumentSyntax;

                    var caret = _textView.Caret.Position.BufferPosition;
                    RegistrySectionSyntax section = root.Sections
                        .FirstOrDefault(s => s.ClosingBracketToken.Span.Span.End == caret);

                    if (section != null)
                    {
                        // remove unnecessary whitespace
                        using (ITextUndoTransaction transaction = _undoHistory.CreateTransaction("Automatic Formatting"))
                        {
                            using (ITextEdit format = buffer.CreateEdit())
                            {
                                var nameSyntax = section.NameSyntax;

                                // between '[' and 'name'
                                if (section.OpeningBracketToken.Span.Span.End != nameSyntax.Span.Span.Start)
                                    format.Delete(new SnapshotSpan(section.OpeningBracketToken.Span.Span.End, nameSyntax.Span.Span.Start));

                                // around separators
                                for (int i = 0; i < nameSyntax.Separators.Count; i++)
                                {
                                    if (nameSyntax.Tokens[i].Span.Span.End != nameSyntax.Separators[i].Span.Span.Start)
                                        format.Delete(new SnapshotSpan(nameSyntax.Tokens[i].Span.Span.End, nameSyntax.Separators[i].Span.Span.Start));

                                    if (i + 1 < nameSyntax.Tokens.Count)
                                        if (nameSyntax.Separators[i].Span.Span.End != nameSyntax.Tokens[i + 1].Span.Span.Start)
                                            format.Delete(new SnapshotSpan(nameSyntax.Separators[i].Span.Span.End, nameSyntax.Tokens[i + 1].Span.Span.Start));
                                }

                                // between 'name' and ']'
                                if (nameSyntax.Span.Span.End != section.ClosingBracketToken.Span.Span.Start)
                                    format.Delete(new SnapshotSpan(nameSyntax.Span.End, section.ClosingBracketToken.Span.Span.Start));

                                format.Apply();
                            }

                            transaction.Complete();
                        }
                    }
                }

                // format on '='
                else if (@char == '=')
                {
                    ITextBuffer buffer = _textView.TextBuffer;

                    SyntaxTree syntaxTree = buffer.GetSyntaxTree();
                    RegistryDocumentSyntax root = syntaxTree.Root as RegistryDocumentSyntax;

                    var caret = _textView.Caret.Position.BufferPosition;
                    RegistryPropertySyntax property = root.Sections
                        .SelectMany(s => s.Properties)
                        .FirstOrDefault(p => p.NameValueDelimiterToken.Span.Span.End == caret);

                    if (property != null)
                    {
                        // reference point is section opening '['
                        SnapshotPoint referencePoint = property.Section.OpeningBracketToken.Span.Span.Start;

                        // find property before
                        RegistryPropertySyntax before = property.Section.Properties
                            .TakeWhile(p => p != property)
                            .LastOrDefault();

                        // override reference point if found property before
                        if (before != null)
                            referencePoint = before.NameToken.Span.Span.Start;

                        // compare
                        ITextSnapshotLine referenceLine = referencePoint.GetContainingLine();
                        ITextSnapshotLine line = property.NameValueDelimiterToken.Span.Span.End.GetContainingLine();

                        SnapshotSpan referenceIndent = new SnapshotSpan(referenceLine.Start, referencePoint);
                        SnapshotSpan indent = new SnapshotSpan(line.Start, property.NameToken.Span.Span.Start);
                        
                        if (referenceIndent.GetText() != indent.GetText())
                        {
                            // fix indentation
                            using (ITextUndoTransaction transaction = _undoHistory.CreateTransaction("Automatic Formatting"))
                            {
                                using (ITextEdit edit = buffer.CreateEdit())
                                {
                                    // replace leading white space
                                    edit.Replace(indent, referenceIndent.GetText());

                                    edit.Apply();
                                }

                                transaction.Complete();
                            }
                        }
                    }
                }
            }


            public IOleCommandTarget Next { get; set; }

            public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
            {
                int hresult = VSConstants.S_OK;

                hresult = Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

                if (ErrorHandler.Succeeded(hresult))
                {
                    if (pguidCmdGroup == VSConstants.VSStd2K)
                    {
                        switch ((VSConstants.VSStd2KCmdID)nCmdID)
                        {
                            case VSConstants.VSStd2KCmdID.TYPECHAR:
                                char @char = GetTypeChar(pvaIn);
                                OnCharTyped(@char);
                                break;
                        }
                    }
                }

                return hresult;
            }

            public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
            {
                return Next.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText);
            }

            private static char GetTypeChar(IntPtr pvaIn)
            {
                return (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
            }
        }
    }
}
