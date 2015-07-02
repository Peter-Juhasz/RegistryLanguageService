using Microsoft.Win32;
using RegistryLanguageService.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RegistryLanguageService.Semantics
{
    public class RegistryKeySymbol
    {
        public RegistryKeySymbol(string fullName)
        {
            this.FullName = fullName;
        }
        

        public string Name
        {
            get { return this.FullName.Split(RegistrySyntaxFacts.KeySeparator).Last(); }
        }

        public string FullName { get; private set; }

        public RegistryKey Key
        {
            get
            {
                return GetKeyFromFullName(this.FullName);
            }
        }

        public RegistryKeySymbol ContainingSymbol
        {
            get { return this.Parent; }
        }

        public RegistryKeySymbol Parent
        {
            get
            {
                string[] parts = this.FullName.Split(RegistrySyntaxFacts.KeySeparator);

                if (parts.Length == 1)
                    return null;

                return RegistryKeySymbol.FromFullName(
                    String.Join(RegistrySyntaxFacts.KeySeparator.ToString(), parts.Take(parts.Length - 1))
                );
            }
        }

        public IReadOnlyCollection<RegistryKeySymbol> SubKeys
        {
            get
            {
                RegistryKey key = this.Key;

                if (key != null)
                {
                    return key.GetSubKeyNames()
                        .Select(sk => RegistryKeySymbol.FromFullName(String.Join(RegistrySyntaxFacts.KeySeparator.ToString(), this.FullName, sk)))
                        .ToList();
                }

                return Enumerable.Empty<RegistryKeySymbol>().ToList();
            }
        }


        public static RegistryKeySymbol FromFullName(string fullName)
        {
            return new RegistryKeySymbol(fullName);
        }
        
        public static RegistryKey GetKeyFromFullName(string fullName)
        {
            string[] parts = fullName.Split(RegistrySyntaxFacts.KeySeparator);
            RegistryKey hive = GetPredefinedKey(parts.First());

            if (hive == null)
                return null;

            if (parts.Length == 1)
                return hive;

            return hive.OpenSubKey(
                String.Join(RegistrySyntaxFacts.KeySeparator.ToString(), parts.Skip(1))
            );
        }
        
        public static RegistryKey GetPredefinedKey(string predefinedKey)
        {
            if (predefinedKey.Equals("HKEY_CLASSES_ROOT", StringComparison.InvariantCultureIgnoreCase))
                return Registry.ClassesRoot;

            if (predefinedKey.Equals("HKEY_CURRENT_CONFIG", StringComparison.InvariantCultureIgnoreCase))
                return Registry.CurrentConfig;

            if (predefinedKey.Equals("HKEY_CURRENT_USER", StringComparison.InvariantCultureIgnoreCase))
                return Registry.CurrentUser;

            if (predefinedKey.Equals("HKEY_DYN_DATA", StringComparison.InvariantCultureIgnoreCase))
                return Registry.DynData;

            if (predefinedKey.Equals("HKEY_LOCAL_MACHINE", StringComparison.InvariantCultureIgnoreCase))
                return Registry.LocalMachine;
            
            if (predefinedKey.Equals("HKEY_PERFORMANCE_DATA", StringComparison.InvariantCultureIgnoreCase))
                return Registry.PerformanceData;

            if (predefinedKey.Equals("HKEY_USERS", StringComparison.InvariantCultureIgnoreCase))
                return Registry.Users;

            else
                return null;
        }
    }
}
