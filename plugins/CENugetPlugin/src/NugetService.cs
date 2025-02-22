// Copyright (c) 2013-2019  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using CrowEditBase;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using static CrowEditBase.CrowEditBase;
using Crow;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol;
using NuGet.Versioning;
using NuGet.Packaging.Core;
using System.IO.Compression;
using NuGet.Common;
using System.Net.Http;

namespace NugetPlugin
{
	public class NugetService : Service {
		public override string ConfigurationWindowPath => "#CENugetPlugin.ui.winConfiguration.crow";

		public NugetService () : base () {
			initCommands();

			App.ViewCommands.Add (CMDViewNuget);

			if (App.TryGetWindow("#CENugetPlugin.ui.winNugetExplorer.crow", out Window win))
				win.DataSource = this;
		}
		#region commands
		public ActionCommand CMDViewNuget, CMDSearchPackage;
		void initCommands ()
		{
			CMDViewNuget = new ActionCommand("Nuget Explorer", () => App.LoadWindow ("#CENugetPlugin.ui.winNugetExplorer.crow", this));
			CMDSearchPackage = new ActionCommand("Search Package", () => SearchPackage(searchString));
		}
		#endregion
		
		public override void Start() {
			if (CurrentState == Status.Running)
				return;
			CurrentState = Status.Running;
		}
		public override void Stop()
		{
			if (CurrentState != Status.Running)
				return;

			CurrentState = Status.Stopped;
		}
		public override void Pause()
		{
			if (CurrentState != Status.Running)
				return;

			CurrentState = Status.Paused;
		}
		string searchString;
		IEnumerable<IPackageSearchMetadata> searchResults;
		public string SearchString {
			get => searchString;
			set {
				if (searchString == value)
					return;
				searchString = value;
				NotifyValueChanged(searchString);
			}
		}
		public IEnumerable<IPackageSearchMetadata> SearchResults {
			get => searchResults;
		}
		public async Task<Assembly> LoadFromNuget(string id, string version, string? nugetFeedUrl = null, CancellationToken cancellationToken = default)
		{
			ILogger _nugetLogger = NullLogger.Instance;
			var repository = Repository.Factory.GetCoreV3(nugetFeedUrl ?? "https://api.nuget.org/v3/index.json");
			var downloadResource = await repository.GetResourceAsync<DownloadResource>();
			if (!NuGetVersion.TryParse(version, out var nuGetVersion))
			{
				throw new Exception($"invalid version {version} for nuget package {id}");
			}
			using (var downloadResourceResult = await downloadResource.GetDownloadResourceResultAsync(
				new PackageIdentity(id, nuGetVersion),
				new PackageDownloadContext(new SourceCacheContext()),
				globalPackagesFolder: Path.GetTempPath(),
				logger: _nugetLogger,
				token: cancellationToken)){


				if (downloadResourceResult.Status != DownloadResourceResultStatus.Available)
				{
					throw new Exception($"Download of NuGet package failed. DownloadResult Status: {downloadResourceResult.Status}");
				}

				var reader = downloadResourceResult.PackageReader;

				var archive = new ZipArchive(downloadResourceResult.PackageStream);

				var lib = reader.GetLibItems().First()?.Items.First();

				var entry = archive.GetEntry(lib);

				using (MemoryStream decompressed = new MemoryStream()) {
					entry.Open().CopyTo(decompressed);
					var assemblyLoadContext = new System.Runtime.Loader.AssemblyLoadContext(null, isCollectible: true);
					decompressed.Position = 0;
					return assemblyLoadContext.LoadFromStream(decompressed);
				}
			}
		}
		async void ListPackages() {
			ILogger logger = NullLogger.Instance;
			CancellationToken cancellationToken = CancellationToken.None;

			SourceCacheContext cache = new SourceCacheContext();
			SourceRepository repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
			PackageMetadataResource resource = await repository.GetResourceAsync<PackageMetadataResource>();
			
			IEnumerable<IPackageSearchMetadata> packages = await resource.GetMetadataAsync(
				"Newtonsoft.Json",
				includePrerelease: true,
				includeUnlisted: false,
				cache,
				logger,
				cancellationToken);

			foreach (IPackageSearchMetadata package in packages)
			{
				Console.WriteLine($"Version: {package.Identity.Version}");
				Console.WriteLine($"Listed: {package.IsListed}");
				Console.WriteLine($"Tags: {package.Tags}");
				Console.WriteLine($"Description: {package.Description}");
			}		
		}
		async void SearchPackage(string searchString) {
			ILogger logger = NullLogger.Instance;
			CancellationToken cancellationToken = CancellationToken.None;

			SourceCacheContext cache = new SourceCacheContext();
			SourceRepository repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
			PackageSearchResource resource = await repository.GetResourceAsync<PackageSearchResource>();
			SearchFilter searchFilter = new SearchFilter(includePrerelease: true);

			searchResults = await resource.SearchAsync(
				searchString,
				searchFilter,
				skip: 0,
				take: 20,
				logger,
				cancellationToken);

			lock(App.UpdateMutex)
				NotifyValueChanged("SearchResults", searchResults);
		}
		

	}
}