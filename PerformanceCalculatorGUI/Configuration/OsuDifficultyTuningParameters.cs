// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Humanizer;
using osu.Game.Rulesets.Osu.Difficulty;

namespace PerformanceCalculatorGUI.Configuration
{
    public static class OsuDifficultyTuningParameters
    {
        public static readonly IReadOnlyList<DifficultyTuningSection<OsuDifficultyConstants>> Sections = new[]
        {
            new DifficultyTuningSection<OsuDifficultyConstants>("Performance scales",
                doubleParam(nameof(OsuDifficultyConstants.AimPerformanceScale), c => c.AimPerformanceScale, (c, v) => c with { AimPerformanceScale = v }, defaultEnabled: false),
                doubleParam(nameof(OsuDifficultyConstants.SpeedPerformanceScale), c => c.SpeedPerformanceScale, (c, v) => c with { SpeedPerformanceScale = v }, defaultEnabled: false),
                doubleParam(nameof(OsuDifficultyConstants.AccuracyPerformanceScale), c => c.AccuracyPerformanceScale, (c, v) => c with { AccuracyPerformanceScale = v }, defaultEnabled: false),
                doubleParam(nameof(OsuDifficultyConstants.FlashlightPerformanceScale), c => c.FlashlightPerformanceScale, (c, v) => c with { FlashlightPerformanceScale = v }, defaultEnabled: false),
                doubleParam(nameof(OsuDifficultyConstants.ReadingPerformanceScale), c => c.ReadingPerformanceScale, (c, v) => c with { ReadingPerformanceScale = v }, defaultEnabled: false),
                doubleParam(nameof(OsuDifficultyConstants.TotalPerformanceScale), c => c.TotalPerformanceScale, (c, v) => c with { TotalPerformanceScale = v }, defaultEnabled: false),
                doubleParam(nameof(OsuDifficultyConstants.CognitionPerformanceExponent), c => c.CognitionPerformanceExponent, (c, v) => c with { CognitionPerformanceExponent = v }, defaultEnabled: false)
            ),
            new DifficultyTuningSection<OsuDifficultyConstants>("Skill strain scales",
                doubleParam(nameof(OsuDifficultyConstants.AimSkillStrainScale), c => c.AimSkillStrainScale, (c, v) => c with { AimSkillStrainScale = v }),
                doubleParam(nameof(OsuDifficultyConstants.SpeedSkillStrainScale), c => c.SpeedSkillStrainScale, (c, v) => c with { SpeedSkillStrainScale = v }),
                doubleParam(nameof(OsuDifficultyConstants.FlashlightSkillStrainScale), c => c.FlashlightSkillStrainScale, (c, v) => c with { FlashlightSkillStrainScale = v }, defaultEnabled: false),
                doubleParam(nameof(OsuDifficultyConstants.ReadingSkillStrainScale), c => c.ReadingSkillStrainScale, (c, v) => c with { ReadingSkillStrainScale = v })
            ),
            new DifficultyTuningSection<OsuDifficultyConstants>("Aim bonuses",
                doubleParam(nameof(OsuDifficultyConstants.AimWideAngleBonusScale), c => c.AimWideAngleBonusScale, (c, v) => c with { AimWideAngleBonusScale = v }),
                doubleParam(nameof(OsuDifficultyConstants.AimAcuteAngleScale), c => c.AimAcuteAngleScale, (c, v) => c with { AimAcuteAngleScale = v }),
                doubleParam(nameof(OsuDifficultyConstants.AimSliderBonusScale), c => c.AimSliderBonusScale, (c, v) => c with { AimSliderBonusScale = v }),
                doubleParam(nameof(OsuDifficultyConstants.AimVelocityChangeBonusScale), c => c.AimVelocityChangeBonusScale, (c, v) => c with { AimVelocityChangeBonusScale = v }),
                doubleParam(nameof(OsuDifficultyConstants.AimWiggleBonusScale), c => c.AimWiggleBonusScale, (c, v) => c with { AimWiggleBonusScale = v }, defaultEnabled: false),
                doubleParam(nameof(OsuDifficultyConstants.AimHighBpmBonusBase), c => c.AimHighBpmBonusBase, (c, v) => c with { AimHighBpmBonusBase = v })
            ),
            new DifficultyTuningSection<OsuDifficultyConstants>("Flashlight bonuses",
                doubleParam(nameof(OsuDifficultyConstants.FlashlightMaxOpacityBonusScale), c => c.FlashlightMaxOpacityBonusScale, (c, v) => c with { FlashlightMaxOpacityBonusScale = v }, defaultEnabled: false),
                doubleParam(nameof(OsuDifficultyConstants.FlashlightHiddenBonusScale), c => c.FlashlightHiddenBonusScale, (c, v) => c with { FlashlightHiddenBonusScale = v }, defaultEnabled: false),
                doubleParam(nameof(OsuDifficultyConstants.FlashlightMinVelocityScale), c => c.FlashlightMinVelocityScale, (c, v) => c with { FlashlightMinVelocityScale = v }, defaultEnabled: false),
                doubleParam(nameof(OsuDifficultyConstants.FlashlightSliderBonusScale), c => c.FlashlightSliderBonusScale, (c, v) => c with { FlashlightSliderBonusScale = v }, defaultEnabled: false),
                doubleParam(nameof(OsuDifficultyConstants.FlashlightMinAngleScale), c => c.FlashlightMinAngleScale, (c, v) => c with { FlashlightMinAngleScale = v }, defaultEnabled: false)
            ),
            new DifficultyTuningSection<OsuDifficultyConstants>("Rhythm tuning",
                intParam(nameof(OsuDifficultyConstants.RhythmHistoryTimeMax), c => c.RhythmHistoryTimeMax, (c, v) => c with { RhythmHistoryTimeMax = v }, defaultEnabled: false),
                intParam(nameof(OsuDifficultyConstants.RhythmHistoryObjectsMax), c => c.RhythmHistoryObjectsMax, (c, v) => c with { RhythmHistoryObjectsMax = v }, defaultEnabled: false),
                doubleParam(nameof(OsuDifficultyConstants.RhythmOverallScale), c => c.RhythmOverallScale, (c, v) => c with { RhythmOverallScale = v }),
                doubleParam(nameof(OsuDifficultyConstants.RhythmRatioScale), c => c.RhythmRatioScale, (c, v) => c with { RhythmRatioScale = v })
            ),
            new DifficultyTuningSection<OsuDifficultyConstants>("Reading tuning",
                doubleParam(nameof(OsuDifficultyConstants.ReadingWindowSize), c => c.ReadingWindowSize, (c, v) => c with { ReadingWindowSize = v }, defaultEnabled: false),
                doubleParam(nameof(OsuDifficultyConstants.ReadingDistanceInfluenceThreshold), c => c.ReadingDistanceInfluenceThreshold, (c, v) => c with { ReadingDistanceInfluenceThreshold = v }, defaultEnabled: false),
                doubleParam(nameof(OsuDifficultyConstants.ReadingHiddenMultiplier), c => c.ReadingHiddenMultiplier, (c, v) => c with { ReadingHiddenMultiplier = v }, defaultEnabled: false),
                doubleParam(nameof(OsuDifficultyConstants.ReadingDensityMultiplier), c => c.ReadingDensityMultiplier, (c, v) => c with { ReadingDensityMultiplier = v }, defaultEnabled: false),
                doubleParam(nameof(OsuDifficultyConstants.ReadingDensityDifficultyBase), c => c.ReadingDensityDifficultyBase, (c, v) => c with { ReadingDensityDifficultyBase = v }, defaultEnabled: false),
                doubleParam(nameof(OsuDifficultyConstants.ReadingPreemptBalancingFactor), c => c.ReadingPreemptBalancingFactor, (c, v) => c with { ReadingPreemptBalancingFactor = v }, defaultEnabled: false),
                doubleParam(nameof(OsuDifficultyConstants.ReadingPreemptStartingPoint), c => c.ReadingPreemptStartingPoint, (c, v) => c with { ReadingPreemptStartingPoint = v }, defaultEnabled: false),
                doubleParam(nameof(OsuDifficultyConstants.ReadingMinimumAngleRelevancyTime), c => c.ReadingMinimumAngleRelevancyTime, (c, v) => c with { ReadingMinimumAngleRelevancyTime = v }, defaultEnabled: false),
                doubleParam(nameof(OsuDifficultyConstants.ReadingMaximumAngleRelevancyTime), c => c.ReadingMaximumAngleRelevancyTime, (c, v) => c with { ReadingMaximumAngleRelevancyTime = v }, defaultEnabled: false),
                doubleParam(nameof(OsuDifficultyConstants.ReadingReducedDifficultyBaseLine), c => c.ReadingReducedDifficultyBaseLine, (c, v) => c with { ReadingReducedDifficultyBaseLine = v }, defaultEnabled: false, minValue: 0.0),
                doubleParam(nameof(OsuDifficultyConstants.ReadingReducedDifficultyDuration), c => c.ReadingReducedDifficultyDuration, (c, v) => c with { ReadingReducedDifficultyDuration = v }, defaultEnabled: false)
            ),
            new DifficultyTuningSection<OsuDifficultyConstants>("Speed tuning",
                doubleParam(nameof(OsuDifficultyConstants.SpeedSingleSpacingThreshold), c => c.SpeedSingleSpacingThreshold, (c, v) => c with { SpeedSingleSpacingThreshold = v }),
                doubleParam(nameof(OsuDifficultyConstants.SpeedMinBonusBpm), c => c.SpeedMinBonusBpm, (c, v) => c with { SpeedMinBonusBpm = v }),
                doubleParam(nameof(OsuDifficultyConstants.SpeedBalancingFactor), c => c.SpeedBalancingFactor, (c, v) => c with { SpeedBalancingFactor = v }),
                doubleParam(nameof(OsuDifficultyConstants.SpeedHighBpmBonusBase), c => c.SpeedHighBpmBonusBase, (c, v) => c with { SpeedHighBpmBonusBase = v })
            )
        };

