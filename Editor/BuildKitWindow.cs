using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace BuildKit.Editor
{
    public class BuildKitWindow : EditorWindow
    {
        int stackCount = 3;
        string bakeName = "BakedBuild";
        bool bakeCollider = true;
        bool bakeDisableSources = true;
        Vector2 scroll;

        [MenuItem("Tools/BuildKit/Window")]
        public static void Open()
        {
            var w = GetWindow<BuildKitWindow>("BuildKit");
            w.minSize = new Vector2(280, 420);
        }

        void OnEnable()
        {
            BuildKitSession.LoadPrefs();
        }

        void OnDisable()
        {
            BuildKitSession.SavePrefs();
        }

        void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            DrawSource();
            EditorGUILayout.Space(8);
            DrawGrid();
            EditorGUILayout.Space(8);
            DrawTools();
            EditorGUILayout.Space(8);
            DrawVariants();
            EditorGUILayout.Space(8);
            DrawStack();
            EditorGUILayout.Space(8);
            DrawBake();

            EditorGUILayout.EndScrollView();
        }

        void DrawSource()
        {
            EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            var next = (GameObject)EditorGUILayout.ObjectField("Module", BuildKitSession.Source, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck())
            {
                BuildKitSession.Source = next;
                BuildKitSession.SavePrefs();
            }

            BuildModule module = BuildKitSession.SourceModule;
            if (BuildKitSession.Source == null)
            {
                EditorGUILayout.HelpBox("Drag in a prefab or scene object. Any mesh works: a wall, a block, a crate.", MessageType.Info);
                return;
            }

            if (module == null || !module.analyzed)
            {
                EditorGUILayout.HelpBox("Press Analyze to measure the renderers and add a BuildModule component.", MessageType.Warning);
            }
            else
            {
                Vector3 s = module.WorldSize;
                EditorGUILayout.LabelField("Size", $"{s.x:0.###} x {s.y:0.###} x {s.z:0.###}");
                EditorGUILayout.LabelField("Kind", module.kind.ToString());
            }

            if (GUILayout.Button("Analyze"))
                Analyze(BuildKitSession.Source);
        }

        void DrawGrid()
        {
            EditorGUILayout.LabelField("Grid", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            BuildKitSession.UseModuleSizeAsGrid = EditorGUILayout.Toggle("Use module size as grid", BuildKitSession.UseModuleSizeAsGrid);
            using (new EditorGUI.DisabledScope(BuildKitSession.UseModuleSizeAsGrid))
                BuildKitSession.CustomGrid = EditorGUILayout.Vector3Field("Cell size", BuildKitSession.CustomGrid);
            BuildKitSession.GroundY = EditorGUILayout.FloatField("Ground Y", BuildKitSession.GroundY);
            BuildKitSession.Level = EditorGUILayout.IntField("Level", BuildKitSession.Level);
            BuildKitSession.RotationSteps = EditorGUILayout.IntPopup("Rotation", BuildKitSession.RotationSteps,
                new[] { "0", "90", "180", "270" }, new[] { 0, 1, 2, 3 });
            BuildKitSession.Mirror = EditorGUILayout.Toggle("Mirror X", BuildKitSession.Mirror);
            BuildKitSession.AlignToLine = EditorGUILayout.Toggle("Line tool aligns walls", BuildKitSession.AlignToLine);
            BuildKitSession.Parent = (Transform)EditorGUILayout.ObjectField("Parent", BuildKitSession.Parent, typeof(Transform), true);
            if (EditorGUI.EndChangeCheck())
            {
                BuildKitSession.SavePrefs();
                SceneView.RepaintAll();
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Feedback", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            BuildKitSession.ShowGrid = EditorGUILayout.Toggle("Show grid around cursor", BuildKitSession.ShowGrid);
            BuildKitSession.ShowContacts = EditorGUILayout.Toggle("Highlight touching faces", BuildKitSession.ShowContacts);
            BuildKitSession.AvoidOverlap = EditorGUILayout.Toggle("Skip occupied cells", BuildKitSession.AvoidOverlap);
            if (EditorGUI.EndChangeCheck())
            {
                BuildKitSession.SavePrefs();
                SceneView.RepaintAll();
            }
        }

        void DrawTools()
        {
            EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(!BuildKitSession.HasSource))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Place", GUILayout.Height(28))) ToolManager.SetActiveTool<PlaceTool>();
                if (GUILayout.Button("Line", GUILayout.Height(28))) ToolManager.SetActiveTool<LineTool>();
                if (GUILayout.Button("Fill", GUILayout.Height(28))) ToolManager.SetActiveTool<FillTool>();
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.HelpBox("R rotate, M mirror, PgUp/PgDn level, 1-9 variants, Esc exit tool.", MessageType.None);
        }

        void DrawVariants()
        {
            EditorGUILayout.LabelField("Variants (keys 1-9)", EditorStyles.boldLabel);
            var list = BuildKitSession.Variants;
            for (int i = 0; i < list.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField((i + 1).ToString(), GUILayout.Width(16));
                list[i] = (GameObject)EditorGUILayout.ObjectField(list[i], typeof(GameObject), true);
                if (GUILayout.Button("Use", GUILayout.Width(40)))
                {
                    if (list[i] != null)
                    {
                        if (list[i].GetComponent<BuildModule>() == null) Analyze(list[i]);
                        BuildKitSession.Source = list[i];
                    }
                }
                if (GUILayout.Button("x", GUILayout.Width(22)))
                {
                    list.RemoveAt(i);
                    EditorGUILayout.EndHorizontal();
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (list.Count < 9 && GUILayout.Button("Add variant slot"))
                list.Add(BuildKitSession.Source);
        }

        void DrawStack()
        {
            EditorGUILayout.LabelField("Stack", EditorStyles.boldLabel);
            stackCount = EditorGUILayout.IntSlider("Copies", stackCount, 1, 50);
            GameObject[] selected = Selection.gameObjects;
            float height = StackUtility.GuessHeight(selected);
            EditorGUILayout.LabelField("Height per copy", height > 0f ? height.ToString("0.###") : "(select objects)");
            using (new EditorGUI.DisabledScope(selected.Length == 0 || height <= 0f))
            {
                if (GUILayout.Button($"Stack selection {stackCount}x up"))
                {
                    var made = StackUtility.Stack(selected, stackCount, height);
                    Selection.objects = made.ToArray();
                }
            }
        }

        void DrawBake()
        {
            EditorGUILayout.LabelField("Bake", EditorStyles.boldLabel);
            bakeName = EditorGUILayout.TextField("Name", bakeName);
            bakeCollider = EditorGUILayout.Toggle("Add mesh collider", bakeCollider);
            bakeDisableSources = EditorGUILayout.Toggle("Disable sources", bakeDisableSources);
            using (new EditorGUI.DisabledScope(Selection.gameObjects.Length == 0))
            {
                if (GUILayout.Button("Bake selection to one mesh"))
                    BakeUtility.Bake(Selection.gameObjects, bakeName, bakeCollider, bakeDisableSources);
            }
            EditorGUILayout.HelpBox($"Mesh assets are saved under {BakeUtility.DefaultFolder}.", MessageType.None);
        }

        void OnSelectionChange()
        {
            Repaint();
        }

        /// <summary>Adds a BuildModule if needed and measures it. Works on prefab assets and scene objects.</summary>
        public static void Analyze(GameObject target)
        {
            if (target == null) return;

            if (PrefabUtility.IsPartOfPrefabAsset(target))
            {
                string path = AssetDatabase.GetAssetPath(target);
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var module = contents.GetComponent<BuildModule>();
                    if (module == null) module = contents.AddComponent<BuildModule>();
                    module.Analyze();
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }
            else
            {
                var module = target.GetComponent<BuildModule>();
                if (module == null) module = Undo.AddComponent<BuildModule>(target);
                Undo.RecordObject(module, "BuildKit Analyze");
                module.Analyze();
                EditorUtility.SetDirty(module);
            }

            SceneView.RepaintAll();
        }
    }
}
