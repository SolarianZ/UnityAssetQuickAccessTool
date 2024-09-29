using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GBG.AssetQuickAccess.Editor
{
    //[Serializable]
    //public class MenuItemInfo
    //{
    //    public string Path;
    //    public bool HasValidation;
    //    public int Priority;
    //}

    internal class MenuItemSelectWindow : EditorWindow
    {
        public delegate void SubmitHandler(string menuPath, string title);

        public static MenuItemSelectWindow Open(Vector2 upperCenterPosition, SubmitHandler onSubmit, bool showAsDropdown = true)
        {
            MenuItemSelectWindow window = CreateInstance<MenuItemSelectWindow>();
            window._onSubmit += onSubmit;

            float width = 400;
            float height = 300;
            if (showAsDropdown)
            {
                Rect position = new Rect(upperCenterPosition - new Vector2(width / 2, 0), default);
                window.ShowAsDropDown(position, new Vector2(width, height));
                window.position = position;
            }
            else
            {
                window.Show();
            }

            return window;
        }

        public static List<string> GetAllMenuItemPaths()
        {
            List<string> menuPaths = TypeCache.GetMethodsWithAttribute<MenuItem>()
                .SelectMany(method => method.GetCustomAttributes<MenuItem>())
                .Where(attr => attr.validate == false)
                .Select(attr => RemoveShortcutSymbols(attr.menuItem))
                .Distinct()
                .ToList();
            //string[] menuPaths = AppDomain.CurrentDomain.GetAssemblies()
            //     .SelectMany(asm => asm.GetTypes())
            //     .SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            //     .SelectMany(method => method.GetCustomAttributes<MenuItem>())
            //     .Where(attr => attr.validate == false)
            //     .Select(attr => attr.menuItem)
            //     .Distinct()
            //     .ToArray();

            return menuPaths;
        }

        public static string RemoveShortcutSymbols(string menuPath)
        {
            // https://docs.unity3d.com/ScriptReference/MenuItem.html
            //%: Represents Ctrl on Windows and Linux. Cmd on macOS.
            //^: Represents Ctrl on Windows, Linux, and macOS.
            //#: Represents Shift.
            //&: Represents Alt.
            menuPath = menuPath.TrimEnd();
            int lastIndexOfSpace = menuPath.LastIndexOf(' ');
            if (lastIndexOfSpace == -1)
            {
                return menuPath;
            }

            char firstShortcutChar = menuPath[lastIndexOfSpace + 1];
            if (firstShortcutChar != '%' &&
               firstShortcutChar != '^' &&
               firstShortcutChar != '#' &&
               firstShortcutChar != '&')
            {
                //Debug.LogError($"[Asset Quick Access] Failed to remove shortcut symbols from menu path: {menuPath}");
                return menuPath;
            }

            menuPath = menuPath.Substring(0, lastIndexOfSpace).TrimEnd();
            return menuPath;
        }

        //public static bool ExecuteMenuItem(MethodInfo menuMethodInfo, bool validate)
        //{
        //    if (menuMethodInfo.GetParameters().Length == 0)
        //    {
        //        var result = menuMethodInfo.Invoke(null, new object[0]);
        //        return !validate || (bool)result;
        //    }
        //
        //    if (menuMethodInfo.GetParameters()[0].ParameterType == typeof(MenuCommand))
        //    {
        //        var result = menuMethodInfo.Invoke(null, new[] { new MenuCommand(null) });
        //        return !validate || (bool)result;
        //    }
        //
        //    return false;
        //}


        private SubmitHandler _onSubmit;
        private TextField _menuPathField;
        private ListView _menuPathListView;
        private Label _statusLabel;
        private Button _addButton;

        private List<string> _allMenuPaths = new List<string>();
        private List<string> _filteredMenuPaths = new List<string>();


        private void OnEnable()
        {
            _allMenuPaths = GetAllMenuItemPaths();
            _allMenuPaths.Sort();
            _filteredMenuPaths.Clear();
            _filteredMenuPaths.AddRange(_allMenuPaths);
        }

        private void CreateGUI()
        {
            rootVisualElement.style.paddingLeft = 4;
            rootVisualElement.style.paddingRight = 4;
            rootVisualElement.style.paddingTop = 4;
            rootVisualElement.style.paddingBottom = 4;
            rootVisualElement.RegisterCallback<KeyUpEvent>(HandleKeyUp);

            // Menu path field
            _menuPathField = new TextField
            {
                name = "MenuPathField",
                label = "Menu Item",
            };
            _menuPathField.Q<Label>().style.minWidth = 70;
            _menuPathField.Q<Label>().style.maxWidth = 70;
            _menuPathField.RegisterValueChangedCallback(OnMenuPathChanged);
            rootVisualElement.Add(_menuPathField);

            // Menu path ListView
            _menuPathListView = new ListView(_filteredMenuPaths, 28, MakeMenuPathListViewItem, BindMenuPathListViewItem)
            {
                selectionType = SelectionType.Single,
                //showAddRemoveFooter = false,
                //showBorder = false,
                //showFoldoutHeader = false,
                //showBoundCollectionSize = false,
                //showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
                style =
                {
                    flexGrow = 1,
                    marginTop = 8,
                }
            };
            rootVisualElement.Add(_menuPathListView);

            // Status label
            _statusLabel = new Label
            {
                text = "The current MenuItem path is invalid!",
                style =
                {
                    height = 15,
                    color = (Color)new Color32(200, 0, 0, 255),
                }
            };
            rootVisualElement.Add(_statusLabel);

            // Operation buttons
            VisualElement horizontal2 = new VisualElement
            {
                style =
                {
                    flexShrink = 0,
                    flexDirection = FlexDirection.Row,
                }
            };
            rootVisualElement.Add(horizontal2);
            _addButton = new Button(TrySubmit)
            {
                name = "AddButton",
                text = "Add",
                style =
                {
                    flexGrow = 1,
                }
            };
            _addButton.SetEnabled(false);
            horizontal2.Add(_addButton);
            Button cancelButton = new Button(Cancel)
            {
                name = "CancelButton",
                text = "Cancel",
                style =
                {
                    maxWidth = 50
                }
            };
            horizontal2.Add(cancelButton);

            // Focus
            _menuPathField.Focus();
        }


        private bool TryCorrectMenuPath(ref string menuPath)
        {
            if (string.IsNullOrEmpty(menuPath))
            {
                return false;
            }

            bool isValidPath = false;
            for (int i = 0; i < _allMenuPaths.Count; i++)
            {
                if (_allMenuPaths[i].Equals(menuPath, StringComparison.OrdinalIgnoreCase))
                {
                    menuPath = _allMenuPaths[i];
                    isValidPath = true;
                    break;
                }
            }

            return isValidPath;
        }

        private void OnMenuPathChanged(ChangeEvent<string> evt)
        {
            #region Validation

            string menuPath = evt.newValue;
            bool isValidPath = TryCorrectMenuPath(ref menuPath);

            _statusLabel.style.display = isValidPath
                ? DisplayStyle.None
                : DisplayStyle.Flex;

            _addButton.SetEnabled(isValidPath);

            #endregion


            #region Filter

            _filteredMenuPaths.Clear();

            if (string.IsNullOrEmpty(menuPath))
            {
                _filteredMenuPaths.AddRange(_allMenuPaths);
                return;
            }

            foreach (string tempMenuPath in _allMenuPaths)
            {
                if (tempMenuPath.ToUpperInvariant().Contains(menuPath.ToUpperInvariant()))
                {
                    _filteredMenuPaths.Add(tempMenuPath);
                }
            }

            _menuPathListView.Refresh();

            #endregion
        }

        private VisualElement MakeMenuPathListViewItem()
        {
            return new Button
            {
                name = "MenuItemButton",
                style =
                {
                    unityTextAlign = TextAnchor.MiddleLeft,
                }
            };
        }

        private void BindMenuPathListViewItem(VisualElement element, int index)
        {
            string menuPath = _filteredMenuPaths[index];
            Button button = (Button)element;
            button.text = menuPath;
            button.tooltip = menuPath;
            button.clicked += () => _menuPathField.value = menuPath;
        }


        private void HandleKeyUp(KeyUpEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                if (evt.target == _menuPathField)
                {
                    evt.StopImmediatePropagation();
                }
                else
                {
                    evt.StopImmediatePropagation();
                    TrySubmit();
                }
            }
            else if (evt.keyCode == KeyCode.Escape)
            {
                evt.StopImmediatePropagation();
                Cancel();
            }
        }

        private void Cancel()
        {
            SubmitMenuItem(null, null);
        }

        private void TrySubmit()
        {
            string menuPath = _menuPathField.value;
            bool isValidPath = TryCorrectMenuPath(ref menuPath);
            if (!isValidPath)
            {
                return;
            }

            string menuName = string.Empty;
            int slashIndex = menuPath.LastIndexOf('/');
            if (slashIndex > -1)
            {
                menuName = menuPath.Substring(slashIndex + 1);
            }

            SubmitMenuItem(menuPath, menuName);
        }

        private void SubmitMenuItem(string menuPath, string title)
        {
            if (_onSubmit != null)
            {
                SubmitHandler action = _onSubmit;
                _onSubmit = null;
                action.Invoke(menuPath, title);
            }

            Close();
        }
    }
}
