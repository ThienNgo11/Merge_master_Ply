#if USE_SPINE
using System.Collections;
using Spine;
using Spine.Unity;
using AnimationState = Spine.AnimationState;
#endif

using UnityEngine;

namespace DatdevUlts
{
    public class SpineUtils : MonoBehaviour
    {
#if USE_SPINE

        private SkeletonAnimation _skeletonAnimation;
        private SkeletonGraphic _skeletonGraphic;

        private int _checkAnimProtect;

        private Coroutine waitAnimation;
        private Coroutine waitPausing;
        private Coroutine waitSetTime;
        private Coroutine waitSetDataAsset;
        private Coroutine waitUpdateAnim;
        private Coroutine waitSetskin;
        private Coroutine waitSetScaleX;
        private Coroutine waitSetScaleY;

        private bool canPause;
        private bool isPausedLogic;

        public string CurrentSkinName
        {
            get
            {
                if (SkeletonGraphic)
                {
                    return SkeletonGraphic.Skeleton.Skin.Name;
                }
                return SkeletonAnimation.Skeleton.Skin.Name;
            }
        }

        public string CurrentAnimationName
        {
            get
            {
                if (SkeletonGraphic)
                {
                    return SkeletonGraphic.AnimationState.GetCurrent(0).Animation.Name;
                }
                return SkeletonAnimation.AnimationState.GetCurrent(0).Animation.Name;
            }
        }

        public SkeletonGraphic SkeletonGraphic
        {
            get
            {
                CheckComponent();
                return _skeletonGraphic;
            }
        }

        public SkeletonAnimation SkeletonAnimation
        {
            get
            {
                CheckComponent();
                return _skeletonAnimation;
            }
        }

        public ISkeletonComponent SkeletonComponent
        {
            get
            {
                CheckComponent();
                if (SkeletonGraphic)
                {
                    return SkeletonGraphic;
                }

                if (SkeletonAnimation)
                {
                    return SkeletonAnimation;
                }

                return null;
            }
        }

        public int CheckAnimProtect => AddCheckProtect(_checkAnimProtect);

        private void Awake()
        {
            CoroutineRunner.Instance.StartCoroutine(Wait());

            IEnumerator Wait()
            {
                yield return null;
                yield return null;
                yield return null;

                canPause = true;
            }
        }

        private void CheckComponent()
        {
            if (!_skeletonAnimation && !_skeletonGraphic)
            {
                _skeletonAnimation = GetComponent<SkeletonAnimation>();
                _skeletonGraphic = GetComponent<SkeletonGraphic>();
            }

            if (!_skeletonAnimation && !_skeletonGraphic)
            {
                Debug.LogError("Cannot get component");
            }
        }

        private void CheckCoroutine(ref Coroutine coroutine, IEnumerator callback)
        {
            if (coroutine != null)
            {
                CoroutineRunner.Instance.StopCoroutine(coroutine);
            }

            coroutine = CoroutineRunner.Instance.StartCoroutine(callback);
        }

        // Huỷ hết các Animation sắp tới, luôn luôn ra Anim này
        public void SetAnimProtect(string nameAnim, bool loop, float timeScale = 1f, System.Action callBack = null,
            System.Action callBackStart = null, float mixDuration = -1)
        {
            CancelAllAnimNonProtect();
            SetAnimation(nameAnim, loop, timeScale, callBack, callBackStart, mixDuration);
        }

        private int AddCheckProtect(int check)
        {
            if (check > int.MaxValue - 1000)
            {
                check = -1;
            }

            check++;

            return check;
        }

        /// <summary>
        /// Hàm này thường gọi ở callback của các hàm, hàm SetAnimProtect sẽ huỷ callback gọi anim này
        /// </summary>
        /// <param name="nameAnim"></param>
        /// <param name="loop"></param>
        /// <param name="checkAnimProtectWhenCallProtect">Lấy CheckAnimProtect của component này đưa ra một biến khác ở ngoài callback rồi đưa vào</param>
        /// <param name="timeScale"></param>
        /// <param name="callBack"></param>
        /// <param name="callBackStart"></param>
        /// <param name="mixDuration"></param>
        public void SetAnimNonProtect(string nameAnim, bool loop, int checkAnimProtectWhenCallProtect,
            float timeScale = 1f, System.Action callBack = null, System.Action callBackStart = null,
            float mixDuration = -1)
        {
            if (checkAnimProtectWhenCallProtect == _checkAnimProtect)
            {
                SetAnimation(nameAnim, loop, timeScale, callBack, callBackStart, mixDuration);
            }
        }

