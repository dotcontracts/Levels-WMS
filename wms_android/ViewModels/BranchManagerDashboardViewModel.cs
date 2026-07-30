using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using wms_android.Extensions;
using wms_android.shared.Interfaces;
using wms_android.Views;

namespace wms_android.ViewModels
{
    public partial class BranchManagerDashboardViewModel : ObservableObject
    {
        private readonly IParcelService _parcelService;
        private readonly IUserService _userService;
        private readonly HttpClient _httpClient;
        private readonly ILogger<BranchManagerDashboardViewModel> _logger;
        public INavigation Navigation { get; set; }

        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private decimal _previousDayCodSales;
        [ObservableProperty] private decimal _previousDayCodAmountOwed;
        [ObservableProperty] private decimal _todayLiveSales;
        [ObservableProperty] private decimal _monthlyDebit;
        [ObservableProperty] private string _userName;
        [ObservableProperty] private string _branchName;
        [ObservableProperty] private bool _canDispatch;

        // Lock state
        [ObservableProperty] private bool _isLocked;
        [ObservableProperty] private string _lockReason = "none";
        [ObservableProperty] private decimal _lockCodTotal;
        [ObservableProperty] private string _lockPreviousDate;
        [ObservableProperty] private int? _existingDepositId;
        [ObservableProperty] private int? _existingExpenseId;

        // Pending approval banner (non-blocking)
        [ObservableProperty] private bool _showPendingApprovalBanner;

        // Per-part rejection/approval state
        [ObservableProperty] private bool _depositRejected;
        [ObservableProperty] private bool _depositApproved;
        [ObservableProperty] private string _depositRejectionReason = "";
        [ObservableProperty] private decimal _approvedDepositAmount;

        [ObservableProperty] private bool _expenseRejected;
        [ObservableProperty] private bool _expenseApproved;
        [ObservableProperty] private bool _hasExistingExpense;
        [ObservableProperty] private string _expenseRejectionReason = "";
        [ObservableProperty] private decimal _approvedExpenseAmount;
        [ObservableProperty] private string _approvedExpenseDescription = "";

        private int? _branchId;
        private DateTime _codDate;

        // Form inputs
        [ObservableProperty] private string _depositAmountInput = "";
        [ObservableProperty] private string _expenseAmountInput = "";
        [ObservableProperty] private string _expenseCategory = "fuel";
        [ObservableProperty] private string _expenseDescription = "";
        [ObservableProperty] private bool _isSubmitting;
        [ObservableProperty] private string _submitError = "";
        [ObservableProperty] private bool _isRefreshingLock;

        public List<string> ExpenseCategories { get; } = new()
        {
            "fuel", "casual_labor", "hired_cars", "maintenance",
            "office_supplies", "utilities", "transport", "other", "cod_expense"
        };

        public BranchManagerDashboardViewModel(
            IParcelService parcelService,
            IUserService userService,
            HttpClient httpClient,
            ILogger<BranchManagerDashboardViewModel> logger)
        {
            _parcelService = parcelService;
            _userService = userService;
            _httpClient = httpClient;
            _logger = logger;
            UserName = "Manager";
            BranchName = "";
        }

        [RelayCommand]
        private async Task NavigateToDelivery() => await Shell.Current.GoToAsync(nameof(DeliveryView));

