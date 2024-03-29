using System.Reflection;
using System.Windows;

namespace ModManager.Util;

public class CustomPropertyResolver : ICreatesObservableForProperty
{
	public int GetAffinityForObject(Type type, string propertyName, bool beforeChanged = false)
	{
		if (!typeof(FrameworkElement).IsAssignableFrom(type))
			return 0;
		var fi = type.GetTypeInfo().GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
		  .FirstOrDefault(x => x.Name == propertyName);

		return fi != null ? 2 /* POCO affinity+1 */ : 0;
	}

	public IObservable<IObservedChange<object, object>> GetNotificationForProperty(object sender, System.Linq.Expressions.Expression expression, string propertyName, bool beforeChanged = false, bool suppressWarnings = false)
	{
		var foo = (FrameworkElement)sender;
		var value = sender.GetType().GetProperty(propertyName)?.GetValue(sender, null);
		return Observable.Return(new ObservedChange<object, object>(sender, expression, value), new DispatcherScheduler(foo.Dispatcher))
			.Concat(Observable.Never<IObservedChange<object, object>>());
	}
}
