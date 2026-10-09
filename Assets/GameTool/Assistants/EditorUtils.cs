#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameTool.Assistants
{
    public static class EditorUtils
    {
        public static void UpdateEnumInFile(string sysFilePath, string enumName, List<string> newEnumValues, Type typeEnum)
        {
            try
            {
                // Đọc tệp .cs hiện có vào một mảng dòng
                List<string> lines = File.ReadAllLines(sysFilePath).ToList();

                // Tìm dòng chứa khai báo enum
                int enumStartLine = -1;
                int enumEndLine = -1;
                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].Contains("enum " + enumName))
                    {
                        enumStartLine = i;
                        break;
                    }
                }

                if (enumStartLine == -1)
                {
                    // Không tìm thấy khai báo enum, nên không thể sửa đổi
                    Debug.LogError("Không tìm thấy khai báo enum.");
                    return;
                }

                // Tìm dòng kết thúc của enum
                int braceCount = 0;
                for (int i = enumStartLine; i < lines.Count; i++)
                {
                    if (lines[i].Contains("{"))
                    {
                        braceCount++;
                    }

                    if (lines[i].Contains("}"))
                    {
                        braceCount--;
                        if (braceCount == 0)
                        {
                            enumEndLine = i;
                            break;
                        }
                    }
                }

                if (enumEndLine == -1)
                {
                    // Không tìm thấy dòng kết thúc của enum
                    Debug.LogError("Không tìm thấy dòng kết thúc của enum.");
                    return;
                }

                var enumValues = typeEnum.GetEnumNames().ToList();
                for (int i = 0; i < newEnumValues.Count; i++)
                {
                    if (enumValues.Contains(newEnumValues[i]))
                    {
                        continue;
                    }

                    lines.Insert(enumEndLine, newEnumValues[i] + ",");
                    enumEndLine++;
                }

                // Ghi lại tệp .cs
                File.WriteAllLines(sysFilePath, lines);
            }
            catch (Exception ex)
            {
                Debug.LogError("Lỗi: " + ex.Message);
            }
        }

        public static string GetSystemFilePath(string unityFilePath)
        {
            return Application.dataPath + unityFilePath.Remove(0, "Assets".Length);
        }

        public static string GetUnityFilePath(string systemFilePath)
        {
            // Đảm bảo đường dẫn sử dụng dấu sổ xuôi (/)
            string normalizedPath = systemFilePath.Replace("\\", "/");

            // Thư mục gốc của dự án Unity
            string projectRoot = Application.dataPath + "/";

            // Loại bỏ phần đường dẫn đến thư mục gốc của dự án
            if (normalizedPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
            {
                normalizedPath = normalizedPath.Substring(projectRoot.Length);
            }

            return $"Assets/{normalizedPath}";
        }

        /// Unity Path hoặc empty string
        public static string FindFilePath(string fileName, string extension)
        {
            string[] paths = AssetDatabase.FindAssets(fileName);

            foreach (string guid in paths)
            {
                var scriptPath = AssetDatabase.GUIDToAssetPath(guid);
                string scriptName = Path.GetFileName(scriptPath);

                if (scriptName == $"{fileName}.{extension}")
                {
                    return scriptPath;
                }
            }

            Debug.LogWarning("Không tìm thấy tệp " + fileName);
            return "";
        }

        public static void ReImportAsset(string unityPath)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(unityPath);
            AssetDatabase.Refresh();
        }

        public static void ReImportAssets(List<string> unityPaths)
        {
            AssetDatabase.SaveAssets();
            foreach (string path in unityPaths)
            {
                AssetDatabase.ImportAsset(path);
            }
            AssetDatabase.Refresh();
        }
        
        public static void ChangeDefineSymbols(string define, bool isAdd = true)
        {
            BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);

            if (isAdd)
            {
                if (defines.Contains(define))
                {
                    return;
                }

                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, (defines + ";" + define));
            }
            else
            {
                if (defines.Contains(define))
                {
                    string newDefine = defines.Replace(define, "");
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, newDefine);
                }
            }
        }
    }
}

#endif