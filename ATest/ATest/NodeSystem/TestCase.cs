using System;

namespace ATest.NodeSystem
{
    public class TestCase : Node
    {
        [NodeProperty(NodePropertyAttribute.Parameter.None, int.MinValue + 1)] public bool Performed { get; set; }
        [NodeProperty] public float TestFloat { get; set; }
        [NodeProperty] public int TestInt { get; set; }
        [NodeProperty] public DateTime TestDateTime { get; set; }
    }
}
