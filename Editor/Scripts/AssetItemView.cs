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
        private Image _assetIcon;
        private Image _categoryIcon;
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
            style.paddingLeft = 2;
            style.paddingRight = 2;
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

            _assetIcon = new Image
            {
                name = "AssetIcon",
                pickingMode = PickingMode.Ignore,
                style =
                {
                    flexShrink = 0,
                    width = 24,
                    height = 24,
                }
            };
            Add(_assetIcon);

            _title = new Label
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    flexGrow = 1,
                    flexShrink = 1,
                    paddingLeft = 2,
                    paddingRight = 2,
                    overflow = Overflow.Hidden,
                    unityTextAlign = TextAnchor.MiddleLeft,
                    textOverflow = TextOverflow.Ellipsis,
                }
            };
            Add(_title);
        }

        public void Bind(AssetHandle target)
        {
            _assetHandle = target;
            _assetHandle.Update();

            _title.text = _assetHandle.GetDisplayName();
            tooltip = _assetHandle.GetAssetPath();

            Texture assetIconTex;
            Texture categoryIconTex;
            switch (_assetHandle.Category)
            {
                case AssetCategory.ProjectAsset:
                    assetIconTex = GetObjectIcon(_assetHandle.Asset);
                    categoryIconTex = null;
                    break;

                case AssetCategory.SceneObject:
                    assetIconTex = GetObjectIcon(_assetHandle.Asset);
                    categoryIconTex = (Texture)EditorGUIUtility.Load(EditorGUIUtility.isProSkin ? "d_SceneAsset Icon" : "SceneAsset Icon");
                    break;

                case AssetCategory.ExternalFile:
                    assetIconTex = (Texture)EditorGUIUtility.Load(EditorGUIUtility.isProSkin ? "d_Import@2x" : "Import@2x");
                    categoryIconTex = (Texture)EditorGUIUtility.Load(EditorGUIUtility.isProSkin ? "d_Import" : "Import");
                    break;

                default:
                    throw new System.ArgumentOutOfRangeException(nameof(_assetHandle.Category), _assetHandle.Category, null);
            }

            _assetIcon.image = assetIconTex;
            if (categoryIconTex)
            {
                if (_categoryIcon == null)
                {
                    CreateCategoryIcon();
                }

                _categoryIcon.image = categoryIconTex;
                _categoryIcon.style.display = DisplayStyle.Flex;
            }
            else if (_categoryIcon != null)
            {
                _categoryIcon.image = null;
                _categoryIcon.style.display = DisplayStyle.None;
            }
        }

        public void Unbind()
        {
            _assetHandle = null;
            _title.text = null;
            tooltip = null;
            _assetIcon.image = null;
            if (_categoryIcon != null)
            {
                _categoryIcon.image = null;
                _categoryIcon.style.display = DisplayStyle.None;
            }
        }

        private void CreateCategoryIcon()
        {
            if (_categoryIcon != null)
            {
                return;
            }

            _categoryIcon = new Image
            {
                name = "CategoryIcon",
                pickingMode = PickingMode.Ignore,
                style =
                {
                    flexShrink = 0,
                    alignSelf = Align.Center,
                    width = 16,
                    height = 16,
                }
            };
            Add(_categoryIcon);
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


        private static Texture GetObjectIcon(UObject obj)
        {
            Texture iconTex = AssetPreview.GetMiniThumbnail(obj);
            if (!iconTex)
            {
                if (!_warningTexture)
                {
                    _warningTexture = EditorGUIUtility.IconContent("Warning@2x").image;
                }

                iconTex = _warningTexture;
            }

            return iconTex;
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
