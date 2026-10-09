using System;
using System.Collections.Generic;
using System.Linq;
using DatdevUlts.InspectorUtils;
using UnityEngine;
using GameToolSample.ObjectPool;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace GameTool.ObjectPool.Scripts
{
    [CreateAssetMenu(fileName = "PoolingTable", menuName = "ScriptableObject/PoolingTable", order = 0)]
    public class PoolingTable : ScriptableObject
    {
        public List<PoolSerializer> Serializers = new List<PoolSerializer>();

#if UNITY_EDITOR
        public void OnValidate()
        {
            for (int i = 0; i < Serializers.Count; i++)
            {
                Serializers[i].ItemPooling.resourcePaths.Clear();
                for (int j = 0; j < Serializers[i].ItemPooling.listPooling.Count; j++)
                {
                    PoolSerializer poolSerializer = Serializers[i];
                    poolSerializer.ItemPooling.resourcePaths.Add(GetResPath(poolSerializer.ItemPooling.listPooling[j]));

                    int count = Serializers.Count(serializer => serializer.key == poolSerializer.key);
                    if (count >= 2)
                    {
                        Debug.LogError(poolSerializer.key + " " + count);
                        poolSerializer.keyDuplicated = true;
                    }
                    else
                    {
                        poolSerializer.keyDuplicated = false;
                    }
                }
            }
            EditorUtility.SetDirty(this);
        }

        public string GetResPath(BasePooling basePooling)
        {
            var str = AssetDatabase.GetAssetPath(basePooling);
            var index = str.LastIndexOf("Resources", StringComparison.Ordinal);
            if (index >= 0)
            {
                str = str.Substring(index);
                str = str.Remove(0, "Resources/".Length);
                
                index = str.LastIndexOf(".", StringComparison.Ordinal);
                str = str.Remove(index);

                return str;
            }

            return "";
        }

        public void UpdatePrefab()
        {
            foreach (var item in Serializers)
            {
                if (item.key != "None")
                {
                    for (int i = 0; i < item.ItemPooling.listPooling.Count; i++)
                    {
                        BasePooling obj = item.ItemPooling.listPooling[i];
                        if (obj)
                        {
                            if (Enum.TryParse<ePrefabPool>(item.key, out var ename))
                            {
                                obj.poolName = ename;
                                obj.poolNameString = ename.ToString();
                            }

                            obj.index = i;

                            if (PrefabUtility.IsPartOfRegularPrefab(obj))
                            {
                                EditorUtility.SetDirty(obj);
                                PrefabUtility.RecordPrefabInstancePropertyModifications(obj.gameObject);
                            }
                        }
                    }
                }
            }
        }
#endif
    }

    [Serializable]
    public class ItemPooling
    {
#if UNITY_EDITOR
        public List<BasePooling> listPooling;
#endif
        public List<string> resourcePaths;
    }

    [Serializable]
    public class PoolSerializer
    {
        public string key;
        public ItemPooling ItemPooling;
#if UNITY_EDITOR
        [HideInNormalInspector] public bool keyDuplicated;
#endif
    }
}