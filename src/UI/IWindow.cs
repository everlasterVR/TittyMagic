using System.Collections.Generic;

namespace TittyMagic.UI
{
    internal interface IWindow
    {
        int Id();

        Dictionary<string, UIDynamic> GetElements();

        IWindow GetActiveNestedWindow();

        void Rebuild();

        List<UIDynamicSlider> GetSliders();

        void Clear();

        void ClosePopups();
    }
}
