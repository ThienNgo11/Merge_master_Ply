using GameToolSample.UIManager;
using UnityEngine;

namespace GameTool.UI.Scripts.CanvasPopup
{
    public class BaseUI : MonoBehaviour
    {
        [HideInInspector] public eUIType uiType;
        [HideInInspector] public eUIName uiName;

        public virtual void Init(params object[] args)
        {
        }

        public virtual void Pop()
        {
            CanvasManager.Instance.Pop(this);
        }
    }
}

