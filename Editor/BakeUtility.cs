using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BuildKit.Editor
{
    /// <summary>Merges many objects into one mesh asset, grouped by material.</summary>
    public static class BakeUtility
    {
        public const string DefaultFolder = "Assets/BuildKit/Baked";

        public static GameObject Bake(GameObject[] roots, string name, bool addCollider, bool disableSources, string folder = DefaultFolder)
        {
            if (roots == null || roots.Length == 0) return null;
            if (string.IsNullOrWhiteSpace(name)) name = "BakedBuild";

            var seen = new HashSet<MeshFilter>();
            var byMaterial = new Dictionary<Material, List<CombineInstance>>();
            var materialOrder = new List<Material>();

            foreach (GameObject root in roots)
            {
                if (root == null) continue;
                foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(false))
                {
                    if (mf.sharedMesh == null || !seen.Add(mf)) continue;
                    var mr = mf.GetComponent<MeshRenderer>();
                    if (mr == null || !mr.enabled) continue;

                    Material[] mats = mr.sharedMaterials;
                    Mesh mesh = mf.sharedMesh;
                    for (int sub = 0; sub < mesh.subMeshCount; sub++)
                    {
                        Material mat = mats.Length > 0 ? mats[Mathf.Min(sub, mats.Length - 1)] : null;
                        if (mat == null) continue;
                        if (!byMaterial.TryGetValue(mat, out var list))
                        {
                            list = new List<CombineInstance>();
                            byMaterial[mat] = list;
                            materialOrder.Add(mat);
                        }
                        list.Add(new CombineInstance
                        {
                            mesh = mesh,
                            subMeshIndex = sub,
                            transform = mf.transform.localToWorldMatrix
                        });
                    }
                }
            }

            if (materialOrder.Count == 0)
            {
                Debug.LogWarning("BuildKit Bake: no mesh renderers found in the selection.");
                return null;
            }

            var perMaterial = new List<CombineInstance>(materialOrder.Count);
            var temp = new List<Mesh>();
            foreach (Material mat in materialOrder)
            {
                var m = new Mesh { indexFormat = IndexFormat.UInt32 };
                m.CombineMeshes(byMaterial[mat].ToArray(), true, true);
                temp.Add(m);
                perMaterial.Add(new CombineInstance { mesh = m, transform = Matrix4x4.identity });
            }

            var final = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            final.CombineMeshes(perMaterial.ToArray(), false, false);
            final.RecalculateBounds();
            foreach (Mesh m in temp) Object.DestroyImmediate(m);

            EnsureFolder(folder);
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{name}.asset");
            AssetDatabase.CreateAsset(final, path);
            AssetDatabase.SaveAssets();

            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "BuildKit Bake");
            go.AddComponent<MeshFilter>().sharedMesh = final;
            go.AddComponent<MeshRenderer>().sharedMaterials = materialOrder.ToArray();
            if (addCollider) go.AddComponent<MeshCollider>().sharedMesh = final;

            if (disableSources)
            {
                foreach (GameObject root in roots)
                {
                    if (root == null) continue;
                    Undo.RecordObject(root, "BuildKit Bake");
                    root.SetActive(false);
                }
            }

            Selection.activeGameObject = go;
            return go;
        }

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
