using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
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
            GetWindow<AssetQuickAccessWindow>();
        }


        private AssetQuickAccessLocalCache LocalCache => AssetQuickAccessLocalCache.instance;


        private void OnEnable()
        {
            titleContent = EditorGUIUtility.IconContent(
                EditorGUIUtility.isProSkin ? "d_Favorite" : "Favorite");
            titleContent.text = "Asset Quick Access";
            minSize = new Vector2(330, 180);

            AssemblyReloadEvents.afterAssemblyReload -= RefreshData;
            AssemblyReloadEvents.afterAssemblyReload += RefreshData;

            /** After changing the storage method of local data to ScriptableSingleton<T>, this process is no longer necessary
             * // Fix #5
             * EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
             * EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
             */
        }

        private void OnDisable()
        {
            AssemblyReloadEvents.afterAssemblyReload -= RefreshData;

            /** After changing the storage method of local data to ScriptableSingleton<T>, this process is no longer necessary
            * // Fix #5
            * EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            */
        }

        private void ShowButton(Rect position)
        {
            if (GUI.Button(position, EditorGUIUtility.IconContent("_Help"), GUI.skin.FindStyle("IconButton")))
            {
                Application.OpenURL("https://github.com/SolarianZ/UnityAssetQuickAccessTool");
            }
        }

        private void CreateGUI()
        {
            #region Toolbar

            // Toolbar
            Toolbar toolbar = new Toolbar();
            rootVisualElement.Add(toolbar);

            const float CategoryButtonMarginRight = 8;
            RadioButtonGroup radioButtonGroup = new RadioButtonGroup();
#if UNITY_2022_2_OR_NEWER
            radioButtonGroup.Q(className: RadioButtonGroup.containerUssClassName).style.flexDirection = FlexDirection.Row; 
#endif
            radioButtonGroup.RegisterValueChangedCallback(SelectCategory);
            toolbar.Add(radioButtonGroup);

            // All Category
            RadioButton _allCategoryButton = new RadioButton()
            {
                text = "All",
                value = LocalCache.SelectedCategory == AssetCategory.None,
                style = { marginRight = CategoryButtonMarginRight }
            };
            radioButtonGroup.Add(_allCategoryButton);

            // Assets Category
            RadioButton _assetsCategoryButton = new RadioButton()
            {
                text = "Assets",
                value = LocalCache.SelectedCategory == AssetCategory.ProjectAsset,
                style = { marginRight = CategoryButtonMarginRight }
            };
            radioButtonGroup.Add(_assetsCategoryButton);

            // Scene Objects Category
            RadioButton _sceneObjectsCategoryButton = new RadioButton()
            {
                text = "Scene Objects",
                value = LocalCache.SelectedCategory == AssetCategory.SceneObject,
                style = { marginRight = CategoryButtonMarginRight }
            };
            radioButtonGroup.Add(_sceneObjectsCategoryButton);

            // External Files Category
            RadioButton _externalFilesCategoryButton = new RadioButton()
            {
                text = "External Files",
                value = LocalCache.SelectedCategory == AssetCategory.ExternalFile,
                style = { marginRight = CategoryButtonMarginRight }
            };
            radioButtonGroup.Add(_externalFilesCategoryButton);

            #endregion


            // Root canvas
            // Can not add drag and drop manipulator to rootVisualElement directly,
            // so we need an extra visual element(_rootCanvas) to handle drag and drop events
            _rootCanvas = new VisualElement() { style = { flexGrow = 1 } };
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
                itemsSource = LocalCache.AssetHandles,
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

        /** After changing the storage method of local data to ScriptableSingleton<T>, this process is no longer necessary
         * // Fix #5
         * private void OnPlayModeStateChanged(PlayModeStateChange change)
         * {
         *     // Fix #4
         *     // When entering PlayMode, the window will execute OnEnable and reload the data object.
         *     // When exiting PlayMode, the data object is destroyed, but OnEnable is not executed.
         *     // Therefore, we need to reassign the data source.
         *     if (change == PlayModeStateChange.EnteredEditMode)
         *     {
         *         _assetListView.itemsSource = AssetQuickAccessLocalCache.instance.AssetHandles;
         *     }
         * }
        */

        private void RefreshData()
        {
            // Fix #1
            EditorApplication.delayCall += () =>
            {
                _isViewDirty = true;
            };
        }

        private void SelectCategory(ChangeEvent<int> evt)
        {
            LocalCache.SelectedCategory = (AssetCategory)evt.newValue;
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
            AssetHandle assetHandle = (AssetHandle)LocalCache.AssetHandles[index];
            view.Bind(assetHandle);
        }

        private void UnbindAssetListItem(VisualElement element, int index)
        {
            AssetItemView view = (AssetItemView)element;
            view.Unbind();
        }

        private void OnReorderAsset(int fromIndex, int toIndex)
        {
            LocalCache.ForceSave();
        }

        private void RemoveAsset(AssetHandle assetHandle)
        {
            _isViewDirty |= LocalCache.RemoveAsset(assetHandle);
        }

        private void OnDragAndDropAssets(IList<string> assetPaths)
        {
            for (int i = 0; i < assetPaths.Count; i++)
            {
                _isViewDirty |= LocalCache.AddAsset(assetPaths[i]);
            }
        }

        #endregion


        #region Custom menu

        void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Clear All Items"), false, () =>
            {
                LocalCache.ClearAllAssets();
                _isViewDirty = true;
            });
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
                Selection.activeObject = LocalCache;
            });
        }

        #endregion
    }
}
