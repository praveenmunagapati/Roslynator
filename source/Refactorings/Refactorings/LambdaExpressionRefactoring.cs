﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Roslynator.CSharp.Refactorings
{
    internal static class LambdaExpressionRefactoring
    {
        public static void ComputeRefactorings(RefactoringContext context, LambdaExpressionSyntax lambda)
        {
            if (context.IsRefactoringEnabled(RefactoringIdentifiers.ExpandLambdaExpressionBody)
                && ExpandLambdaExpressionBodyRefactoring.CanRefactor(context, lambda))
            {
                context.RegisterRefactoring(
                    "Expand lambda expression body",
                    cancellationToken =>
                    {
                        return ExpandLambdaExpressionBodyRefactoring.RefactorAsync(
                            context.Document,
                            lambda,
                            (ExpressionSyntax)lambda.Body,
                            cancellationToken);
                    });
            }

            if (context.IsRefactoringEnabled(RefactoringIdentifiers.SimplifyLambdaExpression)
                && SimplifyLambdaExpressionRefactoring.CanRefactor(lambda))
            {
                context.RegisterRefactoring(
                    "Simplify lambda expression",
                    cancellationToken =>
                    {
                        return SimplifyLambdaExpressionRefactoring.RefactorAsync(
                            context.Document,
                            lambda,
                            cancellationToken);
                    });
            }
        }
    }
}
