namespace ModManager.Services.Dopus;

public readonly record struct DopusCommand(string Id, DopusCommandArg[] Args)
{
	public override string ToString()
	{
		if(Args.Length > 0)
		{
			return $"{Id} {string.Join(' ', Args)}";
		}
		return Id;
	}

	public string ToStringWithArg(string arg, bool escape = false)
	{
		if (escape) arg = $"\"{arg}\"";

		if(Args.Length > 0)
		{
			return $"{Id} {arg} {string.Join(' ', Args)}";
		}
		return $"{Id} {arg}";
	}
}
