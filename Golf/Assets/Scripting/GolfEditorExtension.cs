// Copyright CodeGamified 2025-2026
// MIT License — Golf
using System.Collections.Generic;
using CodeGamified.Editor;

namespace Golf.Scripting
{
    /// <summary>
    /// Tap-to-code editor metadata for Golf builtins.
    /// </summary>
    public class GolfEditorExtension : IEditorExtension
    {
        public List<EditorTypeInfo> GetAvailableTypes() => new();

        public List<EditorFuncInfo> GetAvailableFunctions() => new()
        {
            // Queries — zero-arg
            new() { Name = "get_ball_x", Hint = "Ball X position", ArgCount = 0 },
            new() { Name = "get_ball_y", Hint = "Ball Y position", ArgCount = 0 },
            new() { Name = "get_hole_x", Hint = "Cup X position", ArgCount = 0 },
            new() { Name = "get_hole_y", Hint = "Cup Y position", ArgCount = 0 },
            new() { Name = "get_dist", Hint = "Distance from ball to cup", ArgCount = 0 },
            new() { Name = "get_angle", Hint = "Angle from ball to cup (degrees)", ArgCount = 0 },
            new() { Name = "get_par", Hint = "Par for current hole", ArgCount = 0 },
            new() { Name = "get_strokes", Hint = "Strokes taken this hole", ArgCount = 0 },
            new() { Name = "get_total_strokes", Hint = "Total strokes all holes", ArgCount = 0 },
            new() { Name = "get_hole_num", Hint = "Current hole number (1-9)", ArgCount = 0 },
            new() { Name = "get_total_holes", Hint = "Total holes in course", ArgCount = 0 },
            new() { Name = "get_ball_moving", Hint = "1 if ball rolling, 0 if stopped", ArgCount = 0 },
            new() { Name = "get_wind_angle", Hint = "Wind direction (degrees)", ArgCount = 0 },
            new() { Name = "get_wind_speed", Hint = "Wind speed", ArgCount = 0 },
            new() { Name = "get_terrain", Hint = "Terrain under ball (0=green,1=fairway,2=rough,3=sand,4=water)", ArgCount = 0 },
            new() { Name = "get_slope_x", Hint = "Slope X at ball position", ArgCount = 0 },
            new() { Name = "get_slope_y", Hint = "Slope Y at ball position", ArgCount = 0 },
            new() { Name = "get_input", Hint = "Manual input value", ArgCount = 0 },
            new() { Name = "get_score", Hint = "Score vs par (negative = under par)", ArgCount = 0 },
            new() { Name = "get_last_dist", Hint = "Distance of last putt", ArgCount = 0 },

            // Commands
            new() { Name = "putt", Hint = "Putt at angle (deg) with power (0-100)", ArgCount = 2 },
            new() { Name = "aim", Hint = "Set aim line angle (visual only)", ArgCount = 1 },

            // Two-arg queries
            new() { Name = "get_terrain_at", Hint = "Terrain type at (x, y)", ArgCount = 2 },
            new() { Name = "get_dist_to", Hint = "Distance from ball to (x, y)", ArgCount = 2 },
        };

        public List<EditorMethodInfo> GetMethodsForType(string t) => new();

        public List<string> GetVariableNameSuggestions() => new()
        {
            "angle", "power", "dist", "moving", "terrain",
            "wind", "slope_x", "slope_y", "par", "strokes",
            "hole_x", "hole_y", "ball_x", "ball_y"
        };

        public List<string> GetStringLiteralSuggestions() => new();
    }
}
