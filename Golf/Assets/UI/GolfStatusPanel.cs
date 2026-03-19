// Copyright CodeGamified 2025-2026
// MIT License — Golf
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CodeGamified.Audio;
using CodeGamified.TUI;
using CodeGamified.Time;
using CodeGamified.Settings;
using CodeGamified.Quality;
using UnityEngine.SceneManagement;
using Golf.Game;
using Golf.Scripting;

namespace Golf.UI
{
    /// <summary>
    /// Right-side status panel — 6 vertical sections with draggable row dividers:
    ///   TITLE  ─┬─  MATCH/SCORE  ─┬─  SCRIPT  ─┬─  SETTINGS  ─┬─  CONTROLS  ─┬─  AUDIO
    /// </summary>
    public class GolfStatusPanel : TerminalWindow
    {
        // ── Dependencies ────────────────────────────────────────
        private GolfMatchManager _match;
        private GolfProgram _playerProgram;
        private Equalizer _equalizer;

        // ── Section layout (6 sections, 5 row draggers) ─────────
        private const int SECTION_COUNT = 6;
        private const int SEC_TITLE    = 0;
        private const int SEC_MATCH    = 1;
        private const int SEC_SCRIPT   = 2;
        private const int SEC_SETTINGS = 3;
        private const int SEC_CONTROLS = 4;
        private const int SEC_AUDIO    = 5;

        private int[] _sectionRows;
        private float[] _sectionRatios = { 0f, 0.12f, 0.28f, 0.48f, 0.65f, 0.82f };
        private TUIRowDragger[] _rowDraggers;
        private bool _sectionsReady;

        // ── Overlay bindings ────────────────────────────────────
        private TUIOverlayBinding _overlays;

        // ── ASCII art animation ─────────────────────────────────
        private float _asciiTimer;
        private int _asciiPhase;
        private float[] _revealThresholds;
        private const float AsciiHold = 5f;
        private const float AsciiAnim = 1f;
        private const int AsciiWordCount = 2;
        private static readonly char[] GlitchGlyphs =
            "░▒▓█▀▄▌▐╬╫╪╩╦╠╣─│┌┐└┘├┤┬┴┼".ToCharArray();

        private static readonly string[][] AsciiWords =
        {
            new[] // MINI
            {
                "  ██     ██ ██ ██    ██ ██       ",
                "  ███   ███ ██ ███   ██ ██       ",
                "  ██ █ █ ██ ██ ██ █  ██ ██       ",
                "  ██  █  ██ ██ ██  █ ██ ██       ",
                "  ██     ██ ██ ██   ███ ██       ",
            },
            new[] // GOLF
            {
                "   ██████  ████████  ██       ████████ ",
                "  ██       ██    ██  ██       ██       ",
                "  ██  ███  ██    ██  ██       ██████   ",
                "  ██   ██  ██    ██  ██       ██       ",
                "   ██████   ████████ ████████ ██       ",
            },
        };

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        protected override void Awake()
        {
            base.Awake();
            windowTitle = "MINI GOLF";
            totalRows = 40;
        }

        public void Bind(GolfMatchManager match, GolfProgram playerProgram)
        {
            _match = match;
            _playerProgram = playerProgram;
        }

        public void BindEqualizer(Equalizer equalizer) => _equalizer = equalizer;

        protected override void OnLayoutReady()
        {
            SetupSections();
        }

        protected override void Update()
        {
            base.Update();
            if (!rowsReady) return;
            _equalizer?.Update(UnityEngine.Time.deltaTime);
            AdvanceAsciiTimer();
            HandleInput();
        }

        // ═══════════════════════════════════════════════════════════════
        // SECTION LAYOUT
        // ═══════════════════════════════════════════════════════════════

