﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.OpenApi.Models;
using Yardarm.Enrichment;
using Yardarm.Names;

namespace Yardarm.Generation.Schema
{
    /// <summary>
    /// Generates the generic OneOf&gt;...&lt; types for various discriminated unions.
    /// </summary>
    internal class OneOfSchemaGenerator : SchemaGeneratorBase
    {
        protected IList<ISchemaClassEnricher> ClassEnrichers { get; }

        protected override NameKind NameKind => NameKind.Class;

        public OneOfSchemaGenerator(INamespaceProvider namespaceProvider, ITypeNameGenerator typeNameGenerator,
            INameFormatterSelector nameFormatterSelector, ISchemaGeneratorFactory schemaGeneratorFactory,
            IEnumerable<ISchemaClassEnricher> classEnrichers)
            : base(namespaceProvider, typeNameGenerator, nameFormatterSelector, schemaGeneratorFactory)
        {
            ClassEnrichers = classEnrichers.ToArray();
        }

        public override SyntaxTree GenerateSyntaxTree(LocatedOpenApiElement<OpenApiSchema> element)
        {
            var classNameAndNamespace = (QualifiedNameSyntax)GetTypeName(element);

            NameSyntax ns = classNameAndNamespace.Left;

            return CSharpSyntaxTree.Create(SyntaxFactory.CompilationUnit()
                .AddMembers(
                    SyntaxFactory.NamespaceDeclaration(ns)
                        .AddMembers(Generate(element)))
                .NormalizeWhitespace());
        }

        public override MemberDeclarationSyntax Generate(LocatedOpenApiElement<OpenApiSchema> element)
        {
            var classNameAndNamespace = (QualifiedNameSyntax)GetTypeName(element);

            NameSyntax ns = classNameAndNamespace.Left;
            SimpleNameSyntax className = classNameAndNamespace.Right;

            var syntaxTree = Generate(ns, className, element.Element.OneOf.Select(p => element.CreateChild(p, "")));

            var classDeclaration = syntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();

            return classDeclaration.Enrich(ClassEnrichers, element);
        }

        public SyntaxTree Generate(NameSyntax ns, SimpleNameSyntax identifier, IEnumerable<LocatedOpenApiElement<OpenApiSchema>> values)
        {
            var builder = new StringBuilder(1024 * 10);

            TypeSyntax[] typeNames = values.Select(p => TypeNameGenerator.GetName(p))
                .ToArray();

            builder.AppendLine($@"namespace {ns}
{{
    public abstract class {identifier} : System.IEquatable<{identifier}>
    {{
        private {identifier}() {{}}

        public abstract bool Equals({identifier} other);");

            AddImplicitOperations(builder, identifier.ToString(), typeNames);
            AddSubTypes(builder, identifier.ToString(), typeNames);

            builder.AppendLine(@"    }
}");

            return CSharpSyntaxTree.ParseText(SourceText.From(builder.ToString()),
                CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8));
        }

        private void AddImplicitOperations(StringBuilder builder, string identifier, IEnumerable<TypeSyntax> typeNames)
        {
            foreach (var typeName in typeNames.OfType<QualifiedNameSyntax>())
            {
                builder.AppendLine(
                    @$"        public static implicit operator {identifier}({typeName} value) =>
            new {identifier}.{typeName.Right}(value);");
            }
        }

        private void AddSubTypes(StringBuilder builder, string identifier, IEnumerable<TypeSyntax> typeNames)
        {
            foreach (var typeName in typeNames.OfType<QualifiedNameSyntax>())
            {
                string subClassName = typeName.Right.ToString();

                builder.AppendLine($@"        public sealed class {subClassName} : {identifier}
        {{
            public {typeName} Value {{ get; }}

            public {subClassName}({typeName} value)
            {{
                Value = value;
            }}

            public static implicit operator {typeName}({subClassName} subClass) => subClass.Value;

            public override bool Equals(object? obj)
            {{
                if (!(obj is {subClassName} subClass)) return false;
                return Equals(subClass);
            }}

            public override bool Equals({identifier} other)
            {{
                if (!(other is {subClassName} subClass)) return false;
                return System.Collections.Generic.EqualityComparer<{typeName}>.Default.Equals(Value, subClass.Value);
            }}

            public bool Equals({subClassName} other)
            {{
                if (other == null) return false;
                return System.Collections.Generic.EqualityComparer<{typeName}>.Default.Equals(Value, other.Value);
            }}

            public overrides int GetHashCode() => Value?.GetHashCode() ?? 0;
        }}");
            }
        }
    }
}
