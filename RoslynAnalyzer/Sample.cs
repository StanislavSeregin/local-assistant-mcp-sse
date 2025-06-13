using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RoslynAnalyzer;

public static class Sample
{
    public static async Task Kek(string solutionPath, CancellationToken cancellationToken)
    {
        var workspace = MSBuildWorkspace.Create();
        var solution = await workspace.OpenSolutionAsync(solutionPath, cancellationToken: cancellationToken);
        var project = solution.Projects.Where(x => x.Name == "Trash.Api").FirstOrDefault() ?? throw new InvalidOperationException();

        List<ClassDeclarationSyntax> classDeclarationSyntaxes = [];
        foreach (var document in project.Documents)
        {
            if (await document.GetSemanticModelAsync(cancellationToken) is { } model
                && await document.GetSyntaxRootAsync(cancellationToken) is { } syntaxRoot)
            {
                var controllerClasses = syntaxRoot.DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Where(c => IsController(c, model))
                    .ToArray();

                classDeclarationSyntaxes.AddRange(controllerClasses);
            }
        }

        var data = classDeclarationSyntaxes;
    }

    private static bool IsController(ClassDeclarationSyntax classDecl, SemanticModel model)
    {
        var symbol = model.GetDeclaredSymbol(classDecl) as ITypeSymbol;
        return symbol?.BaseType?.ToString() is "Microsoft.AspNetCore.Mvc.Controller"
            or "Microsoft.AspNetCore.Mvc.ControllerBase";
    }
}
