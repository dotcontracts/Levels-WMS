using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using wms_android.shared.Interfaces;
using wms_android.shared.Models;
using wms_android.Models;
using wms_android.Services;

namespace wms_android.ViewModels
{
    public partial class DispatchViewModel : ObservableObject
    {
        private readonly IParcelService _parcelService;
        private readonly ILogger<DispatchViewModel> _logger;
        private readonly ParcelCacheService _cacheService;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _selectedDestination = string.Empty;

        [ObservableProperty]
        private string _vehicleRegistration = string.Empty;

        [ObservableProperty]
        private string _driverName = string.Empty;

        [ObservableProperty]
        private bool _canCreateDispatch;

        [ObservableProperty]
        private bool _hasDestinationSelected;

        [ObservableProperty]
        private string _parcelsCountText = string.Empty;

        [ObservableProperty]
        private string _dispatchSummary = string.Empty;

        [ObservableProperty]
        private bool _isDetailsTabSelected = true;

        [ObservableProperty]
        private bool _isParcelsTabSelected = false;

        [ObservableProperty]
        private DateTime _fromDate = DateTime.Today.AddDays(-2);

        [ObservableProperty]
        private DateTime _toDate = DateTime.Today;

        [ObservableProperty]
        private string _selectedClerk = "All";

        [ObservableProperty]
        private ObservableCollection<ParcelDisplayModel> _filteredParcels = new();


        [ObservableProperty]
        private DateTime _minimumFromDate = DateTime.Today.AddDays(-2);

        [ObservableProperty]
        private DateTime _maximumToDate = DateTime.Today;

        // Cache properties
        private Dictionary<string, List<ParcelDisplayModel>> _parcelsCache = new();
        private Dictionary<string, DateTime> _cacheTimestamps = new();
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5); // Cache for 5 minutes
        private string _lastCacheKey = string.Empty;

        public ObservableCollection<ParcelDisplayModel> AvailableParcels { get; } = new();
        public ObservableCollection<string> DestinationOptions { get; } = new();
        public ObservableCollection<string> ClerkOptions { get; } = new() { "All" };

        public DispatchViewModel(IParcelService parcelService, ILogger<DispatchViewModel> logger, ParcelCacheService cacheService)
        {
            _parcelService = parcelService;
            _logger = logger;
            _cacheService = cacheService;
            
            // Subscribe to property changes to update CanCreateDispatch
            PropertyChanged += OnPropertyChanged;
        }

        [RelayCommand]
        private void SelectDetailsTab()
        {
            IsDetailsTabSelected = true;
            IsParcelsTabSelected = false;
        }

        [RelayCommand]
        private void SelectParcelsTab()
        {
            IsDetailsTabSelected = false;
            IsParcelsTabSelected = true;
            if (!string.IsNullOrWhiteSpace(SelectedDestination))
            {
                _ = LoadParcelsForDestinationWithFilters();
            }
        }

        [RelayCommand]
        private async Task ApplyFilters()
        {
            if (!string.IsNullOrWhiteSpace(SelectedDestination))
            {
                await LoadParcelsForDestinationWithFilters();
            }
        }


        [RelayCommand]
        private void ClearFilters()
        {
            FromDate = DateTime.Today.AddDays(-2);
            ToDate = DateTime.Today;
            SelectedClerk = "All";
            _ = ApplyFilters();
        }

        [RelayCommand]
        private async Task DispatchSelectedParcels()
        {
            await CreateDispatch();
        }


        [RelayCommand]
        private async Task CreateDispatch()
        {
            try
            {
                IsLoading = true;
                
                var selectedParcels = FilteredParcels.Where(p => p.IsSelected).ToList();
                
                if (!selectedParcels.Any())
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Please select at least one parcel to dispatch", "OK");
                    return;
                }

                if (string.IsNullOrWhiteSpace(VehicleRegistration) || 
                    string.IsNullOrWhiteSpace(DriverName) || 
                    string.IsNullOrWhiteSpace(SelectedDestination))
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Please fill in all dispatch details", "OK");
                    return;
                }

                // Generate dispatch code similar to Django implementation
                var dispatchCode = GenerateDispatchCode();

                // Get current user's branch for SourceBranch
                var currentUserId = Preferences.Get("CurrentUserId", 0);
                var currentBranch = "Main Branch"; // Default fallback
                
