using System.Collections.Generic;

namespace ATest.NodeSystem
{
    public abstract class Node
    {
        public string Name { get; set; }
        public IList<Node> Children { get; set; }
    }
}