        public static readonly IReadOnlyList<DifficultyTuningParameter<OsuDifficultyConstants>> All =
            Sections.SelectMany(s => s.Parameters).ToArray();

        /// <summary>
        /// Returns property names from <see cref="OsuDifficultyConstants"/> that are not covered by any tuning parameter definition.
        /// Useful as a drift detection warning when the constants record gains new properties.
        /// </summary>
        public static IReadOnlyList<string> GetUncoveredProperties()
        {
            var definedNames = new HashSet<string>(All.Select(p => p.PropertyName));

            return typeof(OsuDifficultyConstants)
                   .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                   .Where(p => p.CanRead && !definedNames.Contains(p.Name))
                   .Select(p => p.Name)
                   .ToList();
        }

        private static DifficultyTuningParameter<OsuDifficultyConstants> doubleParam(
            string propertyName, Func<OsuDifficultyConstants, double> getter,
            Func<OsuDifficultyConstants, double, OsuDifficultyConstants> setter,
            bool defaultEnabled = true, double minValue = 0.01, double? maxValue = null)
        {
            return DifficultyTuningParameter<OsuDifficultyConstants>.ForDouble(
                propertyName, propertyName.Humanize(LetterCasing.Sentence), getter, setter, defaultEnabled, minValue, maxValue);
        }

        private static DifficultyTuningParameter<OsuDifficultyConstants> intParam(
            string propertyName, Func<OsuDifficultyConstants, int> getter,
            Func<OsuDifficultyConstants, int, OsuDifficultyConstants> setter,
            bool defaultEnabled = true, int minValue = 1, int? maxValue = null)
        {
            return DifficultyTuningParameter<OsuDifficultyConstants>.ForInt(
                propertyName, propertyName.Humanize(LetterCasing.Sentence), getter, setter, defaultEnabled, minValue, maxValue);
        }
    }
}
