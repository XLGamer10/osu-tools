// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Mods;
using osu.Game.Scoring;

namespace PerformanceCalculatorGUI.Screens.Collections.Autobalance
{
    public sealed class AutobalanceScoreData
    {
        public ProcessorWorkingBeatmap Working { get; }
        public Mod[] Mods { get; }
        public ScoreInfo ScoreInfo { get; }
        public double ExpectedValue { get; }
        public double Weight { get; }

        public AutobalanceScoreData(ProcessorWorkingBeatmap working, Mod[] mods, ScoreInfo scoreInfo, double expectedValue, double weight = 1.0)
        {
            Working = working;
            Mods = mods;
            ScoreInfo = scoreInfo;
            ExpectedValue = expectedValue;
            Weight = weight;
        }
    }
}
