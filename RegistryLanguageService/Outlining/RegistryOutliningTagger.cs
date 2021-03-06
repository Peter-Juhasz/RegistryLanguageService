﻿using RegistryLanguageService.Syntax;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace RegistryLanguageService
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IOutliningRegionTag))]
    [ContentType(RegistryContentTypeNames.Registry)]
    internal sealed class RegistryOutliningTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return buffer.Properties.GetOrCreateSingletonProperty(
                creator: () => new RegistryOutliningTagger(buffer)
            ) as ITagger<T>;
        }


        private sealed class RegistryOutliningTagger : ITagger<IOutliningRegionTag>
        {
            public RegistryOutliningTagger(ITextBuffer buffer)
            {
                _buffer = buffer;
                _buffer.ChangedLowPriority += OnBufferChanged;
            }

            private readonly ITextBuffer _buffer;
            
            private void OnBufferChanged(object sender, TextContentChangedEventArgs e)
            {
                if (e.After != _buffer.CurrentSnapshot)
                    return;

                SnapshotSpan? changedSpan = null;

                // examine old version
                SyntaxTree oldSyntaxTree = e.Before.GetSyntaxTree();
                RegistryDocumentSyntax oldRoot = oldSyntaxTree.Root as RegistryDocumentSyntax;

                // find affected sections
                IReadOnlyCollection<RegistrySectionSyntax> oldChangedSections = (
                    from change in e.Changes
                    from section in oldRoot.Sections
                    where section.Span.IntersectsWith(change.OldSpan)
                    orderby section.Span.Start
                    select section
                ).ToList();

                if (oldChangedSections.Any())
                {
                    // compute changed span
                    changedSpan = new SnapshotSpan(
                        oldChangedSections.First().Span.Start,
                        oldChangedSections.Last().Span.End
                    );

                    // translate to new version
                    changedSpan = changedSpan.Value.TranslateTo(e.After, SpanTrackingMode.EdgeInclusive);
                }

                // examine current version
                SyntaxTree syntaxTree = e.After.GetSyntaxTree();
                RegistryDocumentSyntax root = syntaxTree.Root as RegistryDocumentSyntax;

                // find affected sections
                IReadOnlyCollection<RegistrySectionSyntax> changedSections = (
                    from change in e.Changes
                    from section in root.Sections
                    where section.Span.IntersectsWith(change.NewSpan)
                    orderby section.Span.Start
                    select section
                ).ToList();
                
                if (changedSections.Any())
                {
                    // compute changed span
                    SnapshotSpan newChangedSpan = new SnapshotSpan(
                        changedSections.First().Span.Start,
                        changedSections.Last().Span.End
                    );

                    changedSpan = changedSpan == null
                        ? newChangedSpan
                        : new SnapshotSpan(
                            changedSpan.Value.Start < newChangedSpan.Start ? changedSpan.Value.Start : newChangedSpan.Start,
                            changedSpan.Value.End > newChangedSpan.End ? changedSpan.Value.End : newChangedSpan.End
                        )
                    ;
                }

                // notify if any change affects outlining
                if (changedSpan != null)
                    this.TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(changedSpan.Value));
            }
            

            public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
            {
                ITextBuffer buffer = spans.First().Snapshot.TextBuffer;
                SyntaxTree syntax = buffer.GetSyntaxTree();
                RegistryDocumentSyntax root = syntax.Root as RegistryDocumentSyntax;

                return
                    from section in root.Sections
                    where section.Properties.Any()
                    where spans.IntersectsWith(section.Span)
                    let last = section.Properties.Last()
                    let collapsibleSpan = new SnapshotSpan(
                        section.ClosingBracketToken.Span.Span.End,
                        (last.TrailingTrivia.LastOrDefault() ?? last.ValueToken).Span.Span.End
                    )
                    select new TagSpan<IOutliningRegionTag>(
                        collapsibleSpan,
                        new OutliningRegionTag(
                            collapsedForm: "...",
                            collapsedHintForm: collapsibleSpan.GetText().Trim()
                        )
                    )
                ;
            }

            public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
        }
    }
}
