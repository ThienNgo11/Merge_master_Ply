using System.Collections.Generic;
using DatdevUlts.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace DatdevUlts.AnimationUtils
{
    [CustomPropertyDrawer(typeof(AnimName))]
    public class AnimNameEditor : PropertyDrawer
    {
        private int idhash;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var animationController = (AnimatorController)property.serializedObject.targetObject;
            var listAnim = GetStateNames(0, animationController.Animator);
            EditorUtils.DrawDropDownButton(property, position, ref idhash, label, typeof(PropertyAttribute), listAnim);
        }
        
        public List<string> GetStateNames(int layerIndex, Animator animator)
        {
            List<string> stateNames = new List<string>();

            // Get the animator controller
            UnityEditor.Animations.AnimatorController animatorController =
                (UnityEditor.Animations.AnimatorController)animator.runtimeAnimatorController;

            if (animatorController != null && animatorController.layers.Length > layerIndex)
            {
                AnimatorControllerLayer layer = animatorController.layers[layerIndex];
                foreach (ChildAnimatorState state in layer.stateMachine.states)
                {
                    stateNames.Add(state.state.name);
                }
            }

            return stateNames;
        }
    }
}