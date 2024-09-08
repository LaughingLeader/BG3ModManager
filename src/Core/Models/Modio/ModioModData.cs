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
	public ModioMod? Data { get; set; }

	[Reactive] public string? ModId { get; set; }
	[Reactive] public bool IsEnabled { get; private set; }

	public void Update(ModioMod? data)
	{
		if (data != null) Data = data;
	}

	public ModioModData() : base()
	{
		this.WhenAnyValue(x => x.Data).WhereNotNull().Where(x => !String.IsNullOrEmpty(x.NameId)).Select(x => x.NameId).BindTo(this, x => x.ModId);
		this.WhenAnyValue(x => x.ModId).Select(x => !String.IsNullOrEmpty(x)).ObserveOn(RxApp.MainThreadScheduler).BindTo(this, x => x.IsEnabled);
	}
}
