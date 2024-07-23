using System.Collections;
using System.Collections.Generic;
using System.IO;
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
#if !GBG_AQA_HOTKEY_OFF
        [MenuItem("Tools/Bamboo/Asset Quick Access %q")]
#else
        [MenuItem("Tools/Bamboo/Asset Quick Access")]
#endif
        [MenuItem("Window/Asset Management/Asset Quick Access")]
        public static void Open()
        {
            GetWindow<AssetQuickAccessWindow>();
        }

        public static void AddItems(IList<UObject> objects, IList<string> paths, IList<string> urls)
        {
            HashSet<UObject> objectHashSet = new HashSet<UObject>();
            if (objects != null)
            {
                objectHashSet = new HashSet<UObject>(objects);
            }

            HashSet<string> stringHashSet = null; // For paths and urls
            if (paths != null)
            {
                stringHashSet = new HashSet<string>();
                foreach (string rawPath in paths)
                {
                    string path = rawPath.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    UObject asset = null;
                    if (path.StartsWith("Assets") || path.StartsWith("Packages"))
                    {
                        asset = AssetDatabase.LoadAssetAtPath<UObject>(path);
                    }
                    else if (path.StartsWith(Application.dataPath))
                    {
                        path = Path.Combine("Assets", path.Remove(0, Application.dataPath.Length));
                        asset = AssetDatabase.LoadAssetAtPath<UObject>(path);
                    }

                    if (asset)
                    {
                        objectHashSet.Add(asset);
                    }
                    else
                    {
                        stringHashSet.Add(rawPath);
                    }
                }
            }

            StringBuilder errorsBuilder = null;
            bool added = AssetQuickAccessLocalCache.instance.AddObjects(objectHashSet, ref errorsBuilder, false);
            if (stringHashSet != null)
            {
                added |= AssetQuickAccessLocalCache.instance.AddExternalPaths(stringHashSet, ref errorsBuilder, false);
            }

            if (urls != null)
            {
                stringHashSet?.Clear();
                if (stringHashSet == null)
                {
                    stringHashSet = new HashSet<string>();
                }
                for (int i = 0; i < urls.Count; i++)
                {
                    stringHashSet.Add(urls[i]);
                }

                added |= AssetQuickAccessLocalCache.instance.AddUrls(stringHashSet, ref errorsBuilder, false);
            }

            if (_instance)
            {
                if (added)
                {
                    _instance.SetViewDirty();
                }

                if (errorsBuilder != null && errorsBuilder.Length > 0)
                {
                    _instance.ShowNotification(new GUIContent(errorsBuilder.ToString()));
                }
            }
        }

#if !GBG_AQA_CONTEXT_MENU_OFF
        [MenuItem("Assets/Bamboo/Add to Asset Quick Access")]
        [MenuItem("GameObject/Bamboo/Add to Asset Quick Access")]
#endif
        public static void AddSelectedObjects()
        {
            AddItems(Selection.objects, null, null);
        }

        [MenuItem("Assets/Bamboo/Add to Asset Quick Access", validate = true)]
        [MenuItem("GameObject/Bamboo/Add to Asset Quick Access", validate = true)]
        private static bool AddSelectedObjectsValidate() => Selection.objects.Length > 0;

#if !GBG_AQA_CONTEXT_MENU_OFF
        [MenuItem("CONTEXT/Component/Bamboo/Add to Asset Quick Access")]
        public static void AddContextObject(MenuCommand command)
        {
            if (command.context)
            {
                AddItems(new UObject[] { command.context }, null, null);
            }
        }
