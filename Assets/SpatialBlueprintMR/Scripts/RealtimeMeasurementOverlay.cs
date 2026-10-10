using UnityEngine;
using UnityEngine.InputSystem;

namespace SpatialBlueprintMR
{
    /// <summary>Shows live layout dimensions from above and wall clearance while walking through the layout.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class RealtimeMeasurementOverlay : MonoBehaviour
    {
        [SerializeField] private bool measurementsVisible = true;
        [SerializeField, Min(0.1f)] private float maximumClearanceMetres = 20f;
        [SerializeField] private LayerMask measurementCollisionLayers = Physics.DefaultRaycastLayers;
        [Header("Bird's-eye dimension guides")]
        [SerializeField, Min(0.1f)] private float minimumGuideOffsetMetres = 1.2f;
        [SerializeField, Min(0.1f)] private float maximumGuideOffsetMetres = 1.75f;
        [SerializeField, Range(0.05f, 0.25f)] private float guideOffsetAsLayoutFraction = 0.12f;
        [SerializeField, Min(0f)] private float labelOffsetPixels = 14f;

        private Camera _camera;
        private BlueprintModel _layout;
        private DesktopPreviewCameraController _cameraController;
        private static GUIStyle _dimensionLabelStyle;

        public bool MeasurementsVisible => measurementsVisible;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _cameraController = GetComponent<DesktopPreviewCameraController>();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
            {
                measurementsVisible = !measurementsVisible;
            }
            if (_layout == null)
            {
                _layout = FindFirstObjectByType<BlueprintModel>();
            }
            if (_cameraController == null)
            {
                _cameraController = GetComponent<DesktopPreviewCameraController>();
            }
        }

        private void OnGUI()
        {
            if (!measurementsVisible)
            {
                return;
            }

            var isBirdsEye = _cameraController != null && _cameraController.IsBirdsEyeView;
            var panel = new Rect(16, 204, 390, isBirdsEye ? 112 : 156);
            GUI.Box(panel, isBirdsEye ? "Bird's-eye measurements — live" : "Walkthrough measurements — live");

            if (_layout == null || !_layout.TryGetLayoutMeasurements(out var dimensions))
            {
                GUI.Label(new Rect(panel.x + 14, panel.y + 30, panel.width - 28, 40), "Load a blueprint to view live measurements.");
                return;
            }

            GUI.Label(new Rect(panel.x + 14, panel.y + 28, panel.width - 28, 22), $"Width: {FormatDistance(dimensions.x)}");
            GUI.Label(new Rect(panel.x + 14, panel.y + 50, panel.width - 28, 22), $"Length: {FormatDistance(dimensions.y)}");

            if (isBirdsEye)
            {
                DrawBirdsEyeDimensionGuides(dimensions);
                GUI.Label(new Rect(panel.x + 14, panel.y + 75, panel.width - 28, 22), "Updates when the plan is calibrated or scaled.");
                return;
            }

            var forward = Vector3.ProjectOnPlane(_camera.transform.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }
            else
            {
                forward.Normalize();
            }

            var right = Vector3.Cross(Vector3.up, forward).normalized;
            GUI.Label(new Rect(panel.x + 14, panel.y + 75, panel.width - 28, 22), $"Forward clearance: {MeasureClearance(forward)}");
            GUI.Label(new Rect(panel.x + 14, panel.y + 97, panel.width - 28, 22), $"Left / right clearance: {MeasureClearance(-right)} / {MeasureClearance(right)}");

            var fromCenter = _camera.transform.position - _layout.transform.position;
            var horizontalOffset = Vector3.Dot(fromCenter, _layout.transform.right);
            var depthOffset = Vector3.Dot(fromCenter, _layout.transform.forward);
            GUI.Label(new Rect(panel.x + 14, panel.y + 119, panel.width - 28, 22), $"From layout center — X: {FormatDistance(horizontalOffset)}, Z: {FormatDistance(depthOffset)}");
        }

        private string MeasureClearance(Vector3 direction)
        {
            if (Physics.Raycast(_camera.transform.position, direction, out var hit, maximumClearanceMetres,
                    measurementCollisionLayers, QueryTriggerInteraction.Ignore))
            {
                return FormatDistance(hit.distance);
            }
            return $"> {FormatDistance(maximumClearanceMetres)}";
        }

