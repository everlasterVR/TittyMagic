using System.Text;
using TittyMagic.Configs;
using UnityEngine;
using UnityEngine.Events;
using static TittyMagic.Script;

namespace TittyMagic.UI
{
    internal class DevWindow : WindowBase
    {
        public DevWindow(string id, UnityAction onReturnToParent) : base(id)
        {
            buildAction = () =>
            {
                CreateBackButton(false);
                CreateColliderGroupChooser(true);
                CreateLeftDebugArea();
                elements["backButton"].AddListener(onReturnToParent);
                tittyMagic.colliderVisualizer.enabled = true;
                tittyMagic.colliderVisualizer.ShowPreviewsJSON.val = true;
            };

            closeAction = () =>
            {
                tittyMagic.colliderVisualizer.ShowPreviewsJSON.val = false;
                tittyMagic.colliderVisualizer.enabled = false;
            };
        }

        private void CreateColliderGroupChooser(bool rightSide, int spacing = 0)
        {
            var storable = tittyMagic.hardColliderHandler.colliderGroupsJsc;
            elements[$"{storable.name}Spacer"] = tittyMagic.NewSpacer(spacing, rightSide);

            var chooser = tittyMagic.CreatePopupAuto(storable, rightSide, 360f);
            chooser.popup.labelText.color = Color.black;

            elements[storable.name] = chooser;
        }

        private JSONStorableString _leftDebugArea;
        private ColliderConfigGroup[] _colliderConfigs;

        private void CreateLeftDebugArea()
        {
            _leftDebugArea = new JSONStorableString("leftDebugArea", "");
            var textField = tittyMagic.CreateTextField(_leftDebugArea, rightSide: false);
            textField.UItext.fontSize = 28;
            textField.height = 1070;
            elements[_leftDebugArea.name] = textField;

            _colliderConfigs = tittyMagic.hardColliderHandler.colliderConfigs.ToArray();
        }

        public void UpdateLeftDebugInfo()
        {
            var sb = new StringBuilder();
            sb.Append("\n");

            for(int i = 0; i < _colliderConfigs.Length; i++)
            {
                var config = _colliderConfigs[i];
                var leftPos = config.left.collider.attachedRigidbody.position;
                float x = Calc.RoundToDecimals(leftPos.x, 1000f);
                float y = Calc.RoundToDecimals(leftPos.y, 1000f);
                float z = Calc.RoundToDecimals(leftPos.z, 1000f);
                sb.Append($"{config.id} {x} {y} {z}");

                if(i != _colliderConfigs.Length - 1)
                {
                    sb.Append("\n\n");
                }
            }

            _leftDebugArea.val = sb.ToString();
        }
    }
}
