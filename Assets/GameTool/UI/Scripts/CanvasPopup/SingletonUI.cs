using UnityEngine;

namespace GameTool.UI.Scripts.CanvasPopup
{
    public class SingletonUI<T> : BaseUI where T : BaseUI
    {
        private static T singleton;

        public static T Instance
        {
            get
            {
                if (singleton == null)
                {
                    singleton = (T)FindObjectOfType(typeof(T));
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
            return singleton != null;
        }

        protected virtual void Awake()
        {
            if (singleton && singleton != this)
            {
                Destroy(gameObject);
            }
            else
            {
                singleton = (T)(BaseUI)this;
            }
        }
    }
}