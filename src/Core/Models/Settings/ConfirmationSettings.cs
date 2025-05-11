using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModManager.Models.Settings;

[DataContract]
public class ConfirmationSettings : ReactiveObject
{
	[Reactive, DataMember] public bool DisableAdminModeWarning { get; set; }
}
