using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UObject = UnityEngine.Object;

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

        public static void AddItems(IList<UObject> objects, IList<string> paths)
        {
            HashSet<UObject> objectHashSet = new HashSet<UObject>(objects?.Count ?? 0);
            if (objects != null)
            {
                objectHashSet = new HashSet<UObject>(objects);
            }

            HashSet<string> externalPathHashSet = null;
            if (paths != null)
            {
                externalPathHashSet = new HashSet<string>(paths?.Count ?? 0);
                foreach (string path in paths)
                {
                    UObject asset = AssetDatabase.LoadAssetAtPath<UObject>(path);
                    if (asset)
                    {
                        objectHashSet.Add(asset);
                    }
                    else
                    {
                        externalPathHashSet.Add(path);
                    }
                }
            }

            StringBuilder errorsBuilder = null;
            bool added = AssetQuickAccessLocalCache.instance.AddObjects(objectHashSet, ref errorsBuilder, false);
            if (externalPathHashSet != null)
            {
                added |= AssetQuickAccessLocalCache.instance.AddExternalFiles(paths, ref errorsBuilder, false);
            }

            if (_instance)
            {
                _instance._isViewDirty |= added;

                if (errorsBuilder != null && errorsBuilder.Length > 0)
                {
                    _instance.ShowNotification(new GUIContent(errorsBuilder.ToString()));
                }
            }
        }

#if !GBG_AQA_CONTEXT_MENU_OFF
        [MenuItem("Assets/Bamboo/Add to Asset Quick Access", priority = 20)]
        [MenuItem("GameObject/Bamboo/Add to Asset Quick Access", priority = 0)]
