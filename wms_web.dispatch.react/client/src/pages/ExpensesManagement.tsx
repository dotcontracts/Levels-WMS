import React, { useState, useEffect, useMemo } from 'react';
import {
  Receipt,
  Plus,
  Search,
  Filter,
  Download,
  Eye,
  CheckCircle,
  XCircle,
  Clock,
  AlertCircle,
  DollarSign,
  Calendar,
  User,
  Building2,
  Car,
  Fuel,
  Wrench,
  RefreshCw,
  FileText,
  ThumbsUp,
  ThumbsDown
} from 'lucide-react';
import { Card, Button, Badge, Input, Modal, Select } from '../components/ui';
import { VirtualScrollTable } from '../components/VirtualScrollTable';
import { wmsApi, DailyExpense, CreateDailyExpenseDto, ApproveExpenseDto } from '../services/wmsApi';
import { useAuth } from '../context/AuthContext';
import { fmtDate } from '../utils/dateUtils';

interface ExpenseFilters {
  search: string;
  category: string;
  status: string;
  branchId: string;
  clerkId: string;
  dateRange: string;
}

interface ExpenseStats {
  totalExpenses: number;
  pendingExpenses: number;
  approvedExpenses: number;
  rejectedExpenses: number;
  monthlyTotal: number;
  dailyAverage: number;
}

