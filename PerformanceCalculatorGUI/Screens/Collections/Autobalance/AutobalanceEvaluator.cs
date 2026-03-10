// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Osu.Difficulty;
using PerformanceCalculatorGUI.Configuration;

namespace PerformanceCalculatorGUI.Screens.Collections.Autobalance
{
    public class AutobalanceEvaluator<TConstants>
    {
        private const double big_penalty = 1e12;

        private readonly Func<TConstants, IWorkingBeatmap, DifficultyCalculator> createDifficultyCalculator;
        private readonly PerformanceCalculator performanceCalculator;
        private readonly Func<PerformanceAttributes?, AutobalanceTarget, double?> getTargetValue;

        public AutobalanceEvaluator(Func<TConstants, IWorkingBeatmap, DifficultyCalculator> createDifficultyCalculator,
                                    PerformanceCalculator performanceCalculator,
                                    Func<PerformanceAttributes?, AutobalanceTarget, double?> getTargetValue)
        {
            this.createDifficultyCalculator = createDifficultyCalculator;
            this.performanceCalculator = performanceCalculator;
            this.getTargetValue = getTargetValue;
        }

        public EvaluationResult Evaluate(TConstants constants, IReadOnlyList<AutobalanceScoreData> dataset,
                                         AutobalanceTarget target, DifficultyTuningParameter<TConstants>[] parameters, double[] values)
        {
            try
            {
                var tuning = ApplyParameters(constants, parameters, values);

                int n = dataset.Count;
                var computedActuals = new double[n];
                var valid = new bool[n];

                Parallel.For(0, n, i =>
                {
                    var entry = dataset[i];
                    var difficultyCalculator = createDifficultyCalculator(tuning, entry.Working);
                    var difficultyAttributes = difficultyCalculator.Calculate(entry.Mods);
                    var performanceAttributes = performanceCalculator.Calculate(entry.ScoreInfo, difficultyAttributes);
                    double? actual = getTargetValue(performanceAttributes, target);

                    if (actual != null)
                    {
                        computedActuals[i] = actual.Value;
                        valid[i] = true;
                    }
                });

                double weightedErrorSum = 0;
                double weightSum = 0;
                int validCount = 0;
                var actuals = new double[n];
                var expecteds = new double[n];

                for (int i = 0; i < n; i++)
                {
                    if (!valid[i])
                        continue;

                    double actual = computedActuals[i];
                    double expected = dataset[i].ExpectedValue;
                    double weight = dataset[i].Weight;

                    double diff = actual - expected;
                    weightedErrorSum += weight * diff * diff;
                    weightSum += weight;

                    actuals[validCount] = actual;
                    expecteds[validCount] = expected;
                    validCount++;
                }

                if (validCount == 0 || weightSum <= 0)
                    return new EvaluationResult(big_penalty, 0, big_penalty);

                double rmse = Math.Sqrt(weightedErrorSum / weightSum);
                double spearman = validCount >= 2 ? ComputeSpearmanCorrelation(actuals, expecteds, validCount) : 0;
                double loss = rmse * (2.0 - spearman);

                return new EvaluationResult(rmse, spearman, loss);
            }
            catch
            {
                return new EvaluationResult(big_penalty, 0, big_penalty);
            }
        }

        public static TConstants ApplyParameters(TConstants baseConstants, DifficultyTuningParameter<TConstants>[] parameters, double[] values)
        {
            var result = baseConstants;

            for (int i = 0; i < parameters.Length; i++)
            {
                double value = values[i];

                if (double.IsNaN(value) || double.IsInfinity(value))
                    continue;

                double clamped = Math.Max(value, parameters[i].MinValue);

                if (parameters[i].MaxValue is { } maxValue)
                    clamped = Math.Min(clamped, maxValue);

                result = parameters[i].Setter(result, clamped);
            }

            return result;
        }

        internal static double ComputeSpearmanCorrelation(double[] actual, double[] expected, int count)
        {
            if (count < 2)
                return 0;

            double[] actualRanks = computeRanks(actual, count);
            double[] expectedRanks = computeRanks(expected, count);

            double sumDSq = 0;

            for (int i = 0; i < count; i++)
            {
                double d = actualRanks[i] - expectedRanks[i];
                sumDSq += d * d;
            }

            return 1.0 - 6.0 * sumDSq / (count * ((double)count * count - 1));
        }

        private static double[] computeRanks(double[] values, int count)
        {
            var indexed = new (double value, int index)[count];

            for (int i = 0; i < count; i++)
                indexed[i] = (values[i], i);

            Array.Sort(indexed, (a, b) => a.value.CompareTo(b.value));

            double[] ranks = new double[count];
            int pos = 0;

            while (pos < count)
            {
                int end = pos;

                while (end < count - 1 && Math.Abs(indexed[end + 1].value - indexed[end].value) < 1e-9)
                    end++;

                double avgRank = (pos + end) / 2.0 + 1;

                for (int k = pos; k <= end; k++)
                    ranks[indexed[k].index] = avgRank;

                pos = end + 1;
            }

            return ranks;
        }

        public static Func<PerformanceAttributes?, AutobalanceTarget, double?> GetOsuTargetValueFunc()
        {
            return (attributes, target) =>
            {
                if (attributes == null)
                    return null;

                return target switch
                {
                    AutobalanceTarget.Total => attributes.Total,
                    AutobalanceTarget.Aim => (attributes as OsuPerformanceAttributes)?.Aim,
                    AutobalanceTarget.Speed => (attributes as OsuPerformanceAttributes)?.Speed,
                    AutobalanceTarget.Accuracy => (attributes as OsuPerformanceAttributes)?.Accuracy,
                    AutobalanceTarget.Reading => (attributes as OsuPerformanceAttributes)?.Reading,
                    AutobalanceTarget.Flashlight => (attributes as OsuPerformanceAttributes)?.Flashlight,
                    _ => null
                };
            };
        }
    }
}
