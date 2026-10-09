using System.Linq;
using GameTool.Assistants;
using GameTool.Audio.Scripts;
using GameToolSample.Audio;
using UnityEditor;
#if UNITY_2021_1_OR_NEWER
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#endif
using UnityEngine;

namespace GameTool.Audio.Editor
{
    public class AudioSetupEditorWindow : EditorWindow
    {
        private AudioTable _table;
        private Vector2 _scroll;
        private Vector2 _scroll2;
        private bool tab1 = true;
        private int currentPageSound = 1;
        private int sizePage = 25;

#if UNITY_2021_1_OR_NEWER
        private VisualElement _visualElement;
        private IntegerField _arraySizeSound;
#else
        private int arraySizeSound;
#endif

        [MenuItem("GameTool/Audio/Audio Manager")]
        private static void ShowWindow()
        {
            var window = GetWindow<AudioSetupEditorWindow>();
            window.titleContent = new GUIContent("Audio Manager");
            window.Show();
        }

        private void OnEnable()
        {
            _table = Resources.Load<AudioTable>("AudioTable");
            #if !UNITY_2021_1_OR_NEWER
            arraySizeSound = _table.soundTracksSerializers.Count;
            #endif
            Undo.RecordObject(_table, "AudioTable");
        }

#if UNITY_2021_1_OR_NEWER
        private void CreateGUI()
        {
            minSize = new Vector2(800, 400);
            _visualElement = rootVisualElement;
            SerializedObject tableSerializedObject = new SerializedObject(_table);

            Button saveButton = new Button(Save);
            saveButton.text = "SAVE";
            _visualElement.Add(saveButton);


            Button improveButton = new Button(ImproveClick);
            improveButton.text = "IMPROVE AUDIO";
            _visualElement.Add(improveButton);

            VisualElement horGroup = new();
            horGroup.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
            horGroup.style.justifyContent = new StyleEnum<Justify>(Justify.Center);
            horGroup.style.alignItems = new StyleEnum<Align>(Align.Center);
            horGroup.style.minHeight = new StyleLength(new Length(21));

            if (tab1)
            {
                Label toggle = new Label();
                toggle.text = "Music";

                Button soundBtn = new Button(() =>
                {
                    tab1 = false;
                    _visualElement.Clear();
                    CreateGUI();
                });
                soundBtn.text = "Sound";
                toggle.style.width = new StyleLength(new Length(49, LengthUnit.Percent));
                toggle.style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter);
                soundBtn.style.width = new StyleLength(new Length(49, LengthUnit.Percent));
                horGroup.Add(toggle);
                horGroup.Add(soundBtn);
            }
            else
            {
                Button musicBtn = new Button(() =>
                {
                    tab1 = true;
                    _visualElement.Clear();
                    CreateGUI();
                });
                musicBtn.text = "Music";

                Label toggle = new Label();
                toggle.text = "Sound";
                
                
                toggle.style.width = new StyleLength(new Length(49, LengthUnit.Percent));
                toggle.style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter);
                musicBtn.style.width = new StyleLength(new Length(49, LengthUnit.Percent));

                horGroup.Add(musicBtn);
                horGroup.Add(toggle);
            }

            _visualElement.Add(horGroup);

