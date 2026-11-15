using UnityEngine;

namespace BuildKit
{
    public enum ModuleKind
    {
        Generic,
        Wall,
        Floor,
        Corner,
        Door,
        Window,
        Roof,
        Prop
    }

    public enum ModuleSide
    {
        Left,
        Right,
        Back,
        Front,
        Bottom,
        Top
    }

    /// <summary>
    /// Marks an object as a snappable module and stores its measured bounds.
    /// Add it to a prefab or scene object and call Analyze, or use the BuildKit window.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("BuildKit/Build Module")]
    public class BuildModule : MonoBehaviour
    {
        public ModuleKind kind = ModuleKind.Generic;

        [Tooltip("Bounds of all renderers under this object, in this object's local space. Set by Analyze.")]
        public Bounds localBounds = new Bounds(Vector3.zero, Vector3.one);

        [Tooltip("True once Analyze has measured real renderers.")]
        public bool analyzed;

        public Vector3 LocalSize => localBounds.size;

        /// <summary>Size in world units, including this object's scale.</summary>
        public Vector3 WorldSize => Vector3.Scale(localBounds.size, Abs(transform.lossyScale));

        /// <summary>Bottom center of the bounds in local space. Placement snaps this point to the grid.</summary>
        public Vector3 BottomCenterLocal => new Vector3(localBounds.center.x, localBounds.min.y, localBounds.center.z);

        public Vector3 BottomCenterWorld => transform.TransformPoint(BottomCenterLocal);

        public Vector3 GetSnapPointLocal(ModuleSide side)
        {
            Vector3 c = localBounds.center;
            Vector3 e = localBounds.extents;
            switch (side)
            {
                case ModuleSide.Left: return new Vector3(c.x - e.x, c.y, c.z);
                case ModuleSide.Right: return new Vector3(c.x + e.x, c.y, c.z);
                case ModuleSide.Back: return new Vector3(c.x, c.y, c.z - e.z);
                case ModuleSide.Front: return new Vector3(c.x, c.y, c.z + e.z);
                case ModuleSide.Bottom: return new Vector3(c.x, c.y - e.y, c.z);
                case ModuleSide.Top: return new Vector3(c.x, c.y + e.y, c.z);
                default: return c;
            }
        }

        public Vector3 GetSnapPointWorld(ModuleSide side) => transform.TransformPoint(GetSnapPointLocal(side));

        /// <summary>Measures the renderers under this object and stores the result.</summary>
        public void Analyze()
        {
            if (ModuleAnalyzer.TryComputeLocalBounds(transform, out Bounds b))
            {
                b.size = ModuleAnalyzer.CleanSize(b.size);
                localBounds = b;
                analyzed = true;
            }
            else
            {
                localBounds = new Bounds(Vector3.zero, Vector3.one);
                analyzed = false;
            }
        }

        static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = analyzed ? new Color(0.2f, 0.9f, 1f, 0.9f) : new Color(1f, 0.6f, 0.2f, 0.9f);
            Gizmos.DrawWireCube(localBounds.center, localBounds.size);
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(BottomCenterLocal, 0.05f);
        }
    }
}
