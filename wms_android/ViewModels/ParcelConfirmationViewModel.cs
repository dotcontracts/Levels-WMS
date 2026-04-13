using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using wms_android.shared.Models;
using wms_android.shared.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;

namespace wms_android.ViewModels
{
    public partial class ParcelConfirmationViewModel : ObservableObject
    {
        private readonly IParcelService _parcelService;
        private readonly IUserService _userService;
        private readonly ILogger<ParcelConfirmationViewModel> _logger;
        private IEnumerable<Parcel> _todaysParcels = new List<Parcel>();
        private readonly List<ParcelConfirmationItem> _allConfirmationItems = new();

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private DateTime _todayDate = DateTime.Today;

        [ObservableProperty]
        private ObservableCollection<User> _managerUsers = new();

        [ObservableProperty]
        private User? _selectedManager;

        [ObservableProperty]
        private ObservableCollection<string> _destinations = new();

        [ObservableProperty]
        private string _selectedDestination = "All";

        [ObservableProperty]
        private ObservableCollection<ParcelConfirmationItem> _filteredParcels = new();

        [ObservableProperty]
        private bool _hasConfirmedParcels;

        // Password verification properties
        [ObservableProperty]
        private string _confirmerPassword = "";

        [ObservableProperty]
        private bool _isConfirmerSelected;

        [ObservableProperty]
        private bool _isConfirmerVerified;

        [ObservableProperty]
        private bool _showVerifyButton = true;

        [ObservableProperty]
        private bool _showVerificationStatus;

        [ObservableProperty]
        private string _verificationStatusText = "";

        [ObservableProperty]
        private Color _verificationStatusColor = Colors.Red;

        [ObservableProperty]
        private string _passwordPromptText = "";

        public ParcelConfirmationViewModel(
            IParcelService parcelService,
            IUserService userService,
            ILogger<ParcelConfirmationViewModel> logger)
        {
            _parcelService = parcelService;
            _userService = userService;
            _logger = logger;
        }

        // Helper method to get user display name
        private string GetUserDisplayName(User user)
        {
            return !string.IsNullOrEmpty(user.FirstName) && !string.IsNullOrEmpty(user.LastName) 
                ? $"{user.FirstName} {user.LastName}" 
                : user.Username;
        }

        public async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                _logger.LogInformation("Loading parcel confirmation data");

                var currentUserId = Preferences.Get("CurrentUserId", 0);
                if (currentUserId == 0)
                {
                    _logger.LogWarning("No current user ID found");
                    await Application.Current.MainPage.DisplayAlert("Error", "User not logged in", "OK");
                    return;
                }

                // Load manager users
                await LoadManagerUsers();

                // Load today's parcels for the current user
                await LoadTodaysParcels(currentUserId);

                // Load destinations from parcels
                LoadDestinations();

                // Apply initial filter
                ApplyDestinationFilter();

