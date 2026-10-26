using Microsoft.EntityFrameworkCore;
using wms_android.shared.Data;
using wms_android.shared.Models;
using wms_android.shared.Interfaces;
using wms_android.shared.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Npgsql; // Added for PostgresException

namespace wms_android.api.Services
{
    public class ApiParcelService : IParcelService
    {
        private readonly AppDbContext _context;

        public ApiParcelService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Parcel>> GetParcelsAsync()
        {
            return await _context.Parcels.ToListAsync();
        }

        public async Task<Parcel> GetParcelByIdAsync(Guid id)
        {
            return await _context.Parcels
                .AsNoTracking() // Prevent loading navigation properties and tracking
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Parcel> CreateParcelAsync(Parcel parcelInitialState)
        {
            Console.WriteLine("=== SERVICE: CreateParcelAsync started ===");
            // 1. Initialize parcelToProcess from parcelInitialState
            Parcel parcelToProcess = new Parcel
            {
                Id = parcelInitialState.Id == Guid.Empty ? Guid.NewGuid() : parcelInitialState.Id,
                CreatedAt = parcelInitialState.CreatedAt == default ? DateTime.UtcNow : parcelInitialState.CreatedAt, // Ensure CreatedAt is set
                // WaybillNumber will be set from Shipment
                QRCode = parcelInitialState.QRCode, // Will be updated if based on Waybill
                DispatchedAt = parcelInitialState.DispatchedAt,
                DispatchTrackingCode = parcelInitialState.DispatchTrackingCode,
                CreatedById = parcelInitialState.CreatedById,
                CreatorLastNameSnapshot = parcelInitialState.CreatorLastNameSnapshot,
                Sender = parcelInitialState.Sender,
                SenderTelephone = parcelInitialState.SenderTelephone,
                Receiver = parcelInitialState.Receiver,
                ReceiverTelephone = parcelInitialState.ReceiverTelephone,
                Destination = parcelInitialState.Destination,
                Quantity = parcelInitialState.Quantity,
                Description = parcelInitialState.Description,
                Amount = parcelInitialState.Amount,
                Rate = parcelInitialState.Rate,
                PaymentMethods = parcelInitialState.PaymentMethods,
                TotalAmount = parcelInitialState.TotalAmount,
                TotalRate = parcelInitialState.TotalRate,
                Status = parcelInitialState.Status == 0 ? ParcelStatus.Pending : parcelInitialState.Status // Ensure Status is set
            };

            System.Diagnostics.Debug.WriteLine($"[CreateParcelAsync] Initializing. Parcel ID: {parcelToProcess.Id}");

            string shipmentWaybillNumber = parcelInitialState.WaybillNumber;
            bool waybillGeneratedByServer = false;

            if (string.IsNullOrEmpty(shipmentWaybillNumber))
            {
                shipmentWaybillNumber = await GeneratePotentiallyNonUniqueWaybillNumberAsync();
                waybillGeneratedByServer = true;
                System.Diagnostics.Debug.WriteLine($"[CreateParcelAsync] Server generated initial WaybillNumber for Shipment: {shipmentWaybillNumber}");
            }

            parcelToProcess.WaybillNumber = shipmentWaybillNumber; // Denormalized copy
            if (string.IsNullOrEmpty(parcelToProcess.QRCode) || waybillGeneratedByServer)
            {
                parcelToProcess.QRCode = shipmentWaybillNumber;
                System.Diagnostics.Debug.WriteLine($"[CreateParcelAsync] QRCode set/updated to: {shipmentWaybillNumber}");
            }

            int maxRetries = 5;
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                Shipment newShipment = null;
                System.Diagnostics.Debug.WriteLine($"[CreateParcelAsync] Attempt {attempt + 1} for Shipment Waybill: {shipmentWaybillNumber}");

                try
                {
                    if (attempt > 0 && waybillGeneratedByServer)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CreateParcelAsync] Retrying Shipment waybill generation.");
                        shipmentWaybillNumber = await GeneratePotentiallyNonUniqueWaybillNumberAsync(attempt);
                        parcelToProcess.WaybillNumber = shipmentWaybillNumber;
                        parcelToProcess.QRCode = shipmentWaybillNumber; // Keep QRCode in sync
                        System.Diagnostics.Debug.WriteLine($"[CreateParcelAsync] Server regenerated WaybillNumber for Shipment: {shipmentWaybillNumber}");
                    }

                    if (await _context.Shipments.AnyAsync(s => s.WaybillNumber == shipmentWaybillNumber))
                    {
                        if (!waybillGeneratedByServer)
                        {
                            System.Diagnostics.Debug.WriteLine($"[CreateParcelAsync] Client-provided WaybillNumber '{shipmentWaybillNumber}' for Shipment already exists. Failing fast.");
                            throw new InvalidOperationException($"The WaybillNumber '{shipmentWaybillNumber}' for the shipment already exists and was client-provided.");
                        }
                        if (attempt < maxRetries - 1)
                        {
                            System.Diagnostics.Debug.WriteLine($"[CreateParcelAsync] Server-generated WaybillNumber '{shipmentWaybillNumber}' for Shipment already exists. Retrying.");
                            _context.ChangeTracker.Clear(); // Clear tracker before next attempt
                            continue;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[CreateParcelAsync] Max retries for server-generated unique Shipment waybill (pre-emptive). Last tried: {shipmentWaybillNumber}");
                            throw new InvalidOperationException($"Failed to generate a unique server-generated WaybillNumber for the shipment after {maxRetries} attempts. The last tried waybill was '{shipmentWaybillNumber}'.");
                        }
                    }

                    // Populate CreatorLastNameSnapshot if not already set
                    string creatorLastNameSnapshot = parcelToProcess.CreatorLastNameSnapshot;
                    
                    System.Diagnostics.Debug.WriteLine($"[CreateParcelAsync] Processing parcel. Original CreatorLastNameSnapshot: '{creatorLastNameSnapshot}', CreatedById: {parcelToProcess.CreatedById}");
                    
                    if (string.IsNullOrEmpty(creatorLastNameSnapshot) && parcelToProcess.CreatedById.HasValue)
                    {
                        var user = await _context.Users.FindAsync(parcelToProcess.CreatedById.Value);
                        if (user != null)
                        {
                            creatorLastNameSnapshot = user.LastName;
                            parcelToProcess.CreatorLastNameSnapshot = user.LastName; // Update parcel too
                            System.Diagnostics.Debug.WriteLine($"[CreateParcelAsync] Found user and set CreatorLastNameSnapshot to: '{user.LastName}'");
                        }
                        else
                        {
                            creatorLastNameSnapshot = "Unknown"; // Fallback value
                            System.Diagnostics.Debug.WriteLine($"[CreateParcelAsync] User not found, setting CreatorLastNameSnapshot to 'Unknown'");
                        }
                    }
                    else if (string.IsNullOrEmpty(creatorLastNameSnapshot))
                    {
                        creatorLastNameSnapshot = "Unknown"; // Fallback when no user
                        System.Diagnostics.Debug.WriteLine($"[CreateParcelAsync] No CreatedById, setting CreatorLastNameSnapshot to 'Unknown'");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[CreateParcelAsync] Using existing CreatorLastNameSnapshot: '{creatorLastNameSnapshot}'");
                    }

                    newShipment = new Shipment
                    {
                        Id = Guid.NewGuid(),
                        WaybillNumber = shipmentWaybillNumber,
                        CreatedAt = parcelToProcess.CreatedAt,
                        CreatedById = parcelToProcess.CreatedById,
                        CreatorLastNameSnapshot = creatorLastNameSnapshot
                    };
                    
                    System.Diagnostics.Debug.WriteLine($"[CreateParcelAsync] Created Shipment with CreatorLastNameSnapshot: '{newShipment.CreatorLastNameSnapshot}'");
                     // EF Core recommends DateTimeOffset for CreatedAt in Shipment, ensure conversion if Parcel.CreatedAt is DateTime
                    if (parcelToProcess.CreatedAt.Kind == DateTimeKind.Unspecified) {
                        newShipment.CreatedAt = new DateTimeOffset(DateTime.SpecifyKind(parcelToProcess.CreatedAt, DateTimeKind.Utc));
                    } else {
                        newShipment.CreatedAt = new DateTimeOffset(parcelToProcess.CreatedAt);
                    }


                    parcelToProcess.ShipmentId = newShipment.Id;
                    
                    _context.ChangeTracker.Clear(); // Clear before attaching/adding to avoid conflicts from prior loop iterations.

                    _context.Shipments.Add(newShipment);

                    // Assuming CreateParcelAsync is for NEW parcels primarily.
                    // If it can also update, the logic for parcelInDb needs to be re-evaluated carefully
                    // especially regarding its shipment link. For now, simplifying to ADD only.
                    var existingParcelById = await _context.Parcels.AsNoTracking().FirstOrDefaultAsync(p => p.Id == parcelToProcess.Id);
                    if (existingParcelById != null && parcelInitialState.Id != Guid.Empty) { // Check if client intended an update by providing an ID
                        System.Diagnostics.Debug.WriteLine($"[CreateParcelAsync] Parcel with ID {parcelToProcess.Id} found in DB. Client provided this ID. Updating. (Attempt {attempt + 1})");
                         // If updating, ensure ShipmentId is handled correctly if it can change.
                         // For this flow, we assume it's linked to the newShipment created for this operation.
                        _context.Parcels.Update(parcelToProcess);
                    } else {
                        System.Diagnostics.Debug.WriteLine($"[CreateParcelAsync] Adding new Parcel with ID {parcelToProcess.Id}. (Attempt {attempt + 1})");
                        _context.Parcels.Add(parcelToProcess);
                    }
                    
                    // Removed problematic JSON serialization logging to prevent circular reference errors
                    System.Diagnostics.Debug.WriteLine($"[CreateParcelAsync] Parcel to save (Attempt {attempt + 1}): ID={parcelToProcess.Id}, WaybillNumber={parcelToProcess.WaybillNumber}");
                    System.Diagnostics.Debug.WriteLine($"[CreateParcelAsync] Shipment to save (Attempt {attempt + 1}): ID={newShipment.Id}, WaybillNumber={newShipment.WaybillNumber}");

                    await _context.SaveChangesAsync();
                    System.Diagnostics.Debug.WriteLine($"[CreateParcelAsync] Successfully saved Shipment ID: {newShipment.Id} and Parcel ID: {parcelToProcess.Id} on attempt {attempt + 1}");
                    
                    // Detach the entity from context to prevent circular reference during serialization
                    _context.Entry(parcelToProcess).State = EntityState.Detached;
                    if (newShipment != null)
                    {
                        _context.Entry(newShipment).State = EntityState.Detached;
                    }
                    
                    // Create a clean parcel object without navigation properties to prevent circular references
                    var cleanParcel = new Parcel
                    {
                        Id = parcelToProcess.Id,
                        CreatedAt = parcelToProcess.CreatedAt,
                        WaybillNumber = parcelToProcess.WaybillNumber,
                        QRCode = parcelToProcess.QRCode,
                        DispatchedAt = parcelToProcess.DispatchedAt,
                        DispatchTrackingCode = parcelToProcess.DispatchTrackingCode,
                        CreatedById = parcelToProcess.CreatedById,
                        CreatorLastNameSnapshot = parcelToProcess.CreatorLastNameSnapshot,
                        ShipmentId = parcelToProcess.ShipmentId,
                        Sender = parcelToProcess.Sender,
                        SenderTelephone = parcelToProcess.SenderTelephone,
                        Receiver = parcelToProcess.Receiver,
                        ReceiverTelephone = parcelToProcess.ReceiverTelephone,
                        Destination = parcelToProcess.Destination,
                        Quantity = parcelToProcess.Quantity,
                        Description = parcelToProcess.Description,
                        Amount = parcelToProcess.Amount,
                        Rate = parcelToProcess.Rate,
                        PaymentMethods = parcelToProcess.PaymentMethods,
                        TotalAmount = parcelToProcess.TotalAmount,
                        TotalRate = parcelToProcess.TotalRate,
                        AmountPaid = parcelToProcess.AmountPaid,
                        TransactionCode = parcelToProcess.TransactionCode,
                        PaymentUpdatedAt = parcelToProcess.PaymentUpdatedAt,
                        PaymentUpdatedById = parcelToProcess.PaymentUpdatedById,
                        Status = parcelToProcess.Status
                        // Explicitly NOT setting Shipment, CreatedBy, or PaymentUpdatedBy navigation properties
                    };
                    
                    Console.WriteLine("=== SERVICE: Returning clean parcel ===");
                    return cleanParcel;
                }
                catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
                {
                    System.Diagnostics.Debug.WriteLine($"[CreateParcelAsync] DbUpdateException: Unique constraint violation (likely for Shipment WaybillNumber: {shipmentWaybillNumber}). Attempt {attempt + 1} of {maxRetries}. Error: {ex.Message}");
                     _context.ChangeTracker.Clear(); // Clear tracker before next attempt

                    if (!waybillGeneratedByServer && newShipment != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CreateParcelAsync] Shipment WaybillNumber {newShipment.WaybillNumber} (client-provided) caused DB unique constraint. Failing.");
                        throw new InvalidOperationException($"The WaybillNumber '{newShipment.WaybillNumber}' for the shipment already exists and was client-provided.", ex);
                    }
                    if (attempt == maxRetries - 1)
                    {
                        System.Diagnostics.Debug.WriteLine("[CreateParcelAsync] Max retries for server-generated Shipment waybill (DB unique constraint).");
                        throw; 
                    }
                    // Continue to next attempt if server-generated
            }
            catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CreateParcelAsync] Non-DbUpdateException (Attempt {attempt + 1}): {ex.GetType().Name} - {ex.Message}. {(ex.InnerException == null ? "" : "Inner: " + ex.InnerException.Message)}");
                    throw;
                }
            }
            throw new Exception($"[CreateParcelAsync] Failed to create parcel and shipment after {maxRetries} attempts. Last Waybill: {shipmentWaybillNumber}");
        }

        // Renamed and added attemptOffset
        private static readonly Random _random = new Random();
        private const string AlphanumericChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        public async Task<string> GeneratePotentiallyNonUniqueWaybillNumberAsync(int attemptOffset = 0)
        {
            var suffix = new string(Enumerable.Range(0, 5).Select(_ => AlphanumericChars[_random.Next(AlphanumericChars.Length)]).ToArray());
            var waybillNumber = $"WB{suffix}";
            System.Diagnostics.Debug.WriteLine($"Generated potential WaybillNumber for Shipment: {waybillNumber} (Attempt offset: {attemptOffset})");
            return waybillNumber;
        }

        // Original GenerateWaybillNumberAsync - now points to the new one for compatibility
        public async Task<string> GenerateWaybillNumberAsync()
        {
            return await GeneratePotentiallyNonUniqueWaybillNumberAsync();
        }

        private bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            if (ex.InnerException is Npgsql.PostgresException postgresException)
            {
                return postgresException.SqlState == "23505"; // "unique_violation"
            }
            return false;
        }

        public async Task FinalizeWaybillAsync(Guid parcelId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Starting FinalizeWaybillAsync for parcel ID: {parcelId}");

                // Find the parcel with tracking
                var parcel = await _context.Parcels
                    .FirstOrDefaultAsync(p => p.Id == parcelId);

                if (parcel == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Parcel with ID {parcelId} not found");
                    throw new Exception($"Parcel with ID {parcelId} not found");
                }

                System.Diagnostics.Debug.WriteLine($"Found parcel: ID={parcel.Id}, Status={parcel.Status}");

                // Update the status
                parcel.Status = ParcelStatus.Confirmed;

                System.Diagnostics.Debug.WriteLine("Saving changes to database...");
                var result = await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($"SaveChangesAsync result: {result}");

                // Verify the update
                var updatedParcel = await _context.Parcels
                    .FirstOrDefaultAsync(p => p.Id == parcelId);

                if (updatedParcel?.Status != ParcelStatus.Confirmed)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to verify parcel finalization. Current status: {updatedParcel?.Status}");
                    throw new Exception($"Failed to finalize parcel with ID {parcelId}");
                }

                System.Diagnostics.Debug.WriteLine($"Successfully confirmed parcel: ID={updatedParcel.Id}, Status={updatedParcel.Status}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in FinalizeWaybillAsync: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task FinalizeWaybillAsync()
        {
            // Implement logic if needed, or leave empty if only the Guid version is used server-side.
            await Task.CompletedTask; // Placeholder
        }

        public async Task<List<Parcel>> CreateCartParcels(List<Parcel> parcelInputs)
        {
            if (parcelInputs == null || !parcelInputs.Any())
            {
                return new List<Parcel>();
            }

            // Extract common details for the Shipment from the first parcel DTO
            var firstParcelInput = parcelInputs.First();
            var commonWaybillNumber = firstParcelInput.WaybillNumber;
            int? commonCreatedById = firstParcelInput.CreatedById;

            if (string.IsNullOrEmpty(commonWaybillNumber))
            {
                System.Diagnostics.Debug.WriteLine("[CreateCartParcels] Error: Batch received with no common WaybillNumber for the Shipment.");
                throw new ArgumentException("Parcels in cart must have a common WaybillNumber to be used for the Shipment.");
            }
            System.Diagnostics.Debug.WriteLine($"[CreateCartParcels] Processing batch for Shipment with commonWaybill: {commonWaybillNumber}");

            // Check if a Shipment with this WaybillNumber already exists
            if (await _context.Shipments.AnyAsync(s => s.WaybillNumber == commonWaybillNumber))
            {
                System.Diagnostics.Debug.WriteLine($"[CreateCartParcels] Error: Shipment with WaybillNumber '{commonWaybillNumber}' already exists.");
                throw new InvalidOperationException($"A Shipment with WaybillNumber '{commonWaybillNumber}' already exists. Please use a new waybill number for the cart.");
            }

            // Populate CreatorLastNameSnapshot for Shipment
            string shipmentCreatorLastName = "Unknown"; // Default fallback
            if (commonCreatedById.HasValue)
            {
                var user = await _context.Users.FindAsync(commonCreatedById.Value);
                if (user != null)
                {
                    shipmentCreatorLastName = user.LastName;
                }
            }
            
            // Create the new Shipment
            var newShipment = new Shipment
            {
                Id = Guid.NewGuid(),
                WaybillNumber = commonWaybillNumber,
                CreatedAt = DateTimeOffset.UtcNow, 
                CreatedById = commonCreatedById,
                CreatorLastNameSnapshot = shipmentCreatorLastName
            };
            
            _context.Shipments.Add(newShipment);
            var processedParcels = new List<Parcel>();

            foreach (var parcelInput in parcelInputs)
            {
                Parcel parcelToProcess = new Parcel 
                {
                    Id = parcelInput.Id == Guid.Empty ? Guid.NewGuid() : parcelInput.Id, 
                    Sender = parcelInput.Sender,
                    SenderTelephone = parcelInput.SenderTelephone,
                    Receiver = parcelInput.Receiver,
                    ReceiverTelephone = parcelInput.ReceiverTelephone,
                    Destination = parcelInput.Destination,
                    Quantity = parcelInput.Quantity,
                    Description = parcelInput.Description,
                    Amount = parcelInput.Amount,
                    Rate = parcelInput.Rate,
                    PaymentMethods = parcelInput.PaymentMethods,
                    TotalAmount = parcelInput.TotalAmount,
                    TotalRate = parcelInput.TotalRate,
                    
                    ShipmentId = newShipment.Id,
                    WaybillNumber = newShipment.WaybillNumber, 
                    QRCode = newShipment.WaybillNumber, 

                    CreatedAt = parcelInput.CreatedAt == default ? DateTime.UtcNow : parcelInput.CreatedAt,
                    Status = parcelInput.Status == 0 ? ParcelStatus.Pending : parcelInput.Status,
                    DispatchTrackingCode = parcelInput.DispatchTrackingCode, // Added missing property
                    DispatchedAt = parcelInput.DispatchedAt, // Added missing property
                    CreatedById = parcelInput.CreatedById.HasValue ? parcelInput.CreatedById : commonCreatedById,
                };
                
                if (parcelToProcess.CreatedById.HasValue && string.IsNullOrEmpty(parcelToProcess.CreatorLastNameSnapshot))
                {
                    if (!string.IsNullOrEmpty(newShipment.CreatorLastNameSnapshot) && parcelToProcess.CreatedById == newShipment.CreatedById) {
                         parcelToProcess.CreatorLastNameSnapshot = newShipment.CreatorLastNameSnapshot;
                    } else {
                        var parcelCreator = await _context.Users.FindAsync(parcelToProcess.CreatedById.Value);
                        if (parcelCreator != null)
                        {
                            parcelToProcess.CreatorLastNameSnapshot = parcelCreator.LastName;
                        }
                    }
                }
                
                _context.Parcels.Add(parcelToProcess);
                processedParcels.Add(parcelToProcess); 
            }

            try
            {
                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($"[CreateCartParcels] Successfully saved Shipment ID: {newShipment.Id} and {processedParcels.Count} parcels with Waybill: {commonWaybillNumber}.");
                return processedParcels;
            }
            catch (DbUpdateException dbEx) when (IsUniqueConstraintViolation(dbEx)) 
            {
                System.Diagnostics.Debug.WriteLine($"[CreateCartParcels] DbUpdateException: Unique constraint violation, likely for Shipment WaybillNumber: {commonWaybillNumber}. Error: {dbEx.Message}");
                if (dbEx.InnerException != null) System.Diagnostics.Debug.WriteLine($"[CreateCartParcels] Inner Exception: {dbEx.InnerException.Message}");
                throw new InvalidOperationException($"The common WaybillNumber '{commonWaybillNumber}' for the cart/shipment already exists or caused a conflict. Please try generating a new waybill.", dbEx);
            }
            catch (DbUpdateException dbEx) 
            {
                System.Diagnostics.Debug.WriteLine($"[CreateCartParcels] DbUpdateException (non-unique waybill or other DB issue): {dbEx.Message}");
                if (dbEx.InnerException != null) System.Diagnostics.Debug.WriteLine($"[CreateCartParcels] Inner Exception: {dbEx.InnerException.Message}");
                throw; 
            }
            catch (Exception ex) 
            {
                System.Diagnostics.Debug.WriteLine($"[CreateCartParcels] General Exception: {ex.Message}");
                throw;
            }
        }

        public async Task<decimal> GetTotalSalesForDateAsync(DateTime date)
        {
            return await _context.Parcels
                .Where(p => p.CreatedAt.Date == date.Date)
                .SumAsync(p => p.TotalAmount);
        }

            public async Task<IEnumerable<Parcel>> GetPendingOrdersAsync(DateTime? dateFilter = null)
        {
            var query = _context.Parcels
                .Where(p => p.Status == ParcelStatus.Pending);

            if (dateFilter.HasValue)
            {
                // Filter by the date part of CreatedAt
                query = query.Where(p => p.CreatedAt.Date == dateFilter.Value.Date);
            }

            return await query.ToListAsync();
        }

        public async Task<Parcel> GetParcelByWaybillNumberAsync(string waybillNumber)
        {
            System.Diagnostics.Debug.WriteLine($"[GetParcelByWaybillNumberAsync] Searching for Shipment with WaybillNumber: {waybillNumber}");

            var shipment = await _context.Shipments
                                         .AsNoTracking() // Remove circular reference tracking
                                         .FirstOrDefaultAsync(s => s.WaybillNumber == waybillNumber);

            if (shipment != null)
            {
                // Get first parcel for this shipment without including navigation properties
                var parcel = await _context.Parcels
                                          .AsNoTracking() // Remove circular reference tracking
                                          .FirstOrDefaultAsync(p => p.ShipmentId == shipment.Id);
                System.Diagnostics.Debug.WriteLine($"[GetParcelByWaybillNumberAsync] Found Shipment ID: {shipment.Id}. Retrieved parcel: {parcel?.Id}");
                return parcel; 
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[GetParcelByWaybillNumberAsync] No Shipment found with WaybillNumber: {waybillNumber}. Attempting fallback to denormalized Parcel.WaybillNumber.");
                // Fallback for cases where Parcel.WaybillNumber might be queried directly
                var parcelDirectly = await _context.Parcels
                                                   .AsNoTracking() // Remove circular reference tracking
                .FirstOrDefaultAsync(p => p.WaybillNumber == waybillNumber);
                if (parcelDirectly != null) {
                     System.Diagnostics.Debug.WriteLine($"[GetParcelByWaybillNumberAsync] Fallback: Found Parcel ID: {parcelDirectly.Id} by direct WaybillNumber search.");
                } else {
                     System.Diagnostics.Debug.WriteLine($"[GetParcelByWaybillNumberAsync] Fallback: No Parcel found by direct WaybillNumber search for {waybillNumber}.");
                }
                return parcelDirectly;
            }
        }

        public async Task<IEnumerable<Parcel>> GetParcelsByQRCodeAsync(string qrCode)
        {
            System.Diagnostics.Debug.WriteLine($"[GetParcelsByQRCodeAsync] Searching for Shipment with WaybillNumber (acting as QRCode): {qrCode}");
            var shipment = await _context.Shipments
                                         .AsNoTracking() // Remove circular reference tracking
                                         .FirstOrDefaultAsync(s => s.WaybillNumber == qrCode); // QRCode is Shipment's WaybillNumber

            if (shipment != null)
            {
                // Get parcels for this shipment without including navigation properties
                var parcels = await _context.Parcels
                                           .AsNoTracking() // Remove circular reference tracking
                                           .Where(p => p.ShipmentId == shipment.Id)
                                           .ToListAsync();
                System.Diagnostics.Debug.WriteLine($"[GetParcelsByQRCodeAsync] Found Shipment ID: {shipment.Id}. Returning {parcels.Count} parcels.");
                return parcels;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[GetParcelsByQRCodeAsync] No Shipment found for QRCode (WaybillNumber): {qrCode}. Returning empty list.");
                return Enumerable.Empty<Parcel>();
            }
        }

        public async Task<int> GetParcelCountForDateAsync(DateTime date)
        {
            return await _context.Parcels
                .CountAsync(p => p.CreatedAt.Date == date.Date);
        }

        public async Task DispatchParcelAsync(Parcel parcel)
        {
            var existingParcel = await _context.Parcels.FindAsync(parcel.Id);
            if (existingParcel != null)
            {
                existingParcel.Status = ParcelStatus.InTransit;
                existingParcel.DispatchedAt = DateTime.UtcNow;
                existingParcel.DispatchTrackingCode = parcel.DispatchTrackingCode;
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateParcelStatusAsync(Guid parcelId, ParcelStatus status, int? confirmedById = null, int? deliveredById = null)
        {
            var parcel = await _context.Parcels.FindAsync(parcelId);
            
            if (parcel == null)
            {
                throw new Exception($"Parcel with ID {parcelId} not found");
            }
            
            parcel.Status = status;
            
            if (confirmedById.HasValue)
            {
                parcel.ConfirmedById = confirmedById.Value;
                parcel.ConfirmedAt = DateTime.UtcNow;
            }

            if (status == ParcelStatus.Delivered)
            {
                parcel.DeliveredAt = DateTime.UtcNow;
                if (deliveredById.HasValue)
                {
                    parcel.DeliveredById = deliveredById.Value;
                }
            }
            
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetParcelCountAsync(DateTime date)
        {
            return await _context.Parcels
                .Where(p => p.CreatedAt.Date == date.Date)
                .CountAsync();
        }

        public async Task<int> GetDeliveredParcelCountAsync(DateTime date)
        {
            return await _context.Parcels
                .Where(p => p.CreatedAt.Date == date.Date && p.Status == ParcelStatus.Delivered)
                .CountAsync();
        }

        public async Task<double> GetTotalSalesAsync(DateTime date)
        {
            var totalSales = await _context.Parcels
                .Where(p => p.CreatedAt.Date == date.Date)
                .SumAsync(p => (double)p.TotalAmount);
            
            return totalSales;
        }

        // Placeholder implementation - assumes Parcel has an IsSmsSent property
        public async Task<bool> CheckSmsNotificationSentAsync(Guid parcelId)
        {
            var parcel = await _context.Parcels.FindAsync(parcelId);
            // return parcel?.IsSmsSent ?? false; // Requires IsSmsSent property on Parcel model
            return await Task.FromResult(false); // Temporary return until IsSmsSent is added
        }

        // Placeholder implementation - assumes Parcel has an IsSmsSent property
        public async Task MarkSmsNotificationSentAsync(Guid parcelId)
        {
            var parcel = await _context.Parcels.FindAsync(parcelId);
            if (parcel != null)
            {
                // parcel.IsSmsSent = true; // Requires IsSmsSent property on Parcel model
                // _context.Parcels.Update(parcel);
                // await _context.SaveChangesAsync();
            }
            await Task.CompletedTask; // Temporary return until IsSmsSent is added
        }

        public async Task<IEnumerable<Parcel>> GetParcelsByUserAsync(int userId)
        {
            return await _context.Parcels
                .Where(p => p.CreatedById == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }
        
        public async Task<double> GetAmountOwedByUserAsync(int userId, DateTime date)
        {
            var startOfDay = date.Date.ToUniversalTime();
            var endOfDay = startOfDay.AddDays(1);
            var total = await _context.Parcels
                .Where(p => p.CreatedById == userId &&
                           p.CreatedAt >= startOfDay &&
                           p.CreatedAt < endOfDay &&
                           p.PaymentMethods.ToLower().Contains("paid"))
                .SumAsync(p => (double)p.TotalAmount);
            return total;
        }
        
        public async Task<double> GetCashInByUserAsync(int userId, DateTime date)
        {
            // Use the ParcelDeposits clerk-summary endpoint logic directly
            // Calculate monthly clerk debit for current month + 5 days
            var startDate = new DateTime(date.Year, date.Month, 1); // First day of current month
            var endDate = startDate.AddMonths(1).AddDays(5); // End of month + 5 days
            
            // Get ParcelDeposits for the user in the date range
            var query = _context.ParcelDeposits
                .Include(pd => pd.Parcel)
                .ThenInclude(p => p.CreatedBy)
                .Where(pd => pd.Parcel.CreatedById == userId &&
                           pd.Parcel.CreatedAt >= startDate &&
                           pd.Parcel.CreatedAt < endDate &&
                           pd.Parcel.PaymentMethods.ToLower().Contains("paid"));

            var deposits = await query.ToListAsync();
            
            if (!deposits.Any())
            {
                // If no deposits exist, check for paid parcels without deposits
                var paidParcelsWithoutDeposits = await _context.Parcels
                    .Where(p => p.CreatedById == userId && 
                               p.CreatedAt >= startDate && 
                               p.CreatedAt < endDate && 
                               p.PaymentMethods.ToLower().Contains("paid"))
                    .ToListAsync();
                
                if (!paidParcelsWithoutDeposits.Any())
                    return 0;
                    
                // Return total collected amount as debit (no deposits made)
                return paidParcelsWithoutDeposits.Sum(p => (double)p.TotalAmount);
            }
            
            // Calculate debit: Total Collected - Total Deposited + Total Expenses
            var totalCollected = deposits.Sum(pd => (double)pd.Parcel.TotalAmount);
            var totalDeposited = deposits.Sum(pd => (double)pd.DepositedAmount);
            var totalExpenses = deposits.Sum(pd => (double)pd.Expenses);
            
            var clerkDebit = totalCollected - totalDeposited + totalExpenses;
            
            return clerkDebit;
        }
        
        public async Task<double> GetDailySalesByUserAsync(int userId, DateTime date)
        {
            var startOfDay = date.Date.ToUniversalTime();
            var endOfDay = startOfDay.AddDays(1);
            var total = await _context.Parcels
                .Where(p => p.CreatedById == userId &&
                           p.CreatedAt >= startOfDay &&
                           p.CreatedAt < endOfDay)
                .SumAsync(p => (double)p.TotalAmount);
            return total;
        }
        
        public async Task<double> GetMonthlySalesByUserAsync(int userId, DateTime date)
        {
            var startOfMonth = new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endOfMonth = startOfMonth.AddMonths(1);
            var total = await _context.Parcels
                .Where(p => p.CreatedById == userId &&
                           p.CreatedAt >= startOfMonth &&
                           p.CreatedAt < endOfMonth &&
                           !p.PaymentMethods.ToLower().Contains("contract"))
                .SumAsync(p => (double)p.TotalAmount);
            return total;
        }

        // Missing methods from IParcelService interface
        public async Task<Parcel> UpdateParcelAsync(Parcel parcel)
        {
            var existingParcel = await _context.Parcels.FindAsync(parcel.Id);
            if (existingParcel == null)
            {
                throw new Exception($"Parcel with ID {parcel.Id} not found");
            }

            // Update properties
            existingParcel.Sender = parcel.Sender;
            existingParcel.SenderTelephone = parcel.SenderTelephone;
            existingParcel.Receiver = parcel.Receiver;
            existingParcel.ReceiverTelephone = parcel.ReceiverTelephone;
            existingParcel.Destination = parcel.Destination;
            existingParcel.Description = parcel.Description;
            existingParcel.Quantity = parcel.Quantity;
            existingParcel.Amount = parcel.Amount;
            existingParcel.TotalAmount = parcel.TotalAmount;
            existingParcel.PaymentMethods = parcel.PaymentMethods;
            existingParcel.Status = parcel.Status;

            await _context.SaveChangesAsync();
            return existingParcel;
        }

        public async Task<IEnumerable<Parcel>> GetParcelsReadyForDispatchAsync()
        {
            return await _context.Parcels
                .Where(p => p.Status == ParcelStatus.Pending || p.Status == ParcelStatus.Confirmed)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Parcel>> GetParcelsForDispatchAsync(string? destination = null, 
            List<ParcelStatus>? statuses = null, DateTime? fromDate = null, DateTime? toDate = null, 
            string? createdByUsername = null)
        {
            var query = _context.Parcels
                .Include(p => p.CreatedBy)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(destination))
            {
                query = query.Where(p => p.Destination == destination);
            }

            if (statuses != null && statuses.Any())
            {
                query = query.Where(p => statuses.Contains(p.Status));
            }
            else
            {
                // Default to pending and confirmed for dispatch
                query = query.Where(p => p.Status == ParcelStatus.Pending || p.Status == ParcelStatus.Confirmed);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(p => p.CreatedAt.Date >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(p => p.CreatedAt.Date <= toDate.Value.Date);
            }

            if (!string.IsNullOrEmpty(createdByUsername))
            {
                query = query.Where(p => p.CreatedBy != null && p.CreatedBy.Username == createdByUsername);
            }

            return await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Dispatch> CreateDispatchAsync(Dispatch dispatch)
        {
            // Generate a new UUID for the database ID (primary key)
            dispatch.Id = Guid.NewGuid();
            // Preserve the DispatchCode sent from client (e.g., "KRC-20250716-DSIF1Y6")
            // If no DispatchCode provided, use the generated UUID as fallback
            if (string.IsNullOrEmpty(dispatch.DispatchCode))
            {
                dispatch.DispatchCode = dispatch.Id.ToString();
            }
            dispatch.DispatchTime = DateTime.UtcNow;
            dispatch.Status = "in_transit";

            // Add dispatch to context
            _context.Dispatches.Add(dispatch);

            // Update parcel statuses to "In Transit"
            if (dispatch.ParcelIds != null && dispatch.ParcelIds.Any())
            {
                var parcels = await _context.Parcels
                    .Where(p => dispatch.ParcelIds.Contains(p.Id))
                    .ToListAsync();

                foreach (var parcel in parcels)
                {
                    parcel.Status = ParcelStatus.InTransit;
                    parcel.DispatchedAt = DateTime.UtcNow;
                    parcel.DispatchTrackingCode = dispatch.DispatchCode;
                }
            }

            await _context.SaveChangesAsync();
            return dispatch;
        }

        public async Task<IEnumerable<Dispatch>> GetRecentDispatchesAsync()
        {
            return await _context.Dispatches
                .OrderByDescending(d => d.DispatchTime)
                .Take(50) // Get last 50 dispatches
                .ToListAsync();
        }

        public async Task<IEnumerable<string>> GetDestinationsAsync()
        {
            return await _context.Parcels
                .Where(p => !string.IsNullOrEmpty(p.Destination))
                .Select(p => p.Destination)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();
        }
        
        public async Task<IEnumerable<Branch>> GetAllBranchesAsync()
        {
            return await _context.Branches
                .OrderBy(b => b.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Parcel>> GetAllParcelsAsync()
        {
            return await _context.Parcels
                .Include(p => p.CreatedBy)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<PaginatedResponse<Parcel>> GetParcelsPagedAsync(ParcelQueryParams queryParams)
        {
            var query = _context.Parcels
                .Include(p => p.CreatedBy)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(queryParams.Destination))
            {
                query = query.Where(p => p.Destination == queryParams.Destination);
            }

            if (queryParams.Status.HasValue)
            {
                query = query.Where(p => p.Status == queryParams.Status.Value);
            }
            else if (queryParams.Statuses != null && queryParams.Statuses.Any())
            {
                query = query.Where(p => queryParams.Statuses.Contains(p.Status));
            }

            if (queryParams.FromDate.HasValue)
            {
                var fromUtc = queryParams.FromDate.Value.Date.ToUniversalTime();
                query = query.Where(p => p.CreatedAt >= fromUtc);
            }

            if (queryParams.ToDate.HasValue)
            {
                var toUtc = queryParams.ToDate.Value.Date.AddDays(1).ToUniversalTime();
                query = query.Where(p => p.CreatedAt < toUtc);
            }

            if (!string.IsNullOrWhiteSpace(queryParams.CreatedByUsername))
            {
                query = query.Where(p => p.CreatedBy != null && p.CreatedBy.Username == queryParams.CreatedByUsername);
            }

            // Get total count
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / queryParams.PageSize);

            // Apply pagination
            var parcels = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((queryParams.Page - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToListAsync();

            return new PaginatedResponse<Parcel>
            {
                Data = parcels,
                Page = queryParams.Page,
                PageSize = queryParams.PageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = queryParams.Page < totalPages,
                HasPreviousPage = queryParams.Page > 1
            };
        }

        public async Task<PaginatedResponse<Parcel>> GetParcelsPagedByUserAsync(int userId, ParcelQueryParams queryParams)
        {
            var query = _context.Parcels
                .Include(p => p.CreatedBy)
                .Where(p => p.CreatedById == userId)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(queryParams.Destination))
            {
                query = query.Where(p => p.Destination == queryParams.Destination);
            }

            if (queryParams.Status.HasValue)
            {
                query = query.Where(p => p.Status == queryParams.Status.Value);
            }
            else if (queryParams.Statuses != null && queryParams.Statuses.Any())
            {
                query = query.Where(p => queryParams.Statuses.Contains(p.Status));
            }

            if (queryParams.FromDate.HasValue)
            {
                var fromUtc = queryParams.FromDate.Value.Date.ToUniversalTime();
                query = query.Where(p => p.CreatedAt >= fromUtc);
            }

            if (queryParams.ToDate.HasValue)
            {
                var toUtc = queryParams.ToDate.Value.Date.AddDays(1).ToUniversalTime();
                query = query.Where(p => p.CreatedAt < toUtc);
            }

            if (!string.IsNullOrWhiteSpace(queryParams.CreatedByUsername))
            {
                query = query.Where(p => p.CreatedBy != null && p.CreatedBy.Username == queryParams.CreatedByUsername);
            }

            // Get total count
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / queryParams.PageSize);

            // Apply pagination
            var parcels = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((queryParams.Page - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToListAsync();

            return new PaginatedResponse<Parcel>
            {
                Data = parcels,
                Page = queryParams.Page,
                PageSize = queryParams.PageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = queryParams.Page < totalPages,
                HasPreviousPage = queryParams.Page > 1
            };
        }

        public async Task<IEnumerable<string>> GetUniqueDestinationsAsync(ParcelStatus? status = null)
        {
            var query = _context.Parcels.AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(p => p.Status == status.Value);
            }

            return await query
                .Where(p => !string.IsNullOrEmpty(p.Destination))
                .Select(p => p.Destination)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();
        }

        public async Task<IEnumerable<string>> GetUniqueDestinationsByUserAsync(int userId, ParcelStatus? status = null)
        {
            var query = _context.Parcels
                .Where(p => p.CreatedById == userId)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(p => p.Status == status.Value);
            }

            return await query
                .Where(p => !string.IsNullOrEmpty(p.Destination))
                .Select(p => p.Destination)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();
        }

        public async Task<IEnumerable<string>> GetUniqueClerksByDestinationAsync(string destination, ParcelStatus? status = null)
        {
            var query = _context.Parcels
                .Include(p => p.CreatedBy)
                .Where(p => p.Destination == destination && p.CreatedBy != null)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(p => p.Status == status.Value);
            }

            return await query
                .Select(p => p.CreatedBy!.Username)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        public async Task<IEnumerable<string>> GetUniqueClerksByDestinationAndUserAsync(int userId, string destination, ParcelStatus? status = null)
        {
            var query = _context.Parcels
                .Include(p => p.CreatedBy)
                .Where(p => p.CreatedById == userId && p.Destination == destination && p.CreatedBy != null)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(p => p.Status == status.Value);
            }

            return await query
                .Select(p => p.CreatedBy!.Username)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        public async Task<ClerkCashInSummaryDto?> GetClerkCashInSummaryAsync(int userId, DateTime date)
        {
            var startOfCurrentMonth = new DateTime(date.Year, date.Month, 1);
            var startOfPreviousMonth = startOfCurrentMonth.AddMonths(-1);
            var startOfNextMonth = startOfCurrentMonth.AddMonths(1);

            DateTime startDate, endDate;

            if (date.Day <= 7)
            {
                // Within the 7-day grace period: show previous month + current month up to today
                startDate = startOfPreviousMonth;
                endDate = startOfNextMonth;
            }
            else
            {
                // Past grace period: show current month + 7 days into next month
                startDate = startOfCurrentMonth;
                endDate = startOfNextMonth.AddDays(7);
            }

            var paidParcels = await _context.Parcels
                .Include(p => p.CreatedBy)
                .Where(p => p.CreatedById == userId &&
                            p.CreatedAt >= startDate &&
                            p.CreatedAt < endDate &&
                            p.PaymentMethods.ToLower().Contains("paid") &&
                            !p.PaymentMethods.ToLower().Contains("contract"))
                .ToListAsync();

            if (!paidParcels.Any())
            {
                return null;
            }

            var totalPaid = paidParcels.Sum(p => p.TotalAmount);

            var legacyDeposited = await _context.ParcelDeposits
                .Where(pd => pd.Parcel.CreatedById == userId &&
                             pd.Parcel.CreatedAt >= startDate &&
                             pd.Parcel.CreatedAt < endDate)
                .SumAsync(pd => (decimal?)pd.DepositedAmount) ?? 0m;

            var newDeposited = await _context.DailyExpenses
                .Where(e => e.ClerkId == userId &&
                            e.Category == "clerk_deposit" &&
                            e.Status == "approved" &&
                            e.Date >= startDate &&
                            e.Date < endDate)
                .SumAsync(e => (decimal?)e.Amount) ?? 0m;

            var totalDeposited = legacyDeposited + newDeposited;

            var approvedExpenses = await _context.DailyExpenses
                .Where(e => e.ClerkId == userId &&
                            e.Category != "clerk_deposit" &&
                            e.Status == "approved" &&
                            e.Date >= startDate &&
                            e.Date < endDate)
                .SumAsync(e => (decimal?)e.Amount) ?? 0m;

            var clerkUser = paidParcels.First().CreatedBy;
            var lastExpenseDate = await _context.DailyExpenses
                .Where(e => e.ClerkId == userId && e.Date >= startDate && e.Date < endDate)
                .MaxAsync(e => (DateTime?)e.UpdatedAt);

            var summary = new ClerkCashInSummaryDto
            {
                ClerkId = userId,
                ClerkName = clerkUser != null ? $"{clerkUser.FirstName} {clerkUser.LastName}" : "Unknown",
                ClerkUsername = clerkUser?.Username ?? "unknown",
                TotalPaidAmount = totalPaid,
                TotalDeposited = totalDeposited,
                TotalExpenses = approvedExpenses,
                RemainingDebt = totalPaid - totalDeposited - approvedExpenses,
                ParcelCount = paidParcels.Count,
                LastUpdateDate = lastExpenseDate
            };

            return summary;
        }

        public async Task<BranchManagerDashboardDto> GetBranchManagerDashboardAsync(string branchName, DateTime date)
        {
            var yesterday = date.Date.AddDays(-1);
            var yesterdayStart = yesterday.ToUniversalTime();
            var yesterdayEnd = yesterday.AddDays(1).ToUniversalTime();
            var todayStart = date.Date.ToUniversalTime();
            var todayEnd = todayStart.AddDays(1);
            var monthStart = new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var branchNameLower = branchName.ToLower();

            var previousDayCodSales = await _context.Parcels
                .Where(p => p.Destination.ToLower() == branchNameLower &&
                            p.PaymentMethods.ToLower().Contains("cod") &&
                            p.CreatedAt >= yesterdayStart &&
                            p.CreatedAt < yesterdayEnd)
                .SumAsync(p => (decimal?)p.TotalAmount) ?? 0m;

            var previousDayDeposited = await _context.BranchDeposits
                .Where(bd => bd.Branch.ToLower() == branchNameLower &&
                             bd.Date == yesterday.Date)
                .SumAsync(bd => (decimal?)bd.DepositAmount) ?? 0m;

            var previousDayCodAmountOwed = previousDayCodSales - previousDayDeposited;
            if (previousDayCodAmountOwed < 0) previousDayCodAmountOwed = 0m;

            var todayLiveSales = await _context.Parcels
                .Where(p => p.Destination.ToLower() == branchNameLower &&
                            p.CreatedAt >= todayStart &&
                            p.CreatedAt < todayEnd)
                .SumAsync(p => (decimal?)p.TotalAmount) ?? 0m;

            var monthlyCodTotal = await _context.Parcels
                .Where(p => p.Destination.ToLower() == branchNameLower &&
                            p.PaymentMethods.ToLower().Contains("cod") &&
                            p.CreatedAt >= monthStart)
                .SumAsync(p => (decimal?)p.TotalAmount) ?? 0m;
            var monthlyDeposited = await _context.BranchDeposits
                .Where(bd => bd.Branch.ToLower() == branchNameLower &&
                             bd.Date >= monthStart.Date)
                .SumAsync(bd => (decimal?)bd.DepositAmount) ?? 0m;
            var monthlyApprovedExpenses = await _context.DailyExpenses
                .Where(e => e.BranchName.ToLower() == branchNameLower &&
                            e.Date >= monthStart.Date &&
                            e.Status == "approved")
                .SumAsync(e => (decimal?)e.Amount) ?? 0m;
            var monthlyDebit = monthlyCodTotal - monthlyDeposited - monthlyApprovedExpenses;

            return new BranchManagerDashboardDto
            {
                PreviousDayCodSales = previousDayCodSales,
                PreviousDayCodAmountOwed = previousDayCodAmountOwed,
                TodayLiveSales = todayLiveSales,
                MonthlyDebit = monthlyDebit
            };
        }

        private static DateTime GetLastBookingDay(DateTime date)
        {
            var day = date.Date.AddDays(-1);
            while (day.DayOfWeek == DayOfWeek.Sunday)
                day = day.AddDays(-1);
            return day;
        }

        public async Task<BranchCodLockStatusDto> GetBranchCodLockStatusAsync(string branchName, DateTime date)
        {
            if (date.Date.DayOfWeek == DayOfWeek.Monday)
                return new BranchCodLockStatusDto { IsLocked = false, Reason = "none" };

            var declarationDate = GetLastBookingDay(date);
            var codDate = GetLastBookingDay(declarationDate);
            var codDateStart = DateTime.SpecifyKind(codDate, DateTimeKind.Utc);
            var codDateEnd = DateTime.SpecifyKind(codDate.AddDays(1), DateTimeKind.Utc);
            var declarationDateLocal = DateTime.SpecifyKind(declarationDate, DateTimeKind.Unspecified);
            var declarationDateLocalEnd = DateTime.SpecifyKind(declarationDate.AddDays(1), DateTimeKind.Unspecified);
            var codDateLocal = DateTime.SpecifyKind(codDate, DateTimeKind.Unspecified);
            var codDateLocalEnd = DateTime.SpecifyKind(codDate.AddDays(1), DateTimeKind.Unspecified);
            var branchNameLower = branchName.ToLower();

            var codTotal = await _context.Parcels
                .Where(p => p.Destination.ToLower() == branchNameLower &&
                            p.PaymentMethods.ToLower().Contains("cod") &&
                            p.CreatedAt >= codDateStart &&
                            p.CreatedAt < codDateEnd)
                .SumAsync(p => (decimal?)p.TotalAmount) ?? 0m;

            var dto = new BranchCodLockStatusDto
            {
                PreviousDate = declarationDate,   // T-1: the declaration day (shown in UI)
                CodDate = codDate,                // T-2: the parcel booking day (deposit/expense date)
                PreviousDayCodTotal = codTotal
            };

            if (codTotal <= 0)
            {
                dto.IsLocked = false;
                dto.Reason = "none";
                return dto;
            }

            // Deposit may be dated T-2 (from lock overlay) or T-1 (from report view)
            var depositRecord = await _context.BranchDeposits
                .Where(bd => bd.Branch.ToLower() == branchNameLower &&
                             (bd.Date >= codDateLocal && bd.Date < codDateLocalEnd ||
                              bd.Date >= declarationDateLocal && bd.Date < declarationDateLocalEnd))
                .OrderByDescending(bd => bd.CreatedAt)
                .FirstOrDefaultAsync();

            if (depositRecord == null)
            {
                dto.IsLocked = true;
                dto.Reason = "pending_submission";
                return dto;
            }

            dto.DepositRecordId = depositRecord.Id;
            dto.DepositStatus = depositRecord.Status;
            dto.DepositAmount = depositRecord.DepositAmount;
            dto.DepositRejectionReason = depositRecord.RejectionReason;

            // Expense may be dated T-1 (from lock overlay) or T-2 (from report view)
            var expenseRecord = await _context.DailyExpenses
                .Where(e => e.BranchName.ToLower() == branchNameLower &&
                            e.Category == "cod_expense" &&
                            (e.Date >= declarationDateLocal && e.Date < declarationDateLocalEnd ||
                             e.Date >= codDateLocal && e.Date < codDateLocalEnd))
                .OrderByDescending(e => e.CreatedAt)
                .FirstOrDefaultAsync();

            if (expenseRecord != null)
            {
                dto.ExpenseRecordId = expenseRecord.Id;
                dto.ExpenseStatus = expenseRecord.Status;
                dto.ExpenseAmount = expenseRecord.Amount;
                dto.ExpenseRejectionReason = expenseRecord.RejectionReason;
            }

            if (depositRecord.Status == "rejected" || expenseRecord?.Status == "rejected")
            {
                dto.IsLocked = true;
                dto.Reason = "rejected";
                return dto;
            }

            dto.IsLocked = false;
            dto.Reason = depositRecord.Status == "pending" ? "pending_approval" : "none";
            return dto;
        }

        public async Task<ClerkLockStatusDto> GetClerkLockStatusAsync(int userId, DateTime date)
        {
            var yesterday = GetLastBookingDay(date);
            var yesterdayStart = DateTime.SpecifyKind(yesterday, DateTimeKind.Utc);
            var yesterdayEnd = DateTime.SpecifyKind(yesterday.AddDays(1), DateTimeKind.Utc);
            var yesterdayLocal = DateTime.SpecifyKind(yesterday, DateTimeKind.Unspecified);
            var yesterdayLocalEnd = DateTime.SpecifyKind(yesterday.AddDays(1), DateTimeKind.Unspecified);

            var yesterdayPaidTotal = await _context.Parcels
                .Where(p => p.CreatedById == userId &&
                            p.PaymentMethods.ToLower().Contains("paid") &&
                            !p.PaymentMethods.ToLower().Contains("contract") &&
                            p.CreatedAt >= yesterdayStart &&
                            p.CreatedAt < yesterdayEnd)
                .SumAsync(p => p.TotalAmount);

            var dto = new ClerkLockStatusDto
            {
                PreviousDate = yesterday,
                YesterdayPaidTotal = yesterdayPaidTotal
            };

            if (yesterdayPaidTotal <= 0)
            {
                dto.IsLocked = false;
                dto.Reason = "none";
                return dto;
            }

            // Check for web panel deposit (ParcelDeposits) for yesterday's parcels
            var webDepositTotal = await _context.ParcelDeposits
                .Where(pd => pd.Parcel.CreatedById == userId &&
                             pd.Parcel.CreatedAt >= yesterdayStart &&
                             pd.Parcel.CreatedAt < yesterdayEnd)
                .SumAsync(pd => pd.DepositedAmount);

            var depositRecord = await _context.DailyExpenses
                .Where(e => e.ClerkId == userId &&
                            e.Category == "clerk_deposit" &&
                            e.Date >= yesterdayLocal && e.Date < yesterdayLocalEnd)
                .OrderByDescending(e => e.CreatedAt)
                .FirstOrDefaultAsync();

            if (depositRecord != null)
            {
                dto.DepositRecordId = depositRecord.Id;
                dto.DepositStatus = depositRecord.Status;
                dto.DepositAmount = depositRecord.Amount;
                dto.DepositRejectionReason = depositRecord.RejectionReason;
            }

            var expenseRecords = await _context.DailyExpenses
                .Where(e => e.ClerkId == userId &&
                            e.Category != "clerk_deposit" &&
                            e.Date >= yesterdayLocal && e.Date < yesterdayLocalEnd)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            var latestExpense = expenseRecords.FirstOrDefault();
            if (latestExpense != null)
            {
                dto.ExpenseRecordId = latestExpense.Id;
                dto.ExpenseStatus = latestExpense.Status;
                dto.ExpenseAmount = latestExpense.Amount;
                dto.ExpenseRejectionReason = latestExpense.RejectionReason;
            }

            dto.TotalApprovedExpenses = expenseRecords
                .Where(e => e.Status == "approved")
                .Sum(e => e.Amount);

            var hasRejection = expenseRecords.Any(e => e.Status == "rejected") || depositRecord?.Status == "rejected";

            var activeDepositAmount = (depositRecord != null && depositRecord.Status != "rejected") ? depositRecord.Amount : 0m;
            var activeDepositTotal = webDepositTotal + activeDepositAmount;
            var activeExpenseTotal = expenseRecords.Where(e => e.Status != "rejected").Sum(e => e.Amount);
            var activeTotalDeclared = activeDepositTotal + activeExpenseTotal;

            // Rule 4: nothing active declared for yesterday
            if (webDepositTotal <= 0 && depositRecord == null && !expenseRecords.Any())
            {
                dto.IsLocked = true;
                dto.Reason = "pending_submission";
                return dto;
            }

            // Rules 1 & 2: active (non-rejected) declarations cover yesterdayPaidTotal
            if (activeTotalDeclared >= yesterdayPaidTotal)
            {
                dto.IsLocked = false;
                var allApproved = (depositRecord == null || depositRecord.Status == "approved") &&
                                  expenseRecords.Where(e => e.Status != "rejected").All(e => e.Status == "approved");
                dto.Reason = allApproved ? "none" : "pending_approval";
                return dto;
            }

            // Rule 3: has rejections and active declarations don't cover the total
            if (hasRejection)
            {
                dto.IsLocked = true;
                dto.Reason = "rejected";
                return dto;
            }

            // Declared but not enough to cover yesterdayPaidTotal
            dto.IsLocked = true;
            dto.Reason = "pending_submission";
            return dto;
        }

        public async Task<ClerkReportDto> GetClerkReportAsync(int userId, DateTime date)
        {
            var dto = new ClerkReportDto { ReportDate = date.Date };

            var user = await _context.Users.FindAsync(userId);
            dto.ClerkName = user != null
                ? (!string.IsNullOrWhiteSpace(user.FirstName) ? user.FirstName : user.Username)
                : string.Empty;

            var dayStart = date.Date.ToUniversalTime();
            var dayEnd = dayStart.AddDays(1);

            dto.DailyParcelCount = await _context.Parcels
                .CountAsync(p => p.CreatedById == userId &&
                                 p.CreatedAt >= dayStart && p.CreatedAt < dayEnd &&
                                 !p.PaymentMethods.ToLower().Contains("contract"));

            dto.DailySalesTotal = await _context.Parcels
                .Where(p => p.CreatedById == userId &&
                            p.CreatedAt >= dayStart && p.CreatedAt < dayEnd &&
                            !p.PaymentMethods.ToLower().Contains("contract"))
                .SumAsync(p => (decimal?)p.TotalAmount) ?? 0m;

            var monthStart = new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthEnd = monthStart.AddMonths(1);

            dto.MonthlyTotal = await _context.Parcels
                .Where(p => p.CreatedById == userId &&
                            p.CreatedAt >= monthStart && p.CreatedAt < monthEnd &&
                            !p.PaymentMethods.ToLower().Contains("contract"))
                .SumAsync(p => (decimal?)p.TotalAmount) ?? 0m;

            var allClerkTotals = await _context.Parcels
                .Where(p => p.CreatedById.HasValue &&
                            p.CreatedAt >= monthStart && p.CreatedAt < monthEnd &&
                            !p.PaymentMethods.ToLower().Contains("contract"))
                .GroupBy(p => p.CreatedById)
                .Select(g => new { ClerkId = g.Key, Total = g.Sum(p => (decimal?)p.TotalAmount) ?? 0m })
                .OrderByDescending(g => g.Total)
                .ToListAsync();

            dto.TotalClerksRanked = allClerkTotals.Count;
            var rankIndex = allClerkTotals.FindIndex(g => g.ClerkId == userId);
            dto.MonthlyRank = rankIndex >= 0 ? rankIndex + 1 : dto.TotalClerksRanked;

            var reportDateLocal = DateTime.SpecifyKind(date.Date, DateTimeKind.Unspecified);
            var reportDateLocalEnd = DateTime.SpecifyKind(date.Date.AddDays(1), DateTimeKind.Unspecified);

            var deposits = await _context.DailyExpenses
                .Where(e => e.ClerkId == userId &&
                            e.Category == "clerk_deposit" &&
                            e.Date >= reportDateLocal && e.Date < reportDateLocalEnd)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            dto.Deposits = deposits.Select(e => new ClerkReportExpenseItem
            {
                Id = e.Id,
                Amount = e.Amount,
                Status = e.Status,
                Description = e.Description ?? string.Empty,
                RejectionReason = e.RejectionReason,
                Date = e.Date
            }).ToList();

            var expenses = await _context.DailyExpenses
                .Where(e => e.ClerkId == userId &&
                            e.Category != "clerk_deposit" &&
                            e.Date >= reportDateLocal && e.Date < reportDateLocalEnd)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            dto.Expenses = expenses.Select(e => new ClerkReportExpenseItem
            {
                Id = e.Id,
                Amount = e.Amount,
                Status = e.Status,
                Description = e.Description ?? string.Empty,
                RejectionReason = e.RejectionReason,
                Date = e.Date
            }).ToList();

            return dto;
        }
    }
} 