using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SpatialBlueprintMR
{
    /// <summary>Small, dependency-free parser for the ASCII DXF entities used by floor plans.</summary>
    public static class DxfFloorPlanParser
    {
        private struct Pair
        {
            public string Code;
            public string Value;

            public Pair(string code, string value)
            {
                Code = code;
                Value = value;
            }
        }

        private sealed class Entity
        {
            public string Type;
            public List<Pair> Attributes = new List<Pair>();
        }

        public static CadFloorPlan Parse(string dxfText, CadUnit fallbackUnit = CadUnit.Feet)
        {
            if (string.IsNullOrWhiteSpace(dxfText))
            {
                throw new ArgumentException("The DXF file was empty.", nameof(dxfText));
            }

            var pairs = ReadPairs(dxfText);
            var drawingUnit = ReadDeclaredUnits(pairs);
            var plan = new CadFloorPlan(drawingUnit == CadUnit.Unitless ? fallbackUnit : drawingUnit);

            var entities = ReadEntities(pairs);
            var legacyPolyline = new List<CadPoint>();
            var legacyLayer = "0";
            var legacyClosed = false;
            var readingLegacyPolyline = false;

            foreach (var entity in entities)
            {
                switch (entity.Type)
                {
                    case "LINE":
                        AddLine(plan, entity);
                        break;
                    case "LWPOLYLINE":
                        AddLightweightPolyline(plan, entity);
                        break;
                    case "POLYLINE":
                        legacyPolyline.Clear();
                        legacyLayer = GetString(entity, "8", "0");
                        legacyClosed = IsClosed(entity);
                        readingLegacyPolyline = true;
                        break;
                    case "VERTEX":
                        if (readingLegacyPolyline && TryGetPoint(entity, "10", "20", out var vertex))
                        {
                            legacyPolyline.Add(vertex);
                        }
                        break;
                    case "SEQEND":
                        if (readingLegacyPolyline)
                        {
                            AddPolylineSegments(plan, legacyPolyline, legacyLayer, legacyClosed);
                        }
                        readingLegacyPolyline = false;
                        break;
                }
            }

            if (plan.Segments.Count == 0)
            {
                throw new FormatException("No LINE, LWPOLYLINE, or POLYLINE geometry was found in the DXF ENTITIES section.");
            }

            return plan;
        }

        private static List<Pair> ReadPairs(string dxfText)
        {
            var lines = dxfText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            var pairs = new List<Pair>();
            for (var i = 0; i + 1 < lines.Length; i += 2)
            {
                var code = lines[i].Trim();
                var value = lines[i + 1].Trim();
                if (code.Length > 0)
                {
                    pairs.Add(new Pair(code, value));
                }
            }
            return pairs;
        }

        private static CadUnit ReadDeclaredUnits(List<Pair> pairs)
        {
            for (var i = 0; i < pairs.Count; i++)
            {
                if (!string.Equals(pairs[i].Value, "$INSUNITS", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                for (var j = i + 1; j < pairs.Count && pairs[j].Code != "9"; j++)
                {
                    if (pairs[j].Code == "70" && int.TryParse(pairs[j].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unitCode))
                    {
                        return Enum.IsDefined(typeof(CadUnit), unitCode) ? (CadUnit)unitCode : CadUnit.Unitless;
                    }
                }
            }
            return CadUnit.Unitless;
        }

        private static IEnumerable<Entity> ReadEntities(List<Pair> pairs)
        {
            var result = new List<Entity>();
            var section = string.Empty;
            Entity current = null;

            void CommitCurrent()
            {
                if (current == null)
                {
                    return;
                }

                if (current.Type == "SECTION")
                {
                    section = GetString(current, "2", string.Empty).ToUpperInvariant();
                }
                else if (current.Type == "ENDSEC")
                {
                    section = string.Empty;
                }
                else if (section == "ENTITIES")
                {
                    result.Add(current);
                }
            }

            foreach (var pair in pairs)
            {
                if (pair.Code == "0")
                {
                    CommitCurrent();
                    current = new Entity { Type = pair.Value.ToUpperInvariant() };
                    continue;
                }

                current?.Attributes.Add(pair);
            }
            CommitCurrent();
            return result;
        }

        private static void AddLine(CadFloorPlan plan, Entity entity)
        {
            if (TryGetPoint(entity, "10", "20", out var start) && TryGetPoint(entity, "11", "21", out var end))
            {
                plan.AddSegment(start, end, GetString(entity, "8", "0"));
            }
        }

        private static void AddLightweightPolyline(CadFloorPlan plan, Entity entity)
        {
            var points = new List<CadPoint>();
            float? x = null;
            foreach (var pair in entity.Attributes)
            {
                if (pair.Code == "10" && TryParseFloat(pair.Value, out var nextX))
                {
                    x = nextX;
                }
                else if (pair.Code == "20" && x.HasValue && TryParseFloat(pair.Value, out var y))
                {
                    points.Add(new CadPoint(x.Value, y));
                    x = null;
                }
            }
            AddPolylineSegments(plan, points, GetString(entity, "8", "0"), IsClosed(entity));
        }

        private static void AddPolylineSegments(CadFloorPlan plan, List<CadPoint> points, string layer, bool closed)
        {
            for (var i = 1; i < points.Count; i++)
            {
                plan.AddSegment(points[i - 1], points[i], layer);
            }
            if (closed && points.Count > 2)
            {
                plan.AddSegment(points[points.Count - 1], points[0], layer);
            }
        }

        private static bool IsClosed(Entity entity)
        {
            var flags = GetInt(entity, "70", 0);
            return (flags & 1) == 1;
        }

        private static bool TryGetPoint(Entity entity, string xCode, string yCode, out CadPoint point)
        {
            if (TryParseFloat(GetString(entity, xCode, string.Empty), out var x) && TryParseFloat(GetString(entity, yCode, string.Empty), out var y))
            {
                point = new CadPoint(x, y);
                return true;
            }
            point = default;
            return false;
        }

        private static string GetString(Entity entity, string code, string defaultValue)
        {
            var pair = entity.Attributes.FirstOrDefault(item => item.Code == code);
            return pair.Value ?? defaultValue;
        }

        private static int GetInt(Entity entity, string code, int defaultValue)
        {
            return int.TryParse(GetString(entity, code, string.Empty), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : defaultValue;
        }

        private static bool TryParseFloat(string text, out float value)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}
