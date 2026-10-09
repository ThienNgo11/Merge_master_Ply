using System;
using System.Collections.Generic;
using UnityEngine;
using VInspector;

namespace GameTool.Assistants.DatdevUlts.Aligner
{
    public class Aligner : MonoBehaviour
    {
        public enum AlignmentPlane
        {
            XY_2D, // Sắp xếp trên mặt đứng (xOy)
            XZ_3D  // Sắp xếp trên mặt sàn (xOz)
        }

        public enum Anchor
        {
            TopLeft,
            TopCenter,
            TopRight,
            MiddleLeft,
            MiddleCenter,
            MiddleRight,
            BottomLeft,
            BottomCenter,
            BottomRight
        }

        [Header("Target Objects")] 
        [SerializeField]
        private List<Transform> targets;

        [Header("Alignment Settings")] 
        [SerializeField]
        private AlignmentPlane plane = AlignmentPlane.XY_2D; // Tùy chọn mặt phẳng

        [Min(1)] [SerializeField]
        private int columns = 3;

        [SerializeField] private float spaceX = 2.0f;
        [SerializeField] private float spaceY = 2.0f; // Trong chế độ XZ, biến này đóng vai trò là spaceZ

        [SerializeField] private Vector3 offsetWithCenter = Vector3.zero;

        [Header("Options")] 
        [SerializeField] private Anchor anchor = Anchor.MiddleCenter;

        public List<Transform> Targets => targets;

        public AlignmentPlane Plane => plane;

        public int Columns => columns;

        public float SpaceX => spaceX;

        public float SpaceY => spaceY;

        public Vector3 OffsetWithCenter => offsetWithCenter;

        public Anchor Anchor_ => anchor;

        [Button]
        public void Align()
        {
            Align((index, pos) =>
            {
                if (index < targets.Count && targets[index] != null)
                {
                    Transform target = targets[index];
#if UNITY_EDITOR
                    UnityEditor.Undo.RecordObject(target, "Align Objects");
#endif
                    target.localPosition = pos;
                }
            }, targets.Count);
        }

        public virtual void Align(Action<int, Vector3> onAligned, int countTf)
        {
            if (countTf == 0) return;

            int count = countTf;
            int rows = Mathf.CeilToInt((float)count / columns);

            // 1. Tính kích thước tổng của lưới
            // gridSecondarySize: Là chiều cao (Height) nếu XY, hoặc chiều sâu (Depth) nếu XZ
            float gridWidth = (Mathf.Min(count, columns) - 1) * spaceX;
            float gridSecondarySize = (rows - 1) * spaceY;

            // 2. Tính toán hệ số Anchor
            float xFactor = 0f; // 0: Left, 0.5: Center, 1: Right
            float yFactor = 0f; // 0: Top, 0.5: Middle, 1: Bottom

            switch (anchor)
            {
                case Anchor.TopLeft:      xFactor = 0.0f; yFactor = 0.0f; break;
                case Anchor.TopCenter:    xFactor = 0.5f; yFactor = 0.0f; break;
                case Anchor.TopRight:     xFactor = 1.0f; yFactor = 0.0f; break;
                case Anchor.MiddleLeft:   xFactor = 0.0f; yFactor = 0.5f; break;
                case Anchor.MiddleCenter: xFactor = 0.5f; yFactor = 0.5f; break;
                case Anchor.MiddleRight:  xFactor = 1.0f; yFactor = 0.5f; break;
                case Anchor.BottomLeft:   xFactor = 0.0f; yFactor = 1.0f; break;
                case Anchor.BottomCenter: xFactor = 0.5f; yFactor = 1.0f; break;
                case Anchor.BottomRight:  xFactor = 1.0f; yFactor = 1.0f; break;
            }

            // 3. Tính điểm bắt đầu (Start Position) dựa trên Anchor
            // startPrimary: Vị trí bắt đầu trên trục X
            // startSecondary: Vị trí bắt đầu trên trục Y (nếu XY) hoặc Z (nếu XZ)
            
            float startPrimary = -(gridWidth * xFactor);
            
            // Logic trục dọc: 
            // - Top (yFactor=0) => bắt đầu tại 0, các hàng sau trừ dần đi (xuống dưới/ra sau).
            // - Bottom (yFactor=1) => bắt đầu tại gridSecondarySize, các hàng sau trừ dần về 0.
            float startSecondary = (gridSecondarySize * yFactor);

            // 4. Duyệt và tính vị trí
            for (int i = 0; i < count; i++)
            {
                int col = i % columns;
                int row = i / columns;

                // Tính toán vị trí thô (chưa cộng offset)
                float xPos = startPrimary + (col * spaceX);
                float secondaryPos = startSecondary - (row * spaceY); // Đi từ Top (dương) xuống Bottom (âm/nhỏ hơn)

                Vector3 calculatedPos;

                if (plane == AlignmentPlane.XY_2D)
                {
                    // Hệ xOy: secondaryPos áp dụng cho Y
                    calculatedPos = new Vector3(
                        xPos + offsetWithCenter.x, 
                        secondaryPos + offsetWithCenter.y, 
                        offsetWithCenter.z
                    );
                }
                else // AlignmentPlane.XZ_3D
                {
                    // Hệ xOz: secondaryPos áp dụng cho Z. Y giữ nguyên theo offset.
                    calculatedPos = new Vector3(
                        xPos + offsetWithCenter.x, 
                        offsetWithCenter.y, 
                        secondaryPos + offsetWithCenter.z
                    );
                }

                onAligned?.Invoke(i, calculatedPos);
            }
        }

        private void OnValidate()
        {
            // Tự động sắp xếp trong Editor khi thay đổi giá trị (nếu không đang Play mode)
            if (!Application.isPlaying && targets != null && targets.Count > 0)
            {
                Align((index, pos) =>
                {
                    if (index < targets.Count && targets[index] != null)
                    {
                        targets[index].localPosition = pos;
                    }
                }, targets.Count);
            }
        }
    }
}