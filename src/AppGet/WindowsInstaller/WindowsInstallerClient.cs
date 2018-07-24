﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;

namespace AppGet.WindowsInstaller
{
    public class WindowsInstallerClient
    {
        private static readonly RegistryView[] Views = { RegistryView.Registry32, RegistryView.Registry64 };
        private static readonly RegistryHive[] Hives = { RegistryHive.LocalMachine, RegistryHive.CurrentUser };
        private static readonly string[] Paths =
        {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UpgradeCodes",
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
        };

        public IEnumerable<WindowsInstallerItem> GetKeys()
        {
            var keys = new List<WindowsInstallerItem>();

            foreach (var hive in Hives)
            {
                foreach (var view in Views)
                {
                    foreach (var path in Paths)
                    {
                        keys.InsertRange(0, GetInstallRecords(path, hive, view).ToArray());
                    }
                }
            }

            return keys;
        }

        private IEnumerable<WindowsInstallerItem> GetInstallRecords(string keyName, RegistryHive hive, RegistryView view)
        {
            using (var baseKey = RegistryKey.OpenBaseKey(hive, view))
            using (var key = baseKey.OpenSubKey(keyName, false))
            {
                if (key != null)
                {
                    foreach (var subKeyName in key.GetSubKeyNames())
                    {
                        var subKey = key.OpenSubKey(subKeyName);
                        var installerKey = GetKeyDictionary(subKey, subKeyName);
                        if (installerKey != null)
                        {
                            yield return installerKey;
                        }
                    }
                }
            }

            WindowsInstallerItem GetKeyDictionary(RegistryKey registryKey, string id)
            {
                var names = registryKey.GetValueNames().ToList();

                if (!names.Any() || names.Contains("SystemComponent"))
                {
                    return null;
                }

                var values = names.ToDictionary(c => c, key => JToken.FromObject(registryKey.GetValue(key)));

                return new WindowsInstallerItem
                {
                    Id = id,
                    Is64 = registryKey.View == RegistryView.Registry64,
                    Hive = hive.ToString(),
                    IsUpgradeNode = keyName.EndsWith("UpgradeCodes"),
                    Values = values,
                };
            }
        }

    }
}