using FezEngine.Tools;
using FezGame.Tools;
using Microsoft.Xna.Framework;
using System;
using System.Globalization;
using MonoMod;

namespace FezGame.Structure {
    //internal in FEZ
    
    [MonoModIgnore]
    public interface MenuItem {
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
    
    [MonoModIgnore]
	public class MenuItem<T> : MenuItem {
		private static readonly TimeSpan HoverGrowDuration;
        
		public Action<T, int> SliderValueSetter;
		public Func<T> SliderValueGetter;
		private string text;
		public float ActivityRatio { get; }
		public bool Centered { get; set; }
		public bool Disabled { get; set; }
		public bool Hidden { get; set; }
		public Rectangle HoverArea { get; set; }
		public bool Hovered { get; set; }
		public bool InError { get; set; }
		public bool IsGamerCard { get; set; }
		public bool IsSlider { get; set; }
		public string LocalizationTagFormat { get; set; }
		public bool LocalizeSliderValue { get; set; }
		public MenuLevel Parent { get; set; }
		public bool Selectable { get; set; }
		public Action Selected { get; set; }
		public TimeSpan SinceHovered { get; set; }
		public Vector2 Size { get; set; }
		public Func<string> SuffixText { get; set; }
		public string Text { get; set; }
		public bool UpperCase { get; set; }

		public MenuItem() {
		}

		public extern void ClampTimer();
		public extern void OnSelected();
		public extern void Slide(int direction);
		public override extern string ToString();
	}
}
