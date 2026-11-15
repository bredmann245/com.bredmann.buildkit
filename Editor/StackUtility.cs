using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BuildKit.Editor
{
    /// <summary>Duplicates objects upward by a height, N times. Used to turn one floor into many.</summary>
    public static class StackUtility
    {
        public static List<GameObject> Stack(GameObject[] roots, int count, float height)
        {
            var created = new List<GameObject>();
            if (roots == null || roots.Length == 0 || count <= 0 || height <= 0f) return created;

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();

            foreach (GameObject root in roots)
            {
                if (root == null) continue;
                for (int i = 1; i <= count; i++)
                {
                    GameObject copy = Duplicate(root);
                    copy.transform.position = root.transform.position + Vector3.up * (height * i);
                    created.Add(copy);
                }
            }

            Undo.CollapseUndoOperations(group);
            Undo.SetCurrentGroupName("BuildKit Stack");
            return created;
        }

        /// <summary>Height to stack by: the module height when the selection has one, otherwise the bounds height.</summary>
        public static float GuessHeight(GameObject[] roots)
        {
            float best = 0f;
            foreach (GameObject root in roots)
            {
                if (root == null) continue;
                var module = root.GetComponentInChildren<BuildModule>();
                if (module != null && module.analyzed)
                {
                    best = Mathf.Max(best, module.WorldSize.y);
                    continue;
                }
                var renderers = root.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) continue;
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
                best = Mathf.Max(best, b.size.y);
            }
            return best;
        }

        static GameObject Duplicate(GameObject root)
        {
            GameObject copy;
            GameObject prefab = PrefabUtility.GetCorrespondingObjectFromSource(root);
            if (prefab != null && PrefabUtility.IsOutermostPrefabInstanceRoot(root))
            {
                copy = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform.parent);
                copy.transform.SetPositionAndRotation(root.transform.position, root.transform.rotation);
                copy.transform.localScale = root.transform.localScale;
            }
            else
            {
                copy = Object.Instantiate(root, root.transform.parent);
                copy.transform.SetPositionAndRotation(root.transform.position, root.transform.rotation);
                copy.transform.localScale = root.transform.localScale;
            }
            copy.name = root.name;
            Undo.RegisterCreatedObjectUndo(copy, "BuildKit Stack");
            return copy;
        }
    }
}
