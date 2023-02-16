using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WoolBall.CommandLine;
using WoolBall.Services;

namespace WoolBall.Parsing;

internal sealed class TypeExtractor : ITypeExtractor
{
	public async IAsyncEnumerable<string> GetTypeNamesIn(Project project)
	{
		foreach (var document in project.Documents)
		{
			var model = await document.GetSemanticModelAsync();
			if (model == null)
				continue;

			var root = await model.SyntaxTree.GetRootAsync();
			var typeDeclarations = root.DescendantNodes().OfType<TypeDeclarationSyntax>();

			foreach (var typeDeclaration in typeDeclarations)
			{
				var typeSymbol = SymbolDisplayName(typeDeclaration, model);
				if (typeSymbol != null)
					yield return typeSymbol;
			}
		}
	}

	public async IAsyncEnumerable<Reference> GetTypeReferencesIn(Project project, ReferenceType referenceType)
	{
		foreach (var document in project.Documents)
		{
			await foreach (var typeReference in GetTypeReferencesIn(document, referenceType))
			{
				yield return typeReference;
			}
		}
	}

	private static IEnumerable<string> BaseTypeReferencesOf(BaseTypeSyntax baseTypeSyntax, SemanticModel model)
	{
		if (baseTypeSyntax.Type is GenericNameSyntax genericNameSyntax)
		{
			foreach (var type in ExtractGenericTypes(genericNameSyntax, model))
			{
				yield return type;
			}
		}
		else
		{
			var baseTypeSymbol = SymbolDisplayName(baseTypeSyntax.Type, model);
			if (baseTypeSymbol != null)
				yield return baseTypeSymbol;
		}
	}

	private static IEnumerable<string> ExtractBaseTypes(BaseTypeDeclarationSyntax typeDeclaration, SemanticModel model)
	{
		if (typeDeclaration.BaseList == null)
			yield break;

		foreach (var baseTypeSyntax in typeDeclaration.BaseList.Types)
		{
			foreach (var type in BaseTypeReferencesOf(baseTypeSyntax, model))
			{
				yield return type;
			}
		}
	}

	private static IEnumerable<string> ExtractGenericTypes(GenericNameSyntax genericNameSyntax, SemanticModel model)
	{
		var typeArguments = genericNameSyntax.TypeArgumentList.Arguments;
		foreach (var typeArgument in typeArguments)
		{
			var typeArgumentSymbol = model.GetSymbolInfo(typeArgument).Symbol;
			if (typeArgumentSymbol == null)
				continue;

			yield return typeArgumentSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
		}

		var genericTypeSymbol = model.GetSymbolInfo(genericNameSyntax).Symbol;
		if (genericTypeSymbol != null)
			yield return genericTypeSymbol.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
	}

	private static IEnumerable<string> ExtractTypeDependencies(TypeDeclarationSyntax typeDeclaration, SemanticModel model)
	{
		var methods = typeDeclaration.DescendantNodes().OfType<BaseMethodDeclarationSyntax>();

		foreach (var method in methods)
		{
			foreach (var parameter in method.ParameterList.Parameters)
			{
				if (parameter.Type == null)
					continue;

				var parameterType = SymbolDisplayName(parameter.Type, model);
				if (parameterType != null)
					yield return parameterType;
			}
		}
	}

	private static async IAsyncEnumerable<Reference> GetTypeReferencesIn(Document document, ReferenceType referenceType)
	{
		var model = await document.GetSemanticModelAsync();
		if (model == null)
			yield break;

		var root = await model.SyntaxTree.GetRootAsync();
		var typeDeclarations = root.DescendantNodes().OfType<TypeDeclarationSyntax>();

		foreach (var typeDeclaration in typeDeclarations)
		{
			var sourceType = SymbolDisplayName(typeDeclaration, model);
			if (sourceType == null)
				continue;

			foreach (var p in ReferencesOf(typeDeclaration, model, sourceType, referenceType))
			{
				yield return p;
			}
		}
	}

	private static IEnumerable<Reference> ReferencesOf(TypeDeclarationSyntax typeDeclaration, SemanticModel model, string sourceType, ReferenceType referenceType)
	{
		if (referenceType.HasFlag(ReferenceType.Inheritance))
		{
			foreach (var type in ExtractBaseTypes(typeDeclaration, model))
			{
				yield return new Reference(sourceType, type);
			}
		}

		if (referenceType.HasFlag(ReferenceType.Reference))
		{
			foreach (var type in ExtractTypeDependencies(typeDeclaration, model))
			{
				yield return new Reference(sourceType, type);
			}
		}
	}

	private static string? SymbolDisplayName(SyntaxNode type, SemanticModel model)
	{
		var baseTypeSymbol = model.GetSymbolInfo(type).Symbol;
		return baseTypeSymbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
	}

	private static string? SymbolDisplayName(TypeDeclarationSyntax declarationSyntax, SemanticModel model)
	{
		var typeSymbol = model.GetDeclaredSymbol(declarationSyntax);
		return typeSymbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
	}
}