// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Logging;
using PerformanceCalculatorGUI.Configuration;

namespace PerformanceCalculatorGUI.Screens.Collections.Autobalance
{
    /// <summary>
    /// Nelder-Mead (downhill simplex) optimizer. Gradient-free local search that
    /// works well for refining solutions found by global optimizers.
    /// All internal optimization is performed in [0,1]-normalized space.
    /// </summary>
    public class NelderMeadOptimizer<TConstants>
    {
        private const double bound_lower_factor = 0.33;
        private const double bound_upper_factor = 3.0;

        // Standard Nelder-Mead coefficients
        private const double alpha = 1.0;   // reflection
        private const double gamma = 2.0;   // expansion
        private const double rho = 0.5;     // contraction
        private const double nm_sigma = 0.5; // shrink

        private readonly Func<TConstants, double[], EvaluationResult> evaluate;
        private readonly DifficultyTuningParameter<TConstants>[] parameters;
        private readonly TConstants baseConstants;
        private readonly OptimizerConfig config;

        public NelderMeadOptimizer(Func<TConstants, double[], EvaluationResult> evaluate,
                                    DifficultyTuningParameter<TConstants>[] parameters,
                                    TConstants baseConstants, OptimizerConfig? config = null)
        {
            this.evaluate = evaluate;
            this.parameters = parameters;
            this.baseConstants = baseConstants;
            this.config = config ?? OptimizerConfig.Default;
        }

        public (double[] BestValues, EvaluationResult BestEvaluation) Run(Action<double>? progressCallback = null)
        {
            int n = parameters.Length;

            if (n == 0)
                return (Array.Empty<double>(), evaluate(baseConstants, Array.Empty<double>()));

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

            // Convert initial values to normalized [0,1] space
            double[] initialNorm = new double[n];

            for (int i = 0; i < n; i++)
                initialNorm[i] = toNormalized(initialValues[i], lowerBounds[i], upperBounds[i]);

            double[] globalBestReal = (double[])initialValues.Clone();
            var globalBestEval = evaluate(baseConstants, initialValues);
            int totalIterations = config.MaxGenerations * config.Restarts;
            int iterationsDone = 0;

            Logger.Log($"[Nelder-Mead] {n} parameters, initial loss: {globalBestEval.Loss:F6} (RMSE: {globalBestEval.Rmse:F6})", LoggingTarget.Information);

            for (int restart = 0; restart < config.Restarts; restart++)
            {
                Logger.Log($"[Nelder-Mead] === Restart {restart + 1}/{config.Restarts} ===", LoggingTarget.Information);
                var result = runNelderMead(n, initialNorm, lowerBounds, upperBounds, restart, ref iterationsDone, totalIterations, progressCallback);

                Logger.Log($"[Nelder-Mead] Restart {restart + 1} best loss: {result.BestEvaluation.Loss:F6}", LoggingTarget.Information);

                if (result.BestEvaluation.Loss < globalBestEval.Loss)
                {
                    globalBestEval = result.BestEvaluation;
                    Array.Copy(result.BestValues, globalBestReal, n);
                }
            }

            Logger.Log($"[Nelder-Mead] Final best loss: {globalBestEval.Loss:F6} (RMSE: {globalBestEval.Rmse:F6})", LoggingTarget.Information);

            for (int i = 0; i < n; i++)
                Logger.Log($"[Nelder-Mead]   param[{i}] {parameters[i].PropertyName}: {initialValues[i]:G6} -> {globalBestReal[i]:G6}", LoggingTarget.Information);

            return (globalBestReal, globalBestEval);
        }

