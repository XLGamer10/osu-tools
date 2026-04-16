// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;

namespace PerformanceCalculatorGUI.Configuration
{
    public class DifficultyTuningManager<TConstants>
    {
        public Bindable<TConstants> Current { get; }

        public TConstants Default { get; }

        public DifficultyTuningManager(TConstants defaultConstants)
        {
            Default = defaultConstants;
            Current = new Bindable<TConstants>(defaultConstants);
        }

        public void Reset() => Current.Value = Default;
    }
}
