using System.Windows;

namespace ModManager.Models;

public interface ISelectable
{
	bool IsSelected { get; set; }
	Visibility Visibility { get; set; }
	bool CanDrag { get; }
}
