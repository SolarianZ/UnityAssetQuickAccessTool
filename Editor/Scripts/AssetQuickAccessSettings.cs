using System;
using System.Collections.Generic;
using System.Text;
using UObject = UnityEngine.Object;

namespace GBG.AssetQuickAccess.Editor
{
    internal class AssetQuickAccessSettings
    {
        public IReadOnlyList<AssetHandle> AssetHandles => _assetHandles;

        private readonly List<AssetHandle> _assetHandles = new List<AssetHandle>();

        private static readonly char _guidSeparator = ';';

        private readonly StringBuilder _stringBuilder = new StringBuilder();

        private bool _isStringBuilderDirty = true;


        public AssetQuickAccessSettings(string persistentGuids)
        {
            if (string.IsNullOrEmpty(persistentGuids))
            {
                return;
            }

            var guids = persistentGuids.Split(_guidSeparator, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < guids.Length; i++)
            {
                var guid = guids[i];
                var assetHandle = new AssetHandle(guid);
                _assetHandles.Add(assetHandle);
            }
        }

        public void MarkDirty()
        {
            _isStringBuilderDirty = true;
        }

        public void AddAsset(UObject asset)
        {
            var assetHandle = _assetHandles.Find(handle => handle.Asset && handle.Asset == asset);
            if (assetHandle != null)
            {
                return;
            }

            assetHandle = new AssetHandle(asset);
            _assetHandles.Add(assetHandle);

            MarkDirty();
        }

        public bool RemoveAsset(AssetHandle handle)
        {
            if (_assetHandles.Remove(handle))
            {
                MarkDirty();
                return true;
            }

            return false;
        }

        public void ClearAllAssets()
        {
            if (_assetHandles.Count == 0)
            {
                return;
            }

            _assetHandles.Clear();

            MarkDirty();
        }

        public override string ToString()
        {
            if (_isStringBuilderDirty)
            {
                _stringBuilder.Clear();

                for (int i = 0; i < _assetHandles.Count; i++)
                {
                    var assetHandle = _assetHandles[i];
                    _stringBuilder.Append(assetHandle.Guid).Append(_guidSeparator);
                }

                _isStringBuilderDirty = false;
            }

            return _stringBuilder.ToString();
        }
    }
}
