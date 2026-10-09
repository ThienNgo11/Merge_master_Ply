#if USE_SPINE
using System;
using System.Collections.Generic;
using System.Linq;
using Spine;
using Spine.Unity;
using Spine.Unity.Editor;
using UnityEditor;
using UnityEngine;
using Animation = Spine.Animation;
using AnimationState = Spine.AnimationState;
using Event = UnityEngine.Event;

#if UNITY_EDITOR

namespace GameTool.SpineAnimation.Editor
{
    public class SpineToolEditor : EditorWindow
    {
        public Skeleton curSkeleton;
        public SkeletonAnimation spine;
        public SkeletonGraphic uiSpine;
        public AnimationState currentState;
        public bool isAutoRun;
        public bool isNeedReload;
        public float autoRunSpeed = 1f;
        float animationLastTime;
        public Animation[] animations;
        public Skin[] skins;

        static float CurrentTime => (float)EditorApplication.timeSinceStartup;

        [MenuItem("GameTool/Spine/Spine Preview")]
        static void ShowWindow()
        {
            var editor = GetWindow(typeof(SpineToolEditor));
            editor.titleContent.text = "Spine Tool";
            editor.Show();
        }

        private Vector2 scrollPosition;
        private Vector2 skinPosition;
        private Animation currentAnim;
        private List<Skin> currentSkin = new List<Skin>();
        public GameObject obj;
        private float playTime;
        private string searchAnimation = string.Empty;
        private string searchAnimationOld = string.Empty;
        private string searchSkin = string.Empty;
        private string searchSkinOld = string.Empty;
        private float animPanelWidth = 300;
        private Rect _cursorChangeRect;
        private bool _isResize;
        private SkeletonDataAsset _currentSkeletonDataAsset;
        private const string toolTip1 = "Hole Ctrl + click to copy animation name";
        private const string toolTip2 = "Hole Ctrl + click to copy skin name";
        private bool keyboardSelectAnim = true;

        private void OnEnable()
        {
            EditorApplication.update -= HandleEditorUpdate;
            EditorApplication.update += HandleEditorUpdate;
            Selection.selectionChanged += SelectionChanged;
            _cursorChangeRect = new Rect(animPanelWidth, 0, 5, position.size.y);
            spine = null;
            uiSpine = null;
            if (Selection.activeGameObject != null)
            {
                var tempSpine = Selection.activeGameObject.GetComponent<SkeletonAnimation>();
                if (tempSpine != null)
                {
                    spine = tempSpine;
                    SelectionChanged();
                }

                var tempUiSpine = Selection.activeGameObject.GetComponent<SkeletonGraphic>();
                if (tempUiSpine != null)
                {
                    uiSpine = tempUiSpine;
                    SelectionChanged();
                }
            }
        }

        private void SelectionChanged()
        {
            UpdateSpine();
            Repaint();
        }

        private void OnDisable()
        {
            EditorApplication.update -= HandleEditorUpdate;
            Selection.selectionChanged -= SelectionChanged;
        }

        void HandleEditorUpdate()
        {
            if (currentAnim != null && currentState != null && currentState.GetCurrent(0) != null)
            {
                if (spine != null && _currentSkeletonDataAsset != spine.skeletonDataAsset)
                {
                    _currentSkeletonDataAsset = spine.skeletonDataAsset;
                    //spine.initialSkinName = "default";
                    spine.Initialize(true);
                    uiSpine = null;
                    UpdateSpine();
                }
                else if (uiSpine != null && _currentSkeletonDataAsset != uiSpine.skeletonDataAsset)
                {
                    _currentSkeletonDataAsset = uiSpine.skeletonDataAsset;
                    //uiSpine.initialSkinName = "default";
                    uiSpine.Initialize(true);
                    spine = null;
                    UpdateSpine();
                }

                if (isAutoRun)
                {
                    float deltaTime = CurrentTime - animationLastTime;
                    currentState.Update(deltaTime);
                    animationLastTime = CurrentTime;
                    currentState.TimeScale = autoRunSpeed;
                    var currentTrack = currentState.GetCurrent(0);
                    if (currentTrack != null && currentTrack.TrackTime >= currentAnim.Duration)
                    {
                        currentTrack.TrackTime = 0;
                    }

                    if (spine != null)
                    {
                        spine.LateUpdate();
                        spine.Skeleton.UpdateWorldTransform();
                    }
                    else if (uiSpine != null)
                    {
                        uiSpine.LateUpdate();
                        uiSpine.Skeleton.UpdateWorldTransform();
                        uiSpine.UpdateMesh();
                    }

                    if (currentTrack != null)
                        playTime = currentTrack.TrackTime;
                    Repaint();
                }
                else
                {
                    animationLastTime = CurrentTime;
                    currentState.TimeScale = autoRunSpeed;
                    var currentTrack = currentState.GetCurrent(0);
                    if (currentTrack != null)
                        currentTrack.TrackTime = playTime;
                    currentState.Update(0);

                    if (spine != null)
                    {
                        spine.LateUpdate();
                        spine.Skeleton.UpdateWorldTransform();
                    }
                    else if (uiSpine != null)
                    {
                        uiSpine.LateUpdate();
                        uiSpine.Skeleton.UpdateWorldTransform();
                        uiSpine.UpdateMesh();
                    }
                }
            }
        }

