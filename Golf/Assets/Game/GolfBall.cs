// Copyright CodeGamified 2025-2026
// MIT License — Golf
using System;
using UnityEngine;
using CodeGamified.Time;

namespace Golf.Game
{
    /// <summary>
    /// Golf ball with 2D physics on XZ plane.
    /// Putt sets initial velocity; Update applies friction, slope, wind, wall bounce.
    /// </summary>
    public class GolfBall : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // CONSTANTS
        // ═══════════════════════════════════════════════════════════════

        private const float MAX_POWER = 100f;
        private const float POWER_TO_SPEED = 0.18f;    // power 100 → 18 units/sec
        private const float STOP_THRESHOLD = 0.08f;
        private const float SINK_SPEED = 2.5f;          // max speed to drop in cup
        private const float SLOPE_FORCE = 4f;
        private const float WIND_FORCE = 1.5f;
        private const float BOUNCE_DAMPING = 0.7f;
        private const float PHYSICS_STEP = 1f / 120f;   // fixed sub-step for determinism

        // Friction: fraction of speed retained per second (lower = more friction)
        private static readonly float[] TerrainFriction = { 0.35f, 0.25f, 0.10f, 0.04f, 0.30f };

        // ═══════════════════════════════════════════════════════════════
        // STATE
        // ═══════════════════════════════════════════════════════════════

        public Vector2 Position;        // X, Z
        public Vector2 Velocity;
        public bool IsMoving { get; private set; }
        public float LastPuttDistance { get; private set; }

        private GolfCourse _course;
        private Vector2 _prePuttPos;
        private float _physicsAccum;

        // ═══════════════════════════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════════════════════════

        public event Action OnBallStopped;
        public event Action OnBallSunk;     // reached cup
        public event Action OnBallInWater;

        // ═══════════════════════════════════════════════════════════════
        // INIT
        // ═══════════════════════════════════════════════════════════════

        public void Initialize(GolfCourse course)
        {
            _course = course;
        }

        public void PlaceAtTee()
        {
            var hole = _course.CurrentHole;
            Position = new Vector2(hole.teeX, hole.teeZ);
            Velocity = Vector2.zero;
            IsMoving = false;
            LastPuttDistance = 0f;
            _physicsAccum = 0f;
            SyncTransform();
        }

        // ═══════════════════════════════════════════════════════════════
        // PUTT
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Execute a putt. Angle in degrees (0=+X, 90=+Z). Power 0-100.
        /// Returns false if ball is already moving.
        /// </summary>
        public bool Putt(float angleDeg, float power)
        {
            if (IsMoving) return false;

            power = Mathf.Clamp(power, 0f, MAX_POWER);
            float speed = power * POWER_TO_SPEED;
            float rad = angleDeg * Mathf.Deg2Rad;

            _prePuttPos = Position;
            Velocity = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * speed;
            IsMoving = true;
            _physicsAccum = 0f;
            return true;
        }

        public void ResetToPrePutt()
        {
            Position = _prePuttPos;
            Velocity = Vector2.zero;
            IsMoving = false;
            SyncTransform();
        }

        // ═══════════════════════════════════════════════════════════════
        // PHYSICS UPDATE
        // ═══════════════════════════════════════════════════════════════

        private void Update()
        {
            if (!IsMoving || _course == null) return;

            float timeScale = SimulationTime.Instance?.timeScale ?? 1f;
            if (SimulationTime.Instance != null && SimulationTime.Instance.isPaused) return;

            float dt = UnityEngine.Time.deltaTime * timeScale;
            _physicsAccum += dt;

            // Sub-step for stability
            while (_physicsAccum >= PHYSICS_STEP)
            {
                StepPhysics(PHYSICS_STEP);
                _physicsAccum -= PHYSICS_STEP;
                if (!IsMoving) break;
            }

            SyncTransform();
        }

