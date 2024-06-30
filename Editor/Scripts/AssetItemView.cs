using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UObject = UnityEngine.Object;

namespace GBG.AssetQuickAccess.Editor
{
    internal class AssetItemView : IMGUIContainer
    {
        public static double DoubleClickInterval = 0.3f;

        private AssetHandle _assetHandle;
        private Image _assetIcon;
        private Image _categoryIcon;
        private Label _label;
        private MouseAction _mouseAction = MouseAction.None;
        private double _lastClickTime;

        public event System.Action<AssetHandle> OnWantsToRemoveAssetItem;


        public AssetItemView()
        {
            onGUIHandler = OnGUI;

            // To avoid conflict with the drag action of the ListView items.
            this.RegisterCallback<PointerDownEvent>(evt => evt.StopImmediatePropagation());

            // content
            style.height = new Length(100, LengthUnit.Percent);
            style.flexDirection = FlexDirection.Row;
            style.backgroundColor = EditorGUIUtility.isProSkin
                ? new Color(0.35f, 0.35f, 0.35f, 0.5f)
                : new Color(0.9f, 0.9f, 0.9f, 0.5f);
            // margin
            style.marginLeft = 0;
            style.marginRight = 0;
            style.marginTop = 0;
            style.marginBottom = 0;
            // padding
            style.paddingLeft = 2;
            style.paddingRight = 2;
            style.paddingTop = 0;
            style.paddingBottom = 0;
            // border width
            style.borderLeftWidth = 0;
            style.borderRightWidth = 0;
            style.borderTopWidth = 0;
            style.borderBottomWidth = 0;
            // border radius
            style.borderTopLeftRadius = 0;
            style.borderTopRightRadius = 0;
            style.borderBottomLeftRadius = 0;
            style.borderBottomRightRadius = 0;

            _assetIcon = new Image
            {
                name = "AssetIcon",
                pickingMode = PickingMode.Ignore,
                style =
                {
                    flexShrink = 0,
                    width = 24,
                    height = 24,
                }
            };
            Add(_assetIcon);

            _label = new Label
            {
                name = "AssetLabel",
                pickingMode = PickingMode.Ignore,
                style =
                {
                    flexGrow = 1,
                    flexShrink = 1,
                    paddingLeft = 2,
                    paddingRight = 2,
                    overflow = Overflow.Hidden,
                    unityTextAlign = TextAnchor.MiddleLeft,
                    textOverflow = TextOverflow.Ellipsis,
                }
            };
            Add(_label);
        }

        public void Bind(AssetHandle target)
        {
            _assetHandle = target;
            _assetHandle.Update();

            tooltip = _assetHandle.GetAssetPath();
            _label.text = _assetHandle.GetDisplayName();

            string categoryIconTooltip;
            Texture assetIconTex;
            Texture categoryIconTex;
            switch (_assetHandle.Category)
            {
                case AssetCategory.ProjectAsset:
                    assetIconTex = GetObjectIcon(_assetHandle.Asset, null);
                    categoryIconTex = null;
                    categoryIconTooltip = null;
                    break;

                case AssetCategory.SceneObject:
                    assetIconTex = GetObjectIcon(_assetHandle.Asset, _assetHandle.Scene);
                    categoryIconTex = GetSceneObjectTexture(true);
                    categoryIconTooltip = "Scene Object";
                    break;

                case AssetCategory.ExternalFile:
                    string path = _assetHandle.GetAssetPath();
                    assetIconTex = File.Exists(path) || Directory.Exists(path)
                        ? GetExternalFileTexture(false)
                        : GetWarningTexture();
                    categoryIconTex = GetExternalFileTexture(true);
                    categoryIconTooltip = "External File of Folder";
                    break;

                case AssetCategory.Url:
                    string url = _assetHandle.GetAssetPath();
                    assetIconTex = GetUrlTexture();
                    categoryIconTex = assetIconTex;
                    categoryIconTooltip = "URL";
                    break;

                default:
                    throw new System.ArgumentOutOfRangeException(nameof(_assetHandle.Category), _assetHandle.Category, null);
            }

            _assetIcon.image = assetIconTex;
            if (categoryIconTex)
            {
                if (_categoryIcon == null)
                {
                    CreateCategoryIcon();
                }

                _categoryIcon.tooltip = categoryIconTooltip;
                _categoryIcon.image = categoryIconTex;
                _categoryIcon.style.display = DisplayStyle.Flex;
            }
            else if (_categoryIcon != null)
            {
                _categoryIcon.tooltip = categoryIconTooltip;
                _categoryIcon.image = null;
                _categoryIcon.style.display = DisplayStyle.None;
            }
        }

        public void Unbind()
        {
            _assetHandle = null;

            tooltip = null;
            _assetIcon.image = null;
            _label.text = null;
            if (_categoryIcon != null)
            {
                _categoryIcon.tooltip = null;
                _categoryIcon.image = null;
                _categoryIcon.style.display = DisplayStyle.None;
            }
        }

