using RegistryLanguageService.Syntax;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace RegistryLanguageService
{
    [Export(typeof(IViewTaggerProvider))]
    [TagType(typeof(ITextMarkerTag))]
    [ContentType(RegistryContentTypeNames.Registry)]
    internal sealed class RegistryBracketMatchingTaggerProvider : IViewTaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            return textView.Properties.GetOrCreateSingletonProperty(
                creator: () => new RegistryBracketMatchingTagger(textView)
            ) as ITagger<T>;
        }


        private sealed class RegistryBracketMatchingTagger : ITagger<ITextMarkerTag>
        {
            public RegistryBracketMatchingTagger(ITextView view)
            {
                _view = view;

                _view.Caret.PositionChanged += OnCaretPositionChanged;
            }

            private readonly ITextView _view;

            private static readonly ITextMarkerTag Tag = new TextMarkerTag("bracehighlight");


            private void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
            {
                ITextSnapshotLine oldLine = e.OldPosition.BufferPosition.GetContainingLine();
                ITextSnapshotLine newLine = e.NewPosition.BufferPosition.GetContainingLine();
                
                this.TagsChanged?.Invoke(this,
                    new SnapshotSpanEventArgs(new SnapshotSpan(newLine.Start, newLine.End))
                );

                if (newLine != oldLine)
                {
                    this.TagsChanged?.Invoke(this,
                        new SnapshotSpanEventArgs(new SnapshotSpan(oldLine.Start, oldLine.End))
                    );
                }
            }
            
            public IEnumerable<ITagSpan<ITextMarkerTag>> GetTags(NormalizedSnapshotSpanCollection spans)
            {
                ITextBuffer buffer = spans.First().Snapshot.TextBuffer;
                SyntaxTree syntax = buffer.GetSyntaxTree();
                RegistryDocumentSyntax root = syntax.Root as RegistryDocumentSyntax;

                SnapshotPoint caret = _view.Caret.Position.BufferPosition;

                RegistrySectionSyntax section = root.Sections
                    .FirstOrDefault(s => s.Span.ContainsOrEndsWith(caret));

                if (section != null)
                {
                    // match brackets
                    if (
                        (!section.OpeningBracketToken.IsMissing && !section.ClosingBracketToken.IsMissing) &&
                        (section.OpeningBracketToken.Span.Span.Start == caret || section.ClosingBracketToken.Span.Span.End == caret)
                    )
                    {
                        yield return new TagSpan<ITextMarkerTag>(section.OpeningBracketToken.Span.Span, Tag);
                        yield return new TagSpan<ITextMarkerTag>(section.ClosingBracketToken.Span.Span, Tag);
                    }

                    else
                    {
                        RegistryPropertySyntax property = section.Properties
                            .FirstOrDefault(p => p.Span.ContainsOrEndsWith(caret));

                        if (property != null)
                        {
                            // match name quotes
                            if (
                                (!property.NameOpeningQuoteToken.IsMissing && !property.NameClosingQuoteToken.IsMissing) &&
                                (property.NameOpeningQuoteToken.Span.Span.Start == caret || property.NameClosingQuoteToken.Span.Span.End == caret)
                            )
                            {
                                yield return new TagSpan<ITextMarkerTag>(property.NameOpeningQuoteToken.Span.Span, Tag);
                                yield return new TagSpan<ITextMarkerTag>(property.NameClosingQuoteToken.Span.Span, Tag);
                            }
                            
                            // match value quotes
                            else if (
                                (!property.ValueOpeningQuoteToken.IsMissing && !property.ValueClosingQuoteToken.IsMissing) &&
                                (property.ValueOpeningQuoteToken.Span.Span.Start == caret || property.ValueClosingQuoteToken.Span.Span.End == caret)
                            )
                            {
                                yield return new TagSpan<ITextMarkerTag>(property.ValueOpeningQuoteToken.Span.Span, Tag);
                                yield return new TagSpan<ITextMarkerTag>(property.ValueClosingQuoteToken.Span.Span, Tag);
                            }

                            // match type specifier braces
                            else if (
                                (!property.TypeSpecifierOpeningBraceToken.IsMissing && !property.TypeSpecifierClosingBraceToken.IsMissing) &&
                                (property.TypeSpecifierOpeningBraceToken.Span.Span.Start == caret || property.TypeSpecifierClosingBraceToken.Span.Span.End == caret)
                            )
                            {
                                yield return new TagSpan<ITextMarkerTag>(property.TypeSpecifierOpeningBraceToken.Span.Span, Tag);
                                yield return new TagSpan<ITextMarkerTag>(property.TypeSpecifierClosingBraceToken.Span.Span, Tag);
                            }
                        }
                    }
                }
            }

            public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
        }
    }
}
