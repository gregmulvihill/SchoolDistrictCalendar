#!/usr/bin/env python3
"""
Salem-Keizer School District Calendar Scraper
Generates an iCalendar (.ics) file by scraping the district calendar website.
"""

import asyncio
import json
import os
from datetime import datetime, date, time, timedelta
from pathlib import Path
from typing import List, Dict, Optional, Tuple
from dataclasses import dataclass, asdict
from dateutil import parser

from playwright.async_api import async_playwright, Page
from icalendar import Calendar, Event


@dataclass
class EventDetails:
    """Represents a school calendar event."""
    day_of_event: str  # ISO format date string
    start_time: str    # ISO format time string
    end_time: str      # ISO format time string
    title: str
    key_event: bool
    
    @property
    def date(self) -> date:
        """Convert day_of_event to date object."""
        return datetime.fromisoformat(self.day_of_event).date()
    
    @property
    def start_datetime(self) -> datetime:
        """Combine date and start time."""
        d = self.date
        t = datetime.fromisoformat(f"2000-01-01T{self.start_time}").time()
        return datetime.combine(d, t)
    
    @property
    def end_datetime(self) -> datetime:
        """Combine date and end time."""
        d = self.date
        t = datetime.fromisoformat(f"2000-01-01T{self.end_time}").time()
        return datetime.combine(d, t)


class SchoolCalendarScraper:
    """Scrapes Salem-Keizer school district calendar and generates iCalendar files."""
    
    def __init__(self):
        self.url = "https://salkeiz.k12.or.us/about/calendar"
        self.event_details_cache = "EventDetails.json"
        self.key_event_details_cache = "KeyEventDetails.json"
        self.event_details_collection: List[EventDetails] = []
        self.key_event_details_collection: List[EventDetails] = []
    
    async def scrape_calendar(self, force_refresh: bool = False):
        """
        Scrape the calendar website or load from cache.
        
        Args:
            force_refresh: If True, force re-scraping even if cache exists
        """
        # Check if cache exists and use it unless force_refresh
        if not force_refresh and self._cache_exists():
            print("Loading calendar data from cache...")
            self._load_cache()
        else:
            print("Scraping calendar data from website...")
            await self._scrape_website()
            self._save_cache()
            print(f"Scraped {len(self.event_details_collection)} regular events")
            print(f"Scraped {len(self.key_event_details_collection)} key events")
    
    def _cache_exists(self) -> bool:
        """Check if both cache files exist."""
        return (Path(self.event_details_cache).exists() and 
                Path(self.key_event_details_cache).exists())
    
    def _load_cache(self):
        """Load cached event data from JSON files."""
        with open(self.event_details_cache, 'r') as f:
            data = json.load(f)
            self.event_details_collection = [EventDetails(**item) for item in data]
        
        with open(self.key_event_details_cache, 'r') as f:
            data = json.load(f)
            self.key_event_details_collection = [EventDetails(**item) for item in data]
    
    def _save_cache(self):
        """Save scraped event data to JSON cache files."""
        with open(self.event_details_cache, 'w') as f:
            data = [asdict(event) for event in self.event_details_collection]
            json.dump(data, f, indent=2)
        
        with open(self.key_event_details_cache, 'w') as f:
            data = [asdict(event) for event in self.key_event_details_collection]
            json.dump(data, f, indent=2)
    
    async def _scrape_website(self):
        """Scrape the school district calendar website using Playwright."""
        async with async_playwright() as p:
            browser = await p.chromium.launch(headless=True)
            context = await browser.new_context()
            page = await context.new_page()
            
            # Navigate to calendar page
            await page.goto(self.url)
            await asyncio.sleep(2)  # Wait for page to load
            
            # First capture key events from the current view
            await self._capture_key_events(page)
            
            # Navigate through months and capture regular events
            await page.click('[aria-label="Previous Month"]')
            await asyncio.sleep(1)
            
            for i in range(12):
                await page.click('[aria-label="Next Month"]')
                await asyncio.sleep(1)
                await self._capture_events(page)
            
            await browser.close()
    
    async def _capture_key_events(self, page: Page):
        """Capture key events from the KEY DATES section."""
        # Look for key events in the special container
        key_event_elements = await page.query_selector_all('div.fsDayContainer > article')
        
        for element in key_event_elements:
            try:
                # Extract title
                title_elem = await element.query_selector('div.fsTitle > a.fsCalendarEventLink')
                if not title_elem:
                    continue
                title = await title_elem.inner_text()
                
                # Extract date
                time_elem = await element.query_selector('time')
                if not time_elem:
                    continue
                datetime_str = await time_elem.get_attribute('datetime')
                
                # Parse the date
                event_date = parser.parse(datetime_str).date()
                
                # Key events are typically all-day events
                event = EventDetails(
                    day_of_event=event_date.isoformat(),
                    start_time="00:00:00",
                    end_time="23:59:59",
                    title=title.strip(),
                    key_event=True
                )
                self.key_event_details_collection.append(event)
                
            except Exception as e:
                print(f"Error capturing key event: {e}")
                continue
    
    async def _capture_events(self, page: Page):
        """Capture regular calendar events from the current month view."""
        # Get all calendar day boxes
        day_boxes = await page.query_selector_all('div.fsCalendarDaybox')
        
        for day_box in day_boxes:
            try:
                # Check if there's an event in this day
                event_link = await day_box.query_selector('a.fsCalendarEventLink')
                if not event_link:
                    continue
                
                # Extract title
                title = await event_link.text_content()
                if not title or not title.strip():
                    continue
                
                # Extract date
                date_elem = await day_box.query_selector('div.fsCalendarDate')
                if not date_elem:
                    continue
                
                day = await date_elem.get_attribute('data-day')
                month = await date_elem.get_attribute('data-month')
                year = await date_elem.get_attribute('data-year')
                
                # Month is 0-based in the HTML
                event_date = date(int(year), int(month) + 1, int(day))
                
                # Extract time if available
                start_time_str = "00:00:00"
                end_time_str = "23:59:59"
                
                time_range = await day_box.query_selector('div.fsTimeRange')
                if time_range:
                    time_elements = await time_range.query_selector_all('time')
                    
                    if len(time_elements) >= 1:
                        start_datetime_str = await time_elements[0].get_attribute('datetime')
                        if start_datetime_str:
                            start_dt = parser.parse(start_datetime_str)
                            start_time_str = start_dt.time().isoformat()
                    
                    if len(time_elements) >= 2:
                        end_datetime_str = await time_elements[1].get_attribute('datetime')
                        if end_datetime_str:
                            end_dt = parser.parse(end_datetime_str)
                            end_time_str = end_dt.time().isoformat()
                    elif len(time_elements) == 1:
                        # Single time means start and end are the same
                        end_time_str = start_time_str
                
                event = EventDetails(
                    day_of_event=event_date.isoformat(),
                    start_time=start_time_str,
                    end_time=end_time_str,
                    title=title.strip(),
                    key_event=False
                )
                self.event_details_collection.append(event)
                
            except Exception as e:
                print(f"Error capturing event: {e}")
                continue
    
    def generate_calendar(self, output_filename: Optional[str] = None):
        """
        Generate an iCalendar file from the scraped events.
        
        Args:
            output_filename: Optional custom filename for the output
        """
        # Merge and process events
        all_events = self._merge_events()
        
        if not all_events:
            print("No events to generate calendar from!")
            return
        
        # Create calendar
        cal = Calendar()
        cal.add('prodid', '-//Salem-Keizer School Calendar//salkeiz.k12.or.us//')
        cal.add('version', '2.0')
        cal.add('method', 'PUBLISH')
        cal.add('x-wr-calname', 'Salem-Keizer School Calendar')
        cal.add('x-wr-timezone', 'America/Los_Angeles')
        
        # Add events to calendar
        for event_detail in all_events:
            event = Event()
            
            # Set event properties
            event.add('summary', event_detail.title)
            event.add('dtstart', event_detail.start_datetime)
            event.add('dtend', event_detail.end_datetime)
            event.add('dtstamp', datetime.now())
            event.add('created', datetime.now())
            event.add('last-modified', datetime.now())
            event.add('uid', f"{event_detail.date.isoformat()}-{hash(event_detail.title)}@salkeiz.k12.or.us")
            
            # Mark as all-day if it spans the full day
            if (event_detail.start_time == "00:00:00" and 
                event_detail.end_time == "23:59:59"):
                event.add('x-microsoft-cdo-alldayevent', 'TRUE')
                event.add('x-microsoft-cdo-busystatus', 'FREE')
                event.add('transp', 'TRANSPARENT')
            
            # Add category
            if event_detail.key_event:
                event.add('categories', ['Key Event', 'School Event'])
                event.add('priority', 1)
            else:
                event.add('categories', ['School Event'])
                event.add('priority', 5)
            
            event.add('class', 'PUBLIC')
            event.add('transp', 'TRANSPARENT')
            
            cal.add_component(event)
        
        # Determine output filename
        if not output_filename:
            year_start = min(e.date.year for e in all_events)
            year_end = max(e.date.year for e in all_events)
            output_filename = f"Salem-Keizer_School-Year_Calendar_{year_start}-{year_end}.ics"
        
        # Write calendar to file
        with open(output_filename, 'wb') as f:
            f.write(cal.to_ical())
        
        print(f"Calendar generated: {output_filename}")
        print(f"Total events: {len(all_events)}")
        print(f"Key events: {sum(1 for e in all_events if e.key_event)}")
    
    def _merge_events(self) -> List[EventDetails]:
        """Merge regular and key events, marking duplicates as key events."""
        # Create a dictionary of regular events by date for quick lookup
        events_by_date = {}
        for event in self.event_details_collection:
            event_date = event.date
            if event_date not in events_by_date:
                events_by_date[event_date] = []
            events_by_date[event_date].append(event)
        
        # Process key events
        events_to_add = []
        for key_event in self.key_event_details_collection:
            event_date = key_event.date
            
            # Check if there's a regular event on the same date
            if event_date in events_by_date:
                # Mark the first matching event as a key event
                matching_events = events_by_date[event_date]
                if matching_events:
                    matching_events[0].key_event = True
            else:
                # Add the key event as a new event
                events_to_add.append(key_event)
        
        # Combine all events
        all_events = self.event_details_collection + events_to_add
        
        # Sort by date and start time
        all_events.sort(key=lambda e: (e.date, e.start_time))
        
        return all_events


async def main():
    """Main function to run the calendar scraper."""
    import argparse
    
    parser = argparse.ArgumentParser(description='Scrape Salem-Keizer school calendar and generate iCalendar file')
    parser.add_argument('--force-refresh', '-f', action='store_true',
                        help='Force re-scraping even if cache exists')
    parser.add_argument('--output', '-o', type=str, default=None,
                        help='Output filename for the calendar (default: auto-generated)')
    
    args = parser.parse_args()
    
    scraper = SchoolCalendarScraper()
    
    # Scrape or load from cache
    await scraper.scrape_calendar(force_refresh=args.force_refresh)
    
    # Generate calendar file
    scraper.generate_calendar(output_filename=args.output)


if __name__ == "__main__":
    asyncio.run(main())