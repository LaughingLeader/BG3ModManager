namespace ModManager.Util;

public class OrderNameChangedArgs : EventArgs
{
	public string LastName { get; set; }
	public string NewName { get; set; }
}

public class DivinityGlobalEvents
{
	public event EventHandler<OrderNameChangedArgs> OrderNameChanged;

	public void OnOrderNameChanged(string lastName, string newName)
	{
		var handler = OrderNameChanged;
		if (handler != null)
		{
			handler(this, new OrderNameChangedArgs { LastName = lastName, NewName = newName });
		}
	}
}
