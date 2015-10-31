using System.Reflection;
using System.Collections.Generic;
using FezGame.Structure;

namespace FezGame.Mod {
    public static class FEZModSaveHelper {

        private static Dictionary<string, FieldInfo> fieldCache = new Dictionary<string, FieldInfo>();

        public static T Get<T>(this SaveData saveData, string fieldName) {
            FieldInfo field;
            if (fieldCache.ContainsKey(fieldName)) {
                field = fieldCache[fieldName];
            } else {
                field = saveData.GetType().GetField(fieldName);
                fieldCache[fieldName] = field;
            }
            return (T) field.GetValue(saveData);
        }

        public static void Set(this SaveData saveData, string fieldName, object value) {
            FieldInfo field;
            if (fieldCache.ContainsKey(fieldName)) {
                field = fieldCache[fieldName];
            } else {
                field = saveData.GetType().GetField(fieldName);
                fieldCache[fieldName] = field;
            }
            field.SetValue(saveData, value);
        }

    }
}

