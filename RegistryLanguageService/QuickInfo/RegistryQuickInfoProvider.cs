using RegistryLanguageService.Syntax;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.VisualStudio.Language.StandardClassification;
using RegistryLanguageService.Semantics;
using Microsoft.Win32;
using RegistryLanguageService.Documentation;

namespace RegistryLanguageService.QuickInfo
{
    [Export(typeof(IQuickInfoSourceProvider))]
    [Name("Registry Quick Info Provider")]
    [ContentType(RegistryContentTypeNames.Registry)]
    internal sealed class RegistryQuickInfoSourceProvider : IQuickInfoSourceProvider
    {
#pragma warning disable 649

        [Import]
        private IGlyphService glyphService;

        [Import]
        private IClassificationTypeRegistryService classificationRegistry;

        [Import]
        private IClassificationFormatMapService classificationFormatMapService;

#pragma warning restore 649


        public IQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty(
                creator: () => new RegistryQuickInfoSource(
                    textBuffer,
                    glyphService,
                    classificationFormatMapService, 
                    classificationRegistry
                )
            );
        }


        private sealed class RegistryQuickInfoSource : IQuickInfoSource
        {
            public RegistryQuickInfoSource(
                ITextBuffer buffer,
                IGlyphService glyphService,
                IClassificationFormatMapService classificationFormatMapService,
                IClassificationTypeRegistryService classificationRegistry
            )
            {
                
                _buffer = buffer;
                _glyphService = glyphService;
                _classificationFormatMapService = classificationFormatMapService;
                _classificationRegistry = classificationRegistry;
            }

            private readonly ITextBuffer _buffer;
            private readonly IGlyphService _glyphService;
            private readonly IClassificationFormatMapService _classificationFormatMapService;
            private readonly IClassificationTypeRegistryService _classificationRegistry;

            private static readonly DataTemplate Template;
        

            public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> quickInfoContent, out ITrackingSpan applicableToSpan)
            {
                ITextSnapshot snapshot = _buffer.CurrentSnapshot;
                ITrackingPoint triggerPoint = session.GetTriggerPoint(_buffer);
                SnapshotPoint point = triggerPoint.GetPoint(snapshot);

                SyntaxTree syntax = snapshot.GetSyntaxTree();
                RegistryDocumentSyntax root = syntax.Root as RegistryDocumentSyntax;

                IClassificationFormatMap formatMap = _classificationFormatMapService.GetClassificationFormatMap(session.TextView);

                applicableToSpan = null;

                // find section
                RegistrySectionSyntax section = root.Sections
                    .FirstOrDefault(s => s.Span.ContainsOrEndsWith(point));
                
                if (section != null)
                {
                    RegistryKeySymbol keySymbol = RegistryKeySymbol.FromFullName(
                        String.Join(RegistrySyntaxFacts.KeySeparator.ToString(),
                            section.NameSyntax.Tokens.Select(t => t.Value)
                        )
                    );

                    // provide info about section
                    if (section.NameSyntax.Tokens.Any() &&
                        section.NameSyntax.Span.Span.Contains(point))
                    {
                        // get partial name
                        var token = section.NameSyntax.Tokens
                            .FirstOrDefault(s => s.Span.Span.ContainsOrEndsWith(point));

                        if (token == null)
                            return;

                        string partialName = String.Join(RegistrySyntaxFacts.KeySeparator.ToString(),
                            section.NameSyntax.Tokens
                                .TakeTo(token)
                                .Select(t => t.Value)
                        );
                        RegistryKeySymbol partialKeySymbol = RegistryKeySymbol.FromFullName(partialName);

                        // get glyph and rich formatting
                        var glyph = _glyphService.GetGlyph(StandardGlyphGroup.GlyphOpenFolder, StandardGlyphItem.GlyphItemPublic);
                        var classificationType = _classificationRegistry.GetClassificationType(PredefinedClassificationTypeNames.Identifier);
                        var format = formatMap.GetTextProperties(classificationType);
                        
                        // construct content
                        var content = new QuickInfoContent
                        {
                            Glyph = glyph,
                            Signature = new Run(partialKeySymbol.FullName) { Foreground = format.ForegroundBrush },
                        };
                        
                        // add to session
                        quickInfoContent.Add(
                            new ContentPresenter
                            {
                                Content = content,
                                ContentTemplate = Template,
                            }
                        );
                        applicableToSpan = snapshot.CreateTrackingSpan(token.Span.Span, SpanTrackingMode.EdgeInclusive);
                        return;
                    }

                    // provide info about property
                    RegistryPropertySyntax property = section.Properties
                        .FirstOrDefault(p => p.Span.ContainsOrEndsWith(point));

                    if (property != null)
                    {
                        // name
                        if (!property.NameToken.IsMissing && property.NameToken.Span.Span.ContainsOrEndsWith(point))
                        {
                            string propertyName = property.NameToken.Value;

                            // get glyph
                            var glyph = _glyphService.GetGlyph(StandardGlyphGroup.GlyphGroupProperty, StandardGlyphItem.GlyphItemPublic);
                            var classificationType = _classificationRegistry.GetClassificationType(PredefinedClassificationTypeNames.Identifier);
                            var format = formatMap.GetTextProperties(classificationType);

                            // construct content
                            var content = new QuickInfoContent
                            {
                                Glyph = glyph,
                                Signature = new Run(propertyName) { Foreground = format.ForegroundBrush },
                                Documentation = keySymbol.FullName,
                            };

                            // add to session
                            quickInfoContent.Add(
                                new ContentPresenter
                                {
                                    Content = content,
                                    ContentTemplate = Template,
                                }
                            );
                            applicableToSpan = snapshot.CreateTrackingSpan(property.NameToken.Span.Span, SpanTrackingMode.EdgeInclusive);
                            return;
                        }

                        // type shortcut
                        if (!property.TypeToken.IsMissing && property.TypeToken.Span.Span.ContainsOrEndsWith(point))
                        {
                            string alias = property.TypeToken.Value;
                            RegistryTypeSymbol type = RegistryTypeSymbol.FromAlias(alias);

                            // get glyph
                            var glyph = _glyphService.GetGlyph(StandardGlyphGroup.GlyphGroupStruct, StandardGlyphItem.GlyphItemPublic);
                            var classificationType = _classificationRegistry.GetClassificationType("Registry/Type");
                            var format = formatMap.GetTextProperties(classificationType);

                            // construct content
                            var content = new QuickInfoContent
                            {
                                Glyph = glyph,
                                Signature = new Run(type.Name) { Foreground = format.ForegroundBrush },
                                Documentation = RegistryDocumentation.GetDocumentation(type),
                            };

                            // add to session
                            quickInfoContent.Add(
                                new ContentPresenter
                                {
                                    Content = content,
                                    ContentTemplate = Template,
                                }
                            );
                            applicableToSpan = snapshot.CreateTrackingSpan(property.TypeToken.Span.Span, SpanTrackingMode.EdgeInclusive);
                            return;
                        }
                    }
                }
            }

            void IDisposable.Dispose()
            { }


            static RegistryQuickInfoSource()
            {
                var resources = new ResourceDictionary { Source = new Uri("pack://application:,,,/RegistryLanguageService;component/Themes/Generic.xaml", UriKind.RelativeOrAbsolute) };

                Template = resources.Values.OfType<DataTemplate>().First();
            }
        }
    }
}