        [RelayCommand]
        private async Task NavigateToViewAllParcels()
        {
            try { await Shell.Current.GoToAsync(nameof(AllParcelsView)); }
            catch (Exception ex) { await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK"); }
        }

        [RelayCommand]
        private async Task NavigateToAddParcel() => await Shell.Current.GoToAsync(nameof(ParcelsView));

        [RelayCommand]
        private async Task GenerateReport() => await Shell.Current.GoToAsync(nameof(BranchManagerReportView));

        [RelayCommand]
        private async Task NavigateToParcelConfirmation() => await Shell.Current.GoToAsync("ParcelConfirmationView");

        [RelayCommand]
        private async Task NavigateToDispatch() => await Shell.Current.GoToAsync(nameof(DispatchView));

        [RelayCommand]
        private async Task RefreshLockStatus()
        {
            if (string.IsNullOrEmpty(BranchName)) { IsRefreshingLock = false; return; }

            try
            {
                var lockStatus = await _parcelService.GetBranchCodLockStatusAsync(BranchName, DateTime.Today);

                DepositRejected = lockStatus.DepositStatus == "rejected";
                DepositApproved = lockStatus.DepositStatus == "approved";
                DepositRejectionReason = lockStatus.DepositRejectionReason ?? "";
                ApprovedDepositAmount = DepositApproved ? (lockStatus.DepositAmount ?? 0m) : 0m;
                ExistingDepositId = lockStatus.DepositRecordId;

                HasExistingExpense = lockStatus.ExpenseRecordId.HasValue;
                ExpenseRejected = lockStatus.ExpenseStatus == "rejected";
                ExpenseApproved = lockStatus.ExpenseStatus == "approved";
                ExpenseRejectionReason = lockStatus.ExpenseRejectionReason ?? "";
                ApprovedExpenseAmount = ExpenseApproved ? (lockStatus.ExpenseAmount ?? 0m) : 0m;
                ExistingExpenseId = lockStatus.ExpenseRecordId;

                var isHardLocked = lockStatus.Reason == "pending_submission" || lockStatus.Reason == "rejected";
                IsLocked = isHardLocked;
                LockReason = lockStatus.Reason;
                ManagerShell.IsManagerLocked = isHardLocked;
                ShowPendingApprovalBanner = lockStatus.Reason == "pending_approval";

                if (!isHardLocked)
                {
                    var today = DateTime.Today;
                    var data = await _parcelService.GetBranchManagerDashboardAsync(BranchName, today);
                    PreviousDayCodSales = data.PreviousDayCodSales;
                    PreviousDayCodAmountOwed = data.PreviousDayCodAmountOwed;
                    TodayLiveSales = data.TodayLiveSales;
                    MonthlyDebit = data.MonthlyDebit;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing manager lock status");
            }
            finally
            {
                IsRefreshingLock = false;
            }
        }

        [RelayCommand]
        private async Task SubmitDeposit()
        {
            SubmitError = "";

            // _codDate = T+1 (delivery & declaration day, as returned by PreviousDate from the API)
            // depositDate = T+1 (same — the day the manager collected and must declare)
            var depositDate = _codDate != default ? _codDate.Date : DateTime.Today.AddDays(-1);
            var expenseDate = depositDate;
            var userId = Preferences.Get("CurrentUserId", 0);

            var token = Preferences.Get("AuthToken", string.Empty);
            if (!string.IsNullOrEmpty(token))
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            IsSubmitting = true;
            try
            {
                await _httpClient.AddSessionHeaderAsync();

                // --- Deposit ---
                // Submit if: first-time (pending_submission) OR rejected
                if (!DepositApproved)
                {
                    if (!decimal.TryParse(DepositAmountInput, NumberStyles.Any, CultureInfo.InvariantCulture, out var depositAmount) || depositAmount < 0)
                    {
                        SubmitError = "Please enter a valid deposit amount.";
                        return;
                    }

                    if (ExistingDepositId.HasValue)
                    {
                        // Update existing (rejected) deposit
                        var updatePayload = new { codTotal = LockCodTotal, depositAmount };
                        var updateJson = JsonSerializer.Serialize(updatePayload);
                        var updateContent = new StringContent(updateJson, Encoding.UTF8, "application/json");
                        var updateResponse = await _httpClient.PutAsync($"/api/BranchDeposits/{ExistingDepositId}", updateContent);
                        updateResponse.EnsureSuccessStatusCode();
                    }
                    else
                    {
                        // Create new deposit (first-time submission)
                        var createPayload = new
                        {
                            branch = BranchName,
                            date = depositDate.ToString("yyyy-MM-dd"),
                            codTotal = LockCodTotal,
                            depositAmount
                        };
                        var createJson = JsonSerializer.Serialize(createPayload);
                        var createContent = new StringContent(createJson, Encoding.UTF8, "application/json");
                        var createResponse = await _httpClient.PostAsync("/api/BranchDeposits", createContent);
                        if (!createResponse.IsSuccessStatusCode &&
                            createResponse.StatusCode != System.Net.HttpStatusCode.Conflict)
                            createResponse.EnsureSuccessStatusCode();
                    }
                }

                // --- Expense ---
                if (ExpenseRejected)
                {
                    if (!string.IsNullOrWhiteSpace(ExpenseAmountInput) &&
                        decimal.TryParse(ExpenseAmountInput, NumberStyles.Any, CultureInfo.InvariantCulture, out var expenseAmount) && expenseAmount > 0)
                    {
                        // Delete the rejected expense and recreate
                        if (ExistingExpenseId.HasValue)
                        {
                            var deleteResp = await _httpClient.DeleteAsync($"/api/Expenses/{ExistingExpenseId}");
                            if (!deleteResp.IsSuccessStatusCode)
                                _logger.LogWarning("Could not delete rejected expense {Id}: {Status}", ExistingExpenseId, deleteResp.StatusCode);
                        }

                        var description = string.IsNullOrWhiteSpace(ExpenseDescription) ? "COD delivery expenses" : ExpenseDescription;
                        var expensePayload = new
                        {
                            category = "cod_expense",
                            description,
                            amount = expenseAmount,
                            date = expenseDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                            branchId = _branchId,
                            branchName = BranchName,
                            clerkId = userId,
                            clerkName = UserName
                        };
                        var expenseJson = JsonSerializer.Serialize(expensePayload);
                        var expenseContent = new StringContent(expenseJson, Encoding.UTF8, "application/json");
                        var expenseResp = await _httpClient.PostAsync("/api/Expenses", expenseContent);
                        if (!expenseResp.IsSuccessStatusCode)
                        {
                            var expErr = await expenseResp.Content.ReadAsStringAsync();
                            _logger.LogError("Expense POST failed {Status}: {Body}", expenseResp.StatusCode, expErr);
                            SubmitError = "Deposit saved but expense failed. Please try again.";
                            return;
                        }
                    }
                }
                else if (!HasExistingExpense)
                {
                    // First-time submission with optional expense
                    if (!string.IsNullOrWhiteSpace(ExpenseAmountInput) &&
                        decimal.TryParse(ExpenseAmountInput, NumberStyles.Any, CultureInfo.InvariantCulture, out var expenseAmount) && expenseAmount > 0)
                    {
                        var description = string.IsNullOrWhiteSpace(ExpenseDescription) ? "COD delivery expenses" : ExpenseDescription;
                        var expensePayload = new
                        {
                            category = "cod_expense",
                            description,
                            amount = expenseAmount,
                            date = expenseDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                            branchId = _branchId,
                            branchName = BranchName,
                            clerkId = userId,
                            clerkName = UserName
                        };
                        var expenseJson = JsonSerializer.Serialize(expensePayload);
                        var expenseContent = new StringContent(expenseJson, Encoding.UTF8, "application/json");
                        var expenseResp = await _httpClient.PostAsync("/api/Expenses", expenseContent);
                        if (!expenseResp.IsSuccessStatusCode)
                        {
                            var expErr = await expenseResp.Content.ReadAsStringAsync();
                            _logger.LogError("Expense POST failed {Status}: {Body}", expenseResp.StatusCode, expErr);
                            SubmitError = "Deposit saved but expense failed. Please try again.";
                            return;
                        }
                    }
                }

                IsLocked = false;
                LockReason = "pending_approval";
                ManagerShell.IsManagerLocked = false;
                ShowPendingApprovalBanner = true;
                DepositRejected = false;
                ExpenseRejected = false;
                DepositAmountInput = "";
                ExpenseAmountInput = "";
                ExpenseCategory = "fuel";
                ExpenseDescription = "";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting declaration");
                SubmitError = "Submission failed. Please try again.";
            }
            finally
            {
                IsSubmitting = false;
            }
        }

        public async Task LoadDashboardData()
        {
            try
            {
                IsLoading = true;

                var username = Preferences.Get("CurrentUsername", string.Empty);
                if (string.IsNullOrEmpty(username)) return;

                var user = await _userService.GetUserByUsernameAsync(username);
                if (user == null) return;

                UserName = !string.IsNullOrWhiteSpace(user.FirstName) ? user.FirstName : user.Username;
                BranchName = user.Branch?.Name ?? string.Empty;
                _branchId = user.BranchId;
                CanDispatch = user.CanDispatch || (user.Role?.CanDispatch ?? false);

                if (string.IsNullOrEmpty(BranchName)) return;

                var today = DateTime.Today;

                var lockStatus = await _parcelService.GetBranchCodLockStatusAsync(BranchName, today);
                LockCodTotal = lockStatus.PreviousDayCodTotal;
                _codDate = lockStatus.PreviousDate;
                LockPreviousDate = lockStatus.PreviousDate.ToString("dd MMM yyyy");
                ExistingDepositId = lockStatus.DepositRecordId;
                ExistingExpenseId = lockStatus.ExpenseRecordId;

                // Per-part deposit state
                DepositRejected = lockStatus.DepositStatus == "rejected";
                DepositApproved = lockStatus.DepositStatus == "approved";
                DepositRejectionReason = lockStatus.DepositRejectionReason ?? "";
                ApprovedDepositAmount = DepositApproved ? (lockStatus.DepositAmount ?? 0m) : 0m;

                // Pre-fill deposit input if rejected so user only corrects it
                if (DepositRejected)
                    DepositAmountInput = (lockStatus.DepositAmount ?? 0m).ToString(CultureInfo.InvariantCulture);

                // Per-part expense state
                HasExistingExpense = lockStatus.ExpenseRecordId.HasValue;
                ExpenseRejected = lockStatus.ExpenseStatus == "rejected";
                ExpenseApproved = lockStatus.ExpenseStatus == "approved";
                ExpenseRejectionReason = lockStatus.ExpenseRejectionReason ?? "";
                ApprovedExpenseAmount = ExpenseApproved ? (lockStatus.ExpenseAmount ?? 0m) : 0m;

                // Pre-fill expense input if rejected
                if (ExpenseRejected)
                    ExpenseAmountInput = (lockStatus.ExpenseAmount ?? 0m).ToString(CultureInfo.InvariantCulture);

                var isHardLocked = lockStatus.Reason == "pending_submission" || lockStatus.Reason == "rejected";
                IsLocked = isHardLocked;
                LockReason = lockStatus.Reason;
                ManagerShell.IsManagerLocked = isHardLocked;
                ShowPendingApprovalBanner = lockStatus.Reason == "pending_approval";

                if (!isHardLocked)
                {
                    var data = await _parcelService.GetBranchManagerDashboardAsync(BranchName, today);
                    PreviousDayCodSales = data.PreviousDayCodSales;
                    PreviousDayCodAmountOwed = data.PreviousDayCodAmountOwed;
                    TodayLiveSales = data.TodayLiveSales;
                    MonthlyDebit = data.MonthlyDebit;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading branch manager dashboard");
                Debug.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
