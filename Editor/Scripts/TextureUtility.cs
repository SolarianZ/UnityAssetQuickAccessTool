using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UObject = UnityEngine.Object;

namespace GBG.AssetQuickAccess.Editor
{
    public static class TextureUtility
    {
        #region Static Textures

        private static Texture _sceneObjectTextureCache;
        private static Texture _sceneObjectTextureSmallCache;
        private static Texture _externalFileTextureCache;
        private static Texture _externalFileTextureSmallCache;
        private static Texture _urlTextureCache;
        private static Texture _warningTextureCache;

        public static Texture GetObjectIcon(UObject obj, SceneAsset containingScene)
        {
            if (obj)
            {
                return AssetPreview.GetMiniThumbnail(obj);
            }

            if (containingScene)
            {
                string scenePath = AssetDatabase.GetAssetPath(containingScene);
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene scene = SceneManager.GetSceneAt(i);
                    if (scene.isLoaded && scene.path == scenePath)
                    {
                        return GetWarningTexture();
                    }
                }

                return GetSceneObjectTexture(false);
            }

            return GetWarningTexture();
        }

        public static Texture GetSceneObjectTexture(bool small)
        {
            if (small)
            {
                if (!_sceneObjectTextureSmallCache)
                {
                    _sceneObjectTextureSmallCache = (Texture)EditorGUIUtility.Load(
                        EditorGUIUtility.isProSkin
                            ? "d_UnityEditor.SceneHierarchyWindow"
                            : "UnityEditor.SceneHierarchyWindow");
                }

                return _sceneObjectTextureSmallCache;
            }

            if (!_sceneObjectTextureCache)
            {
                _sceneObjectTextureCache = (Texture)EditorGUIUtility.Load(
                    EditorGUIUtility.isProSkin
                        ? "d_UnityEditor.SceneHierarchyWindow@2x"
                        : "UnityEditor.SceneHierarchyWindow@2x");
            }

            return _sceneObjectTextureCache;
        }

        public static Texture GetExternalFileTexture(bool small)
        {
            if (small)
            {
                if (!_externalFileTextureSmallCache)
                {
                    _externalFileTextureSmallCache = (Texture)EditorGUIUtility.Load(EditorGUIUtility.isProSkin ? "d_Import" : "Import");
                }

                return _externalFileTextureSmallCache;
            }

            if (!_externalFileTextureCache)
            {
                _externalFileTextureCache = (Texture)EditorGUIUtility.Load(EditorGUIUtility.isProSkin ? "d_Import@2x" : "Import@2x");
            }

            return _externalFileTextureCache;
        }

        public static Texture GetUrlTexture()
        {
            if (!_urlTextureCache)
            {
                _urlTextureCache = (Texture)EditorGUIUtility.Load(EditorGUIUtility.isProSkin ? "d_BuildSettings.Web.Small" : "BuildSettings.Web.Small");
            }

            return _urlTextureCache;
        }

        public static Texture GetWarningTexture()
        {
            if (!_warningTextureCache)
            {
                _warningTextureCache = (Texture)EditorGUIUtility.Load("Warning@2x");
            }

            return _warningTextureCache;
        }

        #endregion
    }
}