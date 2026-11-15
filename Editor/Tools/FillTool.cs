using System.Collections.Generic;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace BuildKit.Editor
{
    /// <summary>Drag a rectangle to fill it with modules. Good for floors and roofs.</summary>
    [EditorTool("BuildKit Fill")]
    public class FillTool : BuildKitToolBase
    {
        const int MaxCells = 4096;

        bool dragging;
        Vector3 startGround;
        Vector3 endGround;

        public override GUIContent toolbarIcon => new GUIContent("Fill", "BuildKit: drag a rectangle to fill with modules");

        public override void OnToolGUI(EditorWindow window)
        {
            if (!(window is SceneView)) return;
            if (!HasSource)
            {
                NoSourceGUI("Fill");
                return;
            }

            Event e = Event.current;
            if (HandleCommonKeys(e)) return;

            int id = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(id);

            DrawStatus(StatusLine("Fill") + "  |  drag a rectangle");

            if (!TryGetGroundPoint(e.mousePosition, out Vector3 ground)) return;

            switch (e.type)
            {
                case EventType.MouseMove:
                    window.Repaint();
                    break;

                case EventType.MouseDown:
                    if (e.button == 0 && !e.alt)
                    {
                        GUIUtility.hotControl = id;
                        dragging = true;
                        startGround = ground;
                        endGround = ground;
                        e.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (dragging && e.button == 0)
                    {
                        endGround = ground;
                        e.Use();
                        window.Repaint();
                    }
                    break;

                case EventType.MouseUp:
                    if (dragging && e.button == 0)
                    {
                        dragging = false;
                        GUIUtility.hotControl = 0;
                        endGround = ground;
                        Commit();
                        e.Use();
                    }
                    break;
            }

            int steps = BuildKitSession.RotationSteps;
            if (dragging)
            {
                List<Vector3> cells = ComputeCells(startGround, endGround);
                bool feedback = cells.Count <= MaxFeedbackCells;
                if (cells.Count > 0) DrawCursorContext(BuildKitSession.Snap(endGround, steps), steps);
                foreach (var cell in cells)
                    DrawGhost(cell, steps, feedback);
                DrawCount(cells.Count);
            }
            else
            {
                Vector3 snapped = BuildKitSession.Snap(ground);
                DrawCursorContext(snapped, steps);
                DrawGhost(snapped, steps);
            }
        }

        void Commit()
        {
            List<Vector3> cells = ComputeCells(startGround, endGround);
            if (cells.Count == 0) return;

            int steps = BuildKitSession.RotationSteps;
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Transform parent = BuildKitSession.GetOrCreateGroup("BuildKit Fill");
            foreach (var cell in cells)
            {
                if (Blocked(cell, steps)) continue;
                BuildKitSession.Place(cell, steps, parent);
            }
            Undo.CollapseUndoOperations(group);
            Undo.SetCurrentGroupName("BuildKit Fill");
            SnapFeedback.Invalidate();
        }

        void DrawCount(int count)
        {
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(10, 76, 200, 24));
            GUILayout.Label(count == 0 ? "too many cells" : $"{count} modules", EditorStyles.helpBox);
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        static List<Vector3> ComputeCells(Vector3 a, Vector3 b)
        {
            int steps = BuildKitSession.RotationSteps;
            Vector3 cell = BuildKitSession.RotatedSize(steps);
            Vector3 start = BuildKitSession.Snap(a, steps);
            Vector3 end = BuildKitSession.Snap(b, steps);

            float minX = Mathf.Min(start.x, end.x);
            float maxX = Mathf.Max(start.x, end.x);
            float minZ = Mathf.Min(start.z, end.z);
            float maxZ = Mathf.Max(start.z, end.z);

            int countX = Mathf.RoundToInt((maxX - minX) / cell.x) + 1;
            int countZ = Mathf.RoundToInt((maxZ - minZ) / cell.z) + 1;

            var cells = new List<Vector3>();
            if (countX * countZ > MaxCells) return cells;

            for (int ix = 0; ix < countX; ix++)
            for (int iz = 0; iz < countZ; iz++)
                cells.Add(new Vector3(minX + ix * cell.x, start.y, minZ + iz * cell.z));

            return cells;
        }
    }
}
