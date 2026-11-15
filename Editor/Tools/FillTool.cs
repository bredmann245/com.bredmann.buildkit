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

            if (dragging)
            {
                List<Vector3> cells = ComputeCells(startGround, endGround);
                foreach (var cell in cells)
                    DrawGhost(cell, BuildKitSession.RotationSteps);
            }
            else
            {
                DrawGhost(BuildKitSession.Snap(ground), BuildKitSession.RotationSteps);
            }
        }

        void Commit()
        {
            List<Vector3> cells = ComputeCells(startGround, endGround);
            if (cells.Count == 0) return;

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Transform parent = BuildKitSession.GetOrCreateGroup("BuildKit Fill");
            foreach (var cell in cells)
                BuildKitSession.Place(cell, BuildKitSession.RotationSteps, parent);
            Undo.CollapseUndoOperations(group);
            Undo.SetCurrentGroupName("BuildKit Fill");
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
