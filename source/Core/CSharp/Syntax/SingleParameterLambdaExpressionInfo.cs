﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Roslynator.CSharp.Syntax
{
    public struct SingleParameterLambdaExpressionInfo
    {
        private static SingleParameterLambdaExpressionInfo Default { get; } = new SingleParameterLambdaExpressionInfo();

        private SingleParameterLambdaExpressionInfo(
            LambdaExpressionSyntax lambdaExpression,
            ParameterSyntax parameter,
            CSharpSyntaxNode body)
        {
            LambdaExpression = lambdaExpression;
            Parameter = parameter;
            Body = body;
        }

        public LambdaExpressionSyntax LambdaExpression { get; }

        public ParameterSyntax Parameter { get; }

        public CSharpSyntaxNode Body { get; }

        public ParameterListSyntax ParameterList
        {
            get { return (IsParenthesizedLambda) ? (ParameterListSyntax)Parameter.Parent : null; }
        }

        public string ParameterName
        {
            get { return Parameter?.Identifier.ValueText; }
        }

        public bool IsSimpleLambda
        {
            get { return LambdaExpression?.Kind() == SyntaxKind.SimpleLambdaExpression; }
        }

        public bool IsParenthesizedLambda
        {
            get { return LambdaExpression?.Kind() == SyntaxKind.ParenthesizedLambdaExpression; }
        }

        public bool Success
        {
            get { return LambdaExpression != null; }
        }

        internal static SingleParameterLambdaExpressionInfo Create(
            SyntaxNode node,
            SyntaxInfoOptions options = null)
        {
            options = options ?? SyntaxInfoOptions.Default;

            return CreateCore(options.Walk(node) as LambdaExpressionSyntax, options);
        }

        internal static SingleParameterLambdaExpressionInfo Create(
            LambdaExpressionSyntax lambdaExpression,
            SyntaxInfoOptions options = null)
        {
            return CreateCore(lambdaExpression, options ?? SyntaxInfoOptions.Default);
        }

        internal static SingleParameterLambdaExpressionInfo CreateCore(
            LambdaExpressionSyntax lambdaExpression,
            SyntaxInfoOptions options = null)
        {
            switch (lambdaExpression?.Kind())
            {
                case SyntaxKind.SimpleLambdaExpression:
                    {
                        var simpleLambda = (SimpleLambdaExpressionSyntax)lambdaExpression;

                        ParameterSyntax parameter = simpleLambda.Parameter;

                        if (!options.Check(parameter))
                            break;

                        CSharpSyntaxNode body = simpleLambda.Body;

                        if (!options.Check(body))
                            break;

                        return new SingleParameterLambdaExpressionInfo(simpleLambda, parameter, body);
                    }
                case SyntaxKind.ParenthesizedLambdaExpression:
                    {
                        var parenthesizedLambda = (ParenthesizedLambdaExpressionSyntax)lambdaExpression;

                        ParameterSyntax parameter = parenthesizedLambda
                            .ParameterList?
                            .Parameters
                            .SingleOrDefault(throwException: false);

                        if (!options.Check(parameter))
                            break;

                        CSharpSyntaxNode body = parenthesizedLambda.Body;

                        if (!options.Check(body))
                            break;

                        return new SingleParameterLambdaExpressionInfo(parenthesizedLambda, parameter, body);
                    }
            }

            return Default;
        }

        public override string ToString()
        {
            return LambdaExpression?.ToString() ?? base.ToString();
        }
    }
}
