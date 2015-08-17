using System;
using System.Collections.Generic;
using FezEngine;
using FezEngine.Structure;
using FezEngine.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components {
    public interface ILevelEditor {

        TrileInstance HoveredTrile { get; set; }
        BoundingBox HoveredBox { get; set; }
        FaceOrientation HoveredFace { get; set; }
        int TrileId { get; set; }

        bool ThumbnailScheduled { get; set; }
        int ThumbnailX { get; set; }
        int ThumbnailY { get; set; }
        int ThumbnailSize { get; set; }

        Level CreateNewLevel(string name, int width, int height, int depth, string trileset);
        TrileInstance CreateNewTrile(int trileId, TrileEmplacement emplacement);
        void AddTrile(TrileInstance trile);

    }
}

