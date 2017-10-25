﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Roslynator.CSharp.Refactorings
{
    internal static class AttributeArgumentParameterNameRefactoring
    {
        private static readonly SymbolDisplayFormat _symbolDisplayFormat = new SymbolDisplayFormat(
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers,
            parameterOptions: SymbolDisplayParameterOptions.IncludeName);

        public static async Task ComputeRefactoringsAsync(RefactoringContext context, AttributeArgumentListSyntax argumentList)
        {
            if (context.IsAnyRefactoringEnabled(
                    RefactoringIdentifiers.AddParameterNameToArgument,
                    RefactoringIdentifiers.RemoveParameterNameFromArgument)
                && !context.Span.IsEmpty)
            {
                List<AttributeArgumentSyntax> arguments = null;

                foreach (AttributeArgumentSyntax argument in argumentList.Arguments)
                {
                    if (argument.Expression != null
                        && context.Span.Contains(argument.Expression.Span))
                    {
                        (arguments ?? (arguments = new List<AttributeArgumentSyntax>())).Add(argument);
                    }
                }

                if (arguments?.Count > 0)
                    await AddOrRemoveParameterNameAsync(context, argumentList, arguments.ToArray()).ConfigureAwait(false);
            }
        }

        private static async Task AddOrRemoveParameterNameAsync(
            RefactoringContext context,
            AttributeArgumentListSyntax argumentList,
            AttributeArgumentSyntax[] arguments)
        {
            if (context.IsRefactoringEnabled(RefactoringIdentifiers.AddParameterNameToArgument)
                && await CanAddParameterNameAsync(context, arguments).ConfigureAwait(false))
            {
                context.RegisterRefactoring(
                    "Add parameter name",
                    cancellationToken =>
                    {
                        return AddParameterNameToArgumentsAsync(
                            context.Document,
                            argumentList,
                            arguments,
                            cancellationToken);
                    });
            }

            if (context.IsRefactoringEnabled(RefactoringIdentifiers.RemoveParameterNameFromArgument)
                && arguments.Any(f => f.NameColon != null))
            {
                context.RegisterRefactoring(
                    "Remove parameter name",
                    cancellationToken =>
                    {
                        return RemoveParameterNameFromArgumentsAsync(
                            context.Document,
                            argumentList,
                            arguments,
                            cancellationToken);
                    });
            }
        }

        private static async Task<Document> AddParameterNameToArgumentsAsync(
            Document document,
            AttributeArgumentListSyntax argumentList,
            AttributeArgumentSyntax[] arguments,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            AttributeArgumentListSyntax newArgumentList = AddParameterNameSyntaxRewriter.VisitNode(argumentList, arguments, semanticModel)
                .WithFormatterAnnotation();

            return await document.ReplaceNodeAsync(argumentList, newArgumentList, cancellationToken).ConfigureAwait(false);
        }

        private static Task<Document> RemoveParameterNameFromArgumentsAsync(
            Document document,
            AttributeArgumentListSyntax argumentList,
            AttributeArgumentSyntax[] arguments,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            AttributeArgumentListSyntax newArgumentList = RemoveParameterNameSyntaxRewriter.VisitNode(argumentList, arguments)
                .WithFormatterAnnotation();

            return document.ReplaceNodeAsync(argumentList, newArgumentList, cancellationToken);
        }

        private static AttributeArgumentSyntax AddParameterName(
            AttributeArgumentSyntax argument,
            SemanticModel semanticModel,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (argument.NameColon == null || argument.NameColon.IsMissing)
            {
                IParameterSymbol parameterSymbol = semanticModel.DetermineParameter(
                    argument,
                    allowParams: false,
                    cancellationToken: cancellationToken);

                if (parameterSymbol != null)
                {
                    return argument
                        .WithNameColon(
                            NameColon(parameterSymbol.ToDisplayString(_symbolDisplayFormat))
                                .WithTrailingTrivia(Space))
                        .WithTriviaFrom(argument);
                }
            }

            return argument;
        }

        private static async Task<bool> CanAddParameterNameAsync(
            RefactoringContext context,
            AttributeArgumentSyntax[] arguments)
        {
            foreach (AttributeArgumentSyntax argument in arguments)
            {
                if (argument.NameColon == null || argument.NameColon.IsMissing)
                {
                    SemanticModel semanticModel = await context.GetSemanticModelAsync().ConfigureAwait(false);

                    IParameterSymbol parameterSymbol = semanticModel.DetermineParameter(
                        argument,
                        allowParams: false,
                        cancellationToken: context.CancellationToken);

                    if (parameterSymbol != null)
                        return true;
                }
            }

            return false;
        }

        private class AddParameterNameSyntaxRewriter : CSharpSyntaxRewriter
        {
            private readonly SemanticModel _semanticModel;
            private readonly AttributeArgumentSyntax[] _arguments;

            private AddParameterNameSyntaxRewriter(AttributeArgumentSyntax[] arguments, SemanticModel semanticModel)
            {
                _arguments = arguments;
                _semanticModel = semanticModel;
            }

            public static AttributeArgumentListSyntax VisitNode(
                AttributeArgumentListSyntax argumentList,
                AttributeArgumentSyntax[] arguments,
                SemanticModel semanticModel)
            {
                return (AttributeArgumentListSyntax)new AddParameterNameSyntaxRewriter(arguments, semanticModel).Visit(argumentList);
            }

            public override SyntaxNode VisitAttributeArgument(AttributeArgumentSyntax node)
            {
                if (Array.IndexOf(_arguments, node) != -1)
                    return AddParameterName(node, _semanticModel);

                return base.VisitAttributeArgument(node);
            }
        }

        private class RemoveParameterNameSyntaxRewriter : CSharpSyntaxRewriter
        {
            private static readonly RemoveParameterNameSyntaxRewriter _instance = new RemoveParameterNameSyntaxRewriter();

            private readonly AttributeArgumentSyntax[] _argumments;

            private RemoveParameterNameSyntaxRewriter(AttributeArgumentSyntax[] arguments = null)
            {
                _argumments = arguments;
            }

            public static AttributeArgumentListSyntax VisitNode(AttributeArgumentListSyntax argumentList, AttributeArgumentSyntax[] arguments = null)
            {
                if (arguments == null)
                {
                    return (AttributeArgumentListSyntax)_instance.Visit(argumentList);
                }
                else
                {
                    var instance = new RemoveParameterNameSyntaxRewriter(arguments);
                    return (AttributeArgumentListSyntax)instance.Visit(argumentList);
                }
            }

            public override SyntaxNode VisitAttributeArgument(AttributeArgumentSyntax node)
            {
                if (_argumments == null || Array.IndexOf(_argumments, node) != -1)
                {
                    return node
                        .WithNameColon(null)
                        .WithTriviaFrom(node);
                }

                return base.VisitAttributeArgument(node);
            }
        }
    }
}
