using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UObject = UnityEngine.Object;

namespace GBG.AssetQuickAccess.Editor
{
    internal class AssetItemView : IMGUIContainer
    {
        public static double DoubleClickInterval = 0.3f;
        private static Texture _warningTexture;

        private AssetHandle _assetHandle;
        private Image _icon;
        private Label _title;
        private MouseAction _mouseAction = MouseAction.None;
        private double _lastClickTime;

        public event System.Action<AssetHandle> OnWantsToRemoveAssetItem;


        public AssetItemView()
        {
            onGUIHandler = OnGUI;

            // To avoid conflict with the drag action of the ListView items.
            this.RegisterCallback<PointerDownEvent>(evt => evt.StopImmediatePropagation());

            // content
            style.height = new Length(100, LengthUnit.Percent);
            style.flexDirection = FlexDirection.Row;
            style.backgroundColor = EditorGUIUtility.isProSkin
                ? new Color(0.35f, 0.35f, 0.35f, 0.5f)
                : new Color(0.9f, 0.9f, 0.9f, 0.5f);
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

            _icon = new Image
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    width = new StyleLength(24),
                    height = new Length(100, LengthUnit.Percent),
                    marginLeft = -28 // to avoid overlap with text
                }
            };
            Add(_icon);

            _title = new Label
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    overflow = Overflow.Hidden,
                    unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleLeft),
                    textOverflow = new StyleEnum<TextOverflow>(TextOverflow.Ellipsis),
                }
            };
            Add(_title);
        }

        public void Bind(AssetHandle target)
        {
            _assetHandle = target;
            _title.text = _assetHandle.GetDisplayName();

            if (_assetHandle.Asset)
            {
                tooltip = AssetDatabase.GetAssetPath(_assetHandle.Asset);
            }

            Texture iconTex = AssetPreview.GetMiniThumbnail(_assetHandle.Asset);
            if (!iconTex)
            {
                if (!_warningTexture)
                {
                    _warningTexture = EditorGUIUtility.IconContent("Warning@2x").image;
                }

                iconTex = _warningTexture;
            }
            _icon.image = iconTex;
        }

        public void Unbind()
        {
            _assetHandle = null;
            _title.text = null;
            tooltip = null;
            _icon.image = null;
        }


        private void OnGUI()
        {
            Event e = Event.current;
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0)
                    {
                        double currentTime = EditorApplication.timeSinceStartup;
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
                        DragAndDrop.objectReferences = new UObject[] { _assetHandle.Asset };
                        DragAndDrop.paths = new string[] { AssetDatabase.GetAssetPath(_assetHandle.Asset) };
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
            _assetHandle.PingAsset();
        }

        private void OnDoubleClick()
        {
            _assetHandle.OpenAsset();
        }

        private void OnContextClick(Vector2 mousePosition)
        {
            GenericDropdownMenu menu = new GenericDropdownMenu();
            if (_assetHandle.Asset)
            {
                menu.AddItem("Open", false, () => AssetDatabase.OpenAsset(_assetHandle.Asset));
                menu.AddItem("Copy Path", false, () => GUIUtility.systemCopyBuffer = _assetHandle.GetAssetPath());
                menu.AddItem("Copy Guid", false, () => GUIUtility.systemCopyBuffer = _assetHandle.Guid);
                menu.AddItem("Copy Type", false, () => GUIUtility.systemCopyBuffer = _assetHandle.GetAssetTypeFullName());
                menu.AddItem("Show in Folder", false, () => EditorUtility.RevealInFinder(AssetDatabase.GUIDToAssetPath(_assetHandle.Guid)));
            }
            else
            {
                menu.AddDisabledItem("Open", false);
                menu.AddDisabledItem("Copy Path", false);
                menu.AddItem("Copy Guid", false, () => GUIUtility.systemCopyBuffer = _assetHandle.Guid);
                menu.AddItem("Copy Type", false, () => GUIUtility.systemCopyBuffer = _assetHandle.GetAssetTypeFullName());
                menu.AddDisabledItem("Show in Folder", false);
            }
            menu.AddItem("Remove", false, () => OnWantsToRemoveAssetItem?.Invoke(_assetHandle));
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
