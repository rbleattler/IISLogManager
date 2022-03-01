namespace IISLogManager.Core;

using System;
using System.Collections.Generic;

public class SiteObjectCollection : List<SiteObject>, IComparable<IISLogObjectCollection>, IComparable {
	public int CompareTo(IISLogObjectCollection other) {
		throw new NotImplementedException();
	}

	public int CompareTo(object obj) {
		throw new NotImplementedException();
	}

	public void ParseAllLogs() {
		ForEach(s => s.ParseAllLogs());
	}

	public void FilterAllLogFiles(DateTime startDate, DateTime endDate) {
		ForEach(s => s.FilterLogFiles(startDate, endDate));
	}
}