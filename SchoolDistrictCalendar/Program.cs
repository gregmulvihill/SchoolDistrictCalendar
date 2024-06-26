using Ical.Net;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;

using Microsoft.Playwright;

using System.Text.Json;

namespace SchoolDistrictCalendar;

class Program
{
	private static IDateTime _datestamp = CalDateTime.Today;
	private static List<EventDetails> _eventDetailsCollection = new List<EventDetails>();
	private static List<EventDetails> _keyEventDetailsCollection = new List<EventDetails>();

	static async Task Main(string[] args)
	{
		string cacheFilePath1 = @"EventDetails.json";
		string cacheFilePath2 = @"KeyEventDetails.json";

		// create cache if missing
		if (!File.Exists(cacheFilePath1) || !File.Exists(cacheFilePath2))
		{
			string url = "https://salkeiz.k12.or.us/about/calendar";
			await Browse(url, false, async (page, response) => await OnBrowse(page));

			string json1 = JsonSerializer.Serialize(_eventDetailsCollection, new JsonSerializerOptions { WriteIndented = true });
			File.WriteAllText(cacheFilePath1, json1);

			string json2 = JsonSerializer.Serialize(_keyEventDetailsCollection, new JsonSerializerOptions { WriteIndented = true });
			File.WriteAllText(cacheFilePath2, json2);
		}

		// load cache
		{
			var json1 = File.ReadAllText(cacheFilePath1);
			_eventDetailsCollection = JsonSerializer.Deserialize<List<EventDetails>>(json1, new JsonSerializerOptions { WriteIndented = true });

			var json2 = File.ReadAllText(cacheFilePath2);
			_keyEventDetailsCollection = JsonSerializer.Deserialize<List<EventDetails>>(json2, new JsonSerializerOptions { WriteIndented = true });
		}

		// process cache
		{
			List<EventDetails> toAdd = [];
			ILookup<DateOnly, EventDetails> eventDetailsCollection = _eventDetailsCollection.ToLookup(x => x.DayOfEvent);

			foreach (var keyEventDetails in _keyEventDetailsCollection)
			{
				if (eventDetailsCollection.Contains(keyEventDetails.DayOfEvent))
				{
					var eventDetails = eventDetailsCollection[keyEventDetails.DayOfEvent].First();

					if (eventDetails != null)
					{
						eventDetails.SetKeyEvent(true);
					}
					else
					{
						throw new NotImplementedException();
					}
				}
				else
				{
					toAdd.Add(keyEventDetails);
				}
			}

			var allEventDetails = eventDetailsCollection.Select(x => x.First()).Concat(toAdd).OrderBy(x => x.DayOfEvent).ThenBy(x => x.StartTime).ToArray();

			GenerateCalendar(allEventDetails);
		}
	}

	private static async Task Browse(string url, bool update, Func<IPage, IResponse?, Task> action)
	{
		IPlaywright playwright = await Playwright.CreateAsync();
		IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
		{
			Headless = false,
		});
		//var bco = new BrowserNewContextOptions()
		//{
		//	RecordHarContent = HarContentPolicy.Embed,
		//	RecordHarMode = HarMode.Full,
		//	RecordHarOmitContent = false,
		//	RecordHarPath = harPath,
		//};

		IBrowserContext context = await browser.NewContextAsync();
		IPage page = await context.NewPageAsync();
		//await page.RouteFromHARAsync(harPath, new()
		//{
		//	UpdateContent = RouteFromHarUpdateContentPolicy.Embed,
		//	UpdateMode = HarMode.Full,
		//	Update = update,
		//});
		IResponse? response = await page.GotoAsync(url);

		await Task.Delay(1000);

		await action.Invoke(page, response);

		await Task.Delay(1000);

