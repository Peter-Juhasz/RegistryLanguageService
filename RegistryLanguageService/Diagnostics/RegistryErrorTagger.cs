﻿using RegistryLanguageService.Diagnostics;
using RegistryLanguageService.Syntax;
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
    [TagType(typeof(IErrorTag))]
    [ContentType(RegistryContentTypeNames.Registry)]
    internal sealed class RegistryErrorTaggerProvider : ITaggerProvider
    {
#pragma warning disable 649

        [ImportMany]
        private IEnumerable<IDiagnosticAnalyzer> analyzers;

#pragma warning restore 649


        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return buffer.Properties.GetOrCreateSingletonProperty(
                creator: () => new RegistryErrorTagger(analyzers)
            ) as ITagger<T>;
        }


        private sealed class RegistryErrorTagger : ITagger<IErrorTag>
        {
            public RegistryErrorTagger(IEnumerable<IDiagnosticAnalyzer> analyzers)
            {
                _analyzers = analyzers;
            }

            private readonly IEnumerable<IDiagnosticAnalyzer> _analyzers;
            

            public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
            {
                ITextBuffer buffer = spans.First().Snapshot.TextBuffer;
                SyntaxTree syntax = buffer.GetSyntaxTree();

                return
                    // find intersecting nodes
                    from node in syntax.Root.DescendantsAndSelf()
                    where spans.IntersectsWith(node.Span)
                    let type = node.GetType()
                    
                    // find analyzers for node
                    from analyzer in _analyzers
                    from @interface in analyzer.GetType().GetInterfaces()
                    where @interface.IsGenericType
                       && @interface.GetGenericTypeDefinition() == typeof(ISyntaxNodeAnalyzer<>)
                    let analyzerNodeType = @interface.GetGenericArguments().Single()
                    where analyzerNodeType.IsAssignableFrom(type)

                    // analyze node
                    from diagnostic in typeof(ISyntaxNodeAnalyzer<>)
                        .MakeGenericType(analyzerNodeType)
                        .GetMethod("Analyze")
                        .Invoke(analyzer, new [] { node }) as IEnumerable<ITagSpan<IErrorTag>>
                    select diagnostic
                ;
            }

            public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
        }
    }
}
