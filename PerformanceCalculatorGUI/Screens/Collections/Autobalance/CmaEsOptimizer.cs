// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Logging;
using PerformanceCalculatorGUI.Configuration;

namespace PerformanceCalculatorGUI.Screens.Collections.Autobalance
{
    /// <summary>
    /// CMA-ES (Covariance Matrix Adaptation Evolution Strategy) optimizer.
    /// All internal optimization is performed in [0,1]-normalized space to handle
    /// parameters with vastly different scales.
    /// Reference: Hansen &amp; Ostermeier, "Completely Derandomized Self-Adaptation in Evolution Strategies", 2001.
    /// </summary>
    public class CmaEsOptimizer<TConstants>
    {
        private const double bound_lower_factor = 0.33;
        private const double bound_upper_factor = 3.0;

        private readonly Func<TConstants, double[], EvaluationResult> evaluate;
        private readonly DifficultyTuningParameter<TConstants>[] parameters;
        private readonly TConstants baseConstants;
        private readonly OptimizerConfig config;

        public CmaEsOptimizer(Func<TConstants, double[], EvaluationResult> evaluate,
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

            // Run CMA-ES with restarts, keeping global best (in real space)
            double[] globalBestValues = (double[])initialValues.Clone();
            var globalBestEval = evaluate(baseConstants, initialValues);
            int totalGenerations = config.MaxGenerations * config.Restarts;
            int generationsDone = 0;

            Logger.Log($"[CMA-ES] {n} parameters, initial loss: {globalBestEval.Loss:F6} (RMSE: {globalBestEval.Rmse:F6})", LoggingTarget.Information);

            for (int i = 0; i < n; i++)
                Logger.Log($"[CMA-ES]   param[{i}] {parameters[i].PropertyName}: initial={initialValues[i]:G6}, bounds=[{lowerBounds[i]:G6}, {upperBounds[i]:G6}]", LoggingTarget.Information);

            for (int restart = 0; restart < config.Restarts; restart++)
            {
                Logger.Log($"[CMA-ES] === Restart {restart + 1}/{config.Restarts} ===", LoggingTarget.Information);
                var result = runCmaEs(n, initialNorm, lowerBounds, upperBounds, restart, ref generationsDone, totalGenerations, progressCallback);

                Logger.Log($"[CMA-ES] Restart {restart + 1} best loss: {result.BestEvaluation.Loss:F6}", LoggingTarget.Information);

                if (result.BestEvaluation.Loss < globalBestEval.Loss)
                {
                    globalBestEval = result.BestEvaluation;
                    Array.Copy(result.BestValues, globalBestValues, n);
                }
            }

            Logger.Log($"[CMA-ES] Final best loss: {globalBestEval.Loss:F6} (RMSE: {globalBestEval.Rmse:F6})", LoggingTarget.Information);

            for (int i = 0; i < n; i++)
                Logger.Log($"[CMA-ES]   param[{i}] {parameters[i].PropertyName}: {initialValues[i]:G6} -> {globalBestValues[i]:G6}", LoggingTarget.Information);

            return (globalBestValues, globalBestEval);
        }

