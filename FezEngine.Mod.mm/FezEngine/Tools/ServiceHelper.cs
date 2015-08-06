using System;
using System.Collections.Generic;
using FezGame.Mod;
using Microsoft.Xna.Framework;
using MonoMod;

namespace FezEngine.Tools {
    public static class ServiceHelper {

        public static Game Game { get; set; }

        private static List<object> services = new List<object>();
        private static readonly List<IGameComponent> components = new List<IGameComponent>();
        private static readonly Dictionary<Type, IGameComponent> componentMap = new Dictionary<Type, IGameComponent>();
        private static readonly Dictionary<IGameComponent, List<Type>> componentMapReverse = new Dictionary<IGameComponent, List<Type>>();

        [MonoModIgnore]
        public static void InjectServices(object service) {
        }

        public static void orig_InitializeServices() {
        }

        public static void InitializeServices() {
            int oldCount = services.Count;
            orig_InitializeServices();
            FEZMod.Initialize();
            for (int i = oldCount; i < services.Count; i++) {
                InjectServices(services[i]);
            }
        }

        public static T orig_Get<T>() where T : class {
            return null;
        }

        public static T Get<T>() where T : class {
            T obj = orig_Get<T>();
            if (obj != null) {
                return obj;
            }
            if (FEZMod.GetComponentsAsServices && typeof(IGameComponent).IsAssignableFrom(typeof(T))) {
                return (T) GetComponent(typeof(T));
            }
            return null;
        }

        public static object orig_Get(Type type) {
            return null;
        }

        public static object Get(Type type) {
            object obj = orig_Get(type);
            if (obj != null) {
                return obj;
            }
            if (FEZMod.GetComponentsAsServices && typeof(IGameComponent).IsAssignableFrom(type)) {
                return GetComponent(type);
            }
            return null;
        }

        public static void orig_AddComponent(IGameComponent component, bool addServices) {
        }

        public static void AddComponent(IGameComponent component, bool addServices) {
            orig_AddComponent(component, addServices);

            if (!addServices && FEZMod.HandleComponents) {
                components.Add(component);
                componentMap[component.GetType()] = component;
            }
        }

        public static void orig_RemoveComponent<T>(T component) where T : IGameComponent {
        }

        public static void RemoveComponent<T>(T component) where T : IGameComponent {
            if (!FEZMod.HandleComponents) {
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
            if (!FEZMod.HandleComponents) {
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

