using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class PairedLists<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
        {
            private List<TKey> keys = new List<TKey>();
            private List<TValue> values = new List<TValue>();

            public void Add(TKey key, TValue value)
            {
                keys.Add(key);
                values.Add(value);
            }

            public bool Remove(TKey key)
            {
                int index = keys.IndexOf(key);
                if (index == -1)
                    return false;

                keys.RemoveAt(index);
                values.RemoveAt(index);
                return true;
            }

            public TValue this[TKey key]
            {
                get
                {
                    int index = keys.IndexOf(key);
                    if (index == -1)
                        throw new KeyNotFoundException();

                    return values[index];
                }
                set
                {
                    int index = keys.IndexOf(key);
                    if (index == -1)
                        throw new KeyNotFoundException();

                    values[index] = value;
                }
            }

            public int Count => keys.Count;

            public List<TKey> Keys => keys;

            public List<TValue> Values => values;

            public KeyValuePair<TKey, TValue> this[int index]
            {
                get
                {
                    if (index < 0 || index >= keys.Count)
                    { throw new Exception("IndexOutOfRangeException"); }
                    return new KeyValuePair<TKey, TValue>(keys[index], values[index]);
                }
            }

            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
            {
                for (int i = 0; i < keys.Count; i++)
                {
                    yield return new KeyValuePair<TKey, TValue>(keys[i], values[i]);
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
