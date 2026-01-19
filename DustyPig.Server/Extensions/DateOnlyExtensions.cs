using System;

//namespace DustyPig.Server.Extensions;

public static class DateOnlyExtensions
{
    public static DateOnly ToDateOnly(this DateTime date) => DateOnly.FromDateTime(date);

    public static DateOnly? ToDateOnly(this DateTime? date) => date == null ? null : DateOnly.FromDateTime(date.Value);

    public static DateTime ToDateTime(this DateOnly date) => new DateTime(date.Year, date.Month, date.Day);
}
