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
        [Header("Doorways")]
        [SerializeField, Min(0.01f)] private float doorHeightMetres = 2.1f;
        [SerializeField, Min(0.01f)] private float doorLeafThicknessMetres = 0.04f;
        [SerializeField] private Material doorMaterial;
        [Header("Physics")]
        [Tooltip("Keeps generated walls solid for desktop and XR physics interactions.")]
        [SerializeField] private PhysicsMaterial wallPhysicsMaterial;

        public float MetresPerCadUnit { get; private set; } = 1f;
        public int WallCount { get; private set; }
        public int DoorCount { get; private set; }
        public Vector3 LocalLayoutCenter { get; private set; }
        public Vector2 LayoutSizeMetres { get; private set; }

        private Transform _walls;
        private Transform _overlay;
        private Transform _doors;

        public void Build(IEnumerable<CadSegment> segments, float metresPerCadUnit)
        {
            Build(segments, null, metresPerCadUnit);
        }

        /// <summary>Builds solid wall gaps plus visual-only open door leaves from their respective CAD layers.</summary>
        public void Build(IEnumerable<CadSegment> wallSegments, IEnumerable<CadSegment> doorwaySegments, float metresPerCadUnit)
        {
            Clear();
            MetresPerCadUnit = metresPerCadUnit;
            var wallSegmentList = wallSegments == null ? new List<CadSegment>() : new List<CadSegment>(wallSegments);
            var doorwaySegmentList = doorwaySegments == null ? new List<CadSegment>() : new List<CadSegment>(doorwaySegments);
            LocalLayoutCenter = FindLayoutCenter(wallSegmentList);
            _walls = new GameObject("Walls").transform;
            _walls.SetParent(transform, false);
            _overlay = new GameObject("Plan Overlay").transform;
            _overlay.SetParent(transform, false);
            _doors = new GameObject("Doorways").transform;
            _doors.SetParent(transform, false);

            foreach (var segment in wallSegmentList)
            {
                CreateWall(segment);
                CreateOverlayLine(segment);
                WallCount++;
            }

            foreach (var doorway in doorwaySegmentList)
            {
                CreateDoorway(doorway);
                DoorCount++;
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
            DoorCount = 0;
            LocalLayoutCenter = Vector3.zero;
            LayoutSizeMetres = Vector2.zero;
            _walls = null;
            _overlay = null;
            _doors = null;
        }

        /// <summary>Returns the world-space bounds of the generated wall layout for camera framing.</summary>
        public bool TryGetLayoutBounds(out Bounds bounds)
        {
            if (_walls == null || _walls.childCount == 0)
            {
                bounds = default;
                return false;
            }

            var firstRenderer = _walls.GetChild(0).GetComponent<Renderer>();
            if (firstRenderer == null)
            {
                bounds = default;
                return false;
            }

            bounds = firstRenderer.bounds;
            for (var i = 1; i < _walls.childCount; i++)
            {
                var renderer = _walls.GetChild(i).GetComponent<Renderer>();
                if (renderer != null)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
            return true;
        }

        /// <summary>Returns the CAD footprint dimensions after the current placement/calibration scale is applied.</summary>
        public bool TryGetLayoutMeasurements(out Vector2 dimensionsMetres)
        {
            if (WallCount == 0 || LayoutSizeMetres.x <= 0f || LayoutSizeMetres.y <= 0f)
            {
                dimensionsMetres = default;
                return false;
            }

            var worldScale = transform.lossyScale;
            dimensionsMetres = new Vector2(
                LayoutSizeMetres.x * Mathf.Abs(worldScale.x),
                LayoutSizeMetres.y * Mathf.Abs(worldScale.z));
            return true;
        }

        private void CreateWall(CadSegment segment)
        {
            var start = segment.Start.ToUnity(MetresPerCadUnit) - LocalLayoutCenter;
            var end = segment.End.ToUnity(MetresPerCadUnit) - LocalLayoutCenter;
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

            var wallCollider = wall.GetComponent<BoxCollider>();
            if (wallCollider == null)
            {
                wallCollider = wall.AddComponent<BoxCollider>();
            }
            wallCollider.isTrigger = false;
            wallCollider.sharedMaterial = wallPhysicsMaterial;
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
            line.SetPosition(0, segment.Start.ToUnity(MetresPerCadUnit) - LocalLayoutCenter + Vector3.up * 0.015f);
            line.SetPosition(1, segment.End.ToUnity(MetresPerCadUnit) - LocalLayoutCenter + Vector3.up * 0.015f);
        }

        private void CreateDoorway(CadSegment segment)
        {
            var start = segment.Start.ToUnity(MetresPerCadUnit) - LocalLayoutCenter;
            var end = segment.End.ToUnity(MetresPerCadUnit) - LocalLayoutCenter;
            var direction = end - start;
            var length = direction.magnitude;
            if (length < 0.001f)
            {
                return;
            }

            var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = $"Open door — {segment.Layer}";
            door.transform.SetParent(_doors, false);
            door.transform.localPosition = (start + end) * 0.5f + Vector3.up * (doorHeightMetres * 0.5f);
            door.transform.localRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            door.transform.localScale = new Vector3(doorLeafThicknessMetres, doorHeightMetres, length);

            var doorCollider = door.GetComponent<Collider>();
            if (doorCollider != null)
            {
                doorCollider.enabled = false;
            }

            var renderer = door.GetComponent<MeshRenderer>();
            if (doorMaterial != null)
            {
                renderer.sharedMaterial = doorMaterial;
            }
            else
            {
                renderer.material.color = new Color(0.95f, 0.62f, 0.18f, 0.72f);
            }
        }

        private Vector3 FindLayoutCenter(IEnumerable<CadSegment> segments)
        {
            var hasPoints = false;
            var bounds = default(Bounds);

            foreach (var segment in segments)
            {
                Encapsulate(segment.Start.ToUnity(MetresPerCadUnit));
                Encapsulate(segment.End.ToUnity(MetresPerCadUnit));
            }

            if (!hasPoints)
            {
                LayoutSizeMetres = Vector2.zero;
                return Vector3.zero;
            }

            LayoutSizeMetres = new Vector2(bounds.size.x, bounds.size.z);
            return new Vector3(bounds.center.x, 0f, bounds.center.z);

            void Encapsulate(Vector3 point)
            {
                if (!hasPoints)
                {
                    bounds = new Bounds(point, Vector3.zero);
                    hasPoints = true;
                    return;
                }
                bounds.Encapsulate(point);
            }
        }
    }
}

