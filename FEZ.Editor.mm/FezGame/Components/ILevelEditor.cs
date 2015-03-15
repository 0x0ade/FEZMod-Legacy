using System;
using System.Collections.Generic;
using FezGame.Editor.Widgets;
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

        List<EditorWidget> Widgets { get; set; }
        List<Action> Scheduled { get; set; }

        Level CreateNewLevel(string name, int width, int height, int depth, string trileset);
        TrileInstance CreateNewTrile(int trileId, TrileEmplacement emplacement);
        void AddTrile(TrileInstance trile);

    }
}

