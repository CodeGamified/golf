// Copyright CodeGamified 2025-2026
// MIT License — Golf
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeGamified.Quality;

namespace Golf.Game
{
    /// <summary>
    /// 3D renderer for the golf course. Builds primitive geometry for the green,
    /// cup, ball, walls, terrain patches, and aim indicator.
    /// Rebuilds each time the hole changes.
    /// Glow system: ball point light + HDR emission flashing (same pattern as Pool).
    /// </summary>
    public class GolfRenderer : MonoBehaviour, IQualityResponsive
    {
        // ═══════════════════════════════════════════════════════════════
        // COLORS
        // ═══════════════════════════════════════════════════════════════

        private static readonly Color COLOR_GREEN = new(0.05f, 0.45f, 0.12f);
        private static readonly Color COLOR_FAIRWAY = new(0.12f, 0.55f, 0.18f);
        private static readonly Color COLOR_ROUGH = new(0.08f, 0.30f, 0.06f);
        private static readonly Color COLOR_SAND = new(0.85f, 0.78f, 0.50f);
        private static readonly Color COLOR_WATER = new(0.10f, 0.25f, 0.65f);
        private static readonly Color COLOR_WALL = new(0.35f, 0.30f, 0.28f);
        private static readonly Color COLOR_CUP = new(0.02f, 0.02f, 0.02f);
        private static readonly Color COLOR_BALL = new(0.95f, 0.95f, 0.95f);
        private static readonly Color COLOR_FLAG = new(0.95f, 0.15f, 0.15f);
        private static readonly Color COLOR_AIM = new(0.9f, 0.9f, 0.2f, 0.6f);
        private static readonly Color COLOR_FRAME = new(0.15f, 0.12f, 0.10f);

        // ═══════════════════════════════════════════════════════════════
        // STATE
        // ═══════════════════════════════════════════════════════════════

        private GolfCourse _course;
        private GolfBall _ball;
        private GameObject _holeRoot;
        private GameObject _ballObj;
        private GameObject _aimLine;
        private float _currentAimAngle;
        private bool _dirty = true;

        // ── Glow system ──────────────────────────────────────────
        private Light _ballLight;
        private const float BallLightBaseIntensity = 0.3f;
        private const float BallLightDecay = 3f;

        // Track flashed renderers for decay back to base color
        private readonly List<(Renderer renderer, Color baseColor)> _flashedRenderers = new();

        // ═══════════════════════════════════════════════════════════════
        // INIT
        // ═══════════════════════════════════════════════════════════════

        public void Initialize(GolfCourse course, GolfBall ball)
        {
            _course = course;
            _ball = ball;
            QualityBridge.Register(this);
        }

        private void OnDisable() => QualityBridge.Unregister(this);
        public void OnQualityChanged(QualityTier tier) => _dirty = true;
        public void MarkDirty() => _dirty = true;