const ExpensesManagement: React.FC = () => {
  const { user } = useAuth();
  const [expenses, setExpenses] = useState<DailyExpense[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filters, setFilters] = useState<ExpenseFilters>({
    search: '',
    category: '',
    status: '',
    branchId: '',
    clerkId: '',
    dateRange: ''
  });
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showApprovalModal, setShowApprovalModal] = useState(false);
  const [selectedExpense, setSelectedExpense] = useState<DailyExpense | null>(null);
  const [rejectionReason, setRejectionReason] = useState('');
  const [newExpense, setNewExpense] = useState<CreateDailyExpenseDto>({
    date: new Date().toISOString().split('T')[0],
    category: 'other',
    description: '',
    amount: 0,
    vendor: '',
    receiptNumber: '',
    branchId: undefined,
    clerkId: undefined
  });
  const [expenseType, setExpenseType] = useState<'clerk' | 'operational'>('operational');
  const [branches, setBranches] = useState<{id: number, name: string}[]>([]);
  const [users, setUsers] = useState<{id: number, username: string, firstName?: string, lastName?: string, branchId?: number}[]>([]);

  const expenseCategories = [
    { value: 'fuel', label: 'Fuel', icon: Fuel },
    { value: 'casual_labor', label: 'Casual Labor', icon: User },
    { value: 'hired_cars', label: 'Hired Cars', icon: Car },
    { value: 'maintenance', label: 'Maintenance', icon: Wrench },
    { value: 'office_supplies', label: 'Office Supplies', icon: FileText },
    { value: 'utilities', label: 'Utilities', icon: Building2 },
    { value: 'transport', label: 'Transport', icon: Car },
    { value: 'other', label: 'Other', icon: Receipt },
    { value: 'cod_expense', label: 'COD Delivery Expense', icon: Receipt }
  ];

  useEffect(() => {
    loadExpenseData();
    loadBranchesAndUsers();
  }, []);

  const loadBranchesAndUsers = async () => {
    try {
      const [branchData, userData] = await Promise.all([
        wmsApi.getBranches(),
        wmsApi.getUsers()
      ]);
      console.log('Loaded branches:', branchData);
      console.log('Loaded users:', userData);
      setBranches(branchData);
      setUsers(userData);
    } catch (err) {
      console.error('Failed to load branches/users:', err);
    }
  };

  const loadExpenseData = async () => {
    try {
      setIsLoading(true);
      setError(null);
      const expenseData = await wmsApi.getDailyExpenses();
      setExpenses(expenseData);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load expense data');
    } finally {
      setIsLoading(false);
    }
  };

  const calculateStats = (): ExpenseStats => {
    const totalExpenses = expenses.reduce((sum, exp) => sum + exp.amount, 0);
    const pendingExpenses = expenses
      .filter(exp => exp.status === 'pending')
      .reduce((sum, exp) => sum + exp.amount, 0);
    const approvedExpenses = expenses
      .filter(exp => exp.status === 'approved')
      .reduce((sum, exp) => sum + exp.amount, 0);
    const rejectedExpenses = expenses
      .filter(exp => exp.status === 'rejected')
      .reduce((sum, exp) => sum + exp.amount, 0);

    // Calculate monthly total (current month - APPROVED expenses only)
    const currentMonth = new Date().getMonth();
    const currentYear = new Date().getFullYear();
    const monthlyTotal = expenses
      .filter(exp => {
        const expDate = new Date(exp.date);
        return expDate.getMonth() === currentMonth && 
               expDate.getFullYear() === currentYear &&
               exp.status === 'approved'; // Only include approved expenses
      })
      .reduce((sum, exp) => sum + exp.amount, 0);

    const dailyAverage = expenses.length > 0 ? totalExpenses / expenses.length : 0;

    return {
      totalExpenses,
      pendingExpenses,
      approvedExpenses,
      rejectedExpenses,
      monthlyTotal,
      dailyAverage
    };
  };

  const stats = calculateStats();

  const canApproveExpenses = (): boolean => {
    if (!user) return false;
    
    // Check by roleId (most reliable)
    if (user.roleId === 1) return true; // Admin
    
    // Check by role name
    const roleName = typeof user.role === 'string' ? user.role : (user.role as any)?.Name || '';
    const allowedRoles = ['admin', 'administrator', 'accountant'];
    return allowedRoles.includes(roleName.toLowerCase());
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-KE', {
      style: 'currency',
      currency: 'KES'
    }).format(amount);
  };

  const formatDate = (dateString: string) => fmtDate(dateString);

  const getCategoryIcon = (category: string) => {
    const categoryConfig = expenseCategories.find(cat => cat.value === category);
    return categoryConfig ? categoryConfig.icon : Receipt;
  };

  const getStatusBadge = (status: DailyExpense['status']) => {
    const statusConfig = {
      pending: { color: 'yellow' as const, icon: Clock },
      approved: { color: 'green' as const, icon: CheckCircle },
      rejected: { color: 'red' as const, icon: XCircle }
    };
    
    const config = statusConfig[status];
    const Icon = config.icon;
    
    return (
      <Badge color={config.color} className="flex items-center gap-1">
        <Icon className="w-3 h-3" />
        {status.charAt(0).toUpperCase() + status.slice(1)}
      </Badge>
    );
  };

  const filteredExpenses = useMemo(() => {
    return expenses.filter(expense => {
      if (filters.search && !expense.description.toLowerCase().includes(filters.search.toLowerCase()) &&
          !expense.vendor?.toLowerCase().includes(filters.search.toLowerCase()) &&
          !expense.receiptNumber?.toLowerCase().includes(filters.search.toLowerCase())) {
        return false;
      }
      if (filters.category && expense.category !== filters.category) {
        return false;
      }
      if (filters.status && expense.status !== filters.status) {
        return false;
      }
      return true;
    });
  }, [expenses, filters]);

  const columns = [
    {
      key: 'date',
      header: 'Date',
      sortable: true,
      render: (expense: DailyExpense) => formatDate(expense.date)
    },
    {
      key: 'category',
      header: 'Category',
      sortable: true,
      render: (expense: DailyExpense) => {
        const Icon = getCategoryIcon(expense.category);
        return (
          <div className="flex items-center space-x-2">
            <Icon className="w-4 h-4 text-gray-500" />
            <span className="capitalize">{expense.category.replace('_', ' ')}</span>
          </div>
        );
      }
    },
    {
      key: 'description',
      header: 'Description',
      render: (expense: DailyExpense) => (
        <div>
          <div className="font-medium text-gray-900">{expense.description}</div>
          {expense.vendor && (
            <div className="text-sm text-gray-500">Vendor: {expense.vendor}</div>
          )}
          {expense.receiptNumber && (
            <div className="text-sm text-blue-600">Receipt: {expense.receiptNumber}</div>
          )}
        </div>
      )
    },
    {
      key: 'amount',
      header: 'Amount',
      sortable: true,
      render: (expense: DailyExpense) => (
        <div className="font-medium">{formatCurrency(expense.amount)}</div>
      )
    },
    {
      key: 'requestedBy',
      header: 'Requested By',
      render: (expense: DailyExpense) => (
        <div>
          <div className="font-medium text-gray-900">{expense.createdByName || 'Unknown'}</div>
          {expense.branchName && (
            <div className="text-sm text-gray-500">{expense.branchName}</div>
          )}
          {expense.clerkName && (
            <div className="text-sm text-gray-500">Clerk: {expense.clerkName}</div>
          )}
        </div>
      )
    },
    {
      key: 'status',
      header: 'Status',
      sortable: true,
      render: (expense: DailyExpense) => (
        <div>
          {getStatusBadge(expense.status)}
          {expense.approvedByName && (
            <div className="text-xs text-gray-500 mt-1">By: {expense.approvedByName}</div>
          )}
          {expense.rejectionReason && (
            <div className="text-xs text-red-600 mt-1">{expense.rejectionReason}</div>
          )}
        </div>
      )
    },
    {
      key: 'actions',
      header: 'Actions',
      render: (expense: DailyExpense) => (
        <div className="flex items-center space-x-1 min-w-max">
          <Button
            variant="outline"
            size="sm"
            onClick={() => setSelectedExpense(expense)}
            className="flex items-center space-x-1 px-2 py-1"
          >
            <Eye className="w-3 h-3" />
            <span className="text-xs">View</span>
          </Button>
          {expense.status === 'pending' && canApproveExpenses() && (
            <>
              <Button
                variant="outline"
                size="sm"
                onClick={() => handleApproveExpense(expense, true)}
                className="flex items-center space-x-1 px-2 py-1 text-green-600 hover:text-green-700 border-green-200 hover:border-green-300"
              >
                <ThumbsUp className="w-3 h-3" />
                <span className="text-xs">Approve</span>
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() => handleApproveExpense(expense, false)}
                className="flex items-center space-x-1 px-2 py-1 text-red-600 hover:text-red-700 border-red-200 hover:border-red-300"
              >
                <ThumbsDown className="w-3 h-3" />
                <span className="text-xs">Reject</span>
              </Button>
            </>
          )}
        </div>
      )
    }
  ];

  const handleApproveExpense = async (expense: DailyExpense, approved: boolean) => {
    if (!approved) {
      setSelectedExpense(expense);
      setShowApprovalModal(true);
      return;
    }

    try {
      await wmsApi.approveExpense({
        expenseId: expense.id,
        approved: true
      });
      await loadExpenseData();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to approve expense');
    }
  };

  const handleRejectExpense = async (expense: DailyExpense) => {
    try {
      if (!rejectionReason.trim()) {
        setError('Please provide a rejection reason');
        return;
      }

      await wmsApi.approveExpense({
        expenseId: expense.id,
        approved: false,
        rejectionReason: rejectionReason.trim()
      });
      
      await loadExpenseData();
      setShowApprovalModal(false);
      setSelectedExpense(null);
      setRejectionReason('');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to reject expense');
    }
  };

  const handleCreateExpense = async () => {
    try {
      if (!newExpense.description || newExpense.amount <= 0) {
        setError('Please fill in all required fields');
        return;
      }

      // Prepare expense data based on expense type
      let expenseData: CreateDailyExpenseDto;

      if (expenseType === 'clerk') {
        if (!newExpense.branchId) {
          setError('Please select a branch for this clerk expense');
          return;
        }

        if (!newExpense.clerkId) {
          setError('Please select a clerk for this expense');
          return;
        }

        // Get the selected branch and clerk names
        const selectedBranch = branches.find(b => b.id === newExpense.branchId);
        const selectedClerk = users.find(u => u.id === newExpense.clerkId);

        if (!selectedBranch || !selectedClerk) {
          setError('Selected branch or clerk not found');
          return;
        }

        expenseData = {
          ...newExpense,
          branchName: selectedBranch.name,
          clerkName: selectedClerk.firstName && selectedClerk.lastName 
            ? `${selectedClerk.firstName} ${selectedClerk.lastName}`
            : selectedClerk.username
        };
      } else {
        // Operational expense - no clerk required
        let selectedBranch = null;
        if (newExpense.branchId) {
          selectedBranch = branches.find(b => b.id === newExpense.branchId);
          if (!selectedBranch) {
            setError('Selected branch not found');
            return;
          }
        }

        expenseData = {
          ...newExpense,
          branchName: selectedBranch?.name || 'General Operations',
          clerkName: 'N/A - Operational Expense',
          clerkId: undefined // Clear clerk ID for operational expenses
        };
      }

      console.log('Sending expense data:', expenseData);
      console.log('JSON payload:', JSON.stringify(expenseData, null, 2));

      await wmsApi.createDailyExpense(expenseData);
      await loadExpenseData();
      setShowCreateModal(false);
      setNewExpense({
        date: new Date().toISOString().split('T')[0],
        category: 'other',
        description: '',
        amount: 0,
        vendor: '',
        receiptNumber: '',
        branchId: undefined,
        clerkId: undefined
      });
      setExpenseType('operational');
    } catch (err) {
      console.error('Error creating expense:', err);
      console.error('Error details:', JSON.stringify(err, null, 2));
      setError(err instanceof Error ? err.message : 'Failed to create expense');
    }
  };

  const resetCreateModal = () => {
    setShowCreateModal(false);
    setNewExpense({
      date: new Date().toISOString().split('T')[0],
      category: 'other',
      description: '',
      amount: 0,
      vendor: '',
      receiptNumber: ''
    });
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-brand-500"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex flex-col items-center justify-center min-h-screen space-y-4">
        <AlertCircle className="w-16 h-16 text-red-500" />
        <h2 className="text-xl font-semibold text-gray-900">Error Loading Expenses</h2>
        <p className="text-gray-600">{error}</p>
        <Button onClick={loadExpenseData}>Try Again</Button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center space-y-4 sm:space-y-0">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Expense Management</h1>
          <p className="text-gray-600 mt-1">Track and approve daily operational expenses</p>
        </div>
        <div className="flex items-center space-x-2">
          <Button 
            onClick={loadExpenseData}
            variant="outline"
            className="flex items-center space-x-2"
          >
            <RefreshCw className="w-4 h-4" />
            <span>Refresh</span>
          </Button>
          <Button onClick={() => setShowCreateModal(true)} className="flex items-center space-x-2">
            <Plus className="w-4 h-4" />
            <span>Add Expense</span>
          </Button>
        </div>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <Card className="p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-600">Total Requests</p>
              <p className="text-2xl font-bold text-gray-900">{formatCurrency(stats.totalExpenses)}</p>
              <p className="text-sm text-gray-500 mt-1">All expenses submitted</p>
            </div>
            <Receipt className="w-8 h-8 text-gray-500" />
          </div>
        </Card>

        <Card className="p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-600">Pending Approval</p>
              <p className="text-2xl font-bold text-yellow-600">{formatCurrency(stats.pendingExpenses)}</p>
              <p className="text-sm text-gray-500 mt-1">Awaiting review</p>
            </div>
            <Clock className="w-8 h-8 text-yellow-500" />
          </div>
        </Card>

        <Card className="p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-600">Approved Total</p>
              <p className="text-2xl font-bold text-green-600">{formatCurrency(stats.approvedExpenses)}</p>
              <p className="text-sm text-gray-500 mt-1">All approved expenses</p>
            </div>
            <CheckCircle className="w-8 h-8 text-green-500" />
          </div>
        </Card>

        <Card className="p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-600">Monthly Approved</p>
              <p className="text-2xl font-bold text-blue-600">{formatCurrency(stats.monthlyTotal)}</p>
              <p className="text-sm text-gray-500 mt-1">This month (approved only)</p>
            </div>
            <Calendar className="w-8 h-8 text-blue-500" />
          </div>
        </Card>
      </div>

      {/* Filters */}
      <Card className="p-4">
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          <div className="relative">
            <Search className="absolute left-3 top-3 h-4 w-4 text-gray-400" />
            <Input
              placeholder="Search expenses..."
              value={filters.search}
              onChange={(e) => setFilters(prev => ({ ...prev, search: e.target.value }))}
              className="pl-10"
            />
          </div>
          <Select
            value={filters.category}
            onChange={(value) => setFilters(prev => ({ ...prev, category: (value as unknown) as string }))}
            options={[
              { value: "", label: "All Categories" },
              ...expenseCategories.map(cat => ({
                value: cat.value,
                label: cat.label
              }))
            ]}
          />
          <Select
            value={filters.status}
            onChange={(value) => setFilters(prev => ({ ...prev, status: (value as unknown) as string }))}
            options={[
              { value: "", label: "All Statuses" },
              { value: "pending", label: "Pending" },
              { value: "approved", label: "Approved" },
              { value: "rejected", label: "Rejected" }
            ]}
          />
          <Button
            variant="outline"
            onClick={() => setFilters({ search: '', category: '', status: '', branchId: '', clerkId: '', dateRange: '' })}
            className="flex items-center space-x-2"
          >
            <Filter className="w-4 h-4" />
            <span>Clear Filters</span>
          </Button>
        </div>
      </Card>

      {/* Expenses Table */}
      <Card>
        <div className="p-6 border-b border-gray-200">
          <div className="flex items-center justify-between">
            <h2 className="text-lg font-medium text-gray-900">
              Expense Records ({filteredExpenses.length})
            </h2>
            <Button variant="outline" className="flex items-center space-x-2">
              <Download className="w-4 h-4" />
              <span>Export</span>
            </Button>
          </div>
        </div>
        <div className="overflow-x-auto">
          <div style={{ minWidth: '1200px' }}>
            <VirtualScrollTable
              data={filteredExpenses}
              columns={columns}
              containerHeight={600}
              className="border-0 w-full"
            />
          </div>
        </div>
      </Card>

      {/* Create Expense Modal */}
      <Modal
        isOpen={showCreateModal}
        onClose={resetCreateModal}
        title="Add New Expense"
        size="lg"
      >
        <div className="space-y-6">
          {/* Expense Type Selection */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Expense Type *
            </label>
            <Select
              value={expenseType}
              onChange={(value) => {
                const newType = (value as unknown) as 'clerk' | 'operational';
                setExpenseType(newType);
                // Reset clerk/branch when switching types
                if (newType === 'operational') {
                  setNewExpense(prev => ({ ...prev, clerkId: undefined }));
                }
              }}
              options={[
                { value: 'operational', label: 'Operational Expense (Office rent, utilities, general supplies)' },
                { value: 'clerk', label: 'Clerk-Specific Expense (Fuel, transport, clerk supplies)' }
              ]}
              required
            />
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Date *
              </label>
              <Input
                type="date"
                value={newExpense.date}
                onChange={(e) => setNewExpense(prev => ({ ...prev, date: e.target.value }))}
                required
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Category *
              </label>
              <Select
                value={newExpense.category}
                onChange={(value) => setNewExpense(prev => ({ ...prev, category: value as any }))}
                options={expenseCategories.map(cat => ({
                  value: cat.value,
                  label: cat.label
                }))}
                required
              />
            </div>
          </div>

          {/* Branch and Clerk Selection - Conditional based on expense type */}
          {expenseType === 'clerk' && (
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Branch *
                </label>
                <Select
                  value={newExpense.branchId?.toString() || ''}
                  onChange={(value) => setNewExpense(prev => ({ 
                    ...prev, 
                    branchId: (value as unknown) as string ? parseInt((value as unknown) as string) : undefined,
                    clerkId: undefined // Reset clerk when branch changes
                  }))}
                  options={[
                    { value: '', label: 'Select Branch' },
                    ...branches.map(branch => ({
                      value: branch.id.toString(),
                      label: branch.name
                    }))
                  ]}
                  required
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Clerk *
                </label>
                <Select
                  value={newExpense.clerkId?.toString() || ''}
                  onChange={(value) => setNewExpense(prev => ({ 
                    ...prev, 
                    clerkId: (value as unknown) as string ? parseInt((value as unknown) as string) : undefined 
                  }))}
                  options={[
                    { value: '', label: 'Select Clerk' },
                    ...users
                      .filter(user => {
                        const shouldInclude = !newExpense.branchId || user.branchId === newExpense.branchId;
                        console.log(`User ${user.username} (branchId: ${user.branchId}) - Selected branch: ${newExpense.branchId} - Include: ${shouldInclude}`);
                        return shouldInclude;
                      })
                      .map(user => ({
                        value: user.id.toString(),
                        label: user.firstName && user.lastName 
                          ? `${user.firstName} ${user.lastName} (${user.username})` 
                          : user.username
                      }))
                  ]}
                  disabled={!newExpense.branchId}
                  required
                />
              </div>
            </div>
          )}

          {/* Optional Branch Selection for Operational Expenses */}
          {expenseType === 'operational' && (
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Branch (Optional)
              </label>
              <Select
                value={newExpense.branchId?.toString() || ''}
                onChange={(value) => setNewExpense(prev => ({ 
                  ...prev, 
                  branchId: (value as unknown) as string ? parseInt((value as unknown) as string) : undefined
                }))}
                options={[
                  { value: '', label: 'General Operations (No specific branch)' },
                  ...branches.map(branch => ({
                    value: branch.id.toString(),
                    label: branch.name
                  }))
                ]}
              />
              <p className="text-xs text-gray-500 mt-1">
                Leave as "General Operations" for company-wide expenses like office rent, utilities, etc.
              </p>
            </div>
          )}

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Description *
            </label>
            <Input
              value={newExpense.description}
              onChange={(e) => setNewExpense(prev => ({ ...prev, description: e.target.value }))}
              placeholder="Enter expense description..."
              required
            />
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Amount (KES) *
              </label>
              <Input
                type="number"
                value={newExpense.amount}
                onChange={(e) => setNewExpense(prev => ({ ...prev, amount: parseFloat(e.target.value) || 0 }))}
                placeholder="0.00"
                min="0"
                step="0.01"
                required
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Receipt Number
              </label>
              <Input
                value={newExpense.receiptNumber || ''}
                onChange={(e) => setNewExpense(prev => ({ ...prev, receiptNumber: e.target.value }))}
                placeholder="Optional receipt number"
              />
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Vendor/Supplier
            </label>
            <Input
              value={newExpense.vendor || ''}
              onChange={(e) => setNewExpense(prev => ({ ...prev, vendor: e.target.value }))}
              placeholder="Optional vendor name"
            />
          </div>

          {error && (
            <div className="bg-red-50 border border-red-200 rounded-md p-3">
              <p className="text-sm text-red-600">{error}</p>
            </div>
          )}

          <div className="flex justify-end space-x-3 pt-4 border-t">
            <Button
              variant="outline"
              onClick={resetCreateModal}
            >
              Cancel
            </Button>
            <Button
              onClick={handleCreateExpense}
              className="flex items-center space-x-2"
            >
              <Plus className="w-4 h-4" />
              <span>Create Expense</span>
            </Button>
          </div>
        </div>
      </Modal>

      {/* Expense Details Modal */}
      <Modal
        isOpen={!!selectedExpense && !showApprovalModal}
        onClose={() => setSelectedExpense(null)}
        title="Expense Details"
      >
        {selectedExpense && (
          <div className="space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Date</label>
                <p className="text-sm text-gray-900">{fmtDate(selectedExpense.date)}</p>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Category</label>
                <p className="text-sm text-gray-900 capitalize">{selectedExpense.category.replace('_', ' ')}</p>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Amount</label>
                <p className="text-sm font-semibold text-gray-900">{formatCurrency(selectedExpense.amount)}</p>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Status</label>
                <span className={`px-2 py-1 text-xs font-medium rounded-full ${
                  selectedExpense.status === 'approved' ? 'bg-green-100 text-green-700' :
                  selectedExpense.status === 'rejected' ? 'bg-red-100 text-red-700' :
                  'bg-yellow-100 text-yellow-700'
                }`}>
                  {selectedExpense.status}
                </span>
              </div>
            </div>
            
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
              <p className="text-sm text-gray-900">{selectedExpense.description}</p>
            </div>
            
            {selectedExpense.vendor && (
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Vendor</label>
                <p className="text-sm text-gray-900">{selectedExpense.vendor}</p>
              </div>
            )}
            
            {selectedExpense.receiptNumber && (
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Receipt Number</label>
                <p className="text-sm text-gray-900">{selectedExpense.receiptNumber}</p>
              </div>
            )}
            
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Requested By</label>
              <p className="text-sm text-gray-900">{selectedExpense.createdByName}</p>
              <p className="text-xs text-gray-500">{selectedExpense.branchName}</p>
            </div>
            
            {selectedExpense.clerkName && (
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Clerk</label>
                <p className="text-sm text-gray-900">{selectedExpense.clerkName}</p>
              </div>
            )}
            
            {selectedExpense.approvedByName && (
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  {selectedExpense.status === 'approved' ? 'Approved By' : 'Processed By'}
                </label>
                <p className="text-sm text-gray-900">{selectedExpense.approvedByName}</p>
              </div>
            )}
            
            {selectedExpense.rejectionReason && (
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Rejection Reason</label>
                <p className="text-sm text-red-600">{selectedExpense.rejectionReason}</p>
              </div>
            )}
            
            <div className="flex justify-end space-x-3 pt-4 border-t">
              <Button variant="outline" onClick={() => setSelectedExpense(null)}>
                Close
              </Button>
              {selectedExpense.status === 'pending' && canApproveExpenses() && (
                <>
                  <Button
                    onClick={() => handleApproveExpense(selectedExpense, true)}
                    className="bg-green-600 hover:bg-green-700 text-white"
                  >
                    Approve
                  </Button>
                  <Button
                    onClick={() => {
                      setShowApprovalModal(true);
                    }}
                    className="bg-red-600 hover:bg-red-700 text-white"
                  >
                    Reject
                  </Button>
                </>
              )}
            </div>
          </div>
        )}
      </Modal>

      {/* Rejection Modal */}
      <Modal
        isOpen={showApprovalModal}
        onClose={() => {
          setShowApprovalModal(false);
          setSelectedExpense(null);
        }}
        title="Reject Expense"
      >
        {selectedExpense && (
          <div className="space-y-4">
            <div className="bg-red-50 border border-red-200 rounded-md p-4">
              <h4 className="text-sm font-medium text-red-800">
                You are about to reject this expense:
              </h4>
              <p className="text-sm text-red-700 mt-1">
                {selectedExpense.description} - {formatCurrency(selectedExpense.amount)}
              </p>
            </div>
            
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Rejection Reason *
              </label>
              <textarea
                className="w-full min-h-[100px] p-3 border border-gray-300 rounded-md focus:ring-2 focus:ring-brand-500 focus:border-transparent"
                placeholder="Please provide a reason for rejecting this expense..."
                value={rejectionReason}
                onChange={(e) => setRejectionReason(e.target.value)}
                required
              />
            </div>
            
            {error && (
              <div className="bg-red-50 border border-red-200 rounded-md p-3">
                <p className="text-sm text-red-600">{error}</p>
              </div>
            )}
            
            <div className="flex justify-end space-x-3 pt-4 border-t">
              <Button
                variant="outline"
                onClick={() => {
                  setShowApprovalModal(false);
                  setRejectionReason('');
                }}
              >
                Cancel
              </Button>
              <Button
                onClick={() => handleRejectExpense(selectedExpense)}
                disabled={!rejectionReason.trim()}
                className="bg-red-600 hover:bg-red-700 text-white"
              >
                Reject Expense
              </Button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
};

export default ExpensesManagement;