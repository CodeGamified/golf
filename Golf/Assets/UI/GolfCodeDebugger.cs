// Copyright CodeGamified 2025-2026
// MIT License — Golf
using CodeGamified.TUI;
using Golf.Scripting;

namespace Golf.UI
{
    /// <summary>
    /// Thin adapter — wires a GolfProgram into the engine's CodeDebuggerWindow
    /// via GolfDebuggerData (IDebuggerDataSource). All rendering lives in the engine.
    /// </summary>
    public class GolfCodeDebugger : CodeDebuggerWindow
    {
        protected override void Awake()
        {
            base.Awake();
            windowTitle = "CODE";
        }

        public void Bind(GolfProgram program)
        {
            SetDataSource(new GolfDebuggerData(program));
        }
    }
}
