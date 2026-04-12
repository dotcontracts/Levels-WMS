using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using wms_android.shared.Models;
using wms_android.shared.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;

namespace wms_android.ViewModels
{
    public partial class AllParcelsViewModel : ObservableObject
    {
        private readonly IParcelService _parcelService;
        private readonly ILogger<AllParcelsViewModel> _logger;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isLoadingMore;

        [ObservableProperty]
        private ObservableCollection<Parcel> _parcels = new();

        [ObservableProperty]
        private DateTime _fromDate = DateTime.Today.AddDays(-30);

        [ObservableProperty]
        private DateTime _toDate = DateTime.Today;

        [ObservableProperty]
        private string _selectedPaymentMode = "All";

        [ObservableProperty]
        private string _selectedDestination = "All";

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _totalPages = 1;

        [ObservableProperty]
        private int _totalCount = 0;

        [ObservableProperty]
        private bool _hasNextPage;

        [ObservableProperty]
        private bool _hasPreviousPage;

        [ObservableProperty]
        private bool _isLoadingAll;

        [ObservableProperty]
        private string _statusText = string.Empty;

        // Computed property for UI binding
        public bool IsNotLoadingAll => !IsLoadingAll;

        // Pagination settings
        private const int PAGE_SIZE = 200; // Increased from 50 to 200 for better UX
        private bool _isInitialLoad = true;

        // Filter options collections
        public ObservableCollection<string> PaymentModes { get; } = new()
        {
            "All", "COD", "Paid", "Contract"
        };

        private ObservableCollection<string> _destinations = new() { "All" };
        public ObservableCollection<string> Destinations 
        { 
            get => _destinations;
            private set => SetProperty(ref _destinations, value);
        }

        public AllParcelsViewModel(IParcelService parcelService, ILogger<AllParcelsViewModel> logger)
        {
            _parcelService = parcelService;
            _logger = logger;

            // Subscribe to property changes for auto-filtering
            PropertyChanged += OnViewModelPropertyChanged;
        }

        [RelayCommand]
        public async Task LoadParcels()
        {
            try
            {
                IsLoading = true;
                CurrentPage = 1; // Reset to first page
                _isInitialLoad = true;

                var currentUserId = Preferences.Get("CurrentUserId", 0);
                if (currentUserId == 0)
                {
                    _logger.LogWarning("No current user ID found");
                    StatusText = "Please log in to view parcels";
                    return;
                }

                // Load user-specific destinations for filtering
                await LoadDestinations(currentUserId);

                // Load first page of parcels
                await LoadPage();

                _logger.LogInformation($"Initial load completed: {Parcels.Count} parcels loaded, Total: {TotalCount}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading parcels");
                StatusText = "Failed to load parcels. Please try again.";
                await Application.Current.MainPage.DisplayAlert("Error", "Failed to load parcels", "OK");
            }
            finally
            {
                IsLoading = false;
                _isInitialLoad = false;
            }
        }

