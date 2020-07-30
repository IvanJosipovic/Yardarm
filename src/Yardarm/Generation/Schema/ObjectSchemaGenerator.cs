﻿using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.OpenApi.Models;
using Yardarm.Names;

namespace Yardarm.Generation.Schema
{
    internal class ObjectSchemaGenerator : ISchemaGenerator
    {
        private readonly INameFormatterSelector _nameFormatterSelector;
        private readonly INamespaceProvider _namespaceProvider;

        public ObjectSchemaGenerator(INameFormatterSelector nameFormatterSelector, INamespaceProvider namespaceProvider)
        {
            _nameFormatterSelector = nameFormatterSelector ?? throw new ArgumentNullException(nameof(nameFormatterSelector));
            _namespaceProvider = namespaceProvider ?? throw new ArgumentNullException(nameof(namespaceProvider));
        }

        public SyntaxTree Generate(string name, OpenApiSchema schema)
        {
            var className = _nameFormatterSelector.GetFormatter(NameKind.Class).Format(name);

            return CSharpSyntaxTree.Create(SyntaxFactory.CompilationUnit()
                .AddMembers(
                    SyntaxFactory.NamespaceDeclaration(_namespaceProvider.GetSchemaNamespace(NameKind.Class, schema))
                        .AddMembers(SyntaxFactory.ClassDeclaration(className)
                            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                            .AddMembers(SyntaxFactory.ConstructorDeclaration(className)
                                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                                .WithBody(SyntaxFactory.Block()))
                            .AddMembers(schema.Properties.Select(p => CreateProperty(p.Key, p.Value)).ToArray()))));
        }

        protected virtual MemberDeclarationSyntax CreateProperty(string name, OpenApiSchema type)
        {
            var propertyName = _nameFormatterSelector.GetFormatter(NameKind.Property).Format(name);

            return SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("string"), propertyName)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddAccessorListAccessors(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
        }
    }
}
