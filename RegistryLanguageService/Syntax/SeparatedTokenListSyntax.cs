using RegistryLanguageService.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;

namespace RegistryLanguageService.Syntax
{
    public class SeparatedTokenListSyntax : SyntaxNode
    {
        public SeparatedTokenListSyntax()
        {
            this.Tokens = new List<SnapshotToken>();
            this.Separators = new List<SnapshotToken>();
        }

        public IList<SnapshotToken> Tokens { get; set; }

        public IList<SnapshotToken> Separators { get; set; }


        public RegistrySectionSyntax Section { get; set; }


        public override SnapshotSpan FullSpan
        {
            get
            {
                return new SnapshotSpan(
                    this.Tokens.First().Span.Span.Start,
                    this.Tokens.Last().Span.Span.End
                );
            }
        }

        public override SyntaxNode Parent { get { return this.Section; } }

        public override SnapshotSpan Span
        {
            get
            {
                return new SnapshotSpan(
                    this.Tokens.First().Span.Span.Start,
                    this.Tokens.Last().Span.Span.End
                );
            }
        }

        public override IEnumerable<SyntaxNode> Descendants()
        {
            yield break;
        }

        public override IEnumerable<SnapshotToken> GetTokens()
        {
            using (var tokensEnumerator = this.Tokens.GetEnumerator())
            using (var separatorsEnumerator = this.Tokens.GetEnumerator())
            {
                while (tokensEnumerator.MoveNext())
                {
                    yield return tokensEnumerator.Current;

                    if (separatorsEnumerator.MoveNext())
                        yield return separatorsEnumerator.Current;
                }
            }
        }
    }
}
