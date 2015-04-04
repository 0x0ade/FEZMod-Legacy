using System;
using Common;
using FezEngine.Structure;

namespace FezGame.Mod {
    /// <summary>
    /// Brutal mono mod workarounds, for example avoid importing generic FezEngine types.
    /// </summary>
    public static class BrutalMonoModWorkarounds {

        public static T GetValueInTheMostBrutalWayEver<T>(this Dirtyable<T> dirtyable) {
            return dirtyable.Value;
        }

    }
}

