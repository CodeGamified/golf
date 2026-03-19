// Copyright CodeGamified 2025-2026
// MIT License — Golf
using UnityEngine;
using CodeGamified.Engine;
using CodeGamified.Engine.Compiler;
using CodeGamified.Engine.Runtime;
using CodeGamified.Time;
using Golf.Game;

namespace Golf.Scripting
{
    /// <summary>
    /// Tick-based execution loop for the player's golf program.
    /// Runs at OPS_PER_SECOND scaled by simulation time.
    /// Program loops: HALT resets PC to 0 so the player's script
    /// re-evaluates every cycle (check ball, putt, repeat).
    /// </summary>
    public class GolfProgram : ProgramBehaviour
    {
        public const float OPS_PER_SECOND = 20f;
        private float _opAccumulator;

        private GolfMatchManager _match;
        private GolfCourse _course;
        private GolfBall _ball;
        private GolfRenderer _renderer;

        // Default starter code — aim at hole, putt when ball stops
        private const string DEFAULT_CODE = @"# Mini Golf — Your code controls every putt!
# get_angle()  → angle from ball to cup
# get_dist()   → distance to cup
# putt(a, p)   → putt at angle a, power p (0-100)

moving = get_ball_moving()
if moving == 0:
    angle = get_angle()
    dist = get_dist()

    # Scale power to distance
    power = dist * 8
    if power > 85:
        power = 85

    aim(angle)
    putt(angle, power)
";

        public string CurrentSourceCode => _sourceCode;
        public System.Action OnCodeChanged;

        public void Initialize(GolfMatchManager match, GolfCourse course, GolfBall ball, GolfRenderer renderer)
        {
            _match = match;
            _course = course;
            _ball = ball;
            _renderer = renderer;

            _programName = "GolfPlayer";
            _sourceCode = DEFAULT_CODE;
            _autoRun = true;

            LoadAndRun(_sourceCode);
        }

        protected override void Update()
        {
            if (_executor == null || _program == null || _isPaused) return;

            float timeScale = SimulationTime.Instance?.timeScale ?? 1f;
            if (SimulationTime.Instance != null && SimulationTime.Instance.isPaused) return;

            float simDelta = Time.deltaTime * timeScale;
            _opAccumulator += simDelta * OPS_PER_SECOND;

            int opsToRun = (int)_opAccumulator;
            _opAccumulator -= opsToRun;

            for (int i = 0; i < opsToRun; i++)
            {
                if (_executor.State.IsHalted)
                {
                    _executor.State.PC = 0;
                    _executor.State.IsHalted = false;
                }
                _executor.ExecuteOne();
            }
            if (opsToRun > 0) ProcessEvents();
        }

        protected override IGameIOHandler CreateIOHandler()
            => new GolfIOHandler(_match, _course, _ball, _renderer);

        protected override CompiledProgram CompileSource(string source, string name)
            => PythonCompiler.Compile(source, name, new GolfCompilerExtension());

        protected override void ProcessEvents()
        {
            while (_executor.State.OutputEvents.Count > 0)
                _executor.State.OutputEvents.Dequeue();
        }

        public void UploadCode(string newSource)
        {
            _sourceCode = newSource ?? DEFAULT_CODE;
            LoadAndRun(_sourceCode);
            Debug.Log($"[GolfAI] Uploaded new code ({_program?.Instructions?.Length ?? 0} instructions)");
            OnCodeChanged?.Invoke();
        }

        public void ResetExecution()
        {
            if (_executor?.State == null) return;
            _executor.State.Reset();
            _opAccumulator = 0f;
        }
    }
}
