using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GBG.AssetQuickAccess.Editor
{
    internal class UrlEditWindow : EditorWindow
    {
        public static UrlEditWindow Open(Vector2 centerPosition, Action<string> onSubmit)
        {
            UrlEditWindow window = CreateInstance<UrlEditWindow>();
            window._onSubmit += onSubmit;

            float width = 300;
            float height = 100;
            Rect position = new Rect(centerPosition - new Vector2(width / 2, height / 2), default);
            window.ShowAsDropDown(position, new Vector2(300, 50));
            return window;
        }


        private Action<string> _onSubmit;
        private TextField _urlField;


        private void OnEnable()
        {
            rootVisualElement.style.paddingLeft = 4;
            rootVisualElement.style.paddingRight = 4;
            rootVisualElement.style.paddingTop = 4;
            rootVisualElement.style.paddingBottom = 4;
            rootVisualElement.style.justifyContent = Justify.SpaceBetween;

            _urlField = new TextField
            {
                name = "UrlField",
                label = "Url",
            };
            _urlField.Q<Label>().style.minWidth = 20;
            _urlField.Q<Label>().style.maxWidth = 20;
            _urlField.RegisterCallback<KeyUpEvent>(HandleKeyUp);
            rootVisualElement.Add(_urlField);

            VisualElement horizontal = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                }
            };
            rootVisualElement.Add(horizontal);
            Button addButton = new Button(Submit)
            {
                name = "AddButton",
                text = "Add",
                style =
                {
                    flexGrow = 1,
                }
            };
            horizontal.Add(addButton);
            Button cancelButton = new Button(Cancel)
            {
                name = "CancelButton",
                text = "Cancel",
                style =
                {
                    maxWidth = 50
                }
            };
            horizontal.Add(cancelButton);

            // _urlField.Focus();
            VisualElement textInput = _urlField.Q(className: TextField.inputUssClassName);
            _urlField.schedule.Execute(textInput.Focus);
        }

        private void HandleKeyUp(KeyUpEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                Submit();
            }
            else if (evt.keyCode == KeyCode.Escape)
            {
                Cancel();
            }
        }

        private void Cancel()
        {
            SubmitUrl(null);
        }

        private void Submit()
        {
            string url = _urlField.value;
            SubmitUrl(url);
        }

        private void SubmitUrl(string url)
        {
            if (_onSubmit != null)
            {
                Action<string> action = _onSubmit;
                _onSubmit = null;
                action.Invoke(url);
            }

            Close();
        }
    }
}