        public void CancelAllAnimNonProtect()
        {
            _checkAnimProtect = AddCheckProtect(_checkAnimProtect);
        }

        private void SetAnimation(string nameAnim, bool loop, float timeScale = 1f, System.Action callBack = null,
            System.Action callBackStart = null, float mixDuration = -1)
        {
            if (waitAnimation != null)
            {
                CoroutineRunner.Instance.StopCoroutine(waitAnimation);
            }

            waitAnimation =
                CoroutineRunner.Instance.StartCoroutine(IESetAnimation(nameAnim, loop, timeScale, callBack,
                    callBackStart, mixDuration));
        }

        public void SetAnimationStateEvent(AnimationState.TrackEntryEventDelegate HandleAnimationStateEvent,
            bool isAdd)
        {
            CoroutineRunner.Instance.StartCoroutine(Wait());

            IEnumerator Wait()
            {
                CheckComponent();

                if (!SkeletonAnimation && !SkeletonGraphic)
                {
                    yield return new WaitUntil(() => SkeletonAnimation || SkeletonGraphic);
                }

                if (SkeletonGraphic)
                {
                    if (SkeletonGraphic.AnimationState == null)
                    {
                        yield return new WaitUntil(() => SkeletonGraphic.AnimationState != null);
                    }

                    if (isAdd)
                    {
                        SkeletonGraphic.AnimationState.Event += HandleAnimationStateEvent;
                    }
                    else
                    {
                        SkeletonGraphic.AnimationState.Event -= HandleAnimationStateEvent;
                    }
                }
                else if (SkeletonAnimation)
                {
                    if (SkeletonAnimation.AnimationState == null)
                    {
                        yield return new WaitUntil(() => SkeletonAnimation.AnimationState != null);
                    }

                    if (isAdd)
                    {
                        SkeletonAnimation.AnimationState.Event += HandleAnimationStateEvent;
                    }
                    else
                    {
                        SkeletonAnimation.AnimationState.Event -= HandleAnimationStateEvent;
                    }
                }
            }
        }

        private IEnumerator IESetAnimation(string nameAnim, bool loop, float timeScale = 1f,
            System.Action callBack = null, System.Action callBackStart = null, float mixDuration = -1)
        {
            CheckComponent();

            if (!SkeletonAnimation && !SkeletonGraphic)
            {
                yield return new WaitUntil(() => SkeletonAnimation || SkeletonGraphic);
            }

            if (SkeletonGraphic)
            {
                if (SkeletonGraphic.AnimationState == null)
                {
                    yield return new WaitUntil(() => SkeletonGraphic.AnimationState != null);
                }

                Spine.Animation runAnimation = SkeletonGraphic.SkeletonData.FindAnimation(nameAnim);
                if (runAnimation != null)
                {
                    // ReSharper disable once PossibleNullReferenceException
                    TrackEntry animationEntry = SkeletonGraphic.AnimationState.SetAnimation(0, nameAnim, loop);
                    SetTrack(animationEntry);
                }
                else
                {
                    Debug.LogError($"{nameAnim} is null");
                }
            }
            else if (SkeletonAnimation)
            {
                if (SkeletonAnimation.AnimationState == null)
                {
                    yield return new WaitUntil(() => SkeletonAnimation.AnimationState != null);
                }

                Spine.Animation runAnimation = SkeletonAnimation.skeleton.Data.FindAnimation(nameAnim);
                if (runAnimation != null)
                {
                    // ReSharper disable once PossibleNullReferenceException
                    TrackEntry animationEntry = SkeletonAnimation.AnimationState.SetAnimation(0, nameAnim, loop);
                    SetTrack(animationEntry);
                }
                else
                {
                    Debug.LogError($"{nameAnim} is null");
                }
            }

            void SetTrack(TrackEntry animationEntry)
            {
                if (mixDuration >= 0)
                {
                    animationEntry.MixDuration = mixDuration;
                }

                animationEntry.TimeScale = timeScale;

                if (callBackStart != null)
                {
                    animationEntry.Start += _ => { callBackStart(); };
                }

                if (callBack != null)
                {
                    animationEntry.Complete += _ => { callBack(); };
                }
            }
        }

