using System.Collections;
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
        public IList AssetHandles => _assetHandles;
        public AssetCategory SelectedCategory
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


        public bool AddExternalFiles(IEnumerable<string> filePaths, ref StringBuilder errorsBuilder)
        {
            errorsBuilder?.Clear();

            bool added = false;
            foreach (string path in filePaths)
            {
                if (_assetHandles.Any(h => h.GetAssetPath() == path))
                {
                    errorsBuilder ??= new StringBuilder();
                    errorsBuilder.AppendLine("File already exists.");
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

        public bool AddObjects(IEnumerable<UObject> objects, ref StringBuilder errorsBuilder)
        {
            errorsBuilder?.Clear();

            bool added = false;
            foreach (UObject obj in objects)
            {
                if (EditorUtility.IsPersistent(obj))
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

                    _assetHandles.Add(handle);
                    added = true;
                }
                else
                {
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

        public bool RemoveAsset(AssetHandle handle)
        {
            if (_assetHandles.Remove(handle))
            {
                ForceSave();
                return true;
            }

            return false;
        }

        public void ClearAllAssets()
        {
            _assetHandles.Clear();
            ForceSave();

            Debug.Log("All asset quick access items cleared.");
        }

        public void ForceSave()
        {
            Save(true);
        }
    }
}
