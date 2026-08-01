import React, { useState, useEffect, useMemo } from 'react';
import { 
  FileText, 
  Plus, 
  Search, 
  CheckSquare, 
  Square,
  ArrowLeft,
  Package,
  User,
  Calendar,
  DollarSign
} from 'lucide-react';
import { Card, Button, Input, Select, Table } from '../components/ui';
import { VirtualScrollTable } from '../components/VirtualScrollTable';
import { wmsApi, Parcel, ContractCustomer, CreateInvoiceDto } from '../services/wmsApi';
import { useAuth } from '../context/AuthContext';
import { useNavigate } from 'react-router-dom';

const CreateInvoice: React.FC = () => {
  const { user } = useAuth();
  const navigate = useNavigate();
  
  const [contractCustomers, setContractCustomers] = useState<ContractCustomer[]>([]);
  const [parcels, setParcels] = useState<Parcel[]>([]);
  const [selectedParcelIds, setSelectedParcelIds] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  
  const [formData, setFormData] = useState({
    contractCustomerId: '',
    issueDate: new Date().toISOString().split('T')[0],
    dueDate: '',
    billingPeriodStart: '',
    billingPeriodEnd: '',
    notes: ''
  });
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [searchTerm, setSearchTerm] = useState('');

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async (billingStart?: string, billingEnd?: string) => {
    try {
      setIsLoading(true);
      setError(null);
      const [customersData, parcelsData] = await Promise.all([
        wmsApi.getContractCustomers(),
        wmsApi.getEligibleParcelsForInvoicing(billingStart, billingEnd) // Pass billing period to backend
      ]);
      setContractCustomers(customersData);
      setParcels(parcelsData);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load data');
    } finally {
      setIsLoading(false);
    }
  };

  // Reload parcels when billing period changes
  const reloadParcelsForBillingPeriod = async () => {
    if (formData.billingPeriodStart && formData.billingPeriodEnd) {
      await loadData(formData.billingPeriodStart, formData.billingPeriodEnd);
    } else {
      await loadData(); // Load all eligible parcels if no period specified
    }
  };

  const filteredParcels = useMemo(() => {
    console.log('🔍 Frontend search filtering:', {
      totalEligibleParcels: parcels.length,
      searchTerm,
      hasBillingPeriod: !!(formData.billingPeriodStart && formData.billingPeriodEnd)
    });

    // Backend already filters by status, payment method, dispatch status, and billing period
    // Frontend only handles search term filtering
    let filtered = parcels;

    // Apply search filter only
    if (searchTerm) {
      const beforeSearchFilter = filtered.length;
      filtered = filtered.filter(parcel =>
        parcel.waybillNumber.toLowerCase().includes(searchTerm.toLowerCase()) ||
        parcel.sender.toLowerCase().includes(searchTerm.toLowerCase()) ||
        parcel.receiver.toLowerCase().includes(searchTerm.toLowerCase()) ||
        parcel.destination.toLowerCase().includes(searchTerm.toLowerCase())
      );
      console.log(`🔍 After search filter: ${filtered.length} (was ${beforeSearchFilter})`);
    }

    console.log('✅ Final filtered parcels:', filtered.length);
    return filtered;
  }, [parcels, searchTerm, formData.billingPeriodStart, formData.billingPeriodEnd]);

  const handleParcelSelection = (parcelId: string, selected: boolean) => {
    if (selected) {
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

  const handleChange = async (field: string, value: string) => {
    const newFormData = { ...formData, [field]: value };
    setFormData(newFormData);
    
    if (errors[field]) {
      setErrors(prev => ({ ...prev, [field]: '' }));
    }
    
    // Reload parcels and clear selections when billing period changes
    if (field === 'billingPeriodStart' || field === 'billingPeriodEnd') {
      setSelectedParcelIds([]);
      
      // Reload parcels if both dates are now set
      if (newFormData.billingPeriodStart && newFormData.billingPeriodEnd) {
        await loadData(newFormData.billingPeriodStart, newFormData.billingPeriodEnd);
      } else {
        // If one of the dates was cleared, reload all eligible parcels
        await loadData();
      }
    }
  };

  const validateForm = () => {
    const newErrors: Record<string, string> = {};

    if (!formData.contractCustomerId) {
      newErrors.contractCustomerId = 'Customer is required';
    }
    if (!formData.issueDate) {
      newErrors.issueDate = 'Issue date is required';
    }
    if (!formData.dueDate) {
      newErrors.dueDate = 'Due date is required';
    }
    if (!formData.billingPeriodStart) {
      newErrors.billingPeriodStart = 'Billing period start is required';
    }
    if (!formData.billingPeriodEnd) {
      newErrors.billingPeriodEnd = 'Billing period end is required';
    }
    if (selectedParcelIds.length === 0) {
      newErrors.parcels = 'At least one parcel must be selected';
    }

    // Validate date logic
    if (formData.issueDate && formData.dueDate && formData.issueDate > formData.dueDate) {
      newErrors.dueDate = 'Due date must be after issue date';
    }
    if (formData.billingPeriodStart && formData.billingPeriodEnd && formData.billingPeriodStart > formData.billingPeriodEnd) {
      newErrors.billingPeriodEnd = 'End date must be after start date';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!validateForm()) {
      return;
    }

    try {
      setIsSubmitting(true);
      
      // Safely get user ID as number
      const userId = user?.id ? (typeof user.id === 'number' ? user.id : parseInt(String(user.id))) : 1;
      
      const invoiceData: CreateInvoiceDto = {
        contractCustomerId: parseInt(formData.contractCustomerId),
        issueDate: formData.issueDate,
        dueDate: formData.dueDate,
        billingPeriodStart: formData.billingPeriodStart,
        billingPeriodEnd: formData.billingPeriodEnd,
        notes: formData.notes,
        createdById: userId,
        parcelIds: selectedParcelIds
      };

      await wmsApi.createInvoice(invoiceData);
      
      // Navigate back to invoices list
      navigate('/invoices');
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Failed to create invoice');
    } finally {
      setIsSubmitting(false);
    }
  };

  const calculateTotals = () => {
    const selectedParcels = parcels.filter(p => selectedParcelIds.includes(p.id));
    const total = selectedParcels.reduce((sum, p) => sum + p.totalAmount, 0); // VAT-inclusive total
    const taxRate = 0.16; // 16% VAT
    const subtotal = total / (1 + taxRate); // Extract base amount
    const tax = total - subtotal; // VAT component
    return { subtotal, tax, total, count: selectedParcels.length };
  };

  const totals = calculateTotals();
  const selectedCustomer = contractCustomers.find(c => c.id.toString() === formData.contractCustomerId);

  const parcelColumns = [
    {
      key: 'select',
      header: (
        <div className="flex items-center">
          <button type="button" onClick={handleSelectAll} className="p-1">
            {selectedParcelIds.length === filteredParcels.length && filteredParcels.length > 0 ? 
              <CheckSquare className="w-4 h-4 text-brand-600" /> : 
              <Square className="w-4 h-4 text-gray-400" />
            }
          </button>
        </div>
      ),
      render: (parcel: Parcel) => {
        const isSelected = selectedParcelIds.includes(parcel.id);
        return (
          <button 
            type="button"
            onClick={() => handleParcelSelection(parcel.id, !isSelected)}
            className="p-1"
          >
            {isSelected ? 
              <CheckSquare className="w-4 h-4 text-brand-600" /> : 
              <Square className="w-4 h-4 text-gray-400" />
            }
          </button>
        );
      }
    },
    {
      key: 'waybillNumber',
      header: 'Waybill',
      render: (parcel: Parcel) => (
        <div className="font-medium text-gray-900">{parcel.waybillNumber}</div>
      )
    },
    {
      key: 'sender',
      header: 'Sender',
      render: (parcel: Parcel) => (
        <div>
          <div className="font-medium">{parcel.sender}</div>
          <div className="text-sm text-gray-500">{parcel.senderTelephone}</div>
        </div>
      )
    },
    {
      key: 'receiver',
      header: 'Receiver',
      render: (parcel: Parcel) => (
        <div>
          <div className="font-medium">{parcel.receiver}</div>
          <div className="text-sm text-gray-500">{parcel.destination}</div>
        </div>
      )
    },
    {
      key: 'description',
      header: 'Description',
      render: (parcel: Parcel) => (
        <div>
          <div>{parcel.description}</div>
          <div className="text-sm text-gray-500">Qty: {parcel.quantity}</div>
        </div>
      )
    },
    {
      key: 'dispatchedAt',
      header: 'Dispatched',
      render: (parcel: Parcel) => (
        <div className="text-sm text-gray-600">
          {parcel.dispatchedAt ? new Date(parcel.dispatchedAt).toLocaleDateString('en-GB', {
            day: '2-digit',
            month: 'short',
            year: 'numeric'
          }) : 'N/A'}
        </div>
      )
    },
    {
      key: 'amount',
      header: 'Amount',
      render: (parcel: Parcel) => (
        <div className="text-right">
          <div className="font-medium">KES {parcel.totalAmount.toLocaleString()}</div>
          <div className="text-sm text-gray-500">{parcel.paymentMethods}</div>
        </div>
      )
    }
  ];

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-brand-500"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-4">
          <Button
            variant="outline"
            onClick={() => navigate('/invoices')}
            className="flex items-center space-x-2"
          >
            <ArrowLeft className="w-4 h-4" />
            <span>Back to Invoices</span>
          </Button>
          <div>
            <h1 className="text-3xl font-bold text-gray-900">Create New Invoice</h1>
            <p className="text-gray-600 mt-1">Select parcels and configure billing details</p>
          </div>
        </div>
      </div>

      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Invoice Details */}
        <Card className="p-6">
          <h2 className="text-lg font-medium text-gray-900 mb-4 flex items-center">
            <FileText className="w-5 h-5 mr-2" />
            Invoice Details
          </h2>
          
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                <User className="w-4 h-4 inline mr-1" />
                Contract Customer *
              </label>
              <Select
                value={formData.contractCustomerId}
                onChange={(value) => handleChange('contractCustomerId', String(value))}
                options={[
                  { value: '', label: 'Select a customer' },
                  ...contractCustomers.map(customer => ({
                    value: customer.id.toString(),
                    label: `${customer.name} - ${customer.companyName}`
                  }))
                ]}
              />
              {errors.contractCustomerId && (
                <p className="text-sm text-red-600 mt-1">{errors.contractCustomerId}</p>
              )}
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                <Calendar className="w-4 h-4 inline mr-1" />
                Issue Date *
              </label>
              <Input
                type="date"
                value={formData.issueDate}
                onChange={(e) => handleChange('issueDate', e.target.value)}
              />
              {errors.issueDate && (
                <p className="text-sm text-red-600 mt-1">{errors.issueDate}</p>
              )}
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                <Calendar className="w-4 h-4 inline mr-1" />
                Due Date *
              </label>
              <Input
                type="date"
                value={formData.dueDate}
                onChange={(e) => handleChange('dueDate', e.target.value)}
              />
              {errors.dueDate && (
                <p className="text-sm text-red-600 mt-1">{errors.dueDate}</p>
              )}
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Billing Period Start *
              </label>
              <Input
                type="date"
                value={formData.billingPeriodStart}
                onChange={(e) => handleChange('billingPeriodStart', e.target.value)}
              />
              {errors.billingPeriodStart && (
                <p className="text-sm text-red-600 mt-1">{errors.billingPeriodStart}</p>
              )}
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Billing Period End *
              </label>
              <Input
                type="date"
                value={formData.billingPeriodEnd}
                onChange={(e) => handleChange('billingPeriodEnd', e.target.value)}
              />
              {errors.billingPeriodEnd && (
                <p className="text-sm text-red-600 mt-1">{errors.billingPeriodEnd}</p>
              )}
            </div>
          </div>

          <div className="mt-6">
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Notes
            </label>
            <textarea
              value={formData.notes}
              onChange={(e) => handleChange('notes', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-brand-500"
              rows={3}
              placeholder="Optional notes for this invoice..."
            />
          </div>
        </Card>

        {/* Parcel Selection */}
        <Card className="p-6">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-lg font-medium text-gray-900 flex items-center">
              <Package className="w-5 h-5 mr-2" />
              Select Parcels ({selectedParcelIds.length} selected)
            </h2>
            <div className="flex items-center space-x-4">
              <div className="relative">
                <Search className="absolute left-3 top-3 h-4 w-4 text-gray-400" />
                <Input
                  placeholder="Search parcels..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="pl-10 w-64"
                />
              </div>
            </div>
          </div>

          {errors.parcels && (
            <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-md">
              <p className="text-sm text-red-600">{errors.parcels}</p>
            </div>
          )}

          <VirtualScrollTable
            data={filteredParcels}
            columns={parcelColumns}
            containerHeight={400}
            className="border border-gray-200 rounded-md"
          />

          <div className="mt-4 text-sm text-gray-600">
            Showing {filteredParcels.length} confirmed/delivered parcels with contract payment
            {formData.billingPeriodStart && formData.billingPeriodEnd && (
              <span className="ml-2 px-2 py-1 bg-blue-100 text-blue-800 rounded-md">
                Filtered by dispatch period: {new Date(formData.billingPeriodStart).toLocaleDateString('en-GB')} - {new Date(formData.billingPeriodEnd).toLocaleDateString('en-GB')}
              </span>
            )}
          </div>
        </Card>

        {/* Invoice Summary */}
        {selectedParcelIds.length > 0 && (
          <Card className="p-6">
            <h2 className="text-lg font-medium text-gray-900 mb-4 flex items-center">
              <DollarSign className="w-5 h-5 mr-2" />
              Invoice Summary
            </h2>
            
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div>
                <h3 className="font-medium text-gray-900 mb-2">Customer Details</h3>
                {selectedCustomer && (
                  <div className="text-sm text-gray-600 space-y-1">
                    <div><strong>Name:</strong> {selectedCustomer.name}</div>
                    <div><strong>Company:</strong> {selectedCustomer.companyName}</div>
                    <div><strong>Email:</strong> {selectedCustomer.email}</div>
                    <div><strong>Phone:</strong> {selectedCustomer.phone}</div>
                  </div>
                )}
              </div>
              
              <div>
                <h3 className="font-medium text-gray-900 mb-2">Invoice Totals</h3>
                <div className="text-sm space-y-2">
                  <div className="flex justify-between">
                    <span>Parcels:</span>
                    <span>{totals.count} items</span>
                  </div>
                  <div className="flex justify-between">
                    <span>Subtotal:</span>
                    <span>KES {totals.subtotal.toLocaleString()}</span>
                  </div>
                  <div className="flex justify-between">
                    <span>VAT (16%):</span>
                    <span>KES {totals.tax.toLocaleString()}</span>
                  </div>
                  <div className="flex justify-between border-t pt-2 font-medium">
                    <span>Total:</span>
                    <span>KES {totals.total.toLocaleString()}</span>
                  </div>
                </div>
              </div>
            </div>
          </Card>
        )}

        {/* Actions */}
        <div className="flex justify-end space-x-3 pt-4">
          <Button
            type="button"
            variant="outline"
            onClick={() => navigate('/invoices')}
            disabled={isSubmitting}
          >
            Cancel
          </Button>
          <Button
            type="submit"
            disabled={isSubmitting || selectedParcelIds.length === 0}
            isLoading={isSubmitting}
          >
            Create Invoice
          </Button>
        </div>
      </form>

      {error && (
        <div className="fixed bottom-4 right-4 bg-red-50 border border-red-200 rounded-md p-4 max-w-md">
          <p className="text-sm text-red-600">{error}</p>
        </div>
      )}
    </div>
  );
};

export default CreateInvoice;