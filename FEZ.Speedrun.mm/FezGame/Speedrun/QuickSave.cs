using System;
using FezGame.Structure;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using FezEngine;
using System.Collections.Generic;

namespace FezGame.Speedrun {
    public class QuickSave {

        public SaveData SaveData = new SaveData();
        public Texture2D Thumbnail;

        public TimeSpan Time = new TimeSpan(0);
        public TimeSpan TimeLoading = new TimeSpan(0);

        public List<Vector3> GomezPositions = new List<Vector3>();
        public List<Vector3> GomezVelocities = new List<Vector3>();
        public List<ActionType> GomezActions = new List<ActionType>();
        public List<Viewpoint> GomezRotations = new List<Viewpoint>();
        public List<Vector3> GomezCamPositions = new List<Vector3>();

    }
}

