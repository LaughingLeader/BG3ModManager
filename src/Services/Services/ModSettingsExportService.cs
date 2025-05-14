using ModManager.Models.Mod;
using ModManager.Util;

using System.IO;
using System.Xml;

namespace ModManager.Services;

public record struct ModSettingsPattern(string ModSettingsTemplate, string ModuleShortDescPattern, string ModuleShortDescPatternFormatted, bool IsFixedString = false);

public class ModSettingsExportService(IFileSystemService fileSystemService) : IModSettingsExportService
{
	private readonly IFileSystemService _fs = fileSystemService;

	public const string XML_MODULE_SHORT_DESC_GUID = @"<node id=""ModuleShortDesc""><attribute id=""Folder"" type=""LSString"" value=""{0}""/><attribute id=""MD5"" type=""LSString"" value=""{1}""/><attribute id=""Name"" type=""LSString"" value=""{2}""/><attribute id=""PublishHandle"" type=""uint64"" value=""{5}""/><attribute id=""UUID"" type=""guid"" value=""{3}""/><attribute id=""Version64"" type=""int64"" value=""{4}""/></node>";

	public const string XML_MODULE_SHORT_DESC_FORMATTED_GUID = "<node id=\"ModuleShortDesc\">\n\t<attribute id=\"Folder\" type=\"LSString\" value=\"{0}\"/>\n\t<attribute id=\"MD5\" type=\"LSString\" value=\"{1}\"/>\n\t<attribute id=\"Name\" type=\"LSString\" value=\"{2}\"/>\n\t<attribute id=\"PublishHandle\" type=\"uint64\" value=\"{5}\"/>\n\t<attribute id=\"UUID\" type=\"guid\" value=\"{3}\"/>\n\t<attribute id=\"Version64\" type=\"int64\" value=\"{4}\"/>\n</node>";

	public const string XML_MODULE_SHORT_DESC_FIXEDSTRING = @"<node id=""ModuleShortDesc""><attribute id=""Folder"" type=""LSString"" value=""{0}""/><attribute id=""MD5"" type=""LSString"" value=""{1}""/><attribute id=""Name"" type=""LSString"" value=""{2}""/><attribute id=""UUID"" type=""FixedString"" value=""{3}""/><attribute id=""Version64"" type=""int64"" value=""{4}""/></node>";

	public const string XML_MODULE_SHORT_DESC_FORMATTED_FIXEDSTRING = "<node id=\"ModuleShortDesc\">\n\t<attribute id=\"Folder\" type=\"LSString\" value=\"{0}\"/>\n\t<attribute id=\"MD5\" type=\"LSString\" value=\"{1}\"/>\n\t<attribute id=\"Name\" type=\"LSString\" value=\"{2}\"/>\n\t<attribute id=\"UUID\" type=\"FixedString\" value=\"{3}\"/>\n\t<attribute id=\"Version64\" type=\"int64\" value=\"{4}\"/>\n</node>";

	private static readonly string XML_MOD_SETTINGS_TEMPLATE_P6;
	private static readonly string XML_MOD_SETTINGS_TEMPLATE_P7;
	private static readonly string XML_MOD_SETTINGS_TEMPLATE_P8;

	private static readonly ModSettingsPattern PATTERN_PATCH_6;
	private static readonly ModSettingsPattern PATTERN_PATCH_7;
	private static readonly ModSettingsPattern PATTERN_PATCH_8;

	private static readonly Version VERSION_PATCH_6 = new(4,1,1,4763283);
	private static readonly Version VERSION_PATCH_6_HF_25 = new(4,1,1,5022896);
	private static readonly Version VERSION_PATCH_7 = new(4,1,1,5849914);
	private static readonly Version VERSION_PATCH_7_HF_28 = new(4,1,1,6072089);
	private static readonly Version VERSION_PATCH_8 = new(4,1,1,6758295);

	private static string GetModSettingsTemplateWithHeader(int major, int minor, int build, int revision)
	{
		return $@"<?xml version=""1.0"" encoding=""UTF-8""?><save><version major=""{major}"" minor=""{minor}"" revision=""{build}"" build=""{revision}""/><region id=""ModuleSettings""><node id=""root""><children><node id=""Mods""><children>{{0}}</children></node></children></node></region></save>";
	}