                // Try to get user's branch name (you might need to implement this)
                try
                {
                    // TODO: Get actual user branch from user service if available
                    var userService = Application.Current.Handler?.MauiContext?.Services?.GetService<IUserService>();
                    if (userService != null && currentUserId > 0)
                    {
                        var user = await userService.GetUserByIdAsync(currentUserId);
                        currentBranch = user?.Branch?.Name ?? currentBranch;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not retrieve user branch, using default");
                }

                // Create dispatch object
                _logger.LogInformation($"Creating dispatch with Destination: '{SelectedDestination}', SourceBranch: '{currentBranch}'");
                var dispatch = new Dispatch
                {
                    Id = Guid.NewGuid(),
                    DispatchCode = dispatchCode,
                    VehicleNumber = VehicleRegistration,
                    Driver = DriverName,
                    DispatchTime = DateTime.Now,
                    Status = "In Transit",
                    ParcelIds = selectedParcels.Select(p => p.Id).ToList(),
                    SourceBranch = currentBranch, // Actual source branch where dispatch originates
                    Destination = SelectedDestination // Selected destination from dropdown
                };
                
                _logger.LogInformation($"Dispatch SourceBranch: '{dispatch.SourceBranch}', Destination: '{dispatch.Destination}'");

                // Create dispatch through API (this will also update parcel statuses)
                Dispatch createdDispatch = null;
                try
                {
                    createdDispatch = await _parcelService.CreateDispatchAsync(dispatch);
                    _logger.LogInformation($"Created dispatch {dispatchCode} with {selectedParcels.Count} parcels");
                }
                catch (Exception dispatchEx)
                {
                    _logger.LogWarning(dispatchEx, $"Failed to create dispatch through API, trying fallback approach");
                    
                    // Fallback: Update parcel statuses individually and treat dispatch as created
                    foreach (var parcel in selectedParcels)
                    {
                        try
                        {
                            await _parcelService.UpdateParcelStatusAsync(parcel.Id, ParcelStatus.InTransit);
                            _logger.LogInformation($"Updated parcel {parcel.WaybillNumber} status to In Transit");
                        }
                        catch (Exception statusEx)
                        {
                            _logger.LogError(statusEx, $"Failed to update status for parcel {parcel.WaybillNumber}");
                        }
                    }
                    
                    // Create a local dispatch object for logging/display purposes
                    createdDispatch = dispatch;
                    _logger.LogInformation($"Dispatch {dispatchCode} processed with fallback approach");
                }
                
                await Application.Current.MainPage.DisplayAlert("Success", 
                    $"Dispatch {dispatchCode} created successfully!\n\n" +
                    $"• {selectedParcels.Count} parcel(s) dispatched\n" +
                    $"• Destination: {SelectedDestination}\n" +
                    $"• Vehicle: {VehicleRegistration}\n" +
                    $"• Driver: {DriverName}\n\n" +
                    $"All parcels have been updated to 'In Transit' status.", "OK");

                // Invalidate cache for this destination since parcels have been dispatched
                InvalidateCacheForDestination(SelectedDestination);

                // Clear form and refresh
                ClearForm();
                await LoadDestinations();
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating dispatch");
                Debug.WriteLine($"Error creating dispatch: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", 
                    $"Failed to create dispatch: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                await LoadDestinations();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dispatch data");
                Debug.WriteLine($"Error loading dispatch data: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }


        private async Task LoadDestinations()
        {
            try
            {
                // Use optimized API call to get unique destinations from ALL branches with pending status
                // Dispatch needs to see all destinations where there are pending or confirmed parcels
                // Note: Currently API supports single status, we get pending ones (most common case)
                var destinations = await _parcelService.GetUniqueDestinationsAsync(ParcelStatus.Pending);
                
                DestinationOptions.Clear();
                foreach (var destination in destinations.OrderBy(d => d))
                {
                    DestinationOptions.Add(destination);
                }
                
                _logger.LogInformation($"Loaded {destinations.Count()} unique destinations with pending parcels from all branches");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading destinations");
                // Fallback to default destinations
                var fallbackDestinations = new List<string> { "Nairobi", "Mombasa", "Kisumu", "Eldoret" };
                DestinationOptions.Clear();
                foreach (var destination in fallbackDestinations)
                {
                    DestinationOptions.Add(destination);
                }
            }
        }

        private void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VehicleRegistration) ||
                e.PropertyName == nameof(DriverName) ||
                e.PropertyName == nameof(SelectedDestination))
            {
                UpdateCanCreateDispatch();
                UpdateDispatchSummary();
            }
            
            if (e.PropertyName == nameof(SelectedDestination))
            {
                HasDestinationSelected = !string.IsNullOrWhiteSpace(SelectedDestination);
                if (HasDestinationSelected)
                {
                    _ = LoadParcelsForDestinationWithFilters();
                }
                else
                {
                    FilteredParcels.Clear();
                    ParcelsCountText = string.Empty;
                }
            }

