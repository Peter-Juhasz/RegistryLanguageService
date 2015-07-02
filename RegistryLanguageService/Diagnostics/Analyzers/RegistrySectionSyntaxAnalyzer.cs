using RegistryLanguageService.Syntax;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using System.Collections.Generic;
using System.Linq;

namespace RegistryLanguageService.Diagnostics
{
    [ExportDiagnosticAnalyzer]
    internal sealed class RegistrySectionSyntaxAnalyzer : ISyntaxNodeAnalyzer<RegistrySectionSyntax>
    {
        public const string SectionNameExpected = "SectionNameExpected";
        public const string MissingSectionNameClosingBracketMissing = "MissingSectionNameClosingBracket";

        public IEnumerable<ITagSpan<IErrorTag>> Analyze(RegistrySectionSyntax section)
        {
            // section name is missing
            if (section.NameSyntax.Tokens.All(t => t.IsMissing))
            {
                yield return new TagSpan<IErrorTag>(
                    section.NameSyntax.Tokens.First().Span.Span,
                    new DiagnosticErrorTag(PredefinedErrorTypeNames.SyntaxError, SectionNameExpected, "Section name expected")
                );
            }

            // closing bracket is missing
            else if (section.ClosingBracketToken.IsMissing)
            {
                yield return new TagSpan<IErrorTag>(
                    section.ClosingBracketToken.Span.Span,
                    new DiagnosticErrorTag(PredefinedErrorTypeNames.SyntaxError, MissingSectionNameClosingBracketMissing, "']' expected")
                );
            }
        }
    }
}
