using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using UDebug = UnityEngine.Debug;

namespace GBG.AssetQuickAccess.Editor
{
    internal class UrlEditWindow : EditorWindow
    {
        public delegate void SubmitTitleHandler(string url, string title);

        public static UrlEditWindow Open(Vector2 centerPosition, SubmitTitleHandler onSubmit)
        {
            UrlEditWindow window = CreateInstance<UrlEditWindow>();
            window._onSubmit += onSubmit;

            float width = 300;
            float height = 180;
            Rect position = new Rect(centerPosition - new Vector2(width / 2, height / 2), default);
            window.ShowAsDropDown(position, new Vector2(300, 90));
            return window;
        }


        private SubmitTitleHandler _onSubmit;
        private TextField _urlField;
        private TextField _titleField;
        private Button _getTitleButton;
        private Label _getTitleStatusLabel;

        private readonly double _getTitleTimeout = 3f;
        private string _getTitleUrl;
        private HttpClient _getTitleHttpClient;
        private Task<string> _getTitleTask;


        private void CreateGUI()
        {
            rootVisualElement.style.paddingLeft = 4;
            rootVisualElement.style.paddingRight = 4;
            rootVisualElement.style.paddingTop = 4;
            rootVisualElement.style.paddingBottom = 4;
            rootVisualElement.style.justifyContent = Justify.SpaceBetween;

            rootVisualElement.RegisterCallback<KeyUpEvent>(HandleKeyUp);

            // Url field
            _urlField = new TextField
            {
                name = "UrlField",
                label = "Url",
            };
            _urlField.Q<Label>().style.minWidth = 40;
            _urlField.Q<Label>().style.maxWidth = 40;
            rootVisualElement.Add(_urlField);

            // Title elements
            VisualElement horizontal1 = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                }
            };
            rootVisualElement.Add(horizontal1);
            _titleField = new TextField
            {
                name = "TitleField",
                label = "Title",
                style =
                {
                    flexGrow = 1,
                }
            };
            _titleField.Q<Label>().style.minWidth = 40;
            _titleField.Q<Label>().style.maxWidth = 40;
            horizontal1.Add(_titleField);
            _getTitleButton = new Button(GetWebsiteTitle)
            {
                name = "GetTitleButton",
                text = "Get",
                tooltip = "Try to get the title of the website from the internet.",
                style =
                {
                    maxWidth = 50
                }
            };
            horizontal1.Add(_getTitleButton);
            _getTitleStatusLabel = new Label
            {
                style =
                {
                    height = 15,
                }
            };
            rootVisualElement.Add(_getTitleStatusLabel);

