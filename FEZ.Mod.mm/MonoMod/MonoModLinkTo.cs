using System;

namespace MonoMod {
    [MonoModIgnore]
    public class MonoModLinkTo : Attribute {
        public MonoModLinkTo(Delegate d) {
        }
        public MonoModLinkTo(Type t, string n) {
        }
        public MonoModLinkTo(string t, string n) {
        }
    }
}

