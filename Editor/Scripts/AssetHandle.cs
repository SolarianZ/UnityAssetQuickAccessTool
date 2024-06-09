using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UObject = UnityEngine.Object;
using UScene = UnityEngine.SceneManagement.Scene;

namespace GBG.AssetQuickAccess.Editor
{
    [Serializable]
    internal class AssetHandle
    {
        public AssetCategory Category => _category;
        public UObject Asset => _asset;
        public string Guid => _guid;
        public SceneAsset Scene { get; private set; }


        [SerializeField]
        private AssetCategory _category;
        [SerializeField]
        private UObject _asset;
        /// <summary>
        /// Represent "AssetDatabase Guid" or "GlobalObjectId" or "File Full Path".
        /// </summary>
        [SerializeField]
        private string _guid;
        /// <summary>
        /// Displays when cannot retrieve the project asset or the scene object.
        /// </summary>
        [SerializeField]
        private string _fallbackName;


        public static AssetHandle CreateFromObject(UObject obj, out string error)
        {
            if (!obj)
            {
                error = "Object is null.";
                return null;
            }

            string assetPath = AssetDatabase.GetAssetPath(obj);

            // Project Asset
            if (!string.IsNullOrEmpty(assetPath))
            {
                AssetHandle assetHandle = new AssetHandle
                {
                    _category = AssetCategory.ProjectAsset,
                    _asset = obj,
                    _guid = AssetDatabase.AssetPathToGUID(assetPath),
                    _fallbackName = obj.name,
                };

                error = null;
                return assetHandle;
            }

            // Scene Object
            GameObject go = obj as GameObject;
            if (!go)
            {
                go = (obj as Component)?.gameObject;
                if (!go)
                {
                    error = $"Unsupported object: {obj}.";
                    return null;
                }
            }

            UScene scene = go.scene;
            if (string.IsNullOrEmpty(scene.path))
            {
                error = "Please save the scene to asset before recording a scene object.";
                return null;
            }

            AssetHandle sceneObjectHandle = new AssetHandle
            {
                _category = AssetCategory.SceneObject,
                _asset = obj,
                _guid = GlobalObjectId.GetGlobalObjectIdSlow(obj).ToString(),
                _fallbackName = obj.name,
                Scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path)
            };

            error = null;
            return sceneObjectHandle;
        }

        public static AssetHandle CreateFromExternalFile(string filePath, out string error)
        {
            if (!File.Exists(filePath))
            {
                error = $"File does not exist: {filePath}.";
                return null;
            }

            AssetHandle externalFileHandle = new AssetHandle
            {
                _category = AssetCategory.ExternalFile,
                _guid = filePath,
            };

            error = null;
            return externalFileHandle;
        }

        public static void ForceSaveLocalCache()
        {
            AssetQuickAccessLocalCache.instance.ForceSave();
        }

        protected AssetHandle() { }

