import React, { useState, useEffect, useMemo } from 'react';
import { Package, Search, Filter, RefreshCw, Calendar, MapPin, DollarSign, User, Phone, X, Edit, Save, AlertCircle } from 'lucide-react';
import { useAuth } from '../context/AuthContext';
import { Card, Button, Badge, FilterPanel, FilterField, Modal } from '../components/ui';
import { VirtualScrollTable } from '../components/VirtualScrollTable';
import { wmsApi, Parcel } from '../services/wmsApi';

interface BranchParcelFilterState {
  search: string;
  date: string;
  status: string;
  paymentMethod: string;
}

interface UpdateParcelData {
  amountPaid: number;
  transactionCode: string;
}

const BranchParcels: React.FC = () => {
  const [parcels, setParcels] = useState<Parcel[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showFilters, setShowFilters] = useState(false);
  const [selectedParcel, setSelectedParcel] = useState<Parcel | null>(null);
  const [showUpdateModal, setShowUpdateModal] = useState(false);
  const [updateData, setUpdateData] = useState<UpdateParcelData>({ amountPaid: 0, transactionCode: '' });
  const [updating, setUpdating] = useState(false);

  const [filters, setFilters] = useState<BranchParcelFilterState>({
    search: '',
    date: new Date().toISOString().split('T')[0], // Default to today
    status: '',
    paymentMethod: ''
  });

  const { user, isBranchManager } = useAuth();

  useEffect(() => {
    fetchBranchParcels();
  }, [filters.date]);

  const fetchBranchParcels = async () => {
    try {
      setLoading(true);
      setError('');

      // Debug: Log user information
      console.log('User object:', user);
      console.log('Is branch manager:', isBranchManager());
      console.log('User branch:', user?.branch);

      // Get branch name for filtering
      const branchName = isBranchManager() && user?.branch?.name ? user.branch.name : undefined;
      console.log('Branch name for filtering:', branchName);

      // Branch managers must have a branch assigned to access this feature
      if (isBranchManager() && !branchName) {
        console.log('Branch manager without branch info detected');
        setError('Branch information not found. Please contact administrator.');
        setLoading(false);
        return;
      }

      // Fetch parcels - for admins get all, for branch managers filter by destination
      const data = await wmsApi.getAllParcelsForReports({
        destination: isBranchManager() && branchName ? branchName : undefined
      });

      // Filter by branch destination for branch managers
      let branchFilteredData = data;
      if (isBranchManager() && branchName) {
        branchFilteredData = data.filter(parcel =>
          parcel.destination && parcel.destination.toLowerCase() === branchName.toLowerCase()
        );

        // Debug logging for branch managers
        console.log(`Branch Manager: ${branchName}, Total parcels: ${data.length}, Branch parcels: ${branchFilteredData.length}`);
        console.log('Sample destinations:', data.slice(0, 5).map(p => p.destination));

        // If no parcels found for branch, show helpful message
        if (branchFilteredData.length === 0 && data.length > 0) {
          setError(`No parcels found destined for ${branchName}. Check if your branch name matches the destinations in the system.`);
          setLoading(false);
          return;
        }
      }

      // Filter by selected date
      let filteredData = branchFilteredData;
      if (filters.date) {
        filteredData = branchFilteredData.filter(parcel => {
          if (parcel.createdAt) {
            try {
              const parcelDate = new Date(parcel.createdAt);
              return parcelDate.toISOString().split('T')[0] === filters.date;
            } catch (error) {
              return false;
            }
          }
          return false;
        });
      }

      // Populate user information for parcels that don't have createdBy populated
      try {
        const users = await wmsApi.getUsers();

        const updatedParcels = filteredData.map(parcel => {
          if (!parcel.createdBy && parcel.createdById) {
            const creator = users.find(u => u.id === parcel.createdById);
            if (creator) {
              return {
                ...parcel,
                createdBy: {
                  username: creator.firstName && creator.lastName
                    ? `${creator.firstName} ${creator.lastName}`
                    : creator.username
                }
              };
            }
          }
          return parcel;
        });

        // For branch managers, show all parcels destined for their branch
        // Don't filter by updateability here - show all parcels but control editing in UI
        setParcels(updatedParcels);
      } catch (userErr) {
        // If user fetching fails, just use the filtered data without user info
        // For branch managers, show all parcels destined for their branch
        setParcels(filteredData);
      }
    } catch (err) {
      setError('Failed to load branch parcels');
    } finally {
      setLoading(false);
    }
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-KE', {
      style: 'currency',
      currency: 'KES',
      minimumFractionDigits: 0
    }).format(amount).replace('KES', 'KSh');
  };

  const getStatusLabel = (status: number) => {
    const statusMap = {
      0: 'Pending',
      1: 'Confirmed',
      2: 'In Transit',
      3: 'Delivered',
      4: 'Cancelled'
    };
    return statusMap[status as keyof typeof statusMap] || 'Unknown';
  };

  const getStatusBadge = (status: number) => {
    const statusMap = {
      0: { label: 'Pending', variant: 'warning' as const },
      1: { label: 'Confirmed', variant: 'primary' as const },
      2: { label: 'In Transit', variant: 'primary' as const },
      3: { label: 'Delivered', variant: 'success' as const },
      4: { label: 'Cancelled', variant: 'error' as const },
    };

    const statusInfo = statusMap[status as keyof typeof statusMap] || { label: 'Unknown', variant: 'gray' as const };
    return <Badge variant={statusInfo.variant}>{statusInfo.label}</Badge>;
  };

  const canUpdateParcel = (parcel: Parcel) => {
    // Can update if:
    // 1. Payment method is COD and status is Delivered (3)
    // 2. Payment method is Paid and any status
    const paymentMethod = parcel.paymentMethods.toLowerCase();

    if (paymentMethod.includes('cod')) {
      return parcel.status === 3; // Only delivered COD parcels
    }

    if (paymentMethod.includes('paid')) {
      return true; // Any status for paid parcels
    }

    return false;
  };

  const handleUpdateParcel = (parcel: Parcel) => {
    // Check if parcel can be updated
    if (!canUpdateParcel(parcel)) {
      const paymentMethod = parcel.paymentMethods.toLowerCase();
      let alertMessage = '';

      if (paymentMethod.includes('cod')) {
        alertMessage = 'COD parcels can only be updated when they have been delivered. This parcel is currently not delivered.';
      } else if (paymentMethod.includes('paid')) {
        alertMessage = 'This paid parcel cannot be updated at this time.';
      } else {
        alertMessage = 'This parcel cannot be updated. Please check the parcel status and payment method.';
      }

      alert(alertMessage);
      return;
    }

    setSelectedParcel(parcel);
    setUpdateData({
      amountPaid: parcel.totalAmount, // Default to total amount
      transactionCode: ''
    });
    setShowUpdateModal(true);
  };

  const handleCloseModal = () => {
    setSelectedParcel(null);
    setShowUpdateModal(false);
    setUpdateData({ amountPaid: 0, transactionCode: '' });
  };

  const handleSaveUpdate = async () => {
    if (!selectedParcel) return;

    try {
      setUpdating(true);

      // Use the specific payment update method with user ID
      const success = await wmsApi.updateParcelPayment(selectedParcel.id, {
        amountPaid: updateData.amountPaid,
        transactionCode: updateData.transactionCode,
        updatedById: user?.id ? parseInt(user.id) : undefined
      });

      if (success) {
        // Show success confirmation
        alert('Payment information updated successfully!');
        // Refresh the parcels list
        await fetchBranchParcels();
        handleCloseModal();
      } else {
        setError('Failed to update parcel payment information');
      }
    } catch (err) {
      setError('Failed to update parcel payment information');
    } finally {
      setUpdating(false);
    }
  };

  const handleFilterChange = (key: string, value: string | { from: string; to: string }) => {
    setFilters(prev => ({ ...prev, [key]: value }));
  };

  const clearFilters = () => {
    setFilters({
      search: '',
      date: new Date().toISOString().split('T')[0],
      status: '',
      paymentMethod: ''
    });
  };

  const filterFields: FilterField[] = useMemo(() => [
    {
      key: 'search',
      label: 'Search',
      type: 'text',
      placeholder: 'Search by waybill, sender, or receiver',
      icon: <Search className="w-4 h-4" />
    },
    {
      key: 'date',
      label: 'Date',
      type: 'date',
      placeholder: 'Select date',
      icon: <Calendar className="w-4 h-4" />
    },
    {
      key: 'status',
      label: 'Status',
      type: 'select',
      placeholder: 'Select status',
      icon: <Package className="w-4 h-4" />,
      options: [
        { value: '', label: 'All Statuses' },
        { value: '0', label: 'Pending' },
        { value: '1', label: 'Confirmed' },
        { value: '2', label: 'In Transit' },
        { value: '3', label: 'Delivered' },
        { value: '4', label: 'Cancelled' }
      ]
    },
    {
      key: 'paymentMethod',
      label: 'Payment Method',
      type: 'select',
      placeholder: 'Select payment method',
      icon: <DollarSign className="w-4 h-4" />,
      options: [
        { value: '', label: 'All Methods' },
        { value: 'cod', label: 'COD' },
        { value: 'paid', label: 'Paid' }
      ]
    }
  ], []);

  const filteredParcels = parcels.filter(parcel => {
    // Search filter
    const matchesSearch = !filters.search ||
      parcel.waybillNumber.toLowerCase().includes(filters.search.toLowerCase()) ||
      parcel.sender.toLowerCase().includes(filters.search.toLowerCase()) ||
      parcel.receiver.toLowerCase().includes(filters.search.toLowerCase());

    // Status filter
    const matchesStatus = !filters.status || parcel.status.toString() === filters.status;

    // Payment method filter
    const matchesPaymentMethod = !filters.paymentMethod ||
      parcel.paymentMethods.toLowerCase().includes(filters.paymentMethod.toLowerCase());

    return matchesSearch && matchesStatus && matchesPaymentMethod;
  });

  // Virtual table columns
  const columns = [
    {
      key: 'waybillNumber',
      header: 'Waybill Number',
      width: 180,
      sortable: true,
      render: (parcel: Parcel) => (
        <div>
          <div className="font-medium text-gray-900 dark:text-white">
            {parcel.waybillNumber}
          </div>
          <div className="text-xs text-gray-500">
            ID: {parcel.id.substring(0, 8)}...
          </div>
        </div>
      )
    },
    {
      key: 'destination',
      header: 'Destination',
      width: 150,
      sortable: true,
      render: (parcel: Parcel) => (
        <div className="flex items-center text-gray-900 dark:text-white">
          <MapPin className="w-4 h-4 mr-1 text-gray-400" />
          {parcel.destination}
        </div>
      )
    },
    {
      key: 'status',
      header: 'Status',
      width: 120,
      sortable: true,
      render: (parcel: Parcel) => getStatusBadge(parcel.status)
    },
    {
      key: 'sender',
      header: 'Sender',
      width: 200,
      sortable: true,
      render: (parcel: Parcel) => (
        <div>
          <div className="text-gray-900 dark:text-white">{parcel.sender}</div>
          <div className="text-xs text-gray-500 flex items-center">
            <Phone className="w-3 h-3 mr-1" />
            {parcel.senderTelephone}
          </div>
        </div>
      )
    },
    {
      key: 'receiver',
      header: 'Receiver',
      width: 200,
      sortable: true,
      render: (parcel: Parcel) => (
        <div>
          <div className="text-gray-900 dark:text-white">{parcel.receiver}</div>
          <div className="text-xs text-gray-500 flex items-center">
            <Phone className="w-3 h-3 mr-1" />
            {parcel.receiverTelephone}
          </div>
        </div>
      )
    },
    {
      key: 'totalAmount',
      header: 'Amount',
      width: 120,
      sortable: true,
      render: (parcel: Parcel) => (
        <div>
          <div className="font-medium">{formatCurrency(parcel.totalAmount)}</div>
          <div className="text-xs text-gray-500">Qty: {parcel.quantity}</div>
        </div>
      )
    },
    {
      key: 'paymentMethods',
      header: 'Payment',
      width: 120,
      sortable: true,
      render: (parcel: Parcel) => (
        <Badge variant="gray">
          {parcel.paymentMethods}
        </Badge>
      )
    },
    {
      key: 'createdBy',
      header: 'Clerk',
      width: 150,
      sortable: true,
      render: (parcel: Parcel) => (
        <div>
          <div className="text-gray-900 dark:text-white text-sm">
            {parcel.createdBy?.username || 'N/A'}
          </div>
          <div className="text-xs text-gray-500">
            Created by
          </div>
        </div>
      )
    },
    {
      key: 'actions',
      header: 'Actions',
      width: 100,
      render: (parcel: Parcel) => {
        const canUpdate = canUpdateParcel(parcel);
        const paymentMethod = parcel.paymentMethods.toLowerCase();

        let tooltipText = '';
        if (!canUpdate) {
          if (paymentMethod.includes('cod')) {
            tooltipText = 'COD parcel must be delivered to update payment';
          } else if (paymentMethod.includes('paid')) {
            tooltipText = 'Paid parcels can be updated at any status';
          } else {
            tooltipText = 'Cannot update this parcel';
          }
        } else {
          tooltipText = 'Update Payment';
        }

        return (
          <div className="flex items-center space-x-1">
            <Button
              variant={canUpdate ? "primary" : "outline"}
              size="sm"
              title={tooltipText}
              onClick={() => handleUpdateParcel(parcel)}
              className={!canUpdate ? "opacity-60 cursor-not-allowed" : ""}
            >
              <Edit className="w-4 h-4" />
            </Button>
          </div>
        );
      }
    }
  ];

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-96">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-brand-500"></div>
      </div>
    );
  }

  if (error) {
    return (
      <Card>
        <div className="text-center py-12">
          <Package className="mx-auto h-12 w-12 text-gray-400 mb-4" />
          <h3 className="text-lg font-medium text-gray-900 mb-2">Error Loading Parcels</h3>
          <p className="text-gray-500 mb-4">{error}</p>
          <Button onClick={() => fetchBranchParcels()}>Try Again</Button>
        </div>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-title-md font-bold text-gray-900 dark:text-white">Collection</h1>
          <p className="text-gray-600 dark:text-gray-400">
            {isBranchManager()
              ? `Update payment information for ${user?.branch?.name || 'your branch'} parcels`
              : 'Update payment information for branch parcels'
            }
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
            onClick={() => fetchBranchParcels()}
            disabled={loading}
          >
            <RefreshCw className={`w-4 h-4 mr-2 ${loading ? 'animate-spin' : ''}`} />
            Refresh
          </Button>
        </div>
      </div>

      {/* Filters */}
      {showFilters && (
        <FilterPanel
          fields={filterFields}
          filters={filters as any}
          onFilterChange={handleFilterChange}
          onClearFilters={clearFilters}
          activeFilters={[]}
          collapsible={true}
          defaultExpanded={true}
        />
      )}

      {/* Parcels Table */}
      <Card padding={false}>
        <Card.Header>
          <div className="flex items-center justify-between">
            <h2 className="text-lg font-semibold text-gray-900 dark:text-white">
              {isBranchManager()
                ? `Branch Parcels for ${filters.date} (${filteredParcels.length})`
                : `Parcels for ${filters.date} (${filteredParcels.length})`
              }
            </h2>
            <div className="text-sm text-gray-500">
              {isBranchManager()
                ? `Branch: ${user?.branch?.name}`
                : `Branch: ${user?.branch?.name || 'All Branches'}`
              }
            </div>
          </div>
        </Card.Header>

        <VirtualScrollTable
          data={filteredParcels}
          columns={columns}
          rowHeight={72}
          containerHeight={600}
          loading={loading}
          emptyMessage={
            isBranchManager()
              ? "No parcels found for the selected date in your branch"
              : "No parcels found for the selected date"
          }
          onRowClick={() => { }}
        />
      </Card>

      {/* Update Modal */}
      {showUpdateModal && selectedParcel && (
        <Modal
          isOpen={showUpdateModal}
          onClose={handleCloseModal}
          title={`Update Parcel Payment - ${selectedParcel.waybillNumber}`}
          icon={<Edit className="w-5 h-5" />}
          size="lg"
        >
          <div className="p-4 flex-1">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              {/* Read-only Parcel Details */}
              <div className="space-y-4">
                <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">Parcel Details</h3>

                {/* Basic Information */}
                <div className="bg-gray-50 dark:bg-gray-700 rounded-lg p-4">
                  <h4 className="text-sm font-semibold text-gray-900 dark:text-white mb-3 flex items-center">
                    <Package className="w-4 h-4 mr-2" />
                    Basic Info
                  </h4>
                  <div className="space-y-2 text-sm">
                    <div className="flex justify-between">
                      <span className="text-gray-500 dark:text-gray-400">Status:</span>
                      <div>{getStatusBadge(selectedParcel.status)}</div>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-500 dark:text-gray-400">Destination:</span>
                      <span className="text-gray-900 dark:text-white">{selectedParcel.destination}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-500 dark:text-gray-400">Quantity:</span>
                      <span className="text-gray-900 dark:text-white">{selectedParcel.quantity}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-500 dark:text-gray-400">Created by:</span>
                      <span className="text-gray-900 dark:text-white font-medium">
                        {selectedParcel.createdBy?.username || 'N/A'}
                      </span>
                    </div>
                    {selectedParcel.description && (
                      <div>
                        <span className="text-gray-500 dark:text-gray-400">Description:</span>
                        <p className="text-gray-900 dark:text-white mt-1">{selectedParcel.description}</p>
                      </div>
                    )}
                  </div>
                </div>

                {/* Contact Information */}
                <div className="bg-gray-50 dark:bg-gray-700 rounded-lg p-4">
                  <h4 className="text-sm font-semibold text-gray-900 dark:text-white mb-3 flex items-center">
                    <User className="w-4 h-4 mr-2" />
                    Contact Info
                  </h4>
                  <div className="space-y-3 text-sm">
                    <div>
                      <div className="text-gray-500 dark:text-gray-400 mb-1">Sender:</div>
                      <div className="text-gray-900 dark:text-white font-medium">{selectedParcel.sender}</div>
                      <div className="text-gray-600 dark:text-gray-300 flex items-center">
                        <Phone className="w-3 h-3 mr-1" />
                        {selectedParcel.senderTelephone}
                      </div>
                    </div>
                    <div>
                      <div className="text-gray-500 dark:text-gray-400 mb-1">Receiver:</div>
                      <div className="text-gray-900 dark:text-white font-medium">{selectedParcel.receiver}</div>
                      <div className="text-gray-600 dark:text-gray-300 flex items-center">
                        <Phone className="w-3 h-3 mr-1" />
                        {selectedParcel.receiverTelephone}
                      </div>
                    </div>
                  </div>
                </div>

                {/* Original Financial Information */}
                <div className="bg-gray-50 dark:bg-gray-700 rounded-lg p-4">
                  <h4 className="text-sm font-semibold text-gray-900 dark:text-white mb-3 flex items-center">
                    <DollarSign className="w-4 h-4 mr-2" />
                    Original Amount
                  </h4>
                  <div className="space-y-2 text-sm">
                    <div className="flex justify-between">
                      <span className="text-gray-500 dark:text-gray-400">Total Amount:</span>
                      <span className="text-gray-900 dark:text-white font-bold text-lg">
                        {formatCurrency(selectedParcel.totalAmount)}
                      </span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-500 dark:text-gray-400">Payment Method:</span>
                      <Badge variant="gray">
                        {selectedParcel.paymentMethods}
                      </Badge>
                    </div>
                  </div>
                </div>

                {/* Clerk Information */}
                <div className="bg-gray-50 dark:bg-gray-700 rounded-lg p-4">
                  <h4 className="text-sm font-semibold text-gray-900 dark:text-white mb-3 flex items-center">
                    <User className="w-4 h-4 mr-2" />
                    Clerk Details
                  </h4>
                  <div className="space-y-2 text-sm">
                    <div className="flex justify-between">
                      <span className="text-gray-500 dark:text-gray-400">Clerk Name:</span>
                      <span className="text-gray-900 dark:text-white font-medium">
                        {selectedParcel.createdBy?.username || 'N/A'}
                      </span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-500 dark:text-gray-400">Clerk ID:</span>
                      <span className="text-gray-900 dark:text-white">
                        {selectedParcel.createdById || 'N/A'}
                      </span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-500 dark:text-gray-400">Created At:</span>
                      <span className="text-gray-900 dark:text-white">
                        {new Date(selectedParcel.createdAt).toLocaleDateString()} {new Date(selectedParcel.createdAt).toLocaleTimeString()}
                      </span>
                    </div>
                  </div>
                </div>
              </div>

              {/* Editable Payment Fields */}
              <div className="space-y-4">
                <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">Update Payment</h3>

                <div className="bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg p-4">
                  <div className="flex items-start">
                    <AlertCircle className="w-5 h-5 text-yellow-600 dark:text-yellow-400 mr-2 mt-0.5" />
                    <div className="text-sm text-yellow-700 dark:text-yellow-300">
                      <p className="font-medium mb-1">Update Requirements:</p>
                      <ul className="list-disc list-inside space-y-1">
                        <li>COD parcels can only be updated when delivered</li>
                        <li>Paid parcels can be updated at any status</li>
                        <li>Transaction code is required for audit trail</li>
                      </ul>
                    </div>
                  </div>
                </div>

                <div className="space-y-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                      Amount Paid
                    </label>
                    <input
                      type="number"
                      min="0"
                      step="0.01"
                      value={updateData.amountPaid}
                      onChange={(e) => setUpdateData(prev => ({ ...prev, amountPaid: parseFloat(e.target.value) || 0 }))}
                      className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg focus:ring-2 focus:ring-brand-500 focus:border-brand-500 bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
                      placeholder="Enter amount paid"
                    />
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                      Transaction Code
                    </label>
                    <input
                      type="text"
                      value={updateData.transactionCode}
                      onChange={(e) => setUpdateData(prev => ({ ...prev, transactionCode: e.target.value }))}
                      className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg focus:ring-2 focus:ring-brand-500 focus:border-brand-500 bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
                      placeholder="Enter transaction reference code"
                    />
                  </div>

                  {/* Summary */}
                  <div className="bg-brand-50 dark:bg-brand-900/20 border border-brand-200 dark:border-brand-800 rounded-lg p-4">
                    <h5 className="font-medium text-brand-900 dark:text-brand-100 mb-2">Payment Summary</h5>
                    <div className="space-y-1 text-sm">
                      <div className="flex justify-between">
                        <span className="text-brand-700 dark:text-brand-300">Original Amount:</span>
                        <span className="font-medium text-brand-900 dark:text-brand-100">
                          {formatCurrency(selectedParcel.totalAmount)}
                        </span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-brand-700 dark:text-brand-300">Amount Paid:</span>
                        <span className="font-medium text-brand-900 dark:text-brand-100">
                          {formatCurrency(updateData.amountPaid)}
                        </span>
                      </div>
                      <div className="flex justify-between border-t border-brand-200 dark:border-brand-800 pt-1">
                        <span className="text-brand-700 dark:text-brand-300">Difference:</span>
                        <span className={`font-medium ${updateData.amountPaid - selectedParcel.totalAmount >= 0
                          ? 'text-green-600 dark:text-green-400'
                          : 'text-red-600 dark:text-red-400'
                          }`}>
                          {formatCurrency(updateData.amountPaid - selectedParcel.totalAmount)}
                        </span>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            {/* Action Buttons */}
            <div className="mt-6 flex justify-end space-x-3 pt-4 border-t border-gray-200 dark:border-gray-700">
              <Button variant="outline" onClick={handleCloseModal}>
                Cancel
              </Button>
              <Button
                variant="primary"
                onClick={handleSaveUpdate}
                disabled={updating || !updateData.transactionCode.trim()}
              >
                {updating ? (
                  <>
                    <div className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2" />
                    Updating...
                  </>
                ) : (
                  <>
                    <Save className="w-4 h-4 mr-2" />
                    Update Payment
                  </>
                )}
              </Button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  );
};

export default BranchParcels;