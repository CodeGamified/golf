// Copyright CodeGamified 2025-2026
// MIT License — Golf
using System;
using UnityEngine;

namespace Golf.Game
{
    // ═══════════════════════════════════════════════════════════════
    // DATA STRUCTURES
    // ═══════════════════════════════════════════════════════════════

    public enum TerrainType
    {
        Green = 0,
        Fairway = 1,
        Rough = 2,
        Sand = 3,
        Water = 4
    }

    [Serializable]
    public struct Wall
    {
        public float x, z;          // center
        public float halfWidth, halfDepth; // half-extents

        public Wall(float x, float z, float w, float d)
        {
            this.x = x; this.z = z;
            halfWidth = w * 0.5f; halfDepth = d * 0.5f;
        }

        public float MinX => x - halfWidth;
        public float MaxX => x + halfWidth;
        public float MinZ => z - halfDepth;
        public float MaxZ => z + halfDepth;
    }

    [Serializable]
    public struct TerrainPatch
    {
        public float x, z, radius;
        public TerrainType type;

        public TerrainPatch(float x, float z, float r, TerrainType t)
        {
            this.x = x; this.z = z; radius = r; type = t;
        }
    }

    [Serializable]
    public struct SlopeRegion
    {
        public float x, z, radius;
        public float slopeX, slopeZ; // direction + magnitude

        public SlopeRegion(float x, float z, float r, float sx, float sz)
        {
            this.x = x; this.z = z; radius = r;
            slopeX = sx; slopeZ = sz;
        }
    }

    [Serializable]
    public struct HoleData
    {
        public float width, depth;     // course bounds
        public float teeX, teeZ;       // start position
        public float cupX, cupZ;        // target position
        public float cupRadius;
        public int par;
        public float windAngle, windSpeed;
        public Wall[] walls;
        public TerrainPatch[] terrainPatches;
        public SlopeRegion[] slopeRegions;
    }

    // ═══════════════════════════════════════════════════════════════
    // GOLF COURSE
    // ═══════════════════════════════════════════════════════════════

    public class GolfCourse : MonoBehaviour
    {
        public HoleData[] Holes { get; private set; }
        public int CurrentHoleIndex { get; set; }
        public int TotalHoles => Holes?.Length ?? 0;
        public HoleData CurrentHole => Holes[CurrentHoleIndex];

        public void GenerateCourse()
        {
            Holes = new HoleData[9];
            Holes[0] = MakeHole1();
            Holes[1] = MakeHole2();
            Holes[2] = MakeHole3();
            Holes[3] = MakeHole4();
            Holes[4] = MakeHole5();
            Holes[5] = MakeHole6();
            Holes[6] = MakeHole7();
            Holes[7] = MakeHole8();
            Holes[8] = MakeHole9();
        }

        // ═══════════════════════════════════════════════════════════════
        // TERRAIN QUERIES
        // ═══════════════════════════════════════════════════════════════

        public TerrainType GetTerrain(float x, float z)
        {
            var hole = CurrentHole;
            if (hole.terrainPatches != null)
            {
                for (int i = hole.terrainPatches.Length - 1; i >= 0; i--)
                {
                    var p = hole.terrainPatches[i];
                    float dx = x - p.x;
                    float dz = z - p.z;
                    if (dx * dx + dz * dz <= p.radius * p.radius)
                        return p.type;
                }
            }
            return TerrainType.Green; // default
        }

        public Vector2 GetSlope(float x, float z)
        {
            var hole = CurrentHole;
            if (hole.slopeRegions != null)
            {
                foreach (var s in hole.slopeRegions)
                {
                    float dx = x - s.x;
                    float dz = z - s.z;
                    if (dx * dx + dz * dz <= s.radius * s.radius)
                        return new Vector2(s.slopeX, s.slopeZ);
                }
            }
            return Vector2.zero;
        }

        public Vector2 GetWind()
        {
            var hole = CurrentHole;
            float rad = hole.windAngle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * hole.windSpeed;
        }

        // ═══════════════════════════════════════════════════════════════
        // HOLE DEFINITIONS (9-hole mini-golf course)
        // ═══════════════════════════════════════════════════════════════

        private static HoleData MakeHole1()
        {
            // Straight shot — pure distance/power calibration
            return new HoleData
            {
                width = 4f, depth = 12f,
                teeX = 2f, teeZ = 1.5f,
                cupX = 2f, cupZ = 10.5f,
                cupRadius = 0.3f, par = 2,
                windAngle = 0f, windSpeed = 0f,
                walls = Array.Empty<Wall>(),
                terrainPatches = Array.Empty<TerrainPatch>(),
                slopeRegions = Array.Empty<SlopeRegion>()
            };
        }

        private static HoleData MakeHole2()
        {
            // Diagonal shot — angle calculation
            return new HoleData
            {
                width = 10f, depth = 10f,
                teeX = 1.5f, teeZ = 1.5f,
                cupX = 8.5f, cupZ = 8.5f,
                cupRadius = 0.3f, par = 2,
                windAngle = 0f, windSpeed = 0f,
                walls = Array.Empty<Wall>(),
                terrainPatches = Array.Empty<TerrainPatch>(),
                slopeRegions = Array.Empty<SlopeRegion>()
            };
        }

