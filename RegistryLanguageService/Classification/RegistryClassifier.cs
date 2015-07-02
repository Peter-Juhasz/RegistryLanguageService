﻿using RegistryLanguageService.Syntax;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace RegistryLanguageService
{
    /// <summary>
    /// Classifier provider. It adds the classifier to the set of classifiers.
    /// </summary>
    [Export(typeof(IClassifierProvider))]
    [ContentType(RegistryContentTypeNames.Registry)] // This classifier applies to all text files.
    [Order(Before = Priority.High)]
    internal sealed class RegistryClassifierProvider : IClassifierProvider
    {
#pragma warning disable 649

        /// <summary>
        /// Classification registry to be used for getting a reference
        /// to the custom classification type later.
        /// </summary>
        [Import]
        private IClassificationTypeRegistryService classificationRegistry;

        [Import("Registry")]
        private ISyntacticParser syntacticParser;

#pragma warning restore 649


        /// <summary>
        /// Gets a classifier for the given text buffer.
        /// </summary>
        /// <param name="buffer">The <see cref="ITextBuffer"/> to classify.</param>
        /// <returns>A classifier for the text buffer, or null if the provider cannot do so in its current state.</returns>
        public IClassifier GetClassifier(ITextBuffer buffer)
        {
            return buffer.Properties.GetOrCreateSingletonProperty(
                creator: () => new RegistryClassifier(buffer, syntacticParser, this.classificationRegistry)
            );
        }


        /// <summary>
        /// Classifier that classifies all text as an instance of the "RegistryClassifier" classification type.
        /// </summary>
        private sealed class RegistryClassifier : IClassifier
        {
            /// <summary>
            /// Registrytializes a new instance of the <see cref="RegistryClassifier"/> class.
            /// </summary>
            /// <param name="registry">Classification registry.</param>
            public RegistryClassifier(ITextBuffer buffer, ISyntacticParser syntacticParser, IClassificationTypeRegistryService registry)
            {
                buffer.Properties.AddProperty(typeof(ISyntacticParser), syntacticParser);
            }

#pragma warning disable 67

            /// <summary>
            /// An event that occurs when the classification of a span of text has changed.
            /// </summary>
            /// <remarks>
            /// This event gets raised if a non-text change would affect the classification in some way,
            /// for example typing /* would cause the classification to change in C# without directly
            /// affecting the span.
            /// </remarks>
            public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

#pragma warning restore 67

            /// <summary>
            /// Gets all the <see cref="ClassificationSpan"/> objects that intersect with the given range of text.
            /// </summary>
            /// <remarks>
            /// This method scans the given SnapshotSpan for potential matches for this classification.
            /// In this instance, it classifies everything and returns each span as a new ClassificationSpan.
            /// </remarks>
            /// <param name="span">The span currently being classified.</param>
            /// <returns>A list of ClassificationSpans that represent spans identified to be of this classification.</returns>
            public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
            {
                SyntaxTree syntaxTree = span.Snapshot.GetSyntaxTree();

                return (
                    from token in syntaxTree.Root.GetTokens()
                    where !token.IsMissing
                    where token.Span.Span.IntersectsWith(span)
                    select token.Span
                ).ToList();
            }
        }
    }
}
