namespace ModManager.Models.Settings;

public interface ISerializableSettings : IReactiveNotifyPropertyChanged<IReactiveObject>, IHandleObservableErrors, IReactiveObject
{
	string FileName { get; }
	string? GetDirectory();
	bool SkipEmpty { get; }
}

public abstract class BaseSettings<T>(string fileName) : ReactiveObject where T : ISerializableSettings
{
	[JsonIgnore] public string FileName => fileName;
	[JsonIgnore] public bool SkipEmpty => false;

	public virtual string? GetDirectory() => DivinityApp.GetAppDirectory("Data");
}
