using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DatdevUlts.Editor
{
    public static class MenuItemUtils
    {
        [MenuItem("CONTEXT/Image/Set name Object by Sprite", false, 311220)]
        public static void SetNameObjectByImage(MenuCommand menuCommand)
        {
            var img = (Image)menuCommand.context;
            SetNameObjectBySprite(img.gameObject, img.sprite);
            EditorUtility.SetDirty(menuCommand.context);
        }
        
        [MenuItem("CONTEXT/SpriteRenderer/Set name Object by Sprite", false, 311220)]
        public static void SetNameObjectBySprRenderer(MenuCommand menuCommand)
        {
            var img = (SpriteRenderer)menuCommand.context;
            SetNameObjectBySprite(img.gameObject, img.sprite);
            EditorUtility.SetDirty(menuCommand.context);
        }
        
        [MenuItem("CONTEXT/Image/Sprite Renderer to UI", false, 311219)]
        public static void SpriteRendererToUI(MenuCommand menuCommand)
        {
            var img = ((Image)menuCommand.context).GetComponent<SpriteRenderer>();
            var goj = img.gameObject;
            var spr = img.sprite;
            if (img == null)
            {
                Debug.LogWarning("No SpriteRenderer found on this GameObject.");
                return;
            }

            // Tạo một hành động Undo và ghi lại tất cả các thay đổi trong cùng một lần gọi Undo.RecordObject()
            Undo.RecordObject(goj, "Replace SpriteRenderer with Image UI");

            // Xóa SpriteRenderer
            Object.DestroyImmediate(img, true);

            // Thêm Image UI
            Image image = (Image)menuCommand.context;
            image.sprite = spr; // Có thể thiết lập sprite cho Image nếu bạn muốn
            image.transform.localScale = Vector3.one;
            image.transform.localPosition = new Vector3(image.transform.localPosition.x, image.transform.localPosition.y, 0);

            if (spr.border.magnitude > 0)
            {
                image.type = Image.Type.Sliced;
            }
    
            // Thêm RectTransform và thiết lập size delta
            RectTransform rectTransform = goj.GetComponent<RectTransform>();
            if (rectTransform == null)
                rectTransform = goj.AddComponent<RectTransform>();

            if (spr != null)
                rectTransform.sizeDelta = new Vector2(spr.rect.width, spr.rect.height);

            EditorUtility.SetDirty(goj);
        }

        public static void SetNameObjectBySprite(GameObject goj, Sprite sprite)
        {
            Undo.RecordObject(goj, "SetNameObjectBySprite");
            goj.name = sprite.name;
        }
    }
}