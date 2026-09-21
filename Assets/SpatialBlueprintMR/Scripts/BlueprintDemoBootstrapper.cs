using UnityEngine;
using UnityEngine.InputSystem;

namespace SpatialBlueprintMR
{
    [RequireComponent(typeof(DxfBlueprintImporter), typeof(PlanPlacementController))]
    public sealed class BlueprintDemoBootstrapper : MonoBehaviour
    {
        private DxfBlueprintImporter _importer;
        private PlanPlacementController _placement;
        private BlueprintModel _model;
        private DesktopPreviewCameraController _cameraControls;

        private void Awake()
        {
            _importer = GetComponent<DxfBlueprintImporter>();
            _placement = GetComponent<PlanPlacementController>();
            _model = GetComponent<BlueprintModel>();
            if (GetComponent<BlueprintImportPanel>() == null)
            {
                gameObject.AddComponent<BlueprintImportPanel>();
            }

            var previewCamera = Camera.main;
            if (previewCamera != null)
            {
                _cameraControls = previewCamera.GetComponent<DesktopPreviewCameraController>();
                if (_cameraControls == null)
                {
                    _cameraControls = previewCamera.gameObject.AddComponent<DesktopPreviewCameraController>();
                }
            }
        }

        private void Start()
        {
            if (_importer.HasDefaultPlan)
            {
                _importer.LoadDefaultPlan();
            }
            else
            {
                _importer.LoadIncludedSamplePlan();
            }
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ExitDemo();
            }
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(16, 16, 450, 178), "Spatial Blueprint MR — desktop placement preview");
            GUI.Label(new Rect(30, 45, 420, 22), "W / A / S / D: move camera   Right-click + drag: look around");
            GUI.Label(new Rect(30, 68, 420, 22), "Q / E: rotate plan   [ / ]: scale   B: bird's-eye view");
            GUI.Label(new Rect(30, 91, 420, 22), $"Status: {(_placement.IsLocked ? "locked" : "editing")} | scale {(_placement.CalibrationFactor * 100f):F1}%");
            GUI.Label(new Rect(30, 114, 420, 22), _importer.LastError ?? $"Walls: {_model.WallCount}");
            if (_cameraControls != null && GUI.Button(new Rect(30, 145, 198, 30), _cameraControls.IsBirdsEyeView ? "Return to scene view (B)" : "Bird's-eye view (B)"))
            {
                _cameraControls.ToggleBirdsEyeView(_model);
            }
            if (GUI.Button(new Rect(240, 145, 198, 30), "Exit demo (Esc)"))
            {
                ExitDemo();
            }
        }

        private static void ExitDemo()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}

