// Copyright CodeGamified 2025-2026
// MIT License — Golf
using System;
using System.Collections;
using UnityEngine;
using CodeGamified.Time;

namespace Golf.Game
{
    /// <summary>
    /// Manages match flow: stroke counting, hole progression, scoring, game over, auto-restart.
    /// </summary>
    public class GolfMatchManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // INSPECTOR
        // ═══════════════════════════════════════════════════════════════

        [Header("Match")]
        [Tooltip("Auto-restart round after completion")]
        public bool autoRestart = true;

        [Tooltip("Delay before auto-restart (sim seconds)")]
        public float restartDelay = 3f;

        // ═══════════════════════════════════════════════════════════════
        // STATE
        // ═══════════════════════════════════════════════════════════════

        public int CurrentHoleIndex => _course?.CurrentHoleIndex ?? 0;
        public int TotalHoles => _course?.TotalHoles ?? 0;
        public int StrokesThisHole { get; private set; }
        public int TotalStrokes { get; private set; }
        public int ScoreVsPar { get; private set; }
        public int[] StrokesPerHole { get; private set; }
        public bool IsRoundComplete { get; private set; }
        public bool IsHoleComplete { get; private set; }

        private GolfCourse _course;
        private GolfBall _ball;

        // ═══════════════════════════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════════════════════════

        public event Action<int> OnStroke;          // stroke count
        public event Action<int, int> OnHoleComplete; // hole index, strokes
        public event Action<int> OnRoundComplete;     // total strokes
        public event Action OnRoundRestarted;

        // ═══════════════════════════════════════════════════════════════
        // INIT
        // ═══════════════════════════════════════════════════════════════

        public void Initialize(GolfCourse course, GolfBall ball)
        {
            _course = course;
            _ball = ball;
            StrokesPerHole = new int[course.TotalHoles];

            _ball.OnBallSunk += HandleBallSunk;
            _ball.OnBallInWater += HandleBallInWater;
        }

        public void StartRound()
        {
            _course.CurrentHoleIndex = 0;
            TotalStrokes = 0;
            ScoreVsPar = 0;
            IsRoundComplete = false;
            IsHoleComplete = false;

            for (int i = 0; i < StrokesPerHole.Length; i++)
                StrokesPerHole[i] = 0;

            StartHole();
        }

        // ═══════════════════════════════════════════════════════════════
        // HOLE FLOW
        // ═══════════════════════════════════════════════════════════════

        private void StartHole()
        {
            StrokesThisHole = 0;
            IsHoleComplete = false;
            _ball.PlaceAtTee();
        }

        /// <summary>Call when a putt is executed (before ball moves).</summary>
        public void RecordStroke()
        {
            StrokesThisHole++;
            TotalStrokes++;
            OnStroke?.Invoke(StrokesThisHole);
        }

        private void HandleBallSunk()
        {
            IsHoleComplete = true;
            int holeIdx = _course.CurrentHoleIndex;
            StrokesPerHole[holeIdx] = StrokesThisHole;
            ScoreVsPar += StrokesThisHole - _course.CurrentHole.par;

            Debug.Log($"[GOLF] Hole {holeIdx + 1} complete! {StrokesThisHole} strokes (par {_course.CurrentHole.par}) — {GetScoreName(StrokesThisHole, _course.CurrentHole.par)}");
            OnHoleComplete?.Invoke(holeIdx, StrokesThisHole);

            // Advance to next hole or finish round
            if (_course.CurrentHoleIndex < _course.TotalHoles - 1)
            {
                _course.CurrentHoleIndex++;
                StartHole();
            }
            else
            {
                FinishRound();
            }
        }

        private void HandleBallInWater()
        {
            // Penalty stroke + reset to pre-putt position
            StrokesThisHole++;
            TotalStrokes++;
            Debug.Log($"[GOLF] Water hazard! Penalty stroke. Total this hole: {StrokesThisHole}");
            _ball.ResetToPrePutt();
        }

        private void FinishRound()
        {
            IsRoundComplete = true;
            Debug.Log($"[GOLF] Round complete! Total: {TotalStrokes} strokes, {ScoreVsPar:+#;-#;E} vs par");
            OnRoundComplete?.Invoke(TotalStrokes);

            if (autoRestart)
                StartCoroutine(AutoRestartCoroutine());
        }

        private IEnumerator AutoRestartCoroutine()
        {
            float waited = 0f;
            while (waited < restartDelay)
            {
                if (SimulationTime.Instance != null && SimulationTime.Instance.isPaused)
                {
                    yield return null;
                    continue;
                }
                float ts = SimulationTime.Instance?.timeScale ?? 1f;
                waited += UnityEngine.Time.deltaTime * ts;
                yield return null;
            }

            StartRound();
            OnRoundRestarted?.Invoke();
        }

        private void OnDestroy()
        {
            if (_ball != null)
            {
                _ball.OnBallSunk -= HandleBallSunk;
                _ball.OnBallInWater -= HandleBallInWater;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // SCORING NAMES
        // ═══════════════════════════════════════════════════════════════

        public static string GetScoreName(int strokes, int par)
        {
            int diff = strokes - par;
            if (strokes == 1) return "Hole-in-One!";
            return diff switch
            {
                <= -3 => "Albatross",
                -2 => "Eagle",
                -1 => "Birdie",
                0 => "Par",
                1 => "Bogey",
                2 => "Double Bogey",
                _ => $"+{diff}"
            };
        }
    }
}
