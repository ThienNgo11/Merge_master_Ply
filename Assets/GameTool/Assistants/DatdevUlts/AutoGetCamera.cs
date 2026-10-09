using System;
using UnityEngine;

namespace DatdevUlts
{
    public class AutoGetCamera : MonoBehaviour
    {
        private Canvas _canvas;
        private void Awake()
        {
            GetCamera();
        }

        private void Update()
        {
            GetCamera();
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
            {
                GetCamera();
            }
        }

        public void GetCamera()
        {
            if (!_canvas)
            {
                _canvas = GetComponent<Canvas>();
            }
            
            if (!_canvas.worldCamera)
            {
                _canvas.worldCamera = Camera.main;
            }
        }
    }
}