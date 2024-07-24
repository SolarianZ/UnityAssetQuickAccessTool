using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UObject = UnityEngine.Object;

namespace GBG.AssetQuickAccess.Editor
{
    internal class AssetItemView : VisualElement
    {
        public const string DragGenericData = "GBG_AQA_DragItem";

        public AssetHandle AssetHandle { get; private set; }
        private Image _assetIcon;
        private Image _categoryIcon;
        private Label _label;
        private MouseAction _mouseAction = MouseAction.None;
        private double _lastClickTime;

        public event Action<AssetHandle> OnWantsToRemoveAssetItem;


        public AssetItemView()
        {
            // this.RegisterCallback<PointerDownEvent>(evt => evt.StopImmediatePropagation()); // To avoid conflict with the drag action of the ListView items.
            AssetItemViewActionManipulator actionManipulator = new AssetItemViewActionManipulator();
            actionManipulator.Clicked += OnClick;
            actionManipulator.DoubleClicked += OnDoubleClick;
            actionManipulator.ContextClicked += OnContextClick;
            this.AddManipulator(actionManipulator);

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
            AssetHandle = target;
            AssetHandle.Update();

            tooltip = AssetHandle.GetAssetPath();
            _label.text = AssetHandle.GetDisplayName();

            string categoryIconTooltip;
            Texture assetIconTex;
            Texture categoryIconTex;
            switch (AssetHandle.Category)
            {
                case AssetCategory.ProjectAsset:
                    assetIconTex = GetObjectIcon(AssetHandle.Asset, null);
                    categoryIconTex = null;
                    categoryIconTooltip = null;
                    break;

                case AssetCategory.SceneObject:
                    assetIconTex = GetObjectIcon(AssetHandle.Asset, AssetHandle.Scene);
                    categoryIconTex = GetSceneObjectTexture(true);
                    categoryIconTooltip = "Scene Object";
                    break;

                case AssetCategory.ExternalFile:
                    string path = AssetHandle.GetAssetPath();
                    assetIconTex = File.Exists(path) || Directory.Exists(path)
                        ? GetExternalFileTexture(false)
                        : GetWarningTexture();
                    categoryIconTex = GetExternalFileTexture(true);
                    categoryIconTooltip = "External File of Folder";
                    break;

                case AssetCategory.Url:
                    string url = AssetHandle.GetAssetPath();
                    assetIconTex = GetUrlTexture();
                    categoryIconTex = assetIconTex;
                    categoryIconTooltip = "URL";
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(AssetHandle.Category), AssetHandle.Category, null);
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
            AssetHandle = null;

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

        private void OnClick()
        {
            if (AssetHandle.Category == AssetCategory.ExternalFile)
            {
                Bind(AssetHandle);
            }

            AssetHandle.PingAsset();
        }

        private void OnDoubleClick()
        {
            if (AssetHandle.Category == AssetCategory.ExternalFile)
            {
                Bind(AssetHandle);
            }

            AssetHandle.OpenAsset();
        }

        private void OnContextClick(Vector2 mousePosition)
        {
            switch (AssetHandle.Category)
            {
                case AssetCategory.ProjectAsset:
                    ShowProjectAssetContextMenu(mousePosition);
                    break;

                case AssetCategory.SceneObject:
                    ShowSceneObjectContextMenu(mousePosition);
                    break;

                case AssetCategory.ExternalFile:
                    Bind(AssetHandle);
                    ShowExternalFileContextMenu(mousePosition);
                    break;

                case AssetCategory.Url:
                    ShowUrlContextMenu(mousePosition);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(AssetHandle.Category), AssetHandle.Category, null);
            }
        }

        private void ShowProjectAssetContextMenu(Vector2 mousePosition)
        {
            Assert.IsTrue(AssetHandle.Category == AssetCategory.ProjectAsset);

            GenericDropdownMenu menu = new GenericDropdownMenu();
            if (AssetHandle.Asset)
            {
                menu.AddItem("Open", false, AssetHandle.OpenAsset);
                menu.AddItem("Copy Path", false, AssetHandle.CopyPathToSystemBuffer);
                menu.AddItem("Copy Guid", false, AssetHandle.CopyGuidToSystemBuffer);
                menu.AddItem("Copy Type", false, AssetHandle.CopyTypeFullNameToSystemBuffer);
                menu.AddItem("Show in Folder", false, AssetHandle.ShowInFolder);
            }
            else
            {
                menu.AddItem("Copy Guid", false, AssetHandle.CopyGuidToSystemBuffer);
            }

            menu.AddItem("Remove", false, () => OnWantsToRemoveAssetItem?.Invoke(AssetHandle));
            menu.DropDown(new Rect(mousePosition, Vector2.zero), this, false);
        }

        private void ShowSceneObjectContextMenu(Vector2 mousePosition)
        {
            Assert.IsTrue(AssetHandle.Category == AssetCategory.SceneObject);

            GenericDropdownMenu menu = new GenericDropdownMenu();
            if (AssetHandle.Asset)
            {
                menu.AddItem("Open", false, AssetHandle.OpenAsset);
                menu.AddItem("Copy Hierarchy Path", false, AssetHandle.CopyPathToSystemBuffer);
                menu.AddItem("Copy Type", false, AssetHandle.CopyTypeFullNameToSystemBuffer);
            }
            else if (AssetHandle.Scene)
            {
                menu.AddItem("Open in Scene", false, AssetHandle.OpenAsset);
            }

            menu.AddItem("Remove", false, () => OnWantsToRemoveAssetItem?.Invoke(AssetHandle));
            menu.DropDown(new Rect(mousePosition, Vector2.zero), this, false);
        }

        private void ShowExternalFileContextMenu(Vector2 mousePosition)
        {
            Assert.IsTrue(AssetHandle.Category == AssetCategory.ExternalFile);

            GenericDropdownMenu menu = new GenericDropdownMenu();
            string path = AssetHandle.GetAssetPath();
            if (File.Exists(path) || Directory.Exists(path))
            {
                menu.AddItem("Open", false, AssetHandle.OpenAsset);
                menu.AddItem("Copy Path", false, AssetHandle.CopyPathToSystemBuffer);
                menu.AddItem("Show in Folder", false, AssetHandle.ShowInFolder);
            }
            else
            {
                menu.AddItem("Copy Path", false, AssetHandle.CopyPathToSystemBuffer);
            }

            menu.AddItem("Remove", false, () => OnWantsToRemoveAssetItem?.Invoke(AssetHandle));
            menu.DropDown(new Rect(mousePosition, Vector2.zero), this, false);
        }

        private void ShowUrlContextMenu(Vector2 mousePosition)
        {
            Assert.IsTrue(AssetHandle.Category == AssetCategory.Url);

            GenericDropdownMenu menu = new GenericDropdownMenu();
            menu.AddItem("Open", false, AssetHandle.OpenAsset);
            menu.AddItem("Copy URL", false, AssetHandle.CopyPathToSystemBuffer);
            menu.AddItem("Remove", false, () => OnWantsToRemoveAssetItem?.Invoke(AssetHandle));
            menu.DropDown(new Rect(mousePosition, Vector2.zero), this, false);
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