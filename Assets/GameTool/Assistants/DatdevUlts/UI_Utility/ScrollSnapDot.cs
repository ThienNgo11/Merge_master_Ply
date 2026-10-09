using System.Collections.Generic;
using UnityEngine;

namespace DatdevUlts.UI_Utility
{
    public class ScrollSnapDot : MonoBehaviour
    {
        [SerializeField] private List<GameObject> _listGameObjectOn;
        [SerializeField] private List<GameObject> _listGameObjectOff;
        private bool _isOn;

        public List<GameObject> ListGameObjectOn => _listGameObjectOn;

        public List<GameObject> ListGameObjectOff => _listGameObjectOff;

        public bool IsOn
        {
            get => _isOn;
            private set => _isOn = value;
        }

        public virtual void Switch(bool isOn)
        {
            IsOn = isOn;
            RefreshUI();
        }

        public virtual void RefreshUI()
        {
            for (int i = 0; i < ListGameObjectOn.Count; i++)
            {
                ListGameObjectOn[i].SetActive(IsOn);
            }
            for (int i = 0; i < ListGameObjectOff.Count; i++)
            {
                ListGameObjectOff[i].SetActive(!IsOn);
            }
        }
    }
}