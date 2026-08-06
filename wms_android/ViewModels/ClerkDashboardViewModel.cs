using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using wms_android.shared.DTOs;
using wms_android.Views;
using wms_android.shared.Interfaces;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using wms_android.Extensions;

namespace wms_android.ViewModels
{
    public partial class ClerkDashboardViewModel : ObservableObject
    {
        private readonly IParcelService _parcelService;
        private readonly IUserService _userService;
        private readonly HttpClient _httpClient;
        private readonly ILogger<ClerkDashboardViewModel> _logger;
        public INavigation Navigation { get; set; }

        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private double _amountOwed;
        [ObservableProperty] private double _cashIn;
        [ObservableProperty] private double _dailySales;
        [ObservableProperty] private double _monthlySales;
        [ObservableProperty] private string _userName;
        [ObservableProperty] private string _userRole;
        [ObservableProperty] private bool _canDispatch;

        // Lock state
        [ObservableProperty] private bool _isLocked;
        [ObservableProperty] private string _lockReason = "none";
        [ObservableProperty] private decimal _lockPaidTotal;
        [ObservableProperty] private string _lockPreviousDate;

        // Pending approval banner (non-blocking)
        [ObservableProperty] private bool _showPendingApprovalBanner;

        // Deposit state
        [ObservableProperty] private bool _depositRejected;
        [ObservableProperty] private bool _depositApproved;
        [ObservableProperty] private string _depositRejectionReason = "";
        [ObservableProperty] private decimal _approvedDepositAmount;
        [ObservableProperty] private int? _existingDepositId;

        // Expense state
        [ObservableProperty] private bool _expenseRejected;
        [ObservableProperty] private bool _expenseApproved;
        [ObservableProperty] private string _expenseRejectionReason = "";
        [ObservableProperty] private decimal _approvedExpenseAmount;
        [ObservableProperty] private int? _existingExpenseId;

        // Form inputs
        [ObservableProperty] private string _depositAmountInput = "";
        [ObservableProperty] private string _expenseAmountInput = "";
        [ObservableProperty] private string _expenseCategory = "other";
        [ObservableProperty] private string _expenseDescription = "";
        [ObservableProperty] private bool _isSubmitting;
        [ObservableProperty] private string _submitError = "";
        [ObservableProperty] private bool _isRefreshingLock;

        private DateTime _declarationDate;

        public List<string> ExpenseCategories { get; } = new()
        {
            "fuel", "casual_labor", "hired_cars", "maintenance",
            "office_supplies", "utilities", "transport", "other"
        };

        public ClerkDashboardViewModel(
            IParcelService parcelService,
            IUserService userService,
            HttpClient httpClient,
            ILogger<ClerkDashboardViewModel> logger)
        {
            _parcelService = parcelService;
            _userService = userService;
            _httpClient = httpClient;
            _logger = logger;
            _logger.LogInformation("ClerkDashboardViewModel initialized");
            UserName = "User";
            UserRole = "";
            CanDispatch = false;
        }

        [RelayCommand]
        private async Task NavigateToDelivery()
        {
            _logger.LogInformation("Navigate to delivery view");
            await Shell.Current.GoToAsync(nameof(DeliveryView));
        }

