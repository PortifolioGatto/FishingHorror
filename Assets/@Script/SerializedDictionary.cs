using System.Collections.Generic;

[System.Serializable]
public class SerializedDictionary<TKey, TValue>
{
    [System.Serializable]
    public struct KeyValuePair
    {
        public TKey Key;
        public TValue Value;
        public KeyValuePair(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
    }

    public List<KeyValuePair> keyValuePairs = new List<KeyValuePair>();

    public int Count => keyValuePairs.Count;

    public TValue this[TKey key]
    {
        get
        {
            int index = IndexOfKey(key);
            if (index >= 0)
            {
                return keyValuePairs[index].Value;
            }
            throw new KeyNotFoundException($"Key '{key}' not found in the dictionary.");
        }
        set
        {
            int index = IndexOfKey(key);
            if (index >= 0)
            {
                keyValuePairs[index] = new KeyValuePair(key, value);
            }
            else
            {
                Add(key, value);
            }
        }
    }

    public SerializedDictionary()
    {
        keyValuePairs = new List<KeyValuePair>();
    }

    public void Add(TKey key, TValue value)
    {
        keyValuePairs.Add(new KeyValuePair(key, value));
    }

    public void Remove(TKey key)
    {
        int index = IndexOfKey(key);
        if (index >= 0)
        {
            keyValuePairs.RemoveAt(index);
        }
    }

    public bool ContainsKey(TKey key)
    {
        return IndexOfKey(key) >= 0;
    }

    public int IndexOfKey(TKey key)
    {
        for (int i = 0; i < keyValuePairs.Count; i++)
        {
            if (EqualityComparer<TKey>.Default.Equals(keyValuePairs[i].Key, key))
            {
                return i;
            }
        }
        return -1;
    }

    public void Clear()
    {
        keyValuePairs.Clear();
    }

}
