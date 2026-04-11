using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;
using wms_android.shared.Models;
using wms_android.Views;
using wms_android.shared.Interfaces;
using Microsoft.Maui.Storage;

namespace wms_android.ViewModels
{
    public partial class ClerkDashboardViewModel : ObservableObject
    {
        private readonly IParcelService _parcelService;
        private readonly IUserService _userService;
        private readonly ILogger<ClerkDashboardViewModel> _logger;
        public INavigation Navigation { get; set; }

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private double _amountOwed;

        [ObservableProperty]
        private double _cashIn; // Outstanding debit from clerk summary for current month

        [ObservableProperty]
        private double _dailySales;

        [ObservableProperty]
        private double _monthlySales;

        [ObservableProperty]
        private string _userName;

        [ObservableProperty]
        private string _userRole;

        [ObservableProperty]
        private bool _isManagerOrAdmin;
        
        public ClerkDashboardViewModel(
            IParcelService parcelService, 
            IUserService userService,
            ILogger<ClerkDashboardViewModel> logger)
        {
            _parcelService = parcelService;
            _userService = userService;
            _logger = logger;
            _logger.LogInformation("ClerkDashboardViewModel initialized");
            
            // Default values - show a more friendly loading message
            UserName = "User";
            UserRole = "";
            // Temporarily set to true for testing - will be overridden when user data loads
            IsManagerOrAdmin = true;
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
            _logger.LogInformation("Navigate to view all parcels");
            await Shell.Current.GoToAsync(nameof(AllParcelsView));
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
            _logger.LogInformation("Generate report");
            await Shell.Current.GoToAsync(nameof(ReportView));
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

        public async Task LoadDashboardData()
        {
            try
            {
                IsLoading = true;
                
                // Get today's date
                var today = DateTime.Today;
                
                // Get current user ID
                var currentUserId = Preferences.Get("CurrentUserId", 0);
                if (currentUserId == 0)
                {
                    _logger.LogWarning("No current user ID found for dashboard metrics");
                    return;
                }
                
                // Get user-specific metrics for today
                AmountOwed = await _parcelService.GetAmountOwedByUserAsync(currentUserId, today);
                
                // Get outstanding debit from clerk summary for current month + 7 days of next month
                _logger.LogInformation($"Fetching clerk summary for user {currentUserId} on date {today:yyyy-MM-dd}");
                var clerkSummary = await _parcelService.GetClerkCashInSummaryAsync(currentUserId, today);
                
                if (clerkSummary != null)
                {
                    CashIn = (double)clerkSummary.RemainingDebt;
                    _logger.LogInformation($"Clerk summary found - RemainingDebt: {clerkSummary.RemainingDebt}, TotalPaidAmount: {clerkSummary.TotalPaidAmount}, ParcelCount: {clerkSummary.ParcelCount}");
                }
                else
                {
                    CashIn = 0.0;
                    _logger.LogWarning($"No clerk summary found for user {currentUserId}");
                }
                
                DailySales = await _parcelService.GetDailySalesByUserAsync(currentUserId, today);
                MonthlySales = await _parcelService.GetMonthlySalesByUserAsync(currentUserId, today);
                
                // Try to get current user info from preferences or cache
                var username = Preferences.Get("CurrentUsername", string.Empty);
                _logger.LogInformation($"Retrieved username from preferences: '{username}'");
                Debug.WriteLine($"[ClerkDashboard] Retrieved username from preferences: '{username}'");
                
                if (!string.IsNullOrEmpty(username))
                {
                    var user = await _userService.GetUserByUsernameAsync(username);
                    _logger.LogInformation($"User service returned: {(user != null ? $"User found - ID: {user.Id}, Username: {user.Username}, Role: {user.Role?.Name}" : "null")}");
                    Debug.WriteLine($"[ClerkDashboard] User service returned: {(user != null ? $"User found - ID: {user.Id}, Username: {user.Username}, Role: {user.Role?.Name}" : "null")}");
                    
                    if (user != null)
                    {
                        // Use FirstName if available, fallback to Username if FirstName is null/empty
                        UserName = !string.IsNullOrWhiteSpace(user.FirstName) ? user.FirstName : user.Username;
                        UserRole = user.Role?.Name ?? "User";
                        
                        // Set manager/admin visibility based on role
                        var roleName = user.Role?.Name ?? "Unknown";
                        Debug.WriteLine($"[ClerkDashboard] Role name: '{roleName}', RoleId: {user.Role?.Id}");
                        IsManagerOrAdmin = IsUserManagerOrAdmin(roleName);
                        _logger.LogInformation($"User role: '{roleName}', IsManagerOrAdmin: {IsManagerOrAdmin}");
                        Debug.WriteLine($"[ClerkDashboard] User role: '{roleName}', IsManagerOrAdmin: {IsManagerOrAdmin}");
                    }
                    else
                    {
                        Debug.WriteLine($"[ClerkDashboard] User not found for username: '{username}'");
                        _logger.LogWarning($"User not found for username: '{username}'");
                    }
                }
                else
                {
                    Debug.WriteLine("[ClerkDashboard] No username found in preferences");
                    _logger.LogWarning("No username found in preferences");
                }
                
                _logger.LogInformation($"Dashboard data loaded for user {currentUserId}: Amount Owed: Ksh {AmountOwed:N2}, Outstanding Debit: Ksh {CashIn:N2}, Daily Sales: Ksh {DailySales:N2}, Monthly Sales: Ksh {MonthlySales:N2}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard data");
                Debug.WriteLine($"Error loading dashboard data: {ex.Message}");
                
                // Ensure we have a fallback name even if there's an error
                if (string.IsNullOrWhiteSpace(UserName) || UserName == "User")
                {
                    UserName = "User";
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool IsUserManagerOrAdmin(string roleName)
        {
            Debug.WriteLine($"[ClerkDashboard] IsUserManagerOrAdmin called with roleName: '{roleName}'");
            
            if (string.IsNullOrWhiteSpace(roleName))
            {
                _logger.LogInformation("Role name is null or empty, denying access");
                Debug.WriteLine("[ClerkDashboard] Role name is null or empty, denying access");
                return false;
            }
                
            var role = roleName.ToLowerInvariant();
            Debug.WriteLine($"[ClerkDashboard] Role converted to lowercase: '{role}'");
            
            var isManagerOrAdmin = role.Contains("manager") || 
                                  role.Contains("admin") || 
                                  role.Contains("supervisor") ||
                                  role == "manager" ||
                                  role == "administrator" ||
                                  role == "admin";
            
            Debug.WriteLine($"[ClerkDashboard] Role checks:");
            Debug.WriteLine($"  - contains 'manager': {role.Contains("manager")}");
            Debug.WriteLine($"  - contains 'admin': {role.Contains("admin")}");
            Debug.WriteLine($"  - contains 'supervisor': {role.Contains("supervisor")}");
            Debug.WriteLine($"  - equals 'manager': {role == "manager"}");
            Debug.WriteLine($"  - equals 'administrator': {role == "administrator"}");
            Debug.WriteLine($"  - equals 'admin': {role == "admin"}");
            Debug.WriteLine($"  - Final result: {isManagerOrAdmin}");
                                  
            _logger.LogInformation($"Role check for '{roleName}': {isManagerOrAdmin}");
            return isManagerOrAdmin;
        }
    }
} 