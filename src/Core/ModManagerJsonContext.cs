﻿using ModManager.Models;
using ModManager.Models.Cache;
using ModManager.Models.Extender;
using ModManager.Models.GitHub.Json;
using ModManager.Models.Mod;
using ModManager.Models.Settings;

namespace ModManager;

[JsonSerializable(typeof(DivinityLoadOrder))]
[JsonSerializable(typeof(DivinityModData))]
[JsonSerializable(typeof(ModuleShortDesc))]
[JsonSerializable(typeof(DivinityModScriptExtenderConfig))]
[JsonSerializable(typeof(DivinitySerializedModData))]
[JsonSerializable(typeof(GitHubModsCachedData))]
[JsonSerializable(typeof(GitHubModsCachedData))]
[JsonSerializable(typeof(GitHubReleaseJsonEntry))]
[JsonSerializable(typeof(GitHubRepositoryJsonData))]
[JsonSerializable(typeof(LarianVersion))]
[JsonSerializable(typeof(ModConfig))]
[JsonSerializable(typeof(ModManagerSettings))]
[JsonSerializable(typeof(ModManagerUpdateSettings))]
[JsonSerializable(typeof(ModioMod))]
[JsonSerializable(typeof(ScriptExtenderSettings))]
[JsonSerializable(typeof(ScriptExtenderUpdateConfig))]
[JsonSerializable(typeof(ScriptExtenderUpdateData))]
[JsonSerializable(typeof(ScriptExtenderUpdateResource))]
[JsonSerializable(typeof(ScriptExtenderUpdateVersion))]
[JsonSerializable(typeof(ModioCachedData))]
[JsonSerializable(typeof(UserModConfig))]
public partial class ModManagerJsonContext : JsonSerializerContext
{

}