using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModManager.Services;
public interface IScreenReaderService
{
	bool IsScreenReaderActive();
	void Output(string text, bool interrupt = false);
	void Speak(string text, bool interrupt = false);
	void Close();
	void Silence();
}
