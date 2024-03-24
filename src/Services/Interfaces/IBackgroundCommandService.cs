namespace ModManager;

/// <summary>
/// This service allows shell commands opening BG3MM to communicate with the running instance, using pipes.
/// </summary>
public interface IBackgroundCommandService
{
	/// <summary>
	/// Restarts the NamedPipeServerStream.
	/// </summary>
	void Restart();
}
