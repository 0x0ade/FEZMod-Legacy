using System;

namespace FezGame.Speedrun {
    public class Split {

        public string Level;
        public TimeSpan Time;

        public Split()
            : this("", new TimeSpan()) {
        }

        public Split(string level, TimeSpan time) {
            Level = level;
            Time = time;
        }

    }
}

