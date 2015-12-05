using Common;
using FezEngine.Components;
using FezEngine.Services;
using FezGame.Components;
using FezGame.Tools;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using MonoMod;

namespace FezGame.Structure {
    //internal in FEZ
	public class MenuLevel {
		private string titleString;
		public Action<SpriteBatch, SpriteFont, GlyphTextRenderer, float> OnPostDraw;
		private string xButtonString;
		private string aButtonString;
		private string bButtonString;
		public Action XButtonAction;
		public Action AButtonAction;
		public bool AButtonStarts;
		private bool initialized;
		public readonly List<MenuItem> Items = new List<MenuItem>();
		public MenuLevel Parent;
		public bool IsDynamic;
		public bool Oversized;
		public Action OnReset;
		public Action OnClose;
		public Action OnScrollDown;
		public Action OnScrollUp;
		public virtual string AButtonString { [MonoModIgnore] get; [MonoModIgnore] set; }
		public string BButtonString { [MonoModIgnore] get; [MonoModIgnore] set; }
		public IContentManagerProvider CMProvider { [MonoModIgnore] protected get; [MonoModIgnore] set; }
		public bool ForceCancel { [MonoModIgnore] get; [MonoModIgnore] set; }
		public int SelectedIndex { [MonoModIgnore] get; [MonoModIgnore] set; }
		public MenuItem SelectedItem { [MonoModIgnore] get; }
		public string Title { [MonoModIgnore] get; [MonoModIgnore] set; }
		public bool TrapInput { [MonoModIgnore] get; [MonoModIgnore] set; }
		public string XButtonString { [MonoModIgnore] get; [MonoModIgnore] set; }

		[MonoModIgnore] public extern MenuItem AddItem(string text);
		[MonoModIgnore] public extern MenuItem AddItem(string text, Action onSelect);
		[MonoModIgnore] public extern MenuItem AddItem(string text, Action onSelect, bool defaultItem);
		[MonoModIgnore] public extern MenuItem<T> AddItem<T>(string text, Action onSelect, bool defaultItem, Func<T> sliderValueGetter, Action<T, int> sliderValueSetter);
		[MonoModIgnore] public extern MenuItem AddItem(string text, int at);
		[MonoModIgnore] public extern MenuItem AddItem(string text, Action onSelect, bool defaultItem, int at);
		[MonoModIgnore] public extern MenuItem AddItem(string text, Action onSelect, int at);
		[MonoModIgnore] public extern MenuItem<T> AddItem<T>(string text, Action onSelect, bool defaultItem, Func<T> sliderValueGetter, Action<T, int> sliderValueSetter, int at);

        [MonoModIgnore] public virtual extern void Dispose();
		[MonoModIgnore] public virtual extern void Initialize();
        [MonoModIgnore] public virtual extern void Update(TimeSpan elapsed);
		[MonoModIgnore] public extern bool MoveDown();
		[MonoModIgnore] public extern bool MoveUp();
		[MonoModIgnore] public virtual extern void PostDraw(SpriteBatch batch, SpriteFont font, GlyphTextRenderer tr, float alpha);
		[MonoModIgnore] public virtual extern void Reset();
		[MonoModIgnore] public extern void Select();

	}
}
