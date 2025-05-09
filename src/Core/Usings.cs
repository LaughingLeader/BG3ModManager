﻿global using ReactiveUI;
global using ReactiveUI.Fody.Helpers;

global using System;
global using System.Collections;
global using System.Collections.Generic;
global using System.Collections.ObjectModel;
global using System.IO;
global using System.Linq;
global using System.Net.Http;
global using System.Reactive;
global using System.Reactive.Concurrency;
global using System.Reactive.Disposables;
global using System.Reactive.Linq;
global using System.Threading;
global using System.Threading.Tasks;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Runtime.Serialization;

global using Splat;

global using RxCommandUnit = ReactiveUI.ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit>;
global using ModioMod = Modio.Models.Mod;
global using ModioDownload = Modio.Models.Download;