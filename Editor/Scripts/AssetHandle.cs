using UnityEditor;
using UnityEngine;

namespace GBG.AssetQuickAccess.Editor
{
    internal class AssetHandle
    {
        public string Guid { get { return _guid; } }
        private string _guid;

        public Object Asset
        {
            get { return _asset; ; }
            set
            {
                _asset = value;
                var assetPath = AssetDatabase.GetAssetPath(_asset);
                _guid = AssetDatabase.AssetPathToGUID(assetPath);
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
            var assetPath = AssetDatabase.GetAssetPath(_asset);
            _guid = AssetDatabase.AssetPathToGUID(assetPath);
        }

        public override string ToString()
        {
            return string.Format("{0}@{1}",
                Asset ? Asset.name : "null",
                string.IsNullOrEmpty(Guid) ? "null" : Guid);
        }

        public string GetDisplayName()
        {
            if (Asset)
            {
                return string.Format("{0} ({1})", Asset.name, Asset.GetType().Name);
            }

            return "Missing (Unknown)";
        }
    }
}
