using System;
using Microsoft.Xna.Framework;
using MonoMod;

namespace FezEngine.Structure.Geometry {
    [MonoModIgnore]
    /// <summary>
    /// Probably a FEZ 1.12 - only type.
    /// </summary>
    public struct IndexedVector4 {
        public Vector4 Data;
        public float Index;
    }
}