        [RelayCommand]
        public async Task LoadNextPage()
        {
            if (!HasNextPage || IsLoadingMore || IsLoading)
                return;

            try
            {
                IsLoadingMore = true;
                CurrentPage++;
                await LoadPage(append: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading next page");
                // Revert page number on error
                CurrentPage--;
            }
            finally
            {
                IsLoadingMore = false;
            }
        }

        [RelayCommand]
        public async Task RefreshParcels()
        {
            try
            {
                CurrentPage = 1;
                await LoadPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing parcels");
                StatusText = "Failed to refresh parcels";
            }
        }

        [RelayCommand]
        public async Task ApplyFilters()
        {
            try
            {
                // Reset pagination when filters change
                CurrentPage = 1;
                await LoadPage();
                
                _logger.LogInformation($"Filters applied: {Parcels.Count} parcels match criteria");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying filters");
                StatusText = "Failed to apply filters";
            }
        }

        [RelayCommand]
        public async Task ClearFilters()
        {
            // Temporarily disable auto-filtering to prevent multiple API calls
            PropertyChanged -= OnViewModelPropertyChanged;
            
            try
            {
                FromDate = DateTime.Today.AddDays(-30);
                ToDate = DateTime.Today;
                SelectedPaymentMode = "All";
                SelectedDestination = "All";
                SearchText = string.Empty;
                
                // Re-enable auto-filtering and apply filters
                PropertyChanged += OnViewModelPropertyChanged;
                await ApplyFilters();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing filters");
                // Re-enable auto-filtering even on error
                PropertyChanged += OnViewModelPropertyChanged;
            }
        }

        [RelayCommand]
        public async Task SearchParcels()
        {
            await ApplyFilters();
        }

        [RelayCommand]
        public async Task LoadAllParcels()
        {
            if (IsLoading || IsLoadingAll)
                return;

            try
            {
                IsLoadingAll = true;
                StatusText = "Loading all parcels...";

                var currentUserId = Preferences.Get("CurrentUserId", 0);
                if (currentUserId == 0)
                {
                    StatusText = "Please log in to view parcels";
                    return;
                }

                // Load all pages until no more data
                var page = 1;
                var hasMoreData = true;
                var allParcels = new List<Parcel>();

                while (hasMoreData && page <= 100) // Safety limit: max 100 pages = 20,000 records
                {
                    var queryParams = new ParcelQueryParams
                    {
                        Page = page,
                        PageSize = PAGE_SIZE,
                        FromDate = FromDate,
                        ToDate = ToDate,
                        Destination = SelectedDestination != "All" ? SelectedDestination : null,
                        SearchTerm = !string.IsNullOrWhiteSpace(SearchText) ? SearchText : null
                    };

                    var response = await _parcelService.GetParcelsPagedByUserAsync(currentUserId, queryParams);

                    if (response?.Data != null && response.Data.Any())
                    {
                        allParcels.AddRange(response.Data);
                        hasMoreData = response.HasNextPage;
                        page++;
                        
                        // Update UI progressively
                        StatusText = $"Loading parcels... {allParcels.Count} loaded";
                    }
                    else
                    {
                        hasMoreData = false;
                    }
                }

                // Replace current parcels with all loaded parcels
                Parcels.Clear();
                foreach (var parcel in allParcels)
                {
                    Parcels.Add(parcel);
                }

                // Update pagination state
                TotalCount = allParcels.Count;
                TotalPages = 1; // All data is loaded, so just 1 "page"
                HasNextPage = false;
                HasPreviousPage = false;
                UpdateStatusText();

                _logger.LogInformation($"Loaded all {allParcels.Count} parcels");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading all parcels");
                StatusText = "Failed to load all parcels";
                await Application.Current.MainPage.DisplayAlert("Error", "Failed to load all parcels", "OK");
            }
            finally
            {
                IsLoadingAll = false;
            }
        }

        private async Task LoadPage(bool append = false)
        {
            var currentUserId = Preferences.Get("CurrentUserId", 0);
            if (currentUserId == 0)
                return;

            var queryParams = new ParcelQueryParams
            {
                Page = CurrentPage,
                PageSize = PAGE_SIZE,
                FromDate = FromDate,
                ToDate = ToDate,
                Destination = SelectedDestination != "All" ? SelectedDestination : null,
                SearchTerm = !string.IsNullOrWhiteSpace(SearchText) ? SearchText : null
            };

            // Handle payment method filtering
            if (SelectedPaymentMode != "All")
            {
                // Map payment modes to statuses or handle differently based on your business logic
                // This is a simplified approach - you may need to adjust based on how payment methods are stored
                queryParams.SearchTerm = string.IsNullOrWhiteSpace(queryParams.SearchTerm) 
                    ? SelectedPaymentMode 
                    : $"{queryParams.SearchTerm} {SelectedPaymentMode}";
            }

            _logger.LogInformation($"Loading page {CurrentPage} with filters: Destination={SelectedDestination}, Payment={SelectedPaymentMode}, Search={SearchText}");

            var response = await _parcelService.GetParcelsPagedByUserAsync(currentUserId, queryParams);

            if (response?.Data != null)
            {
                if (!append)
                {
                    // Replace all parcels for new search/filter
                    Parcels.Clear();
                }

                // Add new parcels
                foreach (var parcel in response.Data)
                {
                    Parcels.Add(parcel);
                }

                // Update pagination info
                TotalCount = response.TotalCount;
                TotalPages = response.TotalPages;
                HasNextPage = response.HasNextPage;
                HasPreviousPage = response.HasPreviousPage;

                // Update status text
                UpdateStatusText();

                _logger.LogInformation($"Loaded page {CurrentPage}: {response.Data.Count()} parcels. Total in view: {Parcels.Count}, Total available: {TotalCount}");
            }
            else
            {
                _logger.LogWarning("Received null or empty response from API");
                if (!append)
                {
                    Parcels.Clear();
                    TotalCount = 0;
                    TotalPages = 1;
                    HasNextPage = false;
                    HasPreviousPage = false;
                    UpdateStatusText();
                }
            }
        }

        private async Task LoadDestinations(int userId)
        {
            try
            {
                var destinations = await _parcelService.GetUniqueDestinationsByUserAsync(userId);
                
                Destinations.Clear();
                Destinations.Add("All");
                
                foreach (var destination in destinations.OrderBy(d => d))
                {
                    Destinations.Add(destination);
                }

                _logger.LogInformation($"Loaded {destinations.Count()} unique destinations for user {userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading destinations");
                // Keep fallback destinations
                Destinations.Clear();
                Destinations.Add("All");
                var fallbackDestinations = new[] { "Eldoret", "Kapsabet", "Kakamega", "Kericho", "Kisumu", "Kitale", "Mombasa", "Nairobi", "Nakuru" };
                foreach (var dest in fallbackDestinations)
                {
                    Destinations.Add(dest);
                }
            }
        }

        private void UpdateStatusText()
        {
            if (IsLoading)
            {
                StatusText = "Loading parcels...";
            }
            else if (IsLoadingMore)
            {
                StatusText = $"Showing {Parcels.Count} of {TotalCount} parcels (Loading more...)";
            }
            else if (TotalCount == 0)
            {
                StatusText = "No parcels found matching your criteria";
            }
            else if (TotalCount == Parcels.Count)
            {
                StatusText = $"Showing all {TotalCount} parcels";
            }
            else
            {
                StatusText = $"Showing {Parcels.Count} of {TotalCount} parcels";
            }
        }

        private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Skip auto-filtering during initial load to avoid multiple API calls
            if (_isInitialLoad || IsLoading)
                return;

            // Auto-apply filters when certain properties change
            if (e.PropertyName == nameof(FromDate) ||
                e.PropertyName == nameof(ToDate) ||
                e.PropertyName == nameof(SelectedPaymentMode) ||
                e.PropertyName == nameof(SelectedDestination))
            {
                // Use a small delay to avoid rapid-fire API calls during UI interactions
                _ = Task.Run(async () =>
                {
                    await Task.Delay(500); // 500ms delay
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        if (!IsLoading) // Double-check we're not loading
                        {
                            await ApplyFilters();
                        }
                    });
                });
            }
        }

        partial void OnIsLoadingChanged(bool value)
        {
            UpdateStatusText();
        }

        partial void OnIsLoadingMoreChanged(bool value)
        {
            UpdateStatusText();
        }

        partial void OnTotalCountChanged(int value)
        {
            UpdateStatusText();
        }

        partial void OnIsLoadingAllChanged(bool value)
        {
            OnPropertyChanged(nameof(IsNotLoadingAll));
        }

        /// <summary>
        /// Gets a parcel by index, automatically loading more if needed (infinite scroll support)
        /// </summary>
        public async Task<Parcel?> GetParcelAsync(int index)
        {
            // If we're near the end of loaded parcels and there are more pages, load next page
            if (index >= Parcels.Count - 10 && HasNextPage && !IsLoadingMore && !IsLoading)
            {
                await LoadNextPage();
            }

            return index < Parcels.Count ? Parcels[index] : null;
        }

        /// <summary>
        /// Clears all cached data and resets the view model
        /// </summary>
        public void Reset()
        {
            Parcels.Clear();
            CurrentPage = 1;
            TotalPages = 1;
            TotalCount = 0;
            HasNextPage = false;
            HasPreviousPage = false;
            StatusText = string.Empty;
            _isInitialLoad = true;
        }
    }
}