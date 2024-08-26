using SukiUI.Controls;

using System.Reactive.Subjects;

namespace ModManager.ViewModels;
public class MessageBoxViewModel : ReactiveObject
{
	[Reactive] public string? Title { get; set; }
	[Reactive] public string? Message { get; set; }
	[Reactive] public bool IsVisible { get; set; }

	private readonly Subject<bool> Result = new();

	[Reactive] public InteractionMessageBoxType MessageBoxType { get; set; }

	[ObservableAsProperty] public string? ConfirmButtonText { get; }
	[ObservableAsProperty] public string? CancelButtonText { get; }
	[ObservableAsProperty] public bool CancelVisibility { get; }

	public RxCommandUnit ConfirmCommand { get; }
	public RxCommandUnit CancelCommand { get; }

	public void Open(string title, string message, InteractionMessageBoxType messageBoxType)
	{
		Title = title;
		Message = message;
		MessageBoxType = messageBoxType;
	}

	public void Open(ShowMessageBoxRequest request) => Open(request.Title, request.Message, request.MessageBoxType);

	public IObservable<bool> WaitForResult() => Result.Take(1);

	private void Close(bool result)
	{
		Result.OnNext(result);
	}

	private static string MessageBoxTypeToConfirmationText(InteractionMessageBoxType type)
	{
		if (type.HasFlag(InteractionMessageBoxType.YesNo))
		{
			return "Yes";
		}
		return "OK";
	}

	private static string MessageBoxTypeToCancelText(InteractionMessageBoxType type)
	{
		if (type.HasFlag(InteractionMessageBoxType.YesNo))
		{
			return "No";
		}
		return "Close";
	}

	public MessageBoxViewModel()
	{
		var canRunCommands = this.WhenAnyValue(x => x.IsVisible);

		ConfirmCommand = ReactiveCommand.Create(() => Close(true), canRunCommands);
		CancelCommand = ReactiveCommand.Create(() => Close(false), canRunCommands);

		this.WhenAnyValue(x => x.IsVisible).ObserveOn(RxApp.TaskpoolScheduler).Subscribe(b =>
		{
			if(!b)
			{
				Result.OnNext(false);
			}
		});

		var whenTypeChanges = this.WhenAnyValue(x => x.MessageBoxType);
		whenTypeChanges.Select(MessageBoxTypeToConfirmationText).ToUIProperty(this, x => x.ConfirmButtonText, "OK");
		whenTypeChanges.Select(MessageBoxTypeToCancelText).ToUIProperty(this, x => x.CancelButtonText, "Close");
		whenTypeChanges.Select(x => x.HasFlag(InteractionMessageBoxType.YesNo)).ToUIProperty(this, x => x.CancelVisibility);
	}
}

public class DesignMessageBoxViewModel : MessageBoxViewModel
{
	private const string _longText = @"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Aenean laoreet nisi ac aliquam pellentesque. Fusce lorem urna, varius nec gravida id, tempus vitae dolor. Aenean dignissim lectus in sapien tristique, at tristique neque elementum. Aenean at elit quam. Aliquam tristique aliquam lorem, pellentesque feugiat turpis ultricies nec. Quisque finibus et lorem eget fringilla. Sed lacinia elit neque, eu interdum tellus aliquam sed.

Suspendisse nunc ex, rutrum eu interdum vitae, venenatis ut urna. Sed pulvinar urna eget ante efficitur sagittis. Duis at sapien libero. Duis auctor lacus lacus, eget interdum justo gravida aliquet. Proin gravida, felis nec volutpat semper, est enim volutpat ex, at vestibulum lectus ligula sed mauris. Pellentesque ex augue, bibendum vel est non, auctor pretium mauris. Aenean in porttitor nulla. Nulla quis tellus magna. Nulla non arcu vitae nisl convallis aliquet sit amet eu justo. Mauris et pulvinar sem. Donec fringilla ante eget facilisis auctor. Integer vulputate facilisis augue, nec ultricies quam congue quis.

Maecenas ut arcu sit amet orci congue euismod. Fusce sed tincidunt ipsum. Sed maximus dolor tincidunt varius dictum. Mauris facilisis sodales ex ut auctor. Fusce eleifend elit nec varius dictum. Etiam convallis pulvinar egestas. Morbi semper aliquam pharetra. In efficitur neque tristique, dignissim magna vel, consectetur mauris. Quisque nunc tellus, commodo et enim vel, sagittis fermentum tellus.

Aenean tellus nisl, vestibulum blandit felis eget, tincidunt iaculis lorem. Duis pulvinar eget diam ac aliquam. Praesent lacus est, tincidunt vel ornare eu, aliquet vel lacus. Aenean felis risus, laoreet ac nulla in, sollicitudin pretium odio. Nullam sed viverra lacus. Maecenas ultricies sit amet sem ornare dictum. Nullam vel dapibus ipsum, sed sollicitudin nibh. Curabitur sed commodo nibh, eget facilisis ligula. Phasellus consequat nisl a dui venenatis, ac vestibulum nunc aliquam. Suspendisse sodales lectus in odio consequat suscipit. Cras sapien lectus, accumsan id vehicula non, malesuada ac orci. Mauris eleifend purus et odio dapibus, sed consequat eros bibendum. Maecenas euismod leo mauris, id laoreet mauris lacinia at. Fusce suscipit cursus augue, mattis accumsan arcu placerat id. Suspendisse quis arcu elementum, vulputate tellus ut, tincidunt purus.

In id condimentum nibh. Quisque ac nulla id quam ultrices pulvinar eu iaculis ipsum. Sed non diam et libero tempus dapibus quis ultrices nisi. Ut libero dolor, efficitur non commodo sagittis, semper ut augue. Sed faucibus, velit a tempor hendrerit, tortor nisi finibus orci, vel laoreet leo lectus eget enim. Vivamus semper fringilla sapien, non pretium mi ullamcorper posuere. Maecenas pulvinar eros pellentesque, mollis turpis nec, rutrum nibh. Quisque euismod felis tellus, eu auctor dui suscipit vitae. Integer lobortis ultricies massa sed aliquet.";

	public DesignMessageBoxViewModel() : base()
	{
		Title = "Confirm Deletion";
		Message = "Really delete file(s)?\nThis cannot be undone.\n" + _longText;
		MessageBoxType = InteractionMessageBoxType.Error | InteractionMessageBoxType.YesNo;
	}
}