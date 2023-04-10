#if !UNITY_2021_3_OR_NEWER
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UObject = UnityEngine.Object;
using UDebug = UnityEngine.Debug;

namespace GBG.AssetQuickAccess.Editor
{
    public class AssetQuickAccessWindow_IMGUI : EditorWindow, IHasCustomMenu
    {
        [MenuItem("Tools/Bamboo/Asset Quick Access Window(IMGUI)")]
        [MenuItem("Window/Asset Management/Asset Quick Access Window(IMGUI)")]
        public static void Open()
        {
            GetWindow<AssetQuickAccessWindow_IMGUI>("Asset Quick Access");
        }


        private GUIStyle _assetItemButtonStyle;
        private GUIStyle _tooltipsLabelStyle;


        private void OnEnable()
        {
            // Delete old version key
            EditorPrefs.DeleteKey("GBG.AssetQuickAccess.SettingsPrefs");

            LoadSettings();

            CreateView();
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

        private void OnGUI()
        {
            // Asset list scroll view
            _assetListScrollPos = EditorGUILayout.BeginScrollView(_assetListScrollPos);
            _reorderableList.DoLayoutList();
            TryRemoveAsset();
            EditorGUILayout.EndScrollView();

            // Tool tips
            if (_tooltipsLabelStyle == null)
            {
                _tooltipsLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter
                };
            }

            GUILayout.Label("Drag and drop the asset here to add a new item.\n" +
                            "Click with the middle mouse button to remove the item.",
                _tooltipsLabelStyle);

            // Allow drag and drop asset to window
            if (mouseOverWindow == this)
            {
                if (Event.current.type == EventType.DragUpdated && DragAndDrop.paths.Length > 0)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                }
                else if (Event.current.type == EventType.DragExited)
                {
                    Focus();
                    if (DragAndDrop.paths != null)
                    {
                        OnDropAssets(DragAndDrop.objectReferences);
                        Event.current.Use();
                    }
                }
            }
        }

        private void CreateView()
        {
            _reorderableList = new ReorderableList(_settings.AssetHandles,
                typeof(AssetHandle), true, false, false, false);
            _reorderableList.elementHeightCallback = GetAssetListItemHeight;
            _reorderableList.drawElementCallback = DrawAssetListItem;
            _reorderableList.onReorderCallback = OnReorderAssetList;
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


        #region Asset List View

        private ReorderableList _reorderableList;

        private static Texture _warningTexture;

        private Vector2 _assetListScrollPos;

        private static readonly TimeSpan _doubleClickSpan = new TimeSpan(0, 0, 0, 0, 400);

        private DateTime _lastClickAssetTime;

        private UObject _lastClickedAsset;

        private AssetHandle _assetToRemove;


        private float GetAssetListItemHeight(int index)
        {
            return 24;
        }

        private void DrawAssetListItem(Rect rect, int index, bool isActive, bool isFocused)
        {
            GUILayout.BeginHorizontal();

            var assetHandle = _settings.AssetHandles[index];

            // asset icon
            var iconRect = new Rect
            {
                position = rect.position,
                width = rect.height,
                height = rect.height
            };
            Texture assetIcon = AssetPreview.GetMiniThumbnail(assetHandle.Asset);
            if (!assetIcon)
            {
                if (!_warningTexture)
                {
                    _warningTexture = EditorGUIUtility.IconContent("d_console.warnicon").image;
                }

                assetIcon = _warningTexture;
            }

            GUI.DrawTexture(iconRect, assetIcon);

            // asset button
            var buttonRect = new Rect
            {
                x = rect.x + iconRect.width,
                y = rect.y,
                width = rect.width - iconRect.width,
                height = rect.height
            };
            if (_assetItemButtonStyle == null)
            {
                _assetItemButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleLeft
                };
            }

            if (GUI.Button(buttonRect, _settings.AssetHandles[index].GetDisplayName(),
                    _assetItemButtonStyle))
            {
                if (Event.current.button == 0)
                {
                    OnLeftClickAssetListItem(_settings.AssetHandles[index]);
                }
                else if (Event.current.button == 2)
                {
                    OnMiddleClickAssetListItem(_settings.AssetHandles[index]);
                }
            }

            GUILayout.EndHorizontal();
        }

        private void OnReorderAssetList(ReorderableList list)
        {
            _settings.MarkDirty();
            _isSettingsDirty = true;
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

        private void OnMiddleClickAssetListItem(AssetHandle handle)
        {
            _assetToRemove = handle;
        }

        private void OnDropAssets(IList<UObject> assets)
        {
            for (int i = 0; i < assets.Count; i++)
            {
                _settings.AddAsset(assets[i]);
            }

            _isSettingsDirty = true;
        }

        private void TryRemoveAsset()
        {
            if (_assetToRemove == null)
            {
                return;
            }

            EditorGUIUtility.PingObject(_assetToRemove.Asset);

            _settings.RemoveAsset(_assetToRemove);
            _isSettingsDirty = true;
            _assetToRemove = null;
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
            var rawData = string.Format("{0}\n{1}", _settingsPrefsKey, persistentGuids);
            UDebug.Log(rawData);
        }

        #endregion
    }
}
#endif
