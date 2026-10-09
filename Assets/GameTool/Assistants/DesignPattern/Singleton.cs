using UnityEngine;

namespace GameTool.Assistants.DesignPattern {
    /// <summary>
    /// Singleton base class
    /// </summary>
    public class Singleton<T> where T : class, new()
    {
        private static readonly T singleton = new T();

        public static T Instance
        {
            get
            {
                return singleton;
            }
        }
    }

    /// <summary>
    /// Singleton for mono behavior object
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T singleton;

        public static T Instance
        {
            get
            {
                if (!singleton)
                {
                    singleton = FindObjectOfType<T>();
                    if (singleton == null)
                    {
                        Debug.LogError(typeof(T).Name + "is Null");
                    }
                }

                return singleton;
            }

            protected set => singleton = value;
        }
        public static bool IsInstanceValid()
        {
            return singleton;
        }

        protected virtual void Awake()
        {
            if (singleton && singleton != this)
            {
                Destroy(gameObject);
            }
            else
            {
                singleton = (T)(MonoBehaviour)this;
            }
        }
    }
}