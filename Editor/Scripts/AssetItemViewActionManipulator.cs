using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UObject = UnityEngine.Object;

namespace GBG.AssetQuickAccess.Editor
{
    public class AssetItemViewActionManipulator : PointerManipulator
    {
        internal AssetHandle AssetHandle => ((AssetItemView)target).AssetHandle;
        private bool _draggable;
        private int _clickCount;

        public Action Clicked;
        public Action DoubleClicked;
        public Action<Vector2> ContextClicked;


        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<ContextClickEvent>(OnContextClick);
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<ContextClickEvent>(OnContextClick);
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        }

        private void OnContextClick(ContextClickEvent evt)
        {
            evt.StopImmediatePropagation();
            _draggable = false;
            _clickCount = 0;

            ContextClicked?.Invoke(evt.mousePosition);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button == 0) // Left click
            {
                evt.StopImmediatePropagation();
                _draggable = true;
                _clickCount = evt.clickCount;
            }
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.button == 0) // Left click
            {
                int clickCount = _clickCount;
                _clickCount = 0;
                if (_draggable)
                {
                    _draggable = false;
                    evt.StopImmediatePropagation();

                    switch (clickCount)
                    {
                        case 1: // Click
                            Clicked?.Invoke();
                            break;
                        case 2: // Double click
                            DoubleClicked?.Invoke();
                            break;
                    }
                }
            }
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (_draggable)
            {
                evt.StopImmediatePropagation();
                _draggable = false;
                _clickCount = 0;

                // MEMO Unity Bug: https://issuetracker.unity3d.com/product/unity/issues/guid/UUM-76471
                // This DragAndDrop code causes the VisualElement to fail to receive the PointerUpEvent
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.SetGenericData(AssetItemView.DragGenericData, AssetItemView.DragGenericData);
                DragAndDrop.objectReferences = new UObject[] { AssetHandle.Asset };
                DragAndDrop.paths = new string[] { AssetDatabase.GetAssetPath(AssetHandle.Asset) };
                DragAndDrop.StartDrag(null);
            }
        }
    }
}
