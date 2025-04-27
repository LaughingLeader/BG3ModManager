using System.Runtime.Serialization;

namespace ModManager.Models.Mod;

[DataContract]
public class ModLoadOrderEntry
{
	[DataMember]
	public string? UUID { get; set; }

	[DataMember]
	public string? Name { get; set; }
	public bool Missing { get; set; }

	public ModLoadOrderEntry Clone()
	{
		return new ModLoadOrderEntry() { Name = Name, UUID = UUID, Missing = Missing };
	}
}

[DataContract]
public class ModLoadOrder : ReactiveObject
{
	[Reactive] public string? Name { get; set; }
	[Reactive] public string? FilePath { get; set; }
	[Reactive] public DateTime LastModifiedDate { get; set; }
	[Reactive] public bool IsLoaded { get; set; }

	[Reactive] public bool IsModSettings { get; set; }

	/// <summary>
	/// This is an order from a non-standard order file (info .json, .txt, .tsv).
	/// </summary>
	[Reactive] public bool IsDecipheredOrder { get; set; }

	[ObservableAsProperty] public string? LastModified { get; }

	[DataMember]
	public List<ModLoadOrderEntry> Order { get; set; } = [];

	public void Add(IModEntry mod, bool force = false)
	{
		try
		{
			if (Order != null && mod != null)
			{
				if (force)
				{
					Order.Add(new()
					{
						Name = mod.DisplayName,
						UUID = mod.UUID
					});
				}
				else
				{
					if (Order.Count > 0)
					{
						var alreadyInOrder = false;
						foreach (var x in Order)
						{
							if (x != null && x.UUID == mod.UUID)
							{
								alreadyInOrder = true;
								break;
							}
						}
						if (!alreadyInOrder)
						{
							Order.Add(new()
							{
								Name = mod.DisplayName,
								UUID = mod.UUID
							});
						}
					}
					else
					{
						Order.Add(new()
						{
							Name = mod.DisplayName,
							UUID = mod.UUID
						});
					}
				}
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error adding mod to order:\n{ex}");
		}
	}

	public void Add(IModData mod, bool force = false)
	{
		try
		{
			if (Order != null && mod != null)
			{
				if (force)
				{
					Order.Add(new ModLoadOrderEntry
					{
						UUID = mod.UUID,
						Name = mod.Name,
					});
				}
				else
				{
					if (Order.Count > 0)
					{
						var alreadyInOrder = false;
						foreach (var x in Order)
						{
							if (x != null && x.UUID == mod.UUID)
							{
								alreadyInOrder = true;
								break;
							}
						}
						if (!alreadyInOrder)
						{
							Order.Add(new ModLoadOrderEntry
							{
								UUID = mod.UUID,
								Name = mod.Name,
							});
						}
					}
					else
					{
						Order.Add(new ModLoadOrderEntry
						{
							UUID = mod.UUID,
							Name = mod.Name,
						});
					}
				}
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error adding mod to order:\n{ex}");
		}
	}

	public void AddRange(IEnumerable<IModEntry> mods, bool replace = false)
	{
		foreach (var mod in mods)
		{
			Add(mod, replace);
		}
	}

	public void Remove(IModEntry mod)
	{
		try
		{
			if (Order != null && Order.Count > 0 && mod != null)
			{
				ModLoadOrderEntry entry = null;
				foreach (var x in Order)
				{
					if (x != null && x.UUID == mod.UUID)
					{
						entry = x;
						break;
					}
				}
				if (entry != null) Order.Remove(entry);
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error removing mod from order:\n{ex}");
		}
	}

	public void RemoveRange(IEnumerable<IModEntry> mods)
	{
		if (Order.Count > 0 && mods != null)
		{
			foreach (var mod in mods)
			{
				Remove(mod);
			}
		}
	}

	public void Sort(Comparison<ModLoadOrderEntry> comparison)
	{
		try
		{
			if (Order.Count > 1)
			{
				Order.Sort(comparison);
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error sorting order:\n{ex}");
		}
	}

	public void SetOrder(IEnumerable<ModLoadOrderEntry> nextOrder)
	{
		Order.Clear();
		Order.AddRange(nextOrder);
	}

	public void SetOrder(ModLoadOrder nextOrder)
	{
		Order.Clear();
		Order.AddRange(nextOrder.Order);
	}

	public bool OrderEquals(IEnumerable<string> orderList)
	{
		if (Order.Count > 0)
		{
			return Order.Select(x => x.UUID).SequenceEqual(orderList);
		}
		return false;
	}

	public ModLoadOrder Clone()
	{
		return new ModLoadOrder()
		{
			Name = Name,
			Order = Order.ToList(),
			LastModifiedDate = LastModifiedDate
		};
	}

	public ModLoadOrder()
	{
		this.WhenAnyValue(x => x.LastModifiedDate).Select(x => x.ToString("g")).ToUIProperty(this, x => x.LastModified, "");
	}
}
