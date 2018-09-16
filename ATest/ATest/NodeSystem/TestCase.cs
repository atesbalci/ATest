using System;

namespace ATest.NodeSystem
{
    public class TestCase : Node
    {
        [NodeProperty] public bool Performed { get; set; }
        [NodeProperty] public float TestFloat { get; set; }
        [NodeProperty] public int TestInt { get; set; }
        [NodeProperty] public DateTime TestDateTime { get; set; }
    }
}
