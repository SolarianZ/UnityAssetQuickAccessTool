using UnityEditor;
using UnityEngine;

namespace GBG.AssetQuickAccess.Editor
{
    internal class AssetHandle
    {
        public string Guid => _guid;
        private string _guid;

        public Object Asset
        {
            get { return _asset; ; }
            set
            {
                _asset = value;
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(_asset, out _guid, out long _);
            }
        }
        private Object _asset;


        public AssetHandle(string guid)
        {
            _guid = guid;
            var assetPath = AssetDatabase.GUIDToAssetPath(_guid);
            _asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
        }

        public AssetHandle(Object asset)
        {
            _asset = asset;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(_asset, out _guid, out long _);
        }

        public override string ToString()
        {
            return $"{(Asset ? Asset.name : "null")}@{(string.IsNullOrEmpty(Guid) ? "null" : Guid)}";
        }

        public string GetDisplayName()
        {
            if (Asset)
            {
                return $"{Asset.name} ({Asset.GetType().Name})";
            }

            return "Missing (Unknown)";
        }
    }
}