        public void PauseUpdate()
        {
            isPausedLogic = true;
            CheckCoroutine(ref waitPausing, Wait());

            IEnumerator Wait()
            {
                if (!canPause)
                {
                    yield return new WaitUntil(() => canPause);
                }

                CheckComponent();

                if (SkeletonGraphic)
                {
                    SkeletonGraphic.freeze = true;
                    SkeletonGraphic.UpdateMode = UpdateMode.Nothing;
                }
                else if (SkeletonAnimation)
                {
                    SkeletonAnimation.UpdateMode = UpdateMode.Nothing;
                }
            }
        }

        public void ResumeUpdate(UpdateMode updateMode = UpdateMode.FullUpdate)
        {
            isPausedLogic = false;

            CheckCoroutine(ref waitPausing, Wait());

            IEnumerator Wait()
            {
                if (!canPause)
                {
                    yield return new WaitUntil(() => canPause);
                }

                CheckComponent();
                if (SkeletonGraphic)
                {
                    SkeletonGraphic.UpdateMode = updateMode;
                    SkeletonGraphic.freeze = false;
                }
                else if (SkeletonAnimation)
                {
                    SkeletonAnimation.UpdateMode = updateMode;
                }
            }
        }

        public void SetTimeOfAnimation(float playTime)
        {
            CheckCoroutine(ref waitSetTime, WaitSke(() =>
            {
                var currentState = SkeletonAnimation
                    ? SkeletonAnimation.AnimationState
                    : SkeletonGraphic.AnimationState;
                var currentTrack = currentState.GetCurrent(0);
                if (currentTrack != null)
                    currentTrack.TrackTime = playTime;
                currentState.Update(0);
            }));
        }

        public void SetDataAsset(SkeletonDataAsset dataAsset, string[] nameSkins, bool checkSkinNull = true)
        {
            CheckCoroutine(ref waitSetDataAsset, WaitSke(() =>
            {
                var skeletonComponent = SkeletonComponent;
                string name_ = "";

                Skeleton skeleton = null;
                SkeletonData skeletonData = null;

                SetInitialSkinName(dataAsset);

                if (SkeletonAnimation)
                {
                    SkeletonAnimation.ClearState();
                    SkeletonAnimation.skeletonDataAsset = dataAsset;

                    skeleton = skeletonComponent.Skeleton;
                    skeletonData = dataAsset.GetSkeletonData(false);
                }
                else if (SkeletonGraphic)
                {
                    SkeletonGraphic.Clear();
                    SkeletonGraphic.skeletonDataAsset = dataAsset;

                    skeleton = skeletonComponent.Skeleton;
                    skeletonData = dataAsset.GetSkeletonData(false);
                }
                
                Initialize();
                ChangeSkinSkeleton();

                void Initialize()
                {
                    if (SkeletonAnimation)
                    {
                        SkeletonAnimation.Initialize(true);
                    }
                    else if (SkeletonGraphic)
                    {
                        SkeletonGraphic.Initialize(true);
                        SkeletonGraphic.SetMaterialDirty();
                    }
                }

                void SetInitialSkinName(SkeletonDataAsset skeletonData_)
                {
                    for (int i = 0; i < nameSkins.Length; i++)
                    {
                        name_ = nameSkins[i];

                        if (skeletonData_.GetSkeletonData(false).FindSkin(name_) != null)
                        {
                            break;
                        }

                        if (checkSkinNull)
                        {
                            Debug.LogError($"Name skin {name_} is null");
                        }
                    }

                    if (SkeletonAnimation)
                    {
                        SkeletonAnimation.initialSkinName = name_;
                    }
                    else if (SkeletonGraphic)
                    {
                        SkeletonGraphic.initialSkinName = name_;
                    }
                }

                void ChangeSkinSkeleton()
                {
                    Skin characterSkin = new Skin(name_);

                    for (int i = 0; i < nameSkins.Length; i++)
                    {
                        name_ = nameSkins[i];

                        Skin findSkin = skeletonData.FindSkin(name_);
                        if (findSkin != null)
                        {
                            characterSkin.AddSkin(findSkin);
                        }
                        else
                        {
                            Debug.LogError($"Name skin {name_} is null");
                        }
                    }

                    skeleton.SetSkin(characterSkin);
                    skeleton.SetSlotsToSetupPose();

                    if (SkeletonAnimation)
                    {
                        SkeletonAnimation.Update(0);
                    }
                    else if (SkeletonGraphic)
                    {
                        SkeletonGraphic.Update(0);
                    }
                }
            }));
        }

