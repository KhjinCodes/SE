using System;

namespace IngameScript
{
    partial class Program
    {
        public class ScriptSettingsBinary
        {
            long stored_state = 0;

            public void Set(bool value, int shift)
            {
                stored_state = value ? (stored_state | (1L << shift)) : (stored_state & ~(1L << shift));
            }

            public bool Get(int shift)
            {
                return (stored_state & (1L << shift)) != 0;
            }
        
            public string Export()
            {
                string padded = Convert.ToString(stored_state, 2).PadLeft(64, '0');
                return padded.Substring(Math.Max(padded.Length - 65, 0));
            }

            public bool TryLoad(string data)
            {
                return long.TryParse(data, out stored_state);
            }
        }
    }
}