        /// <summary>Draws architectural-style dimension lines around the plan while looking straight down.</summary>
        private void DrawBirdsEyeDimensionGuides(Vector2 dimensions)
        {
            var localSize = _layout.LayoutSizeMetres;
            if (localSize.x <= 0f || localSize.y <= 0f)
            {
                return;
            }

            var halfWidth = localSize.x * 0.5f;
            var halfLength = localSize.y * 0.5f;
            var scale = _layout.transform.lossyScale;
            var horizontalScale = Mathf.Max(0.001f, (Mathf.Abs(scale.x) + Mathf.Abs(scale.z)) * 0.5f);
            var maximumGuideOffset = Mathf.Max(minimumGuideOffsetMetres, maximumGuideOffsetMetres);
            var desiredGuideOffset = Mathf.Max(minimumGuideOffsetMetres, Mathf.Max(dimensions.x, dimensions.y) * guideOffsetAsLayoutFraction);
            var guideOffset = Mathf.Min(desiredGuideOffset, maximumGuideOffset) / horizontalScale;

            var topLeft = _layout.transform.TransformPoint(new Vector3(-halfWidth, 0.02f, halfLength));
            var topRight = _layout.transform.TransformPoint(new Vector3(halfWidth, 0.02f, halfLength));
            var bottomRight = _layout.transform.TransformPoint(new Vector3(halfWidth, 0.02f, -halfLength));
            var widthStart = _layout.transform.TransformPoint(new Vector3(-halfWidth, 0.02f, halfLength + guideOffset));
            var widthEnd = _layout.transform.TransformPoint(new Vector3(halfWidth, 0.02f, halfLength + guideOffset));
            var lengthStart = _layout.transform.TransformPoint(new Vector3(halfWidth + guideOffset, 0.02f, -halfLength));
            var lengthEnd = _layout.transform.TransformPoint(new Vector3(halfWidth + guideOffset, 0.02f, halfLength));

            if (!TryWorldToGui(widthStart, out var widthStartGui) || !TryWorldToGui(widthEnd, out var widthEndGui) ||
                !TryWorldToGui(topLeft, out var topLeftGui) || !TryWorldToGui(topRight, out var topRightGui) ||
                !TryWorldToGui(lengthStart, out var lengthStartGui) || !TryWorldToGui(lengthEnd, out var lengthEndGui) ||
                !TryWorldToGui(bottomRight, out var bottomRightGui))
            {
                return;
            }

            DrawDimensionGuide(widthStartGui, widthEndGui, topLeftGui, topRightGui, $"WIDTH  {FormatDistance(dimensions.x)}", 0f);
            DrawDimensionGuide(lengthStartGui, lengthEndGui, bottomRightGui, topRightGui, $"LENGTH  {FormatDistance(dimensions.y)}", 90f);
        }

        private bool TryWorldToGui(Vector3 worldPoint, out Vector2 guiPoint)
        {
            var screenPoint = _camera.WorldToScreenPoint(worldPoint);
            if (screenPoint.z <= 0f)
            {
                guiPoint = default;
                return false;
            }

            guiPoint = new Vector2(screenPoint.x, Screen.height - screenPoint.y);
            return true;
        }

        private void DrawDimensionGuide(Vector2 start, Vector2 end, Vector2 extensionStart, Vector2 extensionEnd, string label, float labelRotationDegrees)
        {
            var color = new Color(0.15f, 0.92f, 1f, 0.95f);
            DrawLine(start, end, color, 2f);
            DrawLine(start, extensionStart, color, 1f);
            DrawLine(end, extensionEnd, color, 1f);

            var direction = (end - start).normalized;
            var tick = new Vector2(-direction.y, direction.x) * 6f;
            DrawLine(start - tick, start + tick, color, 2f);
            DrawLine(end - tick, end + tick, color, 2f);

            var outward = ((start - extensionStart) + (end - extensionEnd)).normalized;
            var midpoint = (start + end) * 0.5f + outward * labelOffsetPixels;
            var previousMatrix = GUI.matrix;
            var previousColor = GUI.color;
            GUIUtility.RotateAroundPivot(labelRotationDegrees, midpoint);
            GUI.color = new Color(0.02f, 0.12f, 0.16f, 0.92f);
            GUI.DrawTexture(new Rect(midpoint.x - 110f, midpoint.y - 13f, 220f, 26f), Texture2D.whiteTexture);
            GUI.color = previousColor;
            GUI.Label(new Rect(midpoint.x - 105f, midpoint.y - 11f, 210f, 22f), label, DimensionLabelStyle);
            GUI.matrix = previousMatrix;
        }

        private static GUIStyle DimensionLabelStyle
        {
            get
            {
                if (_dimensionLabelStyle == null)
                {
                    _dimensionLabelStyle = new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontStyle = FontStyle.Bold
                    };
                    _dimensionLabelStyle.normal.textColor = new Color(0.15f, 0.92f, 1f, 1f);
                }
                return _dimensionLabelStyle;
            }
        }

        private static void DrawLine(Vector2 from, Vector2 to, Color color, float thickness)
        {
            var direction = to - from;
            if (direction.sqrMagnitude < 0.001f)
            {
                return;
            }

            var previousColor = GUI.color;
            var previousMatrix = GUI.matrix;
            GUI.color = color;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg, from);
            GUI.DrawTexture(new Rect(from.x, from.y - thickness * 0.5f, direction.magnitude, thickness), Texture2D.whiteTexture);
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }

        private static string FormatDistance(float metres)
        {
            var feet = metres / 0.3048f;
            return $"{metres:F2} m ({feet:F1} ft)";
        }
    }
}

