using System.Text.RegularExpressions;
using System.Web;

using HtmlAgilityPack;

using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;

using ScrapySharp.Extensions;
using ScrapySharp.Network;

internal static class Program
{
	private static IDateTime _datestamp = new CalDateTime(DateTime.Now);
	private static string _tzId = "Pacific Standard Time";//"America/Los_Angeles";

	static void Main()
	{
		SchoolYearEvent[][] schoolYears = GetSchoolYearEvents();
		GenerateCalendar(schoolYears);
	}

	private static SchoolYearEvent[][] GetSchoolYearEvents()
	{
		string _calendarUrl = "https://salkeiz.k12.or.us/about-us/yearly-calendars/";
		ScrapingBrowser _browser = new ScrapingBrowser();
		WebPage webpage = _browser.NavigateToPage(new Uri(_calendarUrl));

		var html = webpage.Html;

		HtmlNode keyDatesNode = html.SelectSingleNode("//*[text()='KEY DATES']");

		if (keyDatesNode == null)
		{
			throw new Exception("Unable to find KEY DATES");
		}

		HtmlNode[] schoolYearNodes = FindSchoolYears(keyDatesNode);
		HtmlNode[] tableNodes = FindTables(keyDatesNode);
		HtmlNode[][] rows = tableNodes.Select(x => x.CssSelect("tr").ToArray()).ToArray();

		return schoolYearNodes.Zip(rows).Select(ab => GetSchoolYearEvents(ab.First, ab.Second)).ToArray();
	}

	private static void GenerateCalendar(SchoolYearEvent[][] schoolYears)
	{
		foreach (var schoolYear in schoolYears)
		{
			var calendar = new Calendar();
			calendar.Method = "PUBLISH";
			//calendar.Group = "School Calendar";
			//calendar.Name = "School Calendar";
			//calendar.ProductId = "-//School Calendar//NONSGML School Calendar//EN";
			//calendar.TimeZones.Add(new VTimeZone("America/Los_Angeles"));
			calendar.AddProperty("X-WR-CALNAME", "School Calendar");

			foreach (var schoolYearEvent in schoolYear)
			{
				var e = new CalendarEvent
				{
					Start = new CalDateTime(schoolYearEvent.DateHead),
					End = new CalDateTime(schoolYearEvent.DateTail),
					Summary = schoolYearEvent.Summary,
					IsAllDay = true,
					Categories = new List<string> { "School Event" },
					Class = "PUBLIC",
					Created = _datestamp,
					LastModified = _datestamp,
					Priority = 5,
					Transparency = "TRANSPARENT",
				};

				calendar.Events.Add(e);
			}

			var serializer = new CalendarSerializer();
			var serializedCalendar = serializer.SerializeToString(calendar);

			var yearHead = schoolYear.First().DateHead.Year;
			var yearTail = schoolYear.Last().DateTail.Year;
			File.WriteAllText($"Salem-Keizer_School-Year_Calendar_{yearHead}-{yearTail}.ics", serializedCalendar);
		}
	}

	private static SchoolYearEvent[] GetSchoolYearEvents(HtmlNode schoolYear, HtmlNode[] rows)
	{
		return rows.Select(x => MakeSchoolYearEvent(schoolYear, x)).ToArray();
	}

	private static SchoolYearEvent MakeSchoolYearEvent(HtmlNode schoolYear, HtmlNode schoolEventRow)
	{
		var year = schoolYear.InnerText.Trim();
		var columns = schoolEventRow.CssSelect("td").ToArray();
		var date = columns[0].InnerText.Trim();
		var schoolEvent = columns[1].InnerText.Trim();

		return new SchoolYearEvent(year, date, schoolEvent);
	}

	private static HtmlNode[] FindTables(HtmlNode node)
	{
		HtmlNode[] nodes = Array.Empty<HtmlNode>();

		while (nodes.Length == 0 && node.ParentNode != null)
		{
			node = node.ParentNode;
			nodes = node.CssSelect("table").ToArray();
		}

		return nodes;
	}

	private static HtmlNode[] FindSchoolYears(HtmlNode node)
	{
		HtmlNode[] nodes = Array.Empty<HtmlNode>();

		while (nodes.Length == 0 && node.ParentNode != null)
		{
			node = node.ParentNode;
			nodes = node.SelectNodes("//*[text()[contains(.,'School Year')]]")
				.Where(x => Regex.IsMatch(x.InnerText, @"^[0-9\-]+ School Year"))
				.ToArray();
		}

		return nodes;
	}
}

internal class SchoolYearEvent
{
	public DateTime DateHead { get; }
	public DateTime DateTail { get; }
	public string Summary { get; }

	public SchoolYearEvent(string schoolYear, string date, string schoolEvent)
	{
		var sy = Regex.Match(schoolYear, @"([0-9]{4})\-([0-9]{2})");

		if (!sy.Success || sy.Groups.Count != 3)
		{
			throw new Exception("Unable to find school year");
		}

		var yearHead = sy.Groups[1].Value;
		var yearTail = "20" + sy.Groups[2].Value;

		var monthDays = Regex.Split(date, @" *\- *");

		var monthDayHead = monthDays[0];
		var monthDayTail = monthDays[0];

		if (monthDays.Length == 2)
		{
			monthDayTail = monthDays[1];

			if (monthDayTail.Length < 4)
			{
				monthDayTail = monthDayHead.Substring(0, 4) + monthDayTail;
			}
		}

		DateHead = DateTime.ParseExact($"{monthDayHead} {yearHead}", "MMM d yyyy", null);
		DateTail = DateTime.ParseExact($"{monthDayTail} {yearHead}", "MMM d yyyy", null);

		if (DateHead.Month < 8)
		{
			DateHead = DateHead.AddYears(1);
		}

		if (DateTail.Month < 8)
		{
			DateTail = DateTail.AddYears(1);
		}

		if (DateTail != DateHead)
		{
			DateTail = DateTail.AddDays(1);
		}

		Summary = HttpUtility.HtmlDecode(schoolEvent);
	}

	public override string ToString()
	{
		return "" +
			DateHead.ToString("MM/dd/yy") +
			"-" +
			DateTail.ToString("MM/dd/yy") +
			" " +
			Summary +
			"";
	}
}
