// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace ConfigClassGenerator
{
    class ProxyClassGenerator
    {
        private readonly Type _proxiedType;
        private readonly string _generatedClassName;
        private readonly CodeDomProvider _codeDomProvider = CodeDomProvider.CreateProvider("C#");
        private readonly string _xmlDocumentationPath;

        private StringBuilder _builder;
        private XmlDocument _xmlDoc;

        public ProxyClassGenerator(Type proxiedType, string generatedClassName, string xmlDocumentationPath)
        {
            _proxiedType = proxiedType;
            _generatedClassName = generatedClassName;
            _xmlDocumentationPath = xmlDocumentationPath;
        }

        public string Generate()
        {
            _builder = new StringBuilder();

            GenerateHeading();
            MapProperties();
            GenerateFooter();

            return _builder.ToString();
        }

        private void GenerateHeading()
        {
            _builder.Append("namespace Silverback.Messaging.Proxies\r\n{\r\n");
            _builder.AppendFormat("    public class {0}\r\n    {{\r\n", _generatedClassName);
            _builder.AppendFormat("        internal {0} ConfluentConfig {{ get; }} = new {0}();\r\n", _proxiedType.FullName);
        }

        private void MapProperties()
        {
            foreach (var property in _proxiedType.GetProperties())
            {
                _builder.AppendLine();

                var summary = GetSummary(property);
                if (summary != null)
                    _builder.AppendFormat("        ///{0}\r\n", summary);

                _builder.AppendFormat("        public {0} {1} {{ get => ConfluentConfig.{1}; set => ConfluentConfig.{1} = value; }}\r\n",
                    GetPropertyTypeString(property.PropertyType), 
                    property.Name);
            }
        }
        private void GenerateFooter()
        {
            _builder.Append("    }\r\n}");
        }

        private string GetPropertyTypeString(Type propertyType)
        {
            var nullableType = Nullable.GetUnderlyingType(propertyType);
            if (nullableType != null)
            {
                return GetTypeName(nullableType) + "?";
            }

            return GetTypeName(propertyType);
        }

        private string GetTypeName(Type type)
        {
            var typeReferenceExpression = new CodeTypeReferenceExpression(new CodeTypeReference(type));
            using (var writer = new StringWriter())
            {
                _codeDomProvider.GenerateCodeFromExpression(typeReferenceExpression, writer, new CodeGeneratorOptions());
                return writer.GetStringBuilder().ToString();
            }
        }

        private string GetSummary(PropertyInfo memberInfo)
        {
            if (_xmlDoc == null)
                LoadXmlDoc();

            var path = "P:" + memberInfo.DeclaringType.FullName + "." + memberInfo.Name;
            var node = _xmlDoc.SelectSingleNode("//member[starts-with(@name, '" + path + "')]");

            if (node == null)
                return null;

            return Regex.Replace(node.InnerXml, @"\s+", " ");
        }

        private void LoadXmlDoc()
        {
            if (!File.Exists(_xmlDocumentationPath))
                return;

            _xmlDoc = new XmlDocument();
            _xmlDoc.Load(_xmlDocumentationPath);
        }
    }
}