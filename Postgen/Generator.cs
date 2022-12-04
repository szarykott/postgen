using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Postgen.Model;

namespace Postgen;

[Generator]
public class HelloSourceGenerator : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        var compilation = context.Compilation;
        
        var controllerTypeSymbol = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.ControllerBase");
        var controllerRouteSymbol = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.RouteAttribute");
        var httpMethodAttributeSymbol = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute");
        
        if (controllerTypeSymbol is null 
            || controllerRouteSymbol is null 
            || httpMethodAttributeSymbol is null)
        {
            return;
        }

        var results = new List<(ControllerDescriptor, IEnumerable<ControllerMethodDescriptor>)>();
        
        IEnumerable<SemanticModel> semanticModels = compilation.SyntaxTrees.Select(x => compilation.GetSemanticModel(x));
        foreach (var semanticModel in semanticModels)
        {
            var controllersWithMethods = GetControllerSymbols(semanticModel, controllerTypeSymbol)
                .Select(controller => ( 
                        GetControllerDescriptor(semanticModel, controller, controllerRouteSymbol),
                        GetControllerMethods(semanticModel, controller, controllerRouteSymbol, httpMethodAttributeSymbol)
                            .Select(x => GetControllerMethodDescriptor(semanticModel, x, controllerRouteSymbol, httpMethodAttributeSymbol))
                    ));

            results.AddRange(controllersWithMethods);
        }
        
        var applicationDescriptor = new ApplicationDescriptor
        {
            Controllers = results
        };
            
        CollectionWriter.WriteV21("postman.collection.g.nocommit.json", applicationDescriptor);
        
        Logger.Flush();
    }

    private IEnumerable<ClassDeclarationSyntax> GetControllerSymbols(SemanticModel semanticModel, INamedTypeSymbol controllerSymbol)
    {
        IEnumerable<ClassDeclarationSyntax> classDeclarations = semanticModel.SyntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();
        foreach (var classDeclaration in classDeclarations)
        {
            if (semanticModel.GetDeclaredSymbol(classDeclaration) is INamedTypeSymbol classSymbol && InheritsFrom(controllerSymbol, classSymbol))
            {
                yield return classDeclaration;
            }
        }
    }

    private IEnumerable<MethodDeclarationSyntax> GetControllerMethods(
        SemanticModel semanticModel, 
        ClassDeclarationSyntax controllerSymbol,
        INamedTypeSymbol controllerRouteSymbol,
        INamedTypeSymbol httpMethodAttributeSymbol)
    {
        var methodDeclarations = controllerSymbol.DescendantNodes().OfType<MethodDeclarationSyntax>();
        foreach (MethodDeclarationSyntax methodDeclaration in methodDeclarations)
        {
            IMethodSymbol? methodSymbol = (IMethodSymbol?)semanticModel.GetDeclaredSymbol(methodDeclaration);

            if (methodSymbol is null)
                continue;
            
            Logger.Log($"Found method {methodSymbol}");

            var attributes = methodSymbol.GetAttributes();

            var isPublic = methodSymbol.DeclaredAccessibility == Accessibility.Public;
            var methodRouteAttributeSymbol = attributes.FirstOrDefault(x => InheritsFrom(controllerRouteSymbol, x.AttributeClass!));
            var methodHttpAttributeSymbol = attributes.FirstOrDefault(x => InheritsFrom(httpMethodAttributeSymbol, x.AttributeClass!));

            if (isPublic && (methodRouteAttributeSymbol is not null || methodHttpAttributeSymbol is not null))
            {
                yield return methodDeclaration;
            }
        }
    }

    private ControllerDescriptor GetControllerDescriptor(SemanticModel semanticModel, ClassDeclarationSyntax controller, INamedTypeSymbol routeAttribute)
    {
        var idx = controller.Identifier.Text.IndexOf("Controller", StringComparison.OrdinalIgnoreCase);
        var name = controller.Identifier.Text[..idx];    
        
        var controllerSymbol = semanticModel.GetDeclaredSymbol(controller);
        var routeAttributeData = controllerSymbol?.GetAttributes().FirstOrDefault(attr => attr.AttributeClass?.Equals(routeAttribute, SymbolEqualityComparer.Default) ?? false);
        var routeArguments = routeAttributeData?.ConstructorArguments;
        if (routeArguments is { IsDefaultOrEmpty: false })
        {
            var argument = routeArguments.Value.First().Value;
            if (argument is string routePrefix)
            {
                var routePrefixSubstituted = routePrefix.Replace("[controller]", name);
                return new ControllerDescriptor(name, routePrefixSubstituted);
            }
        }

        return new ControllerDescriptor(name, null);
    }
    
    private ControllerMethodDescriptor GetControllerMethodDescriptor(
        SemanticModel semanticModel, 
        MethodDeclarationSyntax method, 
        INamedTypeSymbol routeAttribute,
        INamedTypeSymbol httpMethodAttribute)
    {
        var controllerMethodDescriptor = new ControllerMethodDescriptor
        {
            Name = method.Identifier.Text
        };
        
        var methodSymbol = semanticModel.GetDeclaredSymbol(method);
        
        var attributes = methodSymbol?.GetAttributes();
        var methodRouteAttributeSymbol = attributes?.FirstOrDefault(x => InheritsFrom(routeAttribute, x.AttributeClass!));
        var methodHttpAttributeSymbol = attributes?.FirstOrDefault(x => InheritsFrom(httpMethodAttribute, x.AttributeClass!));

        if (methodHttpAttributeSymbol is not null)
        {
            controllerMethodDescriptor.HttpMethod = methodHttpAttributeSymbol.AttributeClass?.Name[4..^9].ToUpper()!;
            var argument = methodHttpAttributeSymbol.ConstructorArguments.FirstOrDefault().Value;
            if (argument is string routePrefix)
            {
                controllerMethodDescriptor.Route = routePrefix;
            }
        }

        if (methodRouteAttributeSymbol is not null && controllerMethodDescriptor.Route is not null)
        {
            var argument = methodRouteAttributeSymbol.ConstructorArguments.FirstOrDefault().Value;
            if (argument is string routePrefix)
            {
                controllerMethodDescriptor.Route = routePrefix;
            }           
        }

        return controllerMethodDescriptor;
    }
    
    private bool InheritsFrom(INamedTypeSymbol baseClass, INamedTypeSymbol subject)
    {
        var currentSubject = subject;
        while (currentSubject.BaseType is not null)
        {
            if (currentSubject.BaseType.Equals(baseClass, SymbolEqualityComparer.Default))
            {
                return true;
            }

            currentSubject = currentSubject.BaseType;
        }

        return false;
    }
    
    public void Initialize(GeneratorInitializationContext context)
    {
#if DEBUG
        if (!Debugger.IsAttached)
        {
            Debugger.Launch();
        }
#endif 
        
        // No initialization required for this one
    }
}
