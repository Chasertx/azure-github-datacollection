using Azure.Monitor.Query.Models;

namespace MetricsWorker.Extensions;

// Helper methods to safely read typed values from a LogsTableRow.
// These return null instead of throwing when data is missing or malformed.
public static class LogsQueryExtensions
{
    // Try to read a column as text.
    public static string? GetString(this LogsTableRow row, string columnName)
    {
        try
        {
            var column = row.GetColumn(columnName);
            return column?.ToString();
        }
        catch
        {
            return null;
        }
    }

    // Try to read a column as a double (for numeric metrics).
    public static double? GetDouble(this LogsTableRow row, string columnName)
    {
        try
        {
            var column = row.GetColumn(columnName);
            if (column == null) return null;

            return Convert.ToDouble(column);
        }
        catch
        {
            return null;
        }
    }

    // Try to read a column as a 64-bit integer.
    public static long? GetInt64(this LogsTableRow row, string columnName)
    {
        try
        {
            var column = row.GetColumn(columnName);
            if (column == null) return null;

            return Convert.ToInt64(column);
        }
        catch
        {
            return null;
        }
    }

    // Try to read a column as DateTimeOffset.
    // Handles values that are already DateTimeOffset or parseable date strings.
    public static DateTimeOffset? GetDateTimeOffset(this LogsTableRow row, string columnName)
    {
        try
        {
            var column = row.GetColumn(columnName);
            if (column == null) return null;

            if (column is DateTimeOffset dto)
                return dto;

            if (DateTime.TryParse(column.ToString(), out var dt))
                return new DateTimeOffset(dt);

            return null;
        }
        catch
        {
            return null;
        }
    }

    // Shared column lookup used by the typed getter helpers.
    // Returns null when the column name cannot be resolved.
    private static object? GetColumn(this LogsTableRow row, string columnName)
    {
        var index = row.GetColumnIndex(columnName);
        return index >= 0 && index < row.Count ? row[index] : null;
    }

    // Maps a column name to its index in the current row.
    // Note: this is currently a placeholder and always returns -1,
    // because LogsTableRow alone does not expose column-name metadata.
    private static int GetColumnIndex(this LogsTableRow row, string columnName)
    {
        // In a complete version, you'd resolve names from the parent LogsTable
        // and cache the mapping for performance.
        for (int i = 0; i < row.Count; i++)
        {
            // Name matching would happen here once column metadata is available.
        }
        return -1;
    }
}
