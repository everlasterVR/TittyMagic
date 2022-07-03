using System.Collections.Generic;

namespace TittyMagic.UI
{
    internal interface IWindow
    {
        Dictionary<string, UIDynamic> GetElements();

        int Id();

        void Rebuild();

        void Clear();
    }
}
