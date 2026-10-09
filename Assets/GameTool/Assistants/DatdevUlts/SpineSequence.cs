using System.Collections.Generic;
using UnityEngine;

namespace DatdevUlts
{
    public class SpineSequence : MonoBehaviour
    {
        #if USE_SPINE
        
        [SerializeField] private SpineUtils _spineUtils;
        [SerializeField] private List<string> _sequence;
        [SerializeField] private bool _loopEnd = true;
        [SerializeField] private bool _loopSequence = true;
        [SerializeField] private bool _startOnEnable = true;
        [SerializeField] private bool _startOnAwake;

        private void Awake()
        {
            if (_startOnAwake)
            {
                StartSequence();
            }
        }

        private void OnEnable()
        {
            if (_startOnEnable)
            {
                StartSequence();
            }
        }

        public void StartSequence()
        {
            if (_spineUtils)
            {
                Sequence(0);
            }
        }

        private void Sequence(int index)
        {
            if (_sequence.Count > index)
            {
                #if USE_SPINE
                var end = _sequence.Count - 1 == index;

                _spineUtils.SetAnimProtect(_sequence[index], end && _loopEnd, callBack: () =>
                {
                    if (end && !_loopSequence)
                    {
                        return;
                    }
                    var indx = index + 1;
                    if (indx >= _sequence.Count)
                    {
                        indx = 0;
                    }
                    Sequence(indx);
                });
                #endif
            }
        }
        
        #endif
    }
}