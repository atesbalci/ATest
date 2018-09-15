using System.Collections.Generic;
using System.Xml.Linq;

namespace ATest.NodeSystem
{
    public class TestCase : Node
    {
        public Dictionary<string, string> Properties { get; set; }
        [NodeProperty] public bool Performed { get; set; }

        public TestCase()
        {
            Properties = new Dictionary<string, string>();
        }

        public override XElement ToXml()
        {
            var retVal =  base.ToXml();
            foreach (var property in Properties)
            {
                retVal.Add(new XElement(property.Key, property.Value));
            }
            return retVal;
        }

        public override void FromXml(XElement element)
        {
            base.FromXml(element);
            foreach (var ele in element.Elements())
            {
                var key = ele.Name.ToString();
                var value = ele.Value;
                Properties.Add(key, value);
            }
        }
    }
}
