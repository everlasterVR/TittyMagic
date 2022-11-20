using System.Text;
using TittyMagic.Models;
using TittyMagic.Handlers;
using UnityEngine;
using UnityEngine.Events;
using static TittyMagic.Script;

namespace TittyMagic.UI
{
    public class DevWindow : WindowBase
    {
        private readonly UnityAction _onReturnToParent;

        public DevWindow(string id, UnityAction onReturnToParent) : base(id)
        {
            _onReturnToParent = onReturnToParent;
        }

        protected override void OnBuild()
        {
            CreateBackButton(false, _onReturnToParent);

            /* Collider group chooser */
            {
                var storable = HardColliderHandler.colliderGroupsJsc;
                var chooser = tittyMagic.CreatePopupAuto(storable, true, 360f);
                chooser.popup.labelText.color = Color.black;
                elements[storable.name] = chooser;
            }

            CreateCalibrateButton(tittyMagic.calibrate, true);

            /* Left debug info area */
            {
                _leftDebugArea = new JSONStorableString("leftDebugArea", "");
                var textField = tittyMagic.CreateTextField(_leftDebugArea, rightSide: false);
                textField.UItext.fontSize = 28;
                textField.height = 1070;
                elements[_leftDebugArea.name] = textField;
                _hardColliderGroups = HardColliderHandler.hardColliderGroups.ToArray();
            }

            // HardColliderHandler.colliderVisualizer.enabled = true;
            // HardColliderHandler.colliderVisualizer.ShowPreviewsJSON.val = true;
        }

        protected override void OnClose()
        {
            HardColliderHandler.colliderVisualizer.ShowPreviewsJSON.val = false;
            HardColliderHandler.colliderVisualizer.enabled = false;
        }

        private JSONStorableString _leftDebugArea;
        private HardColliderGroup[] _hardColliderGroups;

        public void UpdateLeftDebugInfo()
        {
            var sb = new StringBuilder();
            sb.Append("\n");

            // for(int i = 0; i < _hardColliderGroups.Length; i++)
            // {
            //     var group = _hardColliderGroups[i];
            //     var leftPos = group.left.collider.attachedRigidbody.position;
            //     float x = Calc.RoundToDecimals(leftPos.x);
            //     float y = Calc.RoundToDecimals(leftPos.y);
            //     float z = Calc.RoundToDecimals(leftPos.z);
            //     sb.Append($"{group.id} {x} {y} {z}");
            //
            //     if(i != _hardColliderGroups.Length - 1)
            //     {
            //         sb.Append("\n\n");
            //     }
            // }

            _leftDebugArea.val = sb.ToString();
        }
    }
}
