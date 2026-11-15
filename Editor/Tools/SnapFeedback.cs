using UnityEditor;
using UnityEngine;

namespace BuildKit.Editor
{
    /// <summary>
    /// Visual cues that tell the builder where the ghost sits relative to what is already placed:
    /// shared faces light up green, overlaps turn red, a grid and level marker show position.
    /// </summary>
    public static class SnapFeedback
    {
        public struct Result
        {
            public bool overlaps;
            public int touching;
        }

        const float Epsilon = 0.01f;
        const int GridRadius = 5;

        static readonly Color TouchFill = new Color(0.3f, 1f, 0.45f, 0.35f);
        static readonly Color TouchLine = new Color(0.3f, 1f, 0.45f, 1f);
        static readonly Color OverlapLine = new Color(1f, 0.3f, 0.3f, 1f);
        static readonly Color GridLine = new Color(1f, 1f, 1f, 0.08f);
        static readonly Color GridAxis = new Color(1f, 1f, 1f, 0.3f);
        static readonly Color LevelLine = new Color(1f, 0.85f, 0.3f, 0.9f);

        static BuildModule[] cache;
        static bool dirty = true;

        [InitializeOnLoadMethod]
        static void Init()
        {
            EditorApplication.hierarchyChanged += Invalidate;
            Undo.undoRedoPerformed += Invalidate;
        }

        public static void Invalidate() => dirty = true;

        static BuildModule[] Modules
        {
            get
            {
                if (dirty || cache == null)
                {
                    cache = Object.FindObjectsByType<BuildModule>(FindObjectsSortMode.None);
                    dirty = false;
                }
                return cache;
            }
        }

        public static Bounds WorldBounds(BuildModule m)
        {
            Matrix4x4 mat = m.transform.localToWorldMatrix;
            Vector3 min = m.localBounds.min;
            Vector3 max = m.localBounds.max;
            Bounds b = default;
            for (int i = 0; i < 8; i++)
            {
                var c = new Vector3((i & 1) == 0 ? min.x : max.x, (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z);
                Vector3 p = mat.MultiplyPoint3x4(c);
                if (i == 0) b = new Bounds(p, Vector3.zero);
                else b.Encapsulate(p);
            }
            return b;
        }

        public static Bounds GhostBounds(Vector3 bottomCenter, Vector3 size)
        {
            return new Bounds(bottomCenter + Vector3.up * (size.y * 0.5f), size);
        }

        /// <summary>True when a module already fills a meaningful part of this cell.</summary>
        public static bool IsOccupied(Vector3 bottomCenter, Vector3 size)
        {
            Bounds ghost = GhostBounds(bottomCenter, size);
            foreach (BuildModule m in Modules)
            {
                if (m == null) continue;
                Vector3 ov = Overlap(ghost, WorldBounds(m));
                if (ov.x > Epsilon && ov.y > Epsilon && ov.z > Epsilon) return true;
            }
            return false;
        }

        /// <summary>Draws contact faces and overlap outlines for a ghost cell. Call inside OnToolGUI.</summary>
        public static Result DrawContacts(Vector3 bottomCenter, Vector3 size)
        {
            var result = new Result();
            Bounds ghost = GhostBounds(bottomCenter, size);
            Bounds reach = ghost;
            reach.Expand(Epsilon * 4f);

            foreach (BuildModule m in Modules)
            {
                if (m == null) continue;
                Bounds other = WorldBounds(m);
                if (!reach.Intersects(other)) continue;

                Vector3 ov = Overlap(ghost, other);
                bool px = ov.x > Epsilon, py = ov.y > Epsilon, pz = ov.z > Epsilon;

                if (px && py && pz)
                {
                    result.overlaps = true;
                    Handles.color = OverlapLine;
                    Handles.DrawWireCube(other.center, other.size);
                    continue;
                }

                int axis = -1;
                if (!px && py && pz && Mathf.Abs(ov.x) <= Epsilon) axis = 0;
                else if (px && !py && pz && Mathf.Abs(ov.y) <= Epsilon) axis = 1;
                else if (px && py && !pz && Mathf.Abs(ov.z) <= Epsilon) axis = 2;
                if (axis < 0) continue;

                result.touching++;
                DrawSharedFace(ghost, other, axis);
            }

            return result;
        }

        /// <summary>Faint grid around the cursor at the current level, plus a stronger line on the world axes.</summary>
        public static void DrawGrid(Vector3 bottomCenter, Vector3 cell)
        {
            float y = bottomCenter.y;
            int cx = Mathf.FloorToInt(bottomCenter.x / cell.x);
            int cz = Mathf.FloorToInt(bottomCenter.z / cell.z);

            float x0 = (cx - GridRadius) * cell.x;
            float x1 = (cx + GridRadius + 1) * cell.x;
            float z0 = (cz - GridRadius) * cell.z;
            float z1 = (cz + GridRadius + 1) * cell.z;

            for (int i = -GridRadius; i <= GridRadius + 1; i++)
            {
                float x = (cx + i) * cell.x;
                Handles.color = Mathf.Abs(x) < Epsilon ? GridAxis : GridLine;
                Handles.DrawLine(new Vector3(x, y, z0), new Vector3(x, y, z1));

                float z = (cz + i) * cell.z;
                Handles.color = Mathf.Abs(z) < Epsilon ? GridAxis : GridLine;
                Handles.DrawLine(new Vector3(x0, y, z), new Vector3(x1, y, z));
            }
        }

        /// <summary>Vertical marker from the ground to the ghost and a label with cell coordinates and level.</summary>
        public static void DrawLevelMarker(Vector3 bottomCenter, Vector3 cell, int level, float groundY)
        {
            if (level != 0)
            {
                Handles.color = LevelLine;
                var ground = new Vector3(bottomCenter.x, groundY, bottomCenter.z);
                Handles.DrawDottedLine(ground, bottomCenter, 4f);
                Handles.DrawWireDisc(ground, Vector3.up, Mathf.Min(cell.x, cell.z) * 0.15f);
            }

            int cx = Mathf.FloorToInt(bottomCenter.x / cell.x);
            int cz = Mathf.FloorToInt(bottomCenter.z / cell.z);
            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = Color.white },
                fontStyle = FontStyle.Bold
            };
            Handles.Label(bottomCenter + Vector3.up * (cell.y + 0.1f),
                $"cell ({cx}, {cz})  L{level}  y {bottomCenter.y:0.##}", style);
        }

