using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using FezGame.Editor;

namespace FezGame.Mod.Gui {
    public class TrilePickerWidget : AssetPickerWidget {

        [ServiceDependency]
        public IGameLevelManager LevelManager { get; set; }

        protected TrileSet TrileSetOld;

        public Texture2D TrileAtlas { get; protected set; }

        public TrilePickerWidget(Game game) 
            : base(game) {
        }

        public override void Update(GameTime gameTime) {
            if (TrileSetOld != LevelManager.TrileSet) {
                UpdateWidgets();
                TrileSetOld = LevelManager.TrileSet;
            }

            base.Update(gameTime);
        }
        
        public override void UpdateWidgets() {
            Widgets.Clear();
            ScrollOffset = 0f;

            if (TrileAtlas != null) {
                TrileAtlas.Dispose();
            }
            TrileAtlas = LevelManager.TrileSet.TextureAtlas.MaxAlpha();

            foreach (Trile trile in LevelManager.TrileSet.Triles.Values) {
                Widgets.Add(new TrileButtonWidget(Game, trile) {
                    TrileAtlas = TrileAtlas
                });
            }
            
            Widgets.AddRange(PermanentWidgets);
        }
        
        public override void Search(string[] items) {
            for (int i = 0; i < Widgets.Count - PermanentWidgets.Length; i++) {
                TrileButtonWidget widget = (TrileButtonWidget) Widgets[i];
                string name = widget.Trile.Name.ToLower();
                bool contains = false;
                
                for (int ii = 0; ii < items.Length; ii++) {
                    if (name.Contains(items[ii])) {
                        contains = true;
                        break;
                    }
                }
                
                widget.Visible = contains;
            }
        }
        
    }
}