        private (double[] BestValues, EvaluationResult BestEvaluation) runCmaEs(
            int n, double[] initialNorm, double[] lowerBounds, double[] upperBounds,
            int restart, ref int generationsDone, int totalGenerations, Action<double>? progressCallback)
        {
            var random = new Random(config.Seed + restart);

            // --- CMA-ES strategy parameters (from Hansen's tutorial) ---
            int lambda = config.PopulationSize > 0 ? config.PopulationSize : 4 + (int)(3 * Math.Log(n));
            int mu = lambda / 2;

            // Recombination weights
            double[] weights = new double[mu];
            double weightSum = 0;

            for (int i = 0; i < mu; i++)
            {
                weights[i] = Math.Log(mu + 0.5) - Math.Log(i + 1);
                weightSum += weights[i];
            }

            for (int i = 0; i < mu; i++)
                weights[i] /= weightSum;

            double muEff = 0;
            double sumWSquared = 0;

            for (int i = 0; i < mu; i++)
            {
                muEff += weights[i];
                sumWSquared += weights[i] * weights[i];
            }

            muEff = muEff * muEff / sumWSquared;

            // Adaptation rates
            double cc = (4.0 + muEff / n) / (n + 4.0 + 2.0 * muEff / n);
            double cs = (muEff + 2.0) / (n + muEff + 5.0);
            double c1 = 2.0 / ((n + 1.3) * (n + 1.3) + muEff);
            double cmu = Math.Min(1.0 - c1, 2.0 * (muEff - 2.0 + 1.0 / muEff) / ((n + 2.0) * (n + 2.0) + muEff));
            double damps = 1.0 + 2.0 * Math.Max(0, Math.Sqrt((muEff - 1.0) / (n + 1.0)) - 1.0) + cs;
            double chiN = Math.Sqrt(n) * (1.0 - 1.0 / (4.0 * n) + 1.0 / (21.0 * n * n));

            // --- State variables (all in normalized [0,1] space) ---
            double[] mean = new double[n];

            if (restart == 0)
            {
                Array.Copy(initialNorm, mean, n);
            }
            else
            {
                for (int i = 0; i < n; i++)
                    mean[i] = Math.Clamp(initialNorm[i] + (random.NextDouble() * 2 - 1) * 0.5, 0.0, 1.0);
            }

            // Initial step size in normalized space — 0.3 is a good default for [0,1]
            double sigma = config.InitialSigma > 0 ? config.InitialSigma : 0.3;

            // Covariance matrix C = I (stored as flat array, row-major)
            double[] covMatrix = new double[n * n];

            for (int i = 0; i < n; i++)
                covMatrix[i * n + i] = 1.0;

            // Evolution path for C
            double[] pc = new double[n];
            // Evolution path for sigma
            double[] ps = new double[n];

            // Eigendecomposition cache
            double[] eigenvalues = new double[n];
            double[] eigenvectors = new double[n * n]; // row-major
            double[] invsqrtC = new double[n * n];
            bool eigenDirty = true;

            for (int i = 0; i < n; i++)
            {
                eigenvalues[i] = 1.0;
                eigenvectors[i * n + i] = 1.0;
                invsqrtC[i * n + i] = 1.0;
            }

            // Best solution tracking (in real space)
            double[] bestRealValues = new double[n];
            denormalizeInto(mean, lowerBounds, upperBounds, bestRealValues);
            var bestEval = evaluate(baseConstants, bestRealValues);
            double bestLoss = bestEval.Loss;

            // Workspace arrays
            double[][] populationNorm = new double[lambda][];
            double[][] z = new double[lambda][];
            double[] candidateReal = new double[n];
            var fitness = new (double loss, int index)[lambda];

            for (int k = 0; k < lambda; k++)
            {
                populationNorm[k] = new double[n];
                z[k] = new double[n];
            }

            double[] oldMean = new double[n];
            double[] artmp = new double[n];

            int eigenUpdateInterval = Math.Max(1, (int)(1.0 / (10.0 * n * (c1 + cmu))));
            int generationsSinceEigen = 0;

            for (int gen = 0; gen < config.MaxGenerations; gen++)
            {
                // Update eigendecomposition if needed
                if (eigenDirty || generationsSinceEigen >= eigenUpdateInterval)
                {
                    updateEigenDecomposition(n, covMatrix, eigenvalues, eigenvectors, invsqrtC);
                    eigenDirty = false;
                    generationsSinceEigen = 0;
                }

                generationsSinceEigen++;

                // --- Sample population in normalized space ---
                for (int k = 0; k < lambda; k++)
                {
                    // Sample z ~ N(0, I)
                    for (int i = 0; i < n; i++)
                        z[k][i] = sampleGaussian(random);

                    // Transform: x_norm = mean + sigma * B * D * z, clamped to [0, 1]
                    for (int i = 0; i < n; i++)
                    {
                        double sum = 0;

                        for (int j = 0; j < n; j++)
                            sum += eigenvectors[i * n + j] * Math.Sqrt(Math.Max(0, eigenvalues[j])) * z[k][j];

                        populationNorm[k][i] = Math.Clamp(mean[i] + sigma * sum, 0.0, 1.0);
                    }
                }

                // --- Evaluate population (convert to real space for evaluation) ---
                for (int k = 0; k < lambda; k++)
                {
                    denormalizeInto(populationNorm[k], lowerBounds, upperBounds, candidateReal);
                    var eval = evaluate(baseConstants, candidateReal);
                    fitness[k] = (eval.Loss, k);

                    if (eval.Loss < bestLoss)
                    {
                        bestLoss = eval.Loss;
                        bestEval = eval;
                        denormalizeInto(populationNorm[k], lowerBounds, upperBounds, bestRealValues);
                    }
                }

                // Sort by fitness (ascending = better)
                Array.Sort(fitness, (a, b) => a.loss.CompareTo(b.loss));

                int penaltyCount = 0;

                for (int k = 0; k < lambda; k++)
                {
                    if (fitness[k].loss >= 1e11)
                        penaltyCount++;
                }

                if (gen % 10 == 0 || gen < 5)
                {
                    Logger.Log($"[CMA-ES] gen {gen}: best={fitness[0].loss:F4} worst={fitness[lambda - 1].loss:F4} "
                               + $"sigma={sigma:F6} bestEver={bestLoss:F4} penalties={penaltyCount}/{lambda}", LoggingTarget.Information);
                }

                // --- Update mean (in normalized space) ---
                Array.Copy(mean, oldMean, n);

                for (int i = 0; i < n; i++)
                {
                    mean[i] = 0;

                    for (int k = 0; k < mu; k++)
                        mean[i] += weights[k] * populationNorm[fitness[k].index][i];
                }

                // --- Update evolution paths ---
                // ps = (1-cs)*ps + sqrt(cs*(2-cs)*muEff) * C^(-1/2) * (mean-oldMean)/sigma
                double[] diff = new double[n];

                for (int i = 0; i < n; i++)
                    diff[i] = (mean[i] - oldMean[i]) / sigma;

                for (int i = 0; i < n; i++)
                {
                    artmp[i] = 0;

                    for (int j = 0; j < n; j++)
                        artmp[i] += invsqrtC[i * n + j] * diff[j];
                }

                double csFactor = Math.Sqrt(cs * (2.0 - cs) * muEff);

                for (int i = 0; i < n; i++)
                    ps[i] = (1.0 - cs) * ps[i] + csFactor * artmp[i];

                double psNorm = 0;

                for (int i = 0; i < n; i++)
                    psNorm += ps[i] * ps[i];

                psNorm = Math.Sqrt(psNorm);

                // Heaviside function for stalling detection
                double psThreshold = (1.4 + 2.0 / (n + 1.0)) * chiN;
                bool hsig = psNorm / Math.Sqrt(1.0 - Math.Pow(1.0 - cs, 2.0 * (gen + 1))) < psThreshold;
                double hsigD = hsig ? 1.0 : 0.0;

                double ccFactor = Math.Sqrt(cc * (2.0 - cc) * muEff);

                for (int i = 0; i < n; i++)
                    pc[i] = (1.0 - cc) * pc[i] + hsigD * ccFactor * diff[i];

                // --- Update covariance matrix ---
                double oldWeight = 1.0 - c1 - cmu + (1.0 - hsigD) * c1 * cc * (2.0 - cc);

                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j <= i; j++)
                    {
                        double val = oldWeight * covMatrix[i * n + j]
                                     + c1 * pc[i] * pc[j];

                        for (int k = 0; k < mu; k++)
                        {
                            int idx = fitness[k].index;
                            double yi = (populationNorm[idx][i] - oldMean[i]) / sigma;
                            double yj = (populationNorm[idx][j] - oldMean[j]) / sigma;
                            val += cmu * weights[k] * yi * yj;
                        }

                        covMatrix[i * n + j] = val;
                        covMatrix[j * n + i] = val; // symmetric
                    }
                }

