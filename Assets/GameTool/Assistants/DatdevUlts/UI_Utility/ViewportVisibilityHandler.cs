using UnityEngine;
using UnityEngine.UI;

namespace DatdevUlts.UI_Utility
{
    public class ViewportVisibilityHandler : MonoBehaviour
    {
        static Vector3[] worldCorners = new Vector3[4];
        
        public ScrollRect scrollRect;
        private RectTransform viewPort;
        private RectTransform content;

        void Start()
        {
            if (scrollRect == null)
            {
                Debug.LogError("ScrollRect is not assigned!");
                return;
            }

            viewPort = scrollRect.viewport;
            content = scrollRect.content;
        }

        void Update()
        {
            foreach (RectTransform child in content)
            {
                bool isVisible = IsVisibleInViewport(child);

                SetChildrenActive(child.gameObject, isVisible);
            }
        }

        private bool IsVisibleInViewport(RectTransform rectTransform)
        {
            rectTransform.GetWorldCorners(worldCorners);

            Rect rect = new Rect(viewPort.position.x, viewPort.position.y, viewPort.rect.width, viewPort.rect.height);

            foreach (Vector3 corner in worldCorners)
            {
                if (rect.Contains(corner))
                {
                    return true;  // At least one corner is in the viewport
                }
            }
            return false;  // None of the corners are in the viewport
        }

        private void SetChildrenActive(GameObject parent, bool isActive)
        {
            foreach (Transform child in parent.transform)
            {
                child.gameObject.SetActive(isActive);
            }
        }
    }
}