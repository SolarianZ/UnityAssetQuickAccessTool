using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
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

        public static AssetHandle CreateFromExternalFile(string externalPath, out string error)
        {
            if (!File.Exists(externalPath) && !Directory.Exists(externalPath))
            {
                error = $"File or folder does not exist:\n{externalPath}.";
                return null;
            }

            AssetHandle externalFileHandle = new AssetHandle
            {
                _category = AssetCategory.ExternalFile,
                _guid = externalPath,
            };

            error = null;
            return externalFileHandle;
        }

        public static AssetHandle CreateFromUrl(string url, string title, out string error)
        {
            AssetHandle urlHandle = new AssetHandle
            {
                _category = AssetCategory.Url,
                _guid = url,
                _fallbackName = title ?? string.Empty,
            };

            error = null;
            return urlHandle;
        }

        public static void ForceSaveLocalCache()
        {
            AssetQuickAccessLocalCache.instance.ForceSave();
        }

        protected AssetHandle() { }

        public void Update()
        {
            switch (Category)
            {
                case AssetCategory.ProjectAsset:
                    if (_asset && _asset.name != _fallbackName)
                    {
                        _fallbackName = _asset.name;
                        ForceSaveLocalCache();
                    }
                    break;

                case AssetCategory.SceneObject:
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
                    break;

                case AssetCategory.ExternalFile:
                case AssetCategory.Url:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(Category), Category, null);
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
                        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            AssetDatabase.OpenAsset(Scene);
                            Update();
                            if (_asset)
                            {
                                AssetDatabase.OpenAsset(_asset);
                            }
                        }
                    }
                    break;

                case AssetCategory.ExternalFile:
                    if (File.Exists(_guid) || Directory.Exists(_guid))
                    {
                        EditorUtility.OpenWithDefaultApp(_guid);
                    }
                    break;

                case AssetCategory.Url:
                    Application.OpenURL(_guid);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(Category), Category, null);
            }
        }

        public void ShowInFolder()
        {
            EditorUtility.RevealInFinder(GetAssetPath());
        }

        public void CopyPathToSystemBuffer()
        {
            GUIUtility.systemCopyBuffer = GetAssetPath();
        }

        public void CopyGuidToSystemBuffer()
        {
            GUIUtility.systemCopyBuffer = Guid;
        }

        public void CopyTypeFullNameToSystemBuffer()
        {
            GUIUtility.systemCopyBuffer = GetAssetTypeFullName();
        }

        public void CopyInstanceIdToSystemBuffer()
        {
            GUIUtility.systemCopyBuffer = GetAssetInstanceId().ToString();
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
                    return Path.GetFileName(GetAssetPath());

                case AssetCategory.Url:
                    return _guid;

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
                case AssetCategory.Url:
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
                case AssetCategory.Url:
                    return null;

                default:
                    throw new ArgumentOutOfRangeException(nameof(Category), Category, null);
            }
        }

        public int GetAssetInstanceId()
        {
            switch (Category)
            {
                case AssetCategory.ProjectAsset:
                case AssetCategory.SceneObject:
                    if (_asset)
                    {
                        int instanceId = _asset.GetInstanceID();
                        return instanceId;
                    }
                    return 0;

                case AssetCategory.ExternalFile:
                case AssetCategory.Url:
                    return 0;

                default:
                    throw new ArgumentOutOfRangeException(nameof(Category), Category, null);
            }
        }

        public string GetDisplayName()
        {
            if (Asset)
            {
                return $"{GetAssetName()}    <i>({Asset.GetType().Name})</i>";
            }

            switch (Category)
            {
                case AssetCategory.ProjectAsset:
                    return $"Missing    <i>(Name: {GetAssetName()}, Guid: {Guid})</i>";

                case AssetCategory.SceneObject:
                    if (Scene)
                    {
                        return $"{GetAssetName()}    <i>(@{Scene.name})</i>";
                    }
                    else
                    {
                        return $"Missing    (Name: {GetAssetName()}, Scene: null)<i>";
                    }

                case AssetCategory.ExternalFile:
                    string path = GetAssetPath();
                    if (File.Exists(_guid))
                    {
                        string fileName = Path.GetFileName(path);
                        string folderName = Path.GetDirectoryName(path);
                        return $"{fileName}    <i>({folderName})</i>";
                    }
                    else if (Directory.Exists(_guid))
                    {
                        return path;
                    }
                    else
                    {
                        return $"Missing    (Path: {path})<i>";
                    }

                case AssetCategory.Url:
                    if (string.IsNullOrEmpty(_fallbackName))
                    {
                        return _guid;
                    }
                    else
                    {
                        return $"{_fallbackName}    <i>({_guid})</i>";
                    }

                default:
                    throw new ArgumentOutOfRangeException(nameof(Category), Category, null);
            }
        }

        public bool CheckFilter(AssetCategory filter)
        {
            return (Category & filter) != 0;
        }


        #region Legacy compatibility

        internal void UpgradeOldVersionData()
        {
            if (_category == AssetCategory.None)
            {
                _category = AssetCategory.ProjectAsset;
            }
        }

        #endregion
    }
}
