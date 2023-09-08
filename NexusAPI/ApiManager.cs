using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NexusAPI.Responses;

namespace NexusAPI
{
    using System.Threading;

    public static class ApiManager
    {
        private static INexusAPI _client;
        private static string _cache;
        private static Throttle _throttle;
        
        public static void Init(string apiKey, string userAgent, string cacheDir)
        {
            _throttle = new Throttle(30, TimeSpan.FromSeconds(1));

            var factory = new RestEaseClientFactory<INexusAPI>();
            var val1 = factory.InitializeAsync("https://api.nexusmods.com", new RestEaseClientFactoryOptions {EnableHttpLogging = true}).Result;
            _client = factory.Api;

            _cache = cacheDir;
            _client.ApiKey = apiKey;
            _client.UserAgent = userAgent;
            var val2 = _client.ValidateAsync().Result;
        }

        public static void GetMod(string domain, int modId, Action<Mod> doAfter)
        {
            var file = Path.Combine(_cache, $"mod_{domain}_{modId}.json");
            if (File.Exists(file))
            {
                var dt = File.GetCreationTimeUtc(file);
                var diff = DateTime.UtcNow - dt;
                var useIt = false;
                if (diff.Hours < 24)
                {
                    useIt = true;
                }
                else
                {
                    if (RateLimits.IsBlocked())
                    {
                        var tillReset = RateLimits.GetTimeUntilRenewal();
                        if (tillReset.TotalSeconds > 10)
                        {
                            useIt = true;
                        }
                    }
                }

                if (useIt)
                {
                    doAfter?.Invoke(JsonConvert.DeserializeObject<Mod>(File.ReadAllText(file)));
                    return;
                }
                File.Delete(file);
            }

            DoGetMod(domain, modId, file).ContinueWith(x => doAfter?.Invoke(x.Result));
        }
        
        public static Task<Mod> GetMod(string domain, int modId)
        {
            var file = Path.Combine(_cache, $"mod_{domain}_{modId}.json");
            if (!File.Exists(file)) return DoGetMod(domain, modId, file);
            var dt = File.GetCreationTimeUtc(file);
            var diff = DateTime.UtcNow - dt;
            var useIt = false;
            if (diff.Hours < 24)
            {
                useIt = true;
            }
            else
            {
                if (RateLimits.IsBlocked())
                {
                    var tillReset = RateLimits.GetTimeUntilRenewal();
                    if (tillReset.TotalSeconds > 10)
                    {
                        useIt = true;
                    }
                }
            }

            if (useIt)
            {
                return Task.FromResult(JsonConvert.DeserializeObject<Mod>(File.ReadAllText(file)));
            }
            File.Delete(file);

            return DoGetMod(domain, modId, file);
        }

        public static Mod GetModSync(string domain, int modId)
        {
            var file = Path.Combine(_cache, $"mod_{domain}_{modId}.json");
            if (File.Exists(file))
            {
                var dt = File.GetCreationTimeUtc(file);
                var diff = DateTime.UtcNow - dt;
                var useIt = false;
                if (diff.Hours < 24)
                {
                    useIt = true;
                }
                else
                {
                    if (RateLimits.IsBlocked())
                    {
                        var tillReset = RateLimits.GetTimeUntilRenewal();
                        if (tillReset.TotalSeconds > 10)
                        {
                            useIt = true;
                        }
                    }
                }

                if (useIt)
                {
                    return JsonConvert.DeserializeObject<Mod>(File.ReadAllText(file));
                }

                File.Delete(file);
            }

            if (RateLimits.IsBlocked())
            {
                var renewDelay = RateLimits.GetTimeUntilRenewal();
                if (renewDelay.TotalMilliseconds > 0)
                    Thread.Sleep(renewDelay);
            }

            Mod mod;
            try
            {
                mod = _throttle.Queue(() => _client.GetMod(domain, modId).Result).Result;
            }
            catch
            {
                return null;
            }

            File.WriteAllText(file, JsonConvert.SerializeObject(mod, Formatting.None));
            return mod;
        }

        private static async Task<Mod> DoGetMod(string domain, int modId, string file)
        {
            if (RateLimits.IsBlocked())
            {
                var renewDelay = RateLimits.GetTimeUntilRenewal();
                if (renewDelay.TotalMilliseconds > 0)
                {
                    await Task.Delay(renewDelay);
                    return await DoGetMod(domain, modId, file);
                }
            }

            var mod = await (await _throttle.Queue(async () => await _client.GetMod(domain, modId)));

            File.WriteAllText(file, JsonConvert.SerializeObject(mod, Formatting.None));
            return mod;
        }

