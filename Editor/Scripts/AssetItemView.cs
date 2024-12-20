﻿using System;
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

        private VisualElement _container;
        private Image _assetIcon;
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

            // container
            _container = new VisualElement
            {
                name = "Container",
                style =
                {
                    flexGrow = 1,
                    flexDirection = FlexDirection.Row,
                    backgroundColor = EditorGUIUtility.isProSkin
                        ? new Color(0.35f, 0.35f, 0.35f, 0.5f)
                        : new Color(0.9f, 0.9f, 0.9f, 0.5f),
                }
            };
            Add(_container);

            _assetIcon = new Image
            {
                name = "AssetIcon",
                pickingMode = PickingMode.Ignore,
                style =
                {
                    flexShrink = 0,
                    alignSelf = Align.Center,
                    width = 16,
                    height = 16,
                    marginLeft = 4,
                }
            };
            _container.Add(_assetIcon);

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
            _container.Add(_label);
        }

        public void Bind(AssetHandle target)
        {
            AssetHandle = target;
            AssetHandle.Update();

            //tooltip = $"{AssetHandle.GetAssetPath()} ({AssetHandle.Category})";
            tooltip = AssetHandle.GetAssetPath();
            _label.text = AssetHandle.GetDisplayName();

            Texture assetIconTex;
            switch (AssetHandle.Category)
            {
                case AssetCategory.ProjectAsset:
                    assetIconTex = GetObjectIcon(AssetHandle.Asset, null);
                    break;

                case AssetCategory.SceneObject:
                    assetIconTex = GetObjectIcon(AssetHandle.Asset, AssetHandle.Scene);
                    break;

                case AssetCategory.ExternalFile:
                    string path = AssetHandle.GetAssetPath();
                    assetIconTex = File.Exists(path) || Directory.Exists(path)
                        ? GetExternalFileTexture()
                        : GetWarningTexture();
                    break;

                case AssetCategory.Url:
                    assetIconTex = GetUrlTexture();
                    break;

                case AssetCategory.MenuItem:
                    assetIconTex = GetMenuItemTexture();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(AssetHandle.Category), AssetHandle.Category, null);
            }

            _assetIcon.image = assetIconTex;
        }

        public void Unbind()
        {
            AssetHandle = null;

            tooltip = null;
            _assetIcon.image = null;
            _label.text = null;
        }

        public void SetVerticalPadding(float padding)
        {
            style.paddingTop = style.paddingBottom = padding;
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


        #region Get Textures

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

                return GetSceneObjectTexture();
            }

            return GetWarningTexture();
        }

        private static Texture GetSceneObjectTexture()
        {
            return EditorGUIUtility.IconContent("UnityEditor.SceneHierarchyWindow").image;
        }

        private static Texture GetExternalFileTexture()
        {
            return EditorGUIUtility.IconContent("Import").image;
        }

        private static Texture GetUrlTexture()
        {
            return EditorGUIUtility.IconContent("BuildSettings.Web.Small").image;
        }

        private static Texture GetMenuItemTexture()
        {
            return EditorGUIUtility.IconContent("PlayButton").image;
        }

        private static Texture GetWarningTexture()
        {
            return EditorGUIUtility.IconContent("Warning").image;
        }

        #endregion
    }
}