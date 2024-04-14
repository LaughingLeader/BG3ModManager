using Microsoft.CodeAnalysis.Text;

using ModManager.Data;

using System.Text;

namespace ModManager.Generators;

[Generator]
public class SettingsViewGenerator : IIncrementalGenerator
{
	private const string GenerateViewAttributeName = "ModManager.GenerateViewAttribute";
	private const string SettingsEntryAttributeName = "ModManager.SettingsEntryAttribute";

	private static IEnumerable<ValueTuple<IPropertySymbol, AttributeData>> GetSettingsAttributes(IPropertySymbol propertySymbol)
	{
		foreach (var attribute in propertySymbol.GetAttributes())
		{
			if (attribute == null) continue;

			var attName = attribute.AttributeClass?.ToDisplayString();

			if (attName == GenerateViewAttributeName)
			{
				foreach (var sym in propertySymbol.Type.GetMembers())
				{
					if (sym == null) continue;

					if (sym is IPropertySymbol prop)
					{
						foreach (var propAttribute in prop.GetAttributes())
						{
							if (propAttribute == null) continue;

							var propAttName = propAttribute.AttributeClass?.ToDisplayString();

							if (propAttName == SettingsEntryAttributeName)
							{
								yield return (prop!, propAttribute!);
							}
						}
					}
				}
			}
		}
	}

	private static SettingsViewToGenerate? GetToGenerate(SemanticModel semanticModel, SyntaxNode declarationSyntax)
	{
		var symbol = semanticModel.GetDeclaredSymbol(declarationSyntax);

		if (symbol is IPropertySymbol propertySymbol)
		{
			var entries = new List<SettingsEntryData>();

			foreach (var entry in GetSettingsAttributes(propertySymbol))
			{
				entries.Add(SettingsEntryData.FromAttribute(entry.Item1, entry.Item2));
			}

			if(entries.Count > 0)
			{
				return new SettingsViewToGenerate(propertySymbol, entries);
			}
		}

		return null;
	}

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		// Add the marker attribute
		//context.RegisterPostInitializationOutput(static ctx => ctx.AddSource(
		//	"EnumExtensionsAttribute.g.cs", SourceText.From(SourceGenHelpers.SettingsEntry, Encoding.UTF8)));

		// If you're targeting the .NET 7 SDK, use this version instead:
		IncrementalValuesProvider<SettingsViewToGenerate?> entriesToGenerate = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				GenerateViewAttributeName,
				predicate: static (s, _) => true,
				transform: static (ctx, _) => GetToGenerate(ctx.SemanticModel, ctx.TargetNode))
			.Where(static m => m is not null);

		// Generate source code for each enum found
		context.RegisterSourceOutput(entriesToGenerate,
			static (spc, source) => Execute(source, spc));
	}

	static void Execute(in SettingsViewToGenerate? settingsToGenerate, SourceProductionContext context)
	{
		if (settingsToGenerate is { } sg)
		{
			var xaml = sg.ToXaml();
			var codeBehind = sg.ToCode();

			context.AddSource($"{sg.DisplayName}.g.axaml", SourceText.From(xaml, Encoding.UTF8));
			context.AddSource($"{sg.DisplayName}.g.axaml.cs", SourceText.From(codeBehind, Encoding.UTF8));
		}
	}
}