        private static HoleData MakeHole3()
        {
            // Sand trap in center — putt around or power through
            return new HoleData
            {
                width = 8f, depth = 14f,
                teeX = 4f, teeZ = 1.5f,
                cupX = 4f, cupZ = 12.5f,
                cupRadius = 0.3f, par = 2,
                windAngle = 0f, windSpeed = 0f,
                walls = Array.Empty<Wall>(),
                terrainPatches = new[]
                {
                    new TerrainPatch(4f, 7f, 2f, TerrainType.Sand)
                },
                slopeRegions = Array.Empty<SlopeRegion>()
            };
        }

        private static HoleData MakeHole4()
        {
            // L-shaped — wall forces bank shot
            return new HoleData
            {
                width = 12f, depth = 12f,
                teeX = 1.5f, teeZ = 1.5f,
                cupX = 10.5f, cupZ = 10.5f,
                cupRadius = 0.3f, par = 3,
                windAngle = 0f, windSpeed = 0f,
                walls = new[]
                {
                    new Wall(6f, 6f, 8f, 0.4f),   // horizontal wall blocking center
                    new Wall(10f, 3f, 0.4f, 6f),   // vertical wall on right side
                },
                terrainPatches = Array.Empty<TerrainPatch>(),
                slopeRegions = Array.Empty<SlopeRegion>()
            };
        }

        private static HoleData MakeHole5()
        {
            // Water hazard — go around or face penalty
            return new HoleData
            {
                width = 10f, depth = 14f,
                teeX = 5f, teeZ = 1.5f,
                cupX = 5f, cupZ = 12.5f,
                cupRadius = 0.3f, par = 2,
                windAngle = 0f, windSpeed = 0f,
                walls = Array.Empty<Wall>(),
                terrainPatches = new[]
                {
                    new TerrainPatch(5f, 7f, 2.5f, TerrainType.Water)
                },
                slopeRegions = Array.Empty<SlopeRegion>()
            };
        }

        private static HoleData MakeHole6()
        {
            // Sloped green — ball curves
            return new HoleData
            {
                width = 8f, depth = 12f,
                teeX = 4f, teeZ = 1.5f,
                cupX = 4f, cupZ = 10.5f,
                cupRadius = 0.3f, par = 2,
                windAngle = 0f, windSpeed = 0f,
                walls = Array.Empty<Wall>(),
                terrainPatches = Array.Empty<TerrainPatch>(),
                slopeRegions = new[]
                {
                    new SlopeRegion(4f, 6f, 4f, 3f, 0f) // pushes ball right
                }
            };
        }

        private static HoleData MakeHole7()
        {
            // Narrow corridor between walls
            return new HoleData
            {
                width = 10f, depth = 16f,
                teeX = 5f, teeZ = 1.5f,
                cupX = 5f, cupZ = 14.5f,
                cupRadius = 0.3f, par = 3,
                windAngle = 0f, windSpeed = 0f,
                walls = new[]
                {
                    new Wall(3f, 8f, 0.4f, 10f),  // left wall
                    new Wall(7f, 8f, 0.4f, 10f),  // right wall
                },
                terrainPatches = new[]
                {
                    new TerrainPatch(5f, 5f, 1.5f, TerrainType.Rough)
                },
                slopeRegions = Array.Empty<SlopeRegion>()
            };
        }

        private static HoleData MakeHole8()
        {
            // Obstacle gauntlet — sand, rough, wall
            return new HoleData
            {
                width = 10f, depth = 16f,
                teeX = 2f, teeZ = 1.5f,
                cupX = 8f, cupZ = 14.5f,
                cupRadius = 0.3f, par = 3,
                windAngle = 45f, windSpeed = 0.5f,
                walls = new[]
                {
                    new Wall(5f, 8f, 6f, 0.4f),
                },
                terrainPatches = new[]
                {
                    new TerrainPatch(3f, 5f, 1.5f, TerrainType.Sand),
                    new TerrainPatch(7f, 12f, 1.5f, TerrainType.Rough),
                },
                slopeRegions = Array.Empty<SlopeRegion>()
            };
        }

        private static HoleData MakeHole9()
        {
            // Grand finale — wind + slope + sand + water
            return new HoleData
            {
                width = 14f, depth = 18f,
                teeX = 2f, teeZ = 1.5f,
                cupX = 12f, cupZ = 16.5f,
                cupRadius = 0.3f, par = 4,
                windAngle = 135f, windSpeed = 1f,
                walls = new[]
                {
                    new Wall(7f, 6f, 0.4f, 8f),
                    new Wall(10f, 13f, 6f, 0.4f),
                },
                terrainPatches = new[]
                {
                    new TerrainPatch(4f, 10f, 2f, TerrainType.Sand),
                    new TerrainPatch(10f, 6f, 2f, TerrainType.Water),
                    new TerrainPatch(6f, 15f, 1.5f, TerrainType.Rough),
                },
                slopeRegions = new[]
                {
                    new SlopeRegion(8f, 10f, 3f, -2f, 1.5f)
                }
            };
        }
    }
}
