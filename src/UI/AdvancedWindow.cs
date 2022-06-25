// ReSharper disable MemberCanBePrivate.Global
using System;
using UnityEngine;
using static TittyMagic.UI.UIHelpers;

namespace TittyMagic.UI
{
    internal class AdvancedWindow : IWindow
    {
        private readonly Script _script;

        public int Id() => 4;

        public AdvancedWindow(Script script)
        {
            _script = script;
        }

        public void Rebuild()
        {

        }

        public void Clear()
        {

        }
    }
}
