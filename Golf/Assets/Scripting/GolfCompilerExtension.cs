// Copyright CodeGamified 2025-2026
// MIT License — Golf
using System.Collections.Generic;
using CodeGamified.Engine;
using CodeGamified.Engine.Compiler;

namespace Golf.Scripting
{
    /// <summary>
    /// Compiler extension: maps Python function names → CUSTOM opcodes for Golf.
    /// </summary>
    public class GolfCompilerExtension : ICompilerExtension
    {
        public void RegisterBuiltins(CompilerContext ctx) { }

        public bool TryCompileCall(string fn, List<AstNodes.ExprNode> args,
                                   CompilerContext ctx, int line)
        {
            switch (fn)
            {
                // ── Zero-arg queries ───────────────────────────────
                case "get_ball_x":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)GolfOpCode.GET_BALL_X, 0, 0, 0, line, "get_ball_x → R0");
                    return true;
                case "get_ball_y":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)GolfOpCode.GET_BALL_Y, 0, 0, 0, line, "get_ball_y → R0");
                    return true;
                case "get_hole_x":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)GolfOpCode.GET_HOLE_X, 0, 0, 0, line, "get_hole_x → R0");
                    return true;
                case "get_hole_y":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)GolfOpCode.GET_HOLE_Y, 0, 0, 0, line, "get_hole_y → R0");
                    return true;
                case "get_dist":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)GolfOpCode.GET_DIST, 0, 0, 0, line, "get_dist → R0");
                    return true;
                case "get_angle":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)GolfOpCode.GET_ANGLE, 0, 0, 0, line, "get_angle → R0");
                    return true;
                case "get_par":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)GolfOpCode.GET_PAR, 0, 0, 0, line, "get_par → R0");
                    return true;
                case "get_strokes":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)GolfOpCode.GET_STROKES, 0, 0, 0, line, "get_strokes → R0");
                    return true;
                case "get_total_strokes":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)GolfOpCode.GET_TOTAL_STROKES, 0, 0, 0, line, "get_total_strokes → R0");
                    return true;
                case "get_hole_num":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)GolfOpCode.GET_HOLE_NUM, 0, 0, 0, line, "get_hole_num → R0");
                    return true;
                case "get_total_holes":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)GolfOpCode.GET_TOTAL_HOLES, 0, 0, 0, line, "get_total_holes → R0");
                    return true;
                case "get_ball_moving":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)GolfOpCode.GET_BALL_MOVING, 0, 0, 0, line, "get_ball_moving → R0");
                    return true;
                case "get_wind_angle":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)GolfOpCode.GET_WIND_ANGLE, 0, 0, 0, line, "get_wind_angle → R0");
                    return true;
                case "get_wind_speed":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)GolfOpCode.GET_WIND_SPEED, 0, 0, 0, line, "get_wind_speed → R0");
                    return true;
                case "get_terrain":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)GolfOpCode.GET_TERRAIN, 0, 0, 0, line, "get_terrain → R0");
                    return true;
                case "get_slope_x":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)GolfOpCode.GET_SLOPE_X, 0, 0, 0, line, "get_slope_x → R0");
                    return true;
                case "get_slope_y":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)GolfOpCode.GET_SLOPE_Y, 0, 0, 0, line, "get_slope_y → R0");
                    return true;
                case "get_input":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)GolfOpCode.GET_INPUT, 0, 0, 0, line, "get_input → R0");
                    return true;
                case "get_score":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)GolfOpCode.GET_SCORE, 0, 0, 0, line, "get_score → R0");
                    return true;
                case "get_last_dist":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)GolfOpCode.GET_LAST_DIST, 0, 0, 0, line, "get_last_dist → R0");
                    return true;

                // ── Two-arg command: putt(angle, power) ────────────
                case "putt":
                    if (args != null && args.Count >= 2)
                    {
                        args[0].Compile(ctx);               // angle → R0
                        ctx.Emit(OpCode.PUSH, 0);           // save angle
                        args[1].Compile(ctx);               // power → R0
                        ctx.Emit(OpCode.MOV, 1, 0);         // R0 → R1 (power)
                        ctx.Emit(OpCode.POP, 0);            // restore angle → R0
                    }
                    ctx.Emit(OpCode.CUSTOM_0 + (int)GolfOpCode.PUTT, 0, 0, 0, line, "putt(R0=angle,R1=power)");
                    return true;

                // ── One-arg command: aim(angle) ────────────────────
                case "aim":
                    if (args != null && args.Count > 0)
                        args[0].Compile(ctx);               // angle → R0
                    ctx.Emit(OpCode.CUSTOM_0 + (int)GolfOpCode.AIM, 0, 0, 0, line, "aim(R0=angle)");
                    return true;

                // ── Two-arg queries ────────────────────────────────
                case "get_terrain_at":
                    if (args != null && args.Count >= 2)
                    {
                        args[0].Compile(ctx);               // x → R0
                        ctx.Emit(OpCode.PUSH, 0);           // save x
                        args[1].Compile(ctx);               // y → R0
                        ctx.Emit(OpCode.MOV, 1, 0);         // R0 → R1 (y)
                        ctx.Emit(OpCode.POP, 0);            // restore x → R0
                    }
                    ctx.Emit(OpCode.CUSTOM_0 + (int)GolfOpCode.GET_TERRAIN_AT, 0, 0, 0, line, "get_terrain_at(R0,R1) → R0");
                    return true;

                case "get_dist_to":
                    if (args != null && args.Count >= 2)
                    {
                        args[0].Compile(ctx);
                        ctx.Emit(OpCode.PUSH, 0);
                        args[1].Compile(ctx);
                        ctx.Emit(OpCode.MOV, 1, 0);
                        ctx.Emit(OpCode.POP, 0);
                    }
                    ctx.Emit(OpCode.CUSTOM_0 + (int)GolfOpCode.GET_DIST_TO, 0, 0, 0, line, "get_dist_to(R0,R1) → R0");
                    return true;

                default:
                    return false;
            }
        }

        public bool TryCompileMethodCall(string obj, string method, List<AstNodes.ExprNode> args,
                                         CompilerContext ctx, int line) => false;

        public bool TryCompileObjectDecl(string type, string var, List<AstNodes.ExprNode> args,
                                         CompilerContext ctx, int line) => false;
    }
}
