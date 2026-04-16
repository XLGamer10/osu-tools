// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace PerformanceCalculatorGUI.Screens.Collections.Autobalance
{
    public readonly struct AutobalanceResult<TConstants>
    {
        public bool IsFailure { get; }
        public TConstants? Constants { get; }
        public EvaluationResult Evaluation { get; }
        public int SampleCount { get; }
        public string? ErrorMessage { get; }

        private AutobalanceResult(TConstants constants, EvaluationResult evaluation, int sampleCount)
        {
            IsFailure = false;
            Constants = constants;
            Evaluation = evaluation;
            SampleCount = sampleCount;
            ErrorMessage = null;
        }

        private AutobalanceResult(string errorMessage)
        {
            IsFailure = true;
            Constants = default;
            Evaluation = default;
            SampleCount = 0;
            ErrorMessage = errorMessage;
        }

        public static AutobalanceResult<TConstants> Success(TConstants constants, EvaluationResult evaluation, int sampleCount)
            => new AutobalanceResult<TConstants>(constants, evaluation, sampleCount);

        public static AutobalanceResult<TConstants> Failure(string errorMessage)
            => new AutobalanceResult<TConstants>(errorMessage);
    }
}
