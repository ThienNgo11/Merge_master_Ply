using System.Collections.Generic;
using UnityEngine;

namespace GameTool.ObjectPool.Scripts
{
    public static class PoolingManagerExtensions
    {
        private static readonly Dictionary<int, Queue<GameObject>> customPools = new Dictionary<int, Queue<GameObject>>();
        private static readonly Dictionary<int, Transform> customPoolParents = new Dictionary<int, Transform>();
        private static readonly Dictionary<int, List<GameObject>> customAllObjects = new Dictionary<int, List<GameObject>>();

        public static GameObject Spawn(this PoolingManager manager, GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            int key = prefab.GetInstanceID();
            return SpawnInternal(manager, key, prefab.name, prefab, position, rotation, parent);
        }

        public static GameObject Spawn(this PoolingManager manager, GameObject prefab, Vector3 position = default(Vector3))
        {
            return manager.Spawn(prefab, position, prefab.transform.rotation, null);
        }

        public static GameObject Spawn(this PoolingManager manager, string customKey, GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (string.IsNullOrEmpty(customKey))
            {
                return manager.Spawn(prefab, position, rotation, parent);
            }

            int key = customKey.GetHashCode();
            return SpawnInternal(manager, key, customKey, prefab, position, rotation, parent);
        }

        public static T Spawn<T>(this PoolingManager manager, T prefab, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
        {
            GameObject obj = manager.Spawn(prefab.gameObject, position, rotation, parent);
            return obj.GetComponent<T>();
        }

        public static T Spawn<T>(this PoolingManager manager, T prefab, Vector3 position = default(Vector3)) where T : Component
        {
            return manager.Spawn<T>(prefab, position, prefab.transform.rotation, null);
        }

        public static ParticleSystem SpawnParticle(this PoolingManager manager, ParticleSystem prefab, Vector3 position, Quaternion rotation = default(Quaternion), Transform parent = null, float scaleMultiplier = 1f)
        {
            Quaternion rot = (rotation == default(Quaternion)) ? prefab.transform.rotation : rotation;
            ParticleSystem ps = manager.Spawn<ParticleSystem>(prefab, position, rot, parent);

            if (Mathf.Abs(scaleMultiplier - 1f) > 0.001f)
            {
                ps.transform.localScale = prefab.transform.localScale * scaleMultiplier;
            }

            ps.Clear();
            ps.Play();

            float duration = ps.main.duration + ps.main.startLifetime.constantMax;
            manager.Despawn(ps.gameObject, duration);
            return ps;
        }

        public static GameObject SpawnParticle(this PoolingManager manager, GameObject particlePrefab, Vector3 position, Quaternion rotation = default(Quaternion), Transform parent = null, float scaleMultiplier = 1f)
        {
            Quaternion rot = (rotation == default(Quaternion)) ? particlePrefab.transform.rotation : rotation;
            GameObject obj = manager.Spawn(particlePrefab, position, rot, parent);

            if (Mathf.Abs(scaleMultiplier - 1f) > 0.001f)
            {
                obj.transform.localScale = particlePrefab.transform.localScale * scaleMultiplier;
            }

            ParticleSystem ps = obj.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Clear();
                ps.Play();
                float duration = ps.main.duration + ps.main.startLifetime.constantMax;
                manager.Despawn(obj, duration);
            }
            else
            {
                manager.Despawn(obj, 2f);
            }

            return obj;
        }

        public static void Despawn(this PoolingManager manager, GameObject instance, float delay = 0f)
        {
            if (instance == null)
            {
                return;
            }

            PooledItem item = instance.GetComponent<PooledItem>();
            if (item == null)
            {
                instance.SetActive(false);
                return;
            }

            if (delay > 0f)
            {
                item.Despawn(delay);
                return;
            }

            if (item.IsInPool)
            {
                return;
            }

            item.CancelDelay();
            item.IsInPool = true;

            if (customPools.TryGetValue(item.PrefabKey, out Queue<GameObject> queue))
            {
                if (item.ParentPool != null)
                {
                    instance.transform.SetParent(item.ParentPool, false);
                }

                instance.SetActive(false);
                queue.Enqueue(instance);
            }
            else
            {
                instance.SetActive(false);
            }
        }

        public static void Despawn(this PoolingManager manager, Component comp, float delay = 0f)
        {
            if (comp != null)
            {
                manager.Despawn(comp.gameObject, delay);
            }
        }

        private static GameObject SpawnInternal(PoolingManager manager, int key, string poolName, GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            if (!customPools.TryGetValue(key, out Queue<GameObject> queue))
            {
                queue = new Queue<GameObject>();
                customPools.Add(key, queue);
                customAllObjects.Add(key, new List<GameObject>());

                GameObject poolRoot = new GameObject($"Pool_{poolName}");
                poolRoot.transform.SetParent(manager.transform, false);
                customPoolParents.Add(key, poolRoot.transform);
            }

            Transform parentPool = customPoolParents[key];
            GameObject instance = null;

            while (queue.Count > 0)
            {
                GameObject candidate = queue.Dequeue();
                if (candidate != null)
                {
                    instance = candidate;
                    break;
                }
            }

            if (instance == null)
            {
                instance = Object.Instantiate(prefab, parentPool);
                customAllObjects[key].Add(instance);
            }

            PooledItem item = instance.GetComponent<PooledItem>();
            if (item == null)
            {
                item = instance.AddComponent<PooledItem>();
            }

            item.PrefabKey = key;
            item.ParentPool = parentPool;
            item.IsInPool = false;
            item.CancelDelay();

            Transform t = instance.transform;
            if (parent != null)
            {
                t.SetParent(parent, false);
            }

            t.position = position;
            t.rotation = rotation;
            t.localScale = prefab.transform.localScale;
            instance.SetActive(true);

            return instance;
        }
    }
}
