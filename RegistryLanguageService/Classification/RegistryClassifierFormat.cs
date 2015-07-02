//------------------------------------------------------------------------------
// <copyright file="RegistryClassifierFormat.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace RegistryLanguageService
{
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "Registry/Delimiter")]
    [Name("Registry/Delimiter")]
    [UserVisible(true)] // This should be visible to the end user
    [Order(Before = Priority.Default)] // Set the priority to be after the default classifiers
    internal sealed class RegistryDelimiterClassificationFormat : ClassificationFormatDefinition
    {
        public RegistryDelimiterClassificationFormat()
        {
            this.DisplayName = "Registry Delimiter"; // Human readable version of the name
            this.ForegroundColor = Colors.Blue;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "Registry/Type")]
    [Name("Registry/Type")]
    [UserVisible(true)] // This should be visible to the end user
    [Order(Before = Priority.Default)] // Set the priority to be after the default classifiers
    internal sealed class RegistryTypeClassificationFormat : ClassificationFormatDefinition
    {
        public RegistryTypeClassificationFormat()
        {
            this.DisplayName = "Registry Data Type"; // Human readable version of the name
            this.ForegroundColor = Color.FromRgb(43, 145, 175);
        }
    }
}
