using System.Collections.Generic;

namespace TittyMagic.UI
{
    public interface IWindow
    {
        string GetId();

        IWindow GetActiveNestedWindow();

        void Rebuild();

        List<UIDynamicSlider> GetSliders();

        void Clear();

        void ClosePopups();
    }
}
