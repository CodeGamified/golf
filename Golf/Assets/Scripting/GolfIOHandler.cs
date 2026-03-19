// Copyright CodeGamified 2025-2026
// MIT License — Golf
using CodeGamified.Engine;
using CodeGamified.Time;
using Golf.Game;
using UnityEngine;

namespace Golf.Scripting
{
    /// <summary>
    /// Custom opcode enum for Golf. 24 opcodes: 20 queries + 4 commands.
    /// </summary>
    public enum GolfOpCode
    {
        // ═══════════════════════════════════════════════════════════════
        // QUERIES — result → R0
        // ═══════════════════════════════════════════════════════════════

        GET_BALL_X = 0,       // Ball X position
        GET_BALL_Y = 1,       // Ball Z position (player sees as Y)
        GET_HOLE_X = 2,       // Cup X position
        GET_HOLE_Y = 3,       // Cup Z position
        GET_DIST = 4,         // Distance from ball to cup
        GET_ANGLE = 5,        // Angle from ball to cup (degrees, 0=+X, 90=+Z)
        GET_PAR = 6,          // Par for current hole
        GET_STROKES = 7,      // Strokes taken this hole
        GET_TOTAL_STROKES = 8,// Total strokes all holes
        GET_HOLE_NUM = 9,     // Current hole number (1-based)
        GET_TOTAL_HOLES = 10, // Total holes in course
        GET_BALL_MOVING = 11, // Is ball rolling (1) or stopped (0)
        GET_WIND_ANGLE = 12,  // Wind direction (degrees)
        GET_WIND_SPEED = 13,  // Wind speed
        GET_TERRAIN = 14,     // Terrain under ball (0=green,1=fairway,2=rough,3=sand,4=water)
        GET_SLOPE_X = 15,     // Slope X at ball position
        GET_SLOPE_Y = 16,     // Slope Z at ball position
        GET_INPUT = 17,       // Manual input value
        GET_SCORE = 18,       // Score vs par (negative = under par)
        GET_LAST_DIST = 19,   // Distance of last putt

        // ═══════════════════════════════════════════════════════════════
        // COMMANDS — args in R0/R1, result → R0 (1=ok, 0=fail)
        // ═══════════════════════════════════════════════════════════════

        PUTT = 20,            // R0=angle(deg), R1=power(0-100) → 1=ok
        AIM = 21,             // R0=angle(deg) → sets aim line visual

        // ═══════════════════════════════════════════════════════════════
        // TWO-ARG QUERIES — R0=x, R1=y → result in R0
        // ═══════════════════════════════════════════════════════════════

        GET_TERRAIN_AT = 22,  // R0=x, R1=y → terrain type
        GET_DIST_TO = 23,     // R0=x, R1=y → distance from ball to (x,y)
    }

    /// <summary>
    /// Executes Golf custom opcodes. Bridges bytecode ↔ game state.
    /// </summary>
    public class GolfIOHandler : IGameIOHandler
    {
        private readonly GolfMatchManager _match;
        private readonly GolfCourse _course;
        private readonly GolfBall _ball;
        private readonly GolfRenderer _renderer;

        public GolfIOHandler(GolfMatchManager match, GolfCourse course, GolfBall ball, GolfRenderer renderer)
        {
            _match = match;
            _course = course;
            _ball = ball;
            _renderer = renderer;
        }

        public bool PreExecute(Instruction inst, MachineState state) => true;

