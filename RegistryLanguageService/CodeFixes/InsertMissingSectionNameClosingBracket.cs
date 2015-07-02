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
    internal sealed class InsertMissingSectionNameClosingBracket : ICodeFixProvider
    {
        private static readonly IReadOnlyCollection<string> FixableIds = new string[]
        {
            RegistrySectionSyntaxAnalyzer.MissingSectionNameClosingBracketMissing
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
            RegistrySectionSyntax section = root.Sections
                .Where(s => s.NameSyntax.Tokens.Any())
                .TakeWhile(s => s.NameSyntax.Span.End <= span.Start)
                .Last();
            
            yield return new CodeAction(
                $"Fix syntax error: Insert missing '{RegistrySyntaxFacts.SectionNameClosingBracket}'",
                () => Fix(section)
            );
        }
        
        public ITextEdit Fix(RegistrySectionSyntax section)
        {
            ITextBuffer buffer = section.Document.Snapshot.TextBuffer;

            ITextEdit edit = buffer.CreateEdit();
            edit.Insert(section.NameSyntax.Span.Span.End, RegistrySyntaxFacts.SectionNameClosingBracket.ToString());

            return edit;
        }
    }
}
