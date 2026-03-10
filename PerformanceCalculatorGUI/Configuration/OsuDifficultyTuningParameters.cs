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
                doubleParam(nameof(OsuDifficultyConstants.AccuracyPerformanceScale), c => c.AccuracyPerformanceScale, (c, v) => c with { AccuracyPerformanceScale = v }, defaultEnabled: false),
                doubleParam(nameof(OsuDifficultyConstants.TotalPerformanceScale), c => c.TotalPerformanceScale, (c, v) => c with { TotalPerformanceScale = v }, defaultEnabled: false),
                doubleParam(nameof(OsuDifficultyConstants.CognitionPerformanceExponent), c => c.CognitionPerformanceExponent, (c, v) => c with { CognitionPerformanceExponent = v }, defaultEnabled: false)
            ),
            new DifficultyTuningSection<OsuDifficultyConstants>("Aim skill",
                doubleParam(nameof(OsuDifficultyConstants.AimSkillMultiplierSnap), c => c.AimSkillMultiplierSnap, (c, v) => c with { AimSkillMultiplierSnap = v }),
                doubleParam(nameof(OsuDifficultyConstants.AimSkillMultiplierAgility), c => c.AimSkillMultiplierAgility, (c, v) => c with { AimSkillMultiplierAgility = v }),
                doubleParam(nameof(OsuDifficultyConstants.AimSkillMultiplierFlow), c => c.AimSkillMultiplierFlow, (c, v) => c with { AimSkillMultiplierFlow = v }),
                doubleParam(nameof(OsuDifficultyConstants.AimSkillMultiplierTotal), c => c.AimSkillMultiplierTotal, (c, v) => c with { AimSkillMultiplierTotal = v }),
                doubleParam(nameof(OsuDifficultyConstants.AimMeanExponent), c => c.AimMeanExponent, (c, v) => c with { AimMeanExponent = v }),
                doubleParam(nameof(OsuDifficultyConstants.AimStrainDecayBase), c => c.AimStrainDecayBase, (c, v) => c with { AimStrainDecayBase = v })
            ),
            new DifficultyTuningSection<OsuDifficultyConstants>("Speed skill",
                doubleParam(nameof(OsuDifficultyConstants.SpeedSkillMultiplier), c => c.SpeedSkillMultiplier, (c, v) => c with { SpeedSkillMultiplier = v }),
                doubleParam(nameof(OsuDifficultyConstants.SpeedStrainDecayBase), c => c.SpeedStrainDecayBase, (c, v) => c with { SpeedStrainDecayBase = v }),
                doubleParam(nameof(OsuDifficultyConstants.SpeedHarmonicScale), c => c.SpeedHarmonicScale, (c, v) => c with { SpeedHarmonicScale = v }),
                doubleParam(nameof(OsuDifficultyConstants.SpeedDecayExponent), c => c.SpeedDecayExponent, (c, v) => c with { SpeedDecayExponent = v })
            ),
            new DifficultyTuningSection<OsuDifficultyConstants>("Reading skill",
                doubleParam(nameof(OsuDifficultyConstants.ReadingSkillMultiplier), c => c.ReadingSkillMultiplier, (c, v) => c with { ReadingSkillMultiplier = v }),
                doubleParam(nameof(OsuDifficultyConstants.ReadingStrainDecayBase), c => c.ReadingStrainDecayBase, (c, v) => c with { ReadingStrainDecayBase = v })
            ),
            new DifficultyTuningSection<OsuDifficultyConstants>("Flashlight skill",
                doubleParam(nameof(OsuDifficultyConstants.FlashlightSkillMultiplier), c => c.FlashlightSkillMultiplier, (c, v) => c with { FlashlightSkillMultiplier = v }, defaultEnabled: false),
                doubleParam(nameof(OsuDifficultyConstants.FlashlightStrainDecayBase), c => c.FlashlightStrainDecayBase, (c, v) => c with { FlashlightStrainDecayBase = v }, defaultEnabled: false)
            ),
            new DifficultyTuningSection<OsuDifficultyConstants>("Snap aim bonuses",
                doubleParam(nameof(OsuDifficultyConstants.SnapAimWideAngleScale), c => c.SnapAimWideAngleScale, (c, v) => c with { SnapAimWideAngleScale = v }),
                doubleParam(nameof(OsuDifficultyConstants.SnapAimAcuteAngleScale), c => c.SnapAimAcuteAngleScale, (c, v) => c with { SnapAimAcuteAngleScale = v }),
                doubleParam(nameof(OsuDifficultyConstants.SnapAimSliderScale), c => c.SnapAimSliderScale, (c, v) => c with { SnapAimSliderScale = v }),
                doubleParam(nameof(OsuDifficultyConstants.SnapAimVelocityChangeScale), c => c.SnapAimVelocityChangeScale, (c, v) => c with { SnapAimVelocityChangeScale = v }),
                doubleParam(nameof(OsuDifficultyConstants.SnapAimWiggleScale), c => c.SnapAimWiggleScale, (c, v) => c with { SnapAimWiggleScale = v }, defaultEnabled: false),
                doubleParam(nameof(OsuDifficultyConstants.SnapAimHighBpmBonusBase), c => c.SnapAimHighBpmBonusBase, (c, v) => c with { SnapAimHighBpmBonusBase = v }),
                doubleParam(nameof(OsuDifficultyConstants.SnapAimHighBpmExponent), c => c.SnapAimHighBpmExponent, (c, v) => c with { SnapAimHighBpmExponent = v }),
                doubleParam(nameof(OsuDifficultyConstants.SnapAimMaxRepetitionNerf), c => c.SnapAimMaxRepetitionNerf, (c, v) => c with { SnapAimMaxRepetitionNerf = v }),
                doubleParam(nameof(OsuDifficultyConstants.SnapAimMaxVectorInfluence), c => c.SnapAimMaxVectorInfluence, (c, v) => c with { SnapAimMaxVectorInfluence = v })
            ),
            new DifficultyTuningSection<OsuDifficultyConstants>("Agility bonuses",
                doubleParam(nameof(OsuDifficultyConstants.AgilityHighBpmBonusBase), c => c.AgilityHighBpmBonusBase, (c, v) => c with { AgilityHighBpmBonusBase = v }),
                doubleParam(nameof(OsuDifficultyConstants.AgilityHighBpmExponent), c => c.AgilityHighBpmExponent, (c, v) => c with { AgilityHighBpmExponent = v })
            ),
            new DifficultyTuningSection<OsuDifficultyConstants>("Flow aim bonuses",
                doubleParam(nameof(OsuDifficultyConstants.FlowVelocityChangeScale), c => c.FlowVelocityChangeScale, (c, v) => c with { FlowVelocityChangeScale = v })
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
                doubleParam(nameof(OsuDifficultyConstants.SpeedMinBonusBpm), c => c.SpeedMinBonusBpm, (c, v) => c with { SpeedMinBonusBpm = v }),
                doubleParam(nameof(OsuDifficultyConstants.SpeedBalancingFactor), c => c.SpeedBalancingFactor, (c, v) => c with { SpeedBalancingFactor = v }),
                doubleParam(nameof(OsuDifficultyConstants.SpeedHighBpmBonusBase), c => c.SpeedHighBpmBonusBase, (c, v) => c with { SpeedHighBpmBonusBase = v }),
                doubleParam(nameof(OsuDifficultyConstants.SpeedHighBpmExponent), c => c.SpeedHighBpmExponent, (c, v) => c with { SpeedHighBpmExponent = v })
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