        [RelayCommand]
        private async Task NavigateToViewAllParcels()
        {
            try
            {
                _logger.LogInformation("Navigate to view all parcels");
                await Shell.Current.GoToAsync(nameof(AllParcelsView));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to AllParcelsView");
                Debug.WriteLine($"Error navigating to AllParcelsView: {ex.Message}\n{ex.StackTrace}");
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to open parcels view: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task NavigateToAddParcel()
        {
            _logger.LogInformation("Navigate to collect parcel");
            await Shell.Current.GoToAsync(nameof(ParcelsView));
        }

        [RelayCommand]
        private async Task GenerateReport()
        {
            _logger.LogInformation("Navigate to clerk report view");
            await Shell.Current.GoToAsync(nameof(ClerkReportView));
        }

        [RelayCommand]
        private async Task NavigateToParcelConfirmation()
        {
            _logger.LogInformation("Navigate to parcel confirmation");
            await Shell.Current.GoToAsync("ParcelConfirmationView");
        }

        [RelayCommand]
        private async Task NavigateToDispatch()
        {
            _logger.LogInformation("Navigate to dispatch view");
            await Shell.Current.GoToAsync(nameof(DispatchView));
        }

        [RelayCommand]
        private async Task RefreshLockStatus()
        {
            var userId = Preferences.Get("CurrentUserId", 0);
            if (userId == 0) { IsRefreshingLock = false; return; }

            try
            {
                var lockStatus = await _parcelService.GetClerkLockStatusAsync(userId, DateTime.Today);
                ApplyLockStatus(lockStatus);

                if (!IsLocked)
                {
                    var today = DateTime.Today;
                    AmountOwed = await _parcelService.GetAmountOwedByUserAsync(userId, today);
                    var clerkSummary = await _parcelService.GetClerkCashInSummaryAsync(userId, today);
                    CashIn = clerkSummary != null ? (double)clerkSummary.RemainingDebt : 0.0;
                    DailySales = await _parcelService.GetDailySalesByUserAsync(userId, today);
                    MonthlySales = await _parcelService.GetMonthlySalesByUserAsync(userId, today);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing lock status");
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
            var userId = Preferences.Get("CurrentUserId", 0);
            var depositDate = _declarationDate != default ? _declarationDate.Date : DateTime.Today.AddDays(-1);

            var token = Preferences.Get("AuthToken", string.Empty);
            if (!string.IsNullOrEmpty(token))
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            IsSubmitting = true;
            try
            {
                await _httpClient.AddSessionHeaderAsync();

                decimal expenseAmount = 0;
                if (!string.IsNullOrWhiteSpace(ExpenseAmountInput))
                    decimal.TryParse(ExpenseAmountInput, NumberStyles.Any, CultureInfo.InvariantCulture, out expenseAmount);

                if (expenseAmount < 0)
                {
                    SubmitError = "Expense amount cannot be negative.";
                    return;
                }

                var hasDeposit = !DepositApproved && !string.IsNullOrWhiteSpace(DepositAmountInput);
                var hasExpenseOnly = expenseAmount > 0 && string.IsNullOrWhiteSpace(DepositAmountInput);

                if (!DepositApproved && string.IsNullOrWhiteSpace(DepositAmountInput) && expenseAmount <= 0)
                {
                    SubmitError = "Please enter a deposit amount and/or expense amount to declare.";
                    return;
                }

                if (hasDeposit)
                {
                    if (!decimal.TryParse(DepositAmountInput, NumberStyles.Any, CultureInfo.InvariantCulture, out var depositAmount) || depositAmount <= 0)
                    {
                        SubmitError = "Please enter a valid deposit amount.";
                        return;
                    }

                    // Delete rejected deposit before resubmitting
                    if (ExistingDepositId.HasValue && DepositRejected)
                        await _httpClient.DeleteAsync($"/api/Expenses/{ExistingDepositId}");

                    var depositPayload = new
                    {
                        category = "clerk_deposit",
                        description = $"Clerk deposit declaration for {depositDate:dd MMM yyyy}",
                        amount = depositAmount,
                        date = depositDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                        clerkId = userId,
                        clerkName = UserName
                    };
                    var depositJson = JsonSerializer.Serialize(depositPayload);
                    var depositContent = new StringContent(depositJson, Encoding.UTF8, "application/json");
                    var depositResp = await _httpClient.PostAsync("/api/Expenses", depositContent);
                    if (!depositResp.IsSuccessStatusCode)
                    {
                        var err = await depositResp.Content.ReadAsStringAsync();
                        _logger.LogError("clerk_deposit POST failed {Status}: {Body}", depositResp.StatusCode, err);
                        SubmitError = "Deposit submission failed. Please try again.";
                        return;
                    }
                }

                if (expenseAmount > 0)
                {
                    if (ExistingExpenseId.HasValue && ExpenseRejected)
                        await _httpClient.DeleteAsync($"/api/Expenses/{ExistingExpenseId}");

                    var description = string.IsNullOrWhiteSpace(ExpenseDescription) ? "Delivery expenses" : ExpenseDescription;
                    var expensePayload = new
                    {
                        category = ExpenseCategory,
                        description,
                        amount = expenseAmount,
                        date = depositDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                        clerkId = userId,
                        clerkName = UserName
                    };
                    var expJson = JsonSerializer.Serialize(expensePayload);
                    var expContent = new StringContent(expJson, Encoding.UTF8, "application/json");
                    var expResp = await _httpClient.PostAsync("/api/Expenses", expContent);
                    if (!expResp.IsSuccessStatusCode)
                    {
                        SubmitError = "Deposit saved but expense submission failed. Please try again.";
                        return;
                    }
                }

                DepositAmountInput = "";
                ExpenseAmountInput = "";
                ExpenseCategory = "other";
                ExpenseDescription = "";

                var userId2 = Preferences.Get("CurrentUserId", 0);
                var lockStatus = await _parcelService.GetClerkLockStatusAsync(userId2, DateTime.Today);
                ApplyLockStatus(lockStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting clerk declaration");
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

                var today = DateTime.Today;
                var currentUserId = Preferences.Get("CurrentUserId", 0);
                if (currentUserId == 0)
                {
                    _logger.LogWarning("No current user ID found for dashboard metrics");
                    return;
                }

                var lockStatus = await _parcelService.GetClerkLockStatusAsync(currentUserId, today);
                ApplyLockStatus(lockStatus);

                if (!IsLocked)
                {
                    AmountOwed = await _parcelService.GetAmountOwedByUserAsync(currentUserId, today);
                    var clerkSummary = await _parcelService.GetClerkCashInSummaryAsync(currentUserId, today);
                    CashIn = clerkSummary != null ? (double)clerkSummary.RemainingDebt : 0.0;
                    DailySales = await _parcelService.GetDailySalesByUserAsync(currentUserId, today);
                    MonthlySales = await _parcelService.GetMonthlySalesByUserAsync(currentUserId, today);
                }

                var username = Preferences.Get("CurrentUsername", string.Empty);
                if (!string.IsNullOrEmpty(username))
                {
                    var user = await _userService.GetUserByUsernameAsync(username);
                    if (user != null)
                    {
                        UserName = !string.IsNullOrWhiteSpace(user.FirstName) ? user.FirstName : user.Username;
                        UserRole = user.Role?.Name ?? "User";
                        CanDispatch = user.CanDispatch || (user.Role?.CanDispatch ?? false);
                    }
                }

                _logger.LogInformation($"Dashboard loaded - Locked: {IsLocked}, Reason: {LockReason}, PaidTotal: {LockPaidTotal:N2}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard data");
                Debug.WriteLine($"Error loading dashboard data: {ex.Message}");
                if (string.IsNullOrWhiteSpace(UserName) || UserName == "User")
                    UserName = "User";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyLockStatus(ClerkLockStatusDto lockStatus)
        {
            LockPaidTotal = lockStatus.YesterdayPaidTotal;
            _declarationDate = lockStatus.PreviousDate;
            LockPreviousDate = lockStatus.PreviousDate.ToString("dd MMM yyyy");

            ExistingDepositId = lockStatus.DepositRecordId;
            DepositRejected = lockStatus.DepositStatus == "rejected";
            DepositApproved = lockStatus.DepositStatus == "approved";
            DepositRejectionReason = lockStatus.DepositRejectionReason ?? "";
            ApprovedDepositAmount = DepositApproved ? (lockStatus.DepositAmount ?? 0m) : 0m;

            ExistingExpenseId = lockStatus.ExpenseRecordId;
            ExpenseRejected = lockStatus.ExpenseStatus == "rejected";
            ExpenseApproved = lockStatus.TotalApprovedExpenses > 0;
            ExpenseRejectionReason = lockStatus.ExpenseRejectionReason ?? "";
            ApprovedExpenseAmount = lockStatus.TotalApprovedExpenses;

            if (DepositRejected)
            {
                DepositAmountInput = (lockStatus.DepositAmount ?? 0m).ToString(CultureInfo.InvariantCulture);
                ExpenseAmountInput = (lockStatus.ExpenseAmount ?? 0m).ToString(CultureInfo.InvariantCulture);
            }

            var isHardLocked = lockStatus.Reason == "pending_submission" || lockStatus.Reason == "rejected";
            IsLocked = isHardLocked;
            LockReason = lockStatus.Reason;
            AppShell.IsClerkLocked = isHardLocked;
            ShowPendingApprovalBanner = lockStatus.Reason == "pending_approval";
        }
    }
}
