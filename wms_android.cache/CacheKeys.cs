namespace wms_android.cache
{
    public static class CacheKeys
    {
        public static class Parcels
        {
            public const string Prefix = "parcels";
            public static string Paged(int page, int pageSize, string? destination = null, int? status = null, string? search = null, string? fromDate = null, string? toDate = null, string? createdByUsername = null)
                => $"{Prefix}:paged:{page}:{pageSize}:{destination ?? "all"}:{status?.ToString() ?? "all"}:{search?.GetHashCode().ToString() ?? "none"}:{fromDate ?? "none"}:{toDate ?? "none"}:{createdByUsername ?? "all"}";

            public static string ById(string id) => $"{Prefix}:id:{id}";
            public static string Destinations(int? userId) => $"{Prefix}:destinations:{userId?.ToString() ?? "all"}";
            public static string Clerks(string destination) => $"{Prefix}:clerks:{destination}";
        }

        public static class Users
        {
            public const string Prefix = "users";
            public static string All() => $"{Prefix}:all";
            public static string ById(int id) => $"{Prefix}:id:{id}";
            public static string ByUsername(string username) => $"{Prefix}:username:{username}";
        }

        public static class Branches
        {
            public const string Prefix = "branches";
            public static string All() => $"{Prefix}:all";
            public static string ById(int id) => $"{Prefix}:id:{id}";
        }

        public static class Dispatches
        {
            public const string Prefix = "dispatches";
            public static string Paged(int page, int pageSize) => $"{Prefix}:paged:{page}:{pageSize}";
            public static string ById(string id) => $"{Prefix}:id:{id}";
        }

        public static class StaticData
        {
            public const string Prefix = "static";
            public static string Vehicles() => $"{Prefix}:vehicles";
            public static string Drivers() => $"{Prefix}:drivers";
            public static string Roles() => $"{Prefix}:roles";
        }

        public static class Expenses
        {
            public const string Prefix = "expenses";
            public static string All() => $"{Prefix}:all";
            public static string ExpenseTag() => $"{Prefix}:tag";
        }

        public static class Reports
        {
            public const string Prefix = "reports";
            public static string ClerkDebitSummary(string fromDate, string toDate) => $"{Prefix}:clerk-debit:{fromDate}:{toDate}";
            public static string ReportSummary(string fromDate, string toDate, string? destination, string? branch) => $"{Prefix}:summary:{fromDate}:{toDate}:{destination ?? "all"}:{branch ?? "all"}";
            public static string ReportTag() => $"{Prefix}:tag";
        }

        public static class UserDashboard
        {
            public const string Prefix = "dashboard";
            public static string DailySales(int userId, string date) => $"{Prefix}:daily-sales:{userId}:{date}";
            public static string MonthlySales(int userId, string yearMonth) => $"{Prefix}:monthly-sales:{userId}:{yearMonth}";
            public static string AmountOwed(int userId, string date) => $"{Prefix}:amount-owed:{userId}:{date}";
            public static string CashIn(int userId, string date) => $"{Prefix}:cash-in:{userId}:{date}";
            public static string UserTag(int userId) => $"dashboard:user:{userId}";
        }
    }
}
