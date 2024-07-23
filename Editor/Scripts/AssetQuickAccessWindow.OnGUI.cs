using UnityEditor;
using UnityEngine;

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

            // ProcessDragAndDropOut();
            ProcessDragAndDropIn();
        }

        private void ProcessDragAndDropIn()
        {
            // Allow drag and drop asset to window
            if (mouseOverWindow != this)
            {
                return;
            }

            if (Event.current.type == EventType.DragUpdated && DragAndDrop.objectReferences.Length > 0)
            {
                if (!DragGenericData.Equals(DragAndDrop.GetGenericData(DragGenericData)))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                }
            }
            else if (Event.current.type == EventType.DragExited)
            {
                Focus();

                Event.current.Use();
                DragAndDrop.AcceptDrag();

                if (!DragGenericData.Equals(DragAndDrop.GetGenericData(DragGenericData)))
                {
                    AddItems(DragAndDrop.objectReferences, DragAndDrop.paths, null);
                }
            }
        }
    }
}