using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace BuildKit.Editor
{
    /// <summary>
    /// Shared behavior for the scene tools: ground plane raycast, hotkeys and ghost drawing.
    /// </summary>
    public abstract class BuildKitToolBase : EditorTool
    {
        static readonly Color GhostColor = new Color(0.2f, 0.9f, 1f, 1f);
        static readonly Color GhostFill = new Color(0.2f, 0.9f, 1f, 0.12f);

        protected bool HasSource => BuildKitSession.HasSource;

        protected bool TryGetGroundPoint(Vector2 mousePosition, out Vector3 point)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            var plane = new Plane(Vector3.up, new Vector3(0f, BuildKitSession.LevelY, 0f));
            if (plane.Raycast(ray, out float distance))
            {
                point = ray.GetPoint(distance);
                return true;
            }
            point = default;
            return false;
        }

        /// <summary>Handles R, M, PageUp, PageDown, 1 to 9 and Esc. Returns true if the event was used.</summary>
        protected bool HandleCommonKeys(Event e)
        {
            if (e.type != EventType.KeyDown) return false;

            switch (e.keyCode)
            {
                case KeyCode.R:
                    BuildKitSession.RotateBy(e.shift ? -1 : 1);
                    e.Use();
                    return true;
                case KeyCode.M:
                    BuildKitSession.Mirror = !BuildKitSession.Mirror;
                    e.Use();
                    SceneView.RepaintAll();
                    return true;
                case KeyCode.PageUp:
                    BuildKitSession.Level++;
                    e.Use();
                    SceneView.RepaintAll();
                    return true;
                case KeyCode.PageDown:
                    BuildKitSession.Level--;
                    e.Use();
                    SceneView.RepaintAll();
                    return true;
                case KeyCode.Escape:
                    ToolManager.RestorePreviousPersistentTool();
                    e.Use();
                    return true;
            }

            if (e.keyCode >= KeyCode.Alpha1 && e.keyCode <= KeyCode.Alpha9)
            {
                BuildKitSession.SelectVariant(e.keyCode - KeyCode.Alpha1);
                e.Use();
                return true;
            }

            return false;
        }

        protected void DrawGhost(Vector3 bottomCenter, int steps)
        {
            Vector3 size = BuildKitSession.RotatedSize(steps);
            Vector3 center = bottomCenter + Vector3.up * (size.y * 0.5f);
            Handles.color = GhostColor;
            Handles.DrawWireCube(center, size);

            Vector3 half = new Vector3(size.x * 0.5f, 0f, size.z * 0.5f);
            var verts = new[]
            {
                bottomCenter + new Vector3(-half.x, 0f, -half.z),
                bottomCenter + new Vector3(-half.x, 0f, half.z),
                bottomCenter + new Vector3(half.x, 0f, half.z),
                bottomCenter + new Vector3(half.x, 0f, -half.z)
            };
            Handles.DrawSolidRectangleWithOutline(verts, GhostFill, Color.clear);
        }

        protected void DrawStatus(string text)
        {
            Handles.BeginGUI();
            var style = new GUIStyle(EditorStyles.helpBox) { fontSize = 11 };
            GUILayout.BeginArea(new Rect(10, 10, 360, 60));
            GUILayout.Label(text, style);
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        protected string StatusLine(string toolName)
        {
            string src = BuildKitSession.Source != null ? BuildKitSession.Source.name : "(none)";
            return $"BuildKit {toolName}  |  {src}  |  Level {BuildKitSession.Level}  |  Rot {BuildKitSession.RotationSteps * 90}  |  " +
                   (BuildKitSession.Mirror ? "Mirrored\n" : "\n") +
                   "R rotate, M mirror, PgUp/PgDn level, 1-9 variants, Esc exit";
        }

        protected void NoSourceGUI(string toolName)
        {
            DrawStatus($"BuildKit {toolName}: pick and analyze a source in Tools > BuildKit > Window");
        }
    }
}
