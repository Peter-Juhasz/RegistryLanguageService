using System;
using System.Collections.Generic;
using System.Linq;

namespace RegistryLanguageService.Syntax
{
    internal static class RegistrySyntaxFacts
    {
        public const char Comment = ';';

        public const char KeySeparator = '\\';

        public const char PropertyNameValueDelimiter = '=';

        public const char TypeValueDelimiter = ':';

        public const char DeleteKey = '-';

        public const char Quote = '"';

        public const char SectionNameOpeningBracket = '[';

        public const char SectionNameClosingBracket = ']';

        public const char TypeSpecifierOpeningBrace = '(';

        public const char TypeSpecifierClosingBrace = ')';


        public static readonly IReadOnlyCollection<string> KnownDataTypesAndShortcuts = new string[] { "dword", "hex", "hexadecimal" };

        public static bool IsKnownDataTypeNameOrShortcut(string dataType)
        {
            return KnownDataTypesAndShortcuts.Contains(dataType, StringComparer.InvariantCultureIgnoreCase);
        }


        public static readonly IReadOnlyCollection<string> Versions = new []
        {
            "REGEDIT4",
            "Windows Registry Editor Version 5.00",
        };

        public static readonly IReadOnlyCollection<string> PredefinedKeys = new[]
        {
            "HKEY_CLASSES_ROOT",
            "HKEY_CURRENT_CONFIG",
            "HKEY_CURRENT_USER",
            "HKEY_CURRENT_USER_LOCAL_SETTINGS",
            "HKEY_DYN_DATA",
            "HKEY_LOCAL_MACHINE",
            "HKEY_PERFORMANCE_DATA",
            "HKEY_PERFORMANCE_NLSTEXT",
            "HKEY_PERFORMANCE_TEXT",
            "HKEY_USERS",
        };
    }
}
