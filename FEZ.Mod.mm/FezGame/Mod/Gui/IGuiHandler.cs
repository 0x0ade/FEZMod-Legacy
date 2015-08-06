using System;
using Microsoft.Xna.Framework.Graphics;
using FezEngine.Components;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace FezGame.Mod.Gui {
    public interface IGuiHandler {

        SpriteBatch SpriteBatch { get; set; }
        GlyphTextRenderer GTR { get; set; }

        List<GuiWidget> Widgets { get; set; }
        List<Action> Scheduled { get; set; }

        Color DefaultForeground { get; set; }
        Color DefaultBackground { get; set; }

    }
}

