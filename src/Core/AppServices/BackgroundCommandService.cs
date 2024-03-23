using ReactiveUI;

using System.IO;
using System.IO.Pipes;
using System.Reactive.Concurrency;
using System.Text;

namespace DivinityModManager.AppServices;

/// <summary>
/// This service allows shell commands opening BG3MM to communicate with the running instance, using pipes.
/// </summary>
public class BackgroundCommandService
{
	private NamedPipeServerStream _pipe;
	private IDisposable _backgroundTask;

	private async Task WaitForCommandAsync(IScheduler sch, CancellationToken token)
	{
		if (_pipe != null)
		{
			try
			{
				await _pipe.WaitForConnectionAsync(token);

				if (token.IsCancellationRequested) return;

				using (var sr = new StreamReader(_pipe, Encoding.UTF8))
				{
					var message = await sr.ReadToEndAsync();
					if (!String.IsNullOrEmpty(message))
					{
						if (message.IndexOf("nxm://") > -1)
						{
							var nexusMods = Services.Get<INexusModsService>();
							nexusMods.ProcessNXMLinkBackground(message);
						}
					}
				}
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error with server pipe:\n{ex}");
			}
		}

		if (token.IsCancellationRequested) return;

		RxApp.TaskpoolScheduler.Schedule(Restart);
	}

	public void Restart()
	{
		_backgroundTask?.Dispose();
		_pipe?.Dispose();
		try
		{
			_pipe = new NamedPipeServerStream(DivinityApp.PIPE_ID, PipeDirection.In, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
			_backgroundTask = RxApp.TaskpoolScheduler.ScheduleAsync(WaitForCommandAsync);
		}
		catch(IOException)
		{
			//Pipe already exists in another instance
			DivinityApp.Log($"Pipe already exists with id '{DivinityApp.PIPE_ID}'. Currently only one running instance can receive command args.");
		}
	}

	public BackgroundCommandService()
	{
		Restart();
	}
}
