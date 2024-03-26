using System.Windows.Markup;
using System.Xaml;

namespace ModManager.Themes;

internal class ResourceAliasHelper : MarkupExtension
{
	public object ResourceKey { get; set; }

	public override object ProvideValue(IServiceProvider serviceProvider)
	{
		var rootObjectProvider = (IRootObjectProvider)serviceProvider.GetService(typeof(IRootObjectProvider));
		var dictionary = rootObjectProvider?.RootObject as IDictionary;
		return dictionary?[ResourceKey];
	}
}
