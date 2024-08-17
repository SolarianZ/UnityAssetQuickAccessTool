using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using UDebug = UnityEngine.Debug;
using UObject = UnityEngine.Object;

namespace GBG.AssetQuickAccess.Editor
{
    public partial class AssetQuickAccessWindow
    {
        private string _assetIdentifier;

        private GUIStyle _bottomTipsStyle;


        private void DrawBottomContents()
        {
            DrawBottomTips();
            DrawFindObjectContent();
        }

        private void DrawBottomTips()
        {
            if (_bottomTipsStyle == null)
            {
                _bottomTipsStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    richText = true,
                };
            }

            GUILayout.Label("Drag the asset here to add a new item.", _bottomTipsStyle);
        }

        private void DrawFindObjectContent()
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Path/Guid", GUILayout.Width(60));
            _assetIdentifier = EditorGUILayout.TextField(_assetIdentifier);
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_assetIdentifier));
            if (GUILayout.Button("Find", GUILayout.Width(60)))
            {
                FindObjectByPathOrGuid();
            }
            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();
        }

        private void FindObjectByPathOrGuid()
        {
            Assert.IsTrue(!string.IsNullOrEmpty(_assetIdentifier));

            string filePath = AssetDatabase.GUIDToAssetPath(_assetIdentifier);
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = _assetIdentifier;
                if (_assetIdentifier.EndsWith("/") || _assetIdentifier.EndsWith("\\"))
                {
                    filePath = _assetIdentifier.Remove(_assetIdentifier.Length - 1);
                    if (string.IsNullOrEmpty(filePath))
                    {
                        ShowNotification(new GUIContent($"Can not find asset with guid or path:\n{_assetIdentifier}"));
                        return;
                    }
                }
            }

            UObject asset = AssetDatabase.LoadAssetAtPath<UObject>(filePath);
            if (asset)
            {
                EditorGUIUtility.PingObject(asset);
                return;
            }

            // Try find in loaded scenes
            // GameObject.Find can not find disabled GameObject and will only find the first matched GameObject
            // Transform.Find can only find the first matched Transform, we check each name manually to find all matched items
            List<Transform> result = new List<Transform>();
            string[] names = _assetIdentifier.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                {
                    continue;
                }

                foreach (GameObject rootGo in scene.GetRootGameObjects())
                {
                    FindHierarchy(names, rootGo.transform, 0, result);
                }
            }

            if (result.Count > 0)
            {
                // Ping the first item and log others in Console (Unity doesn't support ping multiple objects)
                EditorGUIUtility.PingObject(result[0]);

                if (result.Count > 1)
                {
                    ShowNotification(new GUIContent("Found multiple matching Scene objects.\nPlease check the Console log."));
                    foreach (Transform target in result)
                    {
                        UDebug.Log($"<b>[Asset Quick Access]</b> Found '{_assetIdentifier}' in scene '{target.gameObject.scene.name}'.", target);
                    }
                }

                return;
            }

            // Not found
            ShowNotification(new GUIContent($"Can not find asset with guid or path:\n{_assetIdentifier}"));
        }

        private void ClearBottomTipsGuiCaches()
        {
            _bottomTipsStyle = null;
        }


        private static void FindHierarchy(string[] names, Transform node, int depth, List<Transform> result)
        {
            string targetName = names[depth];
            if (!node.name.Equals(targetName))
            {
                return;
            }

            if (depth == names.Length - 1)
            {
                result.Add(node);
                return;
            }

            for (int i = 0; i < node.childCount; i++)
            {
                Transform child = node.GetChild(i);
                FindHierarchy(names, child, depth + 1, result);
            }
        }
    }
}