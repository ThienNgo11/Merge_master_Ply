using UnityEngine;

namespace LunaMouseEvent
{
    public class MouseRaycastInteraction : MonoBehaviour
    {
        public bool ENABLE_DEBUGGING;
        
        [Space]
        public Camera mainCamera;
        public bool isDragging = false;
        public GameObject selectedObject;

        void Start()
        {
            mainCamera = Camera.main;
        }

        void Update()
        {
            HandleMouseInput();
        }

        void HandleMouseInput()
        {
            // Mouse Down
            if (Input.GetMouseButtonDown(0))
            {
                if (Raycast(out RaycastHit2D hit2D, out RaycastHit hit3D))
                {
                    selectedObject = hit2D.collider != null ? hit2D.collider.gameObject : hit3D.collider.gameObject;
                    isDragging = true;
                    MouseClick(selectedObject);
                }
            }

            // Mouse Drag
            if (isDragging && Input.GetMouseButton(0))
            {
                if (Raycast(out Vector3 dragPosition))
                {
                    MouseDrag(dragPosition);
                }
            }

            // Mouse Up
            if (isDragging && Input.GetMouseButtonUp(0))
            {
                isDragging = false;
                MouseUp(selectedObject);
                selectedObject = null;
            }
        }

        bool Raycast(out RaycastHit2D hit2D, out RaycastHit hit3D)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            hit2D = Physics2D.GetRayIntersection(ray);
            Physics.Raycast(ray, out hit3D);

            return hit2D.collider != null || hit3D.collider != null;
        }

        bool Raycast(out Vector3 dragPosition)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit3D))
            {
                dragPosition = hit3D.point;
                return true;
            }

            if (Physics2D.GetRayIntersection(ray))
            {
                var hit2D = Physics2D.GetRayIntersection(ray);
                dragPosition = hit2D.point;
                return true;
            }

            dragPosition = Vector3.zero;
            return false;
        }

        void MouseClick(GameObject clickedObject)
        {
            Log("Mouse Clicked on: " + clickedObject.name);

            var comp = clickedObject.GetComponent<MouseEventTrigger>();
            if (comp)
            {
                comp.onMouseDown?.Invoke();
            }
        }

        void MouseDrag(Vector3 dragPosition)
        {
            Log("Mouse Dragging to: " + dragPosition);

            if (selectedObject)
            {
                var comp = selectedObject.GetComponent<MouseEventTrigger>();
                if (comp)
                {
                    comp.onMouseDown?.Invoke();
                }
            }
        }

        void MouseUp(GameObject releasedObject)
        {
            Log("Mouse Released from: " + releasedObject.name);

            var comp = releasedObject.GetComponent<MouseEventTrigger>();
            if (comp)
            {
                comp.onMouseDown?.Invoke();
            }
        }

        public void Log(string mess)
        {
            if (ENABLE_DEBUGGING)
            {
                Debug.Log(mess);
            }
        }
    }
}
