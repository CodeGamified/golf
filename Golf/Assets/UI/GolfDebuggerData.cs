// Copyright CodeGamified 2025-2026
// MIT License — Golf
using System.Collections.Generic;
using UnityEngine;
using CodeGamified.Engine;
using CodeGamified.Engine.Runtime;
using CodeGamified.TUI;
using Golf.Scripting;
using static Golf.Scripting.GolfOpCode;

namespace Golf.UI
{
    /// <summary>
    /// Adapts a GolfProgram into the engine's IDebuggerDataSource contract.
    /// Fed to DebuggerSourcePanel, DebuggerMachinePanel, DebuggerStatePanel.
    /// </summary>
    public class GolfDebuggerData : IDebuggerDataSource
    {
        private readonly GolfProgram _program;
        private readonly string _label;

        public GolfDebuggerData(GolfProgram program, string label = null)
        {
            _program = program;
            _label = label;
        }

        public string ProgramName => _label ?? _program?.ProgramName ?? "GolfAI";
        public string[] SourceLines => _program?.Program?.SourceLines;
        public bool HasLiveProgram =>
            _program != null && _program.Executor != null && _program.Program != null
            && _program.Program.Instructions != null && _program.Program.Instructions.Length > 0;
        public int PC
        {
            get
            {
                var s = _program?.State;
                if (s == null) return 0;
                return s.LastExecutedPC >= 0 ? s.LastExecutedPC : s.PC;
            }
        }
        public long CycleCount => _program?.State?.CycleCount ?? 0;

        public string StatusString
        {
            get
            {
                if (_program == null || _program.Executor == null)
                    return TUIColors.Dimmed("NO PROGRAM");
                var state = _program.State;
                if (state == null) return TUIColors.Dimmed("NO STATE");
                int instCount = _program.Program?.Instructions?.Length ?? 0;
                return TUIColors.Fg(TUIColors.BrightGreen, $"TICK {instCount} inst");
            }
        }

        public List<string> BuildSourceLines(int pc, int scrollOffset, int maxRows)
        {
            var lines = new List<string>();
            var src = SourceLines;
            if (src == null) return lines;

            int activeLine = -1;
            int activeEnd = -1;
            bool isHalt = false;
            Instruction activeInst = default;
            if (HasLiveProgram && _program.Program.Instructions.Length > 0
                && pc < _program.Program.Instructions.Length)
            {
                activeInst = _program.Program.Instructions[pc];
                activeLine = activeInst.SourceLine - 1;
                isHalt = activeInst.Op == OpCode.HALT;
                if (activeLine >= 0)
                    activeEnd = SourceHighlight.GetContinuationEnd(src, activeLine);
            }

            // Synthetic "while True:" at display row 0
            if (scrollOffset == 0 && lines.Count < maxRows)
            {
                string whileLine = "while True:";
                if (isHalt)
                    lines.Add(TUIColors.Fg(TUIColors.BrightGreen, $"  {TUIGlyphs.ArrowR}   {whileLine}"));
                else
                    lines.Add($"  {TUIColors.Dimmed(TUIGlyphs.ArrowR)}   {SynthwaveHighlighter.Highlight(whileLine)}");
            }

            int tokenLine = -1;
            if (activeLine >= 0)
            {
                string token = SourceHighlight.GetSourceToken(activeInst);
                if (token != null)
                {
                    for (int k = activeLine; k <= activeEnd; k++)
                    {
                        if (src[k].IndexOf(token) >= 0) { tokenLine = k; break; }
                    }
                }
                if (tokenLine < 0) tokenLine = activeLine;
            }

            int startIdx = scrollOffset > 0 ? scrollOffset - 1 : 0;
            for (int i = startIdx; i < src.Length && lines.Count < maxRows; i++)
            {
                bool isActive = (i >= activeLine && i <= activeEnd);
                bool isTokenLine = (i == tokenLine);

                string prefix;
                if (isTokenLine)
                    prefix = $"  {TUIColors.Fg(TUIColors.BrightGreen, TUIGlyphs.ArrowR)}   ";
                else if (isActive)
                    prefix = $"  {TUIColors.Dimmed(TUIGlyphs.ArrowR)}   ";
                else
                    prefix = "      ";

                string line = SynthwaveHighlighter.Highlight("    " + src[i]);
                if (isTokenLine)
                {
                    string token = SourceHighlight.GetSourceToken(activeInst);
                    if (token != null)
                        line = SourceHighlight.HighlightToken(line, token, TUIColors.BrightGreen);
                }
                lines.Add(prefix + line);
            }

            return lines;
        }

        public List<string> BuildMachineLines(int pc, int maxRows)
        {
            var lines = new List<string>();
            if (!HasLiveProgram) return lines;

            var insts = _program.Program.Instructions;
            int start = Mathf.Max(0, pc - maxRows / 2);
            for (int i = start; i < insts.Length && lines.Count < maxRows; i++)
            {
                bool isCurrent = (i == pc);
                string prefix = isCurrent
                    ? TUIColors.Fg(TUIColors.BrightGreen, $" {TUIGlyphs.ArrowR} ")
                    : "   ";
                string disasm = insts[i].ToString();
                if (isCurrent)
                    disasm = TUIColors.Bold(disasm);
                lines.Add($"{prefix}{i,4}: {disasm}");
            }
            return lines;
        }

        public List<string> BuildStateLines()
        {
            var lines = new List<string>();
            var state = _program?.State;
            if (state == null) return lines;

            lines.Add($" PC   {TUIColors.Fg(TUIColors.BrightCyan, state.PC.ToString())}");
            lines.Add($" Cyc  {state.CycleCount}");
            lines.Add($" Halt {(state.IsHalted ? TUIColors.Fg(TUIColors.Red, "YES") : "no")}");
            lines.Add("");

            int regCount = state.RegisterCount;
            for (int r = 0; r < regCount; r++)
            {
                float val = state.GetRegister(r);
                lines.Add($" R{r}   {val:F2}");
            }

            return lines;
        }
    }
}
