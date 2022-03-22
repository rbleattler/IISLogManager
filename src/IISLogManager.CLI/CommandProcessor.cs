using System.ComponentModel;
using System.Globalization;
using FluentAssertions;
using IISLogManager.Core;
using Spectre.Console;

namespace IISLogManager.CLI;

public class CommandProcessor {
	public void ProcessCommand() { }

	public void ProcessTargetSites(ref SiteObjectCollection? targetSites, IISController iisController,
		Settings? settings, ref RunMode? runMode) {
		var sites = targetSites;
		var runmode = runMode;
		AnsiConsole.Status()
			.Start("Adding Sites...", ctx => {
				if ( !settings!.Interactive && settings.RunMode == RunMode.Target ) {
					if ( settings.SiteNames?.Length > 0 ) {
						AnsiConsole.MarkupLine($"Adding {settings.SiteNames?.Length} sites...");
						string[]? splitSiteNames =
							settings.SiteNames?.Split(',', StringSplitOptions.RemoveEmptyEntries);
						foreach (string siteName in splitSiteNames!) {
							var tsAdd = iisController.Sites.Where(
								sO => sO.SiteName == siteName.Trim()
							);
							sites?.AddRange(tsAdd);
						}
					}

					if ( settings.SiteUrls?.Length > 0 ) {
						AnsiConsole.MarkupLine($"Adding {settings.SiteUrls?.Count()} sites...");
						foreach (string siteUrl in
						         settings.SiteUrls?.Split(',', StringSplitOptions.RemoveEmptyEntries)!) {
							var tsAdd = iisController.Sites.Where(sO => sO.SiteUrl == siteUrl.Trim());
							sites?.AddRange(tsAdd);
						}
					}
				}

				if ( runmode == RunMode.All ) {
					sites?.AddRange(iisController.Sites);
				}
			});
		targetSites = sites;
		runMode = runmode;
	}

	public void ProcessSiteChoices(IISController iisController, ref List<string> siteChoices) {
		foreach (SiteObject site in iisController.Sites) {
			var siteString = string.Format("{0}\t({1})", site.SiteName, site.SiteUrl);
			siteChoices.Add(siteString);
		}
	}

	public int GetSites(List<string> siteChoices) {
		AnsiConsole.MarkupLine("[DarkOrange]Site Name[/]\t([Blue]Site Url[/])");
		siteChoices.ForEach((s) => { AnsiConsole.MarkupLine($"{s}"); });
		return 0;
	}

	public int GetSiteIds(ref IISController iiSController) {
		AnsiConsole.MarkupLine("[DarkOrange]Site Ids[/]");
		iiSController.Sites.ForEach(s => { AnsiConsole.MarkupLine($"{s.Id}"); });
		return 0;
	}

