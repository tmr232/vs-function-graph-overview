using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace FunctionGraphOverview.Tests
{
    public class ColorSchemeDefinitionsTests
    {
        private static readonly string[] ExpectedNames =
        {
            "node.default",
            "node.entry",
            "node.exit",
            "node.throw",
            "node.yield",
            "node.terminate",
            "node.border",
            "node.highlight",
            "edge.regular",
            "edge.consequence",
            "edge.alternative",
            "cluster.border",
            "cluster.with",
            "cluster.tryComplex",
            "cluster.try",
            "cluster.finally",
            "cluster.except",
            "graph.background",
        };

        [Fact]
        public void GetDarkScheme_ContainsExpectedEntries()
        {
            var scheme = ColorSchemeDefinitions.GetDarkScheme();
            Assert.Equal(ExpectedNames.Length, scheme.Count);
            Assert.Equal(ExpectedNames, scheme.Select(c => c.name).ToArray());
        }

        [Fact]
        public void GetLightScheme_ContainsExpectedEntries()
        {
            var scheme = ColorSchemeDefinitions.GetLightScheme();
            Assert.Equal(ExpectedNames.Length, scheme.Count);
            Assert.Equal(ExpectedNames, scheme.Select(c => c.name).ToArray());
        }

        [Theory]
        [MemberData(nameof(AllSchemeEntries))]
        public void AllColors_AreValidHex(string _, string hex)
        {
            Assert.Matches(@"^#[0-9A-Fa-f]{6}$", hex);
        }

        [Fact]
        public void DarkAndLight_HaveSameNames()
        {
            var dark = ColorSchemeDefinitions.GetDarkScheme().Select(c => c.name);
            var light = ColorSchemeDefinitions.GetLightScheme().Select(c => c.name);
            Assert.Equal(dark, light);
        }

        public static IEnumerable<object[]> AllSchemeEntries()
        {
            foreach (var entry in ColorSchemeDefinitions.GetDarkScheme())
                yield return new object[] { entry.name, entry.hex };
            foreach (var entry in ColorSchemeDefinitions.GetLightScheme())
                yield return new object[] { entry.name, entry.hex };
        }
    }
}
