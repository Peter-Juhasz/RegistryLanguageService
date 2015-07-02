//------------------------------------------------------------------------------
// <copyright file="RegistryClassifierClassificationDefinition.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace RegistryLanguageService
{
    /// <summary>
    /// Classification type definition export for RegistryClassifier
    /// </summary>
    internal static class RegistryContentTypeDefinition
    {
#pragma warning disable 649

        [Export]
        [Name("Registry")]
        [BaseDefinition("code")]
        internal static ContentTypeDefinition contentTypeDefinition;

        [Export]
        [FileExtension(".reg")]
        [ContentType(RegistryContentTypeNames.Registry)]
        internal static FileExtensionToContentTypeDefinition fileExtensionToContentTypeDefinition;

#pragma warning restore 649
    }
}
