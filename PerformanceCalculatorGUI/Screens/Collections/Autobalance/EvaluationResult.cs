// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace PerformanceCalculatorGUI.Screens.Collections.Autobalance
{
    public readonly struct EvaluationResult
    {
        public double Rmse { get; }
        public double Spearman { get; }
        public double Loss { get; }

        public EvaluationResult(double rmse, double spearman, double loss)
        {
            Rmse = rmse;
            Spearman = spearman;
            Loss = loss;
        }
    }
}
