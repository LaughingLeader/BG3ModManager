using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace ModManager;

[Generator]
public class HotkeyGenerator : ISourceGenerator
{
	public void Initialize(GeneratorInitializationContext context)
	{
		context.RegisterForSyntaxNotifications(() => new HotkeySyntaxReceiver());
	}

	public void Execute(GeneratorExecutionContext context)
	{
		if (context.SyntaxContextReceiver is not HotkeySyntaxReceiver syntaxReciever) return;
		Debug.WriteLine($"Identifies props:{syntaxReciever.IdentifiedProperties}");

		var sourceBuilder = new StringBuilder();
		foreach (var containingClassGroup in syntaxReciever.IdentifiedProperties.GroupBy(x => x.Property))
		{
			var containingClass = containingClassGroup.Key.ContainingType;
			var namespc = containingClass.ContainingNamespace;
			var source = GenerateClass(context, containingClass, namespc, containingClassGroup.ToList());
			//source = CSharpSyntaxTree.ParseText(source).GetRoot().NormalizeWhitespace().ToFullString();
			var finalText = SourceText.From(source, Encoding.UTF8);
			context.AddSource($"{containingClass.Name}.Keybindings.g.cs", finalText);
		}
	}

	private string GenerateClass(GeneratorExecutionContext context, INamedTypeSymbol @class, INamespaceSymbol @namespace, List<HotkeyPropertyValue> properties)
	{
		var registrationBuilder = new StringBuilder();
		foreach (var prop in properties)
		{
			//public void RegisterCommand(string id, string displayName, IReactiveCommand command, Key key, ModifierKeys modifiers = KeyModifiers.None)
			var args = new List<string>
			{
				prop.Id ?? "",
				prop.DisplayName ?? "",
				prop.Property.Name
			};
			if (prop.Key != null) args.Add(prop.Key);
			if (prop.Modifiers != null) args.Add(prop.Modifiers);
			registrationBuilder.AppendLine($"\t\tkeys.RegisterCommand({string.Join(", ", args)});");
		}
		return @$"
namespace ModManager.ViewModels.Main;

public partial class MainCommandBarViewModel
{{
	public void RegisterKeybindings()
	{{
		var keys = Locator.Current.GetService<AppKeysService>();
		if (keys == null) throw new Exception(""Failed to get AppKeysService - Is it registered?"");

{registrationBuilder}
	}}
}}
";
	}
}