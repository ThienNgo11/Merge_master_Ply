using UnityEngine;
using UnityEngine.Events;

namespace LunaMouseEvent
{
    public class MouseEventTrigger : MonoBehaviour
    {
        public UnityEvent onMouseDown;
        public UnityEvent onDrag;
        public UnityEvent onMouseUp;
    }
}
