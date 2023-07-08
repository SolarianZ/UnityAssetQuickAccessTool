using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace GBG.AssetQuickAccess.Editor
{
    public class DragAndDropManipulator : PointerManipulator
    {
        public event Action<IList<string>> OnDragAndDropAssets;


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
            if (DragAndDrop.paths.Length > 0)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
            }
        }

        private void OnDragPerform(DragPerformEvent _)
        {
            // Sometimes DragAndDrop.objectReferences will be empty, but I don't know the reason.
            OnDragAndDropAssets?.Invoke(DragAndDrop.paths);
        }
    }
}
