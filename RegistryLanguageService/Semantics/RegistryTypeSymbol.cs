using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegistryLanguageService.Semantics
{
    public class RegistryTypeSymbol
    {
        public RegistryTypeSymbol(RegistryValueKind type)
        {
            this.Type = type;
        }

        private readonly IReadOnlyDictionary<RegistryValueKind, string> NameMap = new Dictionary<RegistryValueKind, string>()
        {
            { RegistryValueKind.DWord, "REG_DWORD" },
            { RegistryValueKind.String, "REG_SZ" },
            { RegistryValueKind.Binary, "REG_BINARY" },
        };

        public string Name
        {
            get { return NameMap[this.Type]; }
        }

        public RegistryValueKind Type { get; private set; }

        public static RegistryTypeSymbol FromAlias(string alias)
        {
            return new RegistryTypeSymbol(GetValueKindFromTypeAlias(alias));
        }


        public static RegistryValueKind GetValueKindFromTypeAlias(string alias)
        {
            if (alias.Equals("dword", StringComparison.InvariantCultureIgnoreCase))
                return RegistryValueKind.DWord;

            if (alias.Equals("hex", StringComparison.InvariantCultureIgnoreCase) ||
                alias.Equals("hexadecimal", StringComparison.InvariantCultureIgnoreCase))
                return RegistryValueKind.Binary;

            else
                return RegistryValueKind.String;
        }
    }
}
