using Microsoft.Win32;
using RegistryLanguageService.Semantics;
using System.Collections.Generic;

namespace RegistryLanguageService.Documentation
{
    public static class RegistryDocumentation
    {
        private static readonly IReadOnlyDictionary<RegistryValueKind, string> TypeMap = new Dictionary<RegistryValueKind, string>()
        {
            { RegistryValueKind.DWord, "32-bit number." },
            { RegistryValueKind.String, "Null-terminated string. It will be a Unicode or ANSI string, depending on whether you use the Unicode or ANSI functions." },
            { RegistryValueKind.Binary, "Binary data in any form." },
        };

        public static string GetDocumentation(RegistryTypeSymbol type)
        {
            return TypeMap[type.Type];
        }
    }
}
