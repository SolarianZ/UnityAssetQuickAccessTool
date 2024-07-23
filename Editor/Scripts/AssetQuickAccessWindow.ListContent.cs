using System;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using UObject = UnityEngine.Object;

namespace GBG.AssetQuickAccess.Editor
{
    public partial class AssetQuickAccessWindow
    {
        private const float AssetIconSize = 24f;
        private const float CategoryIconSize = 16f;
        private const float IconMargin = 4f;
        private const double DoubleClickInterval = 0.4f;

        private ReorderableList _assetList;
        private Vector2 _assetListScrollPos;
        private GUIStyle _assetItemStyle;
        private GUIStyle _assetCategoryTooltipStyle;

        private double _lastClickAssetTime;
        private string _lastClickedAsset;
        private MouseAction _mouseAction = MouseAction.None;


        private void CreateListView()
        {
            bool reorderable = LocalCache.SelectedCategories == AssetCategory.None;
            _assetList = new ReorderableList(_filteredAssetHandles, typeof(AssetHandle), reorderable, false, false, false);
            _assetList.headerHeight = 0;
            _assetList.elementHeightCallback = _ => 26;
            _assetList.drawElementCallback = DrawAssetListItem;
            _assetList.onReorderCallback = OnReorderAssetList;
        }

        private void DrawListContent()
        {
            // Asset list scroll view
            using (EditorGUILayout.ScrollViewScope scrollView = new EditorGUILayout.ScrollViewScope(_assetListScrollPos))
            {
                _assetListScrollPos = scrollView.scrollPosition;
                _assetList.draggable = LocalCache.SelectedCategories == AssetCategory.None;
                _assetList.DoLayoutList();
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
                    assetIconTex = TextureUtility.GetObjectIcon(assetHandle);
                    categoryIconTex = null;
                    categoryIconTooltip = null;
                    break;

                case AssetCategory.SceneObject:
                    assetIconTex = TextureUtility.GetObjectIcon(assetHandle);
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
                    // string url = assetHandle.GetAssetPath();
                    assetIconTex = TextureUtility.GetUrlTexture();
                    categoryIconTex = assetIconTex;
                    categoryIconTooltip = "URL";
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(assetHandle.Category), assetHandle.Category, null);
            }

            // Asset item
            using (new GUILayout.HorizontalScope())
            {
                bool mouseHover = rect.Contains(Event.current.mousePosition);
                if (mouseHover)
                {
                    assetHandle.Update();
                }


                #region Asset icon

                Rect assetIconRect = new Rect
                {
                    x = rect.x,
                    y = rect.y + (rect.height - AssetIconSize) / 2f,
                    width = AssetIconSize,
                    height = AssetIconSize
                };
                GUI.DrawTexture(assetIconRect, assetIconTex);

                #endregion


                #region Asset clickable label

                Rect assetButtonRect = new Rect
                {
                    x = rect.x + AssetIconSize,
                    y = rect.y,
                    width = rect.width - AssetIconSize,
                    height = rect.height
                };
                if (_assetItemStyle == null)
                {
                    _assetItemStyle = new GUIStyle(GUI.skin.button)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        richText = true,
                    };
                }

                ProcessAssetItemAction(assetButtonRect, assetHandle);

                if (mouseHover)
                {
                    GUI.Label(assetButtonRect, new GUIContent(assetHandle.GetDisplayName(), assetHandle.GetAssetPath()), _assetItemStyle);
                }
                else
                {
                    GUI.Label(assetButtonRect, assetHandle.GetDisplayName(), _assetItemStyle);
                }

                #endregion


                #region Category icon

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

                #endregion
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

        private void OnLeftClickAssetListItem(AssetHandle handle) { EditorGUIUtility.PingObject(handle.Asset); }

        private void OnLeftDoubleClickAssetListItem(AssetHandle handle) { AssetDatabase.OpenAsset(handle.Asset); }

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
                bool objectLost = false;
                string scenePath = AssetDatabase.GetAssetPath(assetHandle.Scene);
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene scene = SceneManager.GetSceneAt(i);
                    if (scene.isLoaded && scene.path == scenePath)
                    {
                        objectLost = true;
                        break;
                    }
                }

                if (!objectLost)
                {
                    genericMenu.AddItem(new GUIContent("Open in Scene"), false, assetHandle.OpenAsset);
                }
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

        private void ProcessAssetItemAction(Rect dragArea, AssetHandle assetHandle)
        {
            Event e = Event.current;
            if (!dragArea.Contains(e.mousePosition))
            {
                return;
            }

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0)
                    {
                        double currentTime = EditorApplication.timeSinceStartup;
                        if (currentTime - _lastClickAssetTime < DoubleClickInterval && assetHandle.Guid.Equals(_lastClickedAsset))
                        {
                            _mouseAction = MouseAction.DoubleClick;
                            _lastClickAssetTime = 0;
                        }
                        else
                        {
                            _mouseAction = MouseAction.Click;
                            _lastClickAssetTime = currentTime;
                            _lastClickedAsset = assetHandle.Guid;
                        }

                        e.Use();
                    }
                    else if (e.button == 1)
                    {
                        _mouseAction = MouseAction.ContextClick;
                        e.Use();
                    }

                    break;

                case EventType.MouseUp:
                    switch (_mouseAction)
                    {
                        case MouseAction.Click:
                            if (e.button == 0)
                            {
                                _mouseAction = MouseAction.None;
                                OnLeftClickAssetListItem(assetHandle);
                                e.Use();
                            }

                            break;

                        case MouseAction.DoubleClick:
                            if (e.button == 0)
                            {
                                _mouseAction = MouseAction.None;
                                OnLeftDoubleClickAssetListItem(assetHandle);
                                e.Use();
                            }

                            break;

                        case MouseAction.ContextClick:
                            if (e.button == 1)
                            {
                                _mouseAction = MouseAction.None;
                                OnRightClickAssetListItem(assetHandle);
                                e.Use();
                            }

                            break;

                        case MouseAction.None:
                        case MouseAction.Drag: // If drag happens, the code can't reach here.
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(_mouseAction));
                    }

                    break;

                case EventType.MouseDrag:
                    if (_mouseAction == MouseAction.Click)
                    {
                        _mouseAction = MouseAction.Drag;
                        DragAndDrop.PrepareStartDrag();
                        DragAndDrop.SetGenericData(DragGenericData, DragGenericData);
                        DragAndDrop.objectReferences = new UObject[] { assetHandle.Asset };
                        DragAndDrop.paths = new string[] { AssetDatabase.GetAssetPath(assetHandle.Asset) };
                        DragAndDrop.StartDrag(null);
                    }

                    break;
            }
        }

        enum MouseAction : byte
        {
            None,
            Click,
            DoubleClick,
            ContextClick,
            Drag,
        }
    }
}