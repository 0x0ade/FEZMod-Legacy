using System;
using Microsoft.Xna.Framework;
using System.Reflection;
using FezEngine.Mod;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using FezEngine.Structure;
using FezEngine.Tools;

namespace FezGame.Editor {
    public static class EditorUtils {

        public static ArrayCache<BoundingBox> a_BoundingBox_6 = new ArrayCache<BoundingBox>(6);
        public static Cache<List<TrileInstance>> l_TrileInstance = new Cache<List<TrileInstance>>();

        public static T GetPrivate<T>(this object instance, string fieldName) {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null) {
                return default (T);
            }
            return (T) field.GetValue(instance);
        }

        public static T GetPrivateStatic<T>(this Type type, string fieldName) {
            FieldInfo field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            if (field == null) {
                return default (T);
            }
            return (T) field.GetValue(null);
        }

        public static bool Inside(this Vector2 point, Rectangle rectangle) {
            return
                rectangle.X <= point.X && point.X <= rectangle.X + rectangle.Width &&
                rectangle.Y <= point.Y && point.Y <= rectangle.Y + rectangle.Height;
        }

        public static string ToString(Vector2 v) {
            return v.X + ", " + v.Y;
        }

        public static string ToString(Vector3 v) {
            return v.X + ", " + v.Y + ", " + v.Z;
        }
        
        public static Texture2D MaxAlpha(this Texture2D texIn) {
            //WARNING: It is not performant as it pulls the orig texture from the GPU / VRAM, modifies it in RAM and then pushes it back to VRAM.
            //TODO: Learn how to use FBOs in FNA / XNA.
            
            Texture2D texOut = new Texture2D(texIn.GraphicsDevice, texIn.Width, texIn.Height);
            Color[] texData = new Color[texOut.Width * texOut.Height];
            texIn.GetData(texData);
            for (int i = 0; i < texData.Length; i++) {
                texData[i].A = 255;
            }
            texOut.SetData(texData);

            return texOut;
        }
        
        public static void SetRotation(this Mesh mesh, float phi) {
            mesh.FirstGroup.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, phi);
        }
        
        public static void SetRotation(this Mesh mesh, Trile trile, float phi) {
            if (ActorTypeExtensions.IsPickable(trile.ActorSettings.Type)) {
                phi = (float) FezMath.Round(phi / 1.57079637050629) * 1.570796f;
            }
            mesh.FirstGroup.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, phi);
            if (trile.ActorSettings.Type == ActorType.CubeShard || trile.ActorSettings.Type == ActorType.SecretCube || trile.ActorSettings.Type == ActorType.PieceOfHeart) {
                mesh.FirstGroup.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Left, (float) Math.Asin(Math.Sqrt(2.0) / Math.Sqrt(3.0))) * Quaternion.CreateFromAxisAngle(Vector3.Down, 0.7853982f) * mesh.FirstGroup.Rotation;
            }
        }
        
        public static void SetRotation(this Mesh mesh, TrileInstance trile) {
            mesh.SetRotation(trile.Trile, trile.Phi);
        }

    }
}

