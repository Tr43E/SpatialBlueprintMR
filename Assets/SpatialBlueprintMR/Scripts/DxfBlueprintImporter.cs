using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SpatialBlueprintMR
{
    /// <summary>Loads ASCII DXF files and sends eligible linework to a BlueprintModel.</summary>
    [RequireComponent(typeof(BlueprintModel))]
    public sealed class DxfBlueprintImporter : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private TextAsset defaultPlan;
        [SerializeField] private CadUnit fallbackUnits = CadUnit.Feet;

        [Header("Layer filter")]
        [Tooltip("When matching layers are present, only these layers are treated as walls.")]
        [SerializeField] private string[] wallLayerKeywords = { "WALL", "A-WALL", "WALLS" };
        [SerializeField] private bool renderAllLayersWhenNoWallLayerExists = true;

        public CadFloorPlan CurrentPlan { get; private set; }
        public string LastError { get; private set; }
        public bool HasDefaultPlan => defaultPlan != null;
        public event Action<CadFloorPlan> PlanLoaded;

        private BlueprintModel _model;

        private void Awake()
        {
            _model = GetComponent<BlueprintModel>();
        }

        public void LoadDefaultPlan()
        {
            if (defaultPlan == null)
            {
                Debug.LogWarning("No default DXF has been assigned.", this);
                return;
            }
            LoadFromText(defaultPlan.text, defaultPlan.name);
        }

        /// <summary>Loads the included desktop sample without needing a TextAsset assignment in the scene.</summary>
        public void LoadIncludedSamplePlan()
        {
            LoadFromPath(Path.Combine(Application.streamingAssetsPath, "SampleStudio.dxf"));
        }

        public void LoadFromPath(string absolutePath)
        {
            if (!File.Exists(absolutePath))
            {
                LastError = $"DXF file was not found: {absolutePath}";
                Debug.LogError(LastError, this);
                return;
            }
            LoadFromText(File.ReadAllText(absolutePath), Path.GetFileName(absolutePath));
        }

        public void LoadFromText(string dxfText, string sourceName = "DXF")
        {
            try
            {
                CurrentPlan = DxfFloorPlanParser.Parse(dxfText, fallbackUnits);
                var segments = SelectWallSegments(CurrentPlan).ToList();
                _model.Build(segments, CadFloorPlan.MetresPerUnit(CurrentPlan.Units));
                LastError = null;
                Debug.Log($"Imported {sourceName}: {segments.Count} wall segments, {CurrentPlan.Units}.", this);
                PlanLoaded?.Invoke(CurrentPlan);
            }
            catch (Exception exception)
            {
                LastError = $"Could not import {sourceName}: {exception.Message}";
                Debug.LogError(LastError, this);
            }
        }

        private IEnumerable<CadSegment> SelectWallSegments(CadFloorPlan plan)
        {
            var matchingLayers = new HashSet<string>(plan.Layers.Where(IsWallLayer), StringComparer.OrdinalIgnoreCase);
            if (matchingLayers.Count == 0 && renderAllLayersWhenNoWallLayerExists)
            {
                return plan.Segments;
            }
            return plan.Segments.Where(segment => matchingLayers.Contains(segment.Layer));
        }

        private bool IsWallLayer(string layer)
        {
            return wallLayerKeywords.Any(keyword => !string.IsNullOrWhiteSpace(keyword) && layer.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
