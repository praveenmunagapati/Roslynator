﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Roslynator.CSharp.Refactorings
{
    internal static class ReplaceConditionalExpressionWithExpressionRefactoring
    {
        public static void ComputeRefactoring(RefactoringContext context, ExpressionSyntax expression)
        {
            SyntaxNode parent = expression.Parent;

            if (parent?.IsKind(SyntaxKind.ConditionalExpression) == true
                && context.Span.IsBetweenSpans(expression))
            {
                var conditionalExpression = (ConditionalExpressionSyntax)parent;

                if (expression.Equals(conditionalExpression.WhenTrue)
                    || expression.Equals(conditionalExpression.WhenFalse))
                {
                    context.RegisterRefactoring(
                        $"Replace ?: with '{expression}'",
                        cancellationToken => RefactorAsync(context.Document, expression, cancellationToken));
                }
            }
        }

        private static Task<Document> RefactorAsync(
            Document document,
            ExpressionSyntax expression,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            SyntaxNode parent = expression.Parent;

            ExpressionSyntax newNode = expression.WithTriviaFrom(parent);

            return document.ReplaceNodeAsync(parent, newNode, cancellationToken);
        }
    }
}
