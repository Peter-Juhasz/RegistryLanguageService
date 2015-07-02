using RegistryLanguageService.Syntax;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using System.Collections.Generic;

namespace RegistryLanguageService.Diagnostics
{
    [ExportDiagnosticAnalyzer]
    internal sealed class RegistryPropertySyntaxAnalyzer : ISyntaxNodeAnalyzer<RegistryPropertySyntax>
    {
        public const string MissingPropertyNameValueDelimiter = "MissingPropertyNameValueDelimiter";

        public IEnumerable<ITagSpan<IErrorTag>> Analyze(RegistryPropertySyntax property)
        {
            // delimiter missing
            if (property.NameValueDelimiterToken.IsMissing)
            {
                yield return new TagSpan<IErrorTag>(
                    property.NameValueDelimiterToken.Span.Span,
                    new DiagnosticErrorTag(PredefinedErrorTypeNames.SyntaxError, "MissingPropertyNameValueDelimiter", "'=' expected")
                );
            }
        }
    }
}
