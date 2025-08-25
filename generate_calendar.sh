#!/bin/bash

# Salem-Keizer School District Calendar Generator
# Usage: ./generate_calendar.sh [options]

echo "Salem-Keizer School District Calendar Generator"
echo "================================================"
echo ""

# Default behavior: use cache if available
if [[ "$1" == "--help" ]] || [[ "$1" == "-h" ]]; then
    echo "Usage: $0 [options]"
    echo ""
    echo "Options:"
    echo "  --force-refresh, -f    Force re-scraping from website (ignore cache)"
    echo "  --output FILE, -o FILE  Specify output filename (default: auto-generated)"
    echo "  --help, -h              Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0                      # Use cached data if available"
    echo "  $0 -f                   # Force fresh scraping from website"
    echo "  $0 -o mycalendar.ics    # Custom output filename"
    echo "  $0 -f -o new.ics        # Force refresh with custom output"
    exit 0
fi

# Run the Python scraper
python school_calendar_scraper.py "$@"

# Check if successful
if [ $? -eq 0 ]; then
    echo ""
    echo "✓ Calendar successfully generated!"
    echo ""
    echo "You can now import the .ics file into your calendar application:"
    echo "  - Google Calendar: Settings → Import & Export → Import"
    echo "  - Apple Calendar: File → Import"
    echo "  - Outlook: File → Open & Export → Import/Export"
    echo ""
    ls -lh *.ics 2>/dev/null | tail -n 1
else
    echo ""
    echo "✗ Error generating calendar. Please check the error messages above."
    exit 1
fi