﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Eto;

namespace ATest.NodeSystem
{
    [AttributeUsage(AttributeTargets.Property)]
    public class NodePropertyAttribute : Attribute { }

    public abstract class Node
    {
        [NodeProperty] public string Name { get; set; }
        public bool Expanded { get; set; }
        public IList<Node> Children { get; set; }

        protected Node()
        {
            Children = new List<Node>();
        }

        public virtual XElement ToXml()
        {
            var retVal = new XElement(GetType().Name);
            retVal.Add(new XAttribute("Type", GetType().FullName));
            foreach (var attribute in NodePropertiesToXml())
            {
                retVal.Add(attribute);
            }
            foreach (var child in Children)
            {
                retVal.Add(child.ToXml());
            }
            return retVal;
        }

        private IEnumerable<XAttribute> NodePropertiesToXml()
        {
            var retVal = new LinkedList<XAttribute>();
            foreach (var prop in GetType().GetRuntimeProperties().Where(prop => prop.CustomAttributes.Any(x => x.AttributeType == typeof(NodePropertyAttribute))))
            {
                retVal.AddLast(new XAttribute(prop.Name, prop.GetValue(this)));
            }
            return retVal;
        }

        public virtual void FromXml(XElement element)
        {
            var type = GetType();
            foreach (var attribute in element.Attributes())
            {
                var property = type.GetRuntimeProperty(attribute.Name.ToString());
                if(property == null) continue;
                var result = Parse(attribute.Value, property.PropertyType);
                property.SetValue(this, result);
            }
            Children.Clear();
            foreach (var child in element.Elements())
            {
                var typeAttribute = child.Attribute("Type");
                if (typeAttribute == null) continue;
                var node = (Node) Activator.CreateInstance(Type.GetType(typeAttribute.Value));
                node.FromXml(child);
                Children.Add(node);
            }
        }

        protected static object Parse(string input, Type type)
        {
            if (type == typeof(int))
            {
                return int.Parse(input);
            }
            if (type == typeof(float))
            {
                return float.Parse(input);
            }
            if (type == typeof(bool))
            {
                return bool.Parse(input);
            }
            return input;
        }
    }
}