        public void BuildHole()
        {
            if (_holeRoot != null)
                Destroy(_holeRoot);

            _holeRoot = new GameObject("HoleVisuals");
            var hole = _course.CurrentHole;

            // Ground plane
            BuildBox(_holeRoot.transform, "Ground",
                hole.width * 0.5f, -0.05f, hole.depth * 0.5f,
                hole.width, 0.1f, hole.depth, COLOR_GREEN);

            // Frame / border
            float fw = 0.2f;
            BuildBox(_holeRoot.transform, "FrameLeft", -fw * 0.5f, 0f, hole.depth * 0.5f, fw, 0.15f, hole.depth + fw * 2, COLOR_FRAME);
            BuildBox(_holeRoot.transform, "FrameRight", hole.width + fw * 0.5f, 0f, hole.depth * 0.5f, fw, 0.15f, hole.depth + fw * 2, COLOR_FRAME);
            BuildBox(_holeRoot.transform, "FrameBottom", hole.width * 0.5f, 0f, -fw * 0.5f, hole.width + fw * 2, 0.15f, fw, COLOR_FRAME);
            BuildBox(_holeRoot.transform, "FrameTop", hole.width * 0.5f, 0f, hole.depth + fw * 0.5f, hole.width + fw * 2, 0.15f, fw, COLOR_FRAME);

            // Terrain patches
            if (hole.terrainPatches != null)
            {
                foreach (var tp in hole.terrainPatches)
                {
                    Color c = tp.type switch
                    {
                        TerrainType.Fairway => COLOR_FAIRWAY,
                        TerrainType.Rough => COLOR_ROUGH,
                        TerrainType.Sand => COLOR_SAND,
                        TerrainType.Water => COLOR_WATER,
                        _ => COLOR_GREEN
                    };
                    // Approximate circle with flat cylinder
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    go.name = $"Terrain_{tp.type}";
                    go.transform.SetParent(_holeRoot.transform);
                    go.transform.position = new Vector3(tp.x, 0.01f, tp.z);
                    go.transform.localScale = new Vector3(tp.radius * 2f, 0.01f, tp.radius * 2f);
                    SetColor(go, c);
                    RemoveCollider(go);
                }
            }

            // Walls
            if (hole.walls != null)
            {
                int wi = 0;
                foreach (var wall in hole.walls)
                {
                    BuildBox(_holeRoot.transform, $"Wall_{wi++}",
                        wall.x, 0.15f, wall.z,
                        wall.halfWidth * 2f, 0.3f, wall.halfDepth * 2f, COLOR_WALL);
                }
            }

            // Cup (dark circle + flag pole)
            var cup = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cup.name = "Cup";
            cup.transform.SetParent(_holeRoot.transform);
            cup.transform.position = new Vector3(hole.cupX, -0.02f, hole.cupZ);
            cup.transform.localScale = new Vector3(hole.cupRadius * 2f, 0.02f, hole.cupRadius * 2f);
            SetColor(cup, COLOR_CUP);
            RemoveCollider(cup);

            // Flag pole
            BuildBox(_holeRoot.transform, "FlagPole",
                hole.cupX, 0.5f, hole.cupZ, 0.04f, 1f, 0.04f, COLOR_WALL);
            BuildBox(_holeRoot.transform, "Flag",
                hole.cupX + 0.15f, 0.85f, hole.cupZ, 0.3f, 0.2f, 0.02f, COLOR_FLAG);

            // Ball visual
            if (_ballObj == null)
            {
                _ballObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                _ballObj.name = "GolfBall";
                _ballObj.transform.localScale = Vector3.one * 0.2f;
                SetColor(_ballObj, COLOR_BALL);
                RemoveCollider(_ballObj);

                // Point light on ball for glow effects
                var lightGO = new GameObject("BallGlow");
                lightGO.transform.SetParent(_ballObj.transform, false);
                _ballLight = lightGO.AddComponent<Light>();
                _ballLight.type = LightType.Point;
                _ballLight.range = 1.5f;
                _ballLight.intensity = BallLightBaseIntensity;
                _ballLight.color = Color.white;
                _ballLight.shadows = LightShadows.None;
            }

            // Aim line
            if (_aimLine == null)
            {
                _aimLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _aimLine.name = "AimLine";
                _aimLine.transform.localScale = new Vector3(0.04f, 0.02f, 1.5f);
                SetColor(_aimLine, COLOR_AIM);
                RemoveCollider(_aimLine);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // AIM
        // ═══════════════════════════════════════════════════════════════

        public void SetAimAngle(float angleDeg)
        {
            _currentAimAngle = angleDeg;
        }

        // ═══════════════════════════════════════════════════════════════
        // UPDATE
        // ═══════════════════════════════════════════════════════════════

        private void LateUpdate()
        {
            DecayBallLight();
            DecayFlashedRenderers();

            if (_ball == null) return;

            // Ball position
            if (_ballObj != null)
                _ballObj.transform.position = new Vector3(_ball.Position.x, 0.15f, _ball.Position.y);

            // Aim line: show when ball is stopped
            if (_aimLine != null)
            {
                _aimLine.SetActive(!_ball.IsMoving);
                if (!_ball.IsMoving)
                {
                    float rad = _currentAimAngle * Mathf.Deg2Rad;
                    Vector3 dir = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
                    Vector3 ballPos = new Vector3(_ball.Position.x, 0.05f, _ball.Position.y);
                    _aimLine.transform.position = ballPos + dir * 0.8f;
                    _aimLine.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // GLOW / FLASH API — called by GolfBootstrap event wiring
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Flash the ball light to a high intensity + color, then it decays back.</summary>
        public void FlashBallLight(float intensity, Color color)
        {
            if (_ballLight == null) return;
            _ballLight.intensity = intensity;
            _ballLight.color = color;
            _ballLight.range = 1.5f + intensity * 0.3f;
        }

        /// <summary>Flash the ball material to an HDR bloom burst.</summary>
        public void FlashBallColor(float boostMultiplier)
        {
            if (_ballObj == null) return;
            Color boosted = new Color(COLOR_BALL.r * boostMultiplier,
                                       COLOR_BALL.g * boostMultiplier,
                                       COLOR_BALL.b * boostMultiplier);
            SetHDRColor(_ballObj, boosted);
        }

        /// <summary>Flash any game object's material to an HDR color, then decay back.</summary>
        public void FlashObjectGlow(GameObject go, Color hdrColor, Color baseColor)
        {
            if (go == null) return;
            FlashRenderer(go, hdrColor, baseColor);
        }

        // ═══════════════════════════════════════════════════════════════
        // DECAY — runs every LateUpdate
        // ═══════════════════════════════════════════════════════════════

        private void DecayBallLight()
        {
            if (_ballLight == null) return;
            float decay = Mathf.Clamp01(BallLightDecay * Time.unscaledDeltaTime);
            _ballLight.intensity = Mathf.Lerp(_ballLight.intensity, BallLightBaseIntensity, decay);
            _ballLight.color = Color.Lerp(_ballLight.color, Color.white, decay);
            _ballLight.range = Mathf.Lerp(_ballLight.range, 1.5f, decay);

            // Decay ball material back to base
            if (_ballObj != null)
            {
                var r = _ballObj.GetComponent<Renderer>();
                if (r != null)
                {
                    var mat = r.material;
                    Color current = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color;
                    Color next = Color.Lerp(current, COLOR_BALL, decay);
                    SetHDRColorMat(mat, next);
                }
            }
        }

        private void DecayFlashedRenderers()
        {
            float decay = Mathf.Clamp01(BallLightDecay * Time.unscaledDeltaTime);
            for (int i = _flashedRenderers.Count - 1; i >= 0; i--)
            {
                var (fr, baseCol) = _flashedRenderers[i];
                if (fr == null) { _flashedRenderers.RemoveAt(i); continue; }
                var mat = fr.material;
                Color current = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color;
                Color next = Color.Lerp(current, baseCol, decay);
                SetHDRColorMat(mat, next);
                if (Mathf.Abs(next.r - baseCol.r) + Mathf.Abs(next.g - baseCol.g) + Mathf.Abs(next.b - baseCol.b) < 0.03f)
                {
                    SetHDRColorMat(mat, baseCol);
                    _flashedRenderers.RemoveAt(i);
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════════

        private static GameObject BuildBox(Transform parent, string name,
            float x, float y, float z, float sx, float sy, float sz, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(x, y, z);
            go.transform.localScale = new Vector3(sx, sy, sz);
            SetColor(go, color);
            RemoveCollider(go);
            return go;
        }

        private void FlashRenderer(GameObject go, Color hdrColor, Color baseColor)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) return;

            int idx = _flashedRenderers.FindIndex(e => e.renderer == r);
            Color origColor = baseColor;
            if (idx >= 0)
            {
                origColor = _flashedRenderers[idx].baseColor;
                _flashedRenderers.RemoveAt(idx);
            }
            _flashedRenderers.Add((r, origColor));
            SetHDRColorMat(r.material, hdrColor);
        }

        private static void SetHDRColor(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;
            SetHDRColorMat(renderer.material, color);
        }

        private static void SetHDRColorMat(Material mat, Color color)
        {
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else
                mat.color = color;

            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color);
            }
        }

        private static void SetColor(GameObject go, Color color)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) return;
            var mat = r.material;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else
                mat.color = color;
        }

        private static void RemoveCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }
    }
}
