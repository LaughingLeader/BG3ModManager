using System.ComponentModel;

namespace ModManager.Models;

public interface ISelectable : INotifyPropertyChanged
{
	bool IsSelected { get; set; }
	bool IsHidden { get; set; }
	bool IsDraggable { get; }
}