        private void StepPhysics(float dt)
        {
            var hole = _course.CurrentHole;

            // Terrain friction
            TerrainType terrain = _course.GetTerrain(Position.x, Position.y);
            float retention = TerrainFriction[(int)terrain];
            Velocity *= Mathf.Pow(retention, dt);

            // Slope force
            Vector2 slope = _course.GetSlope(Position.x, Position.y);
            if (slope != Vector2.zero)
                Velocity += slope * SLOPE_FORCE * dt;

            // Wind force
            Vector2 wind = _course.GetWind();
            if (wind != Vector2.zero)
                Velocity += wind * WIND_FORCE * dt;

            // Move
            Position += Velocity * dt;

            // Wall collisions
            if (hole.walls != null)
            {
                foreach (var wall in hole.walls)
                    ResolveWallCollision(ref Position, ref Velocity, wall);
            }

            // Course boundary bounce
            float margin = 0.15f;
            if (Position.x < margin) { Position.x = margin; Velocity.x = Mathf.Abs(Velocity.x) * BOUNCE_DAMPING; }
            if (Position.x > hole.width - margin) { Position.x = hole.width - margin; Velocity.x = -Mathf.Abs(Velocity.x) * BOUNCE_DAMPING; }
            if (Position.y < margin) { Position.y = margin; Velocity.y = Mathf.Abs(Velocity.y) * BOUNCE_DAMPING; }
            if (Position.y > hole.depth - margin) { Position.y = hole.depth - margin; Velocity.y = -Mathf.Abs(Velocity.y) * BOUNCE_DAMPING; }

            // Check cup
            float cupDx = Position.x - hole.cupX;
            float cupDz = Position.y - hole.cupZ;
            float cupDist = Mathf.Sqrt(cupDx * cupDx + cupDz * cupDz);
            if (cupDist <= hole.cupRadius && Velocity.magnitude < SINK_SPEED)
            {
                Position = new Vector2(hole.cupX, hole.cupZ);
                Velocity = Vector2.zero;
                IsMoving = false;
                LastPuttDistance = Vector2.Distance(_prePuttPos, Position);
                OnBallSunk?.Invoke();
                return;
            }

            // Check water (ball slowed to near-stop in water)
            if (terrain == TerrainType.Water && Velocity.magnitude < 1f)
            {
                Velocity = Vector2.zero;
                IsMoving = false;
                LastPuttDistance = Vector2.Distance(_prePuttPos, Position);
                OnBallInWater?.Invoke();
                return;
            }

            // Stop check
            if (Velocity.magnitude < STOP_THRESHOLD)
            {
                Velocity = Vector2.zero;
                IsMoving = false;
                LastPuttDistance = Vector2.Distance(_prePuttPos, Position);
                OnBallStopped?.Invoke();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // WALL COLLISION
        // ═══════════════════════════════════════════════════════════════

        private static void ResolveWallCollision(ref Vector2 pos, ref Vector2 vel, Wall wall)
        {
            // AABB overlap test
            if (pos.x < wall.MinX || pos.x > wall.MaxX ||
                pos.y < wall.MinZ || pos.y > wall.MaxZ)
                return;

            // Find closest face and push out
            float dLeft = pos.x - wall.MinX;
            float dRight = wall.MaxX - pos.x;
            float dBottom = pos.y - wall.MinZ;
            float dTop = wall.MaxZ - pos.y;
            float minD = Mathf.Min(Mathf.Min(dLeft, dRight), Mathf.Min(dBottom, dTop));

            if (minD == dLeft) { pos.x = wall.MinX - 0.01f; vel.x = -Mathf.Abs(vel.x) * BOUNCE_DAMPING; }
            else if (minD == dRight) { pos.x = wall.MaxX + 0.01f; vel.x = Mathf.Abs(vel.x) * BOUNCE_DAMPING; }
            else if (minD == dBottom) { pos.y = wall.MinZ - 0.01f; vel.y = -Mathf.Abs(vel.y) * BOUNCE_DAMPING; }
            else { pos.y = wall.MaxZ + 0.01f; vel.y = Mathf.Abs(vel.y) * BOUNCE_DAMPING; }
        }

        // ═══════════════════════════════════════════════════════════════
        // TRANSFORM SYNC
        // ═══════════════════════════════════════════════════════════════

        private void SyncTransform()
        {
            transform.position = new Vector3(Position.x, 0.15f, Position.y);
        }
    }
}
