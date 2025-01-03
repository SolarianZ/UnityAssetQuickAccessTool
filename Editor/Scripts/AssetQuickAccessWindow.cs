﻿using System.Collections;
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

        public static void AddItems(IList<UObject> objects, IList<string> paths,
            IList<(string url, string title)> urlInfos, IList<(string menuPath, string title)> menuItemInfos)
        {
            HashSet<UObject> objectHashSet = new HashSet<UObject>(objects?.Count ?? 0);
            if (objects != null)
            {
                objectHashSet = new HashSet<UObject>(objects);
            }

            HashSet<string> pathHashSet = null;
            if (paths != null)
            {
                pathHashSet = new HashSet<string>();
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
                        pathHashSet.Add(rawPath);
                    }
                }
            }

            StringBuilder errorsBuilder = null;
            bool added = AssetQuickAccessLocalCache.instance.AddObjects(objectHashSet, ref errorsBuilder, false);
            if (pathHashSet != null)
            {
                added |= AssetQuickAccessLocalCache.instance.AddExternalPaths(pathHashSet, ref errorsBuilder, false);
            }

            if (urlInfos != null)
            {
                Dictionary<string, (string url, string title)> urlDict = new Dictionary<string, (string url, string title)>();
                for (int i = 0; i < urlInfos.Count; i++)
                {
                    string url = urlInfos[i].url;
                    if (!urlDict.ContainsKey(url))
                    {
                        urlDict.Add(url, urlInfos[i]);
                    }
                }

                added |= AssetQuickAccessLocalCache.instance.AddUrls(urlDict.Values, ref errorsBuilder, false);
            }

            if (menuItemInfos != null)
            {
                Dictionary<string, (string menuPath, string title)> menuDict = new Dictionary<string, (string menuPath, string title)>();
                for (int i = 0; i < menuItemInfos.Count; i++)
                {
                    string menuPath = menuItemInfos[i].menuPath;
                    if (!menuDict.ContainsKey(menuPath))
                    {
                        menuDict.Add(menuPath, menuItemInfos[i]);
                    }
                }

                added |= AssetQuickAccessLocalCache.instance.AddMenuItems(menuDict.Values, ref errorsBuilder, false);
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
            AddItems(Selection.objects, null, null, null);
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
                AddItems(new UObject[] { command.context }, null, null, null);
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

            titleContent = EditorGUIUtility.IconContent("Favorite");
            titleContent.text = "Asset Quick Access";
            minSize = new Vector2(260, 180);

            AssemblyReloadEvents.afterAssemblyReload -= SetViewDirtyDelay;
            AssemblyReloadEvents.afterAssemblyReload += SetViewDirtyDelay;
            EditorApplication.hierarchyChanged -= SetViewDirty;
            EditorApplication.hierarchyChanged += SetViewDirty;


            #region Remove Old Version Items

            bool oldItemsUpgraded = false;
            for (int i = LocalCache.AssetHandles.Count - 1; i >= 0; i--)
            {
                AssetHandle assetHandle = LocalCache.AssetHandles[i];
                if (assetHandle.Category == AssetCategory.None)
                {
                    assetHandle.UpgradeOldVersionData();
                    oldItemsUpgraded = true;
                }
            }

            if (oldItemsUpgraded)
            {
                LocalCache.ForceSave();
            }

            #endregion
        }

        private void OnDisable()
        {
            _instance = null;

            AssemblyReloadEvents.afterAssemblyReload -= SetViewDirtyDelay;
            EditorApplication.hierarchyChanged -= SetViewDirty;
        }

        private void OnFocus()
        {
            if (_setViewDirtyOnFocus)
            {
                _isViewDirty = true;
            }
        }

        private void ShowButton(Rect pos)
        {
            if (GUI.Button(pos, EditorGUIUtility.IconContent("_Help"), EditorStyles.iconButton))
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
                style =
                {
                    height = StyleKeyword.Auto,
                    justifyContent = Justify.SpaceBetween,
                }
            };
            rootVisualElement.Add(toolbar);

            // Radio Button Group
            const float CategoryButtonMarginRight = 8;
            RadioButtonGroup radioButtonGroup = new RadioButtonGroup
            {
                style = { flexShrink = 1 },
            };
#if UNITY_2022_2_OR_NEWER
            VisualElement radioButtonGroupContainer = radioButtonGroup.Q(className: RadioButtonGroup.containerUssClassName);
            radioButtonGroupContainer.style.flexDirection = FlexDirection.Row;
            radioButtonGroupContainer.style.flexWrap = Wrap.Wrap;
#else
            radioButtonGroup.style.flexWrap = Wrap.Wrap;
