using UnityEngine;

namespace GBG.AssetQuickAccess.Editor
{
    public partial class AssetQuickAccessWindow
    {
        private GUIStyle _bottomTipsStyle;


        private void DrawBottomTips()
        {
            if (_bottomTipsStyle == null)
            {
                _bottomTipsStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    richText = true,
                };
            }

            GUILayout.Label("Drag the asset here to add a new item.", _bottomTipsStyle);
        }

        private void ClearBottomTipsGuiStytleCaches()
        {
            _bottomTipsStyle = null;
        }
    }
}