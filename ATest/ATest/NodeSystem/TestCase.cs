using System;

namespace ATest.NodeSystem
{
    public class TestCase : Node
    {
        [NodeProperty(NodePropertyAttribute.Parameter.None, int.MinValue + 1)] public bool Performed { get; set; }
        [NodeProperty(NodePropertyAttribute.Parameter.MultiLineString)]
        public string Steps { get; set; }
        [NodeProperty(NodePropertyAttribute.Parameter.MultiLineString)]
        public string ExpectedResult { get; set; }
        [NodeProperty(NodePropertyAttribute.Parameter.MultiLineString)]
        public string Result { get; set; }
    }
}
