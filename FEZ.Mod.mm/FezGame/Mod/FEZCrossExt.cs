using Common;
using FezEngine;
using FezEngine.Components;
using FezEngine.Effects;
using FezEngine.Effects.Structures;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MonoMod;
using FezEngine.Mod;
using FezGame.Mod;
using FezGame.Structure;

namespace FEZ.Mod.mm.FezGame.Mod {
    public static class FEZCrossExt {

        #if !FNA

        //Weirdly FEZ 1.11 on Windows lacks one method for MenuLevel. We're extending it here.
        public static MenuItem<T> AddItem<T>(this MenuLevel level, string text, Action onSelect, bool defaultItem, Func<T> sliderValueGetter, Action<T, int> sliderValueSetter/*, int at = -1*/) {
            return level.AddItem<T>(text, onSelect, defaultItem, sliderValueGetter, sliderValueSetter, -1);
        }

        #endif

    }
}

