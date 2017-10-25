﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Roslynator.CSharp.Refactorings
{
    internal static class FormatBinaryExpressionRefactoring
    {
        public static void ComputeRefactorings(RefactoringContext context, BinaryExpressionSyntax binaryExpression)
        {
            binaryExpression = GetBinaryExpression(binaryExpression, context.Span);

            if (binaryExpression != null
                && IsFormattableKind(binaryExpression.Kind()))
            {
                if (binaryExpression.IsSingleLine())
                {
                    context.RegisterRefactoring(
                        "Format binary expression on multiple lines",
                        cancellationToken => CSharpFormatter.ToMultiLineAsync(context.Document, binaryExpression, cancellationToken));
                }
                else
                {
                    context.RegisterRefactoring(
                        "Format binary expression on a single line",
                        cancellationToken => CSharpFormatter.ToSingleLineAsync(context.Document, binaryExpression, cancellationToken));
                }
            }
        }

        private static BinaryExpressionSyntax GetBinaryExpression(BinaryExpressionSyntax binaryExpression, TextSpan span)
        {
            if (span.IsEmpty)
            {
                return GetTopmostBinaryExpression(binaryExpression);
            }
            else if (span.IsBetweenSpans(binaryExpression)
                && binaryExpression == GetTopmostBinaryExpression(binaryExpression))
            {
                return binaryExpression;
            }

            return null;
        }

        private static bool IsFormattableKind(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.LogicalAndExpression:
                case SyntaxKind.LogicalOrExpression:
                case SyntaxKind.BitwiseAndExpression:
                case SyntaxKind.BitwiseOrExpression:
                    return true;
                default:
                    return false;
            }
        }

        private static BinaryExpressionSyntax GetTopmostBinaryExpression(BinaryExpressionSyntax binaryExpression)
        {
            bool success = true;

            while (success)
            {
                success = false;

                if (binaryExpression.Parent != null
                    && IsFormattableKind(binaryExpression.Parent.Kind()))
                {
                    var parent = (BinaryExpressionSyntax)binaryExpression.Parent;

                    if (parent.Left?.IsMissing == false
                        && parent.Right?.IsMissing == false)
                    {
                        binaryExpression = parent;
                        success = true;
                    }
                }
            }

            return binaryExpression;
        }
    }
}
