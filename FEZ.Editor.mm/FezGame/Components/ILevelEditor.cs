using System;
using FezEngine;
using FezEngine.Structure;
using FezEngine.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components {
    public interface ILevelEditor {

        SpriteBatch SpriteBatch { get; set; }
        GlyphTextRenderer GTR { get; set; }

        DateTime BuildDate { get; }

        TrileInstance HoveredTrile { get; set; }
        BoundingBox HoveredBox { get; set; }
        FaceOrientation HoveredFace { get; set; }
        int TrileId { get; set; }

    }
}

