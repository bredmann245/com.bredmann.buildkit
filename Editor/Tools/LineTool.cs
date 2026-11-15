using System.Collections.Generic;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace BuildKit.Editor
{
    /// <summary>Drag to repeat the module along the longest axis of the drag.</summary>
    [EditorTool("BuildKit Line")]
    public class LineTool : BuildKitToolBase
    {
        bool dragging;
        Vector3 startGround;
        Vector3 endGround;

        public override GUIContent toolbarIcon => new GUIContent("Line", "BuildKit: drag to lay modules in a line");

        public override void OnToolGUI(EditorWindow window)
        {
            if (!(window is SceneView)) return;
            if (!HasSource)
            {
                NoSourceGUI("Line");
                return;
            }

            Event e = Event.current;
            if (HandleCommonKeys(e)) return;

            int id = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(id);

            DrawStatus(StatusLine("Line") + "  |  drag to lay a line");

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
                foreach (var cell in ComputeCells(startGround, endGround, out int steps))
                    DrawGhost(cell, steps);
            }
            else
            {
                DrawGhost(BuildKitSession.Snap(ground), BuildKitSession.RotationSteps);
            }
        }

        void Commit()
        {
            List<Vector3> cells = ComputeCells(startGround, endGround, out int steps);
            if (cells.Count == 0) return;

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Transform parent = BuildKitSession.GetOrCreateGroup("BuildKit Line");
            foreach (var cell in cells)
                BuildKitSession.Place(cell, steps, parent);
            Undo.CollapseUndoOperations(group);
            Undo.SetCurrentGroupName("BuildKit Line");
        }

        /// <summary>
        /// Cells along the dominant axis between two ground points. When AlignToLine is on,
        /// a line along Z rotates the module 90 degrees so walls follow the drag.
        /// </summary>
        static List<Vector3> ComputeCells(Vector3 a, Vector3 b, out int steps)
        {
            Vector3 delta = b - a;
            bool alongZ = Mathf.Abs(delta.z) > Mathf.Abs(delta.x);

            steps = BuildKitSession.RotationSteps;
            if (BuildKitSession.AlignToLine && alongZ) steps = (steps + 1) % 4;

            Vector3 cell = BuildKitSession.RotatedSize(steps);
            Vector3 start = BuildKitSession.Snap(a, steps);
            Vector3 end = BuildKitSession.Snap(b, steps);

            float stepSize = alongZ ? cell.z : cell.x;
            float length = alongZ ? end.z - start.z : end.x - start.x;
            int count = Mathf.RoundToInt(Mathf.Abs(length) / stepSize) + 1;
            float sign = Mathf.Sign(length == 0f ? 1f : length);
            Vector3 axis = alongZ ? Vector3.forward : Vector3.right;

            var cells = new List<Vector3>(count);
            for (int i = 0; i < count; i++)
                cells.Add(start + axis * (sign * i * stepSize));
            return cells;
        }
    }
}
