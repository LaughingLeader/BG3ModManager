using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModManager.Services;
public interface IDialogService
{
	Task<OpenFileBrowserDialogResults> OpenFileAsync(OpenFileBrowserDialogRequest context);
	Task<OpenFileBrowserDialogResults> SaveFileAsync(OpenFileBrowserDialogRequest context);
}
