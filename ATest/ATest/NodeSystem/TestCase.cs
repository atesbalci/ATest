using System;

namespace ATest.NodeSystem
{
    public class TestCase : Node
    {
        [NodeProperty(NodePropertyAttribute.Parameter.MultiLineString)]
        public string Steps { get; set; }
        [NodeProperty(NodePropertyAttribute.Parameter.MultiLineString)]
        public string ExpectedResult { get; set; }
        [NodeProperty(NodePropertyAttribute.Parameter.MultiLineString)]
        public string Result { get; set; }

        public TestCase()
        {
            Steps = string.Empty;
            ExpectedResult = string.Empty;
            Result = string.Empty;
        }
    }
}
