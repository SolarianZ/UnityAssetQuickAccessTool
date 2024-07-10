using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UObject = UnityEngine.Object;

namespace GBG.AssetQuickAccess.Editor
{
    public class DragAndDropManipulator : PointerManipulator
    {
        public event Action<IList<UObject>, IList<string>> OnDragAndDrop;


        public DragAndDropManipulator(VisualElement target)
        {
            this.target = target;
        }

        public void RemoveSelfFromTarget()
        {
            target.RemoveManipulator(this);
        }


        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<DragUpdatedEvent>(OnDragUpdate);
            target.RegisterCallback<DragPerformEvent>(OnDragPerform);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<DragUpdatedEvent>(OnDragUpdate);
            target.UnregisterCallback<DragPerformEvent>(OnDragPerform);
        }


        private void OnDragUpdate(DragUpdatedEvent _)
        {
            if (DragAndDrop.paths.Length > 0 || DragAndDrop.objectReferences.Length > 0)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
            }
        }

        private void OnDragPerform(DragPerformEvent _)
        {
            if (AssetItemView.DragGenericData.Equals(DragAndDrop.GetGenericData(AssetItemView.DragGenericData)))
            {
                DragAndDrop.SetGenericData(AssetItemView.DragGenericData, null);
                return;
            }

            // Sometimes the dragged assets are not included in DragAndDrop.objectReferences, for unknown reasons.
            OnDragAndDrop?.Invoke(DragAndDrop.objectReferences, DragAndDrop.paths);
        }
    }
}
