using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using RegistryLanguageService.Semantics;
using RegistryLanguageService.Syntax;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Media;

namespace RegistryLanguageService.CodeCompletion
{
    [Export(typeof(ICompletionSourceProvider))]
    [ContentType(RegistryContentTypeNames.Registry)]
    [Name("Registry Completion")]
    internal sealed class RegistryCompletionSourceProvider : ICompletionSourceProvider
    {
#pragma warning disable 649

        [Import]
        private IGlyphService glyphService;

#pragma warning restore 649


        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
        {
            return new RegistryCompletionSource(textBuffer, glyphService);
        }


        private sealed class RegistryCompletionSource : ICompletionSource
        {
            public RegistryCompletionSource(ITextBuffer buffer, IGlyphService glyphService)
            {
                _buffer = buffer;
                _versionGlyph = glyphService.GetGlyph(StandardGlyphGroup.GlyphKeyword, StandardGlyphItem.GlyphItemPublic);
                _keyGlyph = glyphService.GetGlyph(StandardGlyphGroup.GlyphClosedFolder, StandardGlyphItem.GlyphItemPublic);
                _dataTypeGlyph = glyphService.GetGlyph(StandardGlyphGroup.GlyphGroupStruct, StandardGlyphItem.GlyphItemPublic);
            }

            private readonly ITextBuffer _buffer;
            private readonly ImageSource _versionGlyph, _keyGlyph, _dataTypeGlyph;
            private bool _disposed = false;
            

            public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
            {
                if (_disposed)
                    return;
                
                // get snapshot
                ITextSnapshot snapshot = _buffer.CurrentSnapshot;
                SnapshotPoint? triggerPoint = session.GetTriggerPoint(snapshot);
                if (triggerPoint == null)
                    return;
                
                ITrackingSpan applicableTo = snapshot.CreateTrackingSpan(new SnapshotSpan(triggerPoint.Value, triggerPoint.Value), SpanTrackingMode.EdgeInclusive);

                // get or compute syntax tree
                SyntaxTree syntaxTree = snapshot.GetSyntaxTree();
                RegistryDocumentSyntax root = syntaxTree.Root as RegistryDocumentSyntax;

                // find section
                RegistrySectionSyntax section = root.Sections
                    .FirstOrDefault(s => s.Span.ContainsOrEndsWith(triggerPoint.Value));

                if (section != null)
                {
                    // keys
                    if (!section.OpeningBracketToken.IsMissing &&
                        section.OpeningBracketToken.Span.Span.End <= triggerPoint.Value &&
                        (section.ClosingBracketToken.IsMissing || triggerPoint.Value <= section.ClosingBracketToken.Span.Span.Start)
                    )
                    {
                        SnapshotToken token = section.NameSyntax.Tokens
                            .FirstOrDefault(t => t.Span.Span.ContainsOrEndsWith(triggerPoint.Value));

                        if (token != null)
                            applicableTo = snapshot.CreateTrackingSpan(token.Span.Span, SpanTrackingMode.EdgeInclusive);

                        SnapshotToken first = section.NameSyntax.Tokens.FirstOrDefault();

                        // predefined keys
                        if (section.NameSyntax.Tokens.Count <= 1 || token == first)
                        {
                            IReadOnlyList<Completion> completions = RegistrySyntaxFacts.PredefinedKeys
                                .Select(v => new Completion(v, v, null, _keyGlyph, v))
                                .ToList();

                            completionSets.Add(
                                new CompletionSet("All", "All", applicableTo, completions, null)
                            );
                        }

                        // all other keys
                        else
                        {
                            string fullName = String.Join(RegistrySyntaxFacts.KeySeparator.ToString(),
                                section.NameSyntax.Tokens
                                    .TakeTo(token)
                                    .Select(t => t.Value)
                            );
                            
                            IReadOnlyList<Completion> completions = RegistryKeySymbol.FromFullName(fullName).Parent
                                .SubKeys
                                .Select(v => new Completion(v.Name, v.Name, v.FullName, _keyGlyph, v.FullName))
                                .ToList();

                            completionSets.Add(
                                new CompletionSet("All", "All", applicableTo, completions, null)
                            );
                        }
                    }

                    // find property
                    RegistryPropertySyntax valueSyntax = section.Properties
                        .FirstOrDefault(s => s.Span.ContainsOrEndsWith(triggerPoint.Value));

                    if (valueSyntax != null)
                    {
                        // type aliases
                        if (!valueSyntax.TypeToken.IsMissing &&
                            valueSyntax.TypeToken.Span.Span.ContainsOrEndsWith(triggerPoint.Value))
                        {
                            applicableTo = snapshot.CreateTrackingSpan(valueSyntax.TypeToken.Span.Span, SpanTrackingMode.EdgeInclusive);

                            IReadOnlyList<Completion> completions = RegistrySyntaxFacts.KnownDataTypesAndShortcuts
                                .Select(v => new Completion(v, v, null, _dataTypeGlyph, v))
                                .ToList();
                            
                            completionSets.Add(
                                new CompletionSet("All", "All", applicableTo, completions, null)
                            );
                        }
                    }
                }

                // version
                if (!root.VersionToken.IsMissing &&
                    root.VersionToken.Span.Span.ContainsOrEndsWith(triggerPoint.Value))
                {
                    applicableTo = snapshot.CreateTrackingSpan(root.VersionToken.Span.Span, SpanTrackingMode.EdgeInclusive);

                    IReadOnlyList<Completion> completions = RegistrySyntaxFacts.Versions
                        .Select(v => new Completion(v, v, null, _versionGlyph, v))
                        .ToList();

                    completionSets.Add(
                        new CompletionSet("All", "All", applicableTo, completions, null)
                    );
                }
            }
            

            public void Dispose()
            {
                _disposed = true;
            }
        }
    }
}
