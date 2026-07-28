using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using wms_android.shared.DTOs;
using wms_android.shared.Interfaces;

namespace wms_android.ViewModels
{
    public partial class ClerkReportViewModel : ObservableObject
    {
        private readonly IParcelService _parcelService;
        private readonly ILogger<ClerkReportViewModel> _logger;

        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private DateTime _selectedDate = DateTime.Today;
        [ObservableProperty] private string _clerkName = string.Empty;

        [ObservableProperty] private int _dailyParcelCount;
        [ObservableProperty] private decimal _dailySalesTotal;
        [ObservableProperty] private decimal _monthlyTotal;
        [ObservableProperty] private int _monthlyRank;
        [ObservableProperty] private int _totalClerksRanked;
        [ObservableProperty] private string _rankingLabel = string.Empty;

        [ObservableProperty] private bool _hasDeposits;
        [ObservableProperty] private bool _hasExpenses;

        public ObservableCollection<ClerkReportExpenseItem> Deposits { get; } = new();
        public ObservableCollection<ClerkReportExpenseItem> Expenses { get; } = new();

        public ClerkReportViewModel(IParcelService parcelService, ILogger<ClerkReportViewModel> logger)
        {
            _parcelService = parcelService;
            _logger = logger;
        }

        partial void OnSelectedDateChanged(DateTime value)
        {
            _ = LoadReportAsync();
        }

        [RelayCommand]
        private async Task LoadReport()
        {
            await LoadReportAsync();
        }

        public async Task LoadReportAsync()
        {
            var userId = Preferences.Get("CurrentUserId", 0);
            if (userId == 0)
                return;

            IsLoading = true;
            try
            {
                var report = await _parcelService.GetClerkReportAsync(userId, SelectedDate);

                ClerkName = report.ClerkName;
                DailyParcelCount = report.DailyParcelCount;
                DailySalesTotal = report.DailySalesTotal;
                MonthlyTotal = report.MonthlyTotal;
                MonthlyRank = report.MonthlyRank;
                TotalClerksRanked = report.TotalClerksRanked;
                RankingLabel = TotalClerksRanked > 0
                    ? $"#{MonthlyRank} of {TotalClerksRanked}"
                    : "N/A";

                Deposits.Clear();
                foreach (var d in report.Deposits)
                    Deposits.Add(d);
                HasDeposits = Deposits.Count > 0;

                Expenses.Clear();
                foreach (var e in report.Expenses)
                    Expenses.Add(e);
                HasExpenses = Expenses.Count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading clerk report");
                Debug.WriteLine($"Error loading clerk report: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
