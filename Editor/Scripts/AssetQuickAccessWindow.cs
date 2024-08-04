using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace GBG.AssetQuickAccess.Editor
{
    public partial class AssetQuickAccessWindow : EditorWindow, IHasCustomMenu
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

            SetViewDirty();
            CreateListView();
        }

        private void OnDisable()
        {
            _instance = null;

            AssemblyReloadEvents.afterAssemblyReload -= SetViewDirtyDelay;
            EditorApplication.hierarchyChanged -= SetViewDirty;
        }

        private void OnFocus()
        {
            // FIX: Gui styles may be lost
            ClearAllGuiStytleCaches();
        }

        private void ShowButton(Rect position)
        {
            if (GUI.Button(position, EditorGUIUtility.IconContent("_Help"), GUI.skin.FindStyle("IconButton")))
            {
                Application.OpenURL("https://github.com/SolarianZ/UnityAssetQuickAccessTool");
            }
        }

        private void OnProjectChange()
        {
            SetViewDirty();
        }

        private void SetViewDirty()
        {
            _isViewDirty = true;
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
                _filteredAssetHandles.AddRange(LocalCache.AssetHandles);
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

        private void AddExternalFile()
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

        private void AddExternalFolder()
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

        private void AddUrlEditor()
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