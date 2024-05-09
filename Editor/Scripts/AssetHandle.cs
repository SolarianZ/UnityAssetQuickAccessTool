using System;
using UnityEditor;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace GBG.AssetQuickAccess.Editor
{
    [Serializable]
    internal class AssetHandle
    {
        public UObject Asset => _asset;
        public string Guid => _guid;
        public string TypeFullName => _typeFullName;

        [SerializeField]
        private UObject _asset;
        [SerializeField]
        private string _guid;
        [SerializeField]
        private string _typeFullName;


        public AssetHandle(UObject asset)
        {
            _asset = asset;
            string assetPath = AssetDatabase.GetAssetPath(_asset);
            _guid = AssetDatabase.AssetPathToGUID(assetPath);
            _typeFullName = _asset.GetType().FullName;
        }

        public override string ToString()
        {
            string guidStr = string.IsNullOrEmpty(Guid) ? "null" : Guid;
            string assetStr = Asset ? Asset.name : "null";
            string typeFullName = string.IsNullOrEmpty(TypeFullName) ? "null" : TypeFullName;
            return $"Guid:{guidStr}, Asset:{assetStr}, Type:{typeFullName}";
        }

        public string GetDisplayName()
        {
            if (Asset)
            {
                return $"{Asset.name}    <i>({Asset.GetType().Name})</i>";
            }

            return $"Missing <i>(Guid: {Guid}, Type: {TypeFullName})</i>";
        }
    }
}