	//TODO: Ensure there *ARE* logs to process before beginning processing... 
	public void ProcessLogs(ref CommandConfiguration config) {
		//TODO: Add verbose output
		if ( config.TargetSites != null ) {
			// AnsiConsole.MarkupLine("[[DEBUG]] TargetSites is not null...");
			if ( config.Settings is {Filter: true} ) {
				int pathCount = 0;
#if DEBUG
				config.TargetSites.ForEach(p => pathCount += p.LogFilePaths.Count);
				AnsiConsole.MarkupLine($"[[DEBUG]] Filtering enabled...");
				AnsiConsole.MarkupLine($"[[DEBUG]] FromDate : {config.Settings.FromDate} ...");
				AnsiConsole.MarkupLine($"[[DEBUG]] ToDate : {config.Settings.ToDate} ...");
				AnsiConsole.MarkupLine($"[[DEBUG]] Filtering Files... ( Count : {pathCount})");
				pathCount = 0;
#endif
				config.TargetSites.FilterAllLogFiles(
					DateTime.Parse(config.Settings.FromDate ??
					               DateTime.Today.AddMonths(-1).ToString(CultureInfo.CurrentCulture)
					),
					DateTime.Parse(config.Settings.ToDate ??
					               DateTime.Today.ToString(CultureInfo.CurrentCulture)
					)
				);
				config.TargetSites.ForEach(p => pathCount += p.LogFilePaths.Count);
				AnsiConsole.MarkupLine(
					$"[[DEBUG]] Finished Filtering Files... (New Count : {pathCount})");
			}

			foreach (var site in config.TargetSites) {
				AnsiConsole.MarkupLine($"[[DEBUG]] Processing {site.SiteName}...");
				if ( config.OutputMode != OutputMode.LocalDb ) {
					site.ParseAllLogs(); //TODO: Update to implement as a dependent process of the output mode and size/number of logs for a given site
					if ( site.Logs.Count <= 0 ) { }

					AnsiConsole.MarkupLine($"[DarkOrange]{site.SiteName}[/] has no logs from the target date range...");
					continue;
				}

				switch (config.OutputMode) {
					case OutputMode.Local: {
						AnsiConsole.MarkupLine($"[[DEBUG]] Output mode Local...");
						var outFile = site.GetLogFileName(config.OutputDirectory);
						site.Logs.TrimExcess();
						site.Logs.WriteToFile(outFile);
						site.Logs.Clear();
						site.Logs.Dispose();
						AnsiConsole.MarkupLine($"[DarkOrange]Output File :[/] {outFile}");
						break;
					}
					case OutputMode.Remote: {
						AnsiConsole.MarkupLine($"[[DEBUG]] Output mode Remote...");
						if ( string.IsNullOrWhiteSpace(config.OutputUri) ) {
							throw new UriNotSpecifiedException();
						}

						if ( config.OutputUri != null ) {
							ConnectionManager connectionManager = new();
							connectionManager.SetConnection(config.OutputUri);
							if ( config.AuthMode == AuthMode.BearerToken ) {
								if ( config.AuthToken != null ) connectionManager.BearerToken = config.AuthToken;
								connectionManager.SetConnection(
									config.OutputUri,
									connectionManager.BearerToken ?? throw new NullAuthTokenException()
								);
							}

							var response =
								connectionManager.AddLogs(site.Logs, site.SiteUrl, site.SiteName, site.HostName);
							// var response = connectionManager.AddLogs(site.Logs, site.SiteUrl, site.SiteName, site.HostName,
							// 	true);
							AnsiConsole.MarkupLine($"[DarkOrange]Server Response :[/]{response}");
						}

						if ( config.OutputUri == null ) {
							throw new WarningException("OutputUri was not specified.");
						}

						break;
					}
					case OutputMode.LocalDb:
						var iisLogManagerContext = new IISLogManagerContext(
							config.DatabaseProvider ?? DatabaseProvider.Sqlite,
							config.ConnectionString,
							config.Settings?.TableName ?? "IISLogs"
						);
						iisLogManagerContext.Database.EnsureCreated();
						var fileCount = site.LogFilePaths.Count();
						var processed = 0;
						float fileSizeProcessed = 0;
						List<FileInfo> AllFiles = new();
						site.LogFilePaths.ForEach(p => AllFiles.Add(new FileInfo(p)));
						float totalFileSize = 0;
						AllFiles.ForEach(f => totalFileSize += f.Length / 1024f / 1024f);
						var progress = AnsiConsole.Progress()
							.AutoRefresh(true)
							.AutoClear(false)
							.HideCompleted(true)
							.Columns(new ProgressColumn[] {
								new SpinnerColumn(Spinner.Known.Aesthetic),
								new PercentageColumn(),
								new TaskDescriptionColumn(),
								new ProgressBarColumn(),
								new ElapsedTimeColumn()
							});
						// progress.RefreshRate = TimeSpan.FromSeconds(1);
						progress.Start(
							ctx => {
								var ParseLogsTask = ctx.AddTask("[green]Parsing Logs[/]")
									.MaxValue(fileCount);
								ParseLogsTask.StartTask();
								site.LogFilePaths.ForEach(p => {
									var fileInfo = (new FileInfo(p));
									var fileSizeMegs = (fileInfo.Length / 1024f / 1024f);
									ParseLogsTask.Description =
										$"[DarkOrange]{Path.GetFileName(p)}[/] | [DarkOrange]File Size[/] : {fileSizeMegs.ToString("F")} MB / {totalFileSize.ToString("F")} MB | [DarkOrange]File:[/] {processed}/{fileCount} ";
									site.ParseLogs(p);
									if ( site.Logs != null ) {
										iisLogManagerContext.AddRange(site.Logs);
										iisLogManagerContext.SaveChanges();
										site.Logs.Clear();
									}

									GC.Collect();
									processed++;
									fileSizeProcessed += fileSizeMegs;
									ParseLogsTask.Increment(1);
									ctx.Refresh();
								});

								ParseLogsTask.StopTask();
							}
						);

						break;
					case OutputMode.RemoteDb:
						break;
					case null:
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				site.Dispose();
			}
		}
	}

	public Task ProcessLogsAsync(ref CommandConfiguration config) {
		//TODO: Add verbose output
		if ( config.TargetSites != null ) {
			// AnsiConsole.MarkupLine("[[DEBUG]] TargetSites is not null...");
			if ( config.Settings is {Filter: true} ) {
				int pathCount = 0;
#if DEBUG
				config.TargetSites.ForEach(p => pathCount += p.LogFilePaths.Count);
				AnsiConsole.MarkupLine($"[[DEBUG]] Filtering enabled...");
				AnsiConsole.MarkupLine($"[[DEBUG]] FromDate : {config.Settings.FromDate} ...");
				AnsiConsole.MarkupLine($"[[DEBUG]] ToDate : {config.Settings.ToDate} ...");
				AnsiConsole.MarkupLine($"[[DEBUG]] Filtering Files... ( Count : {pathCount})");
				pathCount = 0;
#endif
				config.TargetSites.FilterAllLogFiles(
					DateTime.Parse(config.Settings.FromDate ??
					               DateTime.Today.AddMonths(-1).ToString(CultureInfo.CurrentCulture)
					),
					DateTime.Parse(config.Settings.ToDate ??
					               DateTime.Today.ToString(CultureInfo.CurrentCulture)
					)
				);
				config.TargetSites.ForEach(p => pathCount += p.LogFilePaths.Count);
				AnsiConsole.MarkupLine(
					$"[[DEBUG]] Finished Filtering Files... (New Count : {pathCount})");
			}

			foreach (var site in config.TargetSites) {
				AnsiConsole.MarkupLine($"[[DEBUG]] Processing {site.SiteName}...");
				if ( config.OutputMode != OutputMode.LocalDb ) {
					site.ParseAllLogs(); //TODO: Update to implement as a dependent process of the output mode and size/number of logs for a given site
					if ( site.Logs.Count <= 0 ) { }

					AnsiConsole.MarkupLine($"[DarkOrange]{site.SiteName}[/] has no logs from the target date range...");
					continue;
				}

				switch (config.OutputMode) {
					case OutputMode.Local: {
						AnsiConsole.MarkupLine($"[[DEBUG]] Output mode Local...");
						var outFile = site.GetLogFileName(config.OutputDirectory);
						site.Logs.TrimExcess();
						site.Logs.WriteToFile(outFile);
						site.Logs.Clear();
						site.Logs.Dispose();
						AnsiConsole.MarkupLine($"[DarkOrange]Output File :[/] {outFile}");
						break;
					}
					case OutputMode.Remote: {
						AnsiConsole.MarkupLine($"[[DEBUG]] Output mode Remote...");
						if ( string.IsNullOrWhiteSpace(config.OutputUri) ) {
							throw new UriNotSpecifiedException();
						}

						if ( config.OutputUri != null ) {
							ConnectionManager connectionManager = new();
							connectionManager.SetConnection(config.OutputUri);
							if ( config.AuthMode == AuthMode.BearerToken ) {
								if ( config.AuthToken != null ) connectionManager.BearerToken = config.AuthToken;
								connectionManager.SetConnection(
									config.OutputUri,
									connectionManager.BearerToken ?? throw new NullAuthTokenException()
								);
							}

							var response =
								connectionManager.AddLogs(site.Logs, site.SiteUrl, site.SiteName, site.HostName);
							// var response = connectionManager.AddLogs(site.Logs, site.SiteUrl, site.SiteName, site.HostName,
							// 	true);
							AnsiConsole.MarkupLine($"[DarkOrange]Server Response :[/]{response}");
						}

						if ( config.OutputUri == null ) {
							throw new WarningException("OutputUri was not specified.");
						}

						//TODO: Process Logs for remote output
						break;
					}
					case OutputMode.LocalDb:
						var refConfig = config;
						var iisLogManagerContext = new IISLogManagerContext(
							config.DatabaseProvider ?? DatabaseProvider.Sqlite,
							config.ConnectionString,
							config.Settings?.TableName ?? "IISLogs"
						);
						iisLogManagerContext.Database.EnsureCreated();
						var fileCount = site.LogFilePaths.Count();
						var processed = 0;
						float fileSizeProcessed = 0;
						List<FileInfo> AllFiles = new();
						site.LogFilePaths.ForEach(p => AllFiles.Add(new FileInfo(p)));
						float totalFileSize = 0;
						AllFiles.ForEach(f => totalFileSize += f.Length / 1024f / 1024f);
						var progress = AnsiConsole.Progress()
							.AutoRefresh(true)
							.AutoClear(false)
							.HideCompleted(true)
							.Columns(new ProgressColumn[] {
								new SpinnerColumn(Spinner.Known.Aesthetic),
								new PercentageColumn(),
								new TaskDescriptionColumn(),
								new ProgressBarColumn(),
								new ElapsedTimeColumn()
							});
						// progress.RefreshRate = TimeSpan.FromSeconds(1);
						ParallelOptions parallelOptions = new() {
							MaxDegreeOfParallelism = 3
						};
						CancellationToken cancellationToken = new CancellationToken();
						progress.StartAsync(
							async ctx => {
								var parseLogsTask = ctx.AddTask("[green]Parsing Logs[/]")
									.MaxValue(fileCount);
								parseLogsTask.StartTask();
								await Parallel.ForEachAsync(source: site.LogFilePaths, parallelOptions,
									async (p, cancellationToken) => {
										ValueTask task = new ValueTask();
										var innerContext = new IISLogManagerContext(
											refConfig?.DatabaseProvider ?? DatabaseProvider.Sqlite,
											refConfig?.ConnectionString,
											refConfig.Settings?.TableName ?? "IISLogs"
										);
										await innerContext.Database.EnsureCreatedAsync();
										var fileInfo = (new FileInfo(p));
										var fileSizeMegs = (fileInfo.Length / 1024f / 1024f);
										parseLogsTask.Description =
											$"[DarkOrange]File Size[/] : {fileSizeMegs.ToString("F")} MB / {totalFileSize.ToString("F")} MB | [DarkOrange]File:[/] {processed}/{fileCount} ";
										ctx.Refresh();
										await site.ParseLogsAsync(p, new CancellationToken());
										if ( site.ConcurrentLogs != null ) {
											ParallelOptions guidParallelOptions = new() {
												MaxDegreeOfParallelism = 12,
												CancellationToken = new CancellationToken()
											};
											var guidCancellationToken = new CancellationToken();
											var newTask = new ValueTask();
											await Parallel.ForEachAsync(site.ConcurrentLogs, guidParallelOptions,
												(l, guidCancellationToken) => {
													if ( site.ConcurrentLogs.Any(o => o.UniqueId == l.UniqueId) )
														l.UniqueId = Guid.NewGuid().ToString();
													return ValueTask.CompletedTask;
												});
											try {
												await innerContext.AddRangeAsync(site.ConcurrentLogs);
											} catch (Exception ex) {
												await new Task(innerContext.Invoking(i =>
													i.UpdateRange(site.ConcurrentLogs)));
											}

											await innerContext.SaveChangesAsync();
											site.ConcurrentLogs.Clear();
										}

										// addLogsTask.StopTask();
										GC.Collect();
										processed++;
										fileSizeProcessed += fileSizeMegs;
										parseLogsTask.Increment(1);
										ctx.Refresh();
										await innerContext.DisposeAsync();
									});
								parseLogsTask.StopTask();
							}
						).Wait();

						break;
					case OutputMode.RemoteDb:
						break;
					case null:
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				site.Dispose();
			}
		}

		return Task.CompletedTask;
	}

	private void StartLogParsing(OutputMode outputMode) {
		//TODO: Update to implement as a dependent process of the output mode and size/number of logs for a given site
		switch (outputMode) {
			case OutputMode.Local:
				break;
			case OutputMode.LocalDb:
				break;
			case OutputMode.Remote:
				break;
			case OutputMode.RemoteDb:
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(outputMode), outputMode, null);
		}
	}

	public static CommandProcessor Instance = new();
}