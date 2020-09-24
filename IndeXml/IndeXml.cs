using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace DXML
{
    public class IndeXml : IEnumerable<IndeXml>
    {
        public static IndeXml Parse(string xmlString)
        {
            return new IndeXml(XDocument.Parse(xmlString).Root);
        }

        #region Private Constructors
        private IndeXml(XElement root)
        {
            _root = root;
        }

        private IndeXml(string str)
        {
            _root = new XElement("value", str);
        }

        private IndeXml(IEnumerable<IndeXml> objs)
        {
            _root = Parse($"<Array>{String.Join("", objs.Select(x => x.Text))}</Array>")._root;
        }
        #endregion

        public XElement _root;
        public System.Xml.Linq.XName Name => _root.Name;
        public string Text
        {
            get
            {
                var reader = _root.CreateReader();
                reader.MoveToContent();

                return reader.ReadInnerXml();
            }
        }

        public override string ToString() => Text;

        IEnumerable<XAttribute> NSDeclarations => _root.Attributes().Where(a => a.IsNamespaceDeclaration);
        String NSLabel => _root.GetPrefixOfNamespace(_root.Name.Namespace);

        private bool TryGetMember(string Name, out IndeXml result)
        {
            result = null;

            var att = _root.Attribute(Name);
            if (att != null)
            {
                result = new IndeXml(att.Value);
                return true;
            }

            var nodes = _root.Elements().Where(x => x.Name.LocalName == Name);
            if (nodes.Count() > 1)
            {
                result = new IndeXml(nodes.Select(n => n.HasElements ? new IndeXml(n) : new IndeXml(n.Value)));
                return true;
            }
            if (nodes.Count() == 1)
            {
                var node = nodes.First();
                result = node.HasElements || node.HasAttributes ? new IndeXml(node) : new IndeXml(node.Value);
                return true;
            }

            return true;
        }

        #region Equality Test Overrides
        public override bool Equals(object obj)
        {
            return obj.GetType() == typeof(IndeXml) ? this._root.Equals(((dynamic)obj)._root) :
                obj.GetType() == typeof(String) ? this.Text == (string)obj :
                false;
        }
        public static bool operator ==(IndeXml a, object obj)
        {
            if (Object.ReferenceEquals(a, null) && Object.ReferenceEquals(obj, null))
            {
                return true;
            }
            else if (Object.ReferenceEquals(a, null) ^ Object.ReferenceEquals(obj, null))
            {
                return false;
            }
            return obj.GetType() == typeof(IndeXml) ? a._root == ((dynamic)obj)._root :
                obj.GetType() == typeof(String) ? a.Text == (string)obj :
            false;
        }
        public static bool operator !=(IndeXml a, object obj)
        {
            return !(a == obj);
        }
        public override int GetHashCode()
        {
            return -2112957014 + EqualityComparer<XElement>.Default.GetHashCode(_root);
        }
        #endregion Equality Test Overrides

        #region Indexing And Enumeration
        public IndeXml this[string key]
        {
            get
            {
                IndeXml result;
                if (TryGetMember(key, out result))
                {
                    return result;
                }
                return null;
            }
        }
        public IEnumerator<IndeXml> GetEnumerator()
        {
            return _root.Elements().Select(x => new IndeXml(x)).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _root.Elements().Select(x => new IndeXml(x)).GetEnumerator();
        }
        #endregion
    }
}

namespace DXML.Serialization
{
    public static class Serializer 
    {
        public static T Deserialize<T>(this IndeXml dXml)
        {
            var attrs = new XmlAttributes();

            var root = new XmlRootAttribute(dXml.Name.LocalName.ToString())
            {
                Namespace = dXml.Name.Namespace.ToString()
            };
            attrs.XmlRoot = root;

            var o = new XmlAttributeOverrides();

            o.Add(typeof(T), attrs);

            var ser = new XmlSerializer(typeof(T), o);

            using (var sr = new StringReader(dXml._root.ToString()))
            {
                return (T)ser.Deserialize(sr);
            }
        }

        public static string Serialize<T>(this T obj, string rootName, bool skipDeclaration = true, string Namespace = null)
        {
            XmlTypeAttribute XmlTypeAttribute =
            (XmlTypeAttribute)Attribute.GetCustomAttribute(typeof(T), typeof(XmlTypeAttribute));

            var o = new XmlAttributeOverrides();

            var attrs = new XmlAttributes();

            var root = new XmlRootAttribute(rootName);
            root.Namespace = XmlTypeAttribute?.Namespace ?? Namespace ?? root.Namespace;
            attrs.XmlRoot = root;

            o.Add(obj.GetType(), attrs);

            // create the serializer, specifying the overrides to use
            XmlSerializer xmlSerializer = new XmlSerializer(obj.GetType(), o);

            using (StringWriter textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, obj);//, ns);
                var result = textWriter.ToString();
                if (skipDeclaration)
                {
                    result = String.Join(Environment.NewLine, Regex.Split(result, "\r\n|\r|\n").Skip(1));
                }
                return result;
            }
        }
    }
}
