using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using FezEngine.Components;
using MonoMod;

namespace FezGame.Structure {
    [MonoModIgnore]
    public class MenuLevel {

        //stub clone of disassembled code - use with care

        private string titleString;

        public Action<SpriteBatch, SpriteFont, GlyphTextRenderer, float> OnPostDraw;

        private string xButtonString;

        private string aButtonString;

        private string bButtonString;

        public Action XButtonAction;

        public Action AButtonAction;

        public bool AButtonStarts;

        private bool initialized;

        public readonly List<MenuItem> Items;

        public MenuLevel Parent;

        public bool IsDynamic;

        public bool Oversized;

        public Action OnScrollUp;

        public Action OnScrollDown;

        public Action OnClose;

        public Action OnReset;

        public MenuItem AddItem(string text, Action onSelect, int at) {
            return null;
        }

        public MenuItem AddItem(string text) {
            return null;
        }

        public MenuItem AddItem(string text, Action onSelect) {
            return null;
        }

        public MenuItem AddItem(string text, Action onSelect, bool defaultItem) {
            return null;
        }

        /*public MenuItem<T> AddItem<T>(string text, Action onSelect, bool defaultItem, Func<T> sliderValueGetter, Action<T, int> sliderValueSetter) {
            return null
        }*/

        public MenuItem AddItem(string text, Action onSelect, bool defaultItem, int at) {
            return null;
        }

        /*public MenuItem<T> AddItem<T>(string text, Action onSelect, bool defaultItem, Func<T> sliderValueGetter, Action<T, int> sliderValueSetter, int at) {
            return null;
        }*/

        public MenuItem AddItem(string text, int at) {
            return null;
        }

        public virtual void Dispose() {
        }

        public virtual void Initialize() {
        }

        public bool MoveDown() {
            return false;
        }

        public bool MoveUp() {
            return false;
        }

        public virtual void PostDraw(SpriteBatch batch, SpriteFont font, GlyphTextRenderer tr, float alpha) {
        }

        public virtual void Reset() {
        }

        public void Select() {
        }

        public virtual void Update(TimeSpan elapsed) {
        }

    }
}

