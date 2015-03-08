using System;
using FezGame.Structure;

namespace FezGame.Mod {
    public abstract class FezModule {

        public abstract string Name { get; }
        public abstract string Author { get; }
        public abstract string Version { get; }

        public FezModule() {
        }

        public void PreInitialize() {}
        public virtual void ParseArgs(string[] args) {}
        public virtual void Initialize() {}
        public virtual void LoadEssentials() {}
        public virtual void Preload() {}
        public virtual void SaveClear(SaveData saveData) {}
        public virtual void SaveClone(SaveData source, SaveData dest) {}

    }
}

