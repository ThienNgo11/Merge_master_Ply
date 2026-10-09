using System;
using System.Collections;
using DatdevUlts.InspectorUtils;
using GameToolSample.ObjectPool;
using UnityEngine;

namespace GameTool.ObjectPool.Scripts
{
    public class BasePooling : MonoBehaviour
    {
        [NonSerialized] public ePrefabPool poolName = ePrefabPool.None;
        private bool _isInPool;

        public bool IsInPool
        {
            get => _isInPool;
            set => _isInPool = value;
        }
        [HideInNormalInspector]
        public string poolNameString;
        [HideInNormalInspector]
        public int index;
        Transform parrentTrans;

        /// <summary>
        /// Call sau khi được Pooling Manager set vị trí và được bật thành công (activeInHierarchy)
        /// </summary>
        public virtual void Init(params object[] args)
        {
        }
        
        public void SetParrentTrans(Transform trans)
        {
            parrentTrans = trans;
        }

        public void ResetToParrent()
        {
            transform.SetParent(parrentTrans, false);
        }

        public virtual void Disable()
        {
            if (this)
            {
                gameObject.SetActive(false);
            }
        }

        public virtual void Disable(float time)
        {
            StartCoroutine(nameof(WaitDisable), time);
        }

        private IEnumerator WaitDisable(float time)
        {
            yield return new WaitForSeconds(time);
            Disable();
        }

        /// <summary>
        /// Call trước khi được Pooling Manager set vị trí
        /// </summary>
        protected virtual void OnEnable()
        {
            if (PoolingManager.IsInstanceValid() && this)
            {
                if (IsInPool)
                {
                    PoolingManager.Instance.OutThePooler(this);
                }
            }
        }

        protected virtual void OnDisable()
        {
            if (PoolingManager.IsInstanceValid() && this)
            {
                PoolingManager.Instance.PushToPooler(this);
            }
        }

        public void CheckEnum()
        {
            if (poolName.ToString() != poolNameString)
            {
                poolName = (ePrefabPool)Enum.Parse(typeof(ePrefabPool), poolNameString);
            }
        }
    }
}