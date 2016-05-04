using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using FezEngine;
using FezGame.Editor;
using FezEngine.Mod;

namespace FezGame.Mod.Gui {
    public class ArtObjectPickerWidget : AssetPickerWidget {

        public ArtObjectPickerWidget(Game game) 
            : base(game) {
            LargeSpacingX = 64f;
            LargeRows = 3f;
        }

        public override void Update(GameTime gameTime) {
            if (Widgets.Count == PermanentWidgets.Length) {
                UpdateWidgets();
            }

            base.Update(gameTime);
        }
        
        public override void UpdateWidgets() {
            for (int i = 0; i < Widgets.Count - PermanentWidgets.Length; i++) {
                ArtObjectButtonWidget widget = (ArtObjectButtonWidget) Widgets[i];
                widget.Dispose();
            }
            
            Widgets.Clear();
            ScrollOffset = 0f;
            
            IEnumerable<string> list = CMProvider.GetAllIn(ContentPaths.ArtObjects);
            List<string> added = new List<string>();
            foreach (string path in list) {
                string item = path.Substring(ContentPaths.ArtObjects.Length + 1).ToUpper();
                if (item.Contains("\\")) {
                    item = item.Substring(0, item.IndexOf('\\'));
                }
                if (item.Contains("/")) {
                    item = item.Substring(0, item.IndexOf('/'));
                }
                if (added.Contains(item)) {
                    continue;
                }
                added.Add(item);
                
                try {
                    Widgets.Add(new ArtObjectButtonWidget(Game, CMProvider.CurrentLevel.Load<ArtObject>(path)));
                } catch {
                    //It's not an art object (f.e. alternative menu cube skin)
                }
            }
            
            Widgets.AddRange(PermanentWidgets);
        }
        
        public override void Search(string[] items) {
            for (int i = 0; i < Widgets.Count - PermanentWidgets.Length; i++) {
                ArtObjectButtonWidget widget = (ArtObjectButtonWidget) Widgets[i];
                string name = widget.AO.Name.ToLower();
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

