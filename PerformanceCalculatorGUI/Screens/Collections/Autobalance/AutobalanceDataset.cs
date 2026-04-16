// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using osu.Framework.Logging;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets;
using PerformanceCalculatorGUI.Configuration;

namespace PerformanceCalculatorGUI.Screens.Collections.Autobalance
{
    public class AutobalanceDataset
    {
        private readonly ScoreCache scoreCache;
        private readonly RulesetStore rulesets;
        private readonly SettingsManager configManager;

        public AutobalanceDataset(ScoreCache scoreCache, RulesetStore rulesets, SettingsManager configManager)
        {
            this.scoreCache = scoreCache;
            this.rulesets = rulesets;
            this.configManager = configManager;
        }

        public async Task<IReadOnlyList<AutobalanceScoreData>> BuildAsync(Collection collection, AutobalanceTarget target,
                                                                          string rulesetShortName, Action<AutobalanceProgress>? progress = null)
        {
            var dataset = new List<AutobalanceScoreData>();
            var expectedPerformance = collection.ExpectedPerformance;

            if (expectedPerformance.Count == 0)
            {
                progress?.Invoke(new AutobalanceProgress(0.05, "Loading scores", 0, 0));
                return dataset;
            }

            int totalScores = collection.Scores.Length;
            int processed = 0;

            progress?.Invoke(new AutobalanceProgress(0, "Loading scores", 0, totalScores));

            foreach (long scoreId in collection.Scores)
            {
                processed++;

                string key = scoreId.ToString();

                if (!expectedPerformance.TryGetValue(key, out var expectedValues) || !TryGetExpectedValue(expectedValues, target, out double expectedValue))
                {
                    reportDatasetProgress(progress, processed, totalScores);
                    continue;
                }

                SoloScoreInfo? score;

                try
                {
                    score = await scoreCache.GetScore(scoreId).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Logger.Log(e.ToString(), level: LogLevel.Error);
                    reportDatasetProgress(progress, processed, totalScores);
                    continue;
                }

                if (score == null)
                {
                    reportDatasetProgress(progress, processed, totalScores);
                    continue;
                }

                var rulesetInfo = rulesets.GetRuleset(score.RulesetID);

                if (rulesetInfo?.ShortName != rulesetShortName)
                {
                    reportDatasetProgress(progress, processed, totalScores);
                    continue;
                }

                try
                {
                    var working = ProcessorWorkingBeatmap.FromFileOrId(score.BeatmapID.ToString(),
                        cachePath: configManager.GetBindable<string>(Settings.CachePath).Value);
                    var rulesetInstance = rulesetInfo.CreateInstance();
                    var mods = score.Mods.Select(x => x.ToMod(rulesetInstance)).ToArray();
                    var scoreInfo = score.ToScoreInfo(rulesets, working.BeatmapInfo);
                    var parsedScore = new ProcessorScoreDecoder(working).Parse(scoreInfo);

                    double weight = expectedValues.Weight ?? 1.0;
                    dataset.Add(new AutobalanceScoreData(working, mods, parsedScore.ScoreInfo, expectedValue, weight));
                }
                catch (Exception e)
                {
                    Logger.Log(e.ToString(), level: LogLevel.Error);
                }

                reportDatasetProgress(progress, processed, totalScores);
            }

            progress?.Invoke(new AutobalanceProgress(0.05, $"Dataset ready ({dataset.Count} scores)"));
            return dataset;
        }

        internal static bool TryGetExpectedValue(ExpectedPerformanceValues expectedValues, AutobalanceTarget target, out double expectedValue)
        {
            if (target == AutobalanceTarget.Total)
            {
                if (expectedValues.Total.HasValue)
                {
                    expectedValue = expectedValues.Total.Value;
                    return true;
                }

                if (expectedValues.Skills.TryGetValue("pp", out expectedValue))
                    return true;

                if (expectedValues.Skills.TryGetValue("total", out expectedValue))
                    return true;

                return false;
            }

            string key = target switch
            {
                AutobalanceTarget.Aim => "aim",
                AutobalanceTarget.Speed => "speed",
                AutobalanceTarget.Accuracy => "accuracy",
                AutobalanceTarget.Reading => "reading",
                AutobalanceTarget.Flashlight => "flashlight",
                _ => "total"
            };

            return expectedValues.Skills.TryGetValue(key, out expectedValue);
        }

        private static void reportDatasetProgress(Action<AutobalanceProgress>? progress, int processed, int total)
        {
            progress?.Invoke(new AutobalanceProgress(0.05 * processed / total, "Loading scores", processed, total));
        }
    }
}