                eigenDirty = true;

                // --- Update step size ---
                sigma *= Math.Exp(cs / damps * (psNorm / chiN - 1.0));
                sigma = Math.Clamp(sigma, 1e-20, 1e3);

                // --- Check termination ---
                if (sigma < 1e-12)
                    break;

                // Stop if condition number of C is too large
                double maxEig = double.MinValue, minEig = double.MaxValue;

                for (int i = 0; i < n; i++)
                {
                    if (eigenvalues[i] > maxEig) maxEig = eigenvalues[i];
                    if (eigenvalues[i] < minEig) minEig = eigenvalues[i];
                }

                if (minEig > 0 && maxEig / minEig > 1e14)
                    break;

                generationsDone++;
                progressCallback?.Invoke((double)generationsDone / totalGenerations);
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

        /// <summary>
        /// Eigendecomposition of symmetric matrix C using Jacobi iteration.
        /// Also computes C^(-1/2) = B * D^(-1) * B^T.
        /// </summary>
        private static void updateEigenDecomposition(int n, double[] c, double[] eigenvalues, double[] eigenvectors, double[] invsqrtC)
        {
            // Copy C to working matrix
            double[] matrix = new double[n * n];
            Array.Copy(c, matrix, n * n);

            // Initialize eigenvectors to identity
            for (int i = 0; i < n * n; i++)
                eigenvectors[i] = 0;

            for (int i = 0; i < n; i++)
                eigenvectors[i * n + i] = 1.0;

            // Jacobi eigenvalue algorithm for symmetric matrices
            const int max_sweeps = 100;

            for (int sweep = 0; sweep < max_sweeps; sweep++)
            {
                // Check convergence: sum of off-diagonal elements
                double offDiag = 0;

                for (int i = 0; i < n; i++)
                    for (int j = i + 1; j < n; j++)
                        offDiag += Math.Abs(matrix[i * n + j]);

                if (offDiag < 1e-15 * n)
                    break;

                for (int p = 0; p < n; p++)
                {
                    for (int q = p + 1; q < n; q++)
                    {
                        double apq = matrix[p * n + q];

                        if (Math.Abs(apq) < 1e-20)
                            continue;

                        double tau = (matrix[q * n + q] - matrix[p * n + p]) / (2.0 * apq);
                        double t = Math.Sign(tau) / (Math.Abs(tau) + Math.Sqrt(1.0 + tau * tau));
                        double cosTheta = 1.0 / Math.Sqrt(1.0 + t * t);
                        double sinTheta = t * cosTheta;

                        // Update matrix
                        matrix[p * n + q] = 0;
                        matrix[q * n + p] = 0;
                        matrix[p * n + p] -= t * apq;
                        matrix[q * n + q] += t * apq;

                        for (int r = 0; r < n; r++)
                        {
                            if (r == p || r == q)
                                continue;

                            double mrp = matrix[r * n + p];
                            double mrq = matrix[r * n + q];
                            matrix[r * n + p] = matrix[p * n + r] = cosTheta * mrp - sinTheta * mrq;
                            matrix[r * n + q] = matrix[q * n + r] = sinTheta * mrp + cosTheta * mrq;
                        }

                        // Update eigenvectors
                        for (int r = 0; r < n; r++)
                        {
                            double erp = eigenvectors[r * n + p];
                            double erq = eigenvectors[r * n + q];
                            eigenvectors[r * n + p] = cosTheta * erp - sinTheta * erq;
                            eigenvectors[r * n + q] = sinTheta * erp + cosTheta * erq;
                        }
                    }
                }
            }

            // Extract eigenvalues
            for (int i = 0; i < n; i++)
                eigenvalues[i] = Math.Max(matrix[i * n + i], 1e-20);

            // Compute C^(-1/2) = B * D^(-1) * B^T
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j <= i; j++)
                {
                    double sum = 0;

                    for (int k = 0; k < n; k++)
                        sum += eigenvectors[i * n + k] * (1.0 / Math.Sqrt(eigenvalues[k])) * eigenvectors[j * n + k];

                    invsqrtC[i * n + j] = sum;
                    invsqrtC[j * n + i] = sum;
                }
            }
        }

        private static double sampleGaussian(Random random)
        {
            // Box-Muller transform
            double u1 = 1.0 - random.NextDouble(); // avoid log(0)
            double u2 = random.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        }
    }
}
