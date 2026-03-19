// Copyright CodeGamified 2025-2026
// MIT License — Golf
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using CodeGamified.Bootstrap;
using CodeGamified.Camera;
using CodeGamified.Quality;
using CodeGamified.Settings;
using CodeGamified.Time;
using Golf.Game;
using Golf.Core;
using Golf.Scripting;
using Golf.UI;

namespace Golf.Core
{
    /// <summary>
    /// Bootstrap for Golf. Instantiates all systems: course, ball, match manager,
    /// renderer, camera, player program, input provider, TUI. Wires events and starts round.
    /// </summary>
    public class GolfBootstrap : GameBootstrap, IQualityResponsive
    {
        // ═══════════════════════════════════════════════════════════════
        // INSPECTOR
        // ═══════════════════════════════════════════════════════════════

        [Header("Golf")]
        [Tooltip("Enable player scripting")]
        public bool enableScripting = true;

        [Header("Match")]
        [Tooltip("Auto-restart after round over")]
        public bool autoRestart = true;

        [Tooltip("Delay before restarting (sim-seconds)")]
        public float restartDelay = 3f;

        protected override string LogTag => "GOLF";

        // ═══════════════════════════════════════════════════════════════
        // REFERENCES
        // ═══════════════════════════════════════════════════════════════

        private GolfCourse _course;
        private GolfBall _ball;
        private GolfMatchManager _match;
        private GolfRenderer _renderer;
        private GolfProgram _program;
        private GolfInputProvider _input;

        // Trail
        private GolfBallTrail _ballTrail;

        // TUI
        private GolfTUIManager _tuiManager;

        // Camera
        private CameraAmbientMotion _cameraSway;

        // Post-processing
        private Bloom _bloom;
        private Volume _postProcessVolume;

        // ═══════════════════════════════════════════════════════════════
        // UPDATE
        // ═══════════════════════════════════════════════════════════════

        private void Update()
        {
            UpdateBloomScale();
        }

        private void UpdateBloomScale()
        {
            if (_bloom == null || !_bloom.active) return;
            var cam = Camera.main;
            if (cam == null) return;
            float dist = Vector3.Distance(cam.transform.position,
                CourseCenter(_course?.CurrentHole ?? default));
            float defaultDist = 10f;
            float scale = Mathf.Clamp01(defaultDist / Mathf.Max(dist, 0.01f));
            _bloom.intensity.value = Mathf.Lerp(0.5f, 1.2f, scale);
        }

