using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DatdevUlts.UI_Utility
{
    public class NestedScrollRect : ScrollRect
    {
        [SerializeField] private ScrollRect m_parentScrollRect;

        private bool m_routeToParent;

        private IInitializePotentialDragHandler[] m_parentInitializePotentialDragHandlers;
        private IBeginDragHandler[] m_parentBeginDragHandlers;
        private IDragHandler[] m_parentDragHandlers;
        private IEndDragHandler[] m_parentEndDragHandlers;

        public ScrollRect ParentScrollRect
        {
            get => m_parentScrollRect;
            set
            {
                m_parentScrollRect = value;

                if (ParentScrollRect)
                {
                    MParentInitializePotentialDragHandlers =
                        ParentScrollRect.GetComponents<IInitializePotentialDragHandler>();
                    MParentBeginDragHandlers = ParentScrollRect.GetComponents<IBeginDragHandler>();
                    MParentDragHandlers = ParentScrollRect.GetComponents<IDragHandler>();
                    MParentEndDragHandlers = ParentScrollRect.GetComponents<IEndDragHandler>();
                }
            }
        }

        public bool MRouteToParent
        {
            get => m_routeToParent;
            set => m_routeToParent = value;
        }

        public IInitializePotentialDragHandler[] MParentInitializePotentialDragHandlers
        {
            get => m_parentInitializePotentialDragHandlers;
            set => m_parentInitializePotentialDragHandlers = value;
        }

        public IBeginDragHandler[] MParentBeginDragHandlers
        {
            get => m_parentBeginDragHandlers;
            set => m_parentBeginDragHandlers = value;
        }

        public IDragHandler[] MParentDragHandlers
        {
            get => m_parentDragHandlers;
            set => m_parentDragHandlers = value;
        }

        public IEndDragHandler[] MParentEndDragHandlers
        {
            get => m_parentEndDragHandlers;
            set => m_parentEndDragHandlers = value;
        }

        public override void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (ParentScrollRect)
            {
                for (int i = 0; i < MParentInitializePotentialDragHandlers.Length; ++i)
                {
                    MParentInitializePotentialDragHandlers[i].OnInitializePotentialDrag(eventData);
                }
            }

            base.OnInitializePotentialDrag(eventData);
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            MRouteToParent = (!horizontal && Mathf.Abs(eventData.delta.x) > Mathf.Abs(eventData.delta.y)) ||
                             (!vertical && Mathf.Abs(eventData.delta.x) < Mathf.Abs(eventData.delta.y));
            if (!ParentScrollRect)
            {
                MRouteToParent = false;
            }

            if (MRouteToParent)
            {
                for (int i = 0; i < MParentBeginDragHandlers.Length; ++i)
                {
                    MParentBeginDragHandlers[i].OnBeginDrag(eventData);
                }
            }
            else
            {
                base.OnBeginDrag(eventData);
            }
        }

        public override void OnDrag(PointerEventData eventData)
        {
            if (MRouteToParent)
            {
                for (int i = 0; i < MParentDragHandlers.Length; ++i)
                {
                    MParentDragHandlers[i].OnDrag(eventData);
                }
            }
            else
            {
                base.OnDrag(eventData);
            }
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            if (MRouteToParent)
            {
                for (int i = 0; i < MParentEndDragHandlers.Length; ++i)
                {
                    MParentEndDragHandlers[i].OnEndDrag(eventData);
                }
            }
            else
            {
                base.OnEndDrag(eventData);
            }

            MRouteToParent = false;
        }

        protected override void Awake()
        {
            base.Awake();

            if (ParentScrollRect != null)
            {
                MParentInitializePotentialDragHandlers =
                    ParentScrollRect.GetComponents<IInitializePotentialDragHandler>();
                MParentBeginDragHandlers = ParentScrollRect.GetComponents<IBeginDragHandler>();
                MParentDragHandlers = ParentScrollRect.GetComponents<IDragHandler>();
                MParentEndDragHandlers = ParentScrollRect.GetComponents<IEndDragHandler>();
            }
        }
    }
}