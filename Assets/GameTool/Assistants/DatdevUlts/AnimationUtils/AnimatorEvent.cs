using System;
using UnityEngine;
using UnityEngine.Events;

namespace DatdevUlts.AnimationUtils
{
    public class AnimatorEvent : MonoBehaviour
    {
        /// <summary>
        /// arg1: Tên sự kiện
        /// </summary>
        public event UnityAction<string> TrackEvent;
        
        public void InvokePostEvent(string eventName)
        {
            TrackEvent?.Invoke(eventName);
        }

        public void InvokeRegistEvent(UnityAction<string> callBack)
        {
            TrackEvent += callBack;
        }

        public void InvokeRemoveEvent(UnityAction<string> callBack)
        {
            TrackEvent -= callBack;
        }

        public void ClearEvent()
        {
            TrackEvent = null;
        }
    }
}