using System;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class ScriptSettings
        {
            private MyIni myIni = new MyIni();
            private string scriptName = string.Empty;

            public ScriptSettings(string scriptName)
            {
                this.scriptName = scriptName;
            }

            public void SetComment(string comment, string key, string section)
            {
                MyIniKey iniKey = new MyIniKey($"{scriptName} - {section}", key);
                if (comment == string.Empty) { comment = null; }
                myIni.SetComment(iniKey, comment);
            }

            public void SetSectionComment(string comment, string section)
            {
                section = $"{scriptName} - {section}";
                myIni.SetSectionComment(section, comment);
            }

            public void Set<T>(string key, string section, T value)
            {
                MyIniKey iniKey = new MyIniKey($"{scriptName} - {section}", key);
                myIni.Set(iniKey, value.ToString());
            }

            public T Get<T>(string key, string section)
            {
                MyIniKey iniKey = new MyIniKey($"{scriptName} - {section}", key);
                return (T)Convert.ChangeType(myIni.Get(iniKey).ToString(), typeof(T));
            }

            public T Get<T>(string key, string section, T defaultValue)
            {
                MyIniKey iniKey = new MyIniKey($"{scriptName} - {section}", key);
                if (!myIni.ContainsKey(iniKey))
                { myIni.Set(iniKey, defaultValue.ToString()); return defaultValue; }
                else
                { return (T)Convert.ChangeType(myIni.Get(iniKey).ToString(), typeof(T)); }
            }

            public Color GetColor(string key, string section, Color defaultValue)
            {
                MyIniKey iniKey = new MyIniKey($"{scriptName} - {section}", key);
                if (!myIni.ContainsKey(iniKey))
                { 
                    myIni.Set(iniKey, $"{defaultValue.R}, {defaultValue.G}, {defaultValue.B}, {defaultValue.A}"); 
                    return defaultValue; 
                }
                else
                {
                    string[] parts = myIni.Get(iniKey).ToString().Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries);
                    byte r = byte.Parse(parts[0].Trim());
                    byte g = byte.Parse(parts[1].Trim());
                    byte b = byte.Parse(parts[2].Trim());
                    byte a = byte.Parse(parts[3].Trim());
                    return new Color(r, g, b, a); 
                }
            }

            public string Export()
            {
                return myIni.ToString();
            }

            public bool TryLoad(string data)
            {
                myIni.Invalidate();
                return myIni.TryParse(data);
            }
        
            public void Clear()
            {
                myIni.Clear();
            }
        }
    }
}
