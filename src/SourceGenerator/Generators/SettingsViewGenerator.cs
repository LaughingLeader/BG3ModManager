using ModManager.SourceGenerator.Data;

using System.Diagnostics;
using System.IO;
using System.Text;

namespace ModManager.Generators;

[Generator]
public class SettingsViewGenerator : IIncrementalGenerator
{
	private const string ViewGeneratorAttributeName = "ModManager.ViewGeneratorAttribute";
	private const string GenerateViewAttributeName = "ModManager.GenerateViewAttribute";
	private const string SettingsEntryAttributeName = "ModManager.SettingsEntryAttribute";

	private static IncrementalValueProvider<string?>? projectDirProvider;

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

	private static ValueTuple<INamedTypeSymbol, List<SettingsViewToGenerate>>? GetToGenerate(SemanticModel semanticModel, SyntaxNode declarationSyntax)
	{
		var symbol = semanticModel.GetDeclaredSymbol(declarationSyntax);

		if (symbol is INamedTypeSymbol typeSymbol)
		{
			var views = new List<SettingsViewToGenerate>();

			foreach (var prop in typeSymbol.GetMembers())
			{
				if (prop is IPropertySymbol propertySymbol)
				{
					var entries = new List<SettingsEntryData>();

					foreach (var entry in GetSettingsAttributes(propertySymbol))
					{
						entries.Add(SettingsEntryData.FromAttribute(entry.Item1, entry.Item2));
					}

					if (entries.Count > 0)
					{
						views.Add(new SettingsViewToGenerate(propertySymbol, entries));
					}
				}
			}

			if (views.Count > 0)
			{
				return (typeSymbol, views);
			}
		}

		return null;
	}

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var entriesToGenerate = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				ViewGeneratorAttributeName,
				predicate: static (s, _) => s.IsKind(SyntaxKind.ClassDeclaration),
				transform: static (ctx, _) => GetToGenerate(ctx.SemanticModel, ctx.TargetNode))
			.Where(static x => x is not null)
			.Collect()
			.SelectMany((entries, _) => entries.Distinct());

		IncrementalValueProvider<string?> projectDirProvider = context.AnalyzerConfigOptionsProvider
		.Select(static (provider, _) => {
			provider.GlobalOptions.TryGetValue("build_property.projectdir", out string? projectDirectory);

			return projectDirectory;
		});

		var sourceOuput = entriesToGenerate.Combine(projectDirProvider);
		context.RegisterSourceOutput(sourceOuput, Generate);
	}

	private static void WriteFile(string outputPath, string contents)
	{
		using var writer = new StreamWriter(new FileStream(outputPath, FileMode.Create, FileAccess.Write), new UTF8Encoding(false, false));
		writer.Write(contents);
		writer.Flush();
		writer.Close();
	}

	private static void Generate(SourceProductionContext context, ValueTuple<ValueTuple<INamedTypeSymbol, List<SettingsViewToGenerate>>?, string?> x)
	{
		var (obj, projectDir) = x;
		if(obj?.Item2 is { } entries && projectDir is string projectDirectory)
		{
			var outputDir = Path.Combine(projectDirectory, "Views/Generated/")!;

			var names = string.Join(";", entries.Select(x => x.ClassName));
			Trace.WriteLine($"[SettingsViewGenerator] Names: {names}");
			Trace.WriteLine("");

			foreach (var entry in entries)
			{
				Trace.WriteLine($"[SettingsViewGenerator] Generating view/class for {entry.DisplayName}");
				try
				{
					//context.AddSource(entry.ClassName, SourceText.From(entry.ToXaml(), Encoding.UTF8));
					//context.AddSource($"{entry.ClassName}.cs", SourceText.From(entry.ToCode(), Encoding.UTF8));

					var outputPath = Path.Combine(outputDir, entry.ClassName)!;

					Directory.CreateDirectory(outputDir);

					WriteFile(Path.Combine(outputDir, entry.ClassName)!, entry.ToXaml());
					WriteFile(Path.Combine(outputDir, $"{entry.ClassName}.cs")!, entry.ToCode());
				}
				catch(Exception ex)
				{
					Trace.WriteLine($"[SettingsViewGenerator] Error:\n{ex}");
				}
			}
		}
	}
}
