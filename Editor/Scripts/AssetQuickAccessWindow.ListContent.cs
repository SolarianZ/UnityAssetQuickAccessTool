using System;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Assertions;
using UObject = UnityEngine.Object;

namespace GBG.AssetQuickAccess.Editor
{
    public partial class AssetQuickAccessWindow
    {
        private const float AssetIconSize = 24f;
        private const float CategoryIconSize = 16f;
        private const float IconMargin = 4f;

        private ReorderableList _reorderableList;
        private Vector2 _assetListScrollPos;
        private GUIStyle _assetItemButtonStyle;
        private GUIStyle _tooltipsLabelStyle;

        private static readonly TimeSpan _doubleClickSpan = new TimeSpan(0, 0, 0, 0, 400);
        private DateTime _lastClickAssetTime;
        private UObject _lastClickedAsset;


        private void CreateListView()
        {
            bool reorderable = LocalCache.SelectedCategories == AssetCategory.None;
            _reorderableList = new ReorderableList(_filteredAssetHandles,
                typeof(AssetHandle), reorderable, false, false, false);
            _reorderableList.elementHeightCallback = _ => 26;
            _reorderableList.drawElementCallback = DrawAssetListItem;
            _reorderableList.onReorderCallback = OnReorderAssetList;
        }

        private void DrawListContent()
        {
            // Asset list scroll view
            using (EditorGUILayout.ScrollViewScope scrollView = new EditorGUILayout.ScrollViewScope(_assetListScrollPos))
            {
                _assetListScrollPos = scrollView.scrollPosition;
                _reorderableList.draggable = LocalCache.SelectedCategories == AssetCategory.None;
                _reorderableList.DoLayoutList();
            }
        }

        private void DrawAssetListItem(Rect rect, int index, bool isActive, bool isFocused)
        {
            AssetHandle assetHandle = _filteredAssetHandles[index];
            string categoryIconTooltip; // TODO: Tooltip
            Texture assetIconTex;
            Texture categoryIconTex;
            switch (assetHandle.Category)
            {
                case AssetCategory.ProjectAsset:
                    assetIconTex = TextureUtility.GetObjectIcon(assetHandle.Asset, null);
                    categoryIconTex = null;
                    categoryIconTooltip = null;
                    break;

                case AssetCategory.SceneObject:
                    assetIconTex = TextureUtility.GetObjectIcon(assetHandle.Asset, assetHandle.Scene);
                    categoryIconTex = TextureUtility.GetSceneObjectTexture(true);
                    categoryIconTooltip = "Scene Object";
                    break;

                case AssetCategory.ExternalFile:
                    string path = assetHandle.GetAssetPath();
                    assetIconTex = File.Exists(path) || Directory.Exists(path)
                        ? TextureUtility.GetExternalFileTexture(false)
                        : TextureUtility.GetWarningTexture();
                    categoryIconTex = TextureUtility.GetExternalFileTexture(true);
                    categoryIconTooltip = "External File of Folder";
                    break;

                case AssetCategory.Url:
                    string url = assetHandle.GetAssetPath();
                    assetIconTex = TextureUtility.GetUrlTexture();
                    categoryIconTex = assetIconTex;
                    categoryIconTooltip = "URL";
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(assetHandle.Category), assetHandle.Category, null);
            }

            // asset icon / label / category icon
            using (new GUILayout.HorizontalScope())
            {
                // asset icon
                Rect assetIconRect = new Rect
                {
                    x = rect.x,
                    y = rect.y + (rect.height - AssetIconSize) / 2f,
                    width = AssetIconSize,
                    height = AssetIconSize
                };
                GUI.DrawTexture(assetIconRect, assetIconTex);

                // asset button
                Rect assetButtonRect = new Rect
                {
                    x = rect.x + AssetIconSize,
                    y = rect.y,
                    width = rect.width - AssetIconSize,
                    height = rect.height
                };
                if (_assetItemButtonStyle == null)
                {
                    _assetItemButtonStyle = new GUIStyle(GUI.skin.button)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        richText = true,
                    };
                }

                if (GUI.Button(assetButtonRect, assetHandle.GetDisplayName(), _assetItemButtonStyle))
                {
                    if (Event.current.button == 0)
                    {
                        OnLeftClickAssetListItem(assetHandle);
                    }
                    else if (Event.current.button == 1)
                    {
                        OnRightClickAssetListItem(assetHandle);
                    }
                }

