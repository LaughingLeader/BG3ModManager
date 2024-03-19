using DivinityModManager.Extensions;
using DivinityModManager.Models.Mod;

using LSLib.LS;

using System.IO;

namespace DivinityModManager.Models;

[ScreenReaderHelper(Name = "DisplayName", HelpText = "HelpText")]
public class DivinityGameMasterCampaign : DivinityModData
{
	public Resource MetaResource { get; set; }

	public bool Export(IEnumerable<DivinityModData> order)
	{
		try
		{
			var conversionParams = ResourceConversionParameters.FromGameVersion(DivinityApp.GAME);
			if (File.Exists(FilePath) && new FileInfo(FilePath)?.Length > 0)
			{
				var backupName = Path.Join(Path.GetDirectoryName(FilePath), FileName + ".backup");
				File.Copy(FilePath, backupName, true);
			}

			if (MetaResource.TryFindNode("Dependencies", out var dependenciesNode))
			{
				if (dependenciesNode.Children.TryGetValue("ModuleShortDesc", out var nodeList))
				{
					nodeList.Clear();
					foreach (var m in order)
					{
						var attributes = new Dictionary<string, NodeAttribute>()
						{
							{ "UUID", new NodeAttribute(AttributeType.FixedString) {Value = m.UUID}},
							{ "Name", new NodeAttribute(AttributeType.LSString) {Value = m.Name}},
							{ "Version", new NodeAttribute(AttributeType.Int) {Value = m.Version.VersionInt}},
							{ "MD5", new NodeAttribute(AttributeType.LSString) {Value = m.MD5}},
							{ "Folder", new NodeAttribute(AttributeType.LSString) {Value = m.Folder}},
						};
						var modNode = new Node()
						{
							Name = "ModuleShortDesc",
							Parent = dependenciesNode,
							Attributes = attributes,
							Children = new Dictionary<string, List<Node>>()
						};
						dependenciesNode.AppendChild(modNode);
						//nodeList.Add(modNode);
					}
				}
			}
			ResourceUtils.SaveResource(MetaResource, FilePath, LSLib.LS.Enums.ResourceFormat.LSF, conversionParams);
			if (File.Exists(FilePath))
			{
				File.SetLastWriteTime(FilePath, DateTime.Now);
				File.SetLastAccessTime(FilePath, DateTime.Now);
				LastModified = DateTimeOffset.Now;
				DivinityApp.Log($"Wrote GM campaign metadata to {FilePath}");
			}
			return true;
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error saving GM Campaign meta.lsf:\n{ex}");
		}
		return false;
	}
}
