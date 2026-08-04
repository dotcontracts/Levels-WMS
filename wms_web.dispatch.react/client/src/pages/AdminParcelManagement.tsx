import React, { useState, useEffect, useCallback, useMemo, useRef } from 'react';
import { useAuth } from '../context/AuthContext';
import { useParcelsData } from '../context/ParcelsDataContext';
import { wmsApi, Parcel } from '../services/wmsApi';
import { 
  Package, 
  Search, 
  RefreshCw, 
  Edit, 
  Trash2,
  AlertTriangle,
  CheckCircle,
  X,
  FileText,
  Calendar,
  User,
  MapPin,
  Phone,
  DollarSign,
  Settings,
  Save,
  RotateCcw
} from 'lucide-react';
import { FilterPanel, FilterField } from '../components/ui';
import { VirtualScrollTable } from '../components/VirtualScrollTable';

// Status constants matching the API
const PARCEL_STATUS = {
  PENDING: 0,
  FINALIZED: 1,
  IN_TRANSIT: 2,
  DELIVERED: 3,
  CANCELLED: 4
} as const;

const STATUS_LABELS: Record<number, string> = {
  [PARCEL_STATUS.PENDING]: 'Pending',
  [PARCEL_STATUS.FINALIZED]: 'Confirmed',
  [PARCEL_STATUS.IN_TRANSIT]: 'In Transit',
  [PARCEL_STATUS.DELIVERED]: 'Delivered',
  [PARCEL_STATUS.CANCELLED]: 'Cancelled'
};

const STATUS_COLORS: Record<number, string> = {
  [PARCEL_STATUS.PENDING]: 'bg-warning-100 text-warning-800 border-warning-200',
  [PARCEL_STATUS.FINALIZED]: 'bg-primary-100 text-primary-800 border-primary-200',
  [PARCEL_STATUS.IN_TRANSIT]: 'bg-info-100 text-info-800 border-info-200',
  [PARCEL_STATUS.DELIVERED]: 'bg-success-100 text-success-800 border-success-200',
  [PARCEL_STATUS.CANCELLED]: 'bg-error-100 text-error-800 border-error-200'
};

interface ParcelEditForm {
  waybillNumber: string;
  status: number;
  paymentMethods: string;
  sender: string;
  senderTelephone: string;
  receiver: string;
  receiverTelephone: string;
  destination: string;
  quantity: number;
  amount: number;
  description: string;
}

interface ParcelFilterState {
  waybillNumber: string;
  createdBy: string;
  dateCreated: string;
  status: string;
  destination: string;
  paymentMethod: string;
  amountRange: { from: string; to: string };
  dateRange: { from: string; to: string };
}

interface PaginationState {
  currentPage: number;
  totalPages: number;
  totalCount: number;
  hasMoreData: boolean;
  isLoadingMore: boolean;
}

