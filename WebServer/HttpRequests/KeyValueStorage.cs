using System;
using System.Collections.Generic;

namespace SilentOrbit.HttpRequests
{
    /// <summary>
    /// Exception free dictionary which return null on missing items
    /// </summary>
    public class KeyValueStorage
    {
        readonly Dictionary<string, string> storage = new Dictionary<string, string>();

        public string this[string key]
        {
            get
            {
                string val = null;
                if (storage.TryGetValue(key, out val))
                    return val;
                else
                    return null;
            }
            set
            {
                if (storage.ContainsKey(key))
                    storage.Remove(key);
                storage.Add(key, value);
            }
        }

        public override string ToString()
        {
            string s = "";
            foreach (var kvp in storage)
            {
                s += kvp.Key + "=" + kvp.Value + ", ";
            }
            return s;
        }

    }
}

