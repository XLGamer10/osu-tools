// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using PerformanceCalculatorGUI.Configuration;

namespace PerformanceCalculatorGUI.Screens.Collections.Autobalance
{
    public class SimulatedAnnealingOptimizer<TConstants>
    {
        private const double bound_lower_factor = 0.33;
        private const double bound_upper_factor = 3.0;

        private readonly Func<TConstants, double[], EvaluationResult> evaluate;
        private readonly DifficultyTuningParameter<TConstants>[] parameters;
        private readonly TConstants baseConstants;
        private readonly SAConfig config;

        public SimulatedAnnealingOptimizer(Func<TConstants, double[], EvaluationResult> evaluate,
                                           DifficultyTuningParameter<TConstants>[] parameters,
                                           TConstants baseConstants, SAConfig? config = null)
        {
            this.evaluate = evaluate;
            this.parameters = parameters;
            this.baseConstants = baseConstants;
            this.config = config ?? SAConfig.Default;
        }

        public (double[] BestValues, EvaluationResult BestEvaluation) Run(Action<double>? progressCallback = null)
        {
            int n = parameters.Length;
            double[] lowerBounds = new double[n];
            double[] upperBounds = new double[n];
            double[] initialValues = new double[n];

            for (int i = 0; i < n; i++)
            {
                double baseVal = parameters[i].Getter(baseConstants);

                if (double.IsNaN(baseVal) || double.IsInfinity(baseVal))
                    baseVal = 1.0;

                double lo, hi;

                if (parameters[i].MaxValue is { } maxVal)
                {
                    lo = parameters[i].MinValue;
                    hi = maxVal;
                }
                else
                {
                    lo = Math.Max(parameters[i].MinValue, baseVal * bound_lower_factor);
                    hi = Math.Max(baseVal * bound_upper_factor, parameters[i].MinValue * bound_upper_factor);
                }

                if (double.IsNaN(lo) || double.IsInfinity(lo))
                    lo = parameters[i].MinValue;

                if (double.IsNaN(hi) || double.IsInfinity(hi) || hi <= lo)
                    hi = lo + Math.Max(1e-6, Math.Abs(lo) * 0.1);

                initialValues[i] = Math.Clamp(baseVal, lo, hi);
                lowerBounds[i] = lo;
                upperBounds[i] = hi;
            }

            double[] globalBestValues = (double[])initialValues.Clone();
            var globalBestEval = evaluate(baseConstants, initialValues);
            int totalIterations = config.Iterations * config.Restarts;
            int iterationsDone = 0;

            for (int restart = 0; restart < config.Restarts; restart++)
            {
                var random = new Random(config.Seed + restart);

                double[] currentValues = restart == 0
                    ? (double[])initialValues.Clone()
                    : randomizeStartingPoint(initialValues, lowerBounds, upperBounds, random);

                var currentEval = evaluate(baseConstants, currentValues);
                double currentLoss = currentEval.Loss;

                double[] bestValues = (double[])currentValues.Clone();
                double bestLoss = currentLoss;
                var bestEval = currentEval;

                double temperature = config.InitialTemperature;
                double coolingRate = config.Iterations > 1
                    ? Math.Pow(config.MinTemperature / config.InitialTemperature, 1.0 / (config.Iterations - 1))
                    : config.CoolingRate;

                for (int iteration = 0; iteration < config.Iterations && temperature > config.MinTemperature; iteration++)
                {
                    double[] candidateValues = (double[])currentValues.Clone();

                    for (int p = 0; p < n; p++)
                    {
                        double range = (upperBounds[p] - lowerBounds[p]) * temperature / config.InitialTemperature;
                        double perturbation = (random.NextDouble() * 2 - 1) * range;
                        candidateValues[p] = Math.Clamp(
                            candidateValues[p] + perturbation,
                            lowerBounds[p],
                            upperBounds[p]);
                    }

                    var candidateEval = evaluate(baseConstants, candidateValues);
                    double delta = candidateEval.Loss - currentLoss;

                    if (delta < 0 || random.NextDouble() < Math.Exp(-delta / temperature))
                    {
                        currentValues = candidateValues;
                        currentLoss = candidateEval.Loss;

                        if (currentLoss < bestLoss)
                        {
                            bestLoss = currentLoss;
                            bestEval = candidateEval;
                            Array.Copy(currentValues, bestValues, n);
                        }
                    }

                    temperature *= coolingRate;
                    iterationsDone++;
                    progressCallback?.Invoke((double)iterationsDone / totalIterations);
                }

                if (bestLoss < globalBestEval.Loss)
                {
                    globalBestEval = bestEval;
                    Array.Copy(bestValues, globalBestValues, n);
                }
            }

            return (globalBestValues, globalBestEval);
        }

        private static double[] randomizeStartingPoint(double[] initialValues, double[] lowerBounds, double[] upperBounds, Random random)
        {
            double[] values = new double[initialValues.Length];

            for (int i = 0; i < values.Length; i++)
            {
                double range = (upperBounds[i] - lowerBounds[i]) * 0.5;
                double perturbation = (random.NextDouble() * 2 - 1) * range;
                values[i] = Math.Clamp(initialValues[i] + perturbation, lowerBounds[i], upperBounds[i]);
            }

            return values;
        }
    }
}
