using UnityEngine.Events;

namespace TittyMagic.UI
{
    public class ExperimentalWindow : WindowBase
    {
        private readonly UnityAction _onReturnToParent;

        public ExperimentalWindow(string id, UnityAction onReturnToParent) : base(id)
        {
            _onReturnToParent = onReturnToParent;
        }

        protected override void OnBuild()
        {
            CreateBackButton(false, _onReturnToParent);
        }
    }
}
