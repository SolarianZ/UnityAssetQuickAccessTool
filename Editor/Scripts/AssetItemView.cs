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
                    assetIconTex = GetUrlTexture();
                    categoryIconTex = assetIconTex;
                    categoryIconTooltip = "URL";
                    break;

                case AssetCategory.MenuItem:
                    assetIconTex = GetMenuItemTexture();
                    categoryIconTex = assetIconTex;
                    categoryIconTooltip = "MenuItem";
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

                Assert.IsTrue(_categoryIcon != null);
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

                case AssetCategory.MenuItem:
                    ShowMenuItemContextMenu(mousePosition);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(AssetHandle.Category), AssetHandle.Category, null);
            }
        }

        // The GenericDropdownMenu cannot display beyond the window it is in, and it has bugs in Unity 6000.0.
        // Therefore, we are using GenericMenu instead.
        // MEMO Unity BUG: https://issuetracker.unity3d.com/product/unity/issues/guid/UUM-77265
        // Custom contextual menu is broken or displayed wrongly when it is created with GenericDropdownMenu UIElement

        private void ShowProjectAssetContextMenu(Vector2 mousePosition)
        {
            Assert.IsTrue(AssetHandle.Category == AssetCategory.ProjectAsset);

            GenericMenu genericMenu = new GenericMenu();
            if (AssetHandle.Asset)
            {
                genericMenu.AddItem(new GUIContent("Open"), false, AssetHandle.OpenAsset);
                genericMenu.AddItem(new GUIContent("Copy Path"), false, AssetHandle.CopyPathToSystemBuffer);
                genericMenu.AddItem(new GUIContent("Copy Guid"), false, AssetHandle.CopyGuidToSystemBuffer);
                genericMenu.AddItem(new GUIContent("Copy Type"), false, AssetHandle.CopyTypeFullNameToSystemBuffer);
                genericMenu.AddItem(new GUIContent("Copy Instance Id"), false, AssetHandle.CopyInstanceIdToSystemBuffer);
                genericMenu.AddItem(new GUIContent("Show in Folder"), false, AssetHandle.ShowInFolder);
            }
            else
            {
                genericMenu.AddItem(new GUIContent("Copy Guid"), false, AssetHandle.CopyGuidToSystemBuffer);
            }

            genericMenu.AddItem(new GUIContent("Remove"), false, () => OnWantsToRemoveAssetItem?.Invoke(AssetHandle));
            genericMenu.ShowAsContext();
        }

        private void ShowSceneObjectContextMenu(Vector2 mousePosition)
        {
            Assert.IsTrue(AssetHandle.Category == AssetCategory.SceneObject);

            GenericMenu genericMenu = new GenericMenu();
            if (AssetHandle.Asset)
            {
                genericMenu.AddItem(new GUIContent("Open"), false, AssetHandle.OpenAsset);
                genericMenu.AddItem(new GUIContent("Copy Hierarchy Path"), false, AssetHandle.CopyPathToSystemBuffer);
                genericMenu.AddItem(new GUIContent("Copy Type"), false, AssetHandle.CopyTypeFullNameToSystemBuffer);
                genericMenu.AddItem(new GUIContent("Copy Instance Id"), false, AssetHandle.CopyInstanceIdToSystemBuffer);
            }
            else if (AssetHandle.Scene)
            {
                genericMenu.AddItem(new GUIContent("Open in Scene"), false, AssetHandle.OpenAsset);
            }

            genericMenu.AddItem(new GUIContent("Remove"), false, () => OnWantsToRemoveAssetItem?.Invoke(AssetHandle));
            genericMenu.ShowAsContext();
        }

        private void ShowExternalFileContextMenu(Vector2 mousePosition)
        {
            Assert.IsTrue(AssetHandle.Category == AssetCategory.ExternalFile);

            string path = AssetHandle.GetAssetPath();
            GenericMenu genericMenu = new GenericMenu();
            if (File.Exists(path) || Directory.Exists(path))
            {
                genericMenu.AddItem(new GUIContent("Open"), false, AssetHandle.OpenAsset);
                genericMenu.AddItem(new GUIContent("Copy Path"), false, AssetHandle.CopyPathToSystemBuffer);
                genericMenu.AddItem(new GUIContent("Show in Folder"), false, AssetHandle.ShowInFolder);
            }
            else
            {
                genericMenu.AddItem(new GUIContent("Copy Path"), false, AssetHandle.CopyPathToSystemBuffer);
            }

            genericMenu.AddItem(new GUIContent("Remove"), false, () => OnWantsToRemoveAssetItem?.Invoke(AssetHandle));
            genericMenu.ShowAsContext();
        }

        private void ShowUrlContextMenu(Vector2 mousePosition)
        {
            Assert.IsTrue(AssetHandle.Category == AssetCategory.Url);

            GenericMenu genericMenu = new GenericMenu();
            genericMenu.AddItem(new GUIContent("Open"), false, AssetHandle.OpenAsset);
            genericMenu.AddItem(new GUIContent("Copy URL"), false, AssetHandle.CopyPathToSystemBuffer);
            genericMenu.AddItem(new GUIContent("Remove"), false, () => OnWantsToRemoveAssetItem?.Invoke(AssetHandle));
            genericMenu.ShowAsContext();
        }

        private void ShowMenuItemContextMenu(Vector2 mousePosition)
        {
            Assert.IsTrue(AssetHandle.Category == AssetCategory.MenuItem);

            GenericMenu genericMenu = new GenericMenu();
            genericMenu.AddItem(new GUIContent("Execute"), false, AssetHandle.OpenAsset);
            genericMenu.AddItem(new GUIContent("Remove"), false, () => OnWantsToRemoveAssetItem?.Invoke(AssetHandle));
            genericMenu.ShowAsContext();
        }


        #region Static Textures

        private static Texture _sceneObjectTextureCache;
        private static Texture _sceneObjectTextureSmallCache;
        private static Texture _externalFileTextureCache;
        private static Texture _externalFileTextureSmallCache;
        private static Texture _urlTextureCache;
        private static Texture _menuItemTextureCache;
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

        private static Texture GetMenuItemTexture()
        {
            if (!_menuItemTextureCache)
            {
                _menuItemTextureCache = (Texture)EditorGUIUtility.Load(EditorGUIUtility.isProSkin ? "d_PlayButton@2x" : "PlayButton@2x");
            }

            return _menuItemTextureCache;
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
    }
}