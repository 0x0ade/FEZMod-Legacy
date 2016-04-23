using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using FezGame.Services;
using FezEngine.Tools;

namespace FezGame.Speedrun.Clocks {
    public class SpeedrunClock : ISpeedrunClock {

        protected TimeSpan timePerFrame = TimeSpan.FromMilliseconds(17);

        public bool InGame { get; set; }

        protected IGameStateManager gameState_;
        protected IGameStateManager GameState {
            get {
                if (gameState_ != null) {
                    return gameState_;
                }
                gameState_ = ServiceHelper.Get<IGameStateManager>();
                return gameState_;
            }
        }

        public TimeSpan Time { get; set; }
        public TimeSpan TimeLoading { get; set; }
        public List<Split> Splits { get; set; }
        public double Direction { get; set; }

        public bool Strict { get; set; }

        public bool Running { get; set; }

        public bool Paused { get; set; }

        protected List<SplitCase> defaultSplitCases = new List<SplitCase>();
        public ReadOnlyCollection<SplitCase> DefaultSplitCases {
            get {
                return defaultSplitCases.AsReadOnly();
            }
        }

        public List<SplitCase> SplitCases { get; set; }

        protected Thread clockThread;
        protected List<string> scheduledSplits = new List<string>();

        public SpeedrunClock() {
            InGame = true;
            Time = new TimeSpan();
            TimeLoading = new TimeSpan();
            Splits = new List<Split>();
            Direction = 1D;
            Strict = true;
            Running = false;
            SplitCases = new List<SplitCase>();
        }

        public void Split(string text) {
            scheduledSplits.Add(text);
        }

        protected void Split_(string text) {
            Splits.Add(new Split(text, new TimeSpan()));
        }

        public void Update() {
            if (!Running || Paused || GameState == null || GameState.Loading) {
                return;
            }

            while (scheduledSplits.Count > 0) {
                Split_(scheduledSplits[0]);
                scheduledSplits.RemoveAt(0);
            }

            if (GameState.TimePaused && !Strict) {
                return;
            }
            TimeSpan timeThisFrame = TimeSpan.FromTicks((long) (timePerFrame.Ticks * Direction));
            Time += timeThisFrame;
            if (Time.Ticks < 0) {
                timeThisFrame = Time = new TimeSpan(0);
            }
            if (Splits.Count > 0) {
                Splits[Splits.Count-1].Time += timeThisFrame;
            }

            string split = null;
            foreach (SplitCase tosplit in FezSpeedrun.DefaultSplitCases) {
                if (split != null) {
                    break;
                }
                split = tosplit(this);
            }
            foreach (SplitCase tosplit in defaultSplitCases) {
                if (split != null) {
                    break;
                }
                split = tosplit(this);
            }
            foreach (SplitCase tosplit in SplitCases) {
                if (split != null) {
                    break;
                }
                split = tosplit(this);
            }

            if (split != null) {
                Split_(split);
            }
        }

        public void Dispose() {
            Running = false;
        }

    }
}

