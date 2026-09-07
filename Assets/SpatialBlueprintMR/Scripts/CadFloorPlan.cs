using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpatialBlueprintMR
{
    public enum CadUnit
    {
        Unitless = 0,
        Inches = 1,
        Feet = 2,
        Millimetres = 4,
        Centimetres = 5,
        Metres = 6
    }

    [Serializable]
    public struct CadPoint
    {
        public float X;
        public float Y;

        public CadPoint(float x, float y)
        {
            X = x;
            Y = y;
        }

        public Vector3 ToUnity(float metresPerCadUnit)
        {
            return new Vector3(X * metresPerCadUnit, 0f, Y * metresPerCadUnit);
        }
    }

    [Serializable]
    public struct CadSegment
    {
        public CadPoint Start;
        public CadPoint End;
        public string Layer;

        public CadSegment(CadPoint start, CadPoint end, string layer)
        {
            Start = start;
            End = end;
            Layer = string.IsNullOrWhiteSpace(layer) ? "0" : layer;
        }

        public float LengthInCadUnits
        {
            get
            {
                var dx = End.X - Start.X;
                var dy = End.Y - Start.Y;
                return Mathf.Sqrt(dx * dx + dy * dy);
            }
        }
    }

    public sealed class CadFloorPlan
    {
        public CadUnit Units { get; }
        public IReadOnlyList<CadSegment> Segments => _segments;
        public IReadOnlyCollection<string> Layers => _layers;

        private readonly List<CadSegment> _segments = new List<CadSegment>();
        private readonly HashSet<string> _layers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public CadFloorPlan(CadUnit units)
        {
            Units = units;
        }

        public void AddSegment(CadPoint start, CadPoint end, string layer)
        {
            var segment = new CadSegment(start, end, layer);
            if (segment.LengthInCadUnits < 0.0001f)
            {
                return;
            }

            _segments.Add(segment);
            _layers.Add(segment.Layer);
        }

        public static float MetresPerUnit(CadUnit unit)
        {
            switch (unit)
            {
                case CadUnit.Inches: return 0.0254f;
                case CadUnit.Feet: return 0.3048f;
                case CadUnit.Millimetres: return 0.001f;
                case CadUnit.Centimetres: return 0.01f;
                case CadUnit.Metres: return 1f;
                default: return 1f;
            }
        }
    }
}
