using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UDebug = UnityEngine.Debug;
using UObject = UnityEngine.Object;

namespace GBG.AssetQuickAccess.Editor
{
    internal class AssetItemView : IMGUIContainer
    {
        public static double DoubleClickInterval = 0.3f;

        public Image Icon { get; }

        public Label Title { get; }

        public AssetHandle AssetHandle { get; set; }

        public event System.Action<AssetHandle> OnWantsToRemoveAssetItem;

        private MouseAction _mouseAction = MouseAction.None;

        private double _lastClickTime;


        public AssetItemView()
        {
            onGUIHandler = OnGUI;

            // To avoid conflict with the drag action of the ListView items.
            this.RegisterCallback<PointerDownEvent>(evt => evt.StopImmediatePropagation());

            // content
            style.height = new Length(100, LengthUnit.Percent);
            style.flexDirection = FlexDirection.Row;
            style.backgroundColor = EditorGUIUtility.isProSkin
                ? new Color(0.35f, 0.35f, 0.35f, 1.0f)
                : new Color(0.9f, 0.9f, 0.9f, 1.0f);
            // margin
            style.marginLeft = 0;
            style.marginRight = 0;
            style.marginTop = 0;
            style.marginBottom = 0;
            // padding
            style.paddingLeft = 32; // to avoid overlap with icon
            style.paddingRight = 0;
            style.paddingTop = 0;
            style.paddingBottom = 0;
            // border width
            style.borderLeftWidth = 0;
            style.borderRightWidth = 0;
            style.borderTopWidth = 0;
            style.borderBottomWidth = 0;
            // border radius
            style.borderTopLeftRadius = 0;
            style.borderTopRightRadius = 0;
            style.borderBottomLeftRadius = 0;
            style.borderBottomRightRadius = 0;

            Icon = new Image
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    width = new StyleLength(24),
                    height = new Length(100, LengthUnit.Percent),
                    marginLeft = -28 // to avoid overlap with text
                }
            };
            Add(Icon);

            Title = new Label
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleLeft),
                    textOverflow = new StyleEnum<TextOverflow>(TextOverflow.Ellipsis),
                }
            };
            Add(Title);
        }


        private void OnGUI()
        {
            var e = Event.current;
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0)
                    {
                        var currentTime = EditorApplication.timeSinceStartup;
                        if (currentTime - _lastClickTime < DoubleClickInterval)
                        {
                            _mouseAction = MouseAction.DoubleClick;
                            _lastClickTime = 0;
                        }
                        else
                        {
                            _mouseAction = MouseAction.Click;
                            _lastClickTime = currentTime;
                        }
                        e.Use();
                    }
                    else if (e.button == 1)
                    {
                        _mouseAction = MouseAction.ContextClick;
                        e.Use();
                    }
                    break;

                case EventType.MouseUp:
                    switch (_mouseAction)
                    {
                        case MouseAction.Click:
                            if (e.button == 0)
                            {
                                _mouseAction = MouseAction.None;
                                OnClick();
                                e.Use();
                            }
                            break;

                        case MouseAction.DoubleClick:
                            if (e.button == 0)
                            {
                                _mouseAction = MouseAction.None;
                                OnDoubleClick();
                                e.Use();
                            }
                            break;

                        case MouseAction.ContextClick:
                            if (e.button == 1)
                            {
                                _mouseAction = MouseAction.None;
                                OnContextClick(e.mousePosition);
                                e.Use();
                            }
                            break;

                        case MouseAction.None:
                        case MouseAction.Drag: // If drag happens, the code can't reach here.
                            break;

                        default:
                            throw new System.ArgumentOutOfRangeException(nameof(_mouseAction));
                    }

                    break;

                case EventType.MouseDrag:
                    if (_mouseAction == MouseAction.Click)
                    {
                        _mouseAction = MouseAction.Drag;
                        DragAndDrop.PrepareStartDrag();
                        DragAndDrop.objectReferences = new UObject[] { AssetHandle.Asset };
                        DragAndDrop.paths = new string[] { AssetDatabase.GetAssetPath(AssetHandle.Asset) };
                        DragAndDrop.StartDrag(null);
                    }
                    break;

                case EventType.DragUpdated:
                    if (DragAndDrop.objectReferences.Length > 0)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                    }
                    break;

                default:
                    break;
            }
        }

        private void OnClick()
        {
            EditorGUIUtility.PingObject(AssetHandle.Asset);
        }

        private void OnDoubleClick()
        {
            AssetDatabase.OpenAsset(AssetHandle.Asset);
        }

        private void OnContextClick(Vector2 mousePosition)
        {
            var menu = new GenericDropdownMenu();
            menu.AddItem("Open", false, () => AssetDatabase.OpenAsset(AssetHandle.Asset));
            menu.AddItem("Ping", false, () => EditorGUIUtility.PingObject(AssetHandle.Asset));
            menu.AddItem("Open", false, () => AssetDatabase.OpenAsset(AssetHandle.Asset));
            menu.AddItem("Ping", false, () => EditorGUIUtility.PingObject(AssetHandle.Asset));
            menu.AddItem("Print Guid", false, () => UDebug.Log(AssetHandle.Guid, AssetHandle.Asset));
            menu.AddItem("Print Path", false, () => UDebug.Log(AssetDatabase.GUIDToAssetPath(AssetHandle.Guid), AssetHandle.Asset));
            menu.AddItem("Show in Folder", false, () => EditorUtility.RevealInFinder(AssetDatabase.GUIDToAssetPath(AssetHandle.Guid)));
            menu.AddItem("Remove", false, () => OnWantsToRemoveAssetItem?.Invoke(AssetHandle));
            menu.DropDown(new Rect(this.LocalToWorld(mousePosition), Vector2.zero), this);
        }


        enum MouseAction : byte
        {
            None,
            Click,
            DoubleClick,
            ContextClick,
            Drag,
        }
    }
}
