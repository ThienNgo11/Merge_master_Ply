using System.Collections.Generic;
using System.Linq;
using DatdevUlts.Ults;
using UnityEditor;
using UnityEngine;

namespace DatdevUlts.Editor
{
    [CustomPropertyDrawer(typeof(SortingLayerAtt))]
    public class SortingLayerAttDrawer : PropertyDrawer
    {
        private int idHash;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            List<string> sortingLayerNames = SortingLayer.layers.Select(layer => layer.name).ToList();
            EditorUtils.DrawDropDownButton(property, position, ref idHash, label, typeof(SortingLayerAttDrawer), sortingLayerNames);
        }
    }
}