using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RegistryLanguageService.Syntax
{
    public class RegistryDocumentSyntax : SyntaxNode
    {
        public RegistryDocumentSyntax()
        {
            this.Sections = new List<RegistrySectionSyntax>();
        }

        public ITextSnapshot Snapshot { get; set; }


        public SnapshotToken VersionToken { get; set; }


        public IList<RegistrySectionSyntax> Sections { get; set; }


        public override SnapshotSpan Span
        {
            get
            {
                if (!this.Sections.Any())
                    return new SnapshotSpan(this.Snapshot, 0, 0);

                return new SnapshotSpan(
                    this.VersionToken.Span.Span.Start,
                    this.Sections.Last().Span.End
                );
            }
        }

        public override SnapshotSpan FullSpan
        {
            get
            {
                return new SnapshotSpan(this.Snapshot, 0, this.Snapshot.Length);
            }
        }

        public override SyntaxNode Parent
        {
            get
            {
                return null;
            }
        }

        public override IEnumerable<SyntaxNode> Descendants()
        {
            return this.Sections.SelectMany(s => s.DescendantsAndSelf());
        }


        public override IEnumerable<SnapshotToken> GetTokens()
        {
            yield return this.VersionToken;

            foreach (var token in this.Sections.SelectMany(s => s.GetTokens()))
                yield return token;
        }
    }
}
