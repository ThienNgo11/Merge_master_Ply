using System;
using System.Collections.Generic;
using System.Linq;
using GameToolSample.ObjectPool;
using UnityEditor;
using UnityEngine;

namespace GameTool.ObjectPool.Scripts
{
    [CreateAssetMenu(fileName = "PoolAsset", menuName = "ScriptableObject/PoolAsset", order = 0)]
    public class PoolAsset : ScriptableObject
    {
        public List<PoolAssetItem> poolAsset;

#if UNITY_EDITOR
        [ContextMenu("Re Update")]
        public void OnValidate()
        {
            var table = Resources.Load<PoolingTable>("PoolingTable");
            for (int i = 0; i < poolAsset.Count; i++)
            {
                try
                {
                    poolAsset[i].ListPooling = table.Serializers
                        .Find(music => music.key == poolAsset[i].key.ToString()).ItemPooling.listPooling.ToList();
                }
                catch
                {
                    poolAsset[i].ListPooling.Clear();
                }
            }

            EditorUtility.SetDirty(this);
        }
#endif
    }

    [Serializable]
    public class PoolAssetItem
    {
        public ePrefabPool key = ePrefabPool.None;
        public List<BasePooling> ListPooling = new List<BasePooling>();
        public int amount = 10;
    }
}