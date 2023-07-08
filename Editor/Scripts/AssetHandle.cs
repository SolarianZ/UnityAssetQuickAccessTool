using UnityEditor;
using UnityEngine;

namespace GBG.AssetQuickAccess.Editor
{
    internal class AssetHandle
    {
        public string Guid
        {
            get { return _guid; }
        }

        private string _guid;

        public Object Asset
        {
            get
            {
                return _asset;
                ;
            }
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
            var guidStr = string.IsNullOrEmpty(Guid) ? "null" : Guid;
            var assetStr = Asset ? Asset.name : "null";
            return $"Guid:{guidStr}, Asset:{assetStr}";
        }

        public string GetDisplayName()
        {
            if (Asset)
            {
                return $"{Asset.name}    <i>({Asset.GetType().Name})</i>";
            }

            return $"Missing <i>(Unknown Asset Guid: {Guid})</i>";
        }
    }
}
