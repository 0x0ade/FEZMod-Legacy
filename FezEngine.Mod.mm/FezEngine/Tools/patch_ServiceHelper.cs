using System;
using System.Collections.Generic;
using FezEngine.Mod;
using Microsoft.Xna.Framework;
using MonoMod;

namespace FezEngine.Tools {
    public static class patch_ServiceHelper {
        
        public static Game Game { [MonoModIgnore] get; [MonoModIgnore] set; }
        
        public static Dictionary<Type, Func<IGameComponent, bool, bool>> AddHooks = new Dictionary<Type, Func<IGameComponent, bool, bool>>();

        private static List<object> services = new List<object>();
        private static readonly List<IGameComponent> components = new List<IGameComponent>();
        private static readonly Dictionary<Type, IGameComponent> componentMap = new Dictionary<Type, IGameComponent>();
        private static readonly Dictionary<IGameComponent, List<Type>> componentMapReverse = new Dictionary<IGameComponent, List<Type>>();

        [MonoModIgnore]
        public static extern void InjectServices(object service);

        public static extern void orig_InitializeServices();
        public static void InitializeServices() {
            int oldCount = services.Count;
            orig_InitializeServices();
            FEZModEngine.PassInitialize();
            for (int i = oldCount; i < services.Count; i++) {
                InjectServices(services[i]);
            }
        }

        public static extern T orig_Get<T>() where T : class;
        public static T Get<T>() where T : class {
            T obj = orig_Get<T>();
            if (obj != null || !FEZModEngine.GetComponentsAsServices) {
                return obj;
            }
            if (typeof(IGameComponent).IsAssignableFrom(typeof(T))) {
                return (T) GetComponent(typeof(T));
            }
            return null;
        }

        public static extern object orig_Get(Type type);
        public static object Get(Type type) {
            object obj = orig_Get(type);
            if (obj != null || !FEZModEngine.GetComponentsAsServices) {
                return obj;
            }
            if (typeof(IGameComponent).IsAssignableFrom(type)) {
                return GetComponent(type);
            }
            return null;
        }

        public static extern void orig_AddComponent(IGameComponent component, bool addServices);
        public static void AddComponent(IGameComponent component, bool addServices) {
            Func<IGameComponent, bool, bool> hook;
            if (AddHooks.TryGetValue(component.GetType(), out hook) && !hook(component, addServices)) {
                return;
            }
            
            orig_AddComponent(component, addServices);

            if (!addServices && FEZModEngine.HandleComponents) {
                components.Add(component);
                componentMap[component.GetType()] = component;
            }
        }

        public static extern void orig_RemoveComponent<T>(T component) where T : IGameComponent;
        public static void RemoveComponent<T>(T component) where T : IGameComponent {
            if (!FEZModEngine.HandleComponents) {
                orig_RemoveComponent(component);
                return;
            }

            components.Remove(component);
            componentMap.Remove(typeof(T));
            if (componentMapReverse.ContainsKey(component)) {
                List<Type> reverseList = componentMapReverse[component];
                for (int i = 0; i < reverseList.Count; i++) {
                    componentMap.Remove(reverseList[i]);
                }
                componentMapReverse.Remove(component);
            }

            orig_RemoveComponent(component);
        }

        public static T GetComponent<T>() where T : IGameComponent {
            return (T) GetComponent(typeof(T));
        }

        public static object GetComponent(Type type) {
            if (!FEZModEngine.HandleComponents) {
                return null;
            }

            if (componentMap.ContainsKey(type)) {
                return componentMap[type];
            }

            for (int i = 0; i < components.Count; i++) {
                IGameComponent component = components[i];
                if (type.IsInstanceOfType(component)) {
                    componentMap[type] = component;
                    List<Type> reverseList;
                    if (componentMapReverse.ContainsKey(component)) {
                        reverseList = componentMapReverse[component];
                    } else {
                        reverseList = new List<Type>();
                    }
                    reverseList.Add(type);
                    componentMapReverse[component] = reverseList;
                    return component;
                }
            }

            return null;
        }

    }
}
