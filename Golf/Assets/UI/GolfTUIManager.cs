// Copyright CodeGamified 2025-2026
// MIT License — Golf
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CodeGamified.TUI;
using CodeGamified.Settings;
using CodeGamified.Audio;
using Golf.Game;
using Golf.Scripting;

namespace Golf.UI
{
    /// <summary>
    /// TUI Manager for Golf — left debugger panel + right status panel.
    ///
    /// Layout:
    ///   ┌────────────────────────────┐                ┌────────────────────────────┐
    ///   │ YOUR CODE                  │                │ TITLE                      │
    ///   │ SOURCE ┆ MACHINE ┆ STATE   │   GAME VIEW    ├────────────────────────────┤
    ///   │                            │   (34% open)   │ MATCH / SCORE              │
    ///   │                            │                ├────────────────────────────┤
    ///   │                            │                │ SCRIPT CONTROLS            │
    ///   │                            │                ├────────────────────────────┤
    ///   │                            │                │ SETTINGS                   │
    ///   │                            │                ├────────────────────────────┤
    ///   │                            │                │ CONTROLS                   │
    ///   │                            │                ├────────────────────────────┤
    ///   │                            │                │ AUDIO                      │
    ///   └────────────────────────────┘                └────────────────────────────┘
    /// </summary>
    public class GolfTUIManager : MonoBehaviour, ISettingsListener
    {
        // Dependencies
        private GolfMatchManager _match;
        private GolfProgram _playerProgram;
        private Equalizer _equalizer;

        // Canvas
        private Canvas _canvas;
        private RectTransform _canvasRect;

        // Panels
        private GolfCodeDebugger _debugger;
        private RectTransform _debuggerRect;

        private GolfStatusPanel _statusPanel;
        private RectTransform _statusPanelRect;

        // Edge draggers
        private TUIEdgeDragger _debuggerRightEdge;
        private TUIEdgeDragger _statusLeftEdge;

        // Font
        private TMP_FontAsset _font;
        private float _fontSize;

        // All panel rects for bulk cleanup
        private RectTransform[] _allPanelRects;

        public void Initialize(GolfMatchManager match, GolfProgram program,
                               Equalizer equalizer = null)
        {
            _match = match;
            _playerProgram = program;
            _equalizer = equalizer;
            _fontSize = SettingsBridge.FontSize;

            BuildCanvas();
            BuildPanels();
        }

        private void OnEnable()  => SettingsBridge.Register(this);
        private void OnDisable() => SettingsBridge.Unregister(this);

        public void OnSettingsChanged(SettingsSnapshot settings, SettingsCategory changed)
        {
            if (changed != SettingsCategory.Display) return;
            if (Mathf.Approximately(settings.FontSize, _fontSize)) return;

            _fontSize = settings.FontSize;
            RebuildPanels();
        }

        private void RebuildPanels()
        {
            if (_allPanelRects != null)
                foreach (var rt in _allPanelRects)
                    if (rt != null) Destroy(rt.gameObject);

            _debugger = null;
            _statusPanel = null;

            BuildPanels();
        }

        // ═══════════════════════════════════════════════════════════════
        // CANVAS
        // ═══════════════════════════════════════════════════════════════

        private void BuildCanvas()
        {
            var canvasGO = new GameObject("GolfTUI_Canvas");
            canvasGO.transform.SetParent(transform, false);

            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceCamera;
            _canvas.worldCamera = Camera.main;
            _canvas.sortingOrder = 100;
            _canvas.planeDistance = 1f;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();
            _canvasRect = canvasGO.GetComponent<RectTransform>();

            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esGO = new GameObject("EventSystem");
                esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // PANELS
        // ═══════════════════════════════════════════════════════════════

        private void BuildPanels()
        {
            const float dLeft  = 0f;
            const float dRight = 0.33f;
            const float sLeft  = 0.67f;
            const float sRight = 1.0f;

            // ── Code debugger (left 33%, 3 horizontal columns) ──
            _debuggerRect = CreatePanel("Debugger",
                new Vector2(dLeft, 0f), new Vector2(dRight, 1f));
            _debugger = _debuggerRect.gameObject.AddComponent<GolfCodeDebugger>();
            AddPanelBackground(_debuggerRect);
            _debugger.InitializeProgrammatic(GetFont(), _fontSize,
                _debuggerRect.GetComponent<Image>());
            _debugger.SetTitle("YOUR CODE");
            _debugger.Bind(_playerProgram);

            // ── Status panel (right 33%, 6 vertical sections) ──
            _statusPanelRect = CreatePanel("StatusPanel",
                new Vector2(sLeft, 0f), new Vector2(sRight, 1f));
            _statusPanel = _statusPanelRect.gameObject.AddComponent<GolfStatusPanel>();
            AddPanelBackground(_statusPanelRect);
            _statusPanel.InitializeProgrammatic(GetFont(), _fontSize - 1f,
                _statusPanelRect.GetComponent<Image>());
            _statusPanel.Bind(_match, _playerProgram);
            if (_equalizer != null)
                _statusPanel.BindEqualizer(_equalizer);

            // Track for teardown
            _allPanelRects = new[] { _debuggerRect, _statusPanelRect };

            LinkEdges();
        }

        // ═══════════════════════════════════════════════════════════════
        // EDGE LINKING
        // ═══════════════════════════════════════════════════════════════

        private void LinkEdges()
        {
            _debuggerRightEdge = TUIEdgeDragger.Create(
                _debuggerRect, _canvasRect, TUIEdgeDragger.Edge.Right);
            _statusLeftEdge = TUIEdgeDragger.Create(
                _statusPanelRect, _canvasRect, TUIEdgeDragger.Edge.Left);
        }

        // ═══════════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════════

        private RectTransform CreatePanel(string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_canvasRect, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            return rt;
        }

        private void AddPanelBackground(RectTransform panel)
        {
            var img = panel.gameObject.GetComponent<Image>();
            if (img == null)
                img = panel.gameObject.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.5f);
            img.raycastTarget = true;
        }

        private TMP_FontAsset GetFont()
        {
            if (_font != null) return _font;
            _font = Resources.Load<TMP_FontAsset>("Unifont SDF");
            return _font;
        }
    }
}