            if (tab1)
            {
                ShowTab1(tableSerializedObject);
            }
            else
            {
                ShowTab2(tableSerializedObject);
            }
        }

        private void ShowTab1(SerializedObject tableSerializedObject)
        {
            ScrollView scroller = new ScrollView();
            SerializedProperty property1 = tableSerializedObject.FindProperty("musicTracksSerializers");
            PropertyField propertyField1 = new PropertyField();
            propertyField1.BindProperty(property1);
            scroller.Add(propertyField1);
            _visualElement.Add(scroller);
        }

        private void ShowTab2(SerializedObject tableSerializedObject)
        {
            VisualElement horGroup = new();
            horGroup.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
            horGroup.style.justifyContent = new StyleEnum<Justify>(Justify.FlexEnd);
            horGroup.style.alignItems = new StyleEnum<Align>(Align.Stretch);
            horGroup.style.minHeight = new StyleLength(new Length(21));
            _arraySizeSound = new IntegerField("Array size: ");
            _arraySizeSound.value = _table.soundTracksSerializers.Count;
            horGroup.Add(_arraySizeSound);

            Button enterBtn = new Button(() =>
            {
                var arraySizeSound = _arraySizeSound.value;
                var countTemp = _table.soundTracksSerializers.Count;
                if (arraySizeSound > countTemp)
                {
                    for (int i = 0; i < arraySizeSound - countTemp; i++)
                    {
                        _table.soundTracksSerializers.Add(new SerializerSound());
                    }
                }
                else if (arraySizeSound < countTemp)
                {
                    for (int i = 0; i < countTemp - arraySizeSound; i++)
                    {
                        _table.soundTracksSerializers.RemoveAt(_table.soundTracksSerializers.Count - 1);
                    }
                }

                EditorUtility.SetDirty(_table);
                CustomRepaint();
            })
            {
                text = "Enter"
            };
            horGroup.Add(enterBtn);

            Button cancelBtn = new Button(() =>
            {
                _arraySizeSound.value = _table.soundTracksSerializers.Count;

                EditorUtility.SetDirty(_table);
            })
            {
                text = "Cancel"
            };
            horGroup.Add(cancelBtn);
            _visualElement.Add(horGroup);


            ScrollView scroller = new ScrollView();
            SerializedProperty property2 = tableSerializedObject.FindProperty("soundTracksSerializers");
            var count = _table.soundTracksSerializers.Count;

            var pageCount = Mathf.Clamp((count) / sizePage + 1, 1, int.MaxValue);
            if (currentPageSound > pageCount)
            {
                currentPageSound = pageCount;
            }

            // Hiển thị các element của 1 trang
            int countEleInPage;
            if (currentPageSound == pageCount)
            {
                countEleInPage = count % sizePage;
            }
            else
            {
                countEleInPage = sizePage;
            }

            for (int i = 0; i < countEleInPage; i++)
            {
                var index = i + (currentPageSound - 1) * sizePage;
                var ele = property2.GetArrayElementAtIndex(index);
                horGroup = new()
                {
                    style =
                    {
                        flexDirection = new StyleEnum<FlexDirection>(FlexDirection.RowReverse),
                        justifyContent = new StyleEnum<Justify>(Justify.FlexStart),
                        alignItems = new StyleEnum<Align>(Align.Stretch)
                    }
                };

                horGroup.Add(new Button(() =>
                {
                    _table.soundTracksSerializers.RemoveAt(index);
                    _arraySizeSound.value = _table.soundTracksSerializers.Count;
                    CustomRepaint();
                })
                {
                    text = "Remove"
                });
                var propField = new PropertyField()
                {
                    style = { width = new StyleLength(new Length(85, LengthUnit.Percent)) }
                };
                propField.label = _table.soundTracksSerializers[index].key;
                propField.BindProperty(ele);
                horGroup.Add(propField);
                Label label = new Label(index.ToString())
                {
                    style = { width = 50 }
                };
                horGroup.Add(label);

                scroller.Add(horGroup);
            }

            _visualElement.Add(scroller);


            _visualElement.Add(new Button(() =>
            {
                _table.soundTracksSerializers.Add(new SerializerSound());
                _arraySizeSound.value = _table.soundTracksSerializers.Count;
                tableSerializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(_table);
                CustomRepaint();
            })
            {
                text = "Add"
            });

            _visualElement.Add(new VisualElement() { style = { height = new StyleLength(10) } });


            if (pageCount >= 2)
            {
                horGroup = new VisualElement
                {
                    style =
                    {
                        flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                        justifyContent = new StyleEnum<Justify>(Justify.Center),
                        alignItems = new StyleEnum<Align>(Align.Stretch)
                    }
                };

                for (int i = 1; i <= pageCount; i++)
                {
                    var i1 = i;
                    horGroup.Add(new Button(() =>
                    {
                        currentPageSound = i1; 
                        CustomRepaint();
                    }) { text = i1.ToString() });
                }
                
                _visualElement.Add(horGroup);
            }
        }

        private void CustomRepaint()
        {
            _visualElement.Clear();
            CreateGUI();
        }

