// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Difficulty;
using PerformanceCalculatorGUI.Configuration;

namespace PerformanceCalculatorGUI.Screens.Collections.Autobalance
{
    public class AutobalanceRunner
    {
        private const double dataset_progress_portion = 0.05;

        private readonly AutobalanceDataset dataset;

        public AutobalanceRunner(ScoreCache scoreCache, RulesetStore rulesets, SettingsManager configManager)
        {
            dataset = new AutobalanceDataset(scoreCache, rulesets, configManager);
        }

        public Task<AutobalanceResult<OsuDifficultyConstants>> RunOsuAsync(Collection collection, AutobalanceTarget target,
                                                                           DifficultyTuningParameter<OsuDifficultyConstants>[] selectedParameters,
                                                                           OsuDifficultyConstants baseConstants,
                                                                           OptimizerType optimizerType = OptimizerType.CmaEs,
                                                                           OptimizerConfig? config = null,
                                                                           Action<AutobalanceProgress>? progress = null)
        {
            var osuRuleset = new OsuRuleset();

            return RunAsync(collection, target, selectedParameters, baseConstants, "osu",
                (tuning, working) => new OsuDifficultyCalculator(osuRuleset.RulesetInfo, working, tuning),
                () => osuRuleset.CreatePerformanceCalculator()!,
                AutobalanceEvaluator<OsuDifficultyConstants>.GetOsuTargetValueFunc(),
                optimizerType, config, progress);
        }

        public async Task<AutobalanceResult<TConstants>> RunAsync<TConstants>(
            Collection collection, AutobalanceTarget target,
            DifficultyTuningParameter<TConstants>[] selectedParameters,
            TConstants baseConstants, string rulesetShortName,
            Func<TConstants, IWorkingBeatmap, DifficultyCalculator> createDifficultyCalculator,
            Func<PerformanceCalculator> createPerformanceCalculator,
            Func<PerformanceAttributes?, AutobalanceTarget, double?> getTargetValue,
            OptimizerType optimizerType = OptimizerType.CmaEs,
            OptimizerConfig? config = null,
            Action<AutobalanceProgress>? progress = null)
        {
            config ??= OptimizerConfig.Default;

            progress?.Invoke(new AutobalanceProgress(0, "Preparing..."));

            var scores = await dataset.BuildAsync(collection, target, rulesetShortName, progress).ConfigureAwait(false);

            if (scores.Count == 0)
            {
                progress?.Invoke(new AutobalanceProgress(1, "Failed"));
                return AutobalanceResult<TConstants>.Failure($"No expected values found for {target}.");
            }

            // Filter out integer parameters — they're discrete, not suitable for continuous optimization
            selectedParameters = selectedParameters.Where(p => !p.IsInteger).ToArray();

            var evaluator = new AutobalanceEvaluator<TConstants>(createDifficultyCalculator, createPerformanceCalculator, getTargetValue);

            if (selectedParameters.Length == 0)
            {
                progress?.Invoke(new AutobalanceProgress(dataset_progress_portion, "Evaluating..."));
                var baseEval = evaluator.Evaluate(baseConstants, scores, target, Array.Empty<DifficultyTuningParameter<TConstants>>(), Array.Empty<double>());
                progress?.Invoke(new AutobalanceProgress(1, "Done"));
                return AutobalanceResult<TConstants>.Success(baseConstants, baseEval, scores.Count);
            }

            progress?.Invoke(new AutobalanceProgress(dataset_progress_portion, "Optimizing..."));

            Func<TConstants, double[], EvaluationResult> evalFunc =
                (constants, values) => evaluator.Evaluate(constants, scores, target, selectedParameters, values);

            Func<Action<double>?, (double[] BestValues, EvaluationResult BestEvaluation)> runOptimizer = optimizerType switch
            {
                OptimizerType.NelderMead => new NelderMeadOptimizer<TConstants>(evalFunc, selectedParameters, baseConstants, config).Run,
                _ => new CmaEsOptimizer<TConstants>(evalFunc, selectedParameters, baseConstants, config).Run,
            };

            var (bestValues, bestEvaluation) = await Task.Run(() =>
                runOptimizer(optProgress =>
                {
                    double combined = dataset_progress_portion + (1.0 - dataset_progress_portion) * optProgress;
                    progress?.Invoke(new AutobalanceProgress(combined));
                })
            ).ConfigureAwait(false);

            var bestConstants = AutobalanceEvaluator<TConstants>.ApplyParameters(baseConstants, selectedParameters, bestValues);

            progress?.Invoke(new AutobalanceProgress(1, "Done"));
            return AutobalanceResult<TConstants>.Success(bestConstants, bestEvaluation, scores.Count);
        }
    }
}
