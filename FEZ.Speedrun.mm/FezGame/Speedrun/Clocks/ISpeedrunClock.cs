using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FezGame.Services;
using FezEngine.Services;

namespace FezGame.Speedrun.Clocks {
    public delegate string SplitCase(ISpeedrunClock clock);
    public interface ISpeedrunClock {

        bool InGame { get; }

        TimeSpan Time { get; }
        TimeSpan TimeLoading { get; }
        List<Split> Splits { get; set; }
        bool Strict { get; set; }
        bool Running { get; set; }
        bool Paused { get; set; }

        ReadOnlyCollection<SplitCase> DefaultSplitCases { get; }
        List<SplitCase> SplitCases { get; set; }

        void Split(string text);
        void Update();

    }
}

