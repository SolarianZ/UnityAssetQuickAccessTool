using System;

namespace GBG.AssetQuickAccess.Editor
{
    [Flags]
    public enum AssetCategory
    {
        None = 0,
        ProjectAsset = 1,
        SceneObject = 2,
        ExternalFile = 4,
        Url = 8,
    }
}
