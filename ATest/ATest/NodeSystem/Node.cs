using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace ATest.NodeSystem
{
    public abstract class Node
    {
        public string Name { get; set; }
        public bool Expanded { get; set; }
        public IList<Node> Children { get; set; }

        protected Node()
        {
            Children = new List<Node>();
        }

        public virtual XElement ToXml()
        {
            var retVal = new XElement(GetType().Name);
            retVal.Add(new XAttribute("Name", Name));
            retVal.Add(new XAttribute("Type", GetType().FullName));
            retVal.Add(new XAttribute("Expanded", Expanded));
            foreach (var child in Children)
            {
                retVal.Add(child.ToXml());
            }
            return retVal;
        }

        public virtual void FromXml(XElement element)
        {
            Name = element.Attribute("Name").Value;
            Expanded = bool.Parse(element.Attribute("Expanded").Value);
            Children.Clear();
            foreach (var child in element.Elements())
            {
                var node = (Node) Activator.CreateInstance(Type.GetType(child.Attribute("Type").Value));
                node.FromXml(child);
                Children.Add(node);
            }
        }
    }
}
