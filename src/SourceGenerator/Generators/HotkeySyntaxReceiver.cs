using Microsoft.CodeAnalysis.CSharp.Syntax;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ModManager.Generators;

public record struct HotkeyPropertyValue(IPropertySymbol Property, string? Id = null, string? DisplayName = null, string? Key = null, string? Modifiers = null);

internal class HotkeySyntaxReceiver : ISyntaxContextReceiver
{
	public List<HotkeyPropertyValue> IdentifiedProperties { get; } = [];

	private const string AttributeName = "ModManager.KeybindingAttribute";

	public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
	{
		INamedTypeSymbol? keybindingAttribute = context.SemanticModel.Compilation.GetTypeByMetadataName(AttributeName);

		if (context.Node is PropertyDeclarationSyntax declaration && declaration.AttributeLists.Any())
		{
			var propertySymbol = context.SemanticModel.GetDeclaredSymbol(declaration) as IPropertySymbol;

			foreach (var attributeListSyntax in declaration.AttributeLists)
			{
				foreach (var attributeSyntax in attributeListSyntax.Attributes)
				{
					var attributeSymbol = ModelExtensions.GetSymbolInfo(context.SemanticModel, attributeSyntax).Symbol;
					var fullName = attributeSymbol?.ContainingType?.ToDisplayString() ?? "";

					if(fullName.Contains("Keybinding"))
					{
						Debug.WriteLine(declaration.Identifier.ValueText);
					}

					//if (!keybindingAttribute.Equals(attributeSyntax, SymbolEqualityComparer.Default))
					//{
					//	continue;
					//}

					if (fullName == AttributeName)
					{
						var attributeDeclarationSymbol = context.SemanticModel.GetDeclaredSymbol(attributeSyntax);
						string? displayName = null;
						string? key = null;
						string? modifiers = null;

						foreach(var arg in attributeSyntax.GetArguments())
						{
							if(arg.IsNameOrPosition("DisplayName", 0))
							{
								displayName = arg.Value;
							}
							else if (arg.IsNameOrPosition("Key", 1))
							{
								key = arg.Value;
							}
							else if (arg.IsNameOrPosition("Modifiers", 2))
							{
								modifiers = arg.Value;
							}
						}

						string? id = displayName != null ? displayName.Trim().Replace(" ", "") : "";

						IdentifiedProperties.Add(new HotkeyPropertyValue(propertySymbol, id, displayName, key, modifiers));
					}
				}
			}
		}
	}
}