using Microsoft.Web.Administration;

namespace IISLogManager.Core {
	// This class facilitates the interactions between the local IIS instance and IISLogManager
	public class IISController {
		//* Apparently, the new ServerManager call automatically finds the applicationHost.config. This was not my initial experience. Leaving this here for now as a reference.
		// public ServerManager ServerManager = new ServerManager(true, @"%WinDir%\System32\inetsrv\config\applicationHost.config");
		public readonly ServerManager ServerManager = new();
		public SiteObjectCollection Sites = new();
		private readonly SiteObjectFactory _siteObjectFactory = new();

		/// <summary> 
		/// Convert the Microsoft.Web.Administration Sites found in ServerManager.Sites to SiteObjects
		/// </summary>
		public void GetExtendedSiteList(bool ignoreDefaultSite = true) {
			SiteCollection siteList = ServerManager.Sites;
			foreach (Site site in siteList) {
				if ( site.Name == "Default Web Site" && ignoreDefaultSite ) {
					continue;
				}

				Sites.Add(_siteObjectFactory.BuildSite(site));
			}
		}


		public IISController() { }
	}
}