                // category icon
                if (categoryIconTex)
                {
                    Rect categoryIconRect = new Rect
                    {
                        x = rect.xMax - CategoryIconSize - IconMargin,
                        y = rect.y + (rect.height - CategoryIconSize) / 2f,
                        width = CategoryIconSize,
                        height = CategoryIconSize
                    };
                    GUI.DrawTexture(categoryIconRect, categoryIconTex);
                }
            }
        }

        private void OnReorderAssetList(ReorderableList list)
        {
            Assert.IsTrue(LocalCache.SelectedCategories == AssetCategory.None);

            LocalCache.AssetHandles.Clear();
            foreach (var handle in _filteredAssetHandles)
            {
                LocalCache.AssetHandles.Add(handle);
            }

            LocalCache.ForceSave();
        }

        private void OnLeftClickAssetListItem(AssetHandle handle)
        {
            EditorGUIUtility.PingObject(handle.Asset);

            var now = DateTime.Now;
            if (now - _lastClickAssetTime < _doubleClickSpan &&
                _lastClickedAsset == handle.Asset)
            {
                AssetDatabase.OpenAsset(handle.Asset);
            }

            _lastClickAssetTime = now;
            _lastClickedAsset = handle.Asset;
        }

        private void OnRightClickAssetListItem(AssetHandle assetHandle)
        {
            switch (assetHandle.Category)
            {
                case AssetCategory.ProjectAsset:
                    ShowProjectAssetContextMenu(assetHandle);
                    break;

                case AssetCategory.SceneObject:
                    ShowSceneObjectContextMenu(assetHandle);
                    break;

                case AssetCategory.ExternalFile:
                    // Bind(AssetHandle);
                    ShowExternalFileContextMenu(assetHandle);
                    break;

                case AssetCategory.Url:
                    ShowUrlContextMenu(assetHandle);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(assetHandle.Category), assetHandle.Category, null);
            }
        }

        private void ShowProjectAssetContextMenu(AssetHandle assetHandle)
        {
            Assert.IsTrue(assetHandle.Category == AssetCategory.ProjectAsset);

            GenericMenu genericMenu = new GenericMenu();
            if (assetHandle.Asset)
            {
                genericMenu.AddItem(new GUIContent("Open"), false, assetHandle.OpenAsset);
                genericMenu.AddItem(new GUIContent("Copy Path"), false, assetHandle.CopyPathToSystemBuffer);
                genericMenu.AddItem(new GUIContent("Copy Guid"), false, assetHandle.CopyGuidToSystemBuffer);
                genericMenu.AddItem(new GUIContent("Copy Type"), false, assetHandle.CopyTypeFullNameToSystemBuffer);
                genericMenu.AddItem(new GUIContent("Show in Folder"), false, assetHandle.ShowInFolder);
            }
            else
            {
                genericMenu.AddItem(new GUIContent("Copy Guid"), false, assetHandle.CopyGuidToSystemBuffer);
            }

            genericMenu.AddItem(new GUIContent("Remove"), false, () => RemoveAsset(assetHandle));
            genericMenu.ShowAsContext();
        }

        private void ShowSceneObjectContextMenu(AssetHandle assetHandle)
        {
            Assert.IsTrue(assetHandle.Category == AssetCategory.SceneObject);

            GenericMenu genericMenu = new GenericMenu();
            if (assetHandle.Asset)
            {
                genericMenu.AddItem(new GUIContent("Open"), false, assetHandle.OpenAsset);
                genericMenu.AddItem(new GUIContent("Copy Hierarchy Path"), false, assetHandle.CopyPathToSystemBuffer);
                genericMenu.AddItem(new GUIContent("Copy Type"), false, assetHandle.CopyTypeFullNameToSystemBuffer);
            }
            else if (assetHandle.Scene)
            {
                genericMenu.AddItem(new GUIContent("Open in Scene"), false, assetHandle.OpenAsset);
            }

            genericMenu.AddItem(new GUIContent("Remove"), false, () => RemoveAsset(assetHandle));
            genericMenu.ShowAsContext();
        }

        private void ShowExternalFileContextMenu(AssetHandle assetHandle)
        {
            Assert.IsTrue(assetHandle.Category == AssetCategory.ExternalFile);

            string path = assetHandle.GetAssetPath();
            GenericMenu genericMenu = new GenericMenu();
            if (File.Exists(path) || Directory.Exists(path))
            {
                genericMenu.AddItem(new GUIContent("Open"), false, assetHandle.OpenAsset);
                genericMenu.AddItem(new GUIContent("Copy Path"), false, assetHandle.CopyPathToSystemBuffer);
                genericMenu.AddItem(new GUIContent("Show in Folder"), false, assetHandle.ShowInFolder);
            }
            else
            {
                genericMenu.AddItem(new GUIContent("Copy Path"), false, assetHandle.CopyPathToSystemBuffer);
            }

            genericMenu.AddItem(new GUIContent("Remove"), false, () => RemoveAsset(assetHandle));
            genericMenu.ShowAsContext();
        }

        private void ShowUrlContextMenu(AssetHandle assetHandle)
        {
            Assert.IsTrue(assetHandle.Category == AssetCategory.Url);

            GenericMenu genericMenu = new GenericMenu();
            genericMenu.AddItem(new GUIContent("Open"), false, assetHandle.OpenAsset);
            genericMenu.AddItem(new GUIContent("Copy URL"), false, assetHandle.CopyPathToSystemBuffer);
            genericMenu.AddItem(new GUIContent("Remove"), false, () => RemoveAsset(assetHandle));
            genericMenu.ShowAsContext();
        }

        private void ProcessDragAndDropOut()
        {
            Event e = Event.current;
            switch (e.type)
            {
                case EventType.MouseDown:
                    Debug.LogError("MouseDown");
                    break;

                case EventType.MouseDrag:
                    Debug.LogError("MouseDrag");
                    // if (_mouseAction == MouseAction.Click)
                    // {
                    //     _mouseAction = MouseAction.Drag;
                    //     DragAndDrop.PrepareStartDrag();
                    //     if (_assetHandle.Asset)
                    //     {
                    //         DragAndDrop.SetGenericData(DragGenericData, DragGenericData);
                    //         DragAndDrop.objectReferences = new UObject[] { _assetHandle.Asset };
                    //         DragAndDrop.paths = new string[] { AssetDatabase.GetAssetPath(_assetHandle.Asset) };
                    //     }
                    //
                    //     DragAndDrop.StartDrag(null);
                    // }

                    break;

                // case EventType.DragUpdated:
                //     if (DragAndDrop.objectReferences.Length > 0 &&
                //         !DragGenericData.Equals(DragAndDrop.GetGenericData(DragGenericData)))
                //     {
                //         DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                //     }
                //
                //     break;

                default:
                    break;
            }
        }
    }
}