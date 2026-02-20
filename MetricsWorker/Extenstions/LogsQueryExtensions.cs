using Azure.Monitor.Query.Models;

namespace MetricsWorker.Extensions;

public static class LogsQueryExtensions
{
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

    private static object? GetColumn(this LogsTableRow row, string columnName)
    {
        var index = row.GetColumnIndex(columnName);
        return index >= 0 && index < row.Count ? row[index] : null;
    }

    private static int GetColumnIndex(this LogsTableRow row, string columnName)
    {
        // This is a simplified version - in practice, you'd cache column indices
        for (int i = 0; i < row.Count; i++)
        {
            // You'll need to match against the table's column names
            // This requires access to the LogsTable parent
        }
        return -1;
    }
}