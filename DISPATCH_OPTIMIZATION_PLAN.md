# Dispatch Optimization Plan

## Current Architecture Analysis

### API Layer (wms_android.api)
- **Dispatch Model**: Uses `Dispatch` entity with `DispatchSegment` support for multi-destination dispatches
- **Segment Support**: Already implements `DispatchSegmentService` to handle parcels grouped by destination
- **Key Endpoints**:
  - `POST /api/Dispatches/create` - Creates dispatch with automatic segment grouping
  - `GET /api/Dispatches/branch/{branch}` - Filters dispatches by branch
  - `GET /api/Dispatches/{id}/note` - Returns dispatch with full parcel details

### Android Layer (wms_android)
- **DispatchViewModel**: Has `AllowMultipleDestinations` flag but currently requires destination selection
- **Parcel Loading**: Uses caching and filters but still loads parcels per destination
- **Key Issue**: Requires destination to be selected before loading parcels

### Web Layer (wms_web.dispatch.react)
- **CreateDispatch**: Has multi-destination checkbox and logic for grouping parcels
- **Parcel Filtering**: Advanced filtering system with branch-based filtering
- **Virtual Scrolling**: Uses `VirtualScrollTable` for performance optimization

## Identified Bottlenecks & Issues

1. **Multiple Dispatch Creation Problem**: Currently, users must create separate dispatches for different destinations, creating unnecessary database records
2. **Performance Bottleneck**: Loading parcels per destination causes multiple API calls
3. **User Workflow Friction**: Need to cancel dispatch to switch destinations
4. **Cache Invalidation**: Current cache system could be optimized for multi-destination scenarios
5. **Parcel Loading Delays**: Large parcel lists take time to load and render
6. **Memory Usage**: Loading all parcels at once can cause performance issues on mobile devices

## Implementation Plan

### Phase 1: API Optimization

#### 1.1 Enhanced Batch Loading
- Create endpoint `/api/Parcels/dispatch-multi-dest` to fetch all dispatchable parcels across destinations
- Support pagination and filtering for large datasets
- Include branch and destination metadata for efficient client-side grouping

#### 1.2 Optimize Segment Queries
- Add caching for dispatch segments to reduce database hits
- Implement batch loading of segments when viewing dispatch details
- Add composite indexes for segment queries

#### 1.3 Add Summary Endpoints
- Create `/api/Dispatches/summary` for quick dispatch overview
- Create `/api/Parcels/dispatch-stats` for parcel statistics
- Implement endpoint caching for frequently accessed summary data

#### 1.4 Database Optimization
- Add composite indexes on (Destination, Status, CreatedAt)
- Optimize N+1 query issues in dispatch operations
- Implement database connection pooling improvements

### Phase 2: Android Enhancement

#### 2.1 Remove Destination Dependency
- Load all pending/confirmed parcels regardless of destination
- Implement progressive loading with pagination
- Add offline support for cached parcel data

#### 2.2 Smart Grouping
- Automatically group parcels by destination in UI
- Show summary cards for each destination group
- Implement collapsible destination groups for better navigation

#### 2.3 Batch Selection
- Allow bulk selection across destinations
- Add "Select All" per destination functionality
- Implement exclusion filters for quick deselection

#### 2.4 Progressive Loading
- Implement pagination for large parcel lists
- Add infinite scroll support
- Optimize memory usage with parcel recycling

### Phase 3: Web Enhancement

#### 3.1 Unified Parcel Loading
- Load all parcels without destination constraint
- Implement server-side pagination with client-side filtering
- Add loading states for better UX

#### 3.2 Enhanced Multi-Destination UI
- Improve checkbox interface for better UX
- Add destination summary panels
- Implement drag-and-drop for parcel grouping

#### 3.3 Real-time Summary
- Show totals per destination during selection
- Add visual indicators for selection progress
- Implement quick filter shortcuts

#### 3.4 Optimized Filtering
- Add client-side destination grouping and filtering
- Implement search with debouncing
- Add filter presets for common scenarios

### Phase 4: Performance Optimization

#### 4.1 Database Indexing
- Add composite indexes for dispatch queries
- Optimize parcel status queries
- Implement query result caching

#### 4.2 API Response Caching
- Implement response caching for frequently accessed data
- Add cache invalidation strategy
- Configure appropriate cache TTL values

#### 4.3 Lazy Loading
- Load dispatch parcels on-demand in view screen
- Implement virtual scrolling for large lists
- Add image lazy loading for parcel photos

#### 4.4 Background Sync
- Sync parcel status updates in background
- Implement optimistic UI updates
- Add conflict resolution for concurrent operations

## Success Metrics

- Reduce dispatch creation time by 50%
- Reduce parcel loading time by 70%
- Improve dispatch flexibility (support 10+ destinations)
- Reduce API calls by 60%
- Improve user satisfaction scores

## Implementation Timeline

- Phase 1: 2-3 days
- Phase 2: 3-4 days
- Phase 3: 2-3 days
- Phase 4: 2-3 days

Total estimated time: 9-13 days

## Risk Mitigation

- Maintain backward compatibility with existing dispatch flow
- Implement feature flags for gradual rollout
- Add comprehensive testing for multi-destination scenarios
- Monitor performance metrics during deployment
- Prepare rollback plan for critical issues

## Rollout Strategy

1. **Sprint 1**: Phase 1 API changes (test environment)
2. **Sprint 2**: Phase 2 Android changes (internal testing)
3. **Sprint 3**: Phase 3 Web changes (staging)
4. **Sprint 4**: Phase 4 Performance optimizations (production)
5. **Post-launch**: Monitor and iterate based on user feedback