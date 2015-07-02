using RegistryLanguageService.Syntax;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.Win32;
using RegistryLanguageService.Semantics;

namespace RegistryLanguageService
{
    [Export(typeof(IViewTaggerProvider))]
    [TagType(typeof(ITextMarkerTag))]
    [ContentType(RegistryContentTypeNames.Registry)]
    internal sealed class RegistryHighlightReferencesTaggerProvider : IViewTaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            return textView.Properties.GetOrCreateSingletonProperty(
                creator: () => new RegistryHighlightReferencesTagger(textView)
            ) as ITagger<T>;
        }


        private sealed class RegistryHighlightReferencesTagger : ITagger<ITextMarkerTag>
        {
            public RegistryHighlightReferencesTagger(ITextView view)
            {
                _view = view;

                _view.Caret.PositionChanged += OnCaretPositionChanged;
            }

            private readonly ITextView _view;

            private static readonly ITextMarkerTag Tag = new TextMarkerTag("MarkerFormatDefinition/HighlightedReference");


            private void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
            {
                // TODO: optimize changed spans
                this.TagsChanged?.Invoke(this,
                    new SnapshotSpanEventArgs(new SnapshotSpan(e.TextView.TextBuffer.CurrentSnapshot, 0, e.TextView.TextBuffer.CurrentSnapshot.Length))
                );
            }
            
            public IEnumerable<ITagSpan<ITextMarkerTag>> GetTags(NormalizedSnapshotSpanCollection spans)
            {
                ITextBuffer buffer = spans.First().Snapshot.TextBuffer;
                SyntaxTree syntax = buffer.GetSyntaxTree();
                RegistryDocumentSyntax root = syntax.Root as RegistryDocumentSyntax;
                

                SnapshotPoint caret = _view.Caret.Position.BufferPosition;

                // find section
                RegistrySectionSyntax section = root.Sections
                    .FirstOrDefault(s => s.Span.ContainsOrEndsWith(caret));

                // show duplicate sections
                if (section != null)
                {
                    // match keys
                    SnapshotToken nameToken = section.NameSyntax.Tokens.FirstOrDefault(t => t.Span.Span.ContainsOrEndsWith(caret));

                    if (nameToken != null)
                    {
                        IReadOnlyList<SnapshotToken> hierarchy = section.NameSyntax.Tokens
                            .TakeTo(nameToken)
                            .ToList();
                        
                        return
                            from s in root.Sections
                            let h = s.NameSyntax.Tokens
                            where h.Count >= hierarchy.Count
                            from n in h
                            let path = h.Take(hierarchy.Count).ToList()
                            where hierarchy.Zip(path, (x, y) => x.Value.Equals(y.Value, StringComparison.InvariantCultureIgnoreCase)).All(I => I)
                            select new TagSpan<ITextMarkerTag>(path.Last().Span.Span, Tag)
                        ;
                    }

                    // properties
                    RegistryPropertySyntax property = section.Properties
                        .FirstOrDefault(p => p.Span.ContainsOrEndsWith(caret));

                    if (property != null)
                    {
                        // match subkeys
                        if (!property.NameToken.IsMissing &&
                            property.NameToken.Span.Span.ContainsOrEndsWith(caret) &&
                            section.NameSyntax.Tokens.Any())
                        {
                            IReadOnlyList<SnapshotToken> hierarchy = section.NameSyntax.Tokens
                                .TakeTo(nameToken)
                                .ToList();

                            string name = property.NameToken.Value;

                            return
                                from s in root.Sections
                                let h = s.NameSyntax.Tokens
                                where h.Count >= hierarchy.Count
                                from n in h
                                let path = h.Take(hierarchy.Count).ToList()
                                where hierarchy.Zip(path, (x, y) => x.Value.Equals(y.Value, StringComparison.InvariantCultureIgnoreCase)).All(I => I)

                                from k in s.Properties
                                where !k.NameToken.IsMissing
                                where k.NameToken.Value.Equals(name, StringComparison.InvariantCultureIgnoreCase)
                                select new TagSpan<ITextMarkerTag>(k.NameToken.Span.Span, Tag)
                            ;
                        }

                        // match data types
                        if (!property.TypeToken.IsMissing &&
                            property.TypeToken.Span.Span.ContainsOrEndsWith(caret))
                        {
                            RegistryValueKind dataType = RegistryTypeSymbol.GetValueKindFromTypeAlias(property.TypeToken.Value);

                            return
                                from s in root.Sections
                                from k in s.Properties
                                where !k.TypeToken.IsMissing
                                where RegistryTypeSymbol.GetValueKindFromTypeAlias(k.TypeToken.Value) == dataType
                                select new TagSpan<ITextMarkerTag>(k.TypeToken.Span.Span, Tag)
                            ;
                        }
                    }
                }
                
                return Enumerable.Empty<TagSpan<ITextMarkerTag>>();
            }

            public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
        }
    }
}