        private bool check;

        void OnGUI()
        {
            if (Event.current.type == EventType.MouseDown)
            {
                GUIUtility.keyboardControl = 0;
            }

            if (Application.isPlaying) return;

            // GUILayout.BeginHorizontal(SpineToolStyle.BgPopup2);
            //
            // GUILayout.EndHorizontal();
            if (curSkeleton != null)
            {
                GUILayout.BeginVertical(SpineToolStyle.BgPopup);
                {
                    GUILayout.BeginHorizontal();
                    isAutoRun = GUILayout.Toggle(isAutoRun, "Auto Run", GUILayout.ExpandWidth(false));
                    //SpineToolStyle.focusOnScene.text = spine == null ? uiSpine.name : spine.name;
                    // if (GUILayout.Button(SpineToolStyle.focusOnScene, GUILayout.ExpandWidth(false),
                    //     GUILayout.ExpandHeight(false)))
                    // {
                    //     Selection.activeGameObject = obj;
                    //     EditorGUIUtility.PingObject(Selection.activeObject);
                    // }

                    if (spine != null)
                    {
                        var skeleton =
                            (SkeletonDataAsset)EditorGUILayout.ObjectField("", spine.skeletonDataAsset,
                                typeof(SkeletonDataAsset), true);
                        if (skeleton != spine.skeletonDataAsset)
                        {
                            spine.skeletonDataAsset = skeleton;
                        }
                    }
                    else
                    {
                        var skeleton =
                            (SkeletonDataAsset)EditorGUILayout.ObjectField("", uiSpine.skeletonDataAsset,
                                typeof(SkeletonDataAsset), true);
                        if (skeleton != uiSpine.skeletonDataAsset)
                        {
                            uiSpine.skeletonDataAsset = skeleton;
                        }
                    }


                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.BeginVertical();
                        GUILayout.Label("Frame: " + (int)(playTime * 30), GUILayout.ExpandWidth(false));
                        foreach (var timeline in currentAnim.Timelines)
                        {
                            if (timeline is EventTimeline eventTimeline)
                            {
                                for (int i = 0; i < eventTimeline.Events.Length; i++)
                                {
                                    GUILayout.Label(eventTimeline.Events[i].Data.Name + " : " +
                                                    eventTimeline.Frames[i]);
                                }
                            }
                        }

                        GUILayout.EndVertical();
                    }
                    GUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    {
                        GUILayout.BeginVertical();
                        {
                            if (isAutoRun)
                            {
                                GUILayout.Label("Speed");
                                autoRunSpeed = EditorGUILayout.Slider(autoRunSpeed, 0, 1.5f);
                                currentState.TimeScale = autoRunSpeed;
                            }

                            if (currentAnim != null)
                            {
                                playTime = EditorGUILayout.Slider(playTime, 0, currentAnim.Duration);
                                var rect = GUILayoutUtility.GetLastRect();
                                rect.width -= 65f;
                                rect.position += new Vector2(1.5f, 0f);
                                foreach (var timeline in currentAnim.Timelines)
                                {
                                    if (timeline is EventTimeline eventTimeline)
                                    {
                                        for (int i = 0; i < eventTimeline.Events.Length; i++)
                                        {
                                            var r = rect;
                                            r.position +=
                                                new Vector2(rect.width * eventTimeline.Frames[i] / currentAnim.Duration,
                                                    0);
                                            GUI.Label(r, "|");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                EditorGUILayout.Slider(playTime, 0, 0);
                            }
                        }
                        GUILayout.EndVertical();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();

                GUILayout.BeginHorizontal();
                ViewAnimations();
                ViewSkins();
                GUILayout.EndHorizontal();

                if (isNeedReload)
                {
                    isNeedReload = false;
                    if (spine != null)
                    {
                        spine.ClearState();
                        if (currentAnim != null)
                            spine.AnimationName = currentAnim.Name;
                        //spine.initialSkinName = spine.skeletonDataAsset.GetSkeletonData(true).Skins.Items.Where(skin => skin.Name == spine.initialSkinName).ToArray()[0].Name;
                        spine.LateUpdate();
                        spine.Initialize(true);
                        currentState = spine.state;
                        currentState.TimeScale = autoRunSpeed;
                        spine.gameObject.SetActive(false);
                        spine.gameObject.SetActive(true);
                        ChangeSkin();
                    }
                    else if (uiSpine != null)
                    {
                        uiSpine.Clear();
                        if (currentAnim != null)
                            uiSpine.startingAnimation = currentAnim.Name;
                        uiSpine.AnimationState.SetAnimation(0, currentAnim.Name, false);
                        uiSpine.LateUpdate();
                        uiSpine.Initialize(true);
                        currentState.TimeScale = autoRunSpeed;
                        uiSpine.gameObject.SetActive(false);
                        uiSpine.gameObject.SetActive(true);
                        currentState = uiSpine.AnimationState;
                        ChangeSkin();
                    }

                    Repaint();
                }
            }
            else
            {
                GUILayout.Label("Please select a skeleton!", EditorStyles.helpBox);
            }
        }

        private void ViewAnimations()
        {
            GUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(animPanelWidth));
            searchAnimation = GUILayout.TextField(searchAnimation, SpineToolStyle.SearchBoxStyle);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            {
                int count = 0;

                if (searchAnimation != searchAnimationOld)
                {
                    searchAnimationOld = searchAnimation;
                    animations = curSkeleton.Data.Animations.Items
                        .Where(s => s.Name.ToLower().Contains(searchAnimation)).ToArray();
                }

                foreach (var animation in animations)
                {
                    if (animation == currentAnim)
                    {
                        GUILayout.BeginHorizontal(SpineToolStyle.AnimSelected);
                    }
                    else
                    {
                        GUILayout.BeginHorizontal();
                    }

                    GUILayout.Label(new GUIContent(SpineEditorUtilities.Icons.animation, toolTip1),
                        GUILayout.ExpandWidth(false));
                    GUILayout.Label($"{animation.Name}", SpineToolStyle.AnimName, GUILayout.Height(24));
                    GUILayout.Label($"{animation.Duration}", SpineToolStyle.AnimTime, GUILayout.ExpandWidth(false),
                        GUILayout.Height(24));
                    GUILayout.EndHorizontal();
                    var lastRect = GUILayoutUtility.GetLastRect();
                    if (Event.current.type == EventType.MouseDown && lastRect.Contains(Event.current.mousePosition))
                    {
                        if (spine)
                        {
                            EditorUtility.SetDirty(spine);
                        }
                        else if (uiSpine)
                        {
                            EditorUtility.SetDirty(uiSpine);
                        }

                        currentAnim = animation;
                        isNeedReload = true;
                        if (Event.current.control)
                        {
                            EditorGUIUtility.systemCopyBuffer = animation.Name;
                        }

                        Repaint();
                        keyboardSelectAnim = true;
                    }

                    count++;
                }

                if (Event.current.type == EventType.KeyDown && keyboardSelectAnim)
                {
                    int crrindex = 0;
                    bool isreturn = true;
                    if (Event.current.keyCode == KeyCode.DownArrow)
                    {
                        crrindex = animations.ToList().IndexOf(currentAnim) + 1;
                        isreturn = false;
                    }
                    else if (Event.current.keyCode == KeyCode.UpArrow)
                    {
                        crrindex = animations.ToList().IndexOf(currentAnim) - 1;
                        isreturn = false;
                    }

                    if (crrindex < animations.Length && crrindex >= 0 && !isreturn)
                    {
                        if (spine)
                        {
                            EditorUtility.SetDirty(spine);
                        }
                        else if (uiSpine)
                        {
                            EditorUtility.SetDirty(uiSpine);
                        }

                        currentAnim = animations[crrindex];
                        isNeedReload = true;
                    }
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            ResizeScrollView();
        }

        private void ViewSkins()
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            searchSkin = GUILayout.TextField(searchSkin, SpineToolStyle.SearchBoxStyle);
            skinPosition = GUILayout.BeginScrollView(skinPosition);
            {
                int count = 0;

                if (searchSkin != searchSkinOld)
                {
                    searchSkinOld = searchSkin;
                    skins = curSkeleton.Data.Skins.Items.Where(s => s.Name.ToLower().Contains(searchSkin)).ToArray();
                }

                foreach (var skin in skins)
                {
                    if (spine)
                    {
                        if (skin.Name == spine.initialSkinName)
                        {
                            currentSkin.Add(skin);
                        }
                    }
                    else if (uiSpine)
                    {
                        if (skin.Name == uiSpine.initialSkinName)
                        {
                            currentSkin.Add(skin);
                        }
                    }

                    if (currentSkin.Contains(skin))
                    {
                        GUILayout.BeginHorizontal(SpineToolStyle.AnimSelected);
                    }
                    else
                    {
                        GUILayout.BeginHorizontal();
                    }

                    GUILayout.Label(new GUIContent(SpineEditorUtilities.Icons.skin, toolTip2),
                        GUILayout.ExpandWidth(false));
                    GUILayout.Label($"{skin.Name}", SpineToolStyle.AnimName, GUILayout.Height(24));
                    GUILayout.EndHorizontal();
                    var lastRect = GUILayoutUtility.GetLastRect();
                    if (Event.current.type == EventType.MouseDown && lastRect.Contains(Event.current.mousePosition))
                    {
                        if (!currentSkin.Contains(skin))
                        {
                            currentSkin.Clear();
                            currentSkin.Add(skin);
                            if (spine)
                            {
                                spine.initialSkinName = skin.Name;
                            }
                            else if (uiSpine)
                            {
                                uiSpine.initialSkinName = skin.Name;
                            }
                        }
                        else
                        {
                            currentSkin.Remove(skin);
                        }

                        if (spine)
                        {
                            EditorUtility.SetDirty(spine);
                        }
                        else if (uiSpine)
                        {
                            EditorUtility.SetDirty(uiSpine);
                        }

                        ChangeSkin();
                        if (Event.current.control)
                        {
                            EditorGUIUtility.systemCopyBuffer = skin.Name;
                        }

                        Repaint();
                        keyboardSelectAnim = false;
                    }

                    count++;
                }

                if (Event.current.type == EventType.KeyDown && !keyboardSelectAnim)
                {
                    int crrindex = 0;
                    bool isreturn = true;
                    if (Event.current.keyCode == KeyCode.DownArrow)
                    {
                        crrindex = skins.ToList().FindIndex(skin => skin.Name == spine.initialSkinName) + 1;
                        isreturn = false;
                    }
                    else if (Event.current.keyCode == KeyCode.UpArrow)
                    {
                        crrindex = skins.ToList().FindIndex(skin => skin.Name == spine.initialSkinName) - 1;
                        isreturn = false;
                    }

                    if (crrindex < skins.Length && crrindex >= 0 && !isreturn)
                    {
                        var skin = skins[crrindex];
                        if (!currentSkin.Contains(skin))
                        {
                            currentSkin.Clear();
                            currentSkin.Add(skin);
                            if (spine)
                            {
                                spine.initialSkinName = skin.Name;
                            }
                            else if (uiSpine)
                            {
                                uiSpine.initialSkinName = skin.Name;
                            }
                        }
                        else
                        {
                            currentSkin.Remove(skin);
                        }

                        if (spine)
                        {
                            EditorUtility.SetDirty(spine);
                        }
                        else if (uiSpine)
                        {
                            EditorUtility.SetDirty(uiSpine);
                        }

                        ChangeSkin();

                        Repaint();
                    }
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void UpdateSpine()
        {
            if (Selection.activeGameObject != null)
            {
                var tempSpine = Selection.activeGameObject.GetComponent<SkeletonAnimation>();
                if (tempSpine != null)
                {
                    currentSkin.Clear();
                    spine = tempSpine;
                    uiSpine = null;
                    curSkeleton = spine.Skeleton;
                    obj = spine.gameObject;
                    UpdateViewList();
                    isNeedReload = true;
                    try
                    {
                        currentAnim = spine.skeleton.Data.Animations.Items
                            .Where(animation => animation.Name == spine.AnimationName).ToArray()[0];
                    }
                    catch (Exception e)
                    {
                        currentAnim = spine.skeleton.Data.Animations.Items[0];
                    }

                    try
                    {
                        spine.initialSkinName = spine.skeletonDataAsset.GetSkeletonData(true).Skins.Items
                            .Where(skin => skin.Name == spine.initialSkinName).ToArray()[0].Name;
                    }
                    catch (Exception e)
                    {
                        spine.initialSkinName = spine.skeletonDataAsset.GetSkeletonData(true).Skins.Items[0].Name;
                    }

                    spine.skeleton.SetSkin(spine.initialSkinName);
                    currentState = spine.state;
                }
                else
                {
                    var tempUiSpine = Selection.activeGameObject.GetComponent<SkeletonGraphic>();
                    if (tempUiSpine != null)
                    {
                        currentSkin.Clear();
                        uiSpine = tempUiSpine;
                        spine = null;
                        curSkeleton = uiSpine.Skeleton;
                        obj = uiSpine.gameObject;
                        UpdateViewList();
                        isNeedReload = true;

                        try
                        {
                            currentAnim = uiSpine.Skeleton.Data.Animations.Items.Where(animation =>
                                animation.Name == uiSpine.AnimationState.GetCurrent(0).Animation.Name).ToArray()[0];
                        }
                        catch (Exception e)
                        {
                            currentAnim = uiSpine.Skeleton.Data.Animations.Items[0];
                        }

                        try
                        {
                            uiSpine.initialSkinName = uiSpine.skeletonDataAsset.GetSkeletonData(true).Skins.Items
                                .Where(skin => skin.Name == uiSpine.initialSkinName).ToArray()[0].Name;
                        }
                        catch (Exception e)
                        {
                            uiSpine.initialSkinName =
                                uiSpine.skeletonDataAsset.GetSkeletonData(true).Skins.Items[0].Name;
                        }

                        uiSpine.Skeleton.SetSkin(uiSpine.initialSkinName);
                        currentState = uiSpine.AnimationState;
                    }
                }
            }
        }

        private void ChangeSkin()
        {
            if (currentSkin.Count <= 0)
            {
                return;
            }

            string _name = "";
            if (spine)
            {
                _name = spine.skeletonDataAsset.GetSkeletonData(true).Skins.Items
                    .Where(s => s.Name == spine.initialSkinName).ToArray()[0].Name;
            }

            else if (uiSpine)
            {
                _name = uiSpine.skeletonDataAsset.GetSkeletonData(true).Skins.Items
                    .Where(s => s.Name == uiSpine.initialSkinName).ToArray()[0].Name;
            }

            if (_name == "")
            {
                return;
            }

            Skin skin = new Skin(_name);
            foreach (var skin1 in currentSkin)
            {
                skin.AddSkin(skin1);
            }

            if (spine != null)
            {
                spine.Skeleton.Skin = skin;
                spine.initialSkinName = _name;
                spine.skeleton.SetSkin(skin);
                spine.skeleton.SetSlotsToSetupPose();
                spine.LateUpdate();
                spine.AnimationState.Apply(spine.Skeleton);
            }
            else if (uiSpine != null)
            {
                uiSpine.Skeleton.Skin = skin;
                uiSpine.initialSkinName = _name;
                uiSpine.Skeleton.SetSkin(skin);
                uiSpine.Skeleton.SetSlotsToSetupPose();
                uiSpine.LateUpdate();
                uiSpine.AnimationState.Apply(uiSpine.Skeleton);
            }
        }

        private void UpdateViewList()
        {
            animations = curSkeleton.Data.Animations.Items;
            skins = curSkeleton.Data.Skins.Items;
        }

        private void ResizeScrollView()
        {
            EditorGUIUtility.AddCursorRect(_cursorChangeRect, MouseCursor.ResizeHorizontal);

            if (Event.current.type == EventType.MouseDown && _cursorChangeRect.Contains(Event.current.mousePosition))
            {
                _isResize = true;
            }

            if (_isResize)
            {
                animPanelWidth = Event.current.mousePosition.x;
                animPanelWidth = Mathf.Clamp(animPanelWidth, 100, position.width - 100);
                _cursorChangeRect.Set(animPanelWidth, _cursorChangeRect.y, _cursorChangeRect.width,
                    _cursorChangeRect.height);
                // Repaint();
            }

            if (Event.current.type == EventType.MouseUp)
                _isResize = false;
        }
    }
}

#endif
#endif