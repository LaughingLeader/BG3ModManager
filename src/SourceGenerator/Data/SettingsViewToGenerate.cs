﻿using ModManager.SourceGenerator.Utils;

using System.Text;

namespace ModManager.SourceGenerator.Data;
public readonly record struct SettingsViewToGenerate
{
	public readonly string DisplayName;
	public readonly string TypeName;
	public readonly string Namespace;
	public readonly List<SettingsEntryData> Entries;

#if DEBUG
	public readonly bool IsDebug = true;
#else
	public readonly bool IsDebug = false;
#endif

	public readonly string ClassName;

	public SettingsViewToGenerate(IPropertySymbol symbol, List<SettingsEntryData> entries)
	{
		DisplayName = $"{symbol.Type.Name}View";
		TypeName = symbol.Type.Name;
		Namespace = symbol.Type.ContainingNamespace.ToDisplayString();
		Entries = entries;

		ClassName = $"{DisplayName}.axaml";
	}

	public readonly string ToCode()
	{
		var code = new CodeBuilder();
		code.AppendLine("// <auto-generated/>");
		code.AppendLine($"using {Namespace};");
		code.AppendLine(string.Empty);
		code.AppendLine("namespace ModManager.Views.Generated;");
		code.AppendLine(string.Empty);
		code.AppendLine($"public partial class {DisplayName} : ReactiveUserControl<{TypeName}>");
		code.StartScope();
		code.AppendLine($"public {DisplayName}()");
		code.StartScope();
		code.AppendLine("InitializeComponent();");
		code.EndScope();

		code.EndScope();
		return code.ToString();
	}

	private const string DescriptionAttributeName = "System.ComponentModel.DescriptionAttribute";

	public readonly string ToXaml()
	{
		var code = new CodeBuilder();

		int totalRows = 0;

		code.StartScope("");
		code.StartScope("");

		foreach (var entry in Entries)
		{
			if (entry.DisableAutoGen) continue;

			var textBlockName = $"{entry.PropertyName}TextBlock";
			var tooltipBinding = $"{{Binding ElementName={textBlockName}, Path=(ToolTip.Tip)}}";

			code.AppendLine($"<TextBlock Classes=\"left\" x:Name=\"{textBlockName}\" Text=\"{entry.DisplayName}\" ToolTip.Tip=\"{entry.ToolTip}\" />");

			var controlText = "";
			var bindTo = !string.IsNullOrEmpty(entry.BindTo) ? entry.BindTo : entry.PropertyName;
			var isSingleLine = true;
			var addedRightColumn = true;

			switch (entry.PropertyTypeName)
			{
				case "Boolean":
					controlText = $"<CheckBox Classes=\"right\" IsChecked =\"{{Binding {bindTo}}}\" ToolTip.Tip=\"{tooltipBinding}\"";
					break;
				case "String":
					controlText = $"<TextBox Classes=\"compact\" Text=\"{{Binding {bindTo}}}\" ToolTip.Tip=\"{tooltipBinding}\"";
					break;
				case "TimeSpan":
					controlText = $"<controls:TimeSpanUpDown Classes=\"right\" Value=\"{{Binding {bindTo}}}\" ToolTip.Tip=\"{tooltipBinding}\"";
					break;
				default:
					if (entry.PropertyType.TypeKind == TypeKind.Enum)
					{
						isSingleLine = false;
						var comboText = $"<ComboBox Classes=\"right\" SelectedIndex=\"{{Binding {bindTo}, FallbackValue=0}}\" ToolTip.Tip=\"{tooltipBinding}\"";
						if (!string.IsNullOrEmpty(entry.BindVisibilityTo))
						{
							comboText += $" IsVisible=\"{{Binding {entry.BindVisibilityTo}}}\"";
						}
						comboText += ">";
						code.AppendLine(comboText);
						code.StartScope("");
						
						foreach(var member in entry.PropertyType.GetMembers())
						{
							//Avoid adding .ctor etc
							if (member.Kind == SymbolKind.Field)
							{
								string? tooltip = "";
								var attributes = member.GetAttributes();
								var descriptionAttribute = member.GetAttributes().FirstOrDefault(x => x.AttributeClass?.ToDisplayString() == DescriptionAttributeName);
								if (descriptionAttribute != null)
								{
									if (descriptionAttribute.ConstructorArguments.FirstOrDefault() is var arg && !arg.IsNull)
									{
										tooltip = arg.ToCSharpString().Replace("\"", string.Empty);
									}
									if (string.IsNullOrEmpty(tooltip) && descriptionAttribute.NamedArguments.FirstOrDefault(x => x.Key == "Name") is var namedArg)
									{
										tooltip = namedArg.Value.ToCSharpString().Replace("\"", string.Empty);
									}
								}

								tooltip ??= string.Empty;
								code.AppendLine($"<ComboBoxItem Content=\"{member.Name}\" ToolTip.Tip=\"{tooltip}\" />");
							}
						}

						code.EndScope("");
						code.AppendLine("</ComboBox>");
					}
					else
					{
						addedRightColumn = false;
					}
					break;
			}

			if(isSingleLine && addedRightColumn)
			{
				if (!string.IsNullOrEmpty(entry.BindVisibilityTo))
				{
					controlText += $" IsVisible=\"{{Binding {entry.BindVisibilityTo}}}\"";
				}
				controlText += "/>";

				code.AppendLine(controlText);
			}
			
			code.AppendLine(string.Empty);
			totalRows++;
		}
		return @$"<!--auto-generated-->
<UserControl
	x:Class=""ModManager.Views.Generated.{DisplayName}""
	xmlns=""https://github.com/avaloniaui""
	xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
	xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
	xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""
	xmlns:controls=""using:ModManager.Controls""
	xmlns:vm=""using:{Namespace}""
	x:DataType=""vm:{TypeName}""
	d:DesignHeight=""900""
    d:DesignWidth=""1600""
	mc:Ignorable=""d"">
	<controls:AutoGrid RowCount=""{totalRows}"" RowHeight=""auto"">
		<controls:AutoGrid.ColumnDefinitions>
			<ColumnDefinition Width=""*"" />
			<ColumnDefinition Width=""*"" />
		</controls:AutoGrid.ColumnDefinitions>

		{code.ToString().Trim()}
	</controls:AutoGrid>
</UserControl>";
	}
}