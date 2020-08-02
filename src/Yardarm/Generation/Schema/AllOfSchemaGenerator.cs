﻿using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.OpenApi.Models;
using Yardarm.Enrichment;
using Yardarm.Names;

namespace Yardarm.Generation.Schema
{
    internal class AllOfSchemaGenerator : ObjectSchemaGenerator
    {
        private readonly OpenApiDocument _document;

        public AllOfSchemaGenerator(INamespaceProvider namespaceProvider, ITypeNameGenerator typeNameGenerator,
            INameFormatterSelector nameFormatterSelector, ISchemaGeneratorFactory schemaGeneratorFactory,
            IEnumerable<ISchemaClassEnricher> classEnrichers, IEnumerable<IPropertyEnricher> propertyEnrichers,
            OpenApiDocument document)
            : base(namespaceProvider, typeNameGenerator, nameFormatterSelector, schemaGeneratorFactory, classEnrichers, propertyEnrichers)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
        }

        public override MemberDeclarationSyntax Generate(LocatedOpenApiElement<OpenApiSchema> element)
        {
            var classDefinition = (ClassDeclarationSyntax) base.Generate(element);

            OpenApiSchema schema = element.Element;

            bool addedInheritance = false;
            foreach (var section in schema.AllOf)
            {
                if (!addedInheritance && section.Reference != null)
                {
                    // We can inherit from the reference, but we need to load it from the reference to get the right type name

                    LocatedOpenApiElement<OpenApiSchema> referencedSchema =
                        ((OpenApiSchema)_document.ResolveReference(section.Reference)).CreateRoot(section.Reference.Id);

                    TypeSyntax typeName = TypeNameGenerator.GetName(referencedSchema);

                    classDefinition = classDefinition
                        .WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                            SyntaxFactory.SimpleBaseType(typeName))));

                    addedInheritance = true;
                }
                else
                {
                    classDefinition = AddProperties(classDefinition, element, section.Properties);
                }
            }

            return classDefinition;
        }
    }
}