	static ModSettingsExportService()
	{
		XML_MOD_SETTINGS_TEMPLATE_P6 = GetModSettingsTemplateWithHeader(4, 0, 9, 331);
		XML_MOD_SETTINGS_TEMPLATE_P7 = GetModSettingsTemplateWithHeader(4, 7, 1, 3);
		XML_MOD_SETTINGS_TEMPLATE_P8 = GetModSettingsTemplateWithHeader(4, 8, 0, 200);

		PATTERN_PATCH_6 = new(XML_MOD_SETTINGS_TEMPLATE_P6, XML_MODULE_SHORT_DESC_FIXEDSTRING, XML_MODULE_SHORT_DESC_FORMATTED_FIXEDSTRING, true);
		PATTERN_PATCH_7 = new(XML_MOD_SETTINGS_TEMPLATE_P7, XML_MODULE_SHORT_DESC_GUID, XML_MODULE_SHORT_DESC_FORMATTED_GUID);
		PATTERN_PATCH_8 = new(XML_MOD_SETTINGS_TEMPLATE_P8, XML_MODULE_SHORT_DESC_GUID, XML_MODULE_SHORT_DESC_FORMATTED_GUID);
	}

	public ModSettingsPattern Pattern { get; private set; } = PATTERN_PATCH_8;

	private static ModSettingsPattern GetPattern(Version version)
	{
		//Not patch 7 yet, so UUID is probably a FixedString
		if (version <= VERSION_PATCH_6_HF_25)
		{
			return PATTERN_PATCH_6;
		}
		else if (version <= VERSION_PATCH_7_HF_28)
		{
			return PATTERN_PATCH_7;
		}
		return PATTERN_PATCH_8;
	}

	public void SetGameVersion(Version gameVersion)
	{
		Pattern = GetPattern(gameVersion);
		DivinityApp.Log($"Set game version to [{gameVersion}]");
	}

	public void SetGameVersion(string exePath)
	{
		var info = _fs.FileVersionInfo.GetVersionInfo(exePath);
		if (info != null && info.ProductVersion.IsValid())
		{
			//info.FileVersion is wrong for some reason, but the individual parts should be correct
			//Whereas info.ProductVersion is correct, but the individual parts may be incorrect
			SetGameVersion(new Version(info.ProductVersion));
		}
		else
		{
			DivinityApp.Log($"Failed to get file version from path '{exePath}'");
		}
	}

	public string GenerateModSettingsFile(IEnumerable<ModData> orderList)
	{
		/* The "Mods" node is used for the in-game menu it seems. The selected adventure mod is always at the top. */
		var modShortDescText = "";

		foreach (var mod in orderList)
		{
			if (mod.UUID.IsValid())
			{
				//Use Export to support lists with categories/things that don't need exporting
				var modText = mod.Export(ModExportType.XML, Pattern.ModuleShortDescPattern, Pattern.IsFixedString);
				if (modText.IsValid())
				{
					modShortDescText += modText + Environment.NewLine;
				}
			}
		}

		return string.Format(Pattern.ModSettingsTemplate, modShortDescText);
	}

	public async Task<bool> ExportModSettingsToFileAsync(string outputFolder, IEnumerable<ModData> order, CancellationToken token)
	{
		if (_fs.Directory.Exists(outputFolder))
		{
			var outputFilePath = _fs.Path.Join(outputFolder, "modsettings.lsx");
			var contents = GenerateModSettingsFile(order);
			try
			{
				var xml = new XmlDocument();
				xml.LoadXml(contents);
				using var sw = new StringWriter();
				using var xw = new XmlTextWriter(sw);
				xw.Formatting = Formatting.Indented;
				xw.Indentation = 4;
				xw.IndentChar = ' ';
				xml.WriteTo(xw);

				await FileUtils.WriteTextFileAsync(outputFilePath, sw.ToString(), token);

				return true;
			}
			catch (AccessViolationException ex)
			{
				DivinityApp.Log($"Failed to write file '{outputFilePath}': {ex}");
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error exporting file '{outputFilePath}': {ex}");
			}
		}
		return false;
	}

	public string ToFormattedModuleShortDesc(ModData mod)
	{
		var safeName = System.Security.SecurityElement.Escape(mod.Name);
		if(!Pattern.IsFixedString)
		{
			return string.Format(Pattern.ModuleShortDescPatternFormatted, mod.Folder, mod.MD5, safeName, mod.UUID, mod.Version.VersionInt, mod.PublishHandle);
		}
		else
		{
			//The FixedString version doesn't have PublishHandle
			return string.Format(Pattern.ModuleShortDescPatternFormatted, mod.Folder, mod.MD5, safeName, mod.UUID, mod.Version.VersionInt);
		}
	}
}