                _logger.LogInformation($"Loaded {_todaysParcels.Count()} parcels for confirmation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading parcel confirmation data");
                await Application.Current.MainPage.DisplayAlert("Error", "Failed to load data", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadManagerUsers()
        {
            try
            {
                
                // Get current user's branch information
                var currentUserId = Preferences.Get("CurrentUserId", 0);
                if (currentUserId == 0)
                {
                    await Application.Current.MainPage.DisplayAlert("Debug", "No current user ID found", "OK");
                    _logger.LogWarning("No current user ID found for branch filtering");
                    return;
                }
                
                var currentUser = await _userService.GetUserByIdAsync(currentUserId);
                if (currentUser == null)
                {
                    _logger.LogWarning($"Current user {currentUserId} not found");
                    return;
                }
                
                // Handle users without branch assignment - show all managers/admins
                int? currentUserBranchId = currentUser.BranchId;
                if (currentUserBranchId == null)
                {
                    _logger.LogWarning($"Current user {currentUserId} has no branch assigned - showing all managers/admins");
                }
                else
                {
                    _logger.LogInformation($"Current user: {currentUser.Username}, filtering by branch: {currentUserBranchId}");
                }
                _logger.LogInformation($"Current user: {currentUser.Username} (ID: {currentUserId}, Branch: {currentUserBranchId})");
                _logger.LogInformation($"Filtering confirmers for branch ID: {currentUserBranchId}");
                _logger.LogInformation("Loading manager users (matching Django implementation)...");
                
                
                // Alternative approach: Get all users and filter by role locally
                _logger.LogInformation("Getting all users and filtering by role...");
                var managers = new List<User>();
                var adminUsers = new List<User>();
                
                try
                {
                    var allSystemUsers = await _userService.GetAllUsersAsync();
                    
                    if (allSystemUsers != null)
                    {
                        _logger.LogInformation($"Got {allSystemUsers.Count()} total users from GetAllUsersAsync");
                        
                        // Filter for managers and admins
                        foreach (var user in allSystemUsers)
                        {
                            var roleName = user.Role?.Name?.ToLower();
                            if (roleName == "manager") managers.Add(user);
                            if (roleName == "admin") adminUsers.Add(user);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("GetAllUsersAsync returned null");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "GetAllUsersAsync failed");
                }
                
                // Use the managers and adminUsers we already filtered from allSystemUsers
                var confirmers = managers.Concat(adminUsers).ToList();
                _logger.LogInformation($"Total confirmers from filtered users: {confirmers?.Count() ?? 0}");
                
                // Summary logging
                _logger.LogInformation($"SUMMARY: Found {confirmers.Count} valid confirmers (managers/admins in branch {currentUserBranchId})");
                
                _logger.LogInformation($"About to add {confirmers.Count} confirmers to ManagerUsers dropdown");
                
                // Update UI on main thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ManagerUsers.Clear();
                    
                    if (confirmers.Any())
                    {
                        foreach (var confirmer in confirmers)
                        {
                            ManagerUsers.Add(confirmer);
                        }
                        _logger.LogInformation($"Loaded {confirmers.Count} confirmers (managers + admins)");
                    }
                    else
                    {
                        _logger.LogWarning("No managers or admins found in database - check your user roles");
                    }
                });

                _logger.LogInformation($"Total confirmers loaded: {ManagerUsers.Count}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading manager users");
                await Application.Current.MainPage.DisplayAlert("Error", 
                    $"Failed to load confirmers from server. Please check your connection and try again.\n\nError: {ex.Message}", "OK");
            }
        }

