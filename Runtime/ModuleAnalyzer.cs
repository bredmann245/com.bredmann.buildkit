using UnityEngine;

namespace BuildKit
{
    /// <summary>
    /// Measures the renderers under a transform and reports their bounds in that transform's local space.
    /// Root scale is excluded, so the result is in unscaled local units.
    /// </summary>
    public static class ModuleAnalyzer
    {
        public static bool TryComputeLocalBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            bool any = false;
            Matrix4x4 toRoot = root.worldToLocalMatrix;

            foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = mf.sharedMesh;
                if (mesh == null) continue;
                Encapsulate(ref bounds, ref any, mesh.bounds, toRoot * mf.transform.localToWorldMatrix);
            }

            foreach (SkinnedMeshRenderer smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Mesh mesh = smr.sharedMesh;
                if (mesh == null) continue;
                Encapsulate(ref bounds, ref any, mesh.bounds, toRoot * smr.transform.localToWorldMatrix);
            }

            return any;
        }

        static void Encapsulate(ref Bounds bounds, ref bool any, Bounds b, Matrix4x4 m)
        {
            Vector3 min = b.min;
            Vector3 max = b.max;
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3(
                    (i & 1) == 0 ? min.x : max.x,
                    (i & 2) == 0 ? min.y : max.y,
                    (i & 4) == 0 ? min.z : max.z);
                Vector3 p = m.MultiplyPoint3x4(corner);
                if (!any)
                {
                    bounds = new Bounds(p, Vector3.zero);
                    any = true;
                }
                else
                {
                    bounds.Encapsulate(p);
                }
            }
        }

        /// <summary>Rounds a size to a clean grid step so tiny float noise does not break snapping.</summary>
        public static Vector3 CleanSize(Vector3 size, float step = 0.001f)
        {
            return new Vector3(Clean(size.x, step), Clean(size.y, step), Clean(size.z, step));
        }

        static float Clean(float v, float step)
        {
            float r = Mathf.Round(v / step) * step;
            return Mathf.Max(r, step);
        }
    }
}
