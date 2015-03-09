using System;
using Microsoft.Xna.Framework;
using MonoMod;

namespace FezGame.Structure {
    [MonoModIgnore]
    public interface MenuItem {

        //stub clone of disassembled code - use with care

        float ActivityRatio { get; }

        bool Centered { get; set; }

        bool Disabled { get; set; }

        bool Hidden { get; set; }

        Rectangle HoverArea { get; set; }

        bool Hovered { get; set; }

        bool InError { get; set; }

        bool IsGamerCard { get; set; }

        bool IsSlider { get; set; }

        string LocalizationTagFormat { get; set; }

        bool LocalizeSliderValue { get; set; }

        MenuLevel Parent { get; set; }

        bool Selectable { get; set; }

        Action Selected { get; set; }

        TimeSpan SinceHovered { get; set; }

        Vector2 Size { get; set; }

        Func<string> SuffixText { get; set; }

        string Text { get; set; }

        bool UpperCase { get; set; }

        void ClampTimer();

        void OnSelected();

        void Slide(int direction);

    }
}