        private (double[] BestValues, EvaluationResult BestEvaluation) runNelderMead(
            int n, double[] initialNorm, double[] lowerBounds, double[] upperBounds,
            int restart, ref int iterationsDone, int totalIterations, Action<double>? progressCallback)
        {
            var random = new Random(config.Seed + restart);

            // Build initial simplex: n+1 vertices in normalized space
            int simplexSize = n + 1;
            double[][] simplex = new double[simplexSize][];
            double[] losses = new double[simplexSize];
            var evals = new EvaluationResult[simplexSize];

            // Initial step size for simplex construction
            double step = config.InitialSigma > 0 ? config.InitialSigma : 0.05;

            // First vertex is the initial point (possibly randomized for restarts)
            simplex[0] = new double[n];

            if (restart == 0)
            {
                Array.Copy(initialNorm, simplex[0], n);
            }
            else
            {
                for (int i = 0; i < n; i++)
                    simplex[0][i] = Math.Clamp(initialNorm[i] + (random.NextDouble() * 2 - 1) * step * 3, 0.0, 1.0);
            }

            // Remaining vertices: perturb each dimension
            for (int j = 0; j < n; j++)
            {
                simplex[j + 1] = (double[])simplex[0].Clone();
                simplex[j + 1][j] = Math.Clamp(simplex[0][j] + step, 0.0, 1.0);

                // If we hit the boundary, go the other direction
                if (Math.Abs(simplex[j + 1][j] - simplex[0][j]) < step * 0.5)
                    simplex[j + 1][j] = Math.Clamp(simplex[0][j] - step, 0.0, 1.0);
            }

            // Evaluate all vertices
            double[] realValues = new double[n];

            for (int j = 0; j < simplexSize; j++)
            {
                denormalizeInto(simplex[j], lowerBounds, upperBounds, realValues);
                evals[j] = evaluate(baseConstants, realValues);
                losses[j] = evals[j].Loss;
            }

            // Track best ever
            int bestIdx = 0;

            for (int j = 1; j < simplexSize; j++)
            {
                if (losses[j] < losses[bestIdx])
                    bestIdx = j;
            }

            double[] bestRealValues = new double[n];
            denormalizeInto(simplex[bestIdx], lowerBounds, upperBounds, bestRealValues);
            var bestEval = evals[bestIdx];
            double bestLoss = losses[bestIdx];

            double[] centroid = new double[n];
            double[] reflected = new double[n];
            double[] expanded = new double[n];
            double[] contracted = new double[n];

            for (int iter = 0; iter < config.MaxGenerations; iter++)
            {
                // Find best, worst, second-worst
                int best = 0, worst = 0, secondWorst = 0;

                for (int j = 1; j < simplexSize; j++)
                {
                    if (losses[j] < losses[best]) best = j;
                    if (losses[j] > losses[worst]) worst = j;
                }

                for (int j = 0; j < simplexSize; j++)
                {
                    if (j == worst) continue;

                    if (secondWorst == worst || losses[j] > losses[secondWorst])
                        secondWorst = j;
                }

                // Check convergence: simplex size
                double maxDist = 0;

                for (int j = 0; j < simplexSize; j++)
                {
                    if (j == best) continue;

                    double dist = 0;

                    for (int i = 0; i < n; i++)
                    {
                        double d = simplex[j][i] - simplex[best][i];
                        dist += d * d;
                    }

                    maxDist = Math.Max(maxDist, Math.Sqrt(dist));
                }

                if (maxDist < 1e-12)
                {
                    Logger.Log($"[Nelder-Mead] Converged at iteration {iter} (simplex collapsed)", LoggingTarget.Information);
                    break;
                }

                if (iter % 50 == 0 || iter < 5)
                {
                    Logger.Log($"[Nelder-Mead] iter {iter}: best={losses[best]:F4} worst={losses[worst]:F4} "
                               + $"simplexSize={maxDist:F8} bestEver={bestLoss:F4}", LoggingTarget.Information);
                }

                // Compute centroid of all vertices except worst
                for (int i = 0; i < n; i++)
                {
                    centroid[i] = 0;

                    for (int j = 0; j < simplexSize; j++)
                    {
                        if (j != worst)
                            centroid[i] += simplex[j][i];
                    }

                    centroid[i] /= n; // n = simplexSize - 1
                }

                // Reflection
                for (int i = 0; i < n; i++)
                    reflected[i] = Math.Clamp(centroid[i] + alpha * (centroid[i] - simplex[worst][i]), 0.0, 1.0);

                denormalizeInto(reflected, lowerBounds, upperBounds, realValues);
                var reflectedEval = evaluate(baseConstants, realValues);
                double reflectedLoss = reflectedEval.Loss;

                if (reflectedLoss < losses[best])
                {
                    // Try expansion
                    for (int i = 0; i < n; i++)
                        expanded[i] = Math.Clamp(centroid[i] + gamma * (reflected[i] - centroid[i]), 0.0, 1.0);

                    denormalizeInto(expanded, lowerBounds, upperBounds, realValues);
                    var expandedEval = evaluate(baseConstants, realValues);

                    if (expandedEval.Loss < reflectedLoss)
                    {
                        Array.Copy(expanded, simplex[worst], n);
                        losses[worst] = expandedEval.Loss;
                        evals[worst] = expandedEval;
                    }
                    else
                    {
                        Array.Copy(reflected, simplex[worst], n);
                        losses[worst] = reflectedLoss;
                        evals[worst] = reflectedEval;
                    }
                }
                else if (reflectedLoss < losses[secondWorst])
                {
                    // Accept reflection
                    Array.Copy(reflected, simplex[worst], n);
                    losses[worst] = reflectedLoss;
                    evals[worst] = reflectedEval;
                }
                else
                {
                    // Contraction
                    bool useReflected = reflectedLoss < losses[worst];
                    double[] contractFrom = useReflected ? reflected : simplex[worst];
                    double contractFromLoss = useReflected ? reflectedLoss : losses[worst];

                    for (int i = 0; i < n; i++)
                        contracted[i] = Math.Clamp(centroid[i] + rho * (contractFrom[i] - centroid[i]), 0.0, 1.0);

                    denormalizeInto(contracted, lowerBounds, upperBounds, realValues);
                    var contractedEval = evaluate(baseConstants, realValues);

                    if (contractedEval.Loss < contractFromLoss)
                    {
                        Array.Copy(contracted, simplex[worst], n);
                        losses[worst] = contractedEval.Loss;
                        evals[worst] = contractedEval;
                    }
                    else
                    {
                        // Shrink: move all vertices toward best
                        for (int j = 0; j < simplexSize; j++)
                        {
                            if (j == best) continue;

                            for (int i = 0; i < n; i++)
                                simplex[j][i] = Math.Clamp(simplex[best][i] + nm_sigma * (simplex[j][i] - simplex[best][i]), 0.0, 1.0);

                            denormalizeInto(simplex[j], lowerBounds, upperBounds, realValues);
                            evals[j] = evaluate(baseConstants, realValues);
                            losses[j] = evals[j].Loss;
                        }
                    }
                }

                // Update best ever
                for (int j = 0; j < simplexSize; j++)
                {
                    if (losses[j] < bestLoss)
                    {
                        bestLoss = losses[j];
                        bestEval = evals[j];
                        denormalizeInto(simplex[j], lowerBounds, upperBounds, bestRealValues);
                    }
                }

                iterationsDone++;
                progressCallback?.Invoke((double)iterationsDone / totalIterations);
            }

            return (bestRealValues, bestEval);
        }

        private static double toNormalized(double value, double lo, double hi)
        {
            double range = hi - lo;

            if (range <= 0)
                return 0.5;

            return (value - lo) / range;
        }

        private static void denormalizeInto(double[] normalized, double[] lowerBounds, double[] upperBounds, double[] output)
        {
            for (int i = 0; i < normalized.Length; i++)
                output[i] = lowerBounds[i] + normalized[i] * (upperBounds[i] - lowerBounds[i]);
        }
    }
}