#endif


        private static AssetQuickAccessWindow _instance;
        private bool _isViewDirty;
        private bool _setViewDirtyOnFocus;
        private List<AssetHandle> _filteredAssetHandles = new List<AssetHandle>();
        private AssetQuickAccessLocalCache LocalCache => AssetQuickAccessLocalCache.instance;


        private void OnEnable()
        {
            _instance = this;

            titleContent = EditorGUIUtility.IconContent(
                EditorGUIUtility.isProSkin ? "d_Favorite" : "Favorite");
            titleContent.text = "Asset Quick Access";
            minSize = new Vector2(330, 180);

            AssemblyReloadEvents.afterAssemblyReload -= SetViewDirtyDelay;
            AssemblyReloadEvents.afterAssemblyReload += SetViewDirtyDelay;
            EditorApplication.hierarchyChanged -= SetViewDirty;
            EditorApplication.hierarchyChanged += SetViewDirty;

            /** After changing the storage method of local data to ScriptableSingleton<T>, this process is no longer necessary
             * // Fix #5
             * EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
             * EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
             */

            CreateGUI_Manual();
        }

        private void OnDisable()
        {
            _instance = null;

            AssemblyReloadEvents.afterAssemblyReload -= SetViewDirtyDelay;
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

        private void CreateGUI_Manual()
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

//             // Radio Button Group
//             const float CategoryButtonMarginRight = 8;
//             RadioButtonGroup radioButtonGroup = new RadioButtonGroup
//             {
//                 style = { flexShrink = 1 },
//             };
// #if UNITY_2022_2_OR_NEWER
//             radioButtonGroup.Q(className: RadioButtonGroup.containerUssClassName).style.flexDirection = FlexDirection.Row;
// #endif
//             //radioButtonGroup.RegisterValueChangedCallback(SelectCategory); // Not work in Unity 2021
//             toolbar.Add(radioButtonGroup);
//
//             // All Category
//             RadioButton allCategoryButton = new RadioButton()
//             {
//                 text = "All",
//                 value = LocalCache.SelectedCategories == AssetCategory.None,
//                 style = { marginRight = CategoryButtonMarginRight }
//             };
//             allCategoryButton.RegisterValueChangedCallback(evt =>
//             {
//                 if (evt.newValue) SelectCategory(AssetCategory.None);
//             });
//             radioButtonGroup.Add(allCategoryButton);
//
//             // Project Assets Category
//             RadioButton assetsCategoryButton = new RadioButton()
//             {
//                 text = "Assets",
//                 value = LocalCache.SelectedCategories == AssetCategory.ProjectAsset,
//                 style = { marginRight = CategoryButtonMarginRight }
//             };
//             assetsCategoryButton.RegisterValueChangedCallback(evt =>
//             {
//                 if (evt.newValue) SelectCategory(AssetCategory.ProjectAsset);
//             });
//             radioButtonGroup.Add(assetsCategoryButton);
//
//             // Scene Objects Category
//             RadioButton sceneObjectsCategoryButton = new RadioButton()
//             {
//                 text = "Scene Objects",
//                 value = LocalCache.SelectedCategories == AssetCategory.SceneObject,
//                 style = { marginRight = CategoryButtonMarginRight }
//             };
//             sceneObjectsCategoryButton.RegisterValueChangedCallback(evt =>
//             {
//                 if (evt.newValue) SelectCategory(AssetCategory.SceneObject);
//             });
//             radioButtonGroup.Add(sceneObjectsCategoryButton);
//
//             // External Files Category
//             RadioButton externalFilesCategoryButton = new RadioButton()
//             {
//                 text = "External Items",
//                 value = LocalCache.SelectedCategories == (AssetCategory.ExternalFile | AssetCategory.Url),
//                 style = { marginRight = CategoryButtonMarginRight }
//             };
//             externalFilesCategoryButton.RegisterValueChangedCallback(evt =>
//             {
//                 if (evt.newValue) SelectCategory(AssetCategory.ExternalFile | AssetCategory.Url);
//             });
//             radioButtonGroup.Add(externalFilesCategoryButton);

            // Toolbar Menu
            ToolbarMenu toolbarMenu = new ToolbarMenu
            {
                tooltip = "Add External File or Folder",
                style = { flexShrink = 0 }
            };
            toolbarMenu.menu.AppendAction("Add External File", AddExternalFile);
            toolbarMenu.menu.AppendAction("Add External Folder", AddExternalFolder);
            toolbarMenu.menu.AppendAction("Add URL", AddUrlEditor);
            toolbarMenu.menu.AppendSeparator("");
            toolbarMenu.menu.AppendAction("Remove All Items", _ => RemoveAllItems());
            toolbar.Add(toolbarMenu);

            #endregion


            DragAndDropInManipulator dndInManipulator = new DragAndDropInManipulator(rootVisualElement);
            dndInManipulator.OnDragAndDrop += (objects, paths) => AddItems(objects, paths, null);

            // Asset list view
            _assetListView = new ListView
            {
                itemHeight = 26,
                //reorderable = LocalCache.SelectedCategory == AssetCategory.None,
                // reorderMode = ListViewReorderMode.Animated,
                makeItem = CreateNewAssetListItem,
                bindItem = BindAssetListItem,
                // unbindItem = UnbindAssetListItem,
                //itemsSource = LocalCache.SelectedCategory == AssetCategory.None
                //    ? LocalCache.AssetHandles
                //    : _filteredAssetHandles,
                selectionType = SelectionType.None,
                style =
                {
                    flexGrow = 1,
                    marginTop = 2,
                    minHeight = 40,
                }
            };
            // TODO: item index changed
            // _assetListView.itemIndexChanged += OnReorderAsset;
            rootVisualElement.Add(_assetListView);

            // Tooltip
            Label tipsText = new Label
            {
                text = "Drag the asset here to add a new item.",
                style =
                {
                    unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter),
                    // textOverflow = new StyleEnum<TextOverflow>(TextOverflow.Ellipsis),
                    height = 36
                }
            };
            rootVisualElement.Add(tipsText);

            SetViewDirty();
        }

        private void Update()
        {
            if (_isViewDirty)
            {
                UpdateFilteredAssetHandles();

                if (LocalCache.SelectedCategories == AssetCategory.None)
                {
                    _assetListView.itemsSource = LocalCache.AssetHandles as IList;
                    // _assetListView.reorderable = true;
                }
                else
                {
                    _assetListView.itemsSource = _filteredAssetHandles;
                    // _assetListView.reorderable = false;
                }

                _assetListView.Refresh();
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
            // if (hasFocus)
            {
                _isViewDirty = true;
            }
            // else
            {
                _setViewDirtyOnFocus = true;
            }
        }

        private void SetViewDirtyDelay()
        {
            // Fix #1
            EditorApplication.delayCall += SetViewDirty;
        }

        private void RemoveAsset(AssetHandle assetHandle)
        {
            if (LocalCache.RemoveAsset(assetHandle))
            {
                SetViewDirty();
            }
        }

        private void RemoveAllItems()
        {
            if (EditorUtility.DisplayDialog("Warning",
                    "You are about to remove all items. This action cannot be undone.\nDo you want to remove?",
                    "Remove", "Cancel"))
            {
                LocalCache.RemoveAllAssets();
                SetViewDirty();
            }
        }

        private void SelectCategory(AssetCategory selectedCategory)
        {
            LocalCache.SelectedCategories = selectedCategory;
            SetViewDirty();
        }

        private void UpdateFilteredAssetHandles()
        {
            _filteredAssetHandles.Clear();
            if (LocalCache.SelectedCategories == AssetCategory.None)
            {
                return;
            }

            foreach (AssetHandle assetHandle in LocalCache.AssetHandles)
            {
                if (assetHandle.CheckFilter(LocalCache.SelectedCategories))
                {
                    _filteredAssetHandles.Add(assetHandle);
                }
            }
        }


        #region Toolbar

        private void AddExternalFile(DropdownMenuAction action)
        {
            string filePath = EditorUtility.OpenFilePanel("Select File", null, null);
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            StringBuilder errorsBuilder = null;
            if (LocalCache.AddExternalPaths(new string[] { filePath }, ref errorsBuilder, false))
            {
                SetViewDirty();
            }

            if (errorsBuilder != null && errorsBuilder.Length > 0)
            {
                ShowNotification(new GUIContent(errorsBuilder.ToString()));
            }
        }

        private void AddExternalFolder(DropdownMenuAction action)
        {
            string folderPath = EditorUtility.OpenFolderPanel("Select Folder", null, null);
            if (string.IsNullOrEmpty(folderPath))
            {
                return;
            }

            StringBuilder errorsBuilder = null;
            if (LocalCache.AddExternalPaths(new string[] { folderPath }, ref errorsBuilder, false))
            {
                SetViewDirty();
            }

            if (errorsBuilder != null && errorsBuilder.Length > 0)
            {
                ShowNotification(new GUIContent(errorsBuilder.ToString()));
            }
        }

        private void AddUrlEditor(DropdownMenuAction action)
        {
            Vector2 center = position.center;
            center.y = position.yMin + 100;
            UrlEditWindow.Open(center, AddUrl);
        }

        private void AddUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            AddItems(null, null, new string[] { url });
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
            AssetHandle assetHandle = (AssetHandle)_assetListView.itemsSource[index];
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