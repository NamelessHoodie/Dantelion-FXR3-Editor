using System;
using System.Collections.Generic;
using System.Text;

namespace DFXR3Editor
{
    class IniConfigFile
    {
        string _section;
        string _key;
        public string Value;
        string _defaultValue;
        IniFile _configs;
        public IniConfigFile(string section, string key, object defaultValue, string iniPath)
        {
            this._section = section;
            this._key = key;
            this._defaultValue = defaultValue.ToString();
            this.Value = defaultValue.ToString();
            _configs = new IniFile(iniPath);
        }
        public string ReadConfigsIni()
        {
            if (!_configs.KeyExists(_key, _section))
            {
                _configs.Write(_key, _defaultValue, _section);
                return _defaultValue;
            }
            else
            {
                Value = _configs.Read(_key, _section);
                return Value;
            }
        }

        public void WriteConfigsIni(Object newValue)
        {
            _configs.Write(_key, newValue.ToString(), _section);
        }
    }
}
