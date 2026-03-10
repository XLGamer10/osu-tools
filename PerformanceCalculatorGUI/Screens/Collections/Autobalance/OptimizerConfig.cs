// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace PerformanceCalculatorGUI.Screens.Collections.Autobalance
{
    public class OptimizerConfig
    {
        /// <summary>
        /// Maximum number of generations (CMA-ES) to run per restart.
        /// </summary>
        public int MaxGenerations { get; init; } = 300;

        /// <summary>
        /// Population size (lambda). If 0, uses CMA-ES default: 4 + floor(3 * ln(n)).
        /// </summary>
        public int PopulationSize { get; init; } = 0;

        /// <summary>
        /// Number of independent restarts to run.
        /// </summary>
        public int Restarts { get; init; } = 3;

        /// <summary>
        /// Initial step size (sigma). If &lt;= 0, automatically set to 30% of average parameter range.
        /// </summary>
        public double InitialSigma { get; init; } = 0;

        /// <summary>
        /// Random seed for reproducibility.
        /// </summary>
        public int Seed { get; init; } = 42;

        public static OptimizerConfig Default => new OptimizerConfig();
    }
}
