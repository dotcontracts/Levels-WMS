using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using wms_android.shared.DTOs;
using wms_android.shared.Interfaces;

namespace wms_android.ViewModels
{
    public partial class BranchManagerReportViewModel : ObservableObject
    {
        private readonly IParcelService _parcelService;
        private readonly IUserService _userService;
        private readonly HttpClient _httpClient;
        private readonly ILogger<BranchManagerReportViewModel> _logger;

        // The cycle:
        // T-1 (yesterday)   = parcel booking day → the COD parcels were raised
        // T   (today)       = delivery day → manager delivers, declares deposit + expenses
        // T+1 (tomorrow)    = if manager didn't declare today, they get locked

        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string _branchName = "";
        [ObservableProperty] private string _reportMonth;

        // Monthly summary (all deposits this month)
        [ObservableProperty] private decimal _monthlyCodTotal;
        [ObservableProperty] private decimal _monthlyDeposited;
        [ObservableProperty] private decimal _monthlyBalance;
        [ObservableProperty] private ObservableCollection<BranchDepositDto> _deposits = new();

        // Previous-day COD info (what is being declared today)
        [ObservableProperty] private decimal _previousDayCodTotal;
        [ObservableProperty] private string _previousDayLabel = "";

        // Declaration form
        [ObservableProperty] private string _depositAmountInput = "";
        [ObservableProperty] private string _expenseAmountInput = "";
        [ObservableProperty] private string _expenseCategory = "fuel";
        [ObservableProperty] private string _expenseDescription = "";
        [ObservableProperty] private bool _isSubmitting;
        [ObservableProperty] private string _submitError = "";
        [ObservableProperty] private string _submitSuccess = "";

        public List<string> ExpenseCategories { get; } = new()
        {
            "fuel", "casual_labor", "hired_cars", "maintenance",
            "office_supplies", "utilities", "transport", "other"
        };

        private int? _existingDepositId;
        private int? _branchId;

        public BranchManagerReportViewModel(
            IParcelService parcelService,
            IUserService userService,
            HttpClient httpClient,
            ILogger<BranchManagerReportViewModel> logger)
        {
            _parcelService = parcelService;
            _userService = userService;
            _httpClient = httpClient;
            _logger = logger;
            ReportMonth = DateTime.Today.ToString("MMMM yyyy");
        }

