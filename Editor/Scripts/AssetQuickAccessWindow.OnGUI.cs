using System.Collections;
using UnityEditor;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace GBG.AssetQuickAccess.Editor
{
    public partial class AssetQuickAccessWindow
    {
        public const string DragGenericData = "GBG_AQA_DragItem";

        private void OnGUI()
        {
            if (_isViewDirty)
            {
                _isViewDirty = false;
                UpdateFilteredAssetHandles();
            }

            DrawToolbar();
            DrawListContent();
            DrawBottomTips();

            ProcessDragAndDropOut();
            ProcessDragAndDropIn();
        }

        private void ProcessDragAndDropIn()
        {
            // Allow drag and drop asset to window
            if (mouseOverWindow == this)
            {
                if (Event.current.type == EventType.DragUpdated && DragAndDrop.objectReferences.Length > 0)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                }
                else if (Event.current.type == EventType.DragExited)
                {
                    Focus();
                    if (DragAndDrop.paths != null)
                    {
                        AddItems(DragAndDrop.objectReferences, DragAndDrop.paths, null);
                        Event.current.Use();
                    }
                }
            }
        }
    }
}