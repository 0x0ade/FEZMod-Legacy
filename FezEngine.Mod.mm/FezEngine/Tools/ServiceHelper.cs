using System;
using System.Collections.Generic;
using FezGame.Mod;
using Microsoft.Xna.Framework;

namespace FezEngine.Tools {
    public class ServiceHelper {

        public static Game Game { get; set; }

        private static readonly List<object> services = new List<object>();

        public static void orig_InjectServices(object service) {
        }

        public static void InjectServices(object service) {
            orig_InjectServices(service);
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

    }
}