        public void Update()
        {
            // Project Asset
            if (Category == AssetCategory.ProjectAsset)
            {
                if (_asset && _asset.name != _fallbackName)
                {
                    _fallbackName = _asset.name;
                    ForceSaveLocalCache();
                }
            }
            // Scene Object
            else if (Category == AssetCategory.SceneObject)
            {
                if ((!Scene || !_asset) && GlobalObjectId.TryParse(_guid, out GlobalObjectId globalObjectId))
                {
                    if (!Scene)
                    {
                        string sceneGuid = globalObjectId.assetGUID.ToString();
                        string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
                        Scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
                    }

                    if (!_asset)
                    {
                        _asset = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalObjectId);
                        if (_asset && _asset.name != _fallbackName)
                        {
                            _fallbackName = _asset.name;
                            ForceSaveLocalCache();
                        }
                    }
                }
            }
        }

        public void PingAsset()
        {
            if (_asset)
            {
                EditorGUIUtility.PingObject(_asset);
                return;
            }

            if (Category == AssetCategory.SceneObject && Scene)
            {
                EditorGUIUtility.PingObject(Scene);
            }
        }

        public void OpenAsset()
        {
            switch (Category)
            {
                case AssetCategory.ProjectAsset:
                    if (_asset)
                    {
                        AssetDatabase.OpenAsset(_asset);
                    }
                    break;

                case AssetCategory.SceneObject:
                    if (_asset)
                    {
                        AssetDatabase.OpenAsset(_asset);
                    }
                    else if (Scene)
                    {
                        AssetDatabase.OpenAsset(Scene);
                        Update();
                        if (_asset)
                        {
                            AssetDatabase.OpenAsset(_asset);
                        }
                    }
                    break;

                case AssetCategory.ExternalFile:
                    if (File.Exists(_guid))
                    {
                        EditorUtility.OpenWithDefaultApp(_guid);
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(Category), Category, null);
            }
        }

        public string GetAssetName()
        {
            switch (Category)
            {
                case AssetCategory.ProjectAsset:
                case AssetCategory.SceneObject:
                    if (_asset)
                    {
                        return _asset.name;
                    }
                    return _fallbackName;

                case AssetCategory.ExternalFile:
                    return Path.GetFileName(_guid);

                default:
                    throw new ArgumentOutOfRangeException(nameof(Category), Category, null);
            }
        }

        public string GetAssetPath()
        {
            switch (Category)
            {
                case AssetCategory.ProjectAsset:
                    string path = AssetDatabase.GUIDToAssetPath(_guid);
                    return path;

                case AssetCategory.SceneObject:
                    if (_asset)
                    {
                        GameObject go = _asset as GameObject;
                        if (!go)
                        {
                            go = (_asset as Component)?.gameObject;
                            if (!go)
                            {
                                return null;
                            }
                        }

                        Transform transform = go.transform;
                        StringBuilder builder = new StringBuilder();
                        builder.Append(transform.name);
                        transform = transform.parent;
                        while (transform)
                        {
                            builder.Insert(0, '/').Insert(0, transform.name);
                            transform = transform.parent;
                        }

                        return builder.ToString();
                    }
                    return null;

                case AssetCategory.ExternalFile:
                    return _guid;

                default:
                    throw new ArgumentOutOfRangeException(nameof(Category), Category, null);
            }
        }

        public string GetAssetTypeFullName()
        {
            switch (Category)
            {
                case AssetCategory.ProjectAsset:
                case AssetCategory.SceneObject:
                    if (_asset)
                    {
                        string typeFullName = _asset.GetType().FullName;
                        return typeFullName;
                    }
                    return null;

                case AssetCategory.ExternalFile:
                    return null;

                default:
                    throw new ArgumentOutOfRangeException(nameof(Category), Category, null);
            }
        }

        public string GetDisplayName()
        {
            if (Asset)
            {
                return $"{Asset.name}    <i>({Asset.GetType().Name})</i>";
            }

            switch (Category)
            {
                case AssetCategory.ProjectAsset:
                    return $"Missing    <i>(Name: {_fallbackName}, Guid: {Guid})</i>";

                case AssetCategory.SceneObject:
                    if (Scene)
                    {
                        return $"{_fallbackName}    <i>(@{Scene.name})</i>";
                    }
                    else
                    {
                        return $"Missing    (Name: {_fallbackName}, Scene: null)<i>";
                    }

                case AssetCategory.ExternalFile:
                    if (File.Exists(_guid))
                    {
                        string fileName = Path.GetFileName(_guid);
                        string folderName = Path.GetDirectoryName(_guid);
                        return $"{fileName}    <i>({folderName})</i>";
                    }
                    else
                    {
                        return $"Missing    (Path: {_guid})<i>";
                    }

                default:
                    throw new ArgumentOutOfRangeException(nameof(Category), Category, null);
            }
        }
    }
}
