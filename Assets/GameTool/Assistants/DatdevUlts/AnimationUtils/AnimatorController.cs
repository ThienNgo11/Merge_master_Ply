using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace DatdevUlts.AnimationUtils
{
    [RequireComponent(typeof(Animator))]
    public class AnimatorController : MonoBehaviour
    {
        [SerializeField] private bool _loop;
        [SerializeField] private bool _pausing;
        [SerializeField] private float _timeScale = 1;
        [SerializeField] private bool _ignoreTimeScale;
        [SerializeField] private float _defaultMixDuration = 0.25f;
        [SerializeField] private bool _enableLog;
        [AnimName] [SerializeField] private string _animName;
        private string m_animName;
        private Animator _animator;
        private AnimatorEvent _animatorEvent;
        private bool _pausingLoop;
        private bool _awaked;

        // Các biến đã bị lược bỏ do không còn cần thiết với native animator:
        // _keepUpdate, _protectEnd, _allowEndTime

        public bool Pause
        {
            get => _pausing;
            set
            {
                _pausing = value;
                UpdateAnimatorSpeed();
            }
        }

        public bool IgnoreTimeScale
        {
            get => _ignoreTimeScale;
            set
            {
                _ignoreTimeScale = value;
                UpdateAnimatorSpeed();
            }
        }

        public bool Loop
        {
            get => _loop;
            set => _loop = value;
        }

        public bool Awaked => _awaked;

        public float TimeScale
        {
            get => _timeScale;
            set
            {
                _timeScale = value;
                UpdateAnimatorSpeed();
            }
        }

        public string AnimName
        {
            get => m_animName;
            set => m_animName = value;
        }

        public bool EnableLog
        {
            get => _enableLog;
            set => _enableLog = value;
        }

        // Tự động generate backing field, gọn gàng hơn
        public Action OnStartAnim { get; set; }
        public Action OnEndAnim { get; set; }

        public Animator Animator
        {
            get
            {
                SetupAnimator();
                return _animator;
            }
        }

        public AnimatorEvent AnimatorEvent
        {
            get
            {
                if (!_animatorEvent) _animatorEvent = GetComponent<AnimatorEvent>();
                if (!_animatorEvent) _animatorEvent = gameObject.AddComponent<AnimatorEvent>();
                return _animatorEvent;
            }
        }

        private void Awake()
        {
            // FIX LUNA: KHÔNG tắt Animator. Để Unity/Luna tự quản lý vòng đời.
            SetupAnimator();
        }

        private void OnEnable()
        {
            UpdateAnimatorSpeed();
            if (!_awaked)
            {
                StartCoroutine(CheckAwake());
            }

            IEnumerator CheckAwake()
            {
                yield return null;
                _awaked = true;
            }
        }

        private void SetupAnimator()
        {
            if (!_animator)
            {
                _animator = GetComponentInChildren<Animator>();
            }
        }

        // FIX LUNA: Gom logic chỉnh tốc độ vào 1 hàm, dùng biến native Animator.speed
        private void UpdateAnimatorSpeed()
        {
            if (Animator == null) return;
            
            if (_pausing)
            {
                Animator.speed = 0;
            }
            else
            {
                Animator.speed = _timeScale; 
            }
        }

        public void Update()
        {
            if (_animator == null) return;
            
            // Đồng bộ speed liên tục phòng trường hợp can thiệp từ bên ngoài
            UpdateAnimatorSpeed(); 

            if (_animName != m_animName)
            {
                SetAnimation(_animName);
                return;
            }

            if (_pausing) return;

            // FIX LUNA: Chỉ đọc state để gọi Callback OnEndAnim, không can thiệp thời gian
            if (!_loop && !_pausingLoop && OnEndAnim != null)
            {
                var state = Animator.GetCurrentAnimatorStateInfo(0);
                
                // Nếu animation hiện tại chạy tới frame cuối (normalizedTime >= 1.0f)
                if (state.IsName(m_animName) && state.normalizedTime >= 1.0f)
                {
                    _pausingLoop = true;
                    
                    var callback = OnEndAnim;
                    OnEndAnim = null; // Xóa ngay để tránh invoke liên tục
                    callback?.Invoke();
                }
            }
        }

        public void SetAnimation(string animationName, bool loop, float timeScale = 1f, float mixDuration = -1f,
            Action onStart = null, Action onEnd = null, int layer = 0, bool quiet = true, bool protectEnd = true,
            bool forceCrossFade = true, bool forceReplay = false)
        {
            if (mixDuration <= -0.5f) mixDuration = _defaultMixDuration;

            if (layer == 0)
            {
                OnStartAnim = onStart;
                OnEndAnim = onEnd;
                _loop = loop;
                TimeScale = timeScale; // Tự động gọi UpdateAnimatorSpeed()
            }

            var currentAnimatorStateInfo = Animator.GetCurrentAnimatorStateInfo(layer);
            var length = currentAnimatorStateInfo.length;
            var currentTime = length * (currentAnimatorStateInfo.normalizedTime - (int)currentAnimatorStateInfo.normalizedTime);

            if (!forceCrossFade && mixDuration > length - currentTime - 0.0001f)
            {
                mixDuration = length - currentTime - 0.0001f * 2;
            }

            var animNameBefore = m_animName;
            
            if (layer == 0)
            {
                _pausingLoop = false;
                m_animName = animationName;
                _animName = m_animName;
            }

            // Gọi chạy Animation
            if (forceReplay || animNameBefore != m_animName)
            {
                OnStartAnim?.Invoke();

                if (forceReplay)
                {
                    Animator.Play(animationName, layer, 0);
                }
                else
                {
                    Animator.Play(animationName, layer);
                }
            }
        }

        public void SetAnimation(string animName)
        {
            SetAnimation(animName, _loop);
        }

        [ContextMenu("SetAnimation")]
        public void SetAnimation()
        {
            SetAnimation(m_animName);
        }

        // FIX LUNA: Đã xóa hàm public void Update(float deltaTime)

        public void PostEvent(string eventName)
        {
            AnimatorEvent.InvokePostEvent(eventName);
        }

        public void RegistEvent(UnityAction<string> callBack)
        {
            AnimatorEvent.InvokeRegistEvent(callBack);
        }

        public void RemoveEvent(UnityAction<string> callBack)
        {
            AnimatorEvent.InvokeRemoveEvent(callBack);
        }

        public void ClearEvent()
        {
            AnimatorEvent.ClearEvent();
        }
    }
}