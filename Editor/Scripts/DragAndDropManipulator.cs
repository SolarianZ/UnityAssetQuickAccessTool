#if UNITY_2021_3_OR_NEWER
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UObject = UnityEngine.Object;

namespace GBG.AssetQuickAccess.Editor
{
    public class DragAndDropManipulator : PointerManipulator
    {
        public event Action<IList<UObject>> OnDropAssets;


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
            OnDropAssets?.Invoke(DragAndDrop.objectReferences);
        }
    }
}

#endif
