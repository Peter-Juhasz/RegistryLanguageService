using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System;
using System.Linq;

namespace RegistryLanguageService.Syntax
{
    public class RegistryPropertySyntax : SyntaxNode
    {
        public RegistryPropertySyntax()
        {
            this.LeadingTrivia = new List<SnapshotToken>();
            this.TrailingTrivia = new List<SnapshotToken>();
        }
        
        public RegistrySectionSyntax Section { get; set; }

        
        public SnapshotToken NameOpeningQuoteToken { get; set; }

        public SnapshotToken NameToken { get; set; }

        public SnapshotToken NameClosingQuoteToken { get; set; }


        public SnapshotToken NameValueDelimiterToken { get; set; }


        public SnapshotToken DeleteToken { get; set; }

        public SnapshotToken ValueOpeningQuoteToken { get; set; }


        public SnapshotToken TypeToken { get; set; }

        public SnapshotToken TypeSpecifierOpeningBraceToken { get; set; }

        public SnapshotToken TypeSpecifierToken { get; set; }

        public SnapshotToken TypeSpecifierClosingBraceToken { get; set; }

        public SnapshotToken TypeValueDelimiterToken { get; set; }


        public SnapshotToken ValueToken { get; set; }

        public SnapshotToken ValueClosingQuoteToken { get; set; }


        public IList<SnapshotToken> LeadingTrivia { get; set; }

        public IList<SnapshotToken> TrailingTrivia { get; set; }


        public override SnapshotSpan Span
        {
            get
            {
                return new SnapshotSpan(this.NameOpeningQuoteToken.Span.Span.Start, this.ValueClosingQuoteToken.Span.Span.End);
            }
        }

        public override SnapshotSpan FullSpan
        {
            get
            {
                return new SnapshotSpan(
                    (this.LeadingTrivia.FirstOrDefault() ?? this.NameOpeningQuoteToken).Span.Span.Start,
                    (this.TrailingTrivia.LastOrDefault() ?? this.ValueClosingQuoteToken).Span.Span.End
                );
            }
        }

        public override SyntaxNode Parent
        {
            get
            {
                return this.Section;
            }
        }

        public override IEnumerable<SyntaxNode> Descendants()
        {
            yield break;
        }


        public override IEnumerable<SnapshotToken> GetTokens()
        {
            foreach (SnapshotToken token in this.LeadingTrivia)
                yield return token;

            yield return this.NameOpeningQuoteToken;
            yield return this.NameToken;
            yield return this.NameClosingQuoteToken;
            yield return this.NameValueDelimiterToken;
            yield return this.DeleteToken;
            yield return this.ValueOpeningQuoteToken;
            yield return this.TypeToken;
            yield return this.TypeSpecifierOpeningBraceToken;
            yield return this.TypeSpecifierToken;
            yield return this.TypeSpecifierClosingBraceToken;
            yield return this.TypeValueDelimiterToken;
            yield return this.ValueToken;
            yield return this.ValueClosingQuoteToken;

            foreach (SnapshotToken token in this.TrailingTrivia)
                yield return token;
        }
    }
}
