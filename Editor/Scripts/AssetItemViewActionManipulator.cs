using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GBG.AssetQuickAccess.Editor
{
    public interface IDragAndDropDataProvider
    {
        /// <summary>
        /// Get data associated with current drag and drop operation.
        /// </summary>
        /// <returns></returns>
        IEnumerable<KeyValuePair<string, object>> GetGenericData();

        /// <summary>
        /// Get the objects being dragged.
        /// </summary>
        /// <returns></returns>
        Object[] GetObjectReferences();

        /// <summary>
        /// Get the file names being dragged.
        /// </summary>
        /// <returns></returns>
        string[] GetPaths();

        /// <summary>
        /// Get the dragging title.
        /// </summary>
        /// <returns></returns>
        string GetTitle();
    }

    public class AssetItemViewActionManipulator : PointerManipulator
    {
        private readonly IDragAndDropDataProvider _dragAndDropDataProvider;
        private bool _draggable;


        public AssetItemViewActionManipulator(IDragAndDropDataProvider dragAndDropDataProvider)
        {
            _dragAndDropDataProvider = dragAndDropDataProvider;
        }


        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            evt.StopImmediatePropagation();
            _draggable = true;
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (_draggable)
            {
                evt.StopImmediatePropagation();
                _draggable = false;
            }
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (_draggable)
            {
                evt.StopImmediatePropagation();

                _draggable = false;
                DragAndDrop.PrepareStartDrag();

                IEnumerable<KeyValuePair<string, object>> genericData = _dragAndDropDataProvider.GetGenericData();
                if (genericData != null)
                {
                    foreach (KeyValuePair<string, object> kv in genericData)
                    {
                        DragAndDrop.SetGenericData(kv.Key, kv.Value);
                    }
                }

                DragAndDrop.objectReferences = _dragAndDropDataProvider.GetObjectReferences();
                DragAndDrop.paths = _dragAndDropDataProvider.GetPaths();
                DragAndDrop.StartDrag(_dragAndDropDataProvider.GetTitle());
            }
        }
    }
}