using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace BuildKit.Editor
{
    /// <summary>Click to drop one module. Hold the mouse and drag to paint many.</summary>
    [EditorTool("BuildKit Place")]
    public class PlaceTool : BuildKitToolBase
    {
        Vector3 lastPainted;
        bool painting;
        Transform paintGroup;

        public override GUIContent toolbarIcon => new GUIContent("Place", "BuildKit: place modules one at a time, drag to paint");

        public override void OnToolGUI(EditorWindow window)
        {
            if (!(window is SceneView)) return;
            if (!HasSource)
            {
                NoSourceGUI("Place");
                return;
            }

            Event e = Event.current;
            if (HandleCommonKeys(e)) return;

            int id = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(id);

            DrawStatus(StatusLine("Place"));

            if (!TryGetGroundPoint(e.mousePosition, out Vector3 ground)) return;
            Vector3 snapped = BuildKitSession.Snap(ground);
            DrawCursorContext(snapped, BuildKitSession.RotationSteps);
            DrawGhost(snapped, BuildKitSession.RotationSteps);

            switch (e.type)
            {
                case EventType.MouseMove:
                    window.Repaint();
                    break;

                case EventType.MouseDown:
                    if (e.button == 0 && !e.alt)
                    {
                        GUIUtility.hotControl = id;
                        painting = true;
                        paintGroup = BuildKitSession.Parent;
                        PlaceAt(snapped);
                        e.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (painting && e.button == 0 && snapped != lastPainted)
                    {
                        PlaceAt(snapped);
                        e.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (painting && e.button == 0)
                    {
                        painting = false;
                        GUIUtility.hotControl = 0;
                        e.Use();
                    }
                    break;
            }
        }

        void PlaceAt(Vector3 snapped)
        {
            lastPainted = snapped;
            if (Blocked(snapped, BuildKitSession.RotationSteps)) return;
            BuildKitSession.Place(snapped, BuildKitSession.RotationSteps, paintGroup);
            SnapFeedback.Invalidate();
        }
    }
}
