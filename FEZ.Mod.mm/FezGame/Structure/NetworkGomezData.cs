using System;
using Microsoft.Xna.Framework;
using FezEngine;

namespace FezGame.Structure {
    [Serializable]
    public class NetworkGomezData {

        //Data ID / "timestamp"
        public int DataId = int.MinValue;

        //Gomez mesh data
        public Vector3 Position;
        public Quaternion Rotation;
        public float Opacity;
        public bool Background;
        public ActionType Action;
        public Matrix TextureMatrix;
        public float EffectBackground;
        public Vector3 Scale;
        public bool NoMoreFez;

        //Other Gomez data
        public Viewpoint Viewpoint;
        public bool InCutscene;
        public bool InMap;
        public bool InMenuCube;
        public bool Paused;

        //Level data
        public string Level;

    }
}

