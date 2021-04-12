using System;
using System.Collections.Generic;
using System.Text;

namespace DSFFXEditor
{
    class EditorConfig
    {
        string Section;
        string Key;
        string DefaultValue;
        IniFile Configs;
        public EditorConfig(string Section, string Key, string DefaultValue, string IniPath)
        {
            this.Section = Section;
            this.Key = Key;
            this.DefaultValue = DefaultValue;
            Configs = new IniFile(IniPath);
        }
        public string ReadConfigsIni()
        {
            if (!Configs.KeyExists(Key, Section))
            {
                Configs.Write(Key, DefaultValue, Section);
                return DefaultValue;
            }
            else
            {
                return Configs.Read(Key, Section);
            }
        }

        public void WriteConfigsIni(string NewValue)
        {
            Configs.Write(Key, NewValue, Section);
        }
    }

    class DSFFXConfig 
    {
        public static int _themeSelectorSelectedItem = 0;
        public static string _activeTheme = "DarkRedClay";
        private static string iniPath = "Config/EditorConfigs.ini";
        private static EditorConfig theme = new EditorConfig("General", "Theme", _activeTheme, iniPath);
        private static EditorConfig themeIndex = new EditorConfig("General", "ThemeIndex", _themeSelectorSelectedItem.ToString(), iniPath);
        public static void ReadConfigs() 
        {
            _activeTheme = theme.ReadConfigsIni();
            _themeSelectorSelectedItem = Int32.Parse(themeIndex.ReadConfigsIni());
        }

        public static void SaveConfigs() 
        {
            theme.WriteConfigsIni(_activeTheme);
            themeIndex.WriteConfigsIni(_themeSelectorSelectedItem.ToString());
        }
    }
}
