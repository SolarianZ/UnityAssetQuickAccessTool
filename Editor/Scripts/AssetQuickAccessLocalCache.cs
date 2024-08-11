using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UObject = UnityEngine.Object;
using UDebug = UnityEngine.Debug;

namespace GBG.AssetQuickAccess.Editor
{
    [FilePath("Library/com.greenbamboogames.assetquickaccess/LocalCache.asset",
        FilePathAttribute.Location.ProjectFolder)]
    internal class AssetQuickAccessLocalCache : ScriptableSingleton<AssetQuickAccessLocalCache>
    {
        public IList<AssetHandle> AssetHandles => _assetHandles;
        public AssetCategory SelectedCategories
        {
            get { return _selectedCategory; }
            set
            {
                if (_selectedCategory == value) return;
                _selectedCategory = value;
                ForceSave();
            }
        }

        [SerializeField]
        private List<AssetHandle> _assetHandles = new List<AssetHandle>();
        [SerializeField]
        private AssetCategory _selectedCategory = AssetCategory.None;


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
                    if (errorsBuilder == null)
                    {
                        errorsBuilder = new StringBuilder();
                    }
                    errorsBuilder.AppendLine("File or folder already exists.");
                    continue;
                }

                AssetHandle handle = AssetHandle.CreateFromExternalFile(path, out string error);
                if (!string.IsNullOrEmpty(error))
                {
                    if (errorsBuilder == null)
                    {
                        errorsBuilder = new StringBuilder();
                    }

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
                        if (errorsBuilder == null)
                        {
                            errorsBuilder = new StringBuilder();
                        }
                        errorsBuilder.AppendLine("Asset already exists.");
                        continue;
                    }

                    AssetHandle handle = AssetHandle.CreateFromObject(obj, out string error);
                    if (!string.IsNullOrEmpty(error))
                    {
                        if (errorsBuilder == null)
                        {
                            errorsBuilder = new StringBuilder();
                        }

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
                        if (errorsBuilder == null)
                        {
                            errorsBuilder = new StringBuilder();
                        }
                        errorsBuilder.AppendLine("Object already exists.");
                        continue;
                    }

                    AssetHandle handle = AssetHandle.CreateFromObject(obj, out string error);
                    if (!string.IsNullOrEmpty(error))
                    {
                        if (errorsBuilder == null)
                        {
                            errorsBuilder = new StringBuilder();
                        }

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

        public bool AddUrls(HashSet<string> urls, ref StringBuilder errorsBuilder, bool clearErrorsBuilder)
        {
            if (clearErrorsBuilder)
            {
                errorsBuilder?.Clear();
            }

            bool added = false;
            foreach (string url in urls)
            {
                if (_assetHandles.Any(h => h.GetAssetPath() == url))
                {
                    if (errorsBuilder == null)
                    {
                        errorsBuilder = new StringBuilder();
                    }
                    errorsBuilder.AppendLine("URL already exists.");
                    continue;
                }

                AssetHandle handle = AssetHandle.CreateFromUrl(url, out string error);
                if (!string.IsNullOrEmpty(error))
                {
                    if (errorsBuilder == null)
                    {
                        errorsBuilder = new StringBuilder();
                    }

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

            UDebug.Log("<b>[Asset Quick Access]</b> All asset quick access items removed.");
        }

        public void ForceSave()
        {
            Save(true);
        }
    }
}
