using System.Windows;

namespace ModManager.Models;

public interface ISelectable
{
	bool IsSelected { get; set; }
	bool IsVisible { get; set; }
	bool CanDrag { get; }
}
