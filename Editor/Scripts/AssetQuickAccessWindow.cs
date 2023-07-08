#if UNITY_2021_3_OR_NEWER
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UDebug = UnityEngine.Debug;
using UObject = UnityEngine.Object;

namespace GBG.AssetQuickAccess.Editor
{
    public class AssetQuickAccessWindow : EditorWindow, IHasCustomMenu
    {
        [MenuItem("Tools/Bamboo/Asset Quick Access Window")]
        [MenuItem("Window/Asset Management/Asset Quick Access Window")]
        public static void Open()
        {
            var window = GetWindow<AssetQuickAccessWindow>("Asset Quick Access");
            window.minSize = new Vector2(400, 120);
        }


        private void OnEnable()
        {
            ConvertOldData();

            LoadSettings();

            CreateView();
            _isViewDirty = true;
        }

        private void OnLostFocus()
        {
            if (_isSettingsDirty)
            {
                SaveSettings();
            }
        }

        private void OnDisable()
        {
            if (_isSettingsDirty)
            {
                SaveSettings();
            }
        }

        private void Update()
        {
            if (_isViewDirty)
            {
                _assetListView.RefreshItems();
                _isViewDirty = false;
            }
        }

        private void OnProjectChange()
        {
            _isViewDirty = true;
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
                unbindItem = UnbindAssetListItem,
                itemsSource = _settings.AssetHandles,
                selectionType = SelectionType.None,
                style =
                {
                    flexGrow = 1,
                }
            };
            _assetListView.itemIndexChanged += OnReorderAsset;
            _rootCanvas.Add(_assetListView);

            // Tool tips
            var tipsText = new Label
            {
                text = "Drag and drop the asset here to add a new item.",
                style =
                {
                    unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter),
                    textOverflow = new StyleEnum<TextOverflow>(TextOverflow.Ellipsis),
                    height = 36
                }
            };
            _rootCanvas.Add(tipsText);

