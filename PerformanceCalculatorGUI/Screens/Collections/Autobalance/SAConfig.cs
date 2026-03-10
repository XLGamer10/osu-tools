// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace PerformanceCalculatorGUI.Screens.Collections.Autobalance
{
    public class SAConfig
    {
        public int Iterations { get; init; } = 5000;
        public double InitialTemperature { get; init; } = 100.0;
        public double MinTemperature { get; init; } = 0.001;
        public double CoolingRate { get; init; } = 0.999;
        public int Restarts { get; init; } = 5;
        public int Seed { get; init; } = 42;

        public static SAConfig Default => new SAConfig();
    }
}
