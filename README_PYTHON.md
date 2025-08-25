# Salem-Keizer School District Calendar Generator (Python)

A Python script that generates an iCalendar (.ics) file for the Salem-Keizer school district by scraping their official calendar website.

## Features

- Scrapes both regular events and key district events
- Caches scraped data to avoid unnecessary re-scraping
- Generates standard iCalendar files compatible with all major calendar applications
- Command-line interface with options for force refresh and custom output

## Quick Start

```bash
# Generate calendar using cached data (if available)
./generate_calendar.sh

# Force fresh scraping from website
./generate_calendar.sh --force-refresh

# Specify custom output filename
./generate_calendar.sh --output my-school-calendar.ics
```

## Installation

### Prerequisites

- Python 3.8 or higher
- pip (Python package manager)

### Setup

1. Install required Python packages:
```bash
pip install -r requirements.txt
```

2. Install Playwright browser (first time only):
```bash
playwright install chromium
```

## Usage

### Using the Convenience Script

The easiest way to generate the calendar:

```bash
./generate_calendar.sh
```

### Using Python Directly

For more control, you can run the Python script directly:

```bash
# Use cached data if available
python school_calendar_scraper.py

# Force re-scraping
python school_calendar_scraper.py --force-refresh

# Custom output filename
python school_calendar_scraper.py --output custom-calendar.ics

# Combine options
python school_calendar_scraper.py -f -o new-calendar.ics
```

## How It Works

1. **Scraping**: The script uses Playwright to automate a browser and navigate through the Salem-Keizer calendar website
2. **Caching**: Scraped data is saved to JSON files (`EventDetails.json` and `KeyEventDetails.json`) to avoid repeated scraping
3. **Processing**: Events are merged, deduplicated, and sorted
4. **Generation**: Creates a standard iCalendar file with all events

## Cache Management

The script automatically caches scraped data to improve performance:

- Cache files: `EventDetails.json` and `KeyEventDetails.json`
- To force fresh data, use the `--force-refresh` flag
- Delete cache files manually to reset: `rm *.json`

## Importing the Calendar

Once generated, you can import the .ics file into your calendar application:

### Google Calendar
1. Open Google Calendar
2. Click the gear icon → Settings
3. Select "Import & Export" from the left menu
4. Click "Select file from your computer" and choose the .ics file
5. Select which calendar to add events to
6. Click "Import"

### Apple Calendar (macOS/iOS)
1. Open Calendar app
2. File → Import (or drag and drop the .ics file)
3. Select the calendar to add events to
4. Click "Import"

### Outlook
1. Open Outlook
2. File → Open & Export → Import/Export
3. Select "Import an iCalendar (.ics) file"
4. Browse to the .ics file
5. Click "OK"

## Files Generated

- `Salem-Keizer_School-Year_Calendar_YYYY-YYYY.ics` - The calendar file (YYYY represents the school year)
- `EventDetails.json` - Cached regular events data
- `KeyEventDetails.json` - Cached key events data

## Troubleshooting

### Script fails to run
- Ensure Python 3.8+ is installed: `python --version`
- Install dependencies: `pip install -r requirements.txt`
- Install Playwright browser: `playwright install chromium`

### No events found
- The website structure may have changed
- Try force refresh: `./generate_calendar.sh -f`
- Check if the website is accessible: https://salkeiz.k12.or.us/about/calendar

### Import issues
- Ensure the .ics file was generated successfully
- Check that the file isn't empty or corrupted
- Try opening the file in a text editor to verify it contains BEGIN:VCALENDAR

## Technical Details

- **Language**: Python 3.8+
- **Web Scraping**: Playwright (headless Chromium)
- **Calendar Format**: iCalendar (RFC 5545)
- **Dependencies**: playwright, icalendar, python-dateutil

## License

Same as the original C# project.