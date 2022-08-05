#if UNITY_2021_3_OR_NEWER
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using UObject = UnityEngine.Object;

namespace GBG.AssetQuickAccess.Editor
{
    public class AssetQuickAccessWindow : EditorWindow, IHasCustomMenu
    {
        [MenuItem("Window/Asset Management/Asset Quick Access Window")]
        public static void Open()
        {
            GetWindow<AssetQuickAccessWindow>("Asset Quick Access");
        }


        private void OnEnable()
        {
            LoadSettings();

            CreateView();
            _isViewDirty = true;
        }

        private void OnDisable()
        {
            SaveSettings();
        }

        private void Update()
        {
            if (_isViewDirty)
            {
                _assetListView.RefreshItems();
                _isViewDirty = false;
            }
        }

        private void CreateView()
        {
            // Root canvas
            // Can not add drag and drop manipulator to rootVisualElement directly,
            // so we need an extra visual element(_rootCanvas) to handle drag and drop events
            _rootCanvas = new VisualElement();
            _rootCanvas.StretchToParentSize();
            rootVisualElement.Add(_rootCanvas);
            var dragDropManipulator = new DragAndDropManipulator(_rootCanvas);
            dragDropManipulator.OnDropAssets += OnDropAssets;

            // Asset list view
            _assetListView = new ListView
            {
                reorderable = true,
                reorderMode = ListViewReorderMode.Animated,
                makeItem = CreateNewAssetListItem,
                bindItem = BindAssetListItem,
                itemsSource = (IList)_settings.AssetHandles,
                selectionType = SelectionType.None
            };
            _assetListView.itemIndexChanged += OnReorderAsset;
            _rootCanvas.Add(_assetListView);

            // Tool tips
            var tipsText = new Label
            {
                text = "Drag and drop asset here to record item.\n" +
                       "Click with middle mouse button to remove item.",
                style =
                {
                    unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter),
                    textOverflow = new StyleEnum<TextOverflow>(TextOverflow.Ellipsis),
                    height = 36
                }
            };
            _rootCanvas.Add(tipsText);
        }


        #region Settings

        private static readonly string _settingsPrefsKey = "GBG.AssetQuickAccess.SettingsPrefs";

        private AssetQuickAccessSettings _settings;


        private void LoadSettings()
        {
            var persistentGuids = EditorPrefs.GetString(_settingsPrefsKey, "");
            _settings = new AssetQuickAccessSettings(persistentGuids);
        }

        private void SaveSettings()
        {
            var persistentGuids = _settings.ToString();
            EditorPrefs.SetString(_settingsPrefsKey, persistentGuids);
        }

        #endregion


        #region Asset List View

        private VisualElement _rootCanvas;

        private bool _isViewDirty;

        private ListView _assetListView;

        private static readonly string _assetIconElementName = "asset-quick-access__asset-icon-image";


        private VisualElement CreateNewAssetListItem()
        {
            var button = new Button
            {
                style =
                {
                    // content
                    //alignItems =    new StyleEnum<Align>(Align.FlexStart),
                    //alignContent =    new StyleEnum<Align>(Align.FlexStart),
                    unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleLeft),
                    textOverflow = new StyleEnum<TextOverflow>(TextOverflow.Ellipsis),
                    // margin
                    marginLeft = 0,
                    marginRight = 0,
                    marginTop = 0,
                    marginBottom = 0,
                    // padding
                    paddingLeft = 32, // to avoid overlap with icon
                    paddingRight = 0,
                    paddingTop = 0,
                    paddingBottom = 0,
                    // border width
                    borderLeftWidth = 0,
                    borderRightWidth = 0,
                    borderTopWidth = 0,
                    borderBottomWidth = 0,
                    // border radius
                    borderTopLeftRadius = 0,
                    borderTopRightRadius = 0,
                    borderBottomLeftRadius = 0,
                    borderBottomRightRadius = 0,
                    // position
                    position = Position.Absolute,
                    left = 0f,
                    top = 1f,
                    right = 2f,
                    bottom = 1f,
                }
            };

            var iconImg = new Image
            {
                name = _assetIconElementName,
                style =
                {
                    width = new StyleLength(24),
                    height = new StyleLength(24),
                    marginLeft = -28 // to avoid overlap with text
                }
            };
            button.Add(iconImg);

            return button;
        }

        private void BindAssetListItem(VisualElement element, int index)
        {
            var button = (Button)element;
            var assetHandle = _settings.AssetHandles[index];
            button.text = assetHandle.GetDisplayName();
            button.RegisterCallback<ClickEvent>(e => OnLeftClickAssetListItem(assetHandle, e));
            button.RegisterCallback<MouseDownEvent>(e => OnRightOrMiddleClickAssetListItem(assetHandle, e));

            var iconImg = button.Q<Image>(_assetIconElementName);
            iconImg.image = AssetPreview.GetMiniThumbnail(assetHandle.Asset);

            //var elementContainer = button.parent;
            //elementContainer.style.paddingLeft = 0;
            //elementContainer.style.paddingRight = 0;
            //elementContainer.style.paddingTop = 0;
            //elementContainer.style.paddingBottom = 0;
        }

        private void OnLeftClickAssetListItem(AssetHandle handle, ClickEvent e)
        {
            // ClickEvent will only response mouse left click
            Assert.AreEqual(e.button, (int)MouseButton.LeftMouse);

            EditorGUIUtility.PingObject(handle.Asset);

            if (e.clickCount > 1)
            {
                AssetDatabase.OpenAsset(handle.Asset);
            }
        }

        private void OnRightOrMiddleClickAssetListItem(AssetHandle handle, MouseDownEvent e)
        {
            // MouseDownEvent will not response mouse right or middle click
            Assert.AreNotEqual(e.button, (int)MouseButton.LeftMouse);

            if (e.button == (int)MouseButton.MiddleMouse)
            {
                EditorGUIUtility.PingObject(handle.Asset);

                _settings.RemoveAsset(handle);
                _isViewDirty = true;
            }
        }

        private void OnReorderAsset(int fromIndex, int toIndex)
        {
            _settings.MarkDirty();
        }

        private void OnDropAssets(IList<UObject> assets)
        {
            for (int i = 0; i < assets.Count; i++)
            {
                _settings.AddAsset(assets[i]);
            }

            _isViewDirty = true;
        }

        #endregion


        #region Custom menu

        void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Clear all assets"), false, ClearAllAssets);
        }


        private void ClearAllAssets()
        {
            _settings.ClearAllAssets();
            _isViewDirty = true;

            EditorPrefs.DeleteKey(_settingsPrefsKey);
        }

        #endregion
    }
}
#endif
