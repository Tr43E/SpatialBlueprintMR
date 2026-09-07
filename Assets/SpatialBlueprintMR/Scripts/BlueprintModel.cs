using System.Collections.Generic;
using UnityEngine;

namespace SpatialBlueprintMR
{
    /// <summary>Builds Unity wall geometry from floor-plan centre lines.</summary>
    public sealed class BlueprintModel : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float wallHeightMetres = 2.6f;
        [SerializeField, Min(0.01f)] private float wallThicknessMetres = 0.12f;
        [SerializeField, Min(0.001f)] private float planLineWidthMetres = 0.02f;
        [SerializeField] private Material wallMaterial;
        [SerializeField] private Material planLineMaterial;

        public float MetresPerCadUnit { get; private set; } = 1f;
        public int WallCount { get; private set; }

        private Transform _walls;
        private Transform _overlay;

        public void Build(IEnumerable<CadSegment> segments, float metresPerCadUnit)
        {
            Clear();
            MetresPerCadUnit = metresPerCadUnit;
            _walls = new GameObject("Walls").transform;
            _walls.SetParent(transform, false);
            _overlay = new GameObject("Plan Overlay").transform;
            _overlay.SetParent(transform, false);

            foreach (var segment in segments)
            {
                CreateWall(segment);
                CreateOverlayLine(segment);
                WallCount++;
            }
        }

        public void Clear()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
            WallCount = 0;
        }

        private void CreateWall(CadSegment segment)
        {
            var start = segment.Start.ToUnity(MetresPerCadUnit);
            var end = segment.End.ToUnity(MetresPerCadUnit);
            var direction = end - start;
            var length = direction.magnitude;
            if (length < 0.001f)
            {
                return;
            }

            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = $"Wall — {segment.Layer}";
            wall.transform.SetParent(_walls, false);
            wall.transform.localPosition = (start + end) * 0.5f + Vector3.up * (wallHeightMetres * 0.5f);
            wall.transform.localRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            wall.transform.localScale = new Vector3(wallThicknessMetres, wallHeightMetres, length + wallThicknessMetres);

            var renderer = wall.GetComponent<MeshRenderer>();
            if (wallMaterial != null)
            {
                renderer.sharedMaterial = wallMaterial;
            }
            else
            {
                renderer.material.color = new Color(0.15f, 0.7f, 1f, 0.28f);
            }
        }

        private void CreateOverlayLine(CadSegment segment)
        {
            var lineObject = new GameObject($"Plan line — {segment.Layer}");
            lineObject.transform.SetParent(_overlay, false);
            var line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.startWidth = planLineWidthMetres;
            line.endWidth = planLineWidthMetres;
            line.numCapVertices = 4;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            if (planLineMaterial != null)
            {
                line.sharedMaterial = planLineMaterial;
            }
            else
            {
                line.material = new Material(Shader.Find("Sprites/Default"));
                line.material.color = new Color(0.2f, 0.95f, 1f, 0.95f);
            }
            line.SetPosition(0, segment.Start.ToUnity(MetresPerCadUnit) + Vector3.up * 0.015f);
            line.SetPosition(1, segment.End.ToUnity(MetresPerCadUnit) + Vector3.up * 0.015f);
        }
    }
}
