using System;

namespace FezGame.Mod {
    public abstract class FezModule {

        public abstract string Name { get; }
        public abstract string Author { get; }
        public abstract string Version { get; }

        public FezModule() {
        }

        public void Initialize() {}
        public void ParseArgs(string[] args) {}

    }
}

