// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;

namespace PerformanceCalculatorGUI.Configuration
{
    public sealed class DifficultyTuningParameter<TConstants>
    {
        public string PropertyName { get; }
        public string UiLabel { get; }
        public bool IsInteger { get; }
        public double MinValue { get; }
        public double? MaxValue { get; }
        public bool DefaultEnabled { get; }
        public Func<TConstants, double> Getter { get; }
        public Func<TConstants, double, TConstants> Setter { get; }

        private DifficultyTuningParameter(string propertyName, string uiLabel, bool isInteger, double minValue, double? maxValue,
                                          bool defaultEnabled,
                                          Func<TConstants, double> getter, Func<TConstants, double, TConstants> setter)
        {
            PropertyName = propertyName;
            UiLabel = uiLabel;
            IsInteger = isInteger;
            MinValue = minValue;
            MaxValue = maxValue;
            DefaultEnabled = defaultEnabled;
            Getter = getter;
            Setter = setter;
        }

        public DifficultyTuningParameter<TConstants> WithBounds(double minValue, double? maxValue)
            => new DifficultyTuningParameter<TConstants>(PropertyName, UiLabel, IsInteger, minValue, maxValue, DefaultEnabled, Getter, Setter);

        public static DifficultyTuningParameter<TConstants> ForDouble(string propertyName, string uiLabel, Func<TConstants, double> getter,
                                                                      Func<TConstants, double, TConstants> setter, bool defaultEnabled = true,
                                                                      double minValue = 0.01, double? maxValue = null)
            => new DifficultyTuningParameter<TConstants>(propertyName, uiLabel, false, minValue, maxValue, defaultEnabled, getter, setter);

        public static DifficultyTuningParameter<TConstants> ForInt(string propertyName, string uiLabel, Func<TConstants, int> getter,
                                                                   Func<TConstants, int, TConstants> setter, bool defaultEnabled = true,
                                                                   int minValue = 1, int? maxValue = null)
            => new DifficultyTuningParameter<TConstants>(propertyName, uiLabel, true, minValue, maxValue, defaultEnabled,
                                                         t => getter(t), (t, v) => setter(t, (int)v));
    }

    public sealed class DifficultyTuningSection<TConstants>
    {
        public string Title { get; }
        public IReadOnlyList<DifficultyTuningParameter<TConstants>> Parameters { get; }

        public DifficultyTuningSection(string title, params DifficultyTuningParameter<TConstants>[] parameters)
        {
            Title = title;
            Parameters = parameters;
        }
    }
}