        private void CreateCategoryIcon()
        {
            if (_categoryIcon != null)
            {
                return;
            }

            _categoryIcon = new Image
            {
                name = "CategoryIcon",
                //pickingMode = PickingMode.Ignore, // Allow picking to show tooltip
                style =
                {
                    flexShrink = 0,
                    alignSelf = Align.Center,
                    width = 16,
                    height = 16,
                }
            };
            // To avoid conflict with the drag action of the ListView items.
            _categoryIcon.RegisterCallback<PointerDownEvent>(evt => evt.StopImmediatePropagation());

            Add(_categoryIcon);
        }

        private void OnGUI()
        {
            Event e = Event.current;
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0)
                    {
                        double currentTime = EditorApplication.timeSinceStartup;
                        if (currentTime - _lastClickTime < DoubleClickInterval)
                        {
                            _mouseAction = MouseAction.DoubleClick;
                            _lastClickTime = 0;
                        }
                        else
                        {
                            _mouseAction = MouseAction.Click;
                            _lastClickTime = currentTime;
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
                                OnClick();
                                e.Use();
                            }
                            break;

                        case MouseAction.DoubleClick:
                            if (e.button == 0)
                            {
                                _mouseAction = MouseAction.None;
                                OnDoubleClick();
                                e.Use();
                            }
                            break;

                        case MouseAction.ContextClick:
                            if (e.button == 1)
                            {
                                _mouseAction = MouseAction.None;
                                OnContextClick(e.mousePosition);
                                e.Use();
                            }
                            break;

                        case MouseAction.None:
                        case MouseAction.Drag: // If drag happens, the code can't reach here.
                            break;

                        default:
                            throw new System.ArgumentOutOfRangeException(nameof(_mouseAction));
                    }

                    break;

                case EventType.MouseDrag:
                    if (_mouseAction == MouseAction.Click)
                    {
                        _mouseAction = MouseAction.Drag;
                        DragAndDrop.PrepareStartDrag();
                        if (_assetHandle.Asset)
                        {
                            DragAndDrop.objectReferences = new UObject[] { _assetHandle.Asset };
                            DragAndDrop.paths = new string[] { AssetDatabase.GetAssetPath(_assetHandle.Asset) };
                        }
                        DragAndDrop.StartDrag(null);
                    }
                    break;

                case EventType.DragUpdated:
                    if (DragAndDrop.objectReferences.Length > 0)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                    }
                    break;

