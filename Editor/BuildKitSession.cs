using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BuildKit.Editor
{
    /// <summary>
    /// Shared editor state for the BuildKit tools: the active source, grid, level, rotation and mirror.
    /// </summary>
    public static class BuildKitSession
    {
        const string PrefKey = "BuildKit.";

        static GameObject source;
        public static readonly List<GameObject> Variants = new List<GameObject>();

        public static GameObject Source
        {
            get => source;
            set
            {
                if (source == value) return;
                source = value;
                SceneView.RepaintAll();
            }
        }

        public static BuildModule SourceModule => source != null ? source.GetComponent<BuildModule>() : null;
        public static bool HasSource => SourceModule != null && SourceModule.analyzed;

        public static bool UseModuleSizeAsGrid = true;
        public static Vector3 CustomGrid = Vector3.one;
        public static float GroundY = 0f;
        public static int Level;
        public static int RotationSteps;
        public static bool Mirror;
        public static bool AlignToLine = true;
        public static bool ShowGrid = true;
        public static bool ShowContacts = true;
        public static bool AvoidOverlap = true;
        public static Transform Parent;

        public static Quaternion Rotation => RotationFor(RotationSteps);
        public static Quaternion RotationFor(int steps) => Quaternion.Euler(0f, ((steps % 4) + 4) % 4 * 90f, 0f);
        public static Vector3 MirrorScale => new Vector3(Mirror ? -1f : 1f, 1f, 1f);

        /// <summary>World size of the source as it will sit after rotation. Odd rotation steps swap X and Z.</summary>
        public static Vector3 RotatedSize(int steps)
        {
            Vector3 s = UseModuleSizeAsGrid && SourceModule != null ? SourceModule.WorldSize : CustomGrid;
            s = new Vector3(Mathf.Max(s.x, 0.001f), Mathf.Max(s.y, 0.001f), Mathf.Max(s.z, 0.001f));
            bool odd = (((steps % 4) + 4) % 4) % 2 == 1;
            return odd ? new Vector3(s.z, s.y, s.x) : s;
        }

        public static Vector3 Cell => RotatedSize(RotationSteps);

        public static float LevelY => GroundY + Level * Cell.y;

        /// <summary>Snaps a world point so the module occupies a whole grid cell, measured from the world origin.</summary>
        public static Vector3 Snap(Vector3 p, int steps)
        {
            Vector3 cell = RotatedSize(steps);
            float x = (Mathf.Floor(p.x / cell.x) + 0.5f) * cell.x;
            float z = (Mathf.Floor(p.z / cell.z) + 0.5f) * cell.z;
            return new Vector3(x, GroundY + Level * cell.y, z);
        }

        public static Vector3 Snap(Vector3 p) => Snap(p, RotationSteps);

        /// <summary>Creates one copy of the source with its bottom center at the given world point.</summary>
        public static GameObject Place(Vector3 bottomCenter, int steps, Transform parent = null)
        {
            BuildModule module = SourceModule;
            if (module == null) return null;

            GameObject go;
            if (PrefabUtility.IsPartOfPrefabAsset(source))
                go = (GameObject)PrefabUtility.InstantiatePrefab(source);
            else
                go = Object.Instantiate(source);

            go.name = source.name;
            Undo.RegisterCreatedObjectUndo(go, "BuildKit Place");

            Transform t = go.transform;
            Transform p = parent != null ? parent : Parent;
            if (p != null) t.SetParent(p, true);

            t.rotation = RotationFor(steps);
            Vector3 baseScale = source.transform.localScale;
            t.localScale = Vector3.Scale(baseScale, MirrorScale);

            BuildModule placed = go.GetComponent<BuildModule>();
            Vector3 local = placed != null ? placed.BottomCenterLocal : module.BottomCenterLocal;
            Vector3 offset = t.rotation * Vector3.Scale(local, t.lossyScale);
            t.position = bottomCenter - offset;

            return go;
        }

        public static GameObject Place(Vector3 bottomCenter) => Place(bottomCenter, RotationSteps);

        /// <summary>Returns the parent to use for a batch, creating a group object when none is set.</summary>
        public static Transform GetOrCreateGroup(string name)
        {
            if (Parent != null) return Parent;
            var group = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(group, "BuildKit Group");
            return group.transform;
        }

        public static void RotateBy(int delta)
        {
            RotationSteps = ((RotationSteps + delta) % 4 + 4) % 4;
            SceneView.RepaintAll();
        }

        public static void SelectVariant(int index)
        {
            if (index < 0 || index >= Variants.Count || Variants[index] == null) return;
            Source = Variants[index];
        }

        public static void SavePrefs()
        {
            EditorPrefs.SetBool(PrefKey + "UseModuleSize", UseModuleSizeAsGrid);
            EditorPrefs.SetFloat(PrefKey + "GridX", CustomGrid.x);
            EditorPrefs.SetFloat(PrefKey + "GridY", CustomGrid.y);
            EditorPrefs.SetFloat(PrefKey + "GridZ", CustomGrid.z);
            EditorPrefs.SetFloat(PrefKey + "GroundY", GroundY);
            EditorPrefs.SetBool(PrefKey + "AlignToLine", AlignToLine);
            EditorPrefs.SetBool(PrefKey + "ShowGrid", ShowGrid);
            EditorPrefs.SetBool(PrefKey + "ShowContacts", ShowContacts);
            EditorPrefs.SetBool(PrefKey + "AvoidOverlap", AvoidOverlap);
            if (source != null && PrefabUtility.IsPartOfPrefabAsset(source))
                EditorPrefs.SetString(PrefKey + "Source", AssetDatabase.GetAssetPath(source));
        }

        public static void LoadPrefs()
        {
            UseModuleSizeAsGrid = EditorPrefs.GetBool(PrefKey + "UseModuleSize", true);
            CustomGrid = new Vector3(
                EditorPrefs.GetFloat(PrefKey + "GridX", 1f),
                EditorPrefs.GetFloat(PrefKey + "GridY", 1f),
                EditorPrefs.GetFloat(PrefKey + "GridZ", 1f));
            GroundY = EditorPrefs.GetFloat(PrefKey + "GroundY", 0f);
            AlignToLine = EditorPrefs.GetBool(PrefKey + "AlignToLine", true);
            ShowGrid = EditorPrefs.GetBool(PrefKey + "ShowGrid", true);
            ShowContacts = EditorPrefs.GetBool(PrefKey + "ShowContacts", true);
            AvoidOverlap = EditorPrefs.GetBool(PrefKey + "AvoidOverlap", true);
            string path = EditorPrefs.GetString(PrefKey + "Source", "");
            if (source == null && !string.IsNullOrEmpty(path))
                source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
    }
}
