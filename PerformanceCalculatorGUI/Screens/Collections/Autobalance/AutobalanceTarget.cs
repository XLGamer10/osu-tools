// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel;

namespace PerformanceCalculatorGUI.Screens.Collections.Autobalance
{
    public enum AutobalanceTarget
    {
        [Description("Total")]
        Total,

        [Description("Aim")]
        Aim,

        [Description("Speed")]
        Speed,

        [Description("Accuracy")]
        Accuracy,

        [Description("Reading")]
        Reading,

        [Description("Flashlight")]
        Flashlight
    }
}