            // Find asset by guid/path
            var findAssetContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    height = 32,
                    minHeight = 32,
                    paddingTop = 3,
                    paddingBottom = 3,
                },
            };
            _rootCanvas.Add(findAssetContainer);
            var assetUrlField = new TextField("Find Asset")
            {
                style =
                {
                    flexGrow = 1,
                },
                labelElement =
                {
                    style =
                    {
                        width = 68,
                        minWidth = 68,
                        unityTextAlign = TextAnchor.MiddleCenter,
                    }
                }
            };
            findAssetContainer.Add(assetUrlField);
            var findAssetButton = new Button(() =>
                {
                    var url = assetUrlField.value;
                    var filePath = string.Empty;
                    if (!string.IsNullOrEmpty(url))
                    {
                        if (url.StartsWith("Assets", StringComparison.OrdinalIgnoreCase)) filePath = url;
                        else filePath = AssetDatabase.GUIDToAssetPath(url);
                    }
                    var asset = AssetDatabase.LoadAssetAtPath<UObject>(filePath);
                    if (asset)
                    {
                        EditorGUIUtility.PingObject(asset);
                    }
                    else
                    {
                        ShowNotification(new GUIContent($"Can not find asset with guid or path '{url}'."));
                    }
                })
            {
                text = "Find",
            };
            findAssetContainer.Add(findAssetButton);
        }


        #region Settings

        private static string _settingsPrefsKey;

        private AssetQuickAccessSettings _settings;

        private bool _isSettingsDirty;


        private void PrepareSettingsPrefsKey()
        {
            if (string.IsNullOrEmpty(_settingsPrefsKey))
            {
                _settingsPrefsKey = "GBG.AssetQuickAccess.SettingsPrefs@" +
                                    Application.companyName + "@" +
                                    Application.productName;
            }
        }

        private void LoadSettings()
        {
            PrepareSettingsPrefsKey();
            var persistentGuids = EditorPrefs.GetString(_settingsPrefsKey, "");
            _settings = new AssetQuickAccessSettings(persistentGuids);
        }

        private void SaveSettings()
        {
            PrepareSettingsPrefsKey();
            var persistentGuids = _settings.ToString();
            EditorPrefs.SetString(_settingsPrefsKey, persistentGuids);
            _isSettingsDirty = false;
        }

        #endregion


        #region Compatibility

        private void ConvertOldData()
        {
            // Delete version 1 data(Conversion not supported)
            EditorPrefs.DeleteKey("GBG.AssetQuickAccess.SettingsPrefs");

            // TODO: Convert Version 2 data

        }

        #endregion


        #region Asset List View

        private VisualElement _rootCanvas;

        private bool _isViewDirty;

        private ListView _assetListView;

        private static readonly string _assetIconElementName = "asset-quick-access__asset-icon-image";

        private static Texture _warningTexture;


        private VisualElement CreateNewAssetListItem()
        {
            var button = new Button
            {
                style =
                {
                    // content
                    unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleLeft),
                    textOverflow = new StyleEnum<TextOverflow>(TextOverflow.Ellipsis),
                    height = new Length(100, LengthUnit.Percent),
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
                }
            };

            var iconImg = new Image
            {
                name = _assetIconElementName,
                style =
                {
                    width = new StyleLength(24),
                    height = new Length(100, LengthUnit.Percent),
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
            button.RegisterCallback<ClickEvent, AssetHandle>(OnClickAssetListItem, assetHandle);
            button.RegisterCallback<ContextClickEvent, AssetHandle>(OnContextClickAssetListItem, assetHandle);

            var iconImg = button.Q<Image>(_assetIconElementName);
            Texture iconTex = AssetPreview.GetMiniThumbnail(assetHandle.Asset);
            if (!iconTex)
            {
                if (!_warningTexture)
                {
                    _warningTexture = EditorGUIUtility.IconContent("Warning@2x").image;
                }

                iconTex = _warningTexture;
            }

            iconImg.image = iconTex;

            //var elementContainer = button.parent;
            //elementContainer.style.paddingLeft = 0;
            //elementContainer.style.paddingRight = 0;
            //elementContainer.style.paddingTop = 0;
            //elementContainer.style.paddingBottom = 0;
        }

        private void UnbindAssetListItem(VisualElement element, int index)
        {
            var button = (Button)element;
            button.UnregisterCallback<ClickEvent, AssetHandle>(OnClickAssetListItem);
            button.UnregisterCallback<ContextClickEvent, AssetHandle>(OnContextClickAssetListItem);
        }

        private void OnClickAssetListItem(ClickEvent e, AssetHandle handle)
        {
            e.StopPropagation();

            EditorGUIUtility.PingObject(handle.Asset);
            if (e.clickCount > 1)
            {
                AssetDatabase.OpenAsset(handle.Asset);
            }
        }

        private void OnContextClickAssetListItem(ContextClickEvent e, AssetHandle handle)
        {
            e.StopPropagation();

            var menu = new GenericDropdownMenu();
            menu.AddItem("Ping", false, () => EditorGUIUtility.PingObject(handle.Asset));
            menu.AddItem("Print Guid", false, () => UDebug.Log(handle.Guid, handle.Asset));
            menu.AddItem("Print Path", false, () => UDebug.Log(AssetDatabase.GUIDToAssetPath(handle.Guid), handle.Asset));
            menu.AddItem("Show in Folder", false, () => EditorUtility.RevealInFinder(AssetDatabase.GUIDToAssetPath(handle.Guid)));
            menu.AddItem("Remove", false, () =>
            {
                _settings.RemoveAsset(handle);
                _isViewDirty = true;
                _isSettingsDirty = true;
            });
            menu.DropDown(new Rect(e.mousePosition, Vector2.zero), e.currentTarget as VisualElement);
        }

        private void OnReorderAsset(int fromIndex, int toIndex)
        {
            _settings.MarkDirty();
            _isSettingsDirty = true;
        }

        private void OnDropAssets(IList<UObject> assets)
        {
            for (int i = 0; i < assets.Count; i++)
            {
                _settings.AddAsset(assets[i]);
            }

            _isViewDirty = true;
            _isSettingsDirty = true;
        }

        #endregion


        #region Custom menu

        void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Clear all assets"), false, ClearAllAssets);
            menu.AddItem(new GUIContent("Print raw data"), false, PrintRawData);
        }


        private void ClearAllAssets()
        {
            _settings.ClearAllAssets();
            _isViewDirty = true;
            _isSettingsDirty = false;

            PrepareSettingsPrefsKey();
            EditorPrefs.DeleteKey(_settingsPrefsKey);
        }

        private void PrintRawData()
        {
            if (_isSettingsDirty)
            {
                SaveSettings();
            }

            PrepareSettingsPrefsKey();
            var persistentGuids = EditorPrefs.GetString(_settingsPrefsKey, "");
            UDebug.Log($"{_settingsPrefsKey}\n{persistentGuids}");
        }

        #endregion
    }
}
#endif
