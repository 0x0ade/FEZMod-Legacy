using System;
using Microsoft.Xna.Framework;
using System.Reflection;
using FezEngine.Mod;

namespace FezGame.Editor {
    public static class EditorUtils {

        public static ArrayCache<BoundingBox> a_BoundingBox_6 = new ArrayCache<BoundingBox>(6);

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

    }
}

