using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using wms_android.shared.Models;
using wms_android.shared.DTOs;

namespace wms_android.shared.Interfaces
{
    public interface IParcelService
    {
        Task<IEnumerable<Parcel>> GetParcelsAsync();
        Task<Parcel> GetParcelByIdAsync(Guid id);
        Task<Parcel> CreateParcelAsync(Parcel parcel);
        Task<Parcel> UpdateParcelAsync(Parcel parcel);
        Task<string> GenerateWaybillNumberAsync();
        Task FinalizeWaybillAsync(Guid parcelId);
        
        // Add an overload that doesn't require the parcelId (implements compatibility with data version)
        Task FinalizeWaybillAsync();
        
        Task<List<Parcel>> CreateCartParcels(List<Parcel> parcels);
        Task<decimal> GetTotalSalesForDateAsync(DateTime date);
        Task<IEnumerable<Parcel>> GetPendingOrdersAsync(DateTime? dateFilter = null);
        Task<Parcel> GetParcelByWaybillNumberAsync(string waybillNumber);
        Task<IEnumerable<Parcel>> GetParcelsByQRCodeAsync(string qrCode);
        Task<int> GetParcelCountForDateAsync(DateTime date);
        Task DispatchParcelAsync(Parcel parcel);
        Task UpdateParcelStatusAsync(Guid parcelId, ParcelStatus status, int? confirmedById = null);
        
        // Dashboard methods
        Task<int> GetParcelCountAsync(DateTime date);
        Task<int> GetDeliveredParcelCountAsync(DateTime date);
        Task<double> GetTotalSalesAsync(DateTime date);
        
        // SMS notification tracking
        Task<bool> CheckSmsNotificationSentAsync(Guid parcelId);
        Task MarkSmsNotificationSentAsync(Guid parcelId);
        
        // User-specific parcels
        Task<IEnumerable<Parcel>> GetParcelsByUserAsync(int userId);
        
        // User-specific dashboard metrics
        Task<double> GetAmountOwedByUserAsync(int userId, DateTime date); // Total amount of paid parcels for the day
        Task<double> GetCashInByUserAsync(int userId, DateTime date); // Returns monthly clerk debit (current month + 5 days)
        Task<double> GetDailySalesByUserAsync(int userId, DateTime date);
        Task<double> GetMonthlySalesByUserAsync(int userId, DateTime date);
        
        // Dispatch methods
        Task<IEnumerable<Parcel>> GetParcelsReadyForDispatchAsync();
        Task<IEnumerable<Parcel>> GetParcelsForDispatchAsync(string? destination = null, 
            List<ParcelStatus>? statuses = null, DateTime? fromDate = null, DateTime? toDate = null, 
            string? createdByUsername = null);
        Task<Dispatch> CreateDispatchAsync(Dispatch dispatch);
        Task<IEnumerable<Dispatch>> GetRecentDispatchesAsync();
        Task<IEnumerable<string>> GetDestinationsAsync();
        Task<IEnumerable<Branch>> GetAllBranchesAsync();
        Task<IEnumerable<Parcel>> GetAllParcelsAsync();
        
        // Paginated methods for performance optimization
        Task<PaginatedResponse<Parcel>> GetParcelsPagedAsync(ParcelQueryParams queryParams);
        Task<PaginatedResponse<Parcel>> GetParcelsPagedByUserAsync(int userId, ParcelQueryParams queryParams);
        Task<IEnumerable<string>> GetUniqueDestinationsAsync(ParcelStatus? status = null);
        Task<IEnumerable<string>> GetUniqueDestinationsByUserAsync(int userId, ParcelStatus? status = null);
        Task<IEnumerable<string>> GetUniqueClerksByDestinationAsync(string destination, ParcelStatus? status = null);
        Task<IEnumerable<string>> GetUniqueClerksByDestinationAndUserAsync(int userId, string destination, ParcelStatus? status = null);
        
        // ParcelDeposits methods
        Task<ClerkCashInSummaryDto?> GetClerkCashInSummaryAsync(int userId, DateTime date);
    }
} 