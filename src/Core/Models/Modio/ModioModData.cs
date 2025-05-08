using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Modio.Models;

namespace ModManager.Models.Modio;
public class ModioModData : ReactiveObject
{
	[MemberNotNullWhen(true, nameof(IsEnabled))]
	[Reactive] public ModioMod? Data { get; set; }

	[Reactive] public uint Id { get; set; }
	[Reactive] public string? NameId { get; set; }
	[Reactive] public bool IsEnabled { get; private set; }

	[ObservableAsProperty] public string? Description { get; }
	[ObservableAsProperty] public DateTimeOffset LastUpdated { get; }
	[ObservableAsProperty] public string? ExternalLink { get; }
	[ObservableAsProperty] public string? Author { get; }

	private static readonly string _modPageUrlPattern = "https://mod.io/g/baldursgate3/m/{0}";

	public void Update(ModioMod? data)
	{
		if (data != null) Data = data;
	}

	public ModioModData() : base()
	{
		var whenData = this.WhenAnyValue(x => x.Data).WhereNotNull();
		whenData.Select(x => x.Id).BindTo(this, x => x.Id);
		whenData.Where(x => x.NameId.IsValid()).Select(x => x.NameId).BindTo(this, x => x.NameId);

		whenData.Select(x => x.DescriptionPlaintext).ToUIProperty(this, x => x.Description);
		whenData.Select(x => x.DateUpdated).Select(DateTimeOffset.FromUnixTimeSeconds).ToUIProperty(this, x => x.LastUpdated);
		whenData.Select(x => x.NameId).Select(x => string.Format(_modPageUrlPattern, x)).ToUIProperty(this, x => x.ExternalLink);
		whenData.Select(x => x.SubmittedBy?.Username).ToUIProperty(this, x => x.Author);

		this.WhenAnyValue(x => x.Id).Select(x => x != 0).ObserveOn(RxApp.MainThreadScheduler).BindTo(this, x => x.IsEnabled);
	}
}
