using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace GBG.AssetQuickAccess.Editor
{
    [FilePath("Library/com.greenbamboogames.assetquickaccess/LocalCache.asset",
        FilePathAttribute.Location.ProjectFolder)]
    internal class AssetQuickAccessLocalCache : ScriptableSingleton<AssetQuickAccessLocalCache>
    {
        public const int MaxRecentAssetCount = 16;

        public IList<AssetHandle> AssetHandles => _assetHandles;
        public AssetCategory SelectedCategories
        {
            get { return _selectedCategory; }
            set
            {
                if (_selectedCategory == value && !_isRecentAssetsSelected)
                    return;
                _selectedCategory = value;
                _isRecentAssetsSelected = false;
                ForceSave();
            }
        }
        public IList<AssetHandle> RecentAssetHandles => _recentAssetHandles;
        public bool IsRecentAssetsSelected
        {
            get { return _isRecentAssetsSelected; }
            set
            {
                if (_isRecentAssetsSelected == value)
                    return;
                _selectedCategory = AssetCategory.None;
                _isRecentAssetsSelected = value;
                ForceSave();
            }
        }

        [SerializeField]
        private List<AssetHandle> _assetHandles = new List<AssetHandle>();
        [SerializeField]
        private AssetCategory _selectedCategory = AssetCategory.None;
        [SerializeField]
        private List<AssetHandle> _recentAssetHandles = new List<AssetHandle>();
        [SerializeField]
        private bool _isRecentAssetsSelected;


        public bool AddExternalPaths(IEnumerable<string> paths, ref StringBuilder errorsBuilder, bool clearErrorsBuilder)
        {
            if (clearErrorsBuilder)
            {
                errorsBuilder?.Clear();
            }

            bool added = false;
            foreach (string path in paths)
            {
                if (_assetHandles.Any(h => h.GetAssetPath() == path))
                {
                    errorsBuilder ??= new StringBuilder();
                    errorsBuilder.AppendLine("File or folder already exists.");
                    continue;
                }

                AssetHandle handle = AssetHandle.CreateFromExternalFile(path, out string error);
                if (!string.IsNullOrEmpty(error))
                {
                    errorsBuilder ??= new StringBuilder();
                    errorsBuilder.AppendLine(error);
                }

                if (handle == null)
                {
                    continue;
                }

                _assetHandles.Add(handle);
                added = true;
            }

            if (added)
            {
                ForceSave();
            }

            return added;
        }

        public bool AddObjects(IEnumerable<UObject> objects, ref StringBuilder errorsBuilder, bool clearErrorsBuilder)
        {
            if (clearErrorsBuilder)
            {
                errorsBuilder?.Clear();
            }

            bool added = false;
            foreach (UObject obj in objects)
            {
                if (EditorUtility.IsPersistent(obj))
                {
                    if (_assetHandles.Any(h => h.Asset == obj))
                    {
                        errorsBuilder ??= new StringBuilder();
                        errorsBuilder.AppendLine("Asset already exists.");
                        continue;
                    }

                    AssetHandle handle = AssetHandle.CreateFromObject(obj, out string error);
                    if (!string.IsNullOrEmpty(error))
                    {
                        errorsBuilder ??= new StringBuilder();
                        errorsBuilder.AppendLine(error);
                    }

                    if (handle == null)
                    {
                        continue;
                    }

                    _assetHandles.Add(handle);
                    added = true;
                }
                else
                {
                    if (_assetHandles.Any(h => h.Asset == obj))
                    {
                        errorsBuilder ??= new StringBuilder();
                        errorsBuilder.AppendLine("Object already exists.");
                        continue;
                    }

                    AssetHandle handle = AssetHandle.CreateFromObject(obj, out string error);
                    if (!string.IsNullOrEmpty(error))
                    {
                        errorsBuilder ??= new StringBuilder();
                        errorsBuilder.AppendLine(error);
                    }

                    if (handle == null)
                    {
                        continue;
                    }

                    if (_assetHandles.Any(h => h.Guid == handle.Guid))
                    {
                        continue;
                    }

                    _assetHandles.Add(handle);
                    added = true;
                }
            }

            if (added)
            {
                ForceSave();
            }

            return added;
        }

        public bool AddUrls(IEnumerable<(string url, string title)> urlInfos, ref StringBuilder errorsBuilder, bool clearErrorsBuilder)
        {
            if (clearErrorsBuilder)
            {
                errorsBuilder?.Clear();
            }

            bool added = false;
            foreach ((string url, string title) urlInfo in urlInfos)
            {
                if (_assetHandles.Any(h => h.GetAssetPath() == urlInfo.url))
                {
                    errorsBuilder ??= new StringBuilder();
                    errorsBuilder.AppendLine("URL already exists.");
                    continue;
                }

                AssetHandle handle = AssetHandle.CreateFromUrl(urlInfo.url, urlInfo.title, out string error);
                if (!string.IsNullOrEmpty(error))
                {
                    errorsBuilder ??= new StringBuilder();
                    errorsBuilder.AppendLine(error);
                }

                if (handle == null)
                {
                    continue;
                }

                _assetHandles.Add(handle);
                added = true;
            }

            if (added)
            {
                ForceSave();
            }

            return added;
        }

        public bool AddMenuItems(IEnumerable<(string menuPath, string title)> menuItemInfos, ref StringBuilder errorsBuilder, bool clearErrorsBuilder)
        {
            if (clearErrorsBuilder)
            {
                errorsBuilder?.Clear();
            }

            bool added = false;
            foreach ((string menuPath, string title) menuItemInfo in menuItemInfos)
            {
                if (_assetHandles.Any(h => h.GetAssetPath() == menuItemInfo.menuPath))
                {
                    errorsBuilder ??= new StringBuilder();
                    errorsBuilder.AppendLine("Menu item already exists.");
                    continue;
                }

                AssetHandle handle = AssetHandle.CreateFromMenuPath(menuItemInfo.menuPath, menuItemInfo.title, out string error);
                if (!string.IsNullOrEmpty(error))
                {
                    errorsBuilder ??= new StringBuilder();
                    errorsBuilder.AppendLine(error);
                }

                if (handle == null)
                {
                    continue;
                }

                _assetHandles.Add(handle);
                added = true;
            }

            if (added)
            {
                ForceSave();
            }

            return added;
        }

        public bool RemoveAsset(AssetHandle handle)
        {
            if (_assetHandles.Remove(handle))
            {
                ForceSave();
                return true;
            }

            return false;
        }

        public void RemoveAllAssets()
        {
            _assetHandles.Clear();
            ForceSave();

            Debug.Log("All asset quick access items removed.");
        }

        internal bool AddRecentAsset(UObject obj)
        {
            if (!obj)
                return false;

            if (!EditorUtility.IsPersistent(obj))
                return false;

            int removedCount = _recentAssetHandles.RemoveAll(h => !h.Asset);

            AssetHandle handle = AssetHandle.CreateFromObject(obj, out string error);
            int existingHandleIndex = _recentAssetHandles.FindIndex(h => h.Guid == handle.Guid);
            if (existingHandleIndex != -1)
            {
                _recentAssetHandles.RemoveAt(existingHandleIndex);
                _recentAssetHandles.Insert(0, handle);
                ForceSave();
                return true;
            }

            if (!string.IsNullOrEmpty(error))
                Debug.LogError($"Error on adding recent asset: {error}");

            if (handle == null)
            {
                if (removedCount > 0)
                    ForceSave();
                return false;
            }

            while (_recentAssetHandles.Count >= MaxRecentAssetCount)
                _recentAssetHandles.RemoveAt(MaxRecentAssetCount - 1);

            _recentAssetHandles.Insert(0, handle);
            ForceSave();

            return true;
        }

        public void RemoveAllRecentAssets()
        {
            _recentAssetHandles.Clear();
            ForceSave();
        }

        public void ForceSave()
        {
            Save(true);
        }
    }
}