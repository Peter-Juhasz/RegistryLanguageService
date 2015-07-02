using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Linq;
using System;

namespace RegistryLanguageService.Syntax
{
    public class RegistrySectionSyntax : SyntaxNode
    {
        public RegistrySectionSyntax()
        {
            this.Properties = new List<RegistryPropertySyntax>();
            this.LeadingTrivia = new List<SnapshotToken>();
            this.TrailingTrivia = new List<SnapshotToken>();
        }

        public RegistryDocumentSyntax Document { get; set; }

        public IList<RegistryPropertySyntax> Properties { get; set; }


        public SnapshotToken OpeningBracketToken { get; set; }

        public SnapshotToken DeleteToken { get; set; }

        public SeparatedTokenListSyntax NameSyntax { get; set; }

        public SnapshotToken ClosingBracketToken { get; set; }


        public IList<SnapshotToken> LeadingTrivia { get; set; }

        public IList<SnapshotToken> TrailingTrivia { get; set; }


        public override SnapshotSpan Span
        {
            get
            {
                return this.Properties.Count == 0
                    ? new SnapshotSpan(this.OpeningBracketToken.Span.Span.Start, this.ClosingBracketToken.Span.Span.End)
                    : new SnapshotSpan(this.OpeningBracketToken.Span.Span.Start, this.Properties.Last().ValueToken.Span.Span.End)
                ;
            }
        }

        public override SnapshotSpan FullSpan
        {
            get
            {
                return new SnapshotSpan(
                    (this.LeadingTrivia.FirstOrDefault() ?? this.OpeningBracketToken).Span.Span.Start,
                    (this.TrailingTrivia.LastOrDefault()
                        ?? this.Properties.LastOrDefault()?.TrailingTrivia.LastOrDefault()
                        ?? this.Properties.LastOrDefault()?.ValueToken
                        ?? this.ClosingBracketToken
                    ).Span.Span.End
                );
            }
        }

        public override SyntaxNode Parent
        {
            get
            {
                return this.Document;
            }
        }

        public override IEnumerable<SyntaxNode> Descendants()
        {
            yield return this.NameSyntax;

            foreach (var node in this.Properties)
                yield return node;
        }


        public override IEnumerable<SnapshotToken> GetTokens()
        {
            foreach (SnapshotToken token in this.LeadingTrivia)
                yield return token;

            yield return this.OpeningBracketToken;

            foreach (SnapshotToken token in this.NameSyntax.GetTokens())
                yield return token;

            yield return this.ClosingBracketToken;

            foreach (SnapshotToken token in this.TrailingTrivia)
                yield return token;

            foreach (SnapshotToken token in this.Properties.SelectMany(p => p.GetTokens()))
                yield return token;
        }
    }
}