#else

        private void OnGUI()
        {
            minSize = new Vector2(800, 400);
            SerializedObject table = new SerializedObject(_table);

            if (GUILayout.Button("SAVE"))
            {
                Save();
            }

            if (GUILayout.Button("IMPROVE AUDIO"))
            {
                ImproveClick();
            }

            EditorGUILayout.BeginHorizontal();

            if (tab1)
            {
                GUILayout.Toggle(true, "Music", "Button");

                if (GUILayout.Button("Sound"))
                {
                    tab1 = false;
                }
            }
            else
            {
                if (GUILayout.Button("Music"))
                {
                    tab1 = true;
                }

                GUILayout.Toggle(true, "Sound", "Button");
            }

            EditorGUILayout.EndHorizontal();

            if (tab1)
            {
                _scroll = GUILayout.BeginScrollView(_scroll);

                SerializedProperty property1 = table.FindProperty("musicTracksSerializers");
                EditorGUILayout.PropertyField(property1, true);

                GUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.Space(10);

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField("Array size: ");
                arraySizeSound = EditorGUILayout.IntField(arraySizeSound);

                if (arraySizeSound != _table.soundTracksSerializers.Count)
                {
                    if (GUILayout.Button("Enter", GUILayout.Width(70)))
                    {
                        var countTemp = _table.soundTracksSerializers.Count;
                        if (arraySizeSound > countTemp)
                        {
                            for (int i = 0; i < arraySizeSound - countTemp; i++)
                            {
                                _table.soundTracksSerializers.Add(new SerializerSound());
                            }
                        }
                        else if (arraySizeSound < countTemp)
                        {
                            for (int i = 0; i < countTemp - arraySizeSound; i++)
                            {
                                _table.soundTracksSerializers.RemoveAt(_table.soundTracksSerializers.Count - 1);
                            }
                        }
                        EditorUtility.SetDirty(_table);
                    }
                    if (GUILayout.Button("Cancel", GUILayout.Width(70)))
                    {
                        arraySizeSound = _table.soundTracksSerializers.Count;
                    }
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(0);

                _scroll2 = GUILayout.BeginScrollView(_scroll2);

                SerializedProperty property2 = table.FindProperty("soundTracksSerializers");

                var count = _table.soundTracksSerializers.Count;

                var pageCount = Mathf.Clamp((count) / sizePage + 1, 1, int.MaxValue);
                if (currentPageSound > pageCount)
                {
                    currentPageSound = pageCount;
                }

                // Hiển thị các element của 1 trang
                int countEleInPage;
                if (currentPageSound == pageCount)
                {
                    countEleInPage = count % sizePage;
                }
                else
                {
                    countEleInPage = sizePage;
                }
                for (int i = 0; i < countEleInPage; i++)
                {
                    var index = i + (currentPageSound - 1) * sizePage;
                    var ele = property2.GetArrayElementAtIndex(index);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(index.ToString(), GUILayout.Width(50));
                    EditorGUILayout.PropertyField(ele, new GUIContent(_table.soundTracksSerializers[index].key), true);
                    if (GUILayout.Button("Remove", GUILayout.Width(70)))
                    {
                        _table.soundTracksSerializers.RemoveAt(index);
                        arraySizeSound = _table.soundTracksSerializers.Count;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                if (GUILayout.Button("Add", GUILayout.Width(70)))
                {
                    _table.soundTracksSerializers.Add(new SerializerSound());
                    arraySizeSound = _table.soundTracksSerializers.Count;
                    EditorUtility.SetDirty(_table);
                }

                GUILayout.EndScrollView();
                EditorGUILayout.Space(10);

                // Hiển thị nút chọn trang
                if (pageCount >= 2)
                {
                    EditorGUILayout.BeginHorizontal();
                    for (int i = 1; i <= pageCount; i++)
                    {
                        if (GUILayout.Button(i.ToString()))
                        {
                            currentPageSound = i;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            table.ApplyModifiedProperties();
        }
#endif

        private void ImproveClick()
        {
            _table.Improve();
        }

        private void Save()
        {
            _table.OnValidate();
            UpdateScripts(_table);

            EditorUtility.SetDirty(_table);
            AssetDatabase.SaveAssetIfDirty(_table);
        }

        public static void UpdateScripts(AudioTable instance)
        {
            string systemFilePath = EditorUtils.GetSystemFilePath(EditorUtils.FindFilePath("eAudioName", "cs"));
            EditorUtils.UpdateEnumInFile(systemFilePath, "eSoundName", instance.soundTracksSerializers.Select(sound => sound.key).ToList(),
                typeof(eSoundName));
            EditorUtils.UpdateEnumInFile(systemFilePath, "eMusicName", instance.musicTracksSerializers.Select(music => music.key).ToList(),
                typeof(eMusicName));

            EditorUtils.ReImportAsset(EditorUtils.GetUnityFilePath(systemFilePath));
        }
    }
}