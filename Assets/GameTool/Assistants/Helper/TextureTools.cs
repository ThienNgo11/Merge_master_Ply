using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GameTool.Assistants.Helper
{
#if UNITY_EDITOR
    public class TextureTools : EditorWindow
    {
        public string ChooseePath(string path)
        {
            return EditorUtility.OpenFolderPanel("Chosee Path", path, "*.*");
        }
    }

    public enum Options
    {
        AutoResize = 0,
        AutoFillSprite = 1
    }

    public enum RectOptions
    {
        Center = 0,
        BottomRight = 1,
        TopRight = 2,
        BottomLeft = 3,
        TopLeft = 4,
        Custom = 9
    }

    public class ResizeTexture2D : TextureTools
    {
        Vector2 scrollPositionTexture = Vector2.zero;
        private Options optionsChoose;
        private RectOptions rectOptions;
        List<string> listString = new List<string>();
        List<Texture2D> listTexture2D = new List<Texture2D>();
        private string stringFileInfo = "";
        private int widthCrop;
        private int heightCrop;
        private int widthResize = 1024, heightResize = 1024;
        private List<int> widthResizeAutoList = new List<int>(1000);
        private List<int> heightResizeAutoList = new List<int>(1000);
        private Rect RectInput;
        private int xMod, yMod, defaultView;

        [MenuItem("MyTools/Texture2D _F1", priority = 3)]
        static void Init()
        {
            GetWindow<ResizeTexture2D>();
        }


        void OnGUI()
        {
            EditorGUILayout.LabelField("Find/Edit Texture");
            optionsChoose = (Options)EditorGUILayout.EnumPopup("Choose type: ", optionsChoose);
            ChooseOptions(optionsChoose);

            scrollPositionTexture = EditorGUILayout.BeginScrollView(scrollPositionTexture,
                GUILayout.Width(position.width), GUILayout.Height(600));
            GUILayout.TextArea(stringFileInfo);

            EditorGUILayout.EndScrollView();
        }

        void Review()
        {
            listTexture2D.Clear();
            listString.Clear();
            var selectedObjects = Selection.GetFiltered(typeof(Texture2D), SelectionMode.DeepAssets);
            if (selectedObjects != null)
            {
                if ((widthResize == 0 || heightResize == 0))
                {
                    return;
                }


                if (widthResizeAutoList.Count <= 10)
                {
                    widthResizeAutoList.Clear();

                    for (int i = 0; i < 1000; i++)
                    {
                        widthResizeAutoList.Add(0);
                    }
                }

                if (heightResizeAutoList.Count <= 10)
                {
                    heightResizeAutoList.Clear();
                    for (int i = 0; i < 1000; i++)
                    {
                        heightResizeAutoList.Add(0);
                    }
                }

                for (int index = 0; index < selectedObjects.Length; index++)
                {
                    Texture2D newTexture2D = selectedObjects[index] as Texture2D;
                    listString.Add(AssetDatabase.GetAssetPath(selectedObjects[index]));
                    if (newTexture2D != null)
                    {
                        if (optionsChoose == Options.AutoResize)
                        {
                            byte[] bytes = File.ReadAllBytes(AssetDatabase.GetAssetPath(selectedObjects[index]));
                            var newTexture = new Texture2D(newTexture2D.width, newTexture2D.height,
                                TextureFormat.RGBA32,
                                false);
                            newTexture.LoadImage(bytes);
                            heightResize = newTexture2D.height;
                            widthResize = newTexture2D.width;

                            if (newTexture2D.height % 4 != 0)
                            {
                                if (newTexture2D.height % 4 <= 2)
                                    heightResizeAutoList[index] = (newTexture2D.height / 4 * 4);
                                else
                                    heightResizeAutoList[index] = ((newTexture2D.height / 4 * 4) + 4);
                            }
                            else
                                heightResizeAutoList[index] = newTexture2D.height;


                            if (newTexture2D.width % 4 != 0)
                            {
                                if (newTexture2D.width % 4 <= 2)
                                    widthResizeAutoList[index] = (newTexture2D.width / 4 * 4);
                                else
                                    widthResizeAutoList[index] = (newTexture2D.width / 4 * 4) + 4;
                            }
                            else
                                widthResizeAutoList[index] = newTexture2D.width;

                            newTexture.name = newTexture2D.name;
                            newTexture.Apply();
                            listTexture2D.Add(newTexture);
                        }
                    }
                }

                LoadInfoFile();
            }
        }

        public void ChooseOptions(Options options)
        {
            switch (options)
            {
                case Options.AutoFillSprite:

                    if (Selection.gameObjects.Length > 0)
                    {
                        foreach (GameObject obj in Selection.gameObjects)
                        {
                            EditorGUILayout.TextArea(obj.name);
                        }
                    }

                    if (GUILayout.Button("Auto Fill Missing Sprite"))
                    {
                        AutoFillSprite();
                    }

                    break;

                case Options.AutoResize:
                    EditorGUILayout.TextArea("Width:" + widthResize);
                    EditorGUILayout.TextArea("Height:" + heightResize);
                    EditorGUILayout.Space();
                    if (GUILayout.Button("Review"))
                    {
                        Review();
                    }

                    if (GUILayout.Button("Auto Resize Texture2D"))
                    {
                        AutoResizeTexture();
                    }

                    break;
            }
        }

        private void AutoResizeTexture()
        {
            for (int index = 0; index < listTexture2D.Count; index++)
            {
                TextureScale.Bilinear(listTexture2D[index], widthResizeAutoList[index], heightResizeAutoList[index]);
                byte[] bytes = listTexture2D[index].EncodeToPNG();
                File.WriteAllBytes(listString[index].Replace(Application.dataPath, "Assets"), bytes);
            }

            AssetDatabase.Refresh();
        }

        public void CropTexture(RectOptions rectOptionsArg)
        {
            for (int i = 0; i < listTexture2D.Count; i++)
            {
                Debug.Log(listString[i].Replace(Application.dataPath, "Assets"));
                RectInput.width = widthCrop;
                RectInput.height = heightCrop;

                listTexture2D[i] = CropWithRect(rectOptionsArg, listTexture2D[i], RectInput, xMod, yMod);
                byte[] bytes = listTexture2D[i].EncodeToPNG();
                File.WriteAllBytes(listString[i].Replace(Application.dataPath, "Assets"), bytes);
            }

            AssetDatabase.Refresh();
        }

        public void AutoFillSprite()
        {
            GameObject selectedObject = Selection.activeGameObject;
            Image[] images = selectedObject.GetComponentsInChildren<Image>();
            foreach (Image image in images)
            {
                if (image.sprite != null) continue;


                //load with name
                string objectName = image.transform.name;
                Sprite sprite = Resources.Load<Sprite>(objectName);

                if (sprite != null)
                {
                    image.sprite = sprite;
                }
                else
                {
                    //load with folder
                    string tryObjectName = selectedObject.name + "_export/" + image.transform.name;
                    Sprite trySprite = Resources.Load<Sprite>(tryObjectName);

                    if (trySprite != null)
                    {
                        image.sprite = trySprite;
                    }
                    else
                    {
                        Debug.LogError("Can't find Sprite: " + objectName);
                    }
                }
            }


            Debug.LogError("AutoFillSprite completd");
        }

        private static Texture2D CropWithRect(RectOptions rectOptions, Texture2D texture, Rect r, int xMod, int yMod)
        {
            if (r.height < 0 || r.width < 0)
            {
                return texture;
            }

            Texture2D result = new Texture2D((int)r.width, (int)r.height);
            if (r.width != 0 && r.height != 0)
            {
                float xRect = r.x;
                float yRect = r.y;
                float widthRect = r.width;
                float heightRect = r.height;

                switch (rectOptions)
                {
                    case RectOptions.Center:
                        xRect = (texture.width - r.width) / 2;
                        yRect = (texture.height - r.height) / 2;
                        break;

                    case RectOptions.BottomRight:
                        xRect = texture.width - r.width;
                        break;

                    case RectOptions.BottomLeft:
                        break;

                    case RectOptions.TopLeft:
                        yRect = texture.height - r.height;
                        break;

                    case RectOptions.TopRight:
                        xRect = texture.width - r.width;
                        yRect = texture.height - r.height;
                        break;

                    case RectOptions.Custom:
                        float tempWidth = texture.width - r.width - xMod;
                        float tempHeight = texture.height - r.height - yMod;
                        xRect = tempWidth > texture.width ? 0 : tempWidth;
                        yRect = tempHeight > texture.height ? 0 : tempHeight;
                        break;
                }

                if (texture.width < r.x + r.width || texture.height < r.y + r.height || xRect > r.x + texture.width ||
                    yRect > r.y + texture.height ||
                    xRect < 0 || yRect < 0 || r.width < 0 || r.height < 0)
                {
                    //EditorUtility.DisplayDialog("Set value crop", "Set value crop (Width and Height > 0) less than origin texture size \n" + texture.name + " wrong size", "ReSet");
                    return texture;
                }

                result.SetPixels(texture.GetPixels(Mathf.FloorToInt(xRect), Mathf.FloorToInt(yRect),
                    Mathf.FloorToInt(widthRect),
                    Mathf.FloorToInt(heightRect)));
                result.Apply();
            }

            return result;
        }

        private void LoadInfoFile()
        {
            stringFileInfo = "Total files: " + listTexture2D.Count + "\n";
        }
    }


    public class TextureScale
    {
        public class ThreadData
        {
            public int start;
            public int end;

            public ThreadData(int s, int e)
            {
                start = s;
                end = e;
            }
        }

        private static Color[] texColors;
        private static Color[] newColors;
        private static int w;
        private static float ratioX;
        private static float ratioY;
        private static int w2;
        private static Mutex mutex;

        public static void Point(Texture2D tex, int newWidth, int newHeight)
        {
            ThreadedScale(tex, newWidth, newHeight);
        }

        public static void Bilinear(Texture2D tex, int newWidth, int newHeight)
        {
            ThreadedScale(tex, newWidth, newHeight);
        }

        private static void ThreadedScale(Texture2D tex, int newWidth, int newHeight)
        {
            Resize(tex, newWidth, newHeight);
            tex.Apply();

            texColors = null;
            newColors = null;
        }

        public static void Resize(Texture2D texture2D, int targetX, int targetY, bool mipmap = true,
            FilterMode filter = FilterMode.Bilinear)
        {
            //create a temporary RenderTexture with the target size
            RenderTexture rt = RenderTexture.GetTemporary(targetX, targetY, 0, RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Default);

            //set the active RenderTexture to the temporary texture so we can read from it
            RenderTexture.active = rt;

            //Copy the texture data on the GPU - this is where the magic happens [(;]
            Graphics.Blit(texture2D, rt);
            //resize the texture to the target values (this sets the pixel data as undefined)
#if UNITY_2021_1_OR_NEWER
            texture2D.Reinitialize(targetX, targetY, texture2D.format, mipmap);
#else
            texture2D.Resize(targetX, targetY, texture2D.format, mipmap);
#endif
            texture2D.filterMode = filter;

            try
            {
                //reads the pixel values from the temporary RenderTexture onto the resized texture
                texture2D.ReadPixels(new Rect(0.0f, 0.0f, targetX, targetY), 0, 0);
                //actually upload the changed pixels to the graphics card
                texture2D.Apply();
            }
            catch
            {
                Debug.LogError("Read/Write is not enabled on texture " + texture2D.name);
            }


            RenderTexture.ReleaseTemporary(rt);
        }

        public static void BilinearScale(System.Object obj)
        {
            ThreadData threadData = (ThreadData)obj;
            for (var y = threadData.start; y < threadData.end; y++)
            {
                int yFloor = (int)Mathf.Floor(y * ratioY);
                var y1 = yFloor * w;
                var y2 = (yFloor + 1) * w;
                var yw = y * w2;

                for (var x = 0; x < w2; x++)
                {
                    int xFloor = (int)Mathf.Floor(x * ratioX);
                    var xLerp = x * ratioX - xFloor;
                    newColors[yw + x] = ColorLerpUnclamped(
                        ColorLerpUnclamped(texColors[y1 + xFloor], texColors[y1 + xFloor + 1], xLerp),
                        ColorLerpUnclamped(texColors[y2 + xFloor], texColors[y2 + xFloor + 1], xLerp),
                        y * ratioY - yFloor);
                }
            }

            mutex.WaitOne();
            mutex.ReleaseMutex();
        }

        public static void PointScale(System.Object obj)
        {
            ThreadData threadData = (ThreadData)obj;
            for (var y = threadData.start; y < threadData.end; y++)
            {
                var thisY = (int)(ratioY * y) * w;
                var yw = y * w2;
                for (var x = 0; x < w2; x++)
                {
                    newColors[yw + x] = texColors[(int)(thisY + ratioX * x)];
                }
            }

            mutex.WaitOne();
            mutex.ReleaseMutex();
        }

        private static Color ColorLerpUnclamped(Color c1, Color c2, float value)
        {
            return new Color(c1.r + (c2.r - c1.r) * value,
                c1.g + (c2.g - c1.g) * value,
                c1.b + (c2.b - c1.b) * value,
                c1.a + (c2.a - c1.a) * value);
        }
    }

#endif
}