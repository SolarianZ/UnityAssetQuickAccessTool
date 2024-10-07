using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GBG.AssetQuickAccess.Editor
{
    internal class IMGUITextField : VisualElement, INotifyValueChanged<string>
    {
        public string LabelText
        {
            get => LabelElement.text;
            set
            {
                LabelElement.text = value;
                RefreshLabelElementStyle();
            }
        }
        public string HintText
        {
            get => HintElement.text;
            set => HintElement.text = value;
        }

        public string value
        {
            get => _value;
            set
            {
                if (_value == value)
                {
                    return;
                }

                string previousValue = _value;
                SetValueWithoutNotify(value);
                using (ChangeEvent<string> evt = ChangeEvent<string>.GetPooled(previousValue, _value))
                {
                    evt.target = this;
                    SendEvent(evt);
                }
            }
        }

        public Label LabelElement { get; }
        public Label HintElement { get; }
        public IMGUIContainer InputFieldElement { get; }

        private string _value;


        public IMGUITextField(string labelText = null, string hintText = null)
        {
            style.flexDirection = FlexDirection.Row;
            style.height = 21;

            // label
            LabelElement = new Label
            {
                name = "imgui-text-field-label",
                pickingMode = PickingMode.Ignore,
            };
            Add(LabelElement);

            // imgui input field
            InputFieldElement = new IMGUIContainer(DrawTextField)
            {
                name = "imgui-text-field-container",
                style =
                {
                    flexGrow = 1,
                }
            };
            Add(InputFieldElement);

            // hint label
            HintElement = new Label
            {
                name = "imgui-text-field-hint-label",
                pickingMode = PickingMode.Ignore,
                style =
                {
                    flexGrow = 1,
                    marginLeft = 2,
                    marginRight = 2,
                    marginTop = 1,
                    marginBottom = 1,
                    unityFontStyleAndWeight = FontStyle.Italic,
                }
            };
            InputFieldElement.Add(HintElement);

            LabelText = labelText;
            HintText = hintText;
        }

        public void SetValueWithoutNotify(string newValue)
        {
            _value = newValue;
            RefreshHintDisplay();
        }

        public void RefreshHintDisplay()
        {
            HintElement.style.display = string.IsNullOrEmpty(_value)
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        public void RefreshLabelElementStyle()
        {
            if (string.IsNullOrEmpty(value))
            {
                LabelElement.style.paddingLeft = 0;
                LabelElement.style.paddingRight = 0;
            }
            else
            {
                LabelElement.style.paddingLeft = 3;
                LabelElement.style.paddingRight = 3;
            }
        }

        private void DrawTextField()
        {
            string previousValue = _value;
            EditorGUI.BeginChangeCheck();
            _value = EditorGUILayout.TextField(_value);
            if (EditorGUI.EndChangeCheck())
            {
                RefreshHintDisplay();
                using (ChangeEvent<string> evt = ChangeEvent<string>.GetPooled(previousValue, _value))
                {
                    evt.target = this;
                    SendEvent(evt);
                }
            }
        }
    }
}
