using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameTool.Assistants.Helper.SerializableInterface.Main
{
    [Serializable]
    public class InterF<T> where T : class
    {
        [SerializeField]
        private Object component;
        public T Interface
        {
            get
            {
                if (component is T)
                {
                    return component as T;
                }
                else
                {
                    Debug.LogError("Component is not " + typeof(T).Name);
                    return null;
                }
            }
        }

        public Object Component
        {
            get => component;
            set
            {
                if (!value)
                {
                    component = value;
                    return;
                }
            
                if (component is T)
                {
                    component = value;
                }
                else
                {
                    Debug.LogError("Component is not " + typeof(T).Name);
                }
            }
        }
    }
}