                default:
                    break;
            }
        }

        private void OnClick()
        {
            if (_assetHandle.Category == AssetCategory.ExternalFile)
            {
                Bind(_assetHandle);
            }

            _assetHandle.PingAsset();
        }

        private void OnDoubleClick()
        {
            if (_assetHandle.Category == AssetCategory.ExternalFile)
            {
                Bind(_assetHandle);
            }

            _assetHandle.OpenAsset();
        }

        private void OnContextClick(Vector2 mousePosition)
        {
            switch (_assetHandle.Category)
            {
                case AssetCategory.ProjectAsset:
                    ShowProjectAssetContextMenu(mousePosition);
                    break;

                case AssetCategory.SceneObject:
                    ShowSceneObjectContextMenu(mousePosition);
                    break;

                case AssetCategory.ExternalFile:
                    Bind(_assetHandle);
                    ShowExternalFileContextMenu(mousePosition);
                    break;

                case AssetCategory.Url:
                    ShowUrlContextMenu(mousePosition);
                    break;

                default:
                    throw new System.ArgumentOutOfRangeException(nameof(_assetHandle.Category), _assetHandle.Category, null);
            }
        }

        private void ShowProjectAssetContextMenu(Vector2 mousePosition)
        {
            Assert.IsTrue(_assetHandle.Category == AssetCategory.ProjectAsset);

            GenericDropdownMenu menu = new GenericDropdownMenu();
            if (_assetHandle.Asset)
            {
                menu.AddItem("Open", false, _assetHandle.OpenAsset);
                menu.AddItem("Copy Path", false, _assetHandle.CopyPathToSystemBuffer);
                menu.AddItem("Copy Guid", false, _assetHandle.CopyGuidToSystemBuffer);
                menu.AddItem("Copy Type", false, _assetHandle.CopyTypeFullNameToSystemBuffer);
                menu.AddItem("Show in Folder", false, _assetHandle.ShowInFolder);
            }
            else
            {
                menu.AddItem("Copy Guid", false, _assetHandle.CopyGuidToSystemBuffer);
            }
            menu.AddItem("Remove", false, () => OnWantsToRemoveAssetItem?.Invoke(_assetHandle));
            menu.DropDown(new Rect(this.LocalToWorld(mousePosition), Vector2.zero), this);
        }

        private void ShowSceneObjectContextMenu(Vector2 mousePosition)
        {
            Assert.IsTrue(_assetHandle.Category == AssetCategory.SceneObject);

            GenericDropdownMenu menu = new GenericDropdownMenu();
            if (_assetHandle.Asset)
            {
                menu.AddItem("Open", false, _assetHandle.OpenAsset);
                menu.AddItem("Copy Hierarchy Path", false, _assetHandle.CopyPathToSystemBuffer);
                menu.AddItem("Copy Type", false, _assetHandle.CopyTypeFullNameToSystemBuffer);
            }
            else if (_assetHandle.Scene)
            {
                menu.AddItem("Open in Scene", false, _assetHandle.OpenAsset);
            }
            menu.AddItem("Remove", false, () => OnWantsToRemoveAssetItem?.Invoke(_assetHandle));
            menu.DropDown(new Rect(this.LocalToWorld(mousePosition), Vector2.zero), this);
        }

        private void ShowExternalFileContextMenu(Vector2 mousePosition)
        {
            Assert.IsTrue(_assetHandle.Category == AssetCategory.ExternalFile);

            GenericDropdownMenu menu = new GenericDropdownMenu();
            string path = _assetHandle.GetAssetPath();
            if (File.Exists(path) || Directory.Exists(path))
            {
                menu.AddItem("Open", false, _assetHandle.OpenAsset);
                menu.AddItem("Copy Path", false, _assetHandle.CopyPathToSystemBuffer);
                menu.AddItem("Show in Folder", false, _assetHandle.ShowInFolder);
            }
            else
            {
                menu.AddItem("Copy Path", false, _assetHandle.CopyPathToSystemBuffer);
            }
            menu.AddItem("Remove", false, () => OnWantsToRemoveAssetItem?.Invoke(_assetHandle));
            menu.DropDown(new Rect(this.LocalToWorld(mousePosition), Vector2.zero), this);
        }

        private void ShowUrlContextMenu(Vector2 mousePosition)
        {
            Assert.IsTrue(_assetHandle.Category == AssetCategory.Url);

            GenericDropdownMenu menu = new GenericDropdownMenu();
            menu.AddItem("Open", false, _assetHandle.OpenAsset);
            menu.AddItem("Copy URL", false, _assetHandle.CopyPathToSystemBuffer);
            menu.AddItem("Remove", false, () => OnWantsToRemoveAssetItem?.Invoke(_assetHandle));
            menu.DropDown(new Rect(this.LocalToWorld(mousePosition), Vector2.zero), this);
        }


        #region Static Textures

        private static Texture _sceneObjectTextureCache;
        private static Texture _sceneObjectTextureSmallCache;
        private static Texture _externalFileTextureCache;
        private static Texture _externalFileTextureSmallCache;
        private static Texture _urlTextureCache;
        private static Texture _warningTextureCache;

        private static Texture GetObjectIcon(UObject obj, SceneAsset containingScene)
        {
            if (obj)
            {
                return AssetPreview.GetMiniThumbnail(obj);
            }

            if (containingScene)
            {
                string scenePath = AssetDatabase.GetAssetPath(containingScene);
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene scene = SceneManager.GetSceneAt(i);
                    if (scene.isLoaded && scene.path == scenePath)
                    {
                        return GetWarningTexture();
                    }
                }

                return GetSceneObjectTexture(false);
            }

            return GetWarningTexture();
        }

        private static Texture GetSceneObjectTexture(bool small)
        {
            if (small)
            {
                if (!_sceneObjectTextureSmallCache)
                {
                    _sceneObjectTextureSmallCache = (Texture)EditorGUIUtility.Load(
                        EditorGUIUtility.isProSkin
                        ? "d_UnityEditor.SceneHierarchyWindow"
                        : "UnityEditor.SceneHierarchyWindow");
                }

                return _sceneObjectTextureSmallCache;
            }

            if (!_sceneObjectTextureCache)
            {
                _sceneObjectTextureCache = (Texture)EditorGUIUtility.Load(
                    EditorGUIUtility.isProSkin
                    ? "d_UnityEditor.SceneHierarchyWindow@2x"
                    : "UnityEditor.SceneHierarchyWindow@2x");
            }

            return _sceneObjectTextureCache;
        }

        private static Texture GetExternalFileTexture(bool small)
        {
            if (small)
            {
                if (!_externalFileTextureSmallCache)
                {
                    _externalFileTextureSmallCache = (Texture)EditorGUIUtility.Load(EditorGUIUtility.isProSkin ? "d_Import" : "Import");
                }

                return _externalFileTextureSmallCache;
            }

            if (!_externalFileTextureCache)
            {
                _externalFileTextureCache = (Texture)EditorGUIUtility.Load(EditorGUIUtility.isProSkin ? "d_Import@2x" : "Import@2x");
            }

            return _externalFileTextureCache;
        }

        private static Texture GetUrlTexture()
        {
            if (!_urlTextureCache)
            {
                _urlTextureCache = (Texture)EditorGUIUtility.Load(EditorGUIUtility.isProSkin ? "d_BuildSettings.Web.Small" : "BuildSettings.Web.Small");
            }

            return _urlTextureCache;
        }

        private static Texture GetWarningTexture()
        {
            if (!_warningTextureCache)
            {
                _warningTextureCache = (Texture)EditorGUIUtility.Load("Warning@2x");
            }

            return _warningTextureCache;
        }

        #endregion


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
