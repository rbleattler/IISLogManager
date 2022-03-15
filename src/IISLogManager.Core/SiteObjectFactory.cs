using System.Text.RegularExpressions;
using Microsoft.Web.Administration;

namespace IISLogManager.Core;

public class SiteObjectFactory {
	public SiteObject BuildSite(Site site) {
		SiteObject siteObject = new();
		siteObject.Id = site.Id;
		siteObject.SiteName = site.Name;
		siteObject.SiteUrl = GetSiteUrl(site.Bindings);
		siteObject.HostName = Environment.GetEnvironmentVariable("ComputerName");
		siteObject.LogFormat = site.LogFile.LogFormat;
		// CLEANUP:
		// Removing unused property
		// siteObject.LogFileData = site.LogFile;
		siteObject.LogRoot = Environment.ExpandEnvironmentVariables(site.LogFile.Directory);
		siteObject.IntrinsicLogRoot = $"{siteObject.LogRoot}\\W3SVC{site.Id}";
		//TODO: Error handler for if there is *no log directory here*... maybe we should check to see if IIS is even installed?
		siteObject.LogFilePaths = Directory.Exists(siteObject.IntrinsicLogRoot)
			? Directory.GetFiles(siteObject.IntrinsicLogRoot).ToList()
			: new List<string>();
		return siteObject;
	}

	private string GetSiteUrl(BindingCollection siteBindings) {
		// matches strings that start with www
		string pattern = @"(w{3})(\..*){2,}";
		Regex regex = new(pattern, RegexOptions.IgnoreCase);

		var query = from item in siteBindings
			where !item.Host.Contains("www")
			select item.Host;
		string outItem = query.First();
		// string outItem = rawOutItem.Substring(rawOutItem.LastIndexOf(":") + 1);
		return outItem;
	}

	public SiteObjectFactory() { }
}