using UnityEngine;

namespace SpatialBlueprintMR
{
    [RequireComponent(typeof(DxfBlueprintImporter), typeof(PlanPlacementController))]
    public sealed class BlueprintDemoBootstrapper : MonoBehaviour
    {
        private DxfBlueprintImporter _importer;
        private PlanPlacementController _placement;

        private void Awake()
        {
            _importer = GetComponent<DxfBlueprintImporter>();
            _placement = GetComponent<PlanPlacementController>();
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

        private void OnGUI()
        {
            GUI.Box(new Rect(16, 16, 450, 142), "Spatial Blueprint MR — desktop placement preview");
            GUI.Label(new Rect(30, 45, 420, 22), "Arrow keys: move   Q / E: rotate   Page Up / Down: height");
            GUI.Label(new Rect(30, 68, 420, 22), "[ / ]: scale   L: lock   P: save placement   O: restore placement");
            GUI.Label(new Rect(30, 91, 420, 22), $"Status: {(_placement.IsLocked ? "locked" : "editing")} | scale {(_placement.CalibrationFactor * 100f):F1}%");
            GUI.Label(new Rect(30, 114, 420, 22), _importer.LastError ?? $"Walls: {GetComponent<BlueprintModel>().WallCount}");
        }
    }
}
