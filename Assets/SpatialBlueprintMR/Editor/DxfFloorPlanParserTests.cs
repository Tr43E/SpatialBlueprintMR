using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace SpatialBlueprintMR.Editor
{
    public sealed class DxfFloorPlanParserTests
    {
        [Test]
        public void SampleStudio_UsesFeetAndParsesExpectedGeometry()
        {
            var samplePath = Path.Combine(Application.dataPath, "StreamingAssets", "SampleStudio.dxf");
            var plan = DxfFloorPlanParser.Parse(File.ReadAllText(samplePath));

            Assert.That(plan.Units, Is.EqualTo(CadUnit.Feet));
            Assert.That(plan.Segments.Count, Is.EqualTo(9));
            Assert.That(plan.Segments[0].LengthInCadUnits, Is.EqualTo(22f).Within(0.001f));
        }

        [Test]
        public void MetresPerUnit_ConvertsFeetExactly()
        {
            Assert.That(CadFloorPlan.MetresPerUnit(CadUnit.Feet), Is.EqualTo(0.3048f).Within(0.00001f));
        }
    }
}
