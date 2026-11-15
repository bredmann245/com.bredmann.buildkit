using UnityEditor;
using UnityEngine;

namespace BuildKit.Editor
{
    [CustomEditor(typeof(BuildModule))]
    [CanEditMultipleObjects]
    public class BuildModuleEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(6);

            if (GUILayout.Button("Analyze"))
            {
                foreach (Object t in targets)
                {
                    var module = (BuildModule)t;
                    Undo.RecordObject(module, "BuildKit Analyze");
                    module.Analyze();
                    EditorUtility.SetDirty(module);
                }
                SceneView.RepaintAll();
            }

            if (targets.Length == 1)
            {
                var module = (BuildModule)target;
                Vector3 s = module.WorldSize;
                EditorGUILayout.LabelField("World size", $"{s.x:0.###} x {s.y:0.###} x {s.z:0.###}");
                if (GUILayout.Button("Use as BuildKit source"))
                {
                    BuildKitSession.Source = module.gameObject;
                    BuildKitWindow.Open();
                }
            }
        }
    }
}
