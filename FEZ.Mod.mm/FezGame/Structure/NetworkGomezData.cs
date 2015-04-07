using System;
using System.Runtime.Serialization;
using Microsoft.Xna.Framework;

namespace FezGame.Structure {
    [Serializable]
    public class NetworkGomezData {

        public int DataId = int.MinValue;

        public Vector3 Position;
        public Quaternion Rotation;
        public float Opacity;
        public bool Background;
        public ActionType Action;
        public Matrix TextureMatrix;
        public float EffectBackground;
        public Vector3 Scale;
        public bool NoMoreFez;

    }
}

