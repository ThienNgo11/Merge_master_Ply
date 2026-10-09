using System.Collections.Generic;
using GameTool.Assistants.DesignPattern;
using GameToolSample.ObjectPool;
using UnityEngine;
using Random = UnityEngine.Random;


namespace GameTool.ObjectPool.Scripts
{
    public class PoolingManager : SingletonMonoBehaviour<PoolingManager>
    {
        public PoolAsset Serializers;

        private readonly Dictionary<ePrefabPool, List<BasePooling>> dictPoolItem =
            new Dictionary<ePrefabPool, List<BasePooling>>();

        private readonly Dictionary<ePrefabPool, List<Queue<BasePooling>>> DisPoolers =
            new Dictionary<ePrefabPool, List<Queue<BasePooling>>>();

        private readonly Dictionary<ePrefabPool, List<BasePooling>> ListObj =
            new Dictionary<ePrefabPool, List<BasePooling>>();

        private readonly Dictionary<ePrefabPool, GameObject> parentPoolers = new Dictionary<ePrefabPool, GameObject>();

        protected override void Awake()
        {
            base.Awake();
            UpdateKey();

            Setup();
        }

        public void DisableAllObject()
        {
            foreach (var item in ListObj)
            {
                foreach (var VARIABLE in item.Value)
                {
                    if (VARIABLE)
                    {
                        VARIABLE.ResetToParrent();
                        VARIABLE.Disable();
                    }
                }
            }
        }

        private void Setup()
        {
            foreach (var item in dictPoolItem)
            {
                var kvp = new List<Queue<BasePooling>>();
                for (int i = 0; i < dictPoolItem[item.Key].Count; i++)
                {
                    kvp.Add(new Queue<BasePooling>());
                }

                DisPoolers.Add(item.Key, kvp);
                ListObj.Add(item.Key, new List<BasePooling>());
                var par = new GameObject(item.Key + "Parent");
                par.transform.position = Vector3.zero;
                par.transform.parent = transform;
                parentPoolers.Add(item.Key, par);
            }

            if (Serializers)
            {
                for (int i = 0; i < Serializers.poolAsset.Count; i++)
                {
                    for (int j = 0; j < Serializers.poolAsset[i].ListPooling.Count; j++)
                    {
                        for (int k = 0; k < Serializers.poolAsset[i].amount; k++)
                        {
                            GetObject(Serializers.poolAsset[i].key, index: j);
                        }
                    }
                }
            }

            DisableAllObject();
        }

        public BasePooling GetObject(ePrefabPool objectName, Transform parent = null, Vector3 position = new Vector3(),
            Vector3 scale = new Vector3(), Quaternion rotation = new Quaternion(), int index = -1)
        {
            UpdatePoolAsset(objectName);
            if (dictPoolItem[objectName].Count <= 0)
            {
#if UNITY_EDITOR
                Debug.LogError("Prefab of " + objectName + " is empty");
#endif
                return null;
            }

            BasePooling item = null;
            int idx;
            if (index == -1)
            {
                idx = Random.Range(0, dictPoolItem[objectName].Count);
            }
            else
            {
                idx = index;
            }

            while (!item)
            {
                if (DisPoolers[objectName][idx].Count == 0)
                {
                    item = Instantiate(dictPoolItem[objectName][idx], parentPoolers[objectName].transform);
                    ListObj[objectName].Add(item);
                    item.poolName = objectName;
                    item.poolNameString = objectName.ToString();
                    item.index = idx;
                    item.SetParrentTrans(parentPoolers[objectName].transform);
                }
                else
                {
                    item = DisPoolers[objectName][idx].Dequeue();
                }
            }

            if (!ListObj[item.poolName].Contains(item))
            {
                ListObj[item.poolName].Add(item);
            }

            item.IsInPool = false;
            item.gameObject.SetActive(true);

            var itemTransform = item.transform;
            if (parent)
            {
                itemTransform.SetParent(parent);
            }
            else
            {
                if (!itemTransform.gameObject.activeInHierarchy)
                {
                    return GetObject(objectName, parent, position, scale, rotation, index);
                }
            }

            itemTransform.position = position;

            if (scale == Vector3.zero)
            {
                item.transform.localScale = Vector3.one;
            }
            else
            {
                item.transform.localScale = scale;
            }

            if (rotation.eulerAngles == Vector3.zero)
            {
                itemTransform.rotation = dictPoolItem[objectName][idx].transform.rotation;
            }
            else
            {
                itemTransform.rotation = rotation;
            }

            item.Init();
            return item;
        }

        public void PushToPooler(BasePooling item)
        {
            if (item)
            {
                item.CheckEnum();
                UpdatePoolAsset(item.poolName);
                DisPoolers[item.poolName][item.index].Enqueue(item);
                item.IsInPool = true;
            }
        }

        public void OutThePooler(BasePooling item)
        {
            if (item)
            {
                item.CheckEnum();
                UpdatePoolAsset(item.poolName);
                for (int i = 0; i < DisPoolers[item.poolName][item.index].Count; i++)
                {
                    var itemTemp = DisPoolers[item.poolName][item.index].Dequeue();
                    if (itemTemp.Equals(item))
                    {
                        item.IsInPool = false;
                        return;
                    }

                    DisPoolers[item.poolName][item.index].Enqueue(itemTemp);
                }
            }
        }

        public void UpdateKey()
        {
            dictPoolItem.Clear();
            if (Serializers)
            {
                foreach (var serializer in Serializers.poolAsset)
                {
                    if (!dictPoolItem.ContainsKey(serializer.key))
                    {
                        dictPoolItem.Add(serializer.key, serializer.ListPooling);
                    }
                }
            }
        }

        public void UpdatePoolAsset(ePrefabPool filename)
        {
            if (!dictPoolItem.ContainsKey(filename))
            {
                ReSetup(filename);
            }
        }

        private void ReSetup(ePrefabPool filename)
        {
            var table = Resources.Load<PoolingTable>("PoolingTable");
            var list = new List<BasePooling>();
            var paths = table.Serializers.Find(item => item.key == filename.ToString()).ItemPooling.resourcePaths;
            for (int i = 0; i < paths.Count; i++)
            {
                list.Add(Resources.Load<BasePooling>(paths[i]));
            }

            dictPoolItem.Add(filename, list);

            var kvp = new List<Queue<BasePooling>>();
            for (int i = 0; i < dictPoolItem[filename].Count; i++)
            {
                kvp.Add(new Queue<BasePooling>());
            }

            DisPoolers.Add(filename, kvp);
            ListObj.Add(filename, new List<BasePooling>());
            var par = new GameObject(filename + "Parent");
            par.transform.position = Vector3.zero;
            par.transform.parent = transform;
            parentPoolers.Add(filename, par);
        }
    }
}