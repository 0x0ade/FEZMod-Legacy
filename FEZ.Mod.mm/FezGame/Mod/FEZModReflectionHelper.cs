using System;
using System.Collections.Generic;
using System.Reflection;
using FezEngine.Mod;

namespace FezGame.Mod {
    public static class FEZModReflectionHelper {

        private readonly static Dictionary<string, Type> CacheTypes = new Dictionary<string, Type>(128);

        public static Type DeMMify<T>(bool fallback = true) {
            return DeMMify(typeof(T), fallback);
        }

        public static Type DeMMify(this Type origType, bool fallback = true) {
            Type type_ = null;
            if (CacheTypes.TryGetValue(origType.FullName, out type_)) {
                return type_;
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            List<Assembly> delayedAssemblies = new List<Assembly>();

            foreach (Assembly assembly in assemblies) {
                if (assembly.GetName().Name.EndsWith(".mm")) {
                    continue;
                }
                try {
                    Type[] types = assembly.GetTypes();
                    foreach (Type type in types) {
                        if (type.FullName == origType.FullName) {
                            CacheTypes[origType.FullName] = type;
                            return type;
                        }
                        //TODO handle generic types
                    }
                } catch (ReflectionTypeLoadException e) {
                    ModLogger.Log("FEZMod", "Failed searching a type in FEZModReflectionHelper's GetFEZType.");
                    ModLogger.Log("FEZMod", "Assembly: " + assembly.GetName().Name);
                    ModLogger.Log("FEZMod", e.Message);
                    foreach (Exception le in e.LoaderExceptions) {
                        ModLogger.Log("FEZMod", le.Message);
                    }
                }
            }

            return fallback ? origType : null;
        }

    }
}

