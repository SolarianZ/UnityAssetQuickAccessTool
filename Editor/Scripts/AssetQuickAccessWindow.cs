using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GBG.AssetQuickAccess.Editor
{
    public class AssetQuickAccessWindow : EditorWindow, IHasCustomMenu
    {
        [MenuItem("Tools/Bamboo/Asset Quick Access")]
        [MenuItem("Window/Asset Management/Asset Quick Access")]
        public static void Open()
        {
            AssetQuickAccessWindow window = GetWindow<AssetQuickAccessWindow>("Asset Quick Access");
            window.minSize = new Vector2(300, 120);
        }


        private void OnEnable()
        {
            AssemblyReloadEvents.afterAssemblyReload -= RefreshData;
            AssemblyReloadEvents.afterAssemblyReload += RefreshData;
            // Fix #5: After changing the storage method of local data to ScriptableSingleton<T>, this process is no longer necessary
            //EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            //EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            AssemblyReloadEvents.afterAssemblyReload -= RefreshData;
            // Fix #5: After changing the storage method of local data to ScriptableSingleton<T>, this process is no longer necessary
            //EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void CreateGUI()
        {
            // Root canvas
            // Can not add drag and drop manipulator to rootVisualElement directly,
            // so we need an extra visual element(_rootCanvas) to handle drag and drop events
            _rootCanvas = new VisualElement();
            _rootCanvas.StretchToParentSize();
            rootVisualElement.Add(_rootCanvas);
            DragAndDropManipulator dragDropManipulator = new DragAndDropManipulator(_rootCanvas);
            dragDropManipulator.OnDragAndDropAssets += OnDragAndDropAssets;

            // Asset list view
            _assetListView = new ListView
            {
                fixedItemHeight = 26,
                reorderable = true,
                reorderMode = ListViewReorderMode.Animated,
                makeItem = CreateNewAssetListItem,
                bindItem = BindAssetListItem,
                unbindItem = UnbindAssetListItem,
                itemsSource = AssetQuickAccessLocalCache.instance.AssetHandles,
                selectionType = SelectionType.None,
                style =
                {
                    flexGrow = 1,
                    marginTop = 2,
                    minHeight = 40,
                }
            };
            _assetListView.itemIndexChanged += OnReorderAsset;
            _rootCanvas.Add(_assetListView);

            // Tooltip
            Label tipsText = new Label
            {
                text = "Drag the asset here to add a new item.",
                style =
                {
                    unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter),
                    textOverflow = new StyleEnum<TextOverflow>(TextOverflow.Ellipsis),
                    height = 36
                }
            };
            _rootCanvas.Add(tipsText);

            _isViewDirty = true;
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

        // Fix #5: After changing the storage method of local data to ScriptableSingleton<T>, this process is no longer necessary
        //private void OnPlayModeStateChanged(PlayModeStateChange change)
        //{
        //    // Fix #4
        //    // When entering PlayMode, the window will execute OnEnable and reload the data object.
        //    // When exiting PlayMode, the data object is destroyed, but OnEnable is not executed.
        //    // Therefore, we need to reassign the data source.
        //    if (change == PlayModeStateChange.EnteredEditMode)
        //    {
        //        _assetListView.itemsSource = AssetQuickAccessLocalCache.instance.AssetHandles;
        //    }
        //}

        private void RefreshData()
        {
            // Fix #1
            EditorApplication.delayCall += () =>
            {
                _isViewDirty = true;
            };
        }


        #region Asset List View

        private VisualElement _rootCanvas;
        private bool _isViewDirty;
        private ListView _assetListView;


        private VisualElement CreateNewAssetListItem()
        {
            AssetItemView view = new AssetItemView();
            view.OnWantsToRemoveAssetItem += RemoveAsset;

            return view;
        }

        private void BindAssetListItem(VisualElement element, int index)
        {
            AssetItemView view = (AssetItemView)element;
            AssetHandle assetHandle = (AssetHandle)AssetQuickAccessLocalCache.instance.AssetHandles[index];
            view.Bind(assetHandle);
        }

        private void UnbindAssetListItem(VisualElement element, int index)
        {
            AssetItemView view = (AssetItemView)element;
            view.Unbind();
        }

        private void OnReorderAsset(int fromIndex, int toIndex)
        {
            AssetQuickAccessLocalCache.instance.ForceSave();
        }

        private void RemoveAsset(AssetHandle assetHandle)
        {
            _isViewDirty |= AssetQuickAccessLocalCache.instance.RemoveAsset(assetHandle);
        }

        private void OnDragAndDropAssets(IList<string> assetPaths)
        {
            for (int i = 0; i < assetPaths.Count; i++)
            {
                _isViewDirty |= AssetQuickAccessLocalCache.instance.AddAsset(assetPaths[i]);
            }
        }

        #endregion


        #region Custom menu

        void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Clear All Items"), false, () =>
            {
                AssetQuickAccessLocalCache.instance.ClearAllAssets();
                _isViewDirty = true;
            });
            menu.AddItem(new GUIContent("Print All Items"), false,
                AssetQuickAccessLocalCache.instance.PrintAllAssets);
            menu.AddSeparator("");

            // Source Code
            menu.AddItem(new GUIContent("Source Code"), false, () =>
            {
                Application.OpenURL("https://github.com/SolarianZ/UnityAssetQuickAccessTool");
            });
            menu.AddSeparator("");

            // Debug
            menu.AddItem(new GUIContent("[Debug] Inspect settings"), false, () =>
            {
                Selection.activeObject = AssetQuickAccessLocalCache.instance;
            });
        }

        #endregion
    }
}