#endif
            //radioButtonGroup.RegisterValueChangedCallback(SelectCategory); // Not work in Unity 2021
            toolbar.Add(radioButtonGroup);

            // All Category
            RadioButton allCategoryButton = new RadioButton()
            {
                text = "All",
                value = LocalCache.SelectedCategories == AssetCategory.None,
                style = { marginRight = CategoryButtonMarginRight }
            };
            allCategoryButton.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                    SelectCategory(AssetCategory.None);
            });
            radioButtonGroup.Add(allCategoryButton);

            // Project Assets Category
            RadioButton assetsCategoryButton = new RadioButton()
            {
                text = "Assets",
                value = LocalCache.SelectedCategories == AssetCategory.ProjectAsset,
                style = { marginRight = CategoryButtonMarginRight }
            };
            assetsCategoryButton.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                    SelectCategory(AssetCategory.ProjectAsset);
            });
            radioButtonGroup.Add(assetsCategoryButton);

            // Scene Objects Category
            RadioButton sceneObjectsCategoryButton = new RadioButton()
            {
                text = "Scene Objects",
                value = LocalCache.SelectedCategories == AssetCategory.SceneObject,
                style = { marginRight = CategoryButtonMarginRight }
            };
            sceneObjectsCategoryButton.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                    SelectCategory(AssetCategory.SceneObject);
            });
            radioButtonGroup.Add(sceneObjectsCategoryButton);

            // External Files Category
            RadioButton externalFilesCategoryButton = new RadioButton()
            {
                text = "External Items",
                value = LocalCache.SelectedCategories == (AssetCategory.ExternalFile | AssetCategory.Url),
                style = { marginRight = CategoryButtonMarginRight }
            };
            externalFilesCategoryButton.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                    SelectCategory(AssetCategory.ExternalFile | AssetCategory.Url);
            });
            radioButtonGroup.Add(externalFilesCategoryButton);

            // MenuItem Category
            RadioButton menuItemCategoryButton = new RadioButton()
            {
                text = "Menu Items",
                value = LocalCache.SelectedCategories == AssetCategory.MenuItem,
                style = { marginRight = CategoryButtonMarginRight }
            };
            menuItemCategoryButton.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                    SelectCategory(AssetCategory.MenuItem);
            });
            radioButtonGroup.Add(menuItemCategoryButton);

            // Toolbar Menu
            ToolbarMenu toolbarMenu = new ToolbarMenu
            {
                tooltip = "Add External File or Folder",
                style = { flexShrink = 0 }
            };
            toolbarMenu.menu.AppendAction("Add Selected Objects", _ => AddSelectedObjects(), 
                _ => Selection.objects != null && Selection.objects.Length > 0 
                    ? DropdownMenuAction.Status.Normal 
                    : DropdownMenuAction.Status.Disabled);
            toolbarMenu.menu.AppendAction("Add External File", AddExternalFile);
            toolbarMenu.menu.AppendAction("Add External Folder", AddExternalFolder);
            toolbarMenu.menu.AppendAction("Add Menu Item", AddMenuItemEditor);
            toolbarMenu.menu.AppendAction("Add URL", AddUrlEditor);
            toolbarMenu.menu.AppendSeparator("");
            toolbarMenu.menu.AppendAction("Remove All Items", _ => RemoveAllItems());
            toolbar.Add(toolbarMenu);

            #endregion


            DragAndDropInManipulator dndInManipulator = new DragAndDropInManipulator(rootVisualElement);
            dndInManipulator.OnDragAndDrop += (objects, paths) => AddItems(objects, paths, null, null);

            // Asset list view
            _assetListView = new ListView
            {
                fixedItemHeight = 26,
                //reorderable = LocalCache.SelectedCategory == AssetCategory.None,
                reorderMode = ListViewReorderMode.Animated,
                makeItem = CreateNewAssetListItem,
                bindItem = BindAssetListItem,
                unbindItem = UnbindAssetListItem,
                //itemsSource = LocalCache.SelectedCategory == AssetCategory.None
                //    ? LocalCache.AssetHandles
                //    : _filteredAssetHandles,
                selectionType = SelectionType.None,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
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

            SetViewDirty();
        }

        private void Update()
        {
            if (!_isViewDirty)
            {
                return;
            }

            UpdateFilteredAssetHandles();

            if (LocalCache.SelectedCategories == AssetCategory.None)
            {
                _assetListView.itemsSource = LocalCache.AssetHandles as IList;
                _assetListView.reorderable = true;
            }
            else
            {
                _assetListView.itemsSource = _filteredAssetHandles;
                _assetListView.reorderable = false;
            }

            _assetListView.RefreshItems();
            _isViewDirty = false;
        }

        private void OnProjectChange()
        {
            SetViewDirty();
        }

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

        private void AddMenuItemEditor(DropdownMenuAction action)
        {
            Vector2 upperCenter = position.center;
            upperCenter.y = position.yMin + 50;
            MenuItemSelectWindow.Open(upperCenter, AddMenuItem);
        }

        private void AddUrlEditor(DropdownMenuAction action)
        {
            Vector2 upperCenter = position.center;
            upperCenter.y = position.yMin + 100;
            UrlEditWindow.Open(upperCenter, AddUrl);
        }

        private void AddMenuItem(string menuPath, string menuTitle)
        {
            if (string.IsNullOrWhiteSpace(menuPath))
            {
                return;
            }

            AddItems(null, null, null, new (string menuPath, string title)[] { (menuPath, menuTitle) });
        }

        private void AddUrl(string url, string menuTitle)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            AddItems(null, null, new (string url, string title)[] { (url, menuTitle) }, null);
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
            view.SetVerticalPadding(LocalCache.SelectedCategories == AssetCategory.None ? 0 : 1);
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