        private void SetupSections()
        {
            ComputeSectionRows();
            _sectionsReady = true;

            if (_rowDraggers == null)
            {
                _rowDraggers = new TUIRowDragger[SECTION_COUNT - 1];
                for (int i = 0; i < SECTION_COUNT - 1; i++)
                {
                    int idx = i;
                    int minRow = (i > 0 ? _sectionRows[i] : 1) + 1;
                    int maxRow = (i + 2 < SECTION_COUNT ? _sectionRows[i + 2] : totalRows) - 1;
                    _rowDraggers[i] = AddRowDragger(
                        _sectionRows[i + 1], minRow, maxRow, pos => OnRowDragged(idx, pos));
                }
            }
            else
            {
                float rh = rows.Count > 0 ? rows[0].RowHeight : 18f;
                for (int i = 0; i < SECTION_COUNT - 1; i++)
                {
                    _rowDraggers[i].UpdateRowHeight(rh);
                    _rowDraggers[i].UpdatePosition(_sectionRows[i + 1]);
                    UpdateDraggerLimits(i);
                }
            }

            BuildAndApplyOverlays();
        }

        private void ComputeSectionRows()
        {
            _sectionRows = new int[SECTION_COUNT];
            _sectionRows[0] = 0;
            for (int i = 1; i < SECTION_COUNT; i++)
            {
                int minRow = _sectionRows[i - 1] + 1;
                int maxRow = totalRows - (SECTION_COUNT - i);
                _sectionRows[i] = Mathf.Clamp(
                    Mathf.RoundToInt(totalRows * _sectionRatios[i]), minRow, maxRow);
            }
        }

        private void OnRowDragged(int draggerIndex, int newRow)
        {
            int secIdx = draggerIndex + 1;
            _sectionRows[secIdx] = newRow;
            _sectionRatios[secIdx] = (float)newRow / totalRows;

            if (draggerIndex > 0) UpdateDraggerLimits(draggerIndex - 1);
            if (draggerIndex < SECTION_COUNT - 2) UpdateDraggerLimits(draggerIndex + 1);

            if (_overlays != null)
                _overlays.Apply(rows, null, totalChars);
        }

        private void UpdateDraggerLimits(int draggerIdx)
        {
            int minRow = _sectionRows[draggerIdx] + 1;
            int maxRow = (draggerIdx + 2 < SECTION_COUNT ? _sectionRows[draggerIdx + 2] : totalRows) - 1;
            _rowDraggers[draggerIdx].UpdateLimits(minRow, maxRow);
        }

        private int SectionStart(int sec) => _sectionRows[sec];
        private int SectionEnd(int sec) => sec + 1 < SECTION_COUNT ? _sectionRows[sec + 1] : totalRows;
        private int SectionHeight(int sec) => SectionEnd(sec) - SectionStart(sec);

        // ═══════════════════════════════════════════════════════════════
        // INPUT
        // ═══════════════════════════════════════════════════════════════

