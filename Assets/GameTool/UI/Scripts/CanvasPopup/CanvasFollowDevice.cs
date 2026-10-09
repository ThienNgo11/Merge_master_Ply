using System;
using System.Collections.Generic;
using GameToolSample.GamePlay.Manager;
using LunaTool;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GameTool.UI.Scripts.CanvasPopup
{
    public class CanvasFollowDevice : MonoBehaviour, IUpdate
    {
        [Header("COMPONENT")] [SerializeField] private RectTransform mainRect;
        [SerializeField] private CanvasScaler scaler;
        [SerializeField] private Camera cam;
        [SerializeField] private float aspect;
        [SerializeField] private float refAspect;
        [SerializeField] private bool fatCanvas;
        [SerializeField] private bool fixedScaler;
        [SerializeField] private bool fixedRefAspect;
        [SerializeField] private bool SourceGameIsLanscape;

        /// Aspect cam theo invert
        public float AspectCam;
        public string CurrentName;

        public CanvasScaler Scaler => scaler;
        public float Aspect => aspect;
        public float RefAspect => refAspect;

        /// Có đổi cam size không?
        [Header("CONFIG")] public bool changeCamSize;

        /// có đổi canvas scaler ko?
        public bool changeCanvasScaler;

        public bool changeValueByAuto;

        bool IsInvert;
#pragma warning disable CS0414
        float lastAspect = 0;
#pragma warning restore CS0414

        [Space] [Header("CAM SIZE")] public List<ResolutionInfor> Resolutions = new List<ResolutionInfor>
        {
            new ResolutionInfor
            {
                Name = "Square Phone",
                AspectCam = 1f,
            },
            new ResolutionInfor
            {
                Name = "Fold 2 5G Tablet",
                AspectCam = 2208f / 1768f,
            },
            new ResolutionInfor
            {
                Name = "Ipad",
                AspectCam = 2732f / 2048f,
            },
            new ResolutionInfor
            {
                Name = "Iphone 7",
                AspectCam = 2208f / 1242f,
            },
            new ResolutionInfor
            {
                Name = "Iphone XS Max",
                AspectCam = 2437f / 1125f,
            },
            new ResolutionInfor
            {
                Name = "Redmi Note 10s",
                AspectCam = 2400f / 1080f,
            },
            new ResolutionInfor
            {
                Name = "Z Flip 3",
                AspectCam = 2640f / 1080f,
            },
            new ResolutionInfor
            {
                Name = "Fold2 5G Phone",
                AspectCam = 2658f / 960f,
            },
            new ResolutionInfor
            {
                Name = "Long Phone",
                AspectCam = 3f,
            }
        };

        private void Start()
        {
            ChangeAll();
        }

        private void OnEnable()
        {
            GameManager.Instance.RegisUpdate(this);
        }

        private void OnDisable()
        {
            if (GameManager.IsInstanceValid())
            {
                GameManager.Instance.RemoveUpdate(this);
            }
        }

        [ContextMenu("Change All")]
        public void ChangeAll()
        {
            if (!mainRect)
            {
                mainRect = GetComponent<RectTransform>();
            }

            if (!scaler)
            {
                scaler = GetComponent<CanvasScaler>();
            }

            if (!cam)
            {
                cam = Camera.main;
            }

#if UNITY_EDITOR
            if (cam)
            {
                aspect = cam.pixelRect.width / cam.pixelRect.height;
            }
            else
            {
                aspect = (float)Screen.width / Screen.height;
            }
#else
            aspect = (float)Screen.width / Screen.height;
#endif
            IsInvert = aspect < 1f;

            if (scaler && !fixedRefAspect)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

                if (IsInvert)
                {
                    scaler.referenceResolution = new Vector2(1080f, 1920f);
                }
                else
                {
                    scaler.referenceResolution = new Vector2(1920f, 1080f);
                }

                // Safe area
                refAspect = scaler.referenceResolution.x / scaler.referenceResolution.y;
                if (!Mathf.Approximately(aspect, refAspect) && (!Mathf.Approximately(aspect, 1080f / 1920f) &&
                                                               !Mathf.Approximately(aspect, 1920f / 1080f)))
                {
                    if (IsInvert)
                    {
                        scaler.referenceResolution = new Vector2(1080f, 2240f);
                    }
                    else
                    {
                        scaler.referenceResolution = new Vector2(2240f, 1080f);
                    }
                }
            }

            refAspect = scaler.referenceResolution.x / scaler.referenceResolution.y;

            ChangeScaler();
            FixCamSizeFollowScreen();
        }


        public void ChangeScaler()
        {
            if (fixedScaler)
            {
                return;
            }

            if (aspect < 1080 / 1920f)
            {
                scaler.matchWidthOrHeight = 0;
            }
            else
            {
                scaler.matchWidthOrHeight = 1;
            }

            if (SourceGameIsLanscape)
            {
                scaler.matchWidthOrHeight = 1 - scaler.matchWidthOrHeight;
            }

            if (fatCanvas)
            {
                scaler.matchWidthOrHeight = 1 - scaler.matchWidthOrHeight;
            }
        }

#if !UNITY_LUNA || UNITY_EDITOR
        private void OnRectTransformDimensionsChange()
        {
            if (cam)
            {
                if (Math.Abs(cam.aspect - aspect) > 0.01f)
                {
                    ChangeAll();
                }
            }
            else
            {
                ChangeAll();
            }
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            Undo.RecordObject(this, $"CanvasFollowDevice {name}");
#endif
            ChangeAll();
        }

        private void OnDrawGizmos()
        {
            if (cam)
            {
                if (Math.Abs(cam.aspect - aspect) > 0.01f)
                {
                    ChangeAll();
                }
            }
            else
            {
                ChangeAll();
            }
        }

        private void OnGUI()
        {
            if (cam)
            {
                if (Math.Abs(cam.aspect - aspect) > 0.01f)
                {
                    ChangeAll();
                }
            }
            else
            {
                ChangeAll();
            }
        }
#endif
        [ContextMenu("Fix cam zide follow screen")]
        private void FixCamSizeFollowScreen()
        {
            if ((this && !enabled) || !cam)
                return;

            if (IsInvert)
            {
                AspectCam = 1 / cam.aspect;
            }
            else
            {
                AspectCam = cam.aspect;
            }

            SetValue();

            for (int i = 0; i < Resolutions.Count; i++)
            {
                if (Mathf.Abs(AspectCam - Resolutions[i].AspectCam) < 0.05f)
                {
                    CurrentName = Resolutions[i].Name;

                    if (cam && changeCamSize)
                    {
                        if (cam.orthographic)
                        {
                            if (IsInvert)
                            {
                                cam.orthographicSize = Resolutions[i].CamSize;
                            }
                            else
                            {
                                cam.orthographicSize = Resolutions[i].CamSizeLanspace;
                            }
                        }
                        else
                        {
                            if (IsInvert)
                            {
                                cam.fieldOfView = Resolutions[i].PerspectiveSize;
                            }
                            else
                            {
                                cam.fieldOfView = Resolutions[i].PerspectiveSizeLanspace;
                            }
                        }
                    }

                    if (changeCanvasScaler && scaler)
                    {
                        if (IsInvert)
                        {
                            scaler.matchWidthOrHeight = Resolutions[i].Match;
                        }
                        else
                        {
                            scaler.matchWidthOrHeight = Resolutions[i].MatchLandscape;
                        }
                    }

                    break;
                }
                else
                {
                    int maxIndex = Mathf.Clamp(i + 1, 0, Resolutions.Count - 1);
                    float t = 0.5f;
                    bool change = false;
                    if (AspectCam > Resolutions[i].AspectCam && AspectCam < Resolutions[maxIndex].AspectCam)
                    {
                        t = Mathf.InverseLerp(Resolutions[i].AspectCam, Resolutions[maxIndex].AspectCam,
                            AspectCam);
                        change = true;
                    }

                    if (AspectCam > Resolutions[Resolutions.Count - 1].AspectCam)
                    {
                        if (i > 0)
                        {
                            t = Mathf.InverseLerp(Resolutions[i - 1].AspectCam, Resolutions[maxIndex].AspectCam,
                                AspectCam);
                            change = true;
                        }
                    }

                    if (!change)
                    {
                        continue;
                    }
                    else
                    {
                        var index = i + 1;
                        if (index > maxIndex)
                        {
                            index = maxIndex;
                        }
                        CurrentName = Resolutions[index].Name;
                    }

                    if (cam && changeCamSize)
                    {
                        if (cam.orthographic)
                        {
                            if (IsInvert)
                            {
                                cam.orthographicSize = Mathf.LerpUnclamped(Resolutions[i].CamSize,
                                    Resolutions[maxIndex].CamSize, t);
                            }
                            else
                            {
                                cam.orthographicSize = Mathf.LerpUnclamped(Resolutions[i].CamSizeLanspace,
                                    Resolutions[maxIndex].CamSizeLanspace, t);
                            }
                        }
                        else
                        {
                            if (IsInvert)
                            {
                                cam.fieldOfView = Mathf.LerpUnclamped(Resolutions[i].PerspectiveSize,
                                    Resolutions[maxIndex].PerspectiveSize, t);
                            }
                            else
                            {
                                cam.fieldOfView = Mathf.LerpUnclamped(Resolutions[i].PerspectiveSizeLanspace,
                                    Resolutions[maxIndex].PerspectiveSizeLanspace, t);
                            }
                        }
                    }

                    if (changeCanvasScaler && scaler)
                    {
                        if (IsInvert)
                        {
                            scaler.matchWidthOrHeight = Mathf.Lerp(Resolutions[i].Match,
                                Resolutions[maxIndex].Match, t);
                        }
                        else
                        {
                            scaler.matchWidthOrHeight = Mathf.Lerp(Resolutions[i].MatchLandscape,
                                Resolutions[maxIndex].MatchLandscape, t);
                        }
                    }
                }
            }
        }

        public bool CanUpdate => gameObject.activeInHierarchy;

        public void OnUpdate()
        {
            ChangeAll();
        }

        public void SetValue()
        {
            if (!changeValueByAuto)
            {
                return;
            }

            Resolutions[0].SetFalseAuto();
            Resolutions[Resolutions.Count - 1].SetFalseAuto();

            for (int i = 1; i < Resolutions.Count; i++)
            {
                var current = Resolutions[i];
                if (current.AutoCamSize)
                {
                    var A = Resolutions.FindLast(infor => !infor.AutoCamSize && infor.AspectCam < current.AspectCam);
                    var B = Resolutions.Find(infor => !infor.AutoCamSize && infor.AspectCam > current.AspectCam);
                    var T = Mathf.InverseLerp(A.AspectCam, B.AspectCam, current.AspectCam);
                    current.CamSize = Mathf.LerpUnclamped(A.CamSize, B.CamSize, T);
                }

                if (current.AutoCamSizeLanspace)
                {
                    var A = Resolutions.FindLast(infor =>
                        !infor.AutoCamSizeLanspace && infor.AspectCam < current.AspectCam);
                    var B = Resolutions.Find(infor =>
                        !infor.AutoCamSizeLanspace && infor.AspectCam > current.AspectCam);
                    var T = Mathf.InverseLerp(A.AspectCam, B.AspectCam, current.AspectCam);
                    current.CamSizeLanspace = Mathf.LerpUnclamped(A.CamSizeLanspace, B.CamSizeLanspace, T);
                }

                if (current.AutoPerspectiveSize)
                {
                    var A = Resolutions.FindLast(infor =>
                        !infor.AutoPerspectiveSize && infor.AspectCam < current.AspectCam);
                    var B = Resolutions.Find(infor =>
                        !infor.AutoPerspectiveSize && infor.AspectCam > current.AspectCam);
                    var T = Mathf.InverseLerp(A.AspectCam, B.AspectCam, current.AspectCam);
                    current.PerspectiveSize = Mathf.LerpUnclamped(A.PerspectiveSize, B.PerspectiveSize, T);
                }

                if (current.AutoPerspectiveSizeLanspace)
                {
                    var A = Resolutions.FindLast(infor =>
                        !infor.AutoPerspectiveSizeLanspace && infor.AspectCam < current.AspectCam);
                    var B = Resolutions.Find(infor =>
                        !infor.AutoPerspectiveSizeLanspace && infor.AspectCam > current.AspectCam);
                    var T = Mathf.InverseLerp(A.AspectCam, B.AspectCam, current.AspectCam);
                    current.PerspectiveSizeLanspace =
                        Mathf.LerpUnclamped(A.PerspectiveSizeLanspace, B.PerspectiveSizeLanspace, T);
                }

                if (current.AutoMatch)
                {
                    var A = Resolutions.FindLast(infor => !infor.AutoMatch && infor.AspectCam < current.AspectCam);
                    var B = Resolutions.Find(infor => !infor.AutoMatch && infor.AspectCam > current.AspectCam);
                    var T = Mathf.InverseLerp(A.AspectCam, B.AspectCam, current.AspectCam);
                    current.Match = Mathf.LerpUnclamped(A.Match, B.Match, T);
                }

                if (current.AutoMatchLandscape)
                {
                    var A = Resolutions.FindLast(infor =>
                        !infor.AutoMatchLandscape && infor.AspectCam < current.AspectCam);
                    var B = Resolutions.Find(infor => !infor.AutoMatchLandscape && infor.AspectCam > current.AspectCam);
                    var T = Mathf.InverseLerp(A.AspectCam, B.AspectCam, current.AspectCam);
                    current.MatchLandscape = Mathf.LerpUnclamped(A.MatchLandscape, B.MatchLandscape, T);
                }
            }
        }
    }

    [Serializable]
    public class ResolutionInfor
    {
        public string Name;
        public float AspectCam;

        /// 2D size
        [Header("2D size")] public float CamSize = 9.6f;

        public bool AutoCamSize;
        public float CamSizeLanspace = 5.9f;
        public bool AutoCamSizeLanspace;

        /// 3D size
        [Header("3D size")] public float PerspectiveSize = 60;

        public bool AutoPerspectiveSize;
        public float PerspectiveSizeLanspace = 60;
        public bool AutoPerspectiveSizeLanspace;

        /// canvas scaler
        [Header("Match scaler")] [Range(0f, 1f)]
        public float Match;

        public bool AutoMatch;
        [Range(0f, 1f)] public float MatchLandscape = 1f;
        public bool AutoMatchLandscape;

        public void SetFalseAuto()
        {
            AutoCamSize = false;
            AutoCamSizeLanspace = false;
            AutoPerspectiveSize = false;
            AutoPerspectiveSizeLanspace = false;
            AutoMatch = false;
            AutoMatchLandscape = false;
        }
    }
}