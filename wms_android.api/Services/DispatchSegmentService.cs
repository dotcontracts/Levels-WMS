using wms_android.shared.Models;
using wms_android.shared.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace wms_android.api.Services
{
    public interface IDispatchSegmentService
    {
        Task<DispatchSegment> CreateDispatchSegmentAsync(Guid dispatchId, string destination, List<Guid> parcelIds);
        Task<List<DispatchSegment>> GetSegmentsByDispatchIdAsync(Guid dispatchId);
        Task UpdateSegmentTotalsAsync(Guid segmentId);
        Task<List<DispatchSegment>> GetSegmentsByDispatchIdCachedAsync(Guid dispatchId);
        void InvalidateDispatchCache(Guid dispatchId);
    }

    public class DispatchSegmentService : IDispatchSegmentService
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(30);

        public DispatchSegmentService(AppDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<DispatchSegment> CreateDispatchSegmentAsync(Guid dispatchId, string destination, List<Guid> parcelIds)
        {
            var parcels = await _context.Parcels
                .Where(p => parcelIds.Contains(p.Id))
                .ToListAsync();

            var segment = new DispatchSegment
            {
                Id = Guid.NewGuid(),
                DispatchId = dispatchId,
                Destination = destination,
                TotalParcels = parcels.Count,
                TotalAmount = parcels.Sum(p => p.TotalAmount),
                SequenceNumber = await GetNextSequenceNumberAsync(dispatchId)
            };

            _context.DispatchSegments.Add(segment);
            await _context.SaveChangesAsync();

            // Associate parcels with segment using the many-to-many relationship
            foreach (var parcel in parcels)
            {
                _context.Entry(segment).Collection(s => s.Parcels).Load();
                segment.Parcels.Add(parcel);
            }

            await _context.SaveChangesAsync();

            // Invalidate cache for this dispatch after creating a segment
            InvalidateDispatchCache(dispatchId);

            return segment;
        }

        public async Task<List<DispatchSegment>> GetSegmentsByDispatchIdAsync(Guid dispatchId)
        {
            var segments = await _context.DispatchSegments
                .AsNoTracking() // Remove circular reference tracking
                .Where(ds => ds.DispatchId == dispatchId)
                .OrderBy(ds => ds.SequenceNumber)
                .ToListAsync();
                
            // Load parcels separately to avoid circular references using join table
            foreach (var segment in segments)
            {
                var parcelIds = await _context.Database.SqlQuery<Guid>(
                    $"SELECT \"ParcelsId\" FROM \"DispatchSegmentParcels\" WHERE \"DispatchSegmentId\" = {segment.Id}")
                    .ToListAsync();
                    
                segment.Parcels = await _context.Parcels
                    .AsNoTracking()
                    .Where(p => parcelIds.Contains(p.Id))
                    .ToListAsync();
            }
            
            return segments;
        }

        public async Task<List<DispatchSegment>> GetSegmentsByDispatchIdCachedAsync(Guid dispatchId)
        {
            string cacheKey = $"dispatch_segments_{dispatchId}";

            // Try to get from cache first
            if (_cache.TryGetValue(cacheKey, out List<DispatchSegment>? cachedSegments))
            {
                System.Diagnostics.Debug.WriteLine($"Retrieved segments for dispatch {dispatchId} from cache");
                return cachedSegments;
            }

            // If not in cache, load from database
            System.Diagnostics.Debug.WriteLine($"Loading segments for dispatch {dispatchId} from database");
            var segments = await GetSegmentsByDispatchIdAsync(dispatchId);

            // Store in cache
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheDuration,
                SlidingExpiration = TimeSpan.FromMinutes(10) // Extend cache on access
            };

            _cache.Set(cacheKey, segments, cacheOptions);

            return segments;
        }

        public async Task UpdateSegmentTotalsAsync(Guid segmentId)
        {
            var segment = await _context.DispatchSegments
                .FirstOrDefaultAsync(ds => ds.Id == segmentId);

            if (segment != null)
            {
                // Get parcel statistics without loading navigation properties using join table
                var parcelIds = await _context.Database.SqlQuery<Guid>(
                    $"SELECT \"ParcelsId\" FROM \"DispatchSegmentParcels\" WHERE \"DispatchSegmentId\" = {segmentId}")
                    .ToListAsync();
                    
                var parcelStats = await _context.Parcels
                    .Where(p => parcelIds.Contains(p.Id))
                    .GroupBy(p => 1) // Group by constant since we want aggregate of all matching parcels
                    .Select(g => new { Count = g.Count(), TotalAmount = g.Sum(p => p.TotalAmount) })
                    .FirstOrDefaultAsync();
                    
                segment.TotalParcels = parcelStats?.Count ?? 0;
                segment.TotalAmount = parcelStats?.TotalAmount ?? 0;
                await _context.SaveChangesAsync();

                // Invalidate cache for this dispatch after updating
                InvalidateDispatchCache(segment.DispatchId);
            }
        }

        public void InvalidateDispatchCache(Guid dispatchId)
        {
            string cacheKey = $"dispatch_segments_{dispatchId}";
            _cache.Remove(cacheKey);
            System.Diagnostics.Debug.WriteLine($"Invalidated cache for dispatch {dispatchId}");
        }

        private async Task<int> GetNextSequenceNumberAsync(Guid dispatchId)
        {
            var lastSequence = await _context.DispatchSegments
                .Where(ds => ds.DispatchId == dispatchId)
                .OrderByDescending(ds => ds.SequenceNumber)
                .Select(ds => ds.SequenceNumber)
                .FirstOrDefaultAsync();

            return lastSequence + 1;
        }
    }
}