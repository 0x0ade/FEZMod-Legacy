using System;
using Microsoft.Xna.Framework;

namespace FezGame.Editor {
    public static class VectorHelper {

        public static String ToString(this Vector2 v) {
            return v.X + ", " + v.Y;
        }

        public static String ToString(this Vector3 v) {
            return v.X + ", " + v.Y + ", " + v.Z;
        }

    }
}