        public void ExecuteIO(Instruction inst, MachineState state)
        {
            var op = (GolfOpCode)((int)inst.Op - (int)OpCode.CUSTOM_0);
            var hole = _course.CurrentHole;

            switch (op)
            {
                // ── Queries ──────────────────────────────────────────
                case GolfOpCode.GET_BALL_X:
                    state.SetRegister(0, _ball.Position.x);
                    break;
                case GolfOpCode.GET_BALL_Y:
                    state.SetRegister(0, _ball.Position.y);
                    break;
                case GolfOpCode.GET_HOLE_X:
                    state.SetRegister(0, hole.cupX);
                    break;
                case GolfOpCode.GET_HOLE_Y:
                    state.SetRegister(0, hole.cupZ);
                    break;
                case GolfOpCode.GET_DIST:
                {
                    float dx = hole.cupX - _ball.Position.x;
                    float dz = hole.cupZ - _ball.Position.y;
                    state.SetRegister(0, Mathf.Sqrt(dx * dx + dz * dz));
                    break;
                }
                case GolfOpCode.GET_ANGLE:
                {
                    float dx = hole.cupX - _ball.Position.x;
                    float dz = hole.cupZ - _ball.Position.y;
                    float angle = Mathf.Atan2(dz, dx) * Mathf.Rad2Deg;
                    if (angle < 0f) angle += 360f;
                    state.SetRegister(0, angle);
                    break;
                }
                case GolfOpCode.GET_PAR:
                    state.SetRegister(0, hole.par);
                    break;
                case GolfOpCode.GET_STROKES:
                    state.SetRegister(0, _match.StrokesThisHole);
                    break;
                case GolfOpCode.GET_TOTAL_STROKES:
                    state.SetRegister(0, _match.TotalStrokes);
                    break;
                case GolfOpCode.GET_HOLE_NUM:
                    state.SetRegister(0, _course.CurrentHoleIndex + 1);
                    break;
                case GolfOpCode.GET_TOTAL_HOLES:
                    state.SetRegister(0, _course.TotalHoles);
                    break;
                case GolfOpCode.GET_BALL_MOVING:
                    state.SetRegister(0, _ball.IsMoving ? 1f : 0f);
                    break;
                case GolfOpCode.GET_WIND_ANGLE:
                    state.SetRegister(0, hole.windAngle);
                    break;
                case GolfOpCode.GET_WIND_SPEED:
                    state.SetRegister(0, hole.windSpeed);
                    break;
                case GolfOpCode.GET_TERRAIN:
                    state.SetRegister(0, (float)_course.GetTerrain(_ball.Position.x, _ball.Position.y));
                    break;
                case GolfOpCode.GET_SLOPE_X:
                {
                    Vector2 slope = _course.GetSlope(_ball.Position.x, _ball.Position.y);
                    state.SetRegister(0, slope.x);
                    break;
                }
                case GolfOpCode.GET_SLOPE_Y:
                {
                    Vector2 slope = _course.GetSlope(_ball.Position.x, _ball.Position.y);
                    state.SetRegister(0, slope.y);
                    break;
                }
                case GolfOpCode.GET_INPUT:
                    state.SetRegister(0, GolfInputProvider.Instance?.CurrentInput ?? 0f);
                    break;
                case GolfOpCode.GET_SCORE:
                    state.SetRegister(0, _match.ScoreVsPar);
                    break;
                case GolfOpCode.GET_LAST_DIST:
                    state.SetRegister(0, _ball.LastPuttDistance);
                    break;

                // ── Commands ─────────────────────────────────────────
                case GolfOpCode.PUTT:
                {
                    float angle = state.GetRegister(0);
                    float power = state.GetRegister(1);
                    bool ok = _ball.Putt(angle, power);
                    if (ok) _match.RecordStroke();
                    state.SetRegister(0, ok ? 1f : 0f);
                    break;
                }
                case GolfOpCode.AIM:
                {
                    float angle = state.GetRegister(0);
                    _renderer?.SetAimAngle(angle);
                    state.SetRegister(0, 1f);
                    break;
                }

                // ── Two-arg queries ──────────────────────────────────
                case GolfOpCode.GET_TERRAIN_AT:
                {
                    float x = state.GetRegister(0);
                    float y = state.GetRegister(1);
                    state.SetRegister(0, (float)_course.GetTerrain(x, y));
                    break;
                }
                case GolfOpCode.GET_DIST_TO:
                {
                    float tx = state.GetRegister(0);
                    float ty = state.GetRegister(1);
                    float ddx = tx - _ball.Position.x;
                    float ddy = ty - _ball.Position.y;
                    state.SetRegister(0, Mathf.Sqrt(ddx * ddx + ddy * ddy));
                    break;
                }
            }
        }

        public float GetTimeScale() => SimulationTime.Instance?.timeScale ?? 1f;
        public double GetSimulationTime() => SimulationTime.Instance?.simulationTime ?? 0.0;
    }
}