        private async Task LoadTodaysParcels(int currentUserId)
        {
            try
            {
                var allUserParcels = await _parcelService.GetParcelsByUserAsync(currentUserId);
                _todaysParcels = allUserParcels.Where(p => p.CreatedAt.Date == DateTime.Today && 
                                                          p.Status == ParcelStatus.Pending).ToList();

                // Create confirmation items for each parcel
                _allConfirmationItems.Clear();
                foreach (var parcel in _todaysParcels)
                {
                    _allConfirmationItems.Add(new ParcelConfirmationItem
                    {
                        Parcel = parcel,
                        IsConfirmed = false
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading today's parcels");
                throw;
            }
        }

        private void LoadDestinations()
        {
            try
            {
                _logger.LogInformation($"Loading destinations from {_todaysParcels.Count()} parcels");
                
                var destinations = _todaysParcels
                    .Where(p => !string.IsNullOrEmpty(p.Destination))
                    .Select(p => p.Destination)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToList();

                _logger.LogInformation($"Found {destinations.Count} unique destinations from parcels");

                Destinations.Clear();
                Destinations.Add("All");
                
                // Add destinations from parcels
                foreach (var destination in destinations)
                {
                    Destinations.Add(destination);
                    _logger.LogInformation($"Added destination from parcel: {destination}");
                }
                
                // Add fallback destinations if none found from parcels
                if (destinations.Count == 0)
                {
                    var fallbackDestinations = new[] { "Eldoret", "Kapsabet", "Kakamega", "Kericho", "Kisumu", "Kitale", "Mombasa", "Nairobi", "Nakuru" };
                    foreach (var destination in fallbackDestinations)
                    {
                        Destinations.Add(destination);
                        _logger.LogInformation($"Added fallback destination: {destination}");
                    }
                }
                
                _logger.LogInformation($"Total destinations loaded: {Destinations.Count}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading destinations");
                // Add fallback destinations on error
                Destinations.Clear();
                Destinations.Add("All");
                var fallbackDestinations = new[] { "Eldoret", "Kapsabet", "Kakamega", "Kericho", "Kisumu", "Kitale", "Mombasa", "Nairobi", "Nakuru" };
                foreach (var destination in fallbackDestinations)
                {
                    Destinations.Add(destination);
                }
            }
        }

        [RelayCommand]
        private void ApplyDestinationFilter()
        {
            try
            {
                var filtered = _allConfirmationItems.AsEnumerable();

                if (SelectedDestination != "All")
                {
                    filtered = filtered.Where(item => item.Parcel.Destination == SelectedDestination);
                }

                FilteredParcels.Clear();
                foreach (var item in filtered.OrderByDescending(item => item.Parcel.CreatedAt))
                {
                    FilteredParcels.Add(item);
                }

                UpdateHasConfirmedParcels();
                _logger.LogInformation($"Applied destination filter: {FilteredParcels.Count} parcels match criteria");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying destination filter");
            }
        }

        [RelayCommand]
        private async Task ToggleParcelConfirmation(ParcelConfirmationItem confirmationItem)
        {
            try
            {
                if (SelectedManager == null)
                {
                    await Application.Current.MainPage.DisplayAlert("Security Required", "Please select a confirmer (manager) first", "OK");
                    return;
                }

                if (!IsConfirmerVerified)
                {
                    await Application.Current.MainPage.DisplayAlert("Security Required", 
                        $"Please verify {GetUserDisplayName(SelectedManager)}'s password before confirming parcels.\n\nThis ensures only authorized personnel can confirm parcels.", "OK");
                    return;
                }

                confirmationItem.IsConfirmed = !confirmationItem.IsConfirmed;
                confirmationItem.ConfirmedBy = confirmationItem.IsConfirmed ? SelectedManager : null;
                
                UpdateHasConfirmedParcels();
                
                var action = confirmationItem.IsConfirmed ? "confirmed" : "unconfirmed";
                _logger.LogInformation($"Parcel {confirmationItem.Parcel.WaybillNumber} {action} by verified confirmer {SelectedManager.Username}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling parcel confirmation");
                await Application.Current.MainPage.DisplayAlert("Error", "Failed to update confirmation status", "OK");
            }
        }

        [RelayCommand]
        private async Task FinalizeConfirmedParcels()
        {
            try
            {
                if (!IsConfirmerVerified || SelectedManager == null)
                {
                    await Application.Current.MainPage.DisplayAlert("Security Required", 
                        "A verified confirmer is required to finalize parcels. Please select and verify a confirmer first.", "OK");
                    return;
                }

                // Check if the selected manager has permission to confirm parcels
                if (SelectedManager?.Role?.CanConfirm != true)
                {
                    await Application.Current.MainPage.DisplayAlert("Access Denied", 
                        $"User '{SelectedManager?.Username}' doesn't have permission to confirm parcels. Please select a user with confirmation permissions.", "OK");
                    return;
                }

                var confirmedItems = _allConfirmationItems.Where(item => item.IsConfirmed).ToList();
                
                if (!confirmedItems.Any())
                {
                    await Application.Current.MainPage.DisplayAlert("Info", "No parcels have been confirmed yet", "OK");
                    return;
                }

                bool confirm = await Application.Current.MainPage.DisplayAlert(
                    "Confirm Finalization", 
                    $"Are you sure you want to finalize {confirmedItems.Count} confirmed parcels?\n\nConfirmed by: {GetUserDisplayName(SelectedManager)}\nThis action cannot be undone.", 
                    "Yes", 
                    "No");

                if (!confirm) return;

                IsLoading = true;

                // Update parcel status to Confirmed
                foreach (var item in confirmedItems)
                {
                    item.Parcel.Status = ParcelStatus.Confirmed;
                    await _parcelService.UpdateParcelAsync(item.Parcel);
                }

                await Application.Current.MainPage.DisplayAlert("Success", 
                    $"{confirmedItems.Count} parcels have been confirmed successfully by {GetUserDisplayName(SelectedManager)}", "OK");

                // Reload data to refresh the view
                await LoadDataAsync();

                _logger.LogInformation($"Confirmed {confirmedItems.Count} confirmed parcels by verified confirmer {SelectedManager.Username}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finalizing confirmed parcels");
                await Application.Current.MainPage.DisplayAlert("Error", "Failed to finalize parcels", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UpdateHasConfirmedParcels()
        {
            HasConfirmedParcels = _allConfirmationItems.Any(item => item.IsConfirmed);
        }

        partial void OnSelectedDestinationChanged(string value) => ApplyDestinationFilter();

        partial void OnSelectedManagerChanged(User? value)
        {
            IsConfirmerSelected = value != null;
            IsConfirmerVerified = false;
            ConfirmerPassword = "";
            ShowVerifyButton = value != null;
            ShowVerificationStatus = false;
            VerificationStatusText = "";
            
            if (value != null)
            {
                PasswordPromptText = $"Enter password for {GetUserDisplayName(value)}:";
            }
            else
            {
                PasswordPromptText = "";
            }
        }

        [RelayCommand]
        private async Task VerifyConfirmer()
        {
            if (SelectedManager == null)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "No confirmer selected", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(ConfirmerPassword))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Please enter the confirmer's password", "OK");
                return;
            }

            try
            {
                IsLoading = true;
                _logger.LogInformation($"Verifying password for confirmer: {SelectedManager.Username}");

                // Authenticate the confirmer using their username and entered password
                var authenticatedUser = await _userService.AuthenticateAsync(SelectedManager.Username, ConfirmerPassword);

                if (authenticatedUser != null)
                {
                    IsConfirmerVerified = true;
                    ShowVerifyButton = false;
                    ShowVerificationStatus = true;
                    VerificationStatusText = $"✓ {GetUserDisplayName(SelectedManager)} verified successfully";
                    VerificationStatusColor = Colors.Green;
                    
                    _logger.LogInformation($"Confirmer {SelectedManager.Username} verified successfully");
                    await Application.Current.MainPage.DisplayAlert("Success", 
                        $"{GetUserDisplayName(SelectedManager)} has been verified successfully. You can now confirm parcels.", "OK");
                }
                else
                {
                    IsConfirmerVerified = false;
                    ShowVerificationStatus = true;
                    VerificationStatusText = "✗ Invalid password. Please try again.";
                    VerificationStatusColor = Colors.Red;
                    ConfirmerPassword = ""; // Clear the password field
                    
                    _logger.LogWarning($"Failed to verify confirmer {SelectedManager.Username}");
                    await Application.Current.MainPage.DisplayAlert("Verification Failed", 
                        "Invalid password. Please check the password and try again.", "OK");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during confirmer verification");
                IsConfirmerVerified = false;
                ShowVerificationStatus = true;
                VerificationStatusText = "✗ Verification failed. Please try again.";
                VerificationStatusColor = Colors.Red;
                
                await Application.Current.MainPage.DisplayAlert("Error", 
                    $"Failed to verify confirmer. Please check your connection and try again.\n\nError: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    public partial class ParcelConfirmationItem : ObservableObject
    {
        [ObservableProperty]
        private Parcel _parcel;

        [ObservableProperty]
        private bool _isConfirmed;

        [ObservableProperty]
        private User? _confirmedBy;

        public string ConfirmationStatus => IsConfirmed ? "✓ Confirmed" : "Pending";
        
        public string ConfirmButtonText => IsConfirmed ? "Unconfirm" : "Confirm";
        
        public Color ConfirmButtonColor => IsConfirmed ? 
            Color.FromArgb("#FF6B6B") : // Red for unconfirm
            Color.FromArgb("#51CF66"); // Green for confirm

        partial void OnIsConfirmedChanged(bool value)
        {
            OnPropertyChanged(nameof(ConfirmationStatus));
            OnPropertyChanged(nameof(ConfirmButtonText));
            OnPropertyChanged(nameof(ConfirmButtonColor));
        }
    }

}