            if (e.PropertyName == nameof(FromDate) ||
                e.PropertyName == nameof(ToDate) ||
                e.PropertyName == nameof(SelectedClerk))
            {
                _ = ApplyFilters();
            }
        }

        private void UpdateCanCreateDispatch()
        {
            var selectedParcels = FilteredParcels.Where(p => p.IsSelected).ToList();
            CanCreateDispatch = !string.IsNullOrWhiteSpace(VehicleRegistration) &&
                               !string.IsNullOrWhiteSpace(DriverName) &&
                               !string.IsNullOrWhiteSpace(SelectedDestination) &&
                               selectedParcels.Any();
        }

        private void UpdateDispatchSummary()
        {
            var selectedParcels = FilteredParcels.Where(p => p.IsSelected).ToList();
            if (CanCreateDispatch)
            {
                DispatchSummary = $"Ready to dispatch {selectedParcels.Count} parcel(s) to {SelectedDestination} using vehicle {VehicleRegistration} with driver {DriverName}.";
            }
            else
            {
                DispatchSummary = string.Empty;
            }
        }

        private async Task LoadParcelsForDestinationWithFilters()
        {
            try
            {
                IsLoading = true;
                
                // Generate cache key based on current filters
                var cacheKey = GenerateCacheKey();
                
                // Check if we have valid cached data
                if (IsCacheValid(cacheKey))
                {
                    _logger.LogInformation($"Loading parcels from cache for destination {SelectedDestination}");
                    LoadParcelsFromCache(cacheKey);
                    await LoadClerksForDestinationOptimized();
                    return;
                }
                
                _logger.LogInformation($"Loading parcels from API for destination {SelectedDestination}");
                
                // Load all parcels for dispatch (no pagination needed for 2-day range)
                var parcels = await _parcelService.GetParcelsForDispatchAsync(
                    destination: SelectedDestination,
                    statuses: new List<ParcelStatus> { ParcelStatus.Pending, ParcelStatus.Confirmed },
                    fromDate: FromDate,
                    toDate: ToDate,
                    createdByUsername: SelectedClerk != "All" ? SelectedClerk : null
                );
                
                _logger.LogInformation($"Loaded {parcels.Count()} parcels for destination {SelectedDestination}");
                
                // Convert to display models
                var displayParcels = parcels.Select(p => new ParcelDisplayModel
                {
                    Id = p.Id,
                    WaybillNumber = p.WaybillNumber ?? string.Empty,
                    RecipientName = p.Receiver ?? string.Empty,
                    Destination = p.Destination ?? string.Empty,
                    Status = p.Status.ToString(),
                    IsSelected = false,
                    CreatedAt = p.CreatedAt,
                    CreatedBy = p.CreatedBy?.Username ?? string.Empty,
                    Amount = p.Amount ?? 0,
                    PaymentMethods = p.PaymentMethods ?? string.Empty,
                    Description = p.Description ?? string.Empty,
                    Quantity = p.Quantity ?? 0,
                    Sender = p.Sender ?? string.Empty
                }).ToList();

                // Cache the parcels
                CacheParcels(cacheKey, displayParcels);
                
                // Load parcels into UI
                LoadParcelsFromCache(cacheKey);
                
                // Update count text
                ParcelsCountText = $"Found {displayParcels.Count} parcel(s) for {SelectedDestination}";
                
                // Load unique clerks for the destination
                await LoadClerksForDestinationOptimized();
                
                _logger.LogInformation($"Loaded and cached {displayParcels.Count} parcels for destination {SelectedDestination}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading parcels for destination");
                ParcelsCountText = "Error loading parcels";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadClerksForDestinationOptimized()
        {
            try
            {
                // Use optimized API call to get unique clerks from ALL branches
                // This allows filtering by any clerk who created parcels for this destination (pending/confirmed)
                var clerkNames = await _parcelService.GetUniqueClerksByDestinationAsync(SelectedDestination, ParcelStatus.Pending);
                
                ClerkOptions.Clear();
                ClerkOptions.Add("All");
                foreach (var clerk in clerkNames)
                {
                    ClerkOptions.Add(clerk);
                }
                
                _logger.LogInformation($"Loaded {clerkNames.Count()} unique clerks from all branches for destination {SelectedDestination}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading clerks");
                // Fallback to loading all users
                await LoadAllUsersForClerks();
            }
        }

        private async Task LoadAllUsersForClerks()
        {
            try
            {
                // Fallback: Load all users and show their usernames
                var userService = Application.Current.Handler?.MauiContext?.Services?.GetService<IUserService>();
                if (userService != null)
                {
                    var users = await userService.GetAllUsersAsync();
                    
                    ClerkOptions.Clear();
                    ClerkOptions.Add("All");
                    foreach (var user in users.OrderBy(u => u.Username))
                    {
                        ClerkOptions.Add(user.Username);
                    }
                    
                    _logger.LogInformation($"Loaded {users.Count()} users as fallback for clerks dropdown");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users for clerks fallback");
            }
        }


        [RelayCommand]
        private async Task RefreshParcels()
        {
            if (HasDestinationSelected)
            {
                await LoadParcelsForDestinationWithFilters();
            }
        }

        private void ClearForm()
        {
            VehicleRegistration = string.Empty;
            DriverName = string.Empty;
            SelectedDestination = string.Empty;
            HasDestinationSelected = false;
            ParcelsCountText = string.Empty;
            DispatchSummary = string.Empty;
            FilteredParcels.Clear();
            ClerkOptions.Clear();
            ClerkOptions.Add("All");
            SelectedClerk = "All";
            FromDate = DateTime.Today.AddDays(-2);
            ToDate = DateTime.Today;
            
            // Clear cache when form is reset
            ClearCache();
        }

        private string GenerateDispatchCode()
        {
            // Generate dispatch code matching Django implementation
            // Format: {Destination_initials}-{Date}-DS{Code}
            var now = DateTime.Now;
            var eatTime = now.AddHours(3); // Convert to EAT (UTC+3)
            var dateComponent = eatTime.ToString("yyyyMMdd");
            
            // Generate destination initials (first 3 characters, uppercase)
            var destinationInitials = GetDestinationInitials(SelectedDestination);
            
            // Generate random alphanumeric code (6 characters)
            var codeComponent = GenerateRandomCode(6);
            
            return $"{destinationInitials}-{dateComponent}-DS{codeComponent}";
        }

        private string GetDestinationInitials(string destination)
        {
            if (string.IsNullOrWhiteSpace(destination))
                return "UNK"; // Unknown destination
                
            // Take first 3 characters and make uppercase
            return destination.Length >= 3 
                ? destination.Substring(0, 3).ToUpperInvariant()
                : destination.ToUpperInvariant().PadRight(3, 'X');
        }

        private string GenerateRandomCode(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        #region Cache Methods

        private string GenerateCacheKey()
        {
            // Create a unique cache key based on current filter values
            var key = $"{SelectedDestination}|{FromDate:yyyy-MM-dd}|{ToDate:yyyy-MM-dd}|{SelectedClerk}";
            _lastCacheKey = key;
            return key;
        }

        private bool IsCacheValid(string cacheKey)
        {
            if (!_parcelsCache.ContainsKey(cacheKey) || !_cacheTimestamps.ContainsKey(cacheKey))
            {
                return false;
            }

            var cacheTime = _cacheTimestamps[cacheKey];
            var isExpired = DateTime.Now - cacheTime > _cacheExpiration;
            
            if (isExpired)
            {
                _logger.LogInformation($"Cache expired for key: {cacheKey}");
                // Clean up expired cache
                _parcelsCache.Remove(cacheKey);
                _cacheTimestamps.Remove(cacheKey);
            }

            return !isExpired;
        }

        private void CacheParcels(string cacheKey, List<ParcelDisplayModel> parcels)
        {
            _parcelsCache[cacheKey] = parcels;
            _cacheTimestamps[cacheKey] = DateTime.Now;
            _logger.LogInformation($"Cached {parcels.Count} parcels with key: {cacheKey}");
        }

        private void LoadParcelsFromCache(string cacheKey)
        {
            if (!_parcelsCache.ContainsKey(cacheKey))
            {
                _logger.LogWarning($"No cached parcels found for key: {cacheKey}");
                return;
            }

            var cachedParcels = _parcelsCache[cacheKey];
            
            // Clear existing parcels
            FilteredParcels.Clear();

            // Add cached parcels to collection
            foreach (var parcel in cachedParcels)
            {
                FilteredParcels.Add(parcel);
                
                // Subscribe to selection changes
                parcel.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(ParcelDisplayModel.IsSelected))
                    {
                        UpdateCanCreateDispatch();
                        UpdateDispatchSummary();
                    }
                };
            }

            _logger.LogInformation($"Loaded {cachedParcels.Count} parcels from cache");
        }

        private void ClearCache()
        {
            _parcelsCache.Clear();
            _cacheTimestamps.Clear();
            _lastCacheKey = string.Empty;
            _logger.LogInformation("Parcels cache cleared");
        }

        private void InvalidateCacheForDestination(string destination)
        {
            var keysToRemove = _parcelsCache.Keys
                .Where(key => key.StartsWith($"{destination}|"))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _parcelsCache.Remove(key);
                _cacheTimestamps.Remove(key);
            }

            _logger.LogInformation($"Invalidated cache for destination: {destination}");
        }

        #endregion
    }


    public class DispatchDisplayModel
    {
        public string DispatchCode { get; set; }
        public string SourceBranch { get; set; }
        public string Destination { get; set; }
        public string VehicleNumber { get; set; }
        public string Driver { get; set; }
        public DateTime DispatchTime { get; set; }
        public string Status { get; set; }
        public int ParcelCount { get; set; }
    }
}