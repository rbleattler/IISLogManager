namespace IISLogManager.CLI;

public class FilterConfiguration {
	public FilterState FilterState { get; set; }
	public DateTime FromDate { get; set; }
	public DateTime ToDate { get; set; }

	public FilterConfiguration() { }

	public FilterConfiguration(FilterState filterState, DateTime fromDate, DateTime toDate) {
		FilterState = filterState;
		FromDate = fromDate;
		ToDate = toDate;
	}

	public void SetToDate(string dateString) {
		ToDate = DateTime.Parse(dateString);
	}

	public void SetFromDate(string dateString) {
		FromDate = DateTime.Parse(dateString);
	}
}