        private static Task<ModFile[]> GetModFiles(string domain, int modId)
        {
            var file = Path.Combine(_cache, $"modfiles_{domain}_{modId}.json");
            if (!File.Exists(file)) return DoGetModFiles(domain, modId, file);
            var dt = File.GetCreationTimeUtc(file);
            var diff = DateTime.UtcNow - dt;
            var useIt = false;
            if (diff.Hours < 12)
            {
                useIt = true;
            }
            else
            {
                if (RateLimits.IsBlocked())
                {
                    var tillReset = RateLimits.GetTimeUntilRenewal();
                    if (tillReset.TotalSeconds > 10)
                    {
                        useIt = true;
                    }
                }
            }

            if (useIt)
            {
                return Task.FromResult(JsonConvert.DeserializeObject<ModFile[]>(File.ReadAllText(file)));
            }

            File.Delete(file);

            return DoGetModFiles(domain, modId, file);
        }

        private static ModFile[] GetModFilesSync(string domain, int modId)
        {
            var file = Path.Combine(_cache, $"modfiles_{domain}_{modId}.json");
            if (File.Exists(file))
            {
                var dt = File.GetCreationTimeUtc(file);
                var diff = DateTime.UtcNow - dt;
                var useIt = false;
                if (diff.Hours < 12)
                {
                    useIt = true;
                }
                else
                {
                    if (RateLimits.IsBlocked())
                    {
                        var tillReset = RateLimits.GetTimeUntilRenewal();
                        if (tillReset.TotalSeconds > 10)
                        {
                            useIt = true;
                        }
                    }
                }

                if (useIt)
                {
                    return JsonConvert.DeserializeObject<ModFile[]>(File.ReadAllText(file));
                }
                File.Delete(file);
            }

            if (RateLimits.IsBlocked())
            {
                var renewDelay = RateLimits.GetTimeUntilRenewal();
                if (renewDelay.TotalMilliseconds > 0)
                    Thread.Sleep(renewDelay);
            }

            ModFile[] modfiles;
            try
            {
                modfiles = _throttle.Queue(() => _client.GetModFiles(domain, modId).Result.Files)
                    .Result;
            }
            catch
            {
                return new ModFile[0];
            }

            File.WriteAllText(file, JsonConvert.SerializeObject(modfiles, Formatting.None));
            return modfiles;
        }

        private static async Task<ModFile[]> DoGetModFiles(string domain, int modId, string file)
        {
            if (RateLimits.IsBlocked())
            {
                var renewDelay = RateLimits.GetTimeUntilRenewal();
                if (renewDelay.TotalMilliseconds > 0)
                {
                    await Task.Delay(renewDelay);
                    return await GetModFiles(domain, modId);
                }
            }

            var modFiles = await (await _throttle.Queue(async () => await _client.GetModFiles(domain, modId)));
            File.WriteAllText(file, JsonConvert.SerializeObject(modFiles.Files, Formatting.None));
            return modFiles.Files;
        }

        private static void GetModFiles(string domain, int modId, Action<ModFile[]> doAfter)
        {
            var file = Path.Combine(_cache, $"modfiles_{domain}_{modId}.json");
            if (File.Exists(file))
            {
                var dt = File.GetCreationTimeUtc(file);
                var diff = DateTime.UtcNow - dt;
                var useIt = false;
                if (diff.Hours < 12)
                {
                    useIt = true;
                }
                else
                {
                    if (RateLimits.IsBlocked())
                    {
                        var tillReset = RateLimits.GetTimeUntilRenewal();
                        if (tillReset.TotalSeconds > 10)
                        {
                            useIt = true;
                        }
                    }
                }

                if (useIt)
                {
                    doAfter?.Invoke(JsonConvert.DeserializeObject<ModFile[]>(File.ReadAllText(file)));
                    return;
                }

                File.Delete(file);
            }

            DoGetModFiles(domain, modId, file).ContinueWith(x => doAfter?.Invoke(x.Result));
        }
        
        public static Task<ModFile[]> GetModFiles(string domain, int modId, params FileCategory[] categories)
        {
            var files = GetModFiles(domain, modId).Result;
            return Task.FromResult(files.Where(x => categories.Contains(x.Category)).ToArray());
        }

        public static ModFile[] GetModFilesSync(string domain, int modId, params FileCategory[] categories)
        {
            var files = GetModFilesSync(domain, modId);
            return files.Where(x => categories.Contains(x.Category)).ToArray();
        }

        public static void GetModFiles(string domain, int modId, IEnumerable<FileCategory> categories, Action<ModFile[]> doAfter)
        {
            GetModFiles(domain, modId,
                files => doAfter?.Invoke(files.Where(x => categories.Contains(x.Category)).ToArray()));
        }

