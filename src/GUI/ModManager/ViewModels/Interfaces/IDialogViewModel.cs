using SukiUI.Dialogs;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModManager.ViewModels;
public interface IDialogViewModel
{
	ISukiDialog? Dialog { get; set; }
}
