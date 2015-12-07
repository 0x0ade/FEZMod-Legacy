using System;
using FezGame.Structure;
using FezEngine.Tools;
using FezEngine.Structure;
using FezGame.Components;

namespace FezGame.Mod {
    public abstract class FezModule {

        public abstract string Name { get; }
        public abstract string Author { get; }
        public abstract string Version { get; }

        public FezModule() {
        }

        public virtual void PreInitialize() {}
        public virtual void ParseArgs(string[] args) {}
        public virtual void Initialize() {}
        public virtual void InitializeMenu(MenuBase mb) {}
        public virtual void LoadComponents(Fez game) {}
        public virtual void Exit() {}
        public virtual void HandleCrash(Exception e) {}
        public virtual void LoadEssentials() {}
        public virtual void Preload() {}
        public virtual void SaveClear(SaveData saveData) {}
        public virtual void SaveClone(SaveData source, SaveData dest) {}
        public virtual void SaveRead(SaveData saveData, CrcReader reader) {}
        public virtual void SaveWrite(SaveData saveData, CrcWriter writer) {}
        public virtual string ProcessLevelName(string levelName) {return levelName;}
        public virtual void ProcessLevelData(Level levelData) {}

    }
}

