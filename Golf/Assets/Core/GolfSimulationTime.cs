// Copyright CodeGamified 2025-2026
// MIT License — Golf
using CodeGamified.Time;

namespace Golf.Core
{
    public class GolfSimulationTime : SimulationTime
    {
        protected override float MaxTimeScale => 100f;

        protected override void OnInitialize()
        {
            timeScalePresets = new[] { 0f, 0.25f, 0.5f, 1f, 2f, 5f, 10f, 50f, 100f };
            currentPresetIndex = 3; // 1x
        }

        public override string GetFormattedTime()
        {
            int m = (int)(simulationTime / 60.0);
            int s = (int)(simulationTime % 60.0);
            return $"{m:D2}:{s:D2}";
        }
    }
}
