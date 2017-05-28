﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslynator.CSharp.Refactorings;

namespace Roslynator.CSharp.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CannotImplicitlyConvertTypeCodeFixProvider))]
    [Shared]
    public class CannotImplicitlyConvertTypeCodeFixProvider : BaseCodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(CSharpErrorCodes.CannotImplicitlyConvertType); }
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            if (!Settings.IsAnyCodeFixEnabled(
                CodeFixIdentifiers.ReplaceStringLiteralWithCharacterLiteral,
                CodeFixIdentifiers.UseYieldReturnInsteadOfReturn))
            {
                return;
            }

            SyntaxNode root = await context.GetSyntaxRootAsync().ConfigureAwait(false);

            SyntaxNode node = root.FindNode(context.Span, getInnermostNodeForTie: true);

            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                switch (diagnostic.Id)
                {
                    case CSharpErrorCodes.CannotImplicitlyConvertType:
                        {
                            if (node?.IsKind(SyntaxKind.StringLiteralExpression) == true)
                            {
                                var literalExpression = (LiteralExpressionSyntax)node;

                                if (literalExpression.Token.ValueText.Length == 1)
                                {
                                    SemanticModel semanticModel = await context.GetSemanticModelAsync().ConfigureAwait(false);

                                    if (semanticModel.GetTypeInfo(node, context.CancellationToken).ConvertedType?.IsChar() == true)
                                    {
                                        CodeAction codeAction = CodeAction.Create(
                                            "Replace string literal with character literal",
                                            cancellationToken => ReplaceStringLiteralWithCharacterLiteralRefactoring.RefactorAsync(context.Document, literalExpression, cancellationToken),
                                            CodeFixIdentifiers.ReplaceStringLiteralWithCharacterLiteral + EquivalenceKeySuffix);

                                        context.RegisterCodeFix(codeAction, context.Diagnostics);
                                    }
                                }
                            }

                            if (node.IsParentKind(SyntaxKind.ReturnStatement))
                            {
                                var returnStatement = (ReturnStatementSyntax)node.Parent;

                                SemanticModel semanticModel = await context.GetSemanticModelAsync().ConfigureAwait(false);

                                ISymbol containingSymbol = semanticModel.GetEnclosingSymbol(returnStatement.SpanStart, context.CancellationToken);

                                if (containingSymbol?.IsKind(SymbolKind.Method) == true
                                    && ((IMethodSymbol)containingSymbol).ReturnType?.IsIEnumerableOrConstructedFromIEnumerableOfT() == true)
                                {
                                    CodeAction codeAction = CodeAction.Create(
                                        "Use yield return instead of return",
                                        cancellationToken => UseYieldReturnInsteadOfReturnRefactoring.RefactorAsync(context.Document, returnStatement, SyntaxKind.YieldReturnStatement, semanticModel, cancellationToken),
                                        CodeFixIdentifiers.UseYieldReturnInsteadOfReturn + EquivalenceKeySuffix);

                                    context.RegisterCodeFix(codeAction, diagnostic);
                                }
                            }

                            break;
                        }
                }
            }
        }
    }
}