using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GameTool.Assistants.DictionarySerialize
{
    [Serializable]
    public class _SerialKVPair<TKeyx, TValuex>
    {
        public TKeyx key;
        public TValuex value;

        [HideInInspector] public bool isDuplicate;

        public _SerialKVPair<TKeyx, TValuex> Reset()
        {
            key = default;
            return this;
        }
    }
    
    [Serializable]
    public class Dict<TKey, TValue>
    {
        public bool isDuplicate;

        [SerializeField] private List<_SerialKVPair<TKey, TValue>> list = new List<_SerialKVPair<TKey, TValue>>();

        public void Import(List<TKey> listKey, List<TValue> listValue)
        {
            list.Clear();
            for (var index = 0; index < listKey.Count; index++)
            {
                var key = listKey[index];
                var value = listValue[index];
                list.Add(new _SerialKVPair<TKey, TValue>()
                {
                    key = key,
                    value = value
                });
            }
        }

        public void Export(out List<TKey> listKey, out List<TValue> listValue)
        {
            listKey = new List<TKey>(list.Count);
            listValue = new List<TValue>(list.Count);
            foreach (var item in list)
            {
                listKey.Add(item.key);
                listValue.Add(item.value);
            }
        }

        #region HELPER METHODS
        
        // Hàm hỗ trợ tìm index của Key trong list
        private int IndexOfKey(TKey key)
        {
            var comparer = EqualityComparer<TKey>.Default;
            for (int i = 0; i < list.Count; i++)
            {
                if (comparer.Equals(list[i].key, key)) return i;
            }
            return -1;
        }

        #endregion

        #region DICTIONARY METHODS

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (var item in list)
            {
                yield return new KeyValuePair<TKey, TValue>(item.key, item.value);
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Add(TKey key, TValue value)
        {
            if (ContainsKey(key))
            {
                Debug.LogError("The dictionary already contains the key " + key);
            }
            else
            {
                list.Add(new _SerialKVPair<TKey, TValue> { key = key, value = value });
            }
        }

        public void Clear()
        {
            list.Clear();
        }

        public bool Contains(TValue item)
        {
            var comparer = EqualityComparer<TValue>.Default;
            return list.Any(x => comparer.Equals(x.value, item));
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            int index = IndexOfKey(item.Key);
            if (index >= 0)
            {
                var comparer = EqualityComparer<TValue>.Default;
                return comparer.Equals(list[index].value, item.Value);
            }
            return false;
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            for (int i = 0; i < list.Count; i++)
            {
                array[arrayIndex + i] = new KeyValuePair<TKey, TValue>(list[i].key, list[i].value);
            }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (Contains(item))
            {
                return Remove(item.Key);
            }
            return false;
        }

        public int Count => list.Count;
        public bool IsReadOnly => false;

        public bool ContainsKey(TKey key)
        {
            return IndexOfKey(key) >= 0;
        }

        public bool Remove(TKey key)
        {
            int index = IndexOfKey(key);
            if (index >= 0)
            {
                list.RemoveAt(index);
                return true;
            }
            return false;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            int index = IndexOfKey(key);
            if (index >= 0)
            {
                value = list[index].value;
                return true;
            }
            value = default;
            return false;
        }

        public TValue this[TKey key]
        {
            get 
            {
                int index = IndexOfKey(key);
                if (index >= 0) return list[index].value;
                throw new KeyNotFoundException("Key not found: " + key);
            }
            set 
            {
                int index = IndexOfKey(key);
                if (index >= 0) list[index].value = value;
                else list.Add(new _SerialKVPair<TKey, TValue> { key = key, value = value });
            }
        }

        public TValue GetElementAtKey(TKey key)
        {
            return this[key]; // Sử dụng lại Indexer cho gọn
        }

        public TValue GetElementAtKey(TKey key, TValue defaultValue)
        {
            int index = IndexOfKey(key);
            if (index >= 0)
            {
                return list[index].value;
            }
            
            Add(key, defaultValue);
            return defaultValue;
        }

        public TValue GetElementAtKey(TKey key, Func<TValue> onNotContainsKey)
        {
            int index = IndexOfKey(key);
            if (index >= 0)
            {
                return list[index].value;
            }
            
            TValue value = onNotContainsKey != null ? onNotContainsKey() : default;
            Add(key, value);
            return value;
        }

        public void SetElementAtKey(TKey key, TValue value)
        {
            this[key] = value;
        }

        public ICollection<TKey> Keys => list.Select(x => x.key).ToList();
        public ICollection<TValue> Values => list.Select(x => x.value).ToList();

        public KeyValuePair<TKey, TValue> GetRandomElement()
        {
            if (list.Count == 0) throw new InvalidOperationException("Dictionary is empty.");
            var randomItem = list[Random.Range(0, list.Count)];
            return new KeyValuePair<TKey, TValue>(randomItem.key, randomItem.value);
        }

#if UNITY_EDITOR
        /// <summary>
        /// CHỈ DÙNG TRONG EDITOR NHẰM SERIALIZE RA BÊN NGOÀI CHO DỄ NHÌN
        /// </summary>
        public bool RenameKey(TKey oldKey, TKey newKey, Predicate<TKey> match)
        {
            if (ContainsKey(newKey))
            {
                return false;
            }

            var item = list.Find(pair => match.Invoke(pair.key));
            if (item != null)
            {
                item.key = newKey;
                return true;
            }

            return false;
        }

        public int CountOfList => list.Count;

        public List<_SerialKVPair<TKey, TValue>> List => list;

        public TValue ElementAtList(int index)
        {
            return list[index].value;
        }
#endif

        #endregion DICTIONARY METHODS

        public KeyValuePair<TKey, TValue> ElementAt(int index)
        {
            // Bước 1 & 2: Kiểm tra index có nằm trong giới hạn an toàn của list hay không
            if (index < 0 || index >= list.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index nằm ngoài phạm vi của Dictionary (List).");
            }

            // Bước 3: Lấy phần tử từ list và đóng gói thành KeyValuePair
            var item = list[index];
            return new KeyValuePair<TKey, TValue>(item.key, item.value);
        }
    }
}