		await page.CloseAsync();
		await context.CloseAsync();
		await context.DisposeAsync();
		await browser.DisposeAsync();
		playwright.Dispose();
	}

	//private static async Task HandleSavePageAsync(IPage page)
	//{
	//	await page.GetByLabel("Previous Month").ClickAsync();
	//	await Task.Delay(1000);

	//	for (var i = 0; i < 12; i++)
	//	{
	//		await page.GetByLabel("Next Month").ClickAsync();
	//		await Task.Delay(1000);
	//	}
	//}

	private static async Task OnBrowse(IPage page)
	{
		await CaptureKeyEventsAsync(page);

		await page.GetByLabel("Previous Month").ClickAsync();
		await Task.Delay(1000);

		for (int i = 0; i < 12; i++)
		{
			await page.GetByLabel("Next Month").ClickAsync();
			await Task.Delay(1000);

			await CaptureEventsAsync(page);
		}
	}

	private static async Task CaptureKeyEventsAsync(IPage page)
	{
		_keyEventDetailsCollection = (await page.QuerySelectorAllAsync("div.fsDayContainer > article"))
			.Select(async x =>
			{
				string title = await (await x.QuerySelectorAsync("div.fsTitle > a.fsCalendarEventLink")).InnerTextAsync();
				IElementHandle? elementHandle = await x.QuerySelectorAsync("time");
				string? datetime = await elementHandle.GetAttributeAsync("datetime");
				var datestamp = DateTime.Parse(datetime);

				var dayOfEvent = new DateOnly(datestamp.Year, datestamp.Month, datestamp.Day);
				var startTime = new TimeOnly(0, 0, 0);
				var endTime = new TimeOnly(23, 59, 59);
				var eventDetails = new EventDetails(dayOfEvent, startTime, endTime, title, true);

				return eventDetails;
			})
			.Select(x => x.Result)
			.ToList();
	}

	private static async Task CaptureEventsAsync(IPage page)
	{
		IElementHandle[] fsCalendarDayboxs = (await page.QuerySelectorAllAsync("div.fsCalendarDaybox")).ToArray();
		string?[] fsCalendarDayboxsDEBUG = fsCalendarDayboxs.Select(x => x.TextContentAsync().Result).ToArray();

		for (int i = 0; i < fsCalendarDayboxs.Length; i++)
		{
			IElementHandle fsCalendarDaybox = fsCalendarDayboxs[i];

			DateOnly dateOnly = default;
			string? title = default;

			string html = await fsCalendarDaybox.InnerHTMLAsync();
			/*
			<div class="fsCalendarDate" data-day="29" data-year="2024" data-month="4"><span class="fsCalendarDay">Wed,</span> <span class="fsCalendarMonth">May</span> 29</div>
			<div class="fsCalendarInfo">
				<a class="fsCalendarEventTitle fsCalendarEventLink" title="Question. Persuade. Refer. for Youth &amp; Families " data-occur-id="1711916" href="#">Question. Persuade. Refer. for Youth &amp; Families</a>
				<div class="fsTimeRange">
					<time datetime="2024-05-29T17:45:00-07:00" class="fsStartTime"><span class="fsHour"> 5</span>:<span class="fsMinute">45</span> <span class="fsMeridian">PM</span></time>
					<span class="fsTimeSeperator"> - </span>
					<time datetime="2024-05-29T20:00:00-07:00" class="fsEndTime"><span class="fsHour"> 8</span>:<span class="fsMinute">00</span> <span class="fsMeridian">PM</span></time>
				</div>
			</div>
			*/

			IElementHandle? fsCalendarInfo = await fsCalendarDaybox.QuerySelectorAsync("div.fsCalendarInfo");

			IElementHandle? a = await fsCalendarDaybox.QuerySelectorAsync("a");

			if (a != null)
			{
				title = await a.TextContentAsync();
			}

			if (!string.IsNullOrWhiteSpace(title))
			{
				IElementHandle? fsCalendarDate = await fsCalendarDaybox.QuerySelectorAsync("div.fsCalendarDate");

				int dataday = int.Parse(await fsCalendarDate.GetAttributeAsync("data-day"));
				int datayear = int.Parse(await fsCalendarDate.GetAttributeAsync("data-year"));
				int datamonth = int.Parse(await fsCalendarDate.GetAttributeAsync("data-month"));
				dateOnly = new DateOnly(datayear, datamonth + 1, dataday);

				var startTime = new TimeOnly(0, 0, 0);
				var endTime = new TimeOnly(23, 59, 59);

				IElementHandle? fsTimeRange = await fsCalendarDaybox.QuerySelectorAsync("div.fsTimeRange");

				if (fsTimeRange != null)
				{
					IReadOnlyList<IElementHandle> times = await fsTimeRange.QuerySelectorAllAsync("time");
					DateTime[] timesText = times.Select(async x => await x.GetAttributeAsync("datetime")).Select(x => DateTime.Parse(x.Result)).ToArray();

					if (timesText.Length == 1)
					{
						startTime = ToTimeOnly(timesText[0]);
						endTime = ToTimeOnly(timesText[0]);
					}
					else if (timesText.Length == 2)
					{
						startTime = ToTimeOnly(timesText[0]);
						endTime = ToTimeOnly(timesText[1]);
					}
					else if (timesText.Length > 2)
					{
					}
				}

				_eventDetailsCollection.Add(new EventDetails(dateOnly, startTime, endTime, title, false));
			}
		}
	}

	private static void GenerateCalendar(EventDetails[] schoolYears)
	{
		var calendar = new Calendar();
		calendar.Method = "PUBLISH";
		//calendar.Group = "School Calendar";
		//calendar.Name = "School Calendar";
		//calendar.ProductId = "-//School Calendar//NONSGML School Calendar//EN";
		//calendar.TimeZones.Add(new VTimeZone("America/Los_Angeles"));
		calendar.AddProperty("X-WR-CALNAME", "School Calendar");

		foreach (EventDetails schoolYearEvent in schoolYears)
		{
			TimeOnly dh = schoolYearEvent.StartTime;
			TimeOnly dt = schoolYearEvent.EndTime;
			DateOnly day = schoolYearEvent.DayOfEvent;

			var e = new Ical.Net.CalendarComponents.CalendarEvent
			{
				Start = new CalDateTime(day.Year, day.Month, day.Day, dh.Hour, dh.Minute, dh.Second),
				End = new CalDateTime(day.Year, day.Month, day.Day, dt.Hour, dt.Minute, dt.Second),
				Summary = schoolYearEvent.Title,
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
		string serializedCalendar = serializer.SerializeToString(calendar);

		int yearHead = schoolYears.First().DayOfEvent.Year;
		int yearTail = schoolYears.Last().DayOfEvent.Year;
		File.WriteAllText($"Salem-Keizer_School-Year_Calendar_{yearHead}-{yearTail}.ics", serializedCalendar);
	}

	private static TimeOnly ToTimeOnly(DateTime dateTime)
	{
		return new TimeOnly(dateTime.Hour, dateTime.Minute, dateTime.Second);
	}
}
