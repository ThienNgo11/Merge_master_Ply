using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using GameTool.Assistants.DesignPattern;
using UnityEngine;

namespace DatdevUlts
{
    public class CoroutineRunner : SingletonMonoBehaviour<CoroutineRunner>
    {
        private Dictionary<Tween, GameObject> _tweenDict = new Dictionary<Tween, GameObject>();

        public void AddDoDelay(GameObject o, Tween tween)
        {
            if (tween == null)
            {
                return;
            }

            if (o == null)
            {
                return;
            }

            _tweenDict.Add(tween, o);
        }

        public void RemoveDoDelay(Tween tween)
        {
            if (tween == null)
            {
                return;
            }

            _tweenDict.Remove(tween);
        }

        public void RemoveDoDelay(GameObject o)
        {
            var toRemove = _tweenDict
                .Where(pair => pair.Value == o)
                .Select(pair => pair.Key)
                .ToList();  // Tạo bản sao danh sách key cần xóa

            foreach (var key in toRemove)
            {
                key.Kill();
                RemoveDoDelay(key); // Bây giờ bạn có thể xóa an toàn
            }
        }

#if UNITY_EDITOR
        private void Update()
        {
            foreach (var pair in _tweenDict.Where(pair => !pair.Value ||!pair.Value.activeInHierarchy))
            {
                Debug.LogError(pair.Value.name + " is not active, please call RemoveDoDelay in OnDisable", pair.Value);
            }
        }
#endif
    }
}