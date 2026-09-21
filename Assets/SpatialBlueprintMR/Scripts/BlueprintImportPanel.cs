using System;
using System.IO;
using UnityEngine;

namespace SpatialBlueprintMR
{
    /// <summary>Desktop import panel for building a room from an ASCII DXF floor plan.</summary>
    [RequireComponent(typeof(DxfBlueprintImporter))]
    public sealed class BlueprintImportPanel : MonoBehaviour
    {
        [SerializeField] private bool visibleOnStart = true;

        private DxfBlueprintImporter _importer;
        private string _filePath;
        private string _message;
        private bool _isVisible;

        private void Awake()
        {
            _importer = GetComponent<DxfBlueprintImporter>();
            _isVisible = visibleOnStart;
            _filePath = Path.Combine(Application.streamingAssetsPath, "SampleStudio.dxf");
        }

        private void OnGUI()
        {
            if (!_isVisible)
            {
                if (GUI.Button(new Rect(Screen.width - 174, 16, 158, 30), "Import CAD blueprint"))
                {
                    _isVisible = true;
                }
                return;
            }

            var panel = new Rect(Screen.width - 436, 16, 420, 326);
            GUI.Box(panel, "Build a room from CAD");
            GUI.Label(new Rect(panel.x + 16, panel.y + 34, 385, 40), "Paste the full path to an ASCII .dxf floor plan.\nThe importer creates 1:1 3D walls from matching wall layers.");
            GUI.Label(new Rect(panel.x + 16, panel.y + 82, 120, 22), "Blueprint file");
            _filePath = GUI.TextField(new Rect(panel.x + 16, panel.y + 105, 388, 24), _filePath ?? string.Empty);

            if (GUI.Button(new Rect(panel.x + 16, panel.y + 140, 388, 32), "Build 3D room from blueprint"))
            {
                ImportPath();
            }
            if (GUI.Button(new Rect(panel.x + 16, panel.y + 180, 122, 28), "Studio"))
            {
                LoadStreamingSample("SampleStudio.dxf", "sample studio");
            }
            if (GUI.Button(new Rect(panel.x + 149, panel.y + 180, 122, 28), "1-bed apartment"))
            {
                LoadStreamingSample("OneBedroomApartment.dxf", "one-bedroom apartment");
            }
            if (GUI.Button(new Rect(panel.x + 282, panel.y + 180, 122, 28), "Office suite"))
            {
                LoadStreamingSample("SmallOfficeSuite.dxf", "small office suite");
            }

            GUI.Label(new Rect(panel.x + 16, panel.y + 218, 388, 20), BuildStatus());
            GUI.Label(new Rect(panel.x + 16, panel.y + 241, 388, 42), _message ?? "Tip: paste a full .dxf path, or use one of the included plans above.");

            if (GUI.Button(new Rect(panel.x + 304, panel.y + 287, 100, 22), "Hide panel"))
            {
                _isVisible = false;
            }
        }

        private void ImportPath()
        {
            var importPath = NormalizePastedPath(_filePath);
            _filePath = importPath;
            if (string.IsNullOrWhiteSpace(importPath))
            {
                _message = "Enter the full path to a .dxf file.";
                return;
            }

            if (!string.Equals(Path.GetExtension(importPath), ".dxf", StringComparison.OrdinalIgnoreCase))
            {
                _message = "This first version accepts ASCII .dxf files. Export DWG files as DXF in AutoCAD.";
                return;
            }

            _importer.LoadFromPath(importPath);
            _message = _importer.LastError ?? $"Built {_importer.CurrentPlan.Segments.Count} plan segments from {Path.GetFileName(importPath)}.";
        }

        private void LoadStreamingSample(string fileName, string displayName)
        {
            _filePath = Path.Combine(Application.streamingAssetsPath, fileName);
            _importer.LoadFromPath(_filePath);
            _message = _importer.LastError ?? $"Loaded the {displayName} blueprint.";
        }

        private static string NormalizePastedPath(string value)
        {
            var path = (value ?? string.Empty).Trim().Trim('"');
            if (Uri.TryCreate(path, UriKind.Absolute, out var uri) && uri.IsFile)
            {
                path = uri.LocalPath;
            }
            if (path.Length >= 3 && path[0] == '/' && char.IsLetter(path[1]) && path[2] == ':')
            {
                path = path.Substring(1);
            }
            return path;
        }

        private string BuildStatus()
        {
            if (_importer.LastError != null)
            {
                return "Import failed — see the message below.";
            }
            if (_importer.CurrentPlan == null)
            {
                return "No blueprint loaded yet.";
            }
            return $"Loaded: {_importer.CurrentPlan.Units} | {_importer.CurrentPlan.Segments.Count} line segments | {_importer.CurrentPlan.Layers.Count} layers";
        }
    }
}

