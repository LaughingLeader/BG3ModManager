namespace ModManager.Services.Dopus;

public readonly record struct DopusCommandArg(string Id, string[] Args)
{
	public override string ToString()
	{
		if(Args.Length > 0)
		{
			return $"{Id}={string.Join(',', Args)}";
		}
		return Id;
	}
}
