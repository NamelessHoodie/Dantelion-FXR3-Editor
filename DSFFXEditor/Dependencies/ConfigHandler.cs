using System;
using System.Collections.Generic;
using System.Text;

namespace DSFFXEditor
{
    class IniConfigFile
    {
        string Section;
        string Key;
        public string Value;
        string DefaultValue;
        IniFile Configs;
        public IniConfigFile(string Section, string Key, object DefaultValue, string IniPath)
        {
            this.Section = Section;
            this.Key = Key;
            this.DefaultValue = DefaultValue.ToString();
            this.Value = DefaultValue.ToString();
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
                Value = Configs.Read(Key, Section);
                return Value;
            }
        }

        public void WriteConfigsIni(Object NewValue)
        {
            Configs.Write(Key, NewValue.ToString(), Section);
        }
    }
}
