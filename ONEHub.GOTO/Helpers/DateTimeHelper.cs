namespace ONEHub.GOTO.Helpers
{
    public static class DateTimeHelper
    {
        private const int GmtMinus4 = -4;

        public static TimeSpan GmtMinus4TimeSpan => new TimeSpan(GmtMinus4, 0, 0);

        public static Func<DateTimeOffset> DateTimeOffsetProvider { get; set; } = () => DateTimeOffset.Now;

        public static DateTimeOffset ServerTimeOffset => DateTimeOffsetProvider();

        public static DateTime ServerTime => ServerTimeOffset.DateTime;

        public static DateTime GmtMinus4Now => ServerTimeOffset.ToOffset(GmtMinus4TimeSpan).DateTime;

        public static DateTime UtcNow => ServerTimeOffset.UtcDateTime;

        public static DateTime CreateDateTime(int year, int month, int day, int hour = 0, int minute = 0, int second = 0)
            => new DateTimeOffset(year, month, day, hour, minute, second, TimeSpan.Zero).DateTime;
    }
}
