namespace Eidos.Services;

/// <summary>
/// Service for managing seasonal holiday themes with emojis
/// Provides work-appropriate, inclusive holiday decorations for the UI
/// </summary>
public class HolidayThemeService
{
    private readonly List<Holiday> _holidays;

    public HolidayThemeService()
    {
        _holidays = new List<Holiday>
        {
            // New Year's
            new Holiday
            {
                Name = "New Year's",
                StartMonth = 12,
                StartDay = 31,
                EndMonth = 1,
                EndDay = 2,
                Emojis = new[] { "🎊", "🎉", "✨", "🥳" }
            },

            // Lunar New Year (approximate - varies by year, using late January/early February)
            new Holiday
            {
                Name = "Lunar New Year",
                StartMonth = 1,
                StartDay = 20,
                EndMonth = 2,
                EndDay = 15,
                Emojis = new[] { "🧧", "🏮", "🐉", "🎆" }
            },

            // Valentine's Day
            new Holiday
            {
                Name = "Valentine's Day",
                StartMonth = 2,
                StartDay = 13,
                EndMonth = 2,
                EndDay = 15,
                Emojis = new[] { "💝", "💕", "💖", "🌹" }
            },

            // St. Patrick's Day
            new Holiday
            {
                Name = "St. Patrick's Day",
                StartMonth = 3,
                StartDay = 16,
                EndMonth = 3,
                EndDay = 18,
                Emojis = new[] { "🍀", "🌈", "💚", "🎩" }
            },

            // Spring/Earth Day
            new Holiday
            {
                Name = "Spring/Earth Day",
                StartMonth = 4,
                StartDay = 20,
                EndMonth = 4,
                EndDay = 23,
                Emojis = new[] { "🌸", "🌺", "🌍", "🌱" }
            },

            // Cinco de Mayo
            new Holiday
            {
                Name = "Cinco de Mayo",
                StartMonth = 5,
                StartDay = 4,
                EndMonth = 5,
                EndDay = 6,
                Emojis = new[] { "🎉", "🌮", "🎊", "🎺" }
            },

            // Pride Month
            new Holiday
            {
                Name = "Pride Month",
                StartMonth = 6,
                StartDay = 1,
                EndMonth = 6,
                EndDay = 30,
                Emojis = new[] { "🏳️‍🌈", "🌈", "💜", "✨" }
            },

            // Independence Day (US)
            new Holiday
            {
                Name = "Summer Celebration",
                StartMonth = 7,
                StartDay = 3,
                EndMonth = 7,
                EndDay = 5,
                Emojis = new[] { "🎆", "🎇", "⭐", "🎉" }
            },

            // Back to School
            new Holiday
            {
                Name = "Back to School",
                StartMonth = 8,
                StartDay = 15,
                EndMonth = 9,
                EndDay = 10,
                Emojis = new[] { "📚", "✏️", "🎒", "🍎" }
            },

            // Halloween
            new Holiday
            {
                Name = "Halloween",
                StartMonth = 10,
                StartDay = 25,
                EndMonth = 11,
                EndDay = 1,
                Emojis = new[] { "🎃", "👻", "🦇", "🕸️" }
            },

            // Diwali (approximate - varies by year, using October/November)
            new Holiday
            {
                Name = "Diwali",
                StartMonth = 10,
                StartDay = 20,
                EndMonth = 11,
                EndDay = 15,
                Emojis = new[] { "🪔", "✨", "🎆", "🌟" }
            },

            // Thanksgiving
            new Holiday
            {
                Name = "Gratitude Season",
                StartMonth = 11,
                StartDay = 20,
                EndMonth = 11,
                EndDay = 28,
                Emojis = new[] { "🍂", "🦃", "🥧", "🍁" }
            },

            // Winter Holidays (inclusive of Christmas, Hanukkah, Kwanzaa, New Year's)
            new Holiday
            {
                Name = "Winter Holidays",
                StartMonth = 12,
                StartDay = 1,
                EndMonth = 12,
                EndDay = 31,
                Emojis = new[] { "❄️", "⛄", "🎄", "🕎", "🎁", "✨" }
            }
        };
    }

    /// <summary>
    /// Gets the current active holiday based on today's date
    /// </summary>
    /// <returns>Active holiday or null if no holiday is active</returns>
    public Holiday? GetCurrentHoliday()
    {
        return GetHolidayForDate(DateTime.Now);
    }

    /// <summary>
    /// Gets the active holiday for a specific date
    /// </summary>
    /// <param name="date">Date to check</param>
    /// <returns>Active holiday or null if no holiday is active</returns>
    public Holiday? GetHolidayForDate(DateTime date)
    {
        foreach (var holiday in _holidays)
        {
            if (IsDateInRange(date, holiday))
            {
                return holiday;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets all configured holidays
    /// </summary>
    public IReadOnlyList<Holiday> GetAllHolidays() => _holidays.AsReadOnly();

    /// <summary>
    /// Checks if a date falls within a holiday's range
    /// Handles year-wrapping (e.g., Dec 31 - Jan 2)
    /// </summary>
    private bool IsDateInRange(DateTime date, Holiday holiday)
    {
        var month = date.Month;
        var day = date.Day;

        // Handle year-wrapping holidays (e.g., New Year's: Dec 31 - Jan 2)
        if (holiday.StartMonth > holiday.EndMonth)
        {
            // Date is in the start year portion (e.g., December)
            if (month == holiday.StartMonth && day >= holiday.StartDay)
                return true;

            // Date is in the end year portion (e.g., January)
            if (month == holiday.EndMonth && day <= holiday.EndDay)
                return true;

            // Date is in a month between (shouldn't happen for typical holidays, but handle it)
            if (month > holiday.StartMonth || month < holiday.EndMonth)
                return true;

            return false;
        }

        // Normal case: holiday within same year
        // Check if month is within range
        if (month < holiday.StartMonth || month > holiday.EndMonth)
            return false;

        // If in start month, check if day is >= start day
        if (month == holiday.StartMonth && day < holiday.StartDay)
            return false;

        // If in end month, check if day is <= end day
        if (month == holiday.EndMonth && day > holiday.EndDay)
            return false;

        return true;
    }
}

/// <summary>
/// Represents a holiday with date range and emoji decorations
/// </summary>
public class Holiday
{
    /// <summary>
    /// Display name of the holiday
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Starting month (1-12)
    /// </summary>
    public int StartMonth { get; set; }

    /// <summary>
    /// Starting day of month (1-31)
    /// </summary>
    public int StartDay { get; set; }

    /// <summary>
    /// Ending month (1-12)
    /// </summary>
    public int EndMonth { get; set; }

    /// <summary>
    /// Ending day of month (1-31)
    /// </summary>
    public int EndDay { get; set; }

    /// <summary>
    /// Array of emojis to use for this holiday
    /// Will be randomly selected for variety
    /// </summary>
    public string[] Emojis { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets a random emoji from the holiday's emoji collection
    /// </summary>
    public string GetRandomEmoji()
    {
        if (Emojis.Length == 0)
            return string.Empty;

        var random = new Random();
        return Emojis[random.Next(Emojis.Length)];
    }

    /// <summary>
    /// Gets a specific emoji by index (wraps around if index is out of bounds)
    /// </summary>
    public string GetEmoji(int index)
    {
        if (Emojis.Length == 0)
            return string.Empty;

        return Emojis[index % Emojis.Length];
    }
}
