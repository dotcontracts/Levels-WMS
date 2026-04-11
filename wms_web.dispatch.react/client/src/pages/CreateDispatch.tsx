import React, { useState, useEffect } from 'react';
import { 
  Truck, 
  Package, 
  User, 
  Phone,
  MapPin, 
  CheckCircle, 
  AlertCircle, 
  RefreshCw,
  Search,
  Filter,
  Plus,
  FileText
} from 'lucide-react';
import { useAuth } from '../context/AuthContext';
import { Card, Button, Table, Input, FilterPanel, FilterField } from '../components/ui';
import { VirtualScrollTable } from '../components/VirtualScrollTable';
import { wmsApi, Parcel } from '../services/wmsApi';

interface CreateDispatchFormData {
  dispatchCode: string;
  sourceBranch: string;
  destination: string;
  driverName: string;
  driverPhone: string;
  vehicleNumber: string;
  parcelIds: string[];
  allowMultipleDestinations: boolean;
}

interface ParcelFilterState {
  search: string;
  status: string;
  destination: string;
  createdBy: string;
  dateFrom: string;
  dateTo: string;
}

const CreateDispatch: React.FC = () => {
  const { user, isAdmin, isBranchManager } = useAuth();
  const [parcels, setParcels] = useState<Parcel[]>([]);
  const [filteredParcels, setFilteredParcels] = useState<Parcel[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [showFilters, setShowFilters] = useState(false);
  const [selectedParcelIds, setSelectedParcelIds] = useState<string[]>([]);
  const [isCreating, setIsCreating] = useState(false);
  const [destinations, setDestinations] = useState<string[]>([]);
  const [availableClerks, setAvailableClerks] = useState<{ id: number; name: string; username: string }[]>([]);

  const [filters, setFilters] = useState<ParcelFilterState>({
    search: '',
    status: '', // Show all valid statuses (Pending and Confirmed) by default
    destination: '',
    createdBy: '',
    dateFrom: '',
    dateTo: ''
  });

  const [formData, setFormData] = useState<CreateDispatchFormData>({
    dispatchCode: '',
    sourceBranch: user?.branch?.name || 'Unknown Branch',
    destination: '',
    driverName: '',
    driverPhone: '',
    vehicleNumber: '',
    parcelIds: [],
    allowMultipleDestinations: true // Default to true for multi-destination support
  });

  useEffect(() => {
    fetchParcels();
  }, []);

  useEffect(() => {
    applyFilters();
  }, [parcels, filters]);

  // Update source branch when user data is loaded
  useEffect(() => {
    if (user?.branch?.name) {
      setFormData(prev => ({
        ...prev,
        sourceBranch: user.branch!.name
      }));
    }
  }, [user?.branch?.name]);

  useEffect(() => {
    // Extract unique destinations from parcels
    const destinationSet = new Set(parcels.map(p => p.destination).filter(Boolean));
    const uniqueDestinations = Array.from(destinationSet);
    setDestinations(uniqueDestinations.sort());

    // Extract unique clerks from parcels
    const clerkSet = new Set<string>();
    const clerkMap = new Map<number, { name: string; username: string }>();
    
    parcels.forEach(parcel => {
      if (parcel.createdById && parcel.createdBy?.username) {
        const clerkKey = `${parcel.createdById}:${parcel.createdBy.username}`;
        if (!clerkSet.has(clerkKey)) {
          clerkSet.add(clerkKey);
          clerkMap.set(parcel.createdById, {
            name: parcel.createdBy.username,
            username: parcel.createdBy.username
          });
        }
      }
    });

    const uniqueClerks = Array.from(clerkMap.entries()).map(([id, info]) => ({
      id,
      name: info.name,
      username: info.username
    }));
    
    setAvailableClerks(uniqueClerks.sort((a, b) => a.name.localeCompare(b.name)));
    
    console.log('CreateDispatch - Available clerks:', uniqueClerks);
  }, [parcels]);

  const fetchParcels = async (forceRefresh = false) => {
    try {
      setLoading(true);
      setError('');
      
      console.log('CreateDispatch - Fetching parcels for user:', user);
      console.log('CreateDispatch - User branch ID:', user?.branchId);
      console.log('CreateDispatch - Is branch manager:', isBranchManager());
      console.log('CreateDispatch - Multi-destination mode:', formData.allowMultipleDestinations);
      
      // Use optimized multi-destination dispatch endpoint when allowMultipleDestinations is true
      if (formData.allowMultipleDestinations) {
        const userBranchId = isBranchManager() ? user?.branchId : undefined;
        
        const data = await wmsApi.getParcelsForMultiDestDispatch({
          page: 1,
          pageSize: 500, // Larger page size for multi-destination mode
          statuses: ['0', '1'], // Pending and Confirmed
          fromDate: filters.dateFrom,
          toDate: filters.dateTo,
          createdByUsername: filters.createdBy === 'all' ? undefined : filters.createdBy,
          userBranchId: userBranchId
        });
        
        console.log('CreateDispatch - Full response data:', JSON.stringify(data, null, 2));
        console.log(`CreateDispatch - parcelsByDestination type: ${typeof data.parcelsByDestination}, isArray: ${Array.isArray(data.parcelsByDestination)}`);
        
        // Flatten parcels from destination groups for backward compatibility
        let allParcels: any[] = [];
        
        // Handle the ReferenceHandler.Preserve format with $values property
        let parcelsByDestArray: any[] = [];
        if (Array.isArray(data.parcelsByDestination)) {
          parcelsByDestArray = data.parcelsByDestination;
          console.log(`CreateDispatch - parcelsByDestination is a direct array`);
        } else if (data.parcelsByDestination && data.parcelsByDestination.$values) {
          parcelsByDestArray = data.parcelsByDestination.$values;
          console.log(`CreateDispatch - parcelsByDestination uses $values format`);
        }
        
        // Try different possible response structures
        if (parcelsByDestArray.length > 0) {
          allParcels = parcelsByDestArray.flatMap((group: any) => {
            // Handle both direct parcels array and $values format
            const parcels = group.parcels && group.parcels.$values ? group.parcels.$values : (group.parcels || []);
            return parcels;
          });
          console.log(`CreateDispatch - Flattened ${allParcels.length} parcels from ${parcelsByDestArray.length} destination groups`);
        } else if (Array.isArray(data)) {
          // Fallback: if the entire response is an array
          allParcels = data;
          console.log(`CreateDispatch - Using direct array response with ${allParcels.length} parcels`);
        } else if (data.parcels && Array.isArray(data.parcels)) {
          // Another fallback structure
          allParcels = data.parcels;
          console.log(`CreateDispatch - Using data.parcels array with ${allParcels.length} parcels`);
        } else if (data.parcels && data.parcels.$values) {
          // Handle parcels with $values format
          allParcels = data.parcels.$values;
          console.log(`CreateDispatch - Using data.parcels.$values array with ${allParcels.length} parcels`);
        } else {
          console.log('CreateDispatch - No parcels found or empty response');
          // Don't throw error for empty responses, just set empty array
        }
        
        setParcels(allParcels);
      } else {
        // Use the branch-based filtering method for single-destination mode
        const userBranchId = isBranchManager() ? user?.branchId : undefined;
        
        const data = await wmsApi.getParcelsForDispatchByBranch(userBranchId);
        console.log(`CreateDispatch - Received ${data.length} parcels for dispatch`);
        
        setParcels(data);
      }
    } catch (err) {
      console.error('CreateDispatch - Error fetching parcels:', err);
      setError('Failed to load parcels');
    } finally {
      setLoading(false);
    }
  };

  const applyFilters = () => {
    let filtered = parcels;

    // Apply filters
    if (filters.search) {
      filtered = filtered.filter(parcel =>
        parcel.waybillNumber.toLowerCase().includes(filters.search.toLowerCase()) ||
        parcel.destination.toLowerCase().includes(filters.search.toLowerCase()) ||
        parcel.sender.toLowerCase().includes(filters.search.toLowerCase()) ||
        parcel.receiver.toLowerCase().includes(filters.search.toLowerCase())
      );
    }

    if (filters.status) {
      filtered = filtered.filter(parcel => parcel.status.toString() === filters.status);
    }

    if (filters.destination) {
      filtered = filtered.filter(parcel =>
        parcel.destination.toLowerCase().includes(filters.destination.toLowerCase())
      );
    }

    if (filters.createdBy) {
      filtered = filtered.filter(parcel => {
        if (filters.createdBy === 'all' || !filters.createdBy) return true;
        
        // Filter by clerk ID if it's a number, otherwise by name
        const isNumericFilter = !isNaN(Number(filters.createdBy));
        
        if (isNumericFilter) {
          return parcel.createdById === Number(filters.createdBy);
        } else {
          return parcel.createdBy?.username?.toLowerCase().includes(filters.createdBy.toLowerCase());
        }
      });
    }

    if (filters.dateFrom) {
      filtered = filtered.filter(parcel =>
        new Date(parcel.createdAt) >= new Date(filters.dateFrom)
      );
    }

    if (filters.dateTo) {
      filtered = filtered.filter(parcel =>
        new Date(parcel.createdAt) <= new Date(filters.dateTo)
      );
    }

    setFilteredParcels(filtered);
  };

  const handleFilterChange = (key: string, value: string | { from: string; to: string }) => {
    setFilters(prev => ({ ...prev, [key]: value }));
  };

  const clearFilters = () => {
    setFilters({
      search: '',
      status: '',
      destination: '',
      createdBy: '',
      dateFrom: '',
      dateTo: ''
    });
  };

  const getActiveFilterChips = () => {
    const chips: any[] = [];

    if (filters.search) {
      chips.push({
        key: 'search',
        label: 'Search',
        value: filters.search,
        onRemove: () => handleFilterChange('search', '')
      });
    }

    if (filters.destination) {
      chips.push({
        key: 'destination',
        label: 'Destination',
        value: filters.destination,
        onRemove: () => handleFilterChange('destination', '')
      });
    }

    if (filters.createdBy) {
      chips.push({
        key: 'createdBy',
        label: 'Created by',
        value: filters.createdBy,
        onRemove: () => handleFilterChange('createdBy', '')
      });
    }

    if (filters.dateFrom) {
      chips.push({
        key: 'dateFrom',
        label: 'Date From',
        value: filters.dateFrom,
        onRemove: () => handleFilterChange('dateFrom', '')
      });
    }

    if (filters.dateTo) {
      chips.push({
        key: 'dateTo',
        label: 'Date To',
        value: filters.dateTo,
        onRemove: () => handleFilterChange('dateTo', '')
      });
    }

    return chips;
  };

  const filterFields: FilterField[] = [
    {
      key: 'search',
      label: 'Search',
      type: 'text',
      placeholder: 'Search by waybill, destination, sender, or receiver',
      icon: <Search className="w-4 h-4" />
    },
    {
      key: 'status',
      label: 'Status',
      type: 'select',
      placeholder: 'Select status',
      icon: <Package className="w-4 h-4" />,
      options: [
        { value: '', label: 'All Valid Statuses' },
        { value: '0', label: 'Pending' },
        { value: '1', label: 'Confirmed' }
      ]
    },
    {
      key: 'dateFrom',
      label: 'Date From',
      type: 'date',
      placeholder: 'Select start date'
    },
    {
      key: 'dateTo',
      label: 'Date To',
      type: 'date',
      placeholder: 'Select end date'
    },
    {
      key: 'destination',
      label: 'Destination',
      type: 'text',
      placeholder: 'Filter by destination',
      icon: <MapPin className="w-4 h-4" />
    },
    {
      key: 'createdBy',
      label: 'Created by/Clerk',
      type: 'select',
      placeholder: 'Select clerk',
      icon: <User className="w-4 h-4" />,
      options: [
        { value: '', label: 'All Clerks' },
        ...availableClerks.map(clerk => ({
          value: clerk.id.toString(),
          label: clerk.name
        }))
      ]
    }
  ];

  const handleParcelSelection = (parcelId: string, checked: boolean) => {
    if (checked) {
      setSelectedParcelIds(prev => 
        prev.includes(parcelId) ? prev : [...prev, parcelId]
      );
    } else {
      setSelectedParcelIds(prev => prev.filter(id => id !== parcelId));
    }
  };

  const handleSelectAll = () => {
    if (selectedParcelIds.length === filteredParcels.length) {
      setSelectedParcelIds([]);
    } else {
      setSelectedParcelIds(filteredParcels.map(p => p.id));
    }
  };

  const generateDispatchCode = (destination: string) => {
    if (!destination) return '';
    
    // Get current date in EAT (East Africa Time - UTC+3)
    const now = new Date();
    const eatOffset = 3 * 60; // EAT is UTC+3 (3 hours * 60 minutes)
    const utc = now.getTime() + (now.getTimezoneOffset() * 60000);
    const eatTime = new Date(utc + (eatOffset * 60000));
    const dateStr = eatTime.toISOString().split('T')[0].replace(/-/g, '');
    
    // Generate destination abbreviation (first 3 characters + first character of each word)
    const destinationAbbr = destination
      .toUpperCase()
      .split(' ')
      .map((word, index) => index === 0 ? word.substring(0, 3) : word.charAt(0))
      .join('')
      .substring(0, 4);
    
    // Generate 5 random alphanumeric characters
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
    let randomStr = '';
    for (let i = 0; i < 5; i++) {
      randomStr += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    
    return `${destinationAbbr}-${dateStr}-DS${randomStr}`;
  };

  const handleFormChange = (field: keyof CreateDispatchFormData, value: string | string[] | boolean) => {
    setFormData(prev => ({ ...prev, [field]: value }));

    // Auto-generate dispatch code and filter parcels when destination is selected
    if (field === 'destination' && typeof value === 'string') {
      if (value) {
        const newDispatchCode = generateDispatchCode(value);
        setFormData(prev => ({ ...prev, dispatchCode: newDispatchCode }));

        // Update destination filter to match selected destination
        setFilters(prev => ({ ...prev, destination: value }));
      } else {
        // Clear dispatch code and destination filter when no destination selected
        setFormData(prev => ({ ...prev, dispatchCode: '' }));
        setFilters(prev => ({ ...prev, destination: '' }));
      }
    }
  };

  const handleCreateDispatch = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (selectedParcelIds.length === 0) {
      setError('Please select at least one parcel to dispatch');
      return;
    }

    if (!formData.driverName || !formData.driverPhone || !formData.vehicleNumber) {
      setError('Please fill in vehicle and driver details');
      return;
    }

    if (!formData.allowMultipleDestinations && !formData.destination) {
      setError('Please select a destination');
      return;
    }

    try {
      setIsCreating(true);
      setError('');
      setSuccess('');

      // Get selected parcels
      const selectedParcels = parcels.filter(p => selectedParcelIds.includes(p.id));

      // Group parcels by destination for multi-destination support
      const parcelsByDestination = selectedParcels.reduce((acc, parcel) => {
        const dest = parcel.destination || 'Unknown';
        if (!acc[dest]) {
          acc[dest] = [];
        }
        acc[dest].push(parcel);
        return acc;
      }, {} as Record<string, Parcel[]>);

      const uniqueDestinations = Object.keys(parcelsByDestination);
      const primaryDestination = formData.allowMultipleDestinations
        ? uniqueDestinations[0] || formData.destination
        : formData.destination;

      // Use generated dispatch code
      const dispatchCode = formData.allowMultipleDestinations
        ? generateDispatchCode(primaryDestination)
        : formData.dispatchCode;

      if (!dispatchCode) {
        setError('Dispatch code not generated. Please select a destination.');
        return;
      }

      const dispatchData = {
        dispatchCode,
        sourceBranch: formData.sourceBranch,
        destination: formData.allowMultipleDestinations ? null : primaryDestination,
        driver: formData.driverName,
        driverPhone: formData.driverPhone,
        vehicleNumber: formData.vehicleNumber,
        parcelIds: selectedParcelIds
      };


      const newDispatch = await wmsApi.createDispatch(dispatchData);

      if (newDispatch) {
        // Update parcel statuses to in-transit (status 2)
        const updatePromises = selectedParcelIds.map(parcelId => 
          wmsApi.updateParcelStatus(parcelId, 2)
        );
        
        try {
          await Promise.all(updatePromises);
        } catch (updateError) {
        }
        
        const destinationText = formData.allowMultipleDestinations
          ? `${uniqueDestinations.length} destination(s): ${uniqueDestinations.join(', ')}`
          : primaryDestination;

        setSuccess(`Dispatch ${dispatchCode} created successfully! Dispatched ${selectedParcelIds.length} parcels to ${destinationText}. All parcels updated to in-transit.`);
        
        // Reset form
        setFormData({
          dispatchCode: '',
          sourceBranch: user?.branch?.name || 'Unknown Branch',
          destination: '',
          driverName: '',
          driverPhone: '',
          vehicleNumber: '',
          parcelIds: [],
          allowMultipleDestinations: false
        });
        setSelectedParcelIds([]);
        
        // Refresh parcels
        await fetchParcels();
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create dispatch');
    } finally {
      setIsCreating(false);
    }
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-KE', {
      style: 'currency',
      currency: 'KES',
      minimumFractionDigits: 0
    }).format(amount).replace('KES', 'KSh');
  };

  const getTotalQty = () => {
    return selectedParcelIds.reduce((total, parcelId) => {
      const parcel = parcels.find(p => p.id === parcelId);
      return total + (parcel?.quantity || 0);
    }, 0);
  };

  const getTotalAmount = () => {
    return selectedParcelIds.reduce((total, parcelId) => {
      const parcel = parcels.find(p => p.id === parcelId);
      return total + (parcel?.totalAmount || 0);
    }, 0);
  };

  // Virtual table columns for parcel selection
  const columns = [
    {
      key: 'selection',
      header: (
        <input
          type="checkbox"
          checked={selectedParcelIds.length === filteredParcels.length && filteredParcels.length > 0}
          onChange={handleSelectAll}
          className="rounded border-gray-300 text-brand-600 focus:ring-brand-500"
        />
      ),
      width: 50,
      render: (parcel: Parcel) => (
        <input
          type="checkbox"
          checked={selectedParcelIds.includes(parcel.id)}
          onChange={(e) => handleParcelSelection(parcel.id, e.target.checked)}
          className="rounded border-gray-300 text-brand-600 focus:ring-brand-500"
        />
      )
    },
    {
      key: 'waybillNumber',
      header: 'Waybill Number',
      width: 150,
      sortable: true,
      render: (parcel: Parcel) => (
        <div className="font-medium text-gray-900 dark:text-white">
          {parcel.waybillNumber}
        </div>
      )
    },
    {
      key: 'destination',
      header: 'Destination',
      width: 130,
      sortable: true,
      render: (parcel: Parcel) => (
        <div className="flex items-center text-gray-900 dark:text-white">
          <MapPin className="w-4 h-4 mr-1 text-gray-400" />
          {parcel.destination}
        </div>
      )
    },
    {
      key: 'sender',
      header: 'Sender',
      width: 180,
      sortable: true,
      render: (parcel: Parcel) => (
        <div>
          <div className="text-gray-900 dark:text-white">{parcel.sender}</div>
          <div className="text-sm text-gray-500">{parcel.senderTelephone}</div>
        </div>
      )
    },
    {
      key: 'receiver',
      header: 'Receiver',
      width: 180,
      sortable: true,
      render: (parcel: Parcel) => (
        <div>
          <div className="text-gray-900 dark:text-white">{parcel.receiver}</div>
          <div className="text-sm text-gray-500">{parcel.receiverTelephone}</div>
        </div>
      )
    },
    {
      key: 'quantity',
      header: 'Qty',
      width: 80,
      sortable: true,
      render: (parcel: Parcel) => (
        <div className="text-center">{parcel.quantity}</div>
      )
    },
    {
      key: 'description',
      header: 'Description',
      width: 200,
      sortable: true,
      render: (parcel: Parcel) => (
        <div className="text-gray-900 dark:text-white max-w-xs truncate" title={parcel.description}>
          {parcel.description || 'No description'}
        </div>
      )
    },
    {
      key: 'totalAmount',
      header: 'Amount',
      width: 120,
      sortable: true,
      render: (parcel: Parcel) => (
        <div className="font-medium">{formatCurrency(parcel.totalAmount)}</div>
      )
    },
    {
      key: 'createdAt',
      header: 'Created',
      width: 100,
      sortable: true,
      render: (parcel: Parcel) => (
        <div className="text-sm text-gray-500">
          {new Date(parcel.createdAt).toLocaleDateString()}
        </div>
      )
    }
  ];

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-96">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-brand-500"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">
            Create Dispatch
          </h1>
          <p className="text-gray-600 dark:text-gray-400 mt-1">
            Select parcels and create a new dispatch
          </p>
        </div>
        <div className="flex items-center gap-3">
          <Button
            variant="outline"
            onClick={() => setShowFilters(!showFilters)}
          >
            <Filter className="w-4 h-4 mr-2" />
            Filters
          </Button>
          <Button
            variant="primary"
            onClick={() => fetchParcels(true)}
            disabled={loading}
          >
            <RefreshCw className={`w-4 h-4 mr-2 ${loading ? 'animate-spin' : ''}`} />
            Refresh
          </Button>
        </div>
      </div>

      {/* Error/Success Messages */}
      {error && (
        <div className="bg-error-50 border border-error-200 text-error-800 px-4 py-3 rounded-lg flex items-center gap-2">
          <AlertCircle className="w-5 h-5" />
          {error}
        </div>
      )}

      {success && (
        <div className="bg-success-50 border border-success-200 text-success-800 px-4 py-3 rounded-lg flex items-center gap-2">
          <CheckCircle className="w-5 h-5" />
          {success}
        </div>
      )}

      {/* Filters */}
      {showFilters && (
        <FilterPanel
          fields={filterFields}
          filters={filters as any}
          onFilterChange={handleFilterChange}
          onClearFilters={clearFilters}
          activeFilters={getActiveFilterChips()}
          collapsible={true}
          defaultExpanded={true}
        />
      )}

      <div className="space-y-6">
        {/* Dispatch Form */}
        <Card>
          <div className="p-6">
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
              Dispatch Details
            </h3>
            
            <form onSubmit={handleCreateDispatch} className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                <Input
                  label="Dispatch Code"
                  placeholder="Select destination to generate"
                  value={formData.dispatchCode}
                  onChange={(e) => handleFormChange('dispatchCode', e.target.value)}
                  icon={<FileText className="w-4 h-4" />}
                  disabled
                  readOnly
                />

                <Input
                  label="Source Branch"
                  value={formData.sourceBranch}
                  onChange={(e) => handleFormChange('sourceBranch', e.target.value)}
                  icon={<MapPin className="w-4 h-4" />}
                  disabled
                />

                <div className="space-y-2">
                  <div className="flex items-center gap-3 mb-2">
                    <label className="flex items-center gap-2 cursor-pointer">
                      <input
                        type="checkbox"
                        checked={formData.allowMultipleDestinations}
                        onChange={(e) => handleFormChange('allowMultipleDestinations', e.target.checked)}
                        className="rounded border-gray-300 text-brand-600 focus:ring-brand-500"
                      />
                      <span className="text-sm font-medium text-gray-700 dark:text-gray-300">
                        Allow multiple destinations
                      </span>
                    </label>
                  </div>

                  {!formData.allowMultipleDestinations && (
                    <div className="relative">
                      <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                        Destination *
                      </label>
                      <MapPin className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" />
                      <select
                        value={formData.destination}
                        onChange={(e) => handleFormChange('destination', e.target.value)}
                        className="w-full pl-10 pr-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-brand-500 focus:border-brand-500"
                        required
                      >
                        <option value="">Select destination</option>
                        {destinations.map((dest) => (
                          <option key={dest} value={dest}>
                            {dest}
                          </option>
                        ))}
                      </select>
                    </div>
                  )}

                  {formData.allowMultipleDestinations && selectedParcelIds.length > 0 && (
                    <div className="bg-blue-50 border border-blue-200 rounded-md p-3">
                      <p className="text-sm text-blue-800">
                        <span className="font-semibold">Multiple Destinations Mode:</span>
                        Selected parcels will be grouped by destination automatically. <br/>
                        Destinations: {parcels.filter(p => selectedParcelIds.includes(p.id))
                          .map(p => p.destination)
                          .filter((dest, index, arr) => arr.indexOf(dest) === index)
                          .sort()
                          .join(', ')}
                      </p>
                    </div>
                  )}
                </div>

                <Input
                  label="Driver Name *"
                  placeholder="Enter driver name"
                  value={formData.driverName}
                  onChange={(e) => handleFormChange('driverName', e.target.value)}
                  icon={<User className="w-4 h-4" />}
                  required
                />

                <Input
                  label="Driver Phone Number *"
                  placeholder="e.g., 0712345678 or 254712345678"
                  value={formData.driverPhone}
                  onChange={(e) => handleFormChange('driverPhone', e.target.value)}
                  icon={<Phone className="w-4 h-4" />}
                  type="tel"
                  required
                  helperText="Kenyan mobile number format"
                />

                <Input
                  label="Vehicle Number *"
                  placeholder="Enter vehicle registration"
                  value={formData.vehicleNumber}
                  onChange={(e) => handleFormChange('vehicleNumber', e.target.value)}
                  icon={<Truck className="w-4 h-4" />}
                  required
                />

                {/* Selected Parcels Summary */}
                {selectedParcelIds.length > 0 && (
                  <div className="bg-gray-50 dark:bg-gray-800 p-4 rounded-lg">
                    <h4 className="font-medium text-gray-900 dark:text-white mb-2">
                      Selected Parcels ({selectedParcelIds.length})
                    </h4>
                    <div className="space-y-1 text-sm text-gray-600 dark:text-gray-400">
                      <div>Total Qty: {getTotalQty()}</div>
                      <div>Total Amount: {formatCurrency(getTotalAmount())}</div>
                    </div>
                  </div>
                )}

                <div className="md:col-span-2 lg:col-span-3">
                  <Button
                    type="submit"
                    variant="primary"
                    className="w-full"
                    disabled={isCreating || selectedParcelIds.length === 0}
                    isLoading={isCreating}
                  >
                    <Plus className="w-4 h-4 mr-2" />
                    {isCreating ? 'Creating Dispatch...' : 'Create Dispatch'}
                  </Button>
                </div>
              </form>
            </div>
          </Card>

        {/* Available Parcels */}
        <div>
          <Card padding={false}>
            <div className="p-6 border-b border-gray-200">
              <div className="flex items-center justify-between">
                <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
                  Available Parcels ({filteredParcels.length})
                </h3>
                <div className="flex items-center gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={handleSelectAll}
                  >
                    {selectedParcelIds.length === filteredParcels.length ? 'Deselect All' : 'Select All'}
                  </Button>
                </div>
              </div>
            </div>

            <VirtualScrollTable
              data={filteredParcels}
              columns={columns}
              rowHeight={64}
              containerHeight={500}
              loading={loading}
              emptyMessage={
                Object.values(filters).some(filter => filter) 
                  ? 'Try adjusting your filters' 
                  : 'No pending parcels found for dispatch'
              }
              onRowClick={(parcel) => {
                // Toggle selection on row click
                const isSelected = selectedParcelIds.includes(parcel.id);
                handleParcelSelection(parcel.id, !isSelected);
              }}
            />
          </Card>
        </div>
      </div>
    </div>
  );
};

export default CreateDispatch;