        public void SetSkin(params string[] nameSkins)
        {
            CheckCoroutine(ref waitSetskin, WaitSke(() =>
            {
                var skeletonComponent = SkeletonComponent;

                var skeleton = skeletonComponent.Skeleton;
                var skeletonData = skeleton.Data;

                Skin characterSkin = new Skin(nameSkins[0]);

                for (int i = 0; i < nameSkins.Length; i++)
                {
                    var name_ = nameSkins[i];

                    if (skeletonData.FindSkin(name_) != null)
                    {
                        characterSkin.AddSkin(skeletonData.FindSkin(name_));
                    }
                    else
                    {
                        Debug.LogError($"Name skin {name_} is null");
                    }
                }

                skeleton.SetSkin(characterSkin);
                skeleton.SetSlotsToSetupPose();

                if (SkeletonAnimation)
                {
                    SkeletonAnimation.Update(0);
                }
                else if (SkeletonGraphic)
                {
                    SkeletonGraphic.Update(0);
                }
            }));
        }

        public float ScaleX
        {
            get
            {
                if (SkeletonAnimation)
                {
                    return SkeletonAnimation.Skeleton.ScaleX;
                }

                return SkeletonGraphic.Skeleton.ScaleX;
            }
            set
            {
                CheckCoroutine(ref waitSetScaleX, WaitSke(() =>
                {
                    if (SkeletonAnimation)
                    {
                        SkeletonAnimation.Skeleton.ScaleX = value;
                    }
                    else
                    {
                        SkeletonGraphic.Skeleton.ScaleX = value;
                    }
                }));
            }
        }

        public float ScaleY
        {
            get
            {
                if (SkeletonAnimation)
                {
                    return SkeletonAnimation.Skeleton.ScaleY;
                }

                return SkeletonGraphic.Skeleton.ScaleY;
            }
            set
            {
                CheckCoroutine(ref waitSetScaleY, WaitSke(() =>
                {
                    if (SkeletonAnimation)
                    {
                        SkeletonAnimation.Skeleton.ScaleY = value;
                    }
                    else
                    {
                        SkeletonGraphic.Skeleton.ScaleY = value;
                    }
                }));
            }
        }


        private IEnumerator WaitSke(System.Action callback)
        {
            CheckComponent();

            if (!SkeletonAnimation && !SkeletonGraphic)
            {
                yield return new WaitUntil(() => SkeletonAnimation || SkeletonGraphic);
            }

            if (SkeletonGraphic)
            {
                if (SkeletonGraphic.AnimationState == null)
                {
                    yield return new WaitUntil(() => SkeletonGraphic.AnimationState != null);
                }

                callback?.Invoke();
            }
            else if (SkeletonAnimation)
            {
                if (SkeletonAnimation.AnimationState == null)
                {
                    yield return new WaitUntil(() => SkeletonAnimation.AnimationState != null);
                }

                callback?.Invoke();
            }
        }

        /// <summary>
        /// Trong trường hợp không được khi Pause (thường là xương không di chuyển theo)
        /// Hãy dùng hàm ForceRefresh
        /// </summary>
        public void RefreshAnim()
        {
            CheckCoroutine(ref waitUpdateAnim, WaitSke(() =>
            {
                var isPausing = false;
                if (isPausedLogic)
                {
                    isPausing = true;
                    ResumeUpdate();
                }

                if (SkeletonAnimation)
                {
                    SkeletonAnimation.Update(0);
                    SkeletonAnimation.LateUpdate();
                }
                else if (SkeletonGraphic)
                {
                    SkeletonGraphic.Update(0);
                    SkeletonGraphic.LateUpdate();
                }

                if (isPausing)
                {
                    PauseUpdate();
                }
            }));
        }

        /// <summary>
        /// Hàm refresh cả bone
        /// </summary>
        public void RefreshAnimAndBone()
        {
            CheckCoroutine(ref waitUpdateAnim, WaitSke(() =>
            {
                var isPausing = false;
                if (isPausedLogic)
                {
                    isPausing = true;
                    ResumeUpdate();
                }

                if (SkeletonAnimation)
                {
                    SkeletonAnimation.Update(0);
                    SkeletonAnimation.LateUpdate();
                }
                else if (SkeletonGraphic)
                {
                    SkeletonGraphic.Update(0);
                    SkeletonGraphic.LateUpdate();
                }

                if (isPausing)
                {
                    CheckCoroutine(ref waitPausing, Wait());

                    IEnumerator Wait()
                    {
                        yield return null;
                        yield return null;
                        PauseUpdate();
                    }
                }
            }));
        }
#endif
    }
}