const AdminParcelManagement: React.FC = () => {
  const { user, isAdmin, isAccountant } = useAuth();
  const { getStreamingParcels, loading: cacheLoading } = useParcelsData();
  const [allParcels, setAllParcels] = useState<Parcel[]>([]);
  const [loading, setLoading] = useState(true);
  const usersByIdRef = useRef<Map<number, any> | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  
  // Dropdown options for edit modal
  const [availableDestinations, setAvailableDestinations] = useState<string[]>([]);
  const [availablePaymentMethods, setAvailablePaymentMethods] = useState<string[]>([]);
  
  // Edit Modal State
  const [editingParcel, setEditingParcel] = useState<Parcel | null>(null);
  const [editForm, setEditForm] = useState<ParcelEditForm | null>(null);
  const [saving, setSaving] = useState(false);
  
  // Delete Confirmation State
  const [deletingParcel, setDeletingParcel] = useState<Parcel | null>(null);
  
  // Filter State
  const [filters, setFilters] = useState<ParcelFilterState>({
    waybillNumber: '',
    createdBy: '',
    dateCreated: '',
    status: '',
    destination: '',
    paymentMethod: '',
    amountRange: { from: '', to: '' },
    dateRange: { from: '', to: '' }
  });
  
  // Available clerks for filter dropdown
  const [clerks, setClerks] = useState<{ id: number; username: string }[]>([]);

  // Filter fields configuration
  const filterFields: FilterField[] = useMemo(() => [
    {
      key: 'waybillNumber',
      label: 'Waybill Number',
      type: 'text',
      placeholder: 'Search waybill...',
      icon: <Search className="w-4 h-4" />
    },
    {
      key: 'createdBy',
      label: 'Created By',
      type: 'select',
      placeholder: 'Select clerk',
      options: [
        { value: '', label: 'All Clerks' },
        ...clerks.map(clerk => ({ value: clerk.username, label: clerk.username }))
      ],
      icon: <User className="w-4 h-4" />
    },
    {
      key: 'status',
      label: 'Status',
      type: 'select',
      placeholder: 'Select status',
      options: [
        { value: '', label: 'All Statuses' },
        { value: 'pending', label: 'Pending' },
        { value: 'confirmed', label: 'Confirmed' },
        { value: 'in-transit', label: 'In Transit' },
        { value: 'delivered', label: 'Delivered' },
        { value: 'cancelled', label: 'Cancelled' }
      ]
    },
    {
      key: 'destination',
      label: 'Destination',
      type: 'select',
      placeholder: 'Select destination',
      options: [
        { value: '', label: 'All Destinations' },
        ...availableDestinations.map(dest => ({ value: dest, label: dest }))
      ],
      icon: <MapPin className="w-4 h-4" />
    },
    {
      key: 'paymentMethod',
      label: 'Payment Method',
      type: 'select',
      placeholder: 'Select payment method',
      options: [
        { value: '', label: 'All Payment Methods' },
        ...availablePaymentMethods.map(method => ({ value: method, label: method }))
      ],
      icon: <DollarSign className="w-4 h-4" />
    },
    {
      key: 'amountRange',
      label: 'Amount Range',
      type: 'dateRange',
      placeholder: 'Select amount range'
    },
    {
      key: 'dateRange',
      label: 'Date Range',
      type: 'dateRange',
      placeholder: 'Select date range',
      icon: <Calendar className="w-4 h-4" />
    }
  ], [clerks, availableDestinations, availablePaymentMethods]);

  const ensureUsersCache = useCallback(async () => {
    if (usersByIdRef.current) return usersByIdRef.current;
    const users = await wmsApi.getUsers();
    usersByIdRef.current = new Map(users.map(u => [u.id, u]));
    return usersByIdRef.current;
  }, []);

  // Check admin/accountant access
  useEffect(() => {
    if (user && !isAdmin() && !isAccountant()) {
      setError('Access denied. Admin or Accountant privileges required.');
    }
  }, [user, isAdmin, isAccountant]);

  // Load branches for destination dropdown
  useEffect(() => {
    wmsApi.getBranches().then(branches => {
      setAvailableDestinations(branches.map(b => b.name));
    }).catch(() => {});
  }, []);

  const loadParcels = useCallback(async (forceRefresh = false) => {
    if (!user || (!isAdmin() && !isAccountant())) return;
    try {
      setLoading(true);
      setError(null);
      const result = await getStreamingParcels({});
      const data = result.parcels;
      try {
        const userMap = await ensureUsersCache();
        const clerkList: { id: number; username: string }[] = [];
        const mapped = data.map(parcel => {
          if (!parcel.createdBy && parcel.createdById) {
            const u = userMap.get(parcel.createdById);
            if (u) return { ...parcel, createdBy: { username: u.firstName && u.lastName ? `${u.firstName} ${u.lastName}` : u.username } };
          }
          return parcel;
        });
        userMap.forEach(u => clerkList.push({ id: u.id, username: u.firstName && u.lastName ? `${u.firstName} ${u.lastName}` : u.username }));
        setClerks(clerkList);
        setAllParcels(mapped);
        setAvailablePaymentMethods(Array.from(new Set(mapped.map(p => p.paymentMethods).filter(Boolean))));
      } catch {
        setAllParcels(data);
        setAvailablePaymentMethods(Array.from(new Set(data.map(p => p.paymentMethods).filter(Boolean))));
      }
    } catch {
      setError('Failed to load parcels');
    } finally {
      setLoading(false);
    }
  }, [user, isAdmin, isAccountant, getStreamingParcels, ensureUsersCache]);

  useEffect(() => {
    loadParcels();
  }, []);

  // Client-side filter across full dataset
  const parcels = useMemo(() => {
    let result = allParcels;
    if (filters.waybillNumber) {
      const q = filters.waybillNumber.toLowerCase();
      result = result.filter(p => p.waybillNumber?.toLowerCase().includes(q));
    }
    if (filters.createdBy) result = result.filter(p => p.createdBy?.username === filters.createdBy);
    if (filters.status && filters.status !== 'all') result = result.filter(p => p.status === parseInt(filters.status));
    if (filters.destination) result = result.filter(p => p.destination === filters.destination);
    if (filters.paymentMethod) result = result.filter(p => p.paymentMethods === filters.paymentMethod);
    if (filters.dateRange.from) result = result.filter(p => new Date(p.createdAt) >= new Date(filters.dateRange.from));
    if (filters.dateRange.to) { const to = new Date(filters.dateRange.to); to.setHours(23,59,59,999); result = result.filter(p => new Date(p.createdAt) <= to); }
    if (filters.amountRange.from) result = result.filter(p => (p.totalAmount || 0) >= parseFloat(filters.amountRange.from));
    if (filters.amountRange.to) result = result.filter(p => (p.totalAmount || 0) <= parseFloat(filters.amountRange.to));
    return result.sort((a, b) => new Date(b.createdAt || 0).getTime() - new Date(a.createdAt || 0).getTime());
  }, [allParcels, filters]);

  // Handle filter changes
  const handleFilterChange = (key: string, value: string | { from: string; to: string }) => {
    setFilters(prev => ({ ...prev, [key]: value }));
  };

  // Clear all filters
  const clearFilters = () => {
    setFilters({
      waybillNumber: '',
      createdBy: '',
      dateCreated: '',
      status: '',
      destination: '',
      paymentMethod: '',
      amountRange: { from: '', to: '' },
      dateRange: { from: '', to: '' }
    });
  };

  // Generate active filter chips
  const getActiveFilterChips = () => {
    const chips: any[] = [];

    if (filters.waybillNumber) {
      chips.push({
        key: 'waybillNumber',
        label: 'Waybill',
        value: filters.waybillNumber,
        onRemove: () => handleFilterChange('waybillNumber', '')
      });
    }

    if (filters.createdBy) {
      chips.push({
        key: 'createdBy',
        label: 'Created By',
        value: filters.createdBy,
        onRemove: () => handleFilterChange('createdBy', '')
      });
    }

    if (filters.status) {
      const statusLabel = filterFields.find(f => f.key === 'status')?.options?.find(o => o.value === filters.status)?.label || filters.status;
      chips.push({
        key: 'status',
        label: 'Status',
        value: statusLabel,
        onRemove: () => handleFilterChange('status', '')
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

    if (filters.paymentMethod) {
      chips.push({
        key: 'paymentMethod',
        label: 'Payment Method',
        value: filters.paymentMethod,
        onRemove: () => handleFilterChange('paymentMethod', '')
      });
    }

    if (filters.amountRange.from || filters.amountRange.to) {
      const rangeValue = `${filters.amountRange.from || '0'} - ${filters.amountRange.to || '∞'}`;
      chips.push({
        key: 'amountRange',
        label: 'Amount Range',
        value: rangeValue,
        onRemove: () => handleFilterChange('amountRange', { from: '', to: '' })
      });
    }

    if (filters.dateRange.from || filters.dateRange.to) {
      const rangeValue = `${filters.dateRange.from || 'Start'} - ${filters.dateRange.to || 'End'}`;
      chips.push({
        key: 'dateRange',
        label: 'Date Range',
        value: rangeValue,
        onRemove: () => handleFilterChange('dateRange', { from: '', to: '' })
      });
    }

    return chips;
  };

  // Handle quick status update
  const handleQuickStatusUpdate = async (parcelId: string, newStatus: number) => {
    try {
      const success = await wmsApi.updateParcelStatus(parcelId, newStatus);
      if (success) {
        setAllParcels(prev => prev.map(p => 
          p.id === parcelId ? { ...p, status: newStatus } : p
        ));
        setSuccessMessage(`Parcel status updated to ${STATUS_LABELS[newStatus]}`);
        setTimeout(() => setSuccessMessage(null), 3000);
      } else {
        setError('Failed to update parcel status');
        setTimeout(() => setError(null), 3000);
      }
    } catch (err) {
      setError('Failed to update parcel status');
      setTimeout(() => setError(null), 3000);
    }
  };

  // Open edit modal
  const openEditModal = (parcel: Parcel) => {
    setEditingParcel(parcel);
    setEditForm({
      waybillNumber: parcel.waybillNumber,
      status: parcel.status,
      paymentMethods: parcel.paymentMethods || '',
      sender: parcel.sender || '',
      senderTelephone: parcel.senderTelephone || '',
      receiver: parcel.receiver || '',
      receiverTelephone: parcel.receiverTelephone || '',
      destination: parcel.destination || '',
      quantity: parcel.quantity || 0,
      amount: parcel.totalAmount || parcel.amount || 0,
      description: parcel.description || ''
    });
  };

  // Close edit modal
  const closeEditModal = () => {
    setEditingParcel(null);
    setEditForm(null);
  };

  // Handle edit form changes
  const handleEditFormChange = (field: keyof ParcelEditForm, value: string | number) => {
    if (!editForm) return;
    setEditForm(prev => prev ? { ...prev, [field]: value } : null);
  };

  // Reset edit form
  const resetEditForm = () => {
    if (!editingParcel) return;
    setEditForm({
      waybillNumber: editingParcel.waybillNumber,
      status: editingParcel.status,
      paymentMethods: editingParcel.paymentMethods || '',
      sender: editingParcel.sender || '',
      senderTelephone: editingParcel.senderTelephone || '',
      receiver: editingParcel.receiver || '',
      receiverTelephone: editingParcel.receiverTelephone || '',
      destination: editingParcel.destination || '',
      quantity: editingParcel.quantity || 0,
      amount: editingParcel.totalAmount || editingParcel.amount || 0,
      description: editingParcel.description || ''
    });
  };

  // Save parcel edits
  const saveParcelEdits = async () => {
    if (!editingParcel || !editForm) return;

    const validationErrors: string[] = [];
    if (!editForm.sender?.trim()) validationErrors.push('Sender is required');
    if (!editForm.senderTelephone?.trim()) validationErrors.push('Sender telephone is required');
    if (!editForm.receiver?.trim()) validationErrors.push('Receiver is required');
    if (!editForm.receiverTelephone?.trim()) validationErrors.push('Receiver telephone is required');
    if (!editForm.destination?.trim()) validationErrors.push('Destination is required');
    if (!editForm.paymentMethods?.trim()) validationErrors.push('Payment method is required');
    if (validationErrors.length > 0) {
      setError(validationErrors.join(', '));
      setTimeout(() => setError(null), 5000);
      return;
    }
    
    try {
      setSaving(true);
      
      // Prepare update data with both amount and totalAmount
      const updateData = {
        ...editForm,
        description: editForm.description || '',
        totalAmount: editForm.amount, // Set totalAmount to the edited amount value
        amount: editForm.amount // Keep amount field consistent
      };
      
      const success = await wmsApi.updateParcel(editingParcel.id, updateData);
      
      if (success) {
        setAllParcels(prev => prev.map(p => 
          p.id === editingParcel.id ? { ...p, ...updateData, totalAmount: editForm.amount } : p
        ));
        setSuccessMessage('Parcel updated successfully');
        closeEditModal();
        setTimeout(() => setSuccessMessage(null), 3000);
      } else {
        setError('Failed to update parcel');
        setTimeout(() => setError(null), 3000);
      }
    } catch (err) {
      setError('Failed to update parcel');
      setTimeout(() => setError(null), 3000);
    } finally {
      setSaving(false);
    }
  };

  // Handle delete parcel
  const handleDeleteParcel = async () => {
    if (!deletingParcel) return;
    
    try {
      const success = await wmsApi.deleteParcel(deletingParcel.id);
      
      if (success) {
        setAllParcels(prev => prev.filter(p => p.id !== deletingParcel.id));
        setSuccessMessage('Parcel deleted successfully');
        setDeletingParcel(null);
        setTimeout(() => setSuccessMessage(null), 3000);
      } else {
        setError('Failed to delete parcel');
        setTimeout(() => setError(null), 3000);
      }
    } catch (err) {
      setError('Failed to delete parcel');
      setTimeout(() => setError(null), 3000);
    }
  };

  // Format currency
  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-KE', {
      style: 'currency',
      currency: 'KES',
      minimumFractionDigits: 0
    }).format(amount).replace('KES', 'KSh');
  };

  // Format date
  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  // Virtual table columns for admin parcel management
  const columns = [
    {
      key: 'waybillNumber',
      header: 'Waybill #',
      width: 160,
      sortable: true,
      render: (parcel: Parcel) => (
        <div className="flex items-center">
          <FileText className="w-4 h-4 text-gray-400 mr-2" />
          <span className="text-sm font-medium text-gray-900">
            {parcel.waybillNumber}
          </span>
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
          <div className="text-sm font-medium text-gray-900">{parcel.sender}</div>
          <div className="text-sm text-gray-500 flex items-center">
            <Phone className="w-3 h-3 mr-1" />
            {parcel.senderTelephone}
          </div>
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
          <div className="text-sm font-medium text-gray-900">{parcel.receiver}</div>
          <div className="text-sm text-gray-500 flex items-center">
            <Phone className="w-3 h-3 mr-1" />
            {parcel.receiverTelephone}
          </div>
        </div>
      )
    },
    {
      key: 'destination',
      header: 'Destination',
      width: 140,
      sortable: true,
      render: (parcel: Parcel) => (
        <div className="flex items-center text-sm text-gray-900">
          <MapPin className="w-4 h-4 text-gray-400 mr-1" />
          {parcel.destination}
        </div>
      )
    },
    {
      key: 'amount',
      header: 'Amount',
      width: 120,
      sortable: true,
      render: (parcel: Parcel) => (
        <div className="flex items-center text-sm font-medium text-gray-900">
          <DollarSign className="w-4 h-4 text-gray-400 mr-1" />
          {formatCurrency(parcel.totalAmount || parcel.amount || 0)}
        </div>
      )
    },
    {
      key: 'status',
      header: 'Status',
      width: 140,
      sortable: true,
      render: (parcel: Parcel) => (
        <select
          value={parcel.status}
          onChange={(e) => handleQuickStatusUpdate(parcel.id, Number(e.target.value))}
          className={`text-xs font-medium px-2 py-1 rounded-full border text-center ${STATUS_COLORS[parcel.status]}`}
        >
          {Object.entries(STATUS_LABELS).map(([value, label]) => (
            <option key={value} value={value}>
              {label}
            </option>
          ))}
        </select>
      )
    },
    {
      key: 'createdBy',
      header: 'Created By',
      width: 140,
      sortable: true,
      render: (parcel: Parcel) => (
        <div className="flex items-center text-sm text-gray-900">
          <User className="w-4 h-4 text-gray-400 mr-1" />
          {parcel.createdBy?.username || `User ${parcel.createdById}`}
        </div>
      )
    },
    {
      key: 'createdAt',
      header: 'Created Date',
      width: 160,
      sortable: true,
      render: (parcel: Parcel) => (
        <div className="flex items-center text-sm text-gray-900">
          <Calendar className="w-4 h-4 text-gray-400 mr-1" />
          {formatDate(parcel.createdAt)}
        </div>
      )
    },
    {
      key: 'actions',
      header: 'Actions',
      width: 100,
      render: (parcel: Parcel) => (
        <div className="flex items-center gap-2">
          <button
            onClick={() => openEditModal(parcel)}
            className="text-primary-600 hover:text-primary-700"
            title="Edit Parcel"
          >
            <Edit className="w-4 h-4" />
          </button>
          <button
            onClick={() => setDeletingParcel(parcel)}
            className="text-error-600 hover:text-error-700"
            title="Delete Parcel"
          >
            <Trash2 className="w-4 h-4" />
          </button>
        </div>
      )
    }
  ];


  if (!isAdmin() && !isAccountant()) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <div className="text-center">
          <AlertTriangle className="w-16 h-16 text-error-500 mx-auto mb-4" />
          <h2 className="text-xl font-semibold text-gray-900 mb-2">Access Denied</h2>
          <p className="text-gray-600">You need admin or accountant privileges to access this page.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 flex items-center gap-3">
            <Settings className="w-8 h-8 text-primary-600" />
            Manage Parcels (Admin)
          </h1>
          <p className="text-gray-600 mt-1">
            Administrative parcel management with full CRUD operations
          </p>
        </div>
        <div className="flex items-center gap-3">
          <button
            onClick={() => loadParcels(true)}
            disabled={loading || cacheLoading}
            className="btn-primary flex items-center gap-2"
          >
            <RefreshCw className={`w-4 h-4 ${loading ? 'animate-spin' : ''}`} />
            Refresh
          </button>
        </div>
      </div>

      {/* Success/Error Messages */}
      {successMessage && (
        <div className="bg-success-50 border border-success-200 text-success-800 px-4 py-3 rounded-lg flex items-center gap-2">
          <CheckCircle className="w-5 h-5" />
          {successMessage}
        </div>
      )}
      
      {error && (
        <div className="bg-error-50 border border-error-200 text-error-800 px-4 py-3 rounded-lg flex items-center gap-2">
          <AlertTriangle className="w-5 h-5" />
          {error}
        </div>
      )}

      {/* Filters Panel */}
      <FilterPanel
        fields={filterFields}
        filters={filters as any}
        onFilterChange={handleFilterChange}
        onClearFilters={clearFilters}
        activeFilters={getActiveFilterChips()}
        collapsible={true}
        defaultExpanded={false}
      />

      {/* Parcels Table */}
      <div className="card">
        <div className="p-6 border-b border-gray-200">
          <div className="flex items-center justify-between">
            <h2 className="text-lg font-semibold text-gray-900">
              Parcels ({parcels.length} of {allParcels.length})
            </h2>
          </div>
        </div>

        <VirtualScrollTable
          data={parcels}
          columns={columns}
          rowHeight={64}
          containerHeight={600}
          loading={loading || cacheLoading}
          hasMoreData={false}
          isLoadingMore={false}
          onLoadMore={() => {}}
          emptyMessage={
            Object.values(filters).some(filter => filter) 
              ? 'Try adjusting your filters or refresh the data' 
              : 'No parcels found'
          }
          onRowClick={(parcel) => openEditModal(parcel)}
        />
      </div>

      {/* Edit Parcel Modal */}
      {editingParcel && editForm && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-lg shadow-xl max-w-2xl w-full max-h-[90vh] overflow-y-auto">
            <div className="p-6 border-b border-gray-200">
              <div className="flex items-center justify-between">
                <h3 className="text-lg font-semibold text-gray-900">
                  Edit Parcel - {editingParcel.waybillNumber}
                </h3>
                <button
                  onClick={closeEditModal}
                  className="text-gray-400 hover:text-gray-600"
                >
                  <X className="w-6 h-6" />
                </button>
              </div>
            </div>

            <div className="p-6 space-y-6">
              {/* Read-only System Info */}
              <div className="bg-gray-50 p-4 rounded-lg">
                <h4 className="text-sm font-medium text-gray-700 mb-2">System Information</h4>
                <div className="grid grid-cols-2 gap-4 text-sm">
                  <div>
                    <span className="text-gray-500">Parcel ID:</span>
                    <span className="ml-2 text-gray-900">{editingParcel.id}</span>
                  </div>
                  <div>
                    <span className="text-gray-500">Created At:</span>
                    <span className="ml-2 text-gray-900">{formatDate(editingParcel.createdAt)}</span>
                  </div>
                  {editingParcel.qrCode && (
                    <div>
                      <span className="text-gray-500">QR Code:</span>
                      <span className="ml-2 text-gray-900">{editingParcel.qrCode}</span>
                    </div>
                  )}
                </div>
              </div>

              {/* Editable Fields */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Waybill Number
                  </label>
                  <input
                    type="text"
                    value={editForm.waybillNumber}
                    onChange={(e) => handleEditFormChange('waybillNumber', e.target.value)}
                    className="input-field"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Status
                  </label>
                  <select
                    value={editForm.status}
                    onChange={(e) => handleEditFormChange('status', Number(e.target.value))}
                    className="input-field"
                  >
                    {Object.entries(STATUS_LABELS).map(([value, label]) => (
                      <option key={value} value={value}>
                        {label}
                      </option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Payment Method
                  </label>
                  <select
                    value={editForm.paymentMethods}
                    onChange={(e) => handleEditFormChange('paymentMethods', e.target.value)}
                    className="input-field"
                  >
                    <option value="" disabled>Select Payment Method</option>
                    <option value="Paid">Paid</option>
                    <option value="COD">COD (Cash on Delivery)</option>
                    <option value="Contract">Contract</option>
                    {availablePaymentMethods.filter(method => !['Paid', 'COD', 'Contract'].includes(method)).map(method => (
                      <option key={method} value={method}>{method}</option>
                    ))}
                    {editForm.paymentMethods && !['Paid', 'COD', 'Contract', ...availablePaymentMethods].includes(editForm.paymentMethods) && (
                      <option value={editForm.paymentMethods}>{editForm.paymentMethods}</option>
                    )}
                  </select>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Destination
                  </label>
                  <select
                    value={editForm.destination}
                    onChange={(e) => handleEditFormChange('destination', e.target.value)}
                    className="input-field"
                  >
                    <option value="" disabled>Select Destination</option>
                    {availableDestinations.map(dest => (
                      <option key={dest} value={dest}>{dest}</option>
                    ))}
                    {editForm.destination && !availableDestinations.includes(editForm.destination) && (
                      <option value={editForm.destination}>{editForm.destination}</option>
                    )}
                  </select>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Sender Name
                  </label>
                  <input
                    type="text"
                    value={editForm.sender}
                    onChange={(e) => handleEditFormChange('sender', e.target.value)}
                    className="input-field"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Sender Telephone
                  </label>
                  <input
                    type="text"
                    value={editForm.senderTelephone}
                    onChange={(e) => handleEditFormChange('senderTelephone', e.target.value)}
                    className="input-field"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Receiver Name
                  </label>
                  <input
                    type="text"
                    value={editForm.receiver}
                    onChange={(e) => handleEditFormChange('receiver', e.target.value)}
                    className="input-field"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Receiver Telephone
                  </label>
                  <input
                    type="text"
                    value={editForm.receiverTelephone}
                    onChange={(e) => handleEditFormChange('receiverTelephone', e.target.value)}
                    className="input-field"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Quantity
                  </label>
                  <input
                    type="number"
                    value={editForm.quantity}
                    onChange={(e) => handleEditFormChange('quantity', Number(e.target.value))}
                    className="input-field"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Amount (KSh)
                  </label>
                  <input
                    type="number"
                    value={editForm.amount}
                    onChange={(e) => handleEditFormChange('amount', Number(e.target.value))}
                    className="input-field"
                  />
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Description
                </label>
                <textarea
                  value={editForm.description}
                  onChange={(e) => handleEditFormChange('description', e.target.value)}
                  rows={3}
                  className="input-field"
                />
              </div>
            </div>

            <div className="p-6 border-t border-gray-200 flex items-center justify-between">
              <button
                onClick={resetEditForm}
                className="btn-secondary flex items-center gap-2"
              >
                <RotateCcw className="w-4 h-4" />
                Reset
              </button>
              
              <div className="flex items-center gap-3">
                <button
                  onClick={closeEditModal}
                  className="btn-secondary"
                >
                  Cancel
                </button>
                <button
                  onClick={saveParcelEdits}
                  disabled={saving}
                  className="btn-primary flex items-center gap-2"
                >
                  <Save className="w-4 h-4" />
                  {saving ? 'Saving...' : 'Save Changes'}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Delete Confirmation Modal */}
      {deletingParcel && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-lg shadow-xl max-w-md w-full">
            <div className="p-6">
              <div className="flex items-center mb-4">
                <AlertTriangle className="w-6 h-6 text-error-600 mr-2" />
                <h3 className="text-lg font-semibold text-gray-900">
                  Delete Parcel
                </h3>
              </div>
              
              <p className="text-gray-600 mb-6">
                Are you sure you want to delete parcel <strong>{deletingParcel.waybillNumber}</strong>? 
                This action cannot be undone.
              </p>
              
              <div className="flex items-center gap-3 justify-end">
                <button
                  onClick={() => setDeletingParcel(null)}
                  className="btn-secondary"
                >
                  Cancel
                </button>
                <button
                  onClick={handleDeleteParcel}
                  className="btn-danger flex items-center gap-2"
                >
                  <Trash2 className="w-4 h-4" />
                  Delete Parcel
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default AdminParcelManagement;