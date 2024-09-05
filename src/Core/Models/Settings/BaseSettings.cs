namespace ModManager.Models.Settings;

public interface ISerializableSettings
{
	string FileName { get; }
	string GetDirectory();
}

public abstract class BaseSettings<T>(string fileName) : ReactiveObject where T : ISerializableSettings
{
	[JsonIgnore] public string FileName { get; } = fileName;

	public virtual string GetDirectory() => DivinityApp.GetAppDirectory("Data");
}
