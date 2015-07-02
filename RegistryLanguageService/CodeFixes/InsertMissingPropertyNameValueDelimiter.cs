using RegistryLanguageService.CodeRefactorings;
using RegistryLanguageService.Diagnostics;
using RegistryLanguageService.Syntax;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace RegistryLanguageService.CodeFixes
{
    [Export(typeof(ICodeFixProvider))]
    internal sealed class InsertMissingPropertyNameValueDelimiter : ICodeFixProvider
    {
        private static readonly IReadOnlyCollection<string> FixableIds = new string[]
        {
            RegistryPropertySyntaxAnalyzer.MissingPropertyNameValueDelimiter
        };

        public IEnumerable<string> FixableDiagnosticIds
        {
            get { return FixableIds; }
        }

        public IEnumerable<CodeAction> GetFixes(SnapshotSpan span)
        {
            ITextBuffer buffer = span.Snapshot.TextBuffer;
            SyntaxTree syntax = buffer.GetSyntaxTree();
            RegistryDocumentSyntax root = syntax.Root as RegistryDocumentSyntax;

            // find section
            RegistryPropertySyntax property = root.Sections
                .SelectMany(s => s.Properties)
                .Where(p => !p.NameToken.IsMissing)
                .TakeWhile(p => p.NameToken.Span.Span.End <= span.Start)
                .Last();
            
            yield return new CodeAction(
                $"Fix syntax error: Insert missing '{RegistrySyntaxFacts.PropertyNameValueDelimiter}'",
                () => Fix(property)
            );
        }
        
        public ITextEdit Fix(RegistryPropertySyntax property)
        {
            ITextBuffer buffer = property.Section.Document.Snapshot.TextBuffer;

            ITextEdit edit = buffer.CreateEdit();
            edit.Insert(property.NameToken.Span.Span.End, RegistrySyntaxFacts.PropertyNameValueDelimiter.ToString());

            return edit;
        }
    }
}
