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
        static readonly Color TouchColor = new Color(0.3f, 1f, 0.45f, 1f);
        static readonly Color OverlapColor = new Color(1f, 0.3f, 0.3f, 1f);
        static readonly Color OverlapFill = new Color(1f, 0.3f, 0.3f, 0.18f);

        /// <summary>Feedback per cell gets expensive on big drags, so above this count only the outline is drawn.</summary>
        protected const int MaxFeedbackCells = 96;

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

        /// <summary>
        /// Works out where the ghost goes for the current cursor. In Auto or Face mode a placed module under
        /// the cursor wins and the ghost sits flush against that face; otherwise the ground grid is used.
        /// </summary>
        protected bool TryResolveCursor(Event e, int steps, out Vector3 bottomCenter, out bool faceSnapped, out FaceSnap.Hit hit)
        {
            hit = default;
            faceSnapped = false;
            bottomCenter = default;

            if (BuildKitSession.SnapMode != SnapMode.Grid)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                if (FaceSnap.TryHit(ray, out hit))
                {
                    Vector3 size = BuildKitSession.RotatedSize(steps);
                    bottomCenter = FaceSnap.Resolve(hit, size, e.shift, e.control);
                    faceSnapped = true;
                    return true;
                }
                if (BuildKitSession.SnapMode == SnapMode.Face) return false;
            }

            if (!TryGetGroundPoint(e.mousePosition, out Vector3 ground)) return false;
            bottomCenter = BuildKitSession.Snap(ground, steps);
            return true;
        }

        /// <summary>Handles R, M, G, PageUp, PageDown, 1 to 9 and Esc. Returns true if the event was used.</summary>
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
                case KeyCode.G:
                    BuildKitSession.CycleSnapMode();
                    e.Use();
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

        /// <summary>
        /// Draws one ghost cell. With feedback on, shared faces with placed modules light up green
        /// and the ghost turns red when it overlaps something.
        /// </summary>
        protected SnapFeedback.Result DrawGhost(Vector3 bottomCenter, int steps, bool withFeedback = true)
        {
            Vector3 size = BuildKitSession.RotatedSize(steps);
            var result = new SnapFeedback.Result();
            if (withFeedback && BuildKitSession.ShowContacts)
                result = SnapFeedback.DrawContacts(bottomCenter, size);

            Color line = result.overlaps ? OverlapColor : (result.touching > 0 ? TouchColor : GhostColor);
            Color fill = result.overlaps ? OverlapFill : GhostFill;

            Vector3 center = bottomCenter + Vector3.up * (size.y * 0.5f);
            Handles.color = line;
            Handles.DrawWireCube(center, size);

            Vector3 half = new Vector3(size.x * 0.5f, 0f, size.z * 0.5f);
            var verts = new[]
            {
                bottomCenter + new Vector3(-half.x, 0f, -half.z),
                bottomCenter + new Vector3(-half.x, 0f, half.z),
                bottomCenter + new Vector3(half.x, 0f, half.z),
                bottomCenter + new Vector3(half.x, 0f, -half.z)
            };
            Handles.DrawSolidRectangleWithOutline(verts, fill, Color.clear);
            return result;
        }

        /// <summary>Grid overlay and level marker around the cursor cell. Draw this before the ghost.</summary>
        protected void DrawCursorContext(Vector3 bottomCenter, int steps)
        {
            Vector3 cell = BuildKitSession.RotatedSize(steps);
            if (BuildKitSession.ShowGrid) SnapFeedback.DrawGrid(bottomCenter, cell);
            SnapFeedback.DrawLevelMarker(bottomCenter, cell, BuildKitSession.Level, BuildKitSession.GroundY);
        }

        /// <summary>True when placement should skip this cell because something already occupies it.</summary>
        protected bool Blocked(Vector3 bottomCenter, int steps)
        {
            return BuildKitSession.AvoidOverlap && SnapFeedback.IsOccupied(bottomCenter, BuildKitSession.RotatedSize(steps));
        }

        protected void DrawStatus(string text)
        {
            Handles.BeginGUI();
            var style = new GUIStyle(EditorStyles.helpBox) { fontSize = 11 };
            GUILayout.BeginArea(new Rect(10, 10, 420, 64));
            GUILayout.Label(text, style);
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        protected string StatusLine(string toolName)
        {
            string src = BuildKitSession.Source != null ? BuildKitSession.Source.name : "(none)";
            return $"BuildKit {toolName}  |  {src}  |  Snap {BuildKitSession.SnapMode}  |  Level {BuildKitSession.Level}  |  Rot {BuildKitSession.RotationSteps * 90}" +
                   (BuildKitSession.Mirror ? "  |  Mirrored\n" : "\n") +
                   "R rotate, M mirror, G snap mode, PgUp/PgDn level, 1-9 variants, Esc exit\n" +
                   "Face snap: Shift half step, Ctrl free slide";
        }

        protected void NoSourceGUI(string toolName)
        {
            DrawStatus($"BuildKit {toolName}: pick and analyze a source in Tools > BuildKit > Window");
        }
    }
}
