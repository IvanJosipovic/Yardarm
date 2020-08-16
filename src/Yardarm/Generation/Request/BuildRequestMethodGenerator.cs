﻿using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.OpenApi.Models;
using Yardarm.Helpers;
using Yardarm.Spec;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Yardarm.Generation.Request
{
    public class BuildRequestMethodGenerator : IBuildRequestMethodGenerator
    {
        public const string BuildRequestMethodName = "BuildRequest";
        protected const string RequestMessageVariableName = "requestMessage";

        public MethodDeclarationSyntax Generate(LocatedOpenApiElement<OpenApiOperation> operation) =>
            MethodDeclaration(
                    WellKnownTypes.System.Net.Http.HttpRequestMessage.Name,
        BuildRequestMethodName)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .WithBody(Block(GenerateStatements(operation)));

        protected virtual IEnumerable<StatementSyntax> GenerateStatements(
            LocatedOpenApiElement<OpenApiOperation> operation)
        {
            yield return GenerateRequestMessageVariable(operation);

            yield return ExpressionStatement(AddHeadersMethodGenerator.InvokeAddHeaders(
                ThisExpression(),
                IdentifierName(RequestMessageVariableName)));

            yield return ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                SyntaxHelpers.MemberAccess(RequestMessageVariableName, "Content"),
                BuildContentMethodGenerator.InvokeBuildContent(ThisExpression())));

            yield return ReturnStatement(IdentifierName(RequestMessageVariableName));
        }

        protected virtual StatementSyntax GenerateRequestMessageVariable(
            LocatedOpenApiElement<OpenApiOperation> operation) =>
            MethodHelpers.LocalVariableDeclarationWithInitializer(RequestMessageVariableName,
                ObjectCreationExpression(WellKnownTypes.System.Net.Http.HttpRequestMessage.Name)
                    .AddArgumentListArguments(
                        Argument(GetRequestMethod(operation)),
                        Argument(BuildUriMethodGenerator.InvokeBuildUri(ThisExpression()))));

        protected virtual ExpressionSyntax GetRequestMethod(LocatedOpenApiElement<OpenApiOperation> operation) =>
            operation.Key switch
            {
                "Delete" => QualifiedName(WellKnownTypes.System.Net.Http.HttpMethod.Name, IdentifierName("Delete")),
                "Get" => QualifiedName(WellKnownTypes.System.Net.Http.HttpMethod.Name, IdentifierName("Get")),
                "Head" => QualifiedName(WellKnownTypes.System.Net.Http.HttpMethod.Name, IdentifierName("Head")),
                "Options" => QualifiedName(WellKnownTypes.System.Net.Http.HttpMethod.Name, IdentifierName("Options")),
                "Patch" => QualifiedName(WellKnownTypes.System.Net.Http.HttpMethod.Name, IdentifierName("Patch")),
                "Post" => QualifiedName(WellKnownTypes.System.Net.Http.HttpMethod.Name, IdentifierName("Post")),
                "Put" => QualifiedName(WellKnownTypes.System.Net.Http.HttpMethod.Name, IdentifierName("Put")),
                "Trace" => QualifiedName(WellKnownTypes.System.Net.Http.HttpMethod.Name, IdentifierName("Trace")),
                _ => ObjectCreationExpression(WellKnownTypes.System.Net.Http.HttpMethod.Name).AddArgumentListArguments(
                    Argument(SyntaxHelpers.StringLiteral(operation.Key.ToUpperInvariant())))
            };

        public static InvocationExpressionSyntax InvokeBuildRequest(ExpressionSyntax requestInstance) =>
            InvocationExpression(
                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    requestInstance,
                    IdentifierName(BuildRequestMethodName)));
    }
}