// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Silverback.Integration.Kafka.ConfigClassGenerator
{
    [SuppressMessage("ReSharper", "RedundantStringInterpolation")]
    class ProxyClassGenerator
    {
        private readonly Type _proxiedType;
        private readonly string _generatedClassName;
        private readonly string _baseClassName;
        private readonly CodeDomProvider _codeDomProvider = CodeDomProvider.CreateProvider("C#");
        private readonly string _xmlDocumentationPath;
        private readonly bool _generateNamespace;

        private StringBuilder _builder;
        private XmlDocument _xmlDoc;

        public ProxyClassGenerator(
            Type proxiedType,
            string generatedClassName,
            string baseClassName,
            string xmlDocumentationPath,
            bool generateNamespace)
        {
            _proxiedType = proxiedType;
            _generatedClassName = generatedClassName;
            _baseClassName = baseClassName;
            _xmlDocumentationPath = xmlDocumentationPath;
            _generateNamespace = generateNamespace;
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
            var proxiedTypeName = _proxiedType.FullName;
            var baseClass = _baseClassName != null ? $" : {_baseClassName}" : "";
            var abstractModifier = _baseClassName == null ? " abstract" : "";

            if (_generateNamespace)
            {
                _builder.AppendLine("namespace Silverback.Messaging.Proxies");
                _builder.AppendLine("{");
            }

            _builder.AppendLine($"    public{abstractModifier} class {_generatedClassName}{baseClass}");
            _builder.AppendLine($"    {{");

            if (_baseClassName == null)
            {
                _builder.AppendLine(
                    $"        internal static readonly ConfigurationDictionaryComparer<string, string> ConfluentConfigComparer = new ConfigurationDictionaryComparer<string, string>();");
                _builder.AppendLine();
                _builder.AppendLine(
                    $"        internal abstract Confluent.Kafka.ClientConfig ConfluentBaseConfig {{ get; }}");
                _builder.AppendLine();
                _builder.AppendLine(
                    $"        public abstract void Validate();");
            }
            else
            {
                _builder.AppendLine(
                    $"        internal override Confluent.Kafka.ClientConfig ConfluentBaseConfig {{ get; }} = new {proxiedTypeName}();");
                _builder.AppendLine(
                    $"        internal {proxiedTypeName} ConfluentConfig => ({proxiedTypeName}) ConfluentBaseConfig;");
            }
        }

        private void MapProperties()
        {
            var confluentConfigPropertyName =
                _baseClassName == null
                    ? "ConfluentBaseConfig"
                    : "ConfluentConfig";

            foreach (var property in GetProperties())
            {
                _builder.AppendLine();

                var propertyType = GetPropertyTypeString(property.PropertyType);
                var summary = GetSummary(property);

                if (summary != null)
                    _builder.AppendLine($"        ///{summary}");

                _builder.AppendLine($"        public {propertyType} {property.Name}");
                _builder.AppendLine($"        {{");

                if (property.GetGetMethod() != null)
                    _builder.AppendLine($"            get => {confluentConfigPropertyName}.{property.Name};");

                if (property.Name == "DeliveryReportFields")
                {
                    _builder.AppendLine($"            set");
                    _builder.AppendLine($"            {{");
                    _builder.AppendLine($"                if (value != null)");
                    _builder.AppendLine($"                    {confluentConfigPropertyName}.{property.Name} = value;");
                    _builder.AppendLine($"            }}");
                }
                else if (property.GetSetMethod() != null)
                    _builder.AppendLine($"            set => {confluentConfigPropertyName}.{property.Name} = value;");

                _builder.AppendLine($"        }}");
            }
        }

        private PropertyInfo[] GetProperties()
        {
            var bindingFlags = BindingFlags.Instance | BindingFlags.Public;

            if (_baseClassName != null)
                bindingFlags |= BindingFlags.DeclaredOnly;

            return _proxiedType.GetProperties(bindingFlags);
        }

        private void GenerateFooter()
        {
            _builder.AppendLine("    }");

            if (_generateNamespace)
                _builder.Append("}");
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
                _codeDomProvider.GenerateCodeFromExpression(typeReferenceExpression, writer,
                    new CodeGeneratorOptions());
                return writer.GetStringBuilder().ToString();
            }
        }

        private string GetSummary(PropertyInfo memberInfo)
        {
            if (_xmlDoc == null)
                LoadXmlDoc();

            var path = "P:" + memberInfo.DeclaringType.FullName + "." + memberInfo.Name;
            var node = _xmlDoc?.SelectSingleNode("//member[starts-with(@name, '" + path + "')]");

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