        public async Task LoadReportData()
        {
            IsLoading = true;
            SubmitError = "";
            SubmitSuccess = "";
            try
            {
                var username = Preferences.Get("CurrentUsername", string.Empty);
                if (string.IsNullOrEmpty(username)) return;

                var user = await _userService.GetUserByUsernameAsync(username);
                if (user == null) return;

                BranchName = user.Branch?.Name ?? string.Empty;
                _branchId = user.BranchId;
                if (string.IsNullOrEmpty(BranchName)) return;

                var token = Preferences.Get("AuthToken", string.Empty);
                if (!string.IsNullOrEmpty(token))
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                // Today is the delivery day (T).
                // Previous day (T-1) is when the COD parcels were booked/raised.
                var today = DateTime.Today;
                var previousDay = today.AddDays(-1);

                PreviousDayLabel = previousDay.ToString("dd MMM yyyy");

                // Load previous-day COD total via the dashboard endpoint
                var dashboardUrl = $"/api/parcels/branch-manager-dashboard?branchName={Uri.EscapeDataString(BranchName)}&date={today:yyyy-MM-dd}";
                var dashResponse = await _httpClient.GetAsync(dashboardUrl);
                if (dashResponse.IsSuccessStatusCode)
                {
                    var dashJson = await dashResponse.Content.ReadAsStringAsync();
                    var dashOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var dash = JsonSerializer.Deserialize<BranchManagerDashboardDto>(dashJson, dashOptions);
                    if (dash != null)
                        PreviousDayCodTotal = dash.PreviousDayCodSales;
                }

                // Load this month's deposit records
                var year = today.Year;
                var month = today.Month;
                var url = $"/api/BranchDeposits?branch={Uri.EscapeDataString(BranchName)}&year={year}&month={month}";
                var response = await _httpClient.GetAsync(url);

                Deposits.Clear();
                MonthlyCodTotal = 0;
                MonthlyDeposited = 0;
                _existingDepositId = null;
                DepositAmountInput = "";

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var list = JsonSerializer.Deserialize<System.Collections.Generic.List<BranchDepositDto>>(json, options);

                    if (list != null)
                    {
                        foreach (var d in list)
                        {
                            Deposits.Add(d);
                            MonthlyCodTotal += d.CodTotal;
                            MonthlyDeposited += d.DepositAmount;
                            // Check if there's already a record for today's declaration (date = previous day)
                            if (d.Date.Date == previousDay.Date)
                            {
                                _existingDepositId = d.Id;
                                DepositAmountInput = d.DepositAmount.ToString("F2");
                            }
                        }
                    }
                }

                MonthlyBalance = MonthlyCodTotal - MonthlyDeposited;
                ReportMonth = new DateTime(year, month, 1).ToString("MMMM yyyy");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading branch manager report");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SubmitDeposit()
        {
            SubmitError = "";
            SubmitSuccess = "";

            if (!decimal.TryParse(DepositAmountInput, NumberStyles.Any, CultureInfo.InvariantCulture, out var depositAmount) || depositAmount < 0)
            {
                SubmitError = "Please enter a valid deposit amount.";
                return;
            }

            IsSubmitting = true;
            try
            {
                // Deposit record date = yesterday (the parcel booking/COD day, T-1)
                var depositDate = DateTime.Today.AddDays(-1);
                // Expense date = today (the delivery day, T)
                var expenseDate = DateTime.Today;
                var userId = Preferences.Get("CurrentUserId", 0);
                var userName = Preferences.Get("CurrentUsername", string.Empty);

                if (_existingDepositId.HasValue)
                {
                    var updatePayload = new { codTotal = PreviousDayCodTotal, depositAmount };
                    var content = new StringContent(JsonSerializer.Serialize(updatePayload), Encoding.UTF8, "application/json");
                    var resp = await _httpClient.PutAsync($"/api/BranchDeposits/{_existingDepositId}", content);
                    resp.EnsureSuccessStatusCode();
                }
                else
                {
                    var createPayload = new
                    {
                        branch = BranchName,
                        date = depositDate.ToString("yyyy-MM-dd"),
                        codTotal = PreviousDayCodTotal,
                        depositAmount
                    };
                    var content = new StringContent(JsonSerializer.Serialize(createPayload), Encoding.UTF8, "application/json");
                    var resp = await _httpClient.PostAsync("/api/BranchDeposits", content);
                    if (!resp.IsSuccessStatusCode &&
                        resp.StatusCode != System.Net.HttpStatusCode.Conflict)
                        resp.EnsureSuccessStatusCode();
                }

                if (!string.IsNullOrWhiteSpace(ExpenseAmountInput) &&
                    decimal.TryParse(ExpenseAmountInput, NumberStyles.Any, CultureInfo.InvariantCulture, out var expenseAmount) && expenseAmount > 0)
                {
                    var description = string.IsNullOrWhiteSpace(ExpenseDescription)
                        ? "COD delivery expenses"
                        : ExpenseDescription;
                    var expensePayload = new
                    {
                        category = ExpenseCategory,
                        description,
                        amount = expenseAmount,
                        date = expenseDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                        branchId = _branchId,
                        branchName = BranchName,
                        clerkId = userId,
                        clerkName = userName
                    };
                    var expContent = new StringContent(JsonSerializer.Serialize(expensePayload), Encoding.UTF8, "application/json");
                    var expResp = await _httpClient.PostAsync("/api/Expenses", expContent);
                    if (!expResp.IsSuccessStatusCode)
                    {
                        var expErr = await expResp.Content.ReadAsStringAsync();
                        _logger.LogError("Expense POST failed {Status}: {Body}", expResp.StatusCode, expErr);
                        SubmitError = "Deposit saved but expense failed. Please contact support.";
                    }
                }

                SubmitSuccess = "Declaration submitted for approval.";
                ExpenseAmountInput = "";
                ExpenseCategory = "fuel";
                ExpenseDescription = "";
                await LoadReportData();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting deposit from report");
                SubmitError = "Submission failed. Please try again.";
            }
            finally
            {
                IsSubmitting = false;
            }
        }

        [RelayCommand]
        private async Task GoBack() => await Shell.Current.GoToAsync("..");
    }
}
