﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Roslynator.CSharp.Refactorings
{
    internal static class SwitchStatementRefactoring
    {
        public static async Task ComputeRefactoringsAsync(RefactoringContext context, SwitchStatementSyntax switchStatement)
        {
            if (context.IsRefactoringEnabled(RefactoringIdentifiers.GenerateSwitchSections))
            {
                SemanticModel semanticModel = await context.GetSemanticModelAsync().ConfigureAwait(false);

                if (GenerateSwitchSectionsRefactoring.CanRefactor(switchStatement, semanticModel, context.CancellationToken))
                {
                    context.RegisterRefactoring(
                        "Generate sections",
                        cancellationToken => GenerateSwitchSectionsRefactoring.RefactorAsync(context.Document, switchStatement, cancellationToken));
                }
            }

            SelectedSwitchSectionsRefactoring.ComputeRefactorings(context, switchStatement);

            if (context.IsRefactoringEnabled(RefactoringIdentifiers.ReplaceSwitchWithIfElse))
            {
                TextSpan span = context.Span;

                if (span.IsEmptyAndContainedInSpan(switchStatement.SwitchKeyword)
                    || span.IsBetweenSpans(switchStatement))
                {
                    ReplaceSwitchWithIfElseRefactoring.ComputeRefactoring(context, switchStatement);
                }
            }
        }
    }
}