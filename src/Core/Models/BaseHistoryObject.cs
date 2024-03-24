using ReactiveHistory;

using ReactiveUI;

using System.Reflection;
using System.Runtime.CompilerServices;

namespace ModManager.Models;

public class BaseHistoryObject : ReactiveObject
{
	public IHistory History { get; set; }

	public virtual void Snapshot(Action undo, Action redo)
	{
		History.Snapshot(undo, redo);
	}

	public bool UpdateWithHistory<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
	{
		if (!Equals(field, value))
		{
			if (History != null)
			{
				var undoValue = field;
				var redoValue = value;

				History.Snapshot(() =>
				{
					this.SetProperty(this, propertyName, undoValue);
				}, () =>
				{
					this.SetProperty(this, propertyName, redoValue);
				});
			}

			this.RaiseAndSetIfChanged(ref field, value, propertyName);
			return true;
		}
		return false;
	}

	public bool UpdateWithHistory<T>(ref T field, T value, Action undo, Action redo, [CallerMemberName] string propertyName = null)
	{
		if (!Equals(field, value))
		{
			if (History != null)
			{
				History.Snapshot(undo, redo);
			}

			this.RaiseAndSetIfChanged(ref field, value, propertyName);
			return true;
		}
		return false;
	}

	private bool SetProperty<T>(object targetObject, string propertyName, T value)
	{
		var prop = this.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.Instance);
		if (prop != null && prop.CanWrite)
		{
			prop.SetValue(this, value);
			return true;
		}
		return false;
	}

	private bool SetField<T>(string fieldName, T value, string propertyName = null)
	{
		var field = this.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
		if (field != null)
		{
			field.SetValue(this, value);
			return true;
		}
		return false;
	}
}
