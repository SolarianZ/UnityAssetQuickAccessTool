using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Assertions;
using UObject = UnityEngine.Object;

namespace GBG.AssetQuickAccess.Editor
{
    internal class AssetQuickAccessSettings : ScriptableObject
    {
        private const string _FOLDER = "UserSettings";
        private const string _PATH = _FOLDER + "/" + nameof(AssetQuickAccessSettings) + ".asset";

        private static AssetQuickAccessSettings _instance;
        private static readonly Dictionary<string, AssetHandle> _assetHandleGuidTable = new Dictionary<string, AssetHandle>();


        public static List<string> GetGuids()
        {
            LoadOrCreate();
            return _instance._guids;
        }
        
        public static AssetHandle GetAssetHandle(int guidIndex)
        {
            var guid = GetGuids()[guidIndex];
            return _assetHandleGuidTable[guid];
        }

        public static AssetHandle GetAssetHandle(string guid)
        {
            LoadOrCreate();
            return _assetHandleGuidTable[guid];
        }

        public static void Refresh()
        {
            if (_instance)
            {
                PopulateAssetHandles();
                return;
            }

            LoadOrCreate();
        }

        public static bool AddAsset(string assetPath)
        {
            LoadOrCreate();

            var assetGuid = AssetDatabase.AssetPathToGUID(assetPath, AssetPathToGUIDOptions.OnlyExistingAssets);
            if (string.IsNullOrEmpty(assetGuid))
            {
                Debug.LogError($"Can not load asset at path '{assetPath}'.");
                return false;
            }

            if (_instance._guids.Contains(assetGuid))
            {
                Assert.IsTrue(_assetHandleGuidTable.ContainsKey(assetGuid));
                var asset = AssetDatabase.LoadAssetAtPath<UObject>(assetPath);
                //Debug.Log($"Asset '{asset}' has already been recorded.", asset);
                return false;
            }

            _instance._guids.Add(assetGuid);
            Assert.IsFalse(_assetHandleGuidTable.ContainsKey(assetGuid));
            _assetHandleGuidTable.Add(assetGuid, new AssetHandle(assetGuid));
            ForceSave();

            return true;
        }

        public static bool RemoveAsset(AssetHandle handle)
        {
            LoadOrCreate();

            if (!_assetHandleGuidTable.Remove(handle.Guid))
            {
                Assert.IsFalse(_instance._guids.Contains(handle.Guid));
                Debug.LogError($"Asset handle '{handle}' does not exist.", handle.Asset);
                return false;
            }

            var removed = _instance._guids.Remove(handle.Guid);
            Assert.IsTrue(removed);
            ForceSave();
            return true;
        }

        public static void ClearAllAssets()
        {
            LoadOrCreate();

            Assert.IsTrue(_instance._guids.Count == _assetHandleGuidTable.Count);
            if (_instance._guids.Count > 0)
            {
                _instance._guids.Clear();
                _assetHandleGuidTable.Clear();
                ForceSave();
            }

            Debug.Log("All asset quick access items cleared.");
        }

        public static void PrintGuids()
        {
            LoadOrCreate();

            var sb = new StringBuilder();
            foreach (var guid in _instance._guids)
            {
                sb.AppendLine(guid);
            }

            Debug.Log(sb.ToString(), _instance);
        }

        public static void ForceSave()
        {
            EditorUtility.SetDirty(_instance);
            InternalEditorUtility.SaveToSerializedFileAndForget(new UObject[] { _instance }, _PATH, true);
        }


        private static void PopulateAssetHandles()
        {
            _assetHandleGuidTable.Clear();
            foreach (var guid in _instance._guids)
            {
                _assetHandleGuidTable.Add(guid, new AssetHandle(guid));
            }
        }

        private static void LoadOrCreate()
        {
            if (_instance)
            {
                return;
            }

            // Load
            var objects = InternalEditorUtility.LoadSerializedFileAndForget(_PATH);
            if (objects.Length > 0)
            {
                _instance = (AssetQuickAccessSettings)objects[0];
                PopulateAssetHandles();
                return;
            }

            // Create
            if (!Directory.Exists(_FOLDER))
            {
                Directory.CreateDirectory(_FOLDER);
            }

            _instance = CreateInstance<AssetQuickAccessSettings>();
            PopulateAssetHandles();
            InternalEditorUtility.SaveToSerializedFileAndForget(new UObject[] { _instance }, _PATH, true);
            Debug.Log($"Create new {nameof(AssetQuickAccessSettings)} file at path {_PATH}.");
        }


#pragma warning disable CS0414
        [SerializeField]
        private int _version = 3;
#pragma warning restore CS0414

        [SerializeField]
        private List<string> _guids = new List<string>();
    }
}