            // Operation buttons
            VisualElement horizontal2 = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                }
            };
            rootVisualElement.Add(horizontal2);
            Button addButton = new Button(Submit)
            {
                name = "AddButton",
                text = "Add",
                style =
                {
                    flexGrow = 1,
                }
            };
            horizontal2.Add(addButton);
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

            FocusUrlField();
        }

        private void OnDisable()
        {
            _getTitleHttpClient?.Dispose();
            _getTitleHttpClient = null;
            _getTitleTask = null;
        }

        private void Update()
        {
            UpdateGetWebsiteTitle();
        }


        private void FocusUrlField()
        {
            _urlField.Focus();
        }

        private void FocusTitleField()
        {
            _titleField.Focus();
        }


        private void GetWebsiteTitle()
        {
            Assert.IsTrue(_getTitleTask == null);

            _getTitleStatusLabel.text = null;

            string url = _urlField.value;
            if (string.IsNullOrEmpty(url))
            {
                _getTitleStatusLabel.style.color = GetTextColor(true);
                _getTitleStatusLabel.text = "The url is empty.";
                return;
            }

            if (!url.StartsWith("https://") && !url.StartsWith(@"http://"))
            {
                url = "http://" + url;
            }

            _getTitleUrl = url;
            _getTitleStatusLabel.style.color = GetTextColor(false);
            _getTitleStatusLabel.text = $"Getting title from {_getTitleUrl} ...";

            _getTitleHttpClient = new HttpClient();
            {
                _getTitleHttpClient.Timeout = TimeSpan.FromSeconds(_getTitleTimeout);
                try
                {
                    _getTitleTask = _getTitleHttpClient.GetStringAsync(_getTitleUrl);
                }
                catch (Exception e)
                {
                    _getTitleStatusLabel.style.color = GetTextColor(true);
                    _getTitleStatusLabel.text = e.Message;

                    _getTitleHttpClient.Dispose();
                    _getTitleHttpClient = null;
                    _getTitleTask = null;

                    UDebug.LogException(e);
                }
            }
        }

        private void UpdateGetWebsiteTitle()
        {
            if (_getTitleTask == null)
            {
                return;
            }

            switch (_getTitleTask.Status)
            {
                case TaskStatus.RanToCompletion:
                    string html = _getTitleTask.Result;
                    string title = Regex.Match(html, @"<title[^>]*>(.*?)</title>").Groups[1].Value;
                    if (string.IsNullOrEmpty(title))
                    {
                        _getTitleStatusLabel.style.color = GetTextColor(true);
                        _getTitleStatusLabel.text = "Failed to parse html content";
                    }
                    else
                    {
                        _titleField.value = title;
                        _getTitleStatusLabel.text = null;
                    }

                    _getTitleHttpClient.Dispose();
                    _getTitleHttpClient = null;
                    _getTitleTask = null;

                    break;

                case TaskStatus.Canceled:
                    _getTitleStatusLabel.style.color = GetTextColor(false);
                    _getTitleStatusLabel.text = "Get the website title canceled";

                    _getTitleHttpClient.Dispose();
                    _getTitleHttpClient = null;
                    _getTitleTask = null;

                    break;

                case TaskStatus.Faulted:
                    Exception exception = _getTitleTask.Exception;
                    while (exception.InnerException != null)
                    {
                        exception = exception.InnerException;
                    }
                    _getTitleStatusLabel.style.color = GetTextColor(true);
                    _getTitleStatusLabel.text = exception.Message;

                    _getTitleHttpClient.Dispose();
                    _getTitleHttpClient = null;
                    _getTitleTask = null;

                    break;

                default:
                    break;
            }

            SetElementsEnableStateByGetTitleStatus();
        }

        private void SetElementsEnableStateByGetTitleStatus()
        {
            bool enabled = _getTitleTask == null;
            _urlField.SetEnabled(enabled);
            _titleField.SetEnabled(enabled);
            _getTitleButton.SetEnabled(enabled);
        }


        private void HandleKeyUp(KeyUpEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                if (evt.target == _urlField)
                {
                    evt.StopImmediatePropagation();
                    FocusTitleField();
                }
                else
                {
                    evt.StopImmediatePropagation();
                    Submit();
                }
            }
            else if (evt.keyCode == KeyCode.Tab)
            {
                if (evt.target == _urlField)
                {
                    evt.StopImmediatePropagation();
                    FocusTitleField();
                }
                else if (evt.target == _titleField)
                {
                    evt.StopImmediatePropagation();
                    FocusUrlField();
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
            SubmitUrl(null, null);
        }

        private void Submit()
        {
            string url = _urlField.value;
            string title = _titleField.value;
            SubmitUrl(url, title);
        }

        private void SubmitUrl(string url, string title)
        {
            if (_onSubmit != null)
            {
                SubmitTitleHandler action = _onSubmit;
                _onSubmit = null;
                action.Invoke(url, title);
            }

            Close();
        }


        private static Color GetTextColor(bool isErrorText)
        {
            if (isErrorText)
            {
                return new Color32(200, 0, 0, 255);
            }

            return EditorGUIUtility.isProSkin ? new Color32(196, 196, 196, 255) : new Color32(24, 24, 24, 255);
        }

        //public static async Task<string> GetWebsiteTitleAsync(string url)
        //{
        //    using (HttpClient client = new HttpClient())
        //    {
        //        client.Timeout = TimeSpan.FromSeconds(3);
        //        try
        //        {
        //            string html = await client.GetStringAsync(url);
        //            string title = Regex.Match(html, @"<title\b][^>]*>(.*?)</title>").Groups[1].Value;
        //            return title;
        //        }
        //        catch (TaskCanceledException)
        //        {
        //            Debug.Log("Timeout");
        //            return null;
        //        }
        //        catch (Exception e)
        //        {
        //            Debug.LogException(e);
        //            return null;
        //        }
        //    }
        //}
    }
}
