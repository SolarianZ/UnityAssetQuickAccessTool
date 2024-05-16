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

        [SerializeField]
        private List<AssetHandle> _assetHandles = new List<AssetHandle>();


        public bool AddAsset(string assetPath)
        {
            string assetGuid = AssetDatabase.AssetPathToGUID(assetPath, AssetPathToGUIDOptions.OnlyExistingAssets);
            if (string.IsNullOrEmpty(assetGuid))
            {
                Debug.LogError($"Can not load asset at path '{assetPath}'.");
                return false;
            }

            UObject asset = AssetDatabase.LoadAssetAtPath<UObject>(assetPath);
            if (_assetHandles.Any(handle => handle.Asset == asset))
            {
                return false;
            }

            AssetHandle handle = new AssetHandle(asset);
            _assetHandles.Add(handle);
            ForceSave();

            return true;
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

        public void PrintAllAssets()
        {
            StringBuilder sb = new StringBuilder();
            foreach (AssetHandle handle in _assetHandles)
            {
                sb.AppendLine(handle.ToString());
            }

            Debug.Log(sb.ToString());
        }

        public void ForceSave()
        {
            Save(true);
        }
    }
}
