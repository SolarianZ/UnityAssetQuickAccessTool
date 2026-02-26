using UnityEditor;
using UnityEditor.Callbacks;
using UObject = UnityEngine.Object;

namespace GBG.AssetQuickAccess.Editor
{
    internal static class AssetOpenListener
    {
        [OnOpenAsset(0)]
        public static bool OnOpenAsset(int instanceID)
        {
            UObject obj = EditorUtility.InstanceIDToObject(instanceID);
            AssetQuickAccessLocalCache.instance.AddRecentAsset(obj);
            if (AssetQuickAccessWindow.instance)
                AssetQuickAccessWindow.instance.SetViewDirtyDelay();

            // 仅监听打开事件，不负责实际打开
            return false;
        }
    }
}