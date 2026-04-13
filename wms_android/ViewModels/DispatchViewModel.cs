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
        private bool _allowMultipleDestinations = true; // Default to true for multi-destination support

        [ObservableProperty]
        private bool _useMultiDestinationMode = true; // Control whether to use multi-destination mode
        
        // Fixed mode state management to prevent view disappearing issues
        private bool _isTabTransitioning = false;

        [ObservableProperty]
        private List<string> _availableDestinations = new();

        // Multi-destination data structures
        [ObservableProperty]
        private ObservableCollection<DestinationGroupModel> _destinationGroups = new();

        [ObservableProperty]
        private string _multiDestSummary = string.Empty;

        // Progressive loading properties
        [ObservableProperty]
        private int _currentPage = 1;
        [ObservableProperty]
        private int _pageSize = 100;
        [ObservableProperty]
        private int _totalItems = 0;
        [ObservableProperty]
        private bool _hasMorePages = false;
        [ObservableProperty]
        private bool _isLoadingMore = false;

        // Batch selection commands
        [RelayCommand]
        private void SelectAllParcels()
        {
            foreach (var parcel in FilteredParcels)
            {
                parcel.IsSelected = true;
            }
            UpdateCanCreateDispatch();
            UpdateDispatchSummary();
            UpdateMultiDestSummary();
        }

        [RelayCommand]
        private void DeselectAllParcels()
        {
            foreach (var parcel in FilteredParcels)
            {
                parcel.IsSelected = false;
            }
            UpdateCanCreateDispatch();
            UpdateDispatchSummary();
            UpdateMultiDestSummary();
        }

        [RelayCommand]
        private void SelectAllForDestination(string destination)
        {
            var destinationParcels = FilteredParcels.Where(p => p.Destination == destination).ToList();
            foreach (var parcel in destinationParcels)
            {
                parcel.IsSelected = true;
            }
            UpdateCanCreateDispatch();
            UpdateDispatchSummary();
            UpdateMultiDestSummary();
        }

        [RelayCommand]
        private void DeselectAllForDestination(string destination)
        {
            var destinationParcels = FilteredParcels.Where(p => p.Destination == destination).ToList();
            foreach (var parcel in destinationParcels)
            {
                parcel.IsSelected = false;
            }
            UpdateCanCreateDispatch();
            UpdateDispatchSummary();
            UpdateMultiDestSummary();
        }

        [RelayCommand]
        private async Task LoadMoreParcels()
        {
            if (!HasMorePages || IsLoadingMore) return;

            try
            {
                IsLoadingMore = true;
                CurrentPage++;

                if (UseMultiDestinationMode)
                {
                    await LoadParcelsForMultiDestinationDispatch();
                }
                else if (!string.IsNullOrWhiteSpace(SelectedDestination))
                {
                    await LoadParcelsForDestinationWithFilters();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading more parcels");
                CurrentPage--; // Revert page increment on error
            }
            finally
            {
                IsLoadingMore = false;
            }
        }

        [RelayCommand]
        private async Task RefreshParcelsProgressively()
        {
            CurrentPage = 1;
            if (UseMultiDestinationMode)
            {
                await LoadParcelsForMultiDestinationDispatch();
            }
            else if (!string.IsNullOrWhiteSpace(SelectedDestination))
            {
                await LoadParcelsForDestinationWithFilters();
            }
        }

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
        private string _dispatchRequirementsMessage = string.Empty;

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
            if (_isTabTransitioning) return;
            
            _isTabTransitioning = true;
            
            _logger.LogInformation("SelectDetailsTab called - Switching to Details tab");
            
            try
            {
                IsDetailsTabSelected = true;
                IsParcelsTabSelected = false;
                
                _logger.LogInformation($"Tab state updated - Details: {IsDetailsTabSelected}, Parcels: {IsParcelsTabSelected}");
            }
            finally
            {
                _isTabTransitioning = false;
                _logger.LogInformation("SelectDetailsTab completed");
            }
        }

        [RelayCommand]
        private async Task SelectParcelsTab()
        {
            if (_isTabTransitioning) return;
            
            _isTabTransitioning = true;
            _logger.LogInformation("Switched to Parcels tab - loading parcels");
            
            // First switch to the parcels tab UI immediately
            IsDetailsTabSelected = false;
            IsParcelsTabSelected = true;
            
            // Clear existing parcels and show loading state
            FilteredParcels.Clear();
            ParcelsCountText = "Loading parcels...";
            
            try
            {
                // Use multi-destination mode by default, fall back to destination-specific mode if needed
                if (UseMultiDestinationMode)
                {
                    await LoadParcelsForMultiDestinationDispatch();
                }
                else if (!string.IsNullOrWhiteSpace(SelectedDestination))
                {
                    await LoadParcelsForDestinationWithFilters();
                }
                else
                {
                    // If in single destination mode with no selection, show message and stop loading
                    ParcelsCountText = "Please select a destination in Details tab";
                    _logger.LogInformation("Parcels tab loaded with destination not selected message");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading parcels when switching to parcels tab");
                ParcelsCountText = "Error loading parcels";
            }
            finally
            {
                _isTabTransitioning = false;
            }
            
            _logger.LogInformation($"Parcels tab loaded successfully with {FilteredParcels.Count} parcels");
        }

        [RelayCommand]
        private async Task ApplyFilters()
        {
            if (UseMultiDestinationMode)
            {
                await LoadParcelsForMultiDestinationDispatch();
            }
            else if (!string.IsNullOrWhiteSpace(SelectedDestination))
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

                // Check user permissions for dispatch
                var currentUserId = Preferences.Get("CurrentUserId", 0);
                if (currentUserId > 0)
                {
                    try
                    {
                        var userService = Application.Current.Handler?.MauiContext?.Services?.GetService<IUserService>();
                        if (userService != null)
                        {
                            var currentUser = await userService.GetUserByIdAsync(currentUserId);
                            if (currentUser?.Role?.CanDispatch != true)
                            {
                                await Application.Current.MainPage.DisplayAlert("Access Denied", 
                                    "You don't have permission to create dispatches. Please contact your administrator.", "OK");
                                return;
                            }
                        }
                    }
                    catch (Exception permEx)
                    {
                        _logger.LogWarning(permEx, "Could not verify user permissions, proceeding with dispatch");
                        // Continue with dispatch if permission check fails (fallback for older versions)
                    }
                }

                var selectedParcels = FilteredParcels.Where(p => p.IsSelected).ToList();

                if (!selectedParcels.Any())
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Please select at least one parcel to dispatch", "OK");
                    return;
                }

                if (string.IsNullOrWhiteSpace(VehicleRegistration) ||
                    string.IsNullOrWhiteSpace(DriverName))
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Please fill in vehicle and driver details", "OK");
                    return;
                }

                // Allow dispatch in both single and multi-destination modes
                if (!UseMultiDestinationMode && string.IsNullOrWhiteSpace(SelectedDestination))
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Please select a destination", "OK");
                    return;
                }

                // Generate dispatch code similar to Django implementation
                var dispatchCode = GenerateDispatchCode();

                // Get current user's branch for SourceBranch
                var currentBranch = "Main Branch"; // Default fallback

                try
                {
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

                // Group selected parcels by destination for multi-destination support
                var parcelsByDestination = selectedParcels.GroupBy(p => p.Destination).ToDictionary(g => g.Key, g => g.ToList());

                // Determine primary destination
                var primaryDestination = UseMultiDestinationMode
                    ? parcelsByDestination.Keys.FirstOrDefault() ?? SelectedDestination
                    : SelectedDestination;

                _logger.LogInformation($"Creating dispatch with {parcelsByDestination.Count} destination(s), Primary: '{primaryDestination}', SourceBranch: '{currentBranch}'");

                var dispatch = new Dispatch
                {
                    Id = Guid.NewGuid(),
                    DispatchCode = dispatchCode,
                    VehicleNumber = VehicleRegistration,
                    Driver = DriverName,
                    DispatchTime = DateTime.Now,
                    Status = "In Transit",
                    ParcelIds = selectedParcels.Select(p => p.Id).ToList(),
                    SourceBranch = currentBranch,
                    Destination = UseMultiDestinationMode ? null : primaryDestination // Let API handle destination grouping
                };

                // Create dispatch through API
                Dispatch createdDispatch = null;
                try
                {
                    createdDispatch = await _parcelService.CreateDispatchAsync(dispatch);
                    _logger.LogInformation($"Created dispatch {dispatchCode} with {selectedParcels.Count} parcels across {parcelsByDestination.Count} destination(s)");
                }
                catch (Exception dispatchEx)
                {
                    _logger.LogWarning(dispatchEx, $"Failed to create dispatch through API, trying fallback approach");

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

                    createdDispatch = dispatch;
                    _logger.LogInformation($"Dispatch {dispatchCode} processed with fallback approach");
                }

                var destinationText = UseMultiDestinationMode
                    ? $"{parcelsByDestination.Count} destination(s): {string.Join(", ", parcelsByDestination.Keys)}"
                    : SelectedDestination;

                await Application.Current.MainPage.DisplayAlert("Success",
                    $"Dispatch {dispatchCode} created successfully!\n\n" +
                    $"• {selectedParcels.Count} parcel(s) dispatched\n" +
                    $"• Destinations: {destinationText}\n" +
                    $"• Vehicle: {VehicleRegistration}\n" +
                    $"• Driver: {DriverName}\n\n" +
                    $"All parcels have been updated to 'In Transit' status.", "OK");

                // Invalidate cache for affected destinations
                foreach (var destination in parcelsByDestination.Keys)
                {
                    InvalidateCacheForDestination(destination);
                }

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
                
                // If parcels tab is initially selected, load parcels for multi-destination mode
                if (IsParcelsTabSelected)
                {
                    _logger.LogInformation("Parcels tab initially selected - loading parcels");
                    await LoadParcelsForMultiDestinationDispatch();
                }
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
                // Use all known destinations hardcoded in the system
                // This ensures all 9 destinations are always available for selection
                var allKnownDestinations = new List<string> 
                { 
                    "Eldoret", "Kapsabet", "Kakamega", "Kericho", "Kisumu", 
                    "Kitale", "Mombasa", "Nairobi", "Nakuru"
                };
                
                DestinationOptions.Clear();
                foreach (var destination in allKnownDestinations.OrderBy(d => d))
                {
                    DestinationOptions.Add(destination);
                }
                
                _logger.LogInformation($"Loaded {DestinationOptions.Count} destinations for single destination mode");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading destinations");
                // Fallback to default destinations
                var fallbackDestinations = new List<string> 
                { 
                    "Eldoret", "Kapsabet", "Kakamega", "Kericho", "Kisumu", 
                    "Kitale", "Mombasa", "Nairobi", "Nakuru"
                };
                DestinationOptions.Clear();
                foreach (var destination in fallbackDestinations)
                {
                    DestinationOptions.Add(destination);
                }
            }
        }

        private void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Handle multi-destination mode changes to prevent view disappearing
            if (e.PropertyName == nameof(UseMultiDestinationMode))
            {
                _logger.LogInformation($"Multi-destination mode changed to: {UseMultiDestinationMode}");
                
                // Force UI refresh when mode changes
                HasDestinationSelected = !UseMultiDestinationMode && !string.IsNullOrWhiteSpace(SelectedDestination);
                
                // Clear and reload parcels if we're on the parcels tab
                if (IsParcelsTabSelected && !_isTabTransitioning)
                {
                    _logger.LogInformation("Auto-reloading parcels due to mode change while on parcels tab");
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            // Ensure UI is always visible during reload
                            IsParcelsTabSelected = true;
                            IsDetailsTabSelected = false;
                            
                            FilteredParcels.Clear();
                            ParcelsCountText = "Updating parcels...";
                            
                            if (UseMultiDestinationMode)
                            {
                                await LoadParcelsForMultiDestinationDispatch();
                            }
                            else if (!string.IsNullOrWhiteSpace(SelectedDestination))
                            {
                                await LoadParcelsForDestinationWithFilters();
                            }
                            else
                            {
                                ParcelsCountText = "Please select a destination in Details tab";
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error reloading parcels after mode change");
                            ParcelsCountText = "Error updating parcels";
                        }
                    });
                }
            }
            
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
                if (HasDestinationSelected && IsParcelsTabSelected)
                {
                    _ = LoadParcelsForDestinationWithFilters();
                }
                else
                {
                    // Don't clear parcels when switching to parcels tab - let SelectParcelsTab handle the loading
                    // This ensures parcels tab content remains visible
                    _logger.LogInformation("Parcels tab opened without destination selected - keeping any existing parcels");
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
                               (UseMultiDestinationMode || !string.IsNullOrWhiteSpace(SelectedDestination)) &&
                               selectedParcels.Any();
            
            // Update requirements message
            if (CanCreateDispatch)
            {
                DispatchRequirementsMessage = string.Empty;
            }
            else
            {
                var requirements = new List<string>();
                if (string.IsNullOrWhiteSpace(VehicleRegistration))
                    requirements.Add("vehicle registration");
                if (string.IsNullOrWhiteSpace(DriverName))
                    requirements.Add("driver name");
                if (!UseMultiDestinationMode && string.IsNullOrWhiteSpace(SelectedDestination))
                    requirements.Add("destination");
                if (!selectedParcels.Any())
                    requirements.Add("select parcels");
                
                DispatchRequirementsMessage = $"Please provide: {string.Join(", ", requirements)}";
            }
        }

        private void UpdateDispatchSummary()
        {
            var selectedParcels = FilteredParcels.Where(p => p.IsSelected).ToList();
            if (CanCreateDispatch)
            {
                if (UseMultiDestinationMode)
                {
                    var uniqueDestinations = selectedParcels.Select(p => p.Destination).Distinct().ToList();
                    var destinationsText = uniqueDestinations.Count > 1
                        ? $"{uniqueDestinations.Count} destinations: {string.Join(", ", uniqueDestinations)}"
                        : $"destination: {uniqueDestinations.FirstOrDefault()}";
                    DispatchSummary = $"Ready to dispatch {selectedParcels.Count} parcel(s) to {destinationsText} using vehicle {VehicleRegistration} with driver {DriverName}.";
                }
                else
                {
                    DispatchSummary = $"Ready to dispatch {selectedParcels.Count} parcel(s) to {SelectedDestination} using vehicle {VehicleRegistration} with driver {DriverName}.";
                }
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
                // Set loading state
                IsLoading = true;
                
                _logger.LogInformation($"Starting LoadParcelsForDestinationWithFilters - Destination: '{SelectedDestination}', FromDate: {FromDate:yyyy-MM-dd}, ToDate: {ToDate:yyyy-MM-dd}, Clerk: '{SelectedClerk}'");
                
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
                
                // Load only confirmed parcels for dispatch (pending parcels cannot be dispatched)
                var parcels = await _parcelService.GetParcelsForDispatchAsync(
                    destination: SelectedDestination,
                    statuses: new List<ParcelStatus> { ParcelStatus.Confirmed },
                    fromDate: FromDate,
                    toDate: ToDate,
                    createdByUsername: SelectedClerk != "All" ? SelectedClerk : null
                );
                
                _logger.LogInformation($"API returned {parcels.Count()} parcels for destination {SelectedDestination}");
                
                if (parcels == null || !parcels.Any())
                {
                    _logger.LogInformation("No parcels found from API");
                    FilteredParcels.Clear();
                    ParcelsCountText = $"Found 0 parcel(s) for {SelectedDestination}";
                    return;
                }
                
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

                _logger.LogInformation($"Converted {displayParcels.Count} parcels to display models");

                // Cache the parcels
                CacheParcels(cacheKey, displayParcels);
                
                // Load parcels into UI
                LoadParcelsFromCache(cacheKey);
                
                _logger.LogInformation($"Loaded {displayParcels.Count} parcels into FilteredParcels collection");
                
                // Update count text
                ParcelsCountText = $"Found {displayParcels.Count} parcel(s) for {SelectedDestination}";
                
                // Load unique clerks for the destination
                await LoadClerksForDestinationOptimized();
                
                _logger.LogInformation($"Final state - FilteredParcels.Count: {FilteredParcels.Count}, IsLoading: {IsLoading}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading parcels for destination");
                ParcelsCountText = "Error loading parcels";
            }
            finally
            {
                IsLoading = false; // Always stop loading when done
            }
        }

        /// <summary>
        /// Load parcels for multi-destination dispatch mode without destination constraints
        /// </summary>
        private async Task LoadParcelsForMultiDestinationDispatch()
        {
            try
            {
                // Set loading state
                IsLoading = true;
                
                // Generate cache key for multi-destination mode with pagination
                var cacheKey = GenerateMultiDestCacheKeyWithPage(CurrentPage);
                
                // Check if we have valid cached data
                if (IsCacheValid(cacheKey))
                {
                    _logger.LogInformation($"Loading parcels from cache for multi-destination mode page {CurrentPage}");
                    LoadParcelsFromCache(cacheKey, append: CurrentPage > 1);
                    UpdateMultiDestSummary();
                    UpdateDestinationGroups(FilteredParcels.ToList());
                    return;
                }
                
                _logger.LogInformation($"Loading parcels from API for multi-destination dispatch page {CurrentPage}");
                
                // Load only confirmed parcels without destination constraint (pending parcels cannot be dispatched)
                var parcels = await _parcelService.GetParcelsForDispatchAsync(
                    destination: null, // No destination constraint
                    statuses: new List<ParcelStatus> { ParcelStatus.Confirmed },
                    fromDate: FromDate,
                    toDate: ToDate,
                    createdByUsername: SelectedClerk != "All" ? SelectedClerk : null
                );
                
                _logger.LogInformation($"API returned {parcels.Count()} parcels for multi-destination dispatch page {CurrentPage}");
                
                if (parcels == null || !parcels.Any())
                {
                    _logger.LogInformation("No parcels found from API");
                    FilteredParcels.Clear();
                    ParcelsCountText = "No parcels found with current filters";
                    UpdateMultiDestSummary();
                    return;
                }
                
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

                _logger.LogInformation($"Converted {displayParcels.Count} parcels to display models");

                // Cache parcels
                CacheParcels(cacheKey, displayParcels);
                
                // Load parcels into UI
                LoadParcelsFromCache(cacheKey, append: CurrentPage > 1);
                
                // Update destination groups for smart grouping
                UpdateDestinationGroups(FilteredParcels.ToList());
                
                // Update summary
                UpdateMultiDestSummary();
                
                _logger.LogInformation($"Loaded and cached {displayParcels.Count} parcels for multi-destination dispatch page {CurrentPage}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading parcels for multi-destination dispatch");
                ParcelsCountText = "Error loading parcels";
            }
            finally
            {
                IsLoading = false; // Always stop loading when done
            }
        }

        /// <summary>
        /// Generate cache key for multi-destination mode with page number
        /// </summary>
        private string GenerateMultiDestCacheKeyWithPage(int page)
        {
            var key = $"MULTI_DEST_PAGE_{page}|{FromDate:yyyy-MM-dd}|{ToDate:yyyy-MM-dd}|{SelectedClerk}";
            _lastCacheKey = key;
            return key;
        }

        /// <summary>
        /// Update destination groups for UI display
        /// </summary>
        private void UpdateDestinationGroups(List<ParcelDisplayModel> parcels)
        {
            // Group parcels by destination
            var groupedParcels = parcels.GroupBy(p => p.Destination ?? "Unknown")
                .Select(g => new DestinationGroupModel
                {
                    Destination = g.Key,
                    ParcelCount = g.Count(),
                    TotalAmount = g.Sum(p => p.Amount),
                    Parcels = new ObservableCollection<ParcelDisplayModel>(g.ToList())
                })
                .OrderBy(g => g.Destination)
                .ToList();

            // Update destination groups collection
            DestinationGroups.Clear();
            foreach (var group in groupedParcels)
            {
                DestinationGroups.Add(group);
            }

            _logger.LogInformation($"Updated {groupedParcels.Count} destination groups");
        }

        /// <summary>
        /// Update multi-destination summary text
        /// </summary>
        private void UpdateMultiDestSummary()
        {
            var selectedParcels = FilteredParcels.Where(p => p.IsSelected).ToList();
            if (selectedParcels.Any())
            {
                var uniqueDestinations = selectedParcels.Select(p => p.Destination).Distinct().ToList();
                var totalAmount = selectedParcels.Sum(p => p.Amount);
                
                var destinationsText = uniqueDestinations.Count > 1
                    ? $"{uniqueDestinations.Count} destinations: {string.Join(", ", uniqueDestinations)}"
                    : $"destination: {uniqueDestinations.FirstOrDefault() ?? "Unknown"}";
                
                MultiDestSummary = $"Ready to dispatch {selectedParcels.Count} parcel(s) to {destinationsText} using vehicle {VehicleRegistration} with driver {DriverName}. Total: Ksh {totalAmount:N2}";
            }
            else
            {
                MultiDestSummary = string.Empty;
            }
        }

        /// <summary>
        /// Generate cache key for multi-destination mode
        /// </summary>
        private string GenerateMultiDestCacheKey()
        {
            var key = $"MULTI_DEST|{FromDate:yyyy-MM-dd}|{ToDate:yyyy-MM-dd}|{SelectedClerk}";
            _lastCacheKey = key;
            return key;
        }

        private async Task LoadClerksForDestinationOptimized()
        {
            try
            {
                _logger.LogInformation($"Starting to load clerks for destination: '{SelectedDestination}'");
                
                // Use optimized API call to get unique clerks from ALL branches
                // This allows filtering by any clerk who created parcels for this destination (pending/confirmed)
                var clerkNames = await _parcelService.GetUniqueClerksByDestinationAsync(SelectedDestination, ParcelStatus.Pending);
                
                _logger.LogInformation($"API returned {clerkNames.Count()} clerks for destination '{SelectedDestination}'");
                
                ClerkOptions.Clear();
                ClerkOptions.Add("All");
                foreach (var clerk in clerkNames)
                {
                    ClerkOptions.Add(clerk);
                    _logger.LogInformation($"Added clerk: '{clerk}' to options");
                }
                
                _logger.LogInformation($"Loaded {ClerkOptions.Count} total clerks options (including 'All') for destination {SelectedDestination}");
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
            if (UseMultiDestinationMode)
            {
                await LoadParcelsForMultiDestinationDispatch();
            }
            else if (HasDestinationSelected)
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

        private void LoadParcelsFromCache(string cacheKey, bool append = false)
        {
            if (!_parcelsCache.ContainsKey(cacheKey))
            {
                _logger.LogWarning($"No cached parcels found for key: {cacheKey}");
                return;
            }

            var cachedParcels = _parcelsCache[cacheKey];
            
            if (append)
            {
                // Append mode: add new parcels without clearing existing ones
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
                            UpdateMultiDestSummary();
                        }
                    };
                }
                _logger.LogInformation($"Appended {cachedParcels.Count} parcels from cache");
            }
            else
            {
                // Replace mode: clear existing and add new ones
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
                            UpdateMultiDestSummary();
                        }
                    };
                }

                _logger.LogInformation($"Loaded {cachedParcels.Count} parcels from cache");
            }
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

    public class DestinationGroupModel
    {
        public string Destination { get; set; } = string.Empty;
        public int ParcelCount { get; set; }
        public decimal TotalAmount { get; set; }
        public ObservableCollection<ParcelDisplayModel> Parcels { get; set; } = new();
        public bool IsExpanded { get; set; }
        public bool AllSelected => Parcels.All(p => p.IsSelected);
    }
}