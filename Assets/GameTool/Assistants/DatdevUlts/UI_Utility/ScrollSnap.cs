using System.Collections;
using System.Collections.Generic;
using GameTool.Assistants.DesignPattern;
using GameToolSample.Scripts.Enum;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DatdevUlts.UI_Utility
{
    public class ScrollSnap : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        [Tooltip("Có thể Update để lerp không")] [SerializeField]
        private bool _canLerp = true;

        [Tooltip("Đặt chỉ mục trang bắt đầu - bắt đầu từ 0")] [SerializeField]
        private int _startingPage;

        [Tooltip("Thời gian ngưỡng để vuốt nhanh tính bằng giây")] [SerializeField]
        private float _fastSwipeThresholdTime = 0.3f;

        [Tooltip("Thời gian ngưỡng để vuốt nhanh vào các pixel (không chia tỷ lệ)")] [SerializeField]
        private int _fastSwipeThresholdDistance = 100;

        [Tooltip("Tốc độ trang sẽ đến vị trí mục tiêu nhanh như thế nào")] [SerializeField]
        private float _decelerationRate = 10f;

        [Tooltip("Nút để chuyển đến trang trước")] [SerializeField]
        private Button _prevButton;

        [Tooltip("Nút để chuyển đến trang tiếp theo")] [SerializeField]
        private Button _nextButton;

        [Tooltip("Vuốt ngang hay dọc")] [SerializeField]
        private bool _horizontal;

        [Tooltip("Sự kiện khi trang thay đổi")] [SerializeField]
        private UnityEvent _onPageChangeEvent;

        private int _fastSwipeThresholdMaxLimit;

        private ScrollRect _scrollRectComponent;
        private RectTransform _scrollRectRect;
        private RectTransform _container;


        // number of pages in container
        private int _pageCount;
        private int _currentPage;

        // whether lerping is in progress and target lerp position
        private bool _lerping;
        private Vector2 _lerpTo;

        // target position of every page
        private List<Vector2> _pagePositions = new List<Vector2>();

        // in draggging, when dragging started and where it started
        private bool _dragging;
        private float _timeStamp;
        private Vector2 _startPosition;

        // for showing small page icons
        private bool _showPageSelection;

        public UnityEvent OnPageChangeEvent => _onPageChangeEvent;
        public int CurrentPageIndex => _currentPage;

        public int PageCount => _pageCount;

        public bool CanLerp
        {
            get => _canLerp;
            set => _canLerp = value;
        }

        private void OnEnable()
        {
            ReInit();

            this.RegisterListener(EventID.ChangeCamera, Callback);
        }

        private void OnDisable()
        {
            this.RemoveListener(EventID.ChangeCamera, Callback);
        }

        private void Callback(Component arg1, object[] arg2)
        {
            StartCoroutine(Wait());

            IEnumerator Wait()
            {
                yield return null;
                yield return null;

                ReInit();
            }
        }

        //------------------------------------------------------------------------
        public void InitStart()
        {
            _scrollRectComponent = GetComponent<ScrollRect>();
            _scrollRectRect = GetComponent<RectTransform>();
            _container = _scrollRectComponent.content;

            _container.anchorMin = Vector2.one / 2;
            _container.anchorMax = Vector2.one / 2;

            _lerping = false;

            if (_nextButton)
                _nextButton.GetComponent<Button>().onClick.AddListener(NextScreen);

            if (_prevButton)
                _prevButton.GetComponent<Button>().onClick.AddListener(PreviousScreen);

            ReInit();
        }

        public void ReInit()
        {
            if (!_scrollRectComponent)
            {
                return;
            }

            _pageCount = _container.childCount;
            for (int i = 0; i < _container.childCount; i++)
            {
                var rect = ((RectTransform)_container.GetChild(i));
                rect.anchorMin = Vector2.one / 2;
                rect.anchorMax = Vector2.one / 2;
            }

            SetPagePositions();
            SetPage(_startingPage);
        }

        //------------------------------------------------------------------------
        void Update()
        {
            // if moving to target position
            if (CanLerp && _lerping)
            {
                // prevent overshooting with values greater than 1
                float decelerate = Mathf.Min(_decelerationRate * Time.unscaledDeltaTime, 1f);
                _container.anchoredPosition = Vector2.Lerp(_container.anchoredPosition, _lerpTo, decelerate);
                // time to stop lerping?
                if ((_container.anchoredPosition - _lerpTo).magnitude < 1f)
                {
                    // snap to target and stop lerping
                    _container.anchoredPosition = _lerpTo;
                    _lerping = false;
                    // clear also any scrollrect move that may interfere with our lerping
                    _scrollRectComponent.velocity = Vector2.zero;
                }
            }
        }

        //------------------------------------------------------------------------
        private void SetPagePositions()
        {
            int offsetX;
            int offsetY;
            int containerWidth;
            int containerHeight;
            var width = (int)_scrollRectRect.rect.width;
            var height = (int)_scrollRectRect.rect.height;

            if (_horizontal)
            {
                offsetX = width / 2;
                offsetY = 0;

                containerWidth = (width) * _pageCount;
                containerHeight = (int)_container.sizeDelta.y;

                _fastSwipeThresholdMaxLimit = width;
            }
            else
            {
                offsetY = height / 2;
                offsetX = 0;
                containerHeight = height * _pageCount;
                containerWidth = (int)_container.sizeDelta.x;
                _fastSwipeThresholdMaxLimit = height;
            }

            // set width of container
            Vector2 newSize = new Vector2(containerWidth, containerHeight);
            _container.sizeDelta = newSize;
            Vector2 newPosition = new Vector2((float)containerWidth / 2, 0);
            _container.anchoredPosition = newPosition;

            // delete any previous settings
            _pagePositions.Clear();

            // iterate through all container childern and set their positions
            for (int i = 0; i < _pageCount; i++)
            {
                RectTransform child = _container.GetChild(i).GetComponent<RectTransform>();
                Vector2 childPosition = _horizontal
                    ? new Vector2(i * width - containerWidth / 2f + offsetX, 0)
                    : new Vector2(offsetX, -(i * height - containerHeight / 2 + offsetY));

                child.sizeDelta = new Vector2(width, height);
                child.anchoredPosition = childPosition;
                _pagePositions.Add(-childPosition);
            }
        }

        //------------------------------------------------------------------------
        private void SetPage(int aPageIndex)
        {
            _container.anchoredPosition = _pagePositions[aPageIndex];
            _currentPage = aPageIndex;
            if (_nextButton)
                _nextButton.gameObject.SetActive(aPageIndex != _pageCount - 1);
            if (_prevButton)
                _prevButton.gameObject.SetActive(aPageIndex != 0);
            
            OnPageChangeEvent?.Invoke();
        }

        //------------------------------------------------------------------------
        public void LerpToPage(int aPageIndex)
        {
            aPageIndex = Mathf.Clamp(aPageIndex, 0, _pageCount - 1);
            _lerpTo = _pagePositions[aPageIndex];
            _lerping = true;
            _currentPage = aPageIndex;
            if (_nextButton) _nextButton.gameObject.SetActive(aPageIndex != _pageCount - 1);
            if (_prevButton) _prevButton.gameObject.SetActive(aPageIndex != 0);

            OnPageChangeEvent?.Invoke();
        }

        //------------------------------------------------------------------------
        public void NextScreen()
        {
            LerpToPage(_currentPage + 1);
        }

        //------------------------------------------------------------------------
        public void PreviousScreen()
        {
            LerpToPage(_currentPage - 1);
        }

        //------------------------------------------------------------------------
        private int GetNearestPage()
        {
            // based on distance from current position, find nearest page
            Vector2 currentPosition = _container.anchoredPosition;

            float distance = float.MaxValue;
            int nearestPage = _currentPage;

            for (int i = 0; i < _pagePositions.Count; i++)
            {
                float testDist = Vector2.SqrMagnitude(currentPosition - _pagePositions[i]);
                if (testDist < distance)
                {
                    distance = testDist;
                    nearestPage = i;
                }
            }

            return nearestPage;
        }

        //------------------------------------------------------------------------
        public void OnBeginDrag(PointerEventData aEventData)
        {
            // if currently lerping, then stop it as user is draging
            _lerping = false;
            // not dragging yet
            _dragging = false;
        }

        //------------------------------------------------------------------------
        public void OnEndDrag(PointerEventData aEventData)
        {
            // how much was container's content dragged
            float difference;
            if (_horizontal)
            {
                difference = _startPosition.x - _container.anchoredPosition.x;
            }
            else
            {
                difference = -(_startPosition.y - _container.anchoredPosition.y);
            }

            // test for fast swipe - swipe that moves only +/-1 item
            if (Time.unscaledTime - _timeStamp < _fastSwipeThresholdTime &&
                Mathf.Abs(difference) > _fastSwipeThresholdDistance &&
                Mathf.Abs(difference) < _fastSwipeThresholdMaxLimit)
            {
                if (difference > 0)
                {
                    NextScreen();
                }
                else
                {
                    PreviousScreen();
                }
            }
            else
            {
                // if not fast time, look to which page we got to
                LerpToPage(GetNearestPage());
            }

            _dragging = false;
        }

        //------------------------------------------------------------------------
        public void OnDrag(PointerEventData aEventData)
        {
            if (!_dragging)
            {
                // dragging started
                _dragging = true;
                // save time - unscaled so pausing with Time.scale should not affect it
                _timeStamp = Time.unscaledTime;
                // save current position of cointainer
                _startPosition = _container.anchoredPosition;
            }
        }
    }
}