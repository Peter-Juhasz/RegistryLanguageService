//------------------------------------------------------------------------------
// <copyright file="RegistryClassifierClassificationDefinition.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace RegistryLanguageService
{
    /// <summary>
    /// Classification type definition export for RegistryClassifier
    /// </summary>
    internal static class RegistryClassifierClassificationDefinitions
    {
#pragma warning disable 169
        
        [Export(typeof(ClassificationTypeDefinition))]
        [Name("Registry/Delimiter")]
        private static ClassificationTypeDefinition delimiter;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("Registry/Type")]
        private static ClassificationTypeDefinition type;

#pragma warning restore 169
    }
}