        static void DrawSharedFace(Bounds ghost, Bounds other, int axis)
        {
            float plane = other.center[axis] > ghost.center[axis] ? ghost.max[axis] : ghost.min[axis];
            int u = (axis + 1) % 3;
            int v = (axis + 2) % 3;

            float u0 = Mathf.Max(ghost.min[u], other.min[u]);
            float u1 = Mathf.Min(ghost.max[u], other.max[u]);
            float v0 = Mathf.Max(ghost.min[v], other.min[v]);
            float v1 = Mathf.Min(ghost.max[v], other.max[v]);

            var verts = new Vector3[4];
            verts[0] = Make(axis, plane, u, u0, v, v0);
            verts[1] = Make(axis, plane, u, u0, v, v1);
            verts[2] = Make(axis, plane, u, u1, v, v1);
            verts[3] = Make(axis, plane, u, u1, v, v0);

            Handles.DrawSolidRectangleWithOutline(verts, TouchFill, Color.clear);
            Handles.color = TouchLine;
            Handles.DrawAAPolyLine(4f, verts[0], verts[1], verts[2], verts[3], verts[0]);
        }

        static Vector3 Make(int a, float av, int u, float uv, int v, float vv)
        {
            var p = Vector3.zero;
            p[a] = av;
            p[u] = uv;
            p[v] = vv;
            return p;
        }

        /// <summary>Per axis overlap length. Zero or negative means the boxes do not overlap on that axis.</summary>
        static Vector3 Overlap(Bounds a, Bounds b)
        {
            return new Vector3(
                Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x),
                Mathf.Min(a.max.y, b.max.y) - Mathf.Max(a.min.y, b.min.y),
                Mathf.Min(a.max.z, b.max.z) - Mathf.Max(a.min.z, b.min.z));
        }
    }
}
