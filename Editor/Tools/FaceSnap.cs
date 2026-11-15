using UnityEditor;
using UnityEngine;

namespace BuildKit.Editor
{
    public enum SnapMode
    {
        /// <summary>World grid only.</summary>
        Grid,
        /// <summary>Snap to the face under the cursor when there is one, otherwise the grid.</summary>
        Auto,
        /// <summary>Only snap to faces of placed modules.</summary>
        Face
    }

    /// <summary>
    /// Face to face snapping. Point at a placed module and the ghost sits flush against that face,
    /// sliding along it in steps measured from the module's edge so edges line up even when sizes differ.
    /// </summary>
    public static class FaceSnap
    {
        public struct Hit
        {
            public BuildModule module;
            public Bounds bounds;
            public int axis;
            public int sign;
            public Vector3 point;
        }

        static readonly Color TargetFace = new Color(1f, 0.7f, 0.2f, 1f);
        static readonly Color TargetFill = new Color(1f, 0.7f, 0.2f, 0.12f);
        const float FaceEpsilon = 0.002f;

        /// <summary>Finds the nearest placed module under the ray and which of its six faces was hit.</summary>
        public static bool TryHit(Ray ray, out Hit hit)
        {
            hit = default;
            float best = float.MaxValue;
            bool found = false;

            foreach (BuildModule m in SnapFeedback.AllModules())
            {
                if (m == null) continue;
                Bounds b = SnapFeedback.WorldBounds(m);
                if (!b.IntersectRay(ray, out float d) || d >= best || d < 0f) continue;

                Vector3 p = ray.GetPoint(d);
                if (!ClassifyFace(b, p, out int axis, out int sign)) continue;

                best = d;
                found = true;
                hit = new Hit { module = m, bounds = b, axis = axis, sign = sign, point = p };
            }

            return found;
        }

        /// <summary>
        /// Places a ghost of the given size flush against the hit face. Along the face it steps by the
        /// ghost size (Shift: half, Ctrl: free), measured from the target's min edge.
        /// </summary>
        public static Vector3 Resolve(Hit hit, Vector3 size, bool halfStep, bool freeSlide)
        {
            Bounds b = hit.bounds;
            Vector3 half = size * 0.5f;
            Vector3 result = Vector3.zero;

            // Normal axis: flush against the face.
            float face = hit.sign > 0 ? b.max[hit.axis] : b.min[hit.axis];
            result[hit.axis] = hit.sign > 0 ? face + half[hit.axis] : face - half[hit.axis];

            // The other axes slide along the face.
            for (int a = 0; a < 3; a++)
            {
                if (a == hit.axis) continue;
                if (a == 1)
                {
                    // Side faces keep floors aligned with the target's floor.
                    result.y = b.min.y;
                    continue;
                }
                result[a] = Slide(hit.point[a], b.min[a], b.max[a], size[a], halfStep, freeSlide);
            }

            // Top or bottom face: bottom center sits on the face itself.
            if (hit.axis == 1)
                result.y = hit.sign > 0 ? b.max.y : b.min.y - size.y;

            return result;
        }

        /// <summary>Draws an outline on the face being targeted so the builder sees what the ghost will attach to.</summary>
        public static void DrawTarget(Hit hit)
        {
            Bounds b = hit.bounds;
            int u = (hit.axis + 1) % 3;
            int v = (hit.axis + 2) % 3;
            float plane = hit.sign > 0 ? b.max[hit.axis] : b.min[hit.axis];

            var verts = new Vector3[4];
            verts[0] = Corner(hit.axis, plane, u, b.min[u], v, b.min[v]);
            verts[1] = Corner(hit.axis, plane, u, b.min[u], v, b.max[v]);
            verts[2] = Corner(hit.axis, plane, u, b.max[u], v, b.max[v]);
            verts[3] = Corner(hit.axis, plane, u, b.max[u], v, b.min[v]);

            Handles.DrawSolidRectangleWithOutline(verts, TargetFill, Color.clear);
            Handles.color = TargetFace;
            Handles.DrawAAPolyLine(3f, verts[0], verts[1], verts[2], verts[3], verts[0]);
        }

        static float Slide(float hitValue, float min, float max, float size, bool halfStep, bool freeSlide)
        {
            float halfSize = size * 0.5f;
            if (freeSlide) return hitValue;

            float step = halfStep ? halfSize : size;
            if (step <= 0.0001f) return hitValue;

            // Measure from the target's min edge so a ghost edge lands on that edge at k = 0.
            float origin = min + halfSize;
            float k = Mathf.Round((hitValue - origin) / step);
            return origin + k * step;
        }

        static bool ClassifyFace(Bounds b, Vector3 p, out int axis, out int sign)
        {
            axis = -1;
            sign = 0;
            float bestDist = float.MaxValue;
            for (int a = 0; a < 3; a++)
            {
                float toMin = Mathf.Abs(p[a] - b.min[a]);
                float toMax = Mathf.Abs(p[a] - b.max[a]);
                if (toMin < bestDist) { bestDist = toMin; axis = a; sign = -1; }
                if (toMax < bestDist) { bestDist = toMax; axis = a; sign = 1; }
            }
            return axis >= 0 && bestDist <= Mathf.Max(FaceEpsilon, b.size.magnitude * 0.001f);
        }

        static Vector3 Corner(int a, float av, int u, float uv, int v, float vv)
        {
            var p = Vector3.zero;
            p[a] = av;
            p[u] = uv;
            p[v] = vv;
            return p;
        }
    }
}