        /*public static Task<ModFile> GetModFile(string domain, int modId, int fileId)
        {
            var files = GetModFiles(domain, modId).Result;
            return Task.FromResult(files.FirstOrDefault(x => x.FileID == fileId));
        }*/

        public static void GetModFile(string domain, int modId, int fileId, Action<ModFile> doAfter)
        {
            GetModFiles(domain, modId,
                files => doAfter?.Invoke(files.FirstOrDefault(x => x.FileID == fileId)));
        }

        /*public static Task<ModFileDownloadLink[]> GetDownloadLinks(string domain, int modId, int fileId)
        {
            var file = Path.Combine(_cache, $"links_{domain}_{modId}_{fileId}.json");
            if (File.Exists(file))
            {
                var dt = File.GetCreationTimeUtc(file);
                var diff = DateTime.UtcNow - dt;
                var useIt = false;
                if (diff.Hours < 6)
                {
                    useIt = true;
                }
                else
                {
                    if (RateLimits.IsBlocked())
                    {
                        var tillReset = RateLimits.GetTimeUntilRenewal();
                        if (tillReset.TotalSeconds > 10)
                        {
                            useIt = true;
                        }
                    }
                }

                if (useIt)
                {
                    return Task.FromResult(JsonConvert.DeserializeObject<ModFileDownloadLink[]>(File.ReadAllText(file)));
                }
                File.Delete(file);
            }

            return DoGetDownloadLinks(domain, modId, fileId, file);
        }*/

        // Get a mod by MD5 hash
        public static Task<ModHashResult[]> GetModByHash(string domain, string hash)
        {
            var file = Path.Combine(_cache, $"mod_{domain}_{hash}.json");
            if (!File.Exists(file)) return DoGetModByHash(domain, hash, file);
            var dt = File.GetCreationTimeUtc(file);
            var diff = DateTime.UtcNow - dt;
            var useIt = false;
            if (diff.Hours < 24)
            {
                useIt = true;
            }
            else
            {
                if (RateLimits.IsBlocked())
                {
                    var tillReset = RateLimits.GetTimeUntilRenewal();
                    if (tillReset.TotalSeconds > 10)
                    {
                        useIt = true;
                    }
                }
            }

            if (useIt)
            {
                return Task.FromResult(JsonConvert.DeserializeObject<ModHashResult[]>(File.ReadAllText(file)));
            }

            File.Delete(file);

            return DoGetModByHash(domain, hash, file);
        }

        private static async Task<ModHashResult[]> DoGetModByHash(string domain, string hash, string file)
        {
            if (RateLimits.IsBlocked())
            {
                var renewDelay = RateLimits.GetTimeUntilRenewal();
                if (renewDelay.TotalMilliseconds > 0)
                {
                    await Task.Delay(renewDelay);
                    return await DoGetModByHash(domain, hash, file);
                }
            }

            var mod = await (await _throttle.Queue(async () => await _client.Md5Search(domain, hash)));

            File.WriteAllText(file, JsonConvert.SerializeObject(mod, Formatting.None));
            return mod;
        }

        
        private static async Task<ModFileDownloadLink[]> DoGetDownloadLinks(string domain, int modId, int fileId, string file)
        {
            if (RateLimits.IsBlocked())
            {
                var renewDelay = RateLimits.GetTimeUntilRenewal();
                if (renewDelay.TotalMilliseconds > 0)
                {
                    await Task.Delay(renewDelay);
                    return await DoGetDownloadLinks(domain, modId, fileId, file);
                }
            }

            var links = await (await _throttle.Queue(async () => await _client.GetDownloadLinks(domain, modId, fileId)));

            File.WriteAllText(file, JsonConvert.SerializeObject(links, Formatting.None));
            return links;
        }
        
        public static void GetDownloadLinks(string domain, int modId, int fileId, Action<ModFileDownloadLink[]> doAfter)
        {
            var file = Path.Combine(_cache, $"links_{domain}_{modId}_{fileId}.json");
            if (File.Exists(file))
            {
                var dt = File.GetCreationTimeUtc(file);
                var diff = DateTime.UtcNow - dt;
                var useIt = false;
                if (diff.Hours < 6)
                {
                    useIt = true;
                }
                else
                {
                    if (RateLimits.IsBlocked())
                    {
                        var tillReset = RateLimits.GetTimeUntilRenewal();
                        if (tillReset.TotalSeconds > 10)
                        {
                            useIt = true;
                        }
                    }
                }

                if (useIt)
                {
                    doAfter?.Invoke(JsonConvert.DeserializeObject<ModFileDownloadLink[]>(File.ReadAllText(file)));
                    return;
                }
                File.Delete(file);
            }

            DoGetDownloadLinks(domain, modId, fileId, file).ContinueWith(x => doAfter?.Invoke(x.Result));
        }
    }
}