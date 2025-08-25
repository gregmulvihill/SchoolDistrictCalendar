# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A C# console application that generates iCalendar (.ics) files for the Salem-Keizer school district by scraping event data from their website (https://salkeiz.k12.or.us/about/calendar). The application uses Playwright for browser automation to extract events and creates standardized calendar files that can be imported into any calendar application.

## Build and Run Commands

```bash
# Build the project
dotnet build

# Run the application
dotnet run --project SchoolDistrictCalendar/SchoolDistrictCalendar.csproj

# Build in Release mode
dotnet build -c Release

# Clean build artifacts
dotnet clean
```

## Architecture Overview

The application follows a single-responsibility pattern with web scraping and calendar generation:

1. **Web Scraping Layer** (Program.cs:76-163)
   - Uses Playwright to automate browser interactions with the school district calendar
   - Captures both regular events and key events from different calendar views
   - Caches scraped data in JSON files (`EventDetails.json`, `KeyEventDetails.json`) to avoid repeated scraping

2. **Data Model** (EventDetails.cs)
   - Simple POCO class representing calendar events with date, time, title, and key event flag
   - Supports both all-day and timed events

3. **Calendar Generation** (Program.cs:236-275)
   - Uses Ical.Net library to create standard iCalendar format files
   - Outputs calendar files named with the school year range

## Key Dependencies

- **Microsoft.Playwright** - Browser automation for scraping calendar data
- **Ical.Net** - iCalendar file generation
- **System.Text.Json** - JSON serialization for caching scraped data

## Important Implementation Details

- The application scrapes calendar data once and caches it in JSON files. Delete these files to force a re-scrape.
- Browser automation runs with `Headless: false` (visible browser) - change to `true` in Program.cs:81 for headless operation
- The scraper navigates through 12 months of calendar data starting from the previous month
- All events are marked as all-day events in the generated calendar
- Key events from the district's "KEY DATES" table are merged with regular calendar events

## Testing Considerations

When testing scraping functionality:
- The website structure at salkeiz.k12.or.us may change, requiring selector updates
- Browser automation requires Playwright browsers to be installed (`pwsh bin/Debug/net8.0/playwright.ps1 install`)
- Cache files should be deleted to test fresh scraping runs