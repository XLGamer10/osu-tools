// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace PerformanceCalculatorGUI.Screens.Collections.Autobalance
{
    public readonly struct AutobalanceProgress
    {
        public double Value { get; }
        public string? Stage { get; }
        public int? Completed { get; }
        public int? Total { get; }

        public AutobalanceProgress(double value, string? stage = null, int? completed = null, int? total = null)
        {
            Value = value;
            Stage = stage;
            Completed = completed;
            Total = total;
        }
    }
}
