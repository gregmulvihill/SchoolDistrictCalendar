namespace SchoolDistrictCalendar
{
	public class EventDetails
	{
		public string? Title { get; }
		public TimeOnly StartTime { get; }
		public TimeOnly EndTime { get; }
		public DateOnly DayOfEvent { get; }
		public bool KeyEvent { get; private set; }

		public EventDetails(DateOnly dayOfEvent, TimeOnly startTime, TimeOnly endTime, string? title, bool keyEvent)
		{
			DayOfEvent = dayOfEvent;
			StartTime = startTime;
			EndTime = endTime;
			Title = title;
			KeyEvent = keyEvent;
		}

		public override string ToString() => $"{DayOfEvent} {StartTime}-{EndTime} {KeyEvent} {Title}";

		public void SetKeyEvent(bool keyEvent)
		{
			KeyEvent = keyEvent;
		}
	}
}