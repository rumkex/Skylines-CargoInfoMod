using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ColossalFramework.Globalization;
using ColossalFramework.Plugins;
using UnityEngine;

namespace CargoInfoMod
{
    public class Localization
    {
        private static readonly Dictionary<string, Locale> localeStore = new Dictionary<string, Locale>();

        private static Locale LocaleFromFile(string file)
        {
            var locale = new Locale();
            using (var reader = new StreamReader(File.OpenRead(file)))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var rows = line.Split('\t');
                    if (rows.Length < 2)
                    {
                        Debug.LogErrorFormat("Not enough tabs in locale string from {0}:\n'{1}'", file, line);
                        continue;
                    }
                    locale.AddLocalizedString(new Locale.Key { m_Identifier = rows[0] }, rows[1]);
                }
            }
            return locale;
        }

        public static string Get(string id)
        {
            var lang = LocaleManager.instance.language ?? "en";
            if (!localeStore.ContainsKey(lang))
            {
                Debug.Log(Assembly.GetExecutingAssembly().Location);
                var modPath = PluginManager.instance.FindPluginInfo(Assembly.GetExecutingAssembly()).modPath;
                var localePath = Path.Combine(modPath, $"Locales/{lang}.txt");
                if (!File.Exists(localePath))
                {
                    localeStore.Add(lang, localeStore["en"]);
                }
                localeStore.Add(lang, LocaleFromFile(localePath));
            }
            return localeStore[lang].Get(new Locale.Key { m_Identifier = id });
        }
    }
}