        private void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.P))
                SimulationTime.Instance?.TogglePause();

            if (Input.GetKeyDown(KeyCode.R))
                ReloadScene();

            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (Input.GetKeyDown(KeyCode.F5))
                SettingsBridge.SetMasterVolume(SettingsBridge.MasterVolume + (shift ? -0.1f : 0.1f));
            if (Input.GetKeyDown(KeyCode.F6))
                SettingsBridge.SetMusicVolume(SettingsBridge.MusicVolume + (shift ? -0.1f : 0.1f));
            if (Input.GetKeyDown(KeyCode.F7))
                SettingsBridge.SetSfxVolume(SettingsBridge.SfxVolume + (shift ? -0.1f : 0.1f));
        }

        private void ReloadScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // ═══════════════════════════════════════════════════════════════
        // OVERLAY BINDINGS
        // ═══════════════════════════════════════════════════════════════

        private void BuildAndApplyOverlays()
        {
            if (_overlays == null)
            {
                _overlays = new TUIOverlayBinding();

                // ── Settings sliders ──
                int settingsBase = SectionStart(SEC_SETTINGS);
                _overlays.Slider(settingsBase + 1, -1,
                    () => SettingsBridge.QualityLevel / 3f,
                    v => { int lv = Mathf.RoundToInt(v * 3f); SettingsBridge.SetQualityLevel(lv); QualityBridge.SetTier((QualityTier)lv); },
                    step: 1f / 3f);
                _overlays.Slider(settingsBase + 2, -1,
                    () => FontToSlider(SettingsBridge.FontSize),
                    v => SettingsBridge.SetFontSize(SliderToFont(v)),
                    step: 1f / 40f);

                // ── Controls sliders ──
                int controlsBase = SectionStart(SEC_CONTROLS);
                _overlays.Slider(controlsBase + 1, -1,
                    () => SpeedToSlider(SimulationTime.Instance != null ? SimulationTime.Instance.timeScale : 1f),
                    v => SimulationTime.Instance?.SetTimeScale(SliderToSpeed(v)));

                // ── Audio sliders ──
                int audioBase = SectionStart(SEC_AUDIO);
                _overlays.Slider(audioBase + 1, -1,
                    () => SettingsBridge.MasterVolume,
                    v => SettingsBridge.SetMasterVolume(v));
                _overlays.Slider(audioBase + 2, -1,
                    () => SettingsBridge.MusicVolume,
                    v => SettingsBridge.SetMusicVolume(v));
                _overlays.Slider(audioBase + 3, -1,
                    () => SettingsBridge.SfxVolume,
                    v => SettingsBridge.SetSfxVolume(v));
            }

            _overlays.Apply(rows, null, totalChars);
        }

        private static float SpeedToSlider(float speed)
        {
            speed = Mathf.Clamp(speed, 0.1f, 100f);
            return Mathf.Log10(speed * 10f) / 3f;
        }

        private static float SliderToSpeed(float slider)
        {
            return 0.1f * Mathf.Pow(1000f, Mathf.Clamp01(slider));
        }

        private static float FontToSlider(float fontSize)
        {
            return Mathf.Clamp01((fontSize - 8f) / 40f);
        }

        private static float SliderToFont(float slider)
        {
            return 8f + Mathf.Clamp01(slider) * 40f;
        }

        // ═══════════════════════════════════════════════════════════════
        // RENDER
        // ═══════════════════════════════════════════════════════════════

        protected override void Render()
        {
            ClearAllRows();

            if (!_sectionsReady)
            {
                SetRow(0, $" {TUIColors.Bold("MINI GOLF")}");
                return;
            }

            _overlays?.Sync();

            RenderSection(SEC_TITLE,    BuildTitleSection);
            RenderSection(SEC_MATCH,    BuildMatchSection);
            RenderSection(SEC_SCRIPT,   BuildScriptSection);
            RenderSection(SEC_SETTINGS, BuildSettingsSection);
            RenderSection(SEC_CONTROLS, BuildControlsSection);
            RenderSection(SEC_AUDIO,    BuildAudioSection);

            for (int i = 1; i < SECTION_COUNT; i++)
            {
                int r = _sectionRows[i];
                if (r > 0 && r < totalRows)
                    SetRow(r, Separator());
            }
        }

        private void RenderSection(int sec, Func<int, string[]> builder)
        {
            int start = SectionStart(sec);
            int end = SectionEnd(sec);
            int contentStart = sec > 0 ? start + 1 : start;
            int contentHeight = end - contentStart;
            if (contentHeight <= 0) return;

            var lines = builder(contentHeight);
            for (int i = 0; i < lines.Length && i < contentHeight; i++)
                SetRow(contentStart + i, lines[i]);
        }

        // ── Section 0: TITLE ────────────────────────────────────

        private string[] BuildTitleSection(int maxRows)
        {
            var art = BuildAsciiArt(totalChars);
            int artWidth = art.Length > 0 ? TUIText.VisibleLength(art[0]) : 0;
            int pad = Mathf.Max(0, (totalChars - artWidth) / 2);
            if (pad > 0)
            {
                string spaces = new string(' ', pad);
                for (int i = 0; i < art.Length; i++)
                    if (!string.IsNullOrEmpty(art[i]))
                        art[i] = spaces + art[i];
            }

            if (art.Length > maxRows)
            {
                var trimmed = new string[maxRows];
                System.Array.Copy(art, trimmed, maxRows);
                return trimmed;
            }
            return art;
        }

        // ── Section 1: MATCH / SCORE ────────────────────────────

        private string[] BuildMatchSection(int maxRows)
        {
            var lines = new List<string>();

            Color32 accent = TUIGradient.Sample(0.3f);
            lines.Add($" {TUIColors.Fg(accent, TUIGlyphs.DiamondFilled)} {TUIColors.Bold("MATCH")}");

            if (_match != null)
            {
                string emdash = "\u2014";
                lines.Add($"  Hole {TUIColors.Fg(TUIColors.BrightCyan, $"{_match.CurrentHoleIndex + 1}")} / {_match.TotalHoles}");
                lines.Add($"  {TUIColors.Fg(TUIColors.BrightYellow, $"{_match.StrokesThisHole}")} {emdash} STROKES (this hole)");
                lines.Add($"  {TUIColors.Fg(TUIColors.BrightGreen, $"{_match.TotalStrokes}")} {emdash} TOTAL STROKES");

                string scoreStr = _match.ScoreVsPar <= 0
                    ? TUIColors.Fg(TUIColors.BrightGreen, $"{_match.ScoreVsPar:+#;-#;E}")
                    : TUIColors.Fg(TUIColors.Red, $"+{_match.ScoreVsPar}");
                lines.Add($"  {scoreStr} {emdash} VS PAR");

                if (_match.IsHoleComplete)
                {
                    int par = 3; // fallback
                    if (_match.StrokesPerHole != null && _match.CurrentHoleIndex < _match.StrokesPerHole.Length)
                        par = _match.StrokesPerHole[_match.CurrentHoleIndex];
                    lines.Add($"  {TUIColors.Fg(TUIColors.BrightMagenta, "HOLE COMPLETE")}");
                }
                if (_match.IsRoundComplete)
                    lines.Add($"  {TUIColors.Fg(TUIColors.BrightGreen, "ROUND COMPLETE!")}");
            }
            else
            {
                lines.Add(TUIColors.Dimmed("  No match"));
            }

            return Trim(lines, maxRows);
        }

        // ── Section 2: SCRIPT CONTROLS / LOADER ─────────────────

        private string[] BuildScriptSection(int maxRows)
        {
            var lines = new List<string>();

            Color32 accent = TUIGradient.Sample(0.5f);
            lines.Add($" {TUIColors.Fg(accent, TUIGlyphs.DiamondFilled)} {TUIColors.Bold("SCRIPT")}");

            if (_playerProgram != null)
            {
                int inst = _playerProgram.Program?.Instructions?.Length ?? 0;
                string status = _playerProgram.IsRunning
                    ? TUIColors.Fg(TUIColors.BrightGreen, "RUN")
                    : TUIColors.Dimmed("STP");
                lines.Add($"  {status}  {inst} instructions");
                lines.Add($"  Cycle {_playerProgram.State?.CycleCount ?? 0}");
            }
            else
            {
                lines.Add(TUIColors.Dimmed("  No program"));
            }

            return Trim(lines, maxRows);
        }

        // ── Section 3: SETTINGS ─────────────────────────────────

        private string[] BuildSettingsSection(int maxRows)
        {
            var lines = new List<string>();
            Color32 accent = TUIGradient.Sample(0.6f);
            lines.Add($" {TUIColors.Fg(accent, TUIGlyphs.DiamondFilled)} {TUIColors.Bold("SETTINGS")}");
            lines.Add($"  Quality  {QualityName(SettingsBridge.QualityLevel)}");
            lines.Add($"  Font     {SettingsBridge.FontSize:F0}pt");
            return Trim(lines, maxRows);
        }

        private static string QualityName(int level) => level switch
        {
            0 => "Low",
            1 => "Medium",
            2 => "High",
            3 => "Ultra",
            _ => level.ToString()
        };

        // ── Section 4: CONTROLS ─────────────────────────────────

        private string[] BuildControlsSection(int maxRows)
        {
            var lines = new List<string>();
            Color32 accent = TUIGradient.Sample(0.7f);
            lines.Add($" {TUIColors.Fg(accent, TUIGlyphs.DiamondFilled)} {TUIColors.Bold("CONTROLS")}");

            float speed = SimulationTime.Instance?.timeScale ?? 1f;
            lines.Add($"  Speed    {speed:F1}x");
            lines.Add($"  [P] Pause   [R] Reload");
            return Trim(lines, maxRows);
        }

        // ── Section 5: AUDIO ────────────────────────────────────

        private string[] BuildAudioSection(int maxRows)
        {
            var lines = new List<string>();
            Color32 accent = TUIGradient.Sample(0.85f);
            lines.Add($" {TUIColors.Fg(accent, TUIGlyphs.DiamondFilled)} {TUIColors.Bold("AUDIO")}");
            lines.Add($"  Master {SettingsBridge.MasterVolume:P0}");
            lines.Add($"  Music  {SettingsBridge.MusicVolume:P0}");
            lines.Add($"  SFX    {SettingsBridge.SfxVolume:P0}");
            return Trim(lines, maxRows);
        }

        // ═══════════════════════════════════════════════════════════════
        // ASCII ART ANIMATION
        // ═══════════════════════════════════════════════════════════════

        private void AdvanceAsciiTimer()
        {
            float dt = UnityEngine.Time.deltaTime;
            _asciiTimer += dt;
            float totalCycle = (AsciiHold + AsciiAnim) * AsciiWordCount;
            if (_asciiTimer >= totalCycle)
            {
                _asciiTimer -= totalCycle;
                if (_revealThresholds != null)
                    for (int i = 0; i < _revealThresholds.Length; i++)
                        _revealThresholds[i] = UnityEngine.Random.value;
            }
        }

        private string[] BuildAsciiArt(int maxWidth)
        {
            float totalCycle = (AsciiHold + AsciiAnim) * AsciiWordCount;
            float t = _asciiTimer % totalCycle;
            int wordIdx = Mathf.Clamp((int)(t / (AsciiHold + AsciiAnim)), 0, AsciiWordCount - 1);
            float wordT = t - wordIdx * (AsciiHold + AsciiAnim);
            float reveal = wordT < AsciiAnim ? wordT / AsciiAnim : 1f;

            var art = AsciiWords[wordIdx];
            int charCount = 0;
            foreach (var line in art) charCount += line.Length;

            if (_revealThresholds == null || _revealThresholds.Length != charCount)
            {
                _revealThresholds = new float[charCount];
                for (int i = 0; i < charCount; i++)
                    _revealThresholds[i] = UnityEngine.Random.value;
            }

            var result = new string[art.Length];
            int ci = 0;
            for (int r = 0; r < art.Length; r++)
            {
                var chars = new char[art[r].Length];
                for (int c = 0; c < art[r].Length; c++)
                {
                    char src = art[r][c];
                    if (src == ' ')
                    {
                        chars[c] = ' ';
                    }
                    else if (reveal >= 1f || _revealThresholds[ci] <= reveal)
                    {
                        chars[c] = src;
                    }
                    else
                    {
                        chars[c] = GlitchGlyphs[UnityEngine.Random.Range(0, GlitchGlyphs.Length)];
                    }
                    ci++;
                }
                float hue = (float)r / art.Length;
                Color32 color = TUIGradient.Sample(hue);
                result[r] = TUIColors.Fg(color, new string(chars));
            }
            return result;
        }

        // ═══════════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════════

        private static string[] Trim(List<string> lines, int max)
        {
            if (lines.Count <= max) return lines.ToArray();
            var result = new string[max];
            for (int i = 0; i < max; i++) result[i] = lines[i];
            return result;
        }
    }
}