        private static Vector3 CourseCenter(HoleData hole)
        {
            return new Vector3(hole.width * 0.5f, 0f, hole.depth * 0.5f);
        }

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void Start()
        {
            Log("╔══════════════════════════════════════╗");
            Log("║          GOLF  BOOTSTRAP             ║");
            Log("╚══════════════════════════════════════╝");
            LogDivider();

            // 1. Settings + Quality
            SettingsBridge.Load();
            QualityBridge.SetTier((QualityTier)SettingsBridge.QualityLevel);
            QualityBridge.Register(this);

            // 2. Simulation time
            EnsureSimulationTime<GolfSimulationTime>();
            LogStatus("TIME", "GolfSimulationTime ready");

            // 3. Camera + post-processing
            SetupCamera();
            LogStatus("CAMERA", "Perspective, bloom enabled");

            // 4. Course (domain object — generate holes)
            _course = CreateManager<GolfCourse>("GolfCourse");
            _course.GenerateCourse();
            LogStatus("COURSE", $"{_course.TotalHoles} holes generated");

            // 5. Ball
            _ball = CreateManager<GolfBall>("GolfBall");
            _ball.Initialize(_course);

            // 6. Match manager
            _match = CreateManager<GolfMatchManager>("GolfMatchManager");
            _match.Initialize(_course, _ball);
            LogStatus("MATCH", "MatchManager ready, auto-restart=" + _match.autoRestart);

            // 7. Renderer
            _renderer = CreateManager<GolfRenderer>("GolfRenderer");
            _renderer.Initialize(_course, _ball);
            LogStatus("RENDERER", "Primitive renderer ready");

            // 8. Ball trail
            CreateBallTrail();

            // 9. Input provider
            _input = CreateManager<GolfInputProvider>("GolfInputProvider");
            LogEnabled("INPUT", true, "WASD/Arrows + Space");

            // 10. Player program
            if (enableScripting)
            {
                _program = CreateManager<GolfProgram>("GolfProgram");
                _program.Initialize(_match, _course, _ball, _renderer);
                LogEnabled("SCRIPTING", true, "GolfProgram (20 ops/sec)");
            }
            else
            {
                LogEnabled("SCRIPTING", false);
            }

            // 11. TUI
            CreateTUI();

            // 12. Wire events + start
            WireEvents();
            LogDivider();

            RunAfterFrames(() =>
            {
                _renderer.BuildHole();
                _match.StartRound();
                Log("⛳ Round started — 9 holes, code controls every putt!");
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // CAMERA
        // ═══════════════════════════════════════════════════════════════

        private void SetupCamera()
        {
            var cam = EnsureCamera();
            cam.orthographic = false;
            cam.fieldOfView = 50f;

            // Position above and behind the first hole
            cam.transform.position = new Vector3(5f, 18f, -3f);
            cam.transform.LookAt(new Vector3(5f, 0f, 7f));
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.01f, 0.01f, 0.02f);

            // Ambient sway
            _cameraSway = cam.gameObject.AddComponent<CameraAmbientMotion>();
            _cameraSway.lookAtTarget = new Vector3(5f, 0f, 7f);
            _cameraSway.amplitudeX = 0.3f;
            _cameraSway.amplitudeY = 0.15f;

            // URP post-processing
            var camData = cam.GetComponent<UniversalAdditionalCameraData>()
                ?? cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;

            var volumeGO = new GameObject("PostProcessVolume");
            _postProcessVolume = volumeGO.AddComponent<Volume>();
            _postProcessVolume.isGlobal = true;
            _postProcessVolume.priority = 1;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _bloom = profile.Add<Bloom>();
            _bloom.threshold.overrideState = true;
            _bloom.threshold.value = 0.8f;
            _bloom.intensity.overrideState = true;
            _bloom.intensity.value = 1.2f;
            _bloom.scatter.overrideState = true;
            _bloom.scatter.value = 0.5f;
            _bloom.clamp.overrideState = true;
            _bloom.clamp.value = 20f;
            _bloom.highQualityFiltering.overrideState = true;
            _bloom.highQualityFiltering.value = true;
            _postProcessVolume.profile = profile;

            Log("Camera: perspective, FOV=50, top-down golf view + sway + bloom");
        }

        // ═══════════════════════════════════════════════════════════════
        // BALL TRAIL
        // ═══════════════════════════════════════════════════════════════

        private void CreateBallTrail()
        {
            var go = new GameObject("BallTrail");
            _ballTrail = go.AddComponent<GolfBallTrail>();
            _ballTrail.Initialize(_ball, Color.white);
            Log("Created BallTrail (golf ball trail)");
        }

        // ═══════════════════════════════════════════════════════════════
        // TUI (.engine powered)
        // ═══════════════════════════════════════════════════════════════

        private void CreateTUI()
        {
            var go = new GameObject("GolfTUI");
            _tuiManager = go.AddComponent<GolfTUIManager>();
            _tuiManager.Initialize(_match, _program);
            Log("Created TUI (left debugger + right status panel)");
        }

        // ═══════════════════════════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════════════════════════

        private void WireEvents()
        {
            if (SimulationTime.Instance != null)
            {
                SimulationTime.Instance.OnTimeScaleChanged += s => Log($"Time scale → {s:F0}x");
                SimulationTime.Instance.OnPausedChanged += p => Log(p ? "⏸ PAUSED" : "▶ RESUMED");
            }

            _match.OnStroke += strokes =>
            {
                Log($"Stroke {strokes} │ Hole {_course.CurrentHoleIndex + 1}");
                // Subtle ball glow on each putt
                _renderer?.FlashBallLight(1.5f, Color.white);
                _renderer?.FlashBallColor(2f);
                _ballTrail?.ClearLine();
            };

            _match.OnHoleComplete += OnHoleComplete;
            _match.OnRoundComplete += OnRoundComplete;
            _match.OnRoundRestarted += OnRoundRestarted;
        }

        private void OnHoleComplete(int holeIndex, int strokes)
        {
            string scoreName = GolfMatchManager.GetScoreName(strokes, _course.Holes[holeIndex].par);
            Log($"Hole {holeIndex + 1}: {strokes} strokes ({scoreName})");

            // Glow feedback — brighter for better scores
            int diff = strokes - _course.Holes[holeIndex].par;
            if (diff <= -2) // Eagle or better
            {
                _renderer?.FlashBallLight(4f, Color.yellow);
                _renderer?.FlashBallColor(6f);
            }
            else if (diff <= 0) // Par or birdie
            {
                _renderer?.FlashBallLight(3f, Color.green);
                _renderer?.FlashBallColor(4f);
            }
            else // Bogey+
            {
                _renderer?.FlashBallLight(2f, new Color(1f, 0.5f, 0f));
                _renderer?.FlashBallColor(2.5f);
            }

            _ballTrail?.ClearLine();

            // Rebuild renderer for next hole
            if (_course.CurrentHoleIndex < _course.TotalHoles)
            {
                UpdateCameraForHole();
                _renderer.BuildHole();
            }
        }

        private void OnRoundComplete(int totalStrokes)
        {
            LogDivider();
            Log($"⛳ ROUND COMPLETE — {totalStrokes} strokes, {_match.ScoreVsPar:+#;-#;E} vs par");
            LogDivider();

            // Big flash on round complete
            Color endColor = _match.ScoreVsPar <= 0 ? Color.green : new Color(1f, 0.5f, 0f);
            _renderer?.FlashBallLight(4f, endColor);
            _renderer?.FlashBallColor(6f);
            _ballTrail?.ClearLine();
        }

        private void OnRoundRestarted()
        {
            Log("New round started!");
            _renderer?.MarkDirty();
            _ballTrail?.ClearLine();
            UpdateCameraForHole();
            _renderer.BuildHole();
        }

        private void UpdateCameraForHole()
        {
            var hole = _course.CurrentHole;
            float cx = hole.width * 0.5f;
            float cz = hole.depth * 0.5f;
            float height = Mathf.Max(hole.width, hole.depth) * 1.2f;

            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(cx, height, cz - hole.depth * 0.3f);
                cam.transform.LookAt(new Vector3(cx, 0f, cz));

                if (_cameraSway != null)
                    _cameraSway.lookAtTarget = new Vector3(cx, 0f, cz);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // QUALITY
        // ═══════════════════════════════════════════════════════════════

        public void OnQualityChanged(QualityTier tier) { }

        private void OnDestroy()
        {
            QualityBridge.Unregister(this);
            if (_match != null)
            {
                _match.OnHoleComplete -= OnHoleComplete;
                _match.OnRoundComplete -= OnRoundComplete;
                _match.OnRoundRestarted -= OnRoundRestarted;
            }
        }
    }
}
