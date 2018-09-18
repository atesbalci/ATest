namespace ATest.NodeSystem
{
    public class Category : Node
    {
        [NodeProperty(NodePropertyAttribute.Parameter.MultiLineString)]
        public string TestInput { get; set; }
        [NodeProperty(NodePropertyAttribute.Parameter.MultiLineString)]
        public string ExpectedResult { get; set; }
    }
}
