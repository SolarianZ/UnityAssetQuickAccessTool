#if !UNITY_2021_3_OR_NEWER
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UObject = UnityEngine.Object;

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
            LoadSettings();

            CreateView();
        }

        private void OnDisable()
        {
            SaveSettings();
        }

        private void OnGUI()
        {
            // Asset list scroll view
            _assetListScrollPos = EditorGUILayout.BeginScrollView(_assetListScrollPos);
            _reorderableList.DoLayoutList();
            EditorGUILayout.EndScrollView();

            // Tool tips
            if (_tooltipsLabelStyle == null)
            {
                _tooltipsLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter
                };
            }
            GUILayout.Label("Drag and drop asset here to record item.\n" +
                            "Click with middle mouse button to remove item.",
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
            _reorderableList = new ReorderableList((IList)_settings.AssetHandles,
                typeof(AssetHandle), true, false, false, false);
            _reorderableList.elementHeightCallback = GetAssetListItemHeight;
            _reorderableList.drawElementCallback = DrawAssetListItem;
            _reorderableList.onReorderCallback = OnReorderAssetList;
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

        private ReorderableList _reorderableList;

        private Vector2 _assetListScrollPos;

        private static readonly TimeSpan _doubleClickSpan = new TimeSpan(0, 0, 0, 0, 400);

        private DateTime _lastClickAssetTime;

        private UObject _lastClickedAsset;


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
            var assetIcon = AssetPreview.GetMiniThumbnail(assetHandle.Asset);
            if (assetIcon)
            {
                GUI.DrawTexture(iconRect, assetIcon);
            }
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
                else
                {
                    OnRightOrMiddleClickAssetListItem(_settings.AssetHandles[index]);
                }
            }

            GUILayout.EndHorizontal();
        }

        private void OnReorderAssetList(ReorderableList list)
        {
            _settings.MarkDirty();
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

        private void OnRightOrMiddleClickAssetListItem(AssetHandle handle)
        {
            EditorGUIUtility.PingObject(handle.Asset);

            _settings.RemoveAsset(handle);
        }

        private void OnDropAssets(IList<UObject> assets)
        {
            for (int i = 0; i < assets.Count; i++)
            {
                _settings.AddAsset(assets[i]);
            }
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

            EditorPrefs.DeleteKey(_settingsPrefsKey);
        }

        #endregion
    }
}
#endif
