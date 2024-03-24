namespace ModManager.Models.NexusMods.NXM;

public interface INexusModsProtocol
{
	string GameDomain { get; set; }

	bool IsValid { get; }
	string AsUrl { get; }
	NexusModsProtocolType ProtocolType { get; }
}