#endif
        public static void AddSelectedObjects()
        {
            AddItems(Selection.objects, null);
        }

        [MenuItem("Assets/Bamboo/Add to Asset Quick Access", validate = true)]
        [MenuItem("GameObject/Bamboo/Add to Asset Quick Access", validate = true)]
        private static bool AddSelectedObjectsValidate() => Selection.objects.Length > 0;


        private static AssetQuickAccessWindow _instance;
        private bool _isViewDirty;
        private bool _setViewDirtyOnFocus;
        private AssetQuickAccessLocalCache LocalCache => AssetQuickAccessLocalCache.instance;


        private void OnEnable()
        {
            _instance = this;

            titleContent = EditorGUIUtility.IconContent(
                EditorGUIUtility.isProSkin ? "d_Favorite" : "Favorite");
            titleContent.text = "Asset Quick Access";
            minSize = new Vector2(330, 180);

            AssemblyReloadEvents.afterAssemblyReload -= RefreshDataDelay;
            AssemblyReloadEvents.afterAssemblyReload += RefreshDataDelay;
            EditorApplication.hierarchyChanged -= SetViewDirty;
            EditorApplication.hierarchyChanged += SetViewDirty;

            /** After changing the storage method of local data to ScriptableSingleton<T>, this process is no longer necessary
             * // Fix #5
             * EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
             * EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
             */
        }

        private void OnDisable()
        {
            _instance = null;

            AssemblyReloadEvents.afterAssemblyReload -= RefreshDataDelay;
            EditorApplication.hierarchyChanged -= SetViewDirty;

            /** After changing the storage method of local data to ScriptableSingleton<T>, this process is no longer necessary
            * // Fix #5
            * EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            */
        }

        private void OnFocus()
        {
            if (_setViewDirtyOnFocus)
            {
                _isViewDirty = true;
            }
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
            // For add drag and drop
            rootVisualElement.pickingMode = PickingMode.Position;


            #region Toolbar

            // Toolbar
            Toolbar toolbar = new Toolbar
            {
                style = { justifyContent = Justify.SpaceBetween }
            };
            rootVisualElement.Add(toolbar);

            // Radio Button Group
            const float CategoryButtonMarginRight = 8;
            RadioButtonGroup radioButtonGroup = new RadioButtonGroup
            {
                style = { flexShrink = 1 },
            };
#if UNITY_2022_2_OR_NEWER
            radioButtonGroup.Q(className: RadioButtonGroup.containerUssClassName).style.flexDirection = FlexDirection.Row;
#endif
            //radioButtonGroup.RegisterValueChangedCallback(SelectCategory); // Not work in Unity 2021
            toolbar.Add(radioButtonGroup);

            // All Category
            RadioButton allCategoryButton = new RadioButton()
            {
                text = "All",
                value = LocalCache.SelectedCategory == AssetCategory.None,
                style = { marginRight = CategoryButtonMarginRight }
            };
            allCategoryButton.RegisterValueChangedCallback(evt => { if (evt.newValue) SelectCategory(AssetCategory.None); });
            radioButtonGroup.Add(allCategoryButton);

            // Project Assets Category
            RadioButton assetsCategoryButton = new RadioButton()
            {
                text = "Assets",
                value = LocalCache.SelectedCategory == AssetCategory.ProjectAsset,
                style = { marginRight = CategoryButtonMarginRight }
            };
            assetsCategoryButton.RegisterValueChangedCallback(evt => { if (evt.newValue) SelectCategory(AssetCategory.ProjectAsset); });
            radioButtonGroup.Add(assetsCategoryButton);

            // Scene Objects Category
            RadioButton sceneObjectsCategoryButton = new RadioButton()
            {
                text = "Scene Objects",
                value = LocalCache.SelectedCategory == AssetCategory.SceneObject,
                style = { marginRight = CategoryButtonMarginRight }
            };
            sceneObjectsCategoryButton.RegisterValueChangedCallback(evt => { if (evt.newValue) SelectCategory(AssetCategory.SceneObject); });
            radioButtonGroup.Add(sceneObjectsCategoryButton);

            // External Files Category
            RadioButton externalFilesCategoryButton = new RadioButton()
            {
                text = "External Files",
                value = LocalCache.SelectedCategory == AssetCategory.ExternalFile,
                style = { marginRight = CategoryButtonMarginRight }
            };
            externalFilesCategoryButton.RegisterValueChangedCallback(evt => { if (evt.newValue) SelectCategory(AssetCategory.ExternalFile); });
            radioButtonGroup.Add(externalFilesCategoryButton);

            // Toolbar Menu
            ToolbarMenu toolbarMenu = new ToolbarMenu
            {
                tooltip = "Add External File or Folder",
                style = { flexShrink = 0 }
            };
            toolbarMenu.menu.AppendAction("Add External File", _ => AddExternalFile());
            toolbarMenu.menu.AppendAction("Add External Folder", _ => AddExternalFolder());
            toolbarMenu.menu.AppendSeparator("");
            toolbarMenu.menu.AppendAction("Remove all Items", _ => RemoveAllItems());
            toolbar.Add(toolbarMenu);

            #endregion


            DragAndDropManipulator dragDropManipulator = new DragAndDropManipulator(rootVisualElement);
            dragDropManipulator.OnDragAndDrop += AddItems;

            // Asset list view
            _assetListView = new ListView
            {
                fixedItemHeight = 26,
                reorderable = LocalCache.SelectedCategory == AssetCategory.None,
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
            rootVisualElement.Add(_assetListView);

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
            rootVisualElement.Add(tipsText);

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
            SetViewDirty();
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

        private void SetViewDirty()
        {
            if (hasFocus)
            {
                _isViewDirty = true;
            }
            else
            {
                _setViewDirtyOnFocus = true;
            }
        }

        private void RefreshDataDelay()
        {
            // Fix #1
            EditorApplication.delayCall += () =>
            {
                _isViewDirty = true;
            };
        }

        private void RemoveAsset(AssetHandle assetHandle)
        {
            _isViewDirty |= LocalCache.RemoveAsset(assetHandle);
        }

        private void RemoveAllItems()
        {
            if (EditorUtility.DisplayDialog("Warning",
                "You are about to remove all items. This action cannot be undone.\nDo you want to remove?",
                "Remove", "Cancel"))
            {
                LocalCache.RemoveAllAssets();
                _isViewDirty = true;
            }
        }

        private void SelectCategory(AssetCategory selectedCategory)
        {
            LocalCache.SelectedCategory = selectedCategory;
            _assetListView.reorderable = selectedCategory == AssetCategory.None;

            // TODO: Filter asset items
        }


        #region Toolbar

        private void AddExternalFile()
        {
            string filePath = EditorUtility.OpenFilePanel("Select File", null, null);
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            StringBuilder errorsBuilder = null;
            _isViewDirty |= LocalCache.AddExternalFiles(new string[] { filePath }, ref errorsBuilder, false);
            if (errorsBuilder != null && errorsBuilder.Length > 0)
            {
                ShowNotification(new GUIContent(errorsBuilder.ToString()));
            }
        }

        private void AddExternalFolder()
        {
            string folderPath = EditorUtility.OpenFolderPanel("Select Folder", null, null);
            if (string.IsNullOrEmpty(folderPath))
            {
                return;
            }

            StringBuilder errorsBuilder = null;
            _isViewDirty |= LocalCache.AddExternalFiles(new string[] { folderPath }, ref errorsBuilder, false);
            if (errorsBuilder != null && errorsBuilder.Length > 0)
            {
                ShowNotification(new GUIContent(errorsBuilder.ToString()));
            }
        }

        #endregion


        #region Asset List View

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

        #endregion


        #region Custom menu

        void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
        {
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
