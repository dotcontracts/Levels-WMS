import React, { useState, useEffect, useCallback } from 'react';
import { useAuth } from '../context/AuthContext';
import { wmsApi } from '../services/wmsApi';
import {
  Users,
  UserPlus,
  Search,
  Filter,
  MoreVertical,
  Edit,
  Trash2,
  Eye,
  EyeOff,
  Shield,
  UserCheck,
  AlertCircle,
  CheckCircle,
  X,
  Save,
  RefreshCw,
  Key,
  Mail,
  Phone,
  MapPin,
  Calendar,
  Settings,
  Lock,
  Unlock
} from 'lucide-react';
import { Modal } from '../components/ui';

interface User {
  id: string;
  username: string;
  email?: string;
  firstName?: string;
  lastName?: string;
  role: string;
  roles: string[];
  branch?: string;
  isActive: boolean;
  canConfirm: boolean;
  canDispatch: boolean;
  lastLogin?: string;
  dateJoined?: string;
  phoneNumber?: string;
}

interface UserFormData {
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  password: string;
  confirmPassword: string;
  role: string;
  branch: string;
  phoneNumber: string;
  isActive: boolean;
  // Role permissions
  canConfirm: boolean;
  canDispatch: boolean;
}

interface UserFilters {
  role: string;
  branch: string;
  status: string;
  searchTerm: string;
}

interface CreateUserModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (data: UserFormData) => void;
  loading: boolean;
  editUser?: User | null;
  branches: { id: number; name: string; address?: string; phone?: string; email?: string }[];
}

const CreateUserModal: React.FC<CreateUserModalProps> = ({
  isOpen,
  onClose,
  onSubmit,
  loading,
  editUser,
  branches
}) => {
  const [formData, setFormData] = useState<UserFormData>({
    username: '',
    email: '',
    firstName: '',
    lastName: '',
    password: '',
    confirmPassword: '',
    role: '',
    branch: '',
    phoneNumber: '',
    isActive: true,
    canConfirm: false,
    canDispatch: false
  });

  const [showPassword, setShowPassword] = useState(false);
  const [errors, setErrors] = useState<Partial<UserFormData>>({});

  // Populate form when editing
  useEffect(() => {
    if (editUser) {
      setFormData({
        username: editUser.username,
        email: editUser.email || '',
        firstName: editUser.firstName || '',
        lastName: editUser.lastName || '',
        password: '',
        confirmPassword: '',
        role: editUser.roles?.[0] || editUser.role,
        branch: editUser.branch || '',
        phoneNumber: editUser.phoneNumber || '',
        isActive: editUser.isActive,
        canConfirm: editUser.canConfirm || false,
        canDispatch: editUser.canDispatch || false
      });
    } else {
      setFormData({
        username: '',
        email: '',
        firstName: '',
        lastName: '',
        password: '',
        confirmPassword: '',
        role: '',
        branch: '',
        phoneNumber: '',
        isActive: true,
        canConfirm: false,
        canDispatch: false
      });
    }
    setErrors({});
  }, [editUser, isOpen]);

  const validateForm = (): boolean => {
    const newErrors: Partial<UserFormData> = {};

    if (!formData.username.trim()) {
      newErrors.username = 'Username is required';
    } else if (formData.username.length < 3) {
      newErrors.username = 'Username must be at least 3 characters';
    }

    if (!formData.email.trim()) {
      newErrors.email = 'Email is required';
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      newErrors.email = 'Invalid email format';
    }

    if (!formData.firstName.trim()) {
      newErrors.firstName = 'First name is required';
    }

    if (!formData.lastName.trim()) {
      newErrors.lastName = 'Last name is required';
    }

    if (!editUser) { // Only validate password for new users
      if (!formData.password) {
        newErrors.password = 'Password is required';
      } else if (formData.password.length < 6) {
        newErrors.password = 'Password must be at least 6 characters';
      }

      if (formData.password !== formData.confirmPassword) {
        newErrors.confirmPassword = 'Passwords do not match';
      }
    }

    if (!formData.role) {
      newErrors.role = 'Role is required';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (validateForm()) {
      onSubmit(formData);
    }
  };

  if (!isOpen) return null;

  const availableRoles = ['Admin', 'Manager', 'Employee', 'Clerk', 'Driver', 'Accountant'];

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={editUser ? 'Edit User' : 'Create New User'}
      icon={editUser ? <Edit className="w-5 h-5" /> : <UserPlus className="w-5 h-5" />}
      size="xl"
    >
      <div className="p-6">
        <form onSubmit={handleSubmit} className="space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="form-label">Username *</label>
              <input
                type="text"
                value={formData.username}
                onChange={(e) => setFormData(prev => ({ ...prev, username: e.target.value }))}
                className={`form-input ${errors.username ? 'border-error-500' : ''}`}
                placeholder="Enter username"
                disabled={!!editUser} // Don't allow username changes when editing
              />
              {errors.username && (
                <p className="mt-1 text-sm text-error-600">{errors.username}</p>
              )}
            </div>

            <div>
              <label className="form-label">Email *</label>
              <input
                type="email"
                value={formData.email}
                onChange={(e) => setFormData(prev => ({ ...prev, email: e.target.value }))}
                className={`form-input ${errors.email ? 'border-error-500' : ''}`}
                placeholder="Enter email address"
              />
              {errors.email && (
                <p className="mt-1 text-sm text-error-600">{errors.email}</p>
              )}
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="form-label">First Name *</label>
              <input
                type="text"
                value={formData.firstName}
                onChange={(e) => setFormData(prev => ({ ...prev, firstName: e.target.value }))}
                className={`form-input ${errors.firstName ? 'border-error-500' : ''}`}
                placeholder="Enter first name"
              />
              {errors.firstName && (
                <p className="mt-1 text-sm text-error-600">{errors.firstName}</p>
              )}
            </div>

            <div>
              <label className="form-label">Last Name *</label>
              <input
                type="text"
                value={formData.lastName}
                onChange={(e) => setFormData(prev => ({ ...prev, lastName: e.target.value }))}
                className={`form-input ${errors.lastName ? 'border-error-500' : ''}`}
                placeholder="Enter last name"
              />
              {errors.lastName && (
                <p className="mt-1 text-sm text-error-600">{errors.lastName}</p>
              )}
            </div>
          </div>

          {!editUser && ( // Only show password fields for new users
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="form-label">Password *</label>
                <div className="relative">
                  <input
                    type={showPassword ? 'text' : 'password'}
                    value={formData.password}
                    onChange={(e) => setFormData(prev => ({ ...prev, password: e.target.value }))}
                    className={`form-input pr-10 ${errors.password ? 'border-error-500' : ''}`}
                    placeholder="Enter password"
                  />
                  <button
                    type="button"
                    className="absolute inset-y-0 right-0 flex items-center pr-3"
                    onClick={() => setShowPassword(!showPassword)}
                  >
                    {showPassword ? (
                      <EyeOff className="w-5 h-5 text-gray-400" />
                    ) : (
                      <Eye className="w-5 h-5 text-gray-400" />
                    )}
                  </button>
                </div>
                {errors.password && (
                  <p className="mt-1 text-sm text-error-600">{errors.password}</p>
                )}
              </div>

              <div>
                <label className="form-label">Confirm Password *</label>
                <input
                  type="password"
                  value={formData.confirmPassword}
                  onChange={(e) => setFormData(prev => ({ ...prev, confirmPassword: e.target.value }))}
                  className={`form-input ${errors.confirmPassword ? 'border-error-500' : ''}`}
                  placeholder="Confirm password"
                />
                {errors.confirmPassword && (
                  <p className="mt-1 text-sm text-error-600">{errors.confirmPassword}</p>
                )}
              </div>
            </div>
          )}

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="form-label">Role *</label>
              <select
                value={formData.role}
                onChange={(e) => setFormData(prev => ({ ...prev, role: e.target.value }))}
                className={`form-input ${errors.role ? 'border-error-500' : ''}`}
              >
                <option value="">Select role</option>
                {availableRoles.map(role => (
                  <option key={role} value={role}>{role}</option>
                ))}
              </select>
              {errors.role && (
                <p className="mt-1 text-sm text-error-600">{errors.role}</p>
              )}
            </div>

            <div>
              <label className="form-label">Branch</label>
              <select
                value={formData.branch}
                onChange={(e) => setFormData(prev => ({ ...prev, branch: e.target.value }))}
                className="form-input"
              >
                <option value="">No specific branch</option>
                {branches.map(branch => (
                  <option key={branch.id} value={branch.name}>{branch.name}</option>
                ))}
              </select>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="form-label">Phone Number</label>
              <input
                type="tel"
                value={formData.phoneNumber}
                onChange={(e) => setFormData(prev => ({ ...prev, phoneNumber: e.target.value }))}
                className="form-input"
                placeholder="Enter phone number"
              />
            </div>

            <div className="flex items-center pt-7">
              <input
                type="checkbox"
                id="isActive"
                checked={formData.isActive}
                onChange={(e) => setFormData(prev => ({ ...prev, isActive: e.target.checked }))}
                className="form-checkbox"
              />
              <label htmlFor="isActive" className="ml-2 text-sm text-gray-700">
                Active User
              </label>
            </div>
          </div>

          {/* Role Permissions */}
          <div className="border-t border-gray-200 pt-6">
            <h3 className="text-lg font-medium text-gray-900 mb-4">Role Permissions</h3>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div className="flex items-center">
                <input
                  type="checkbox"
                  id="canConfirm"
                  checked={formData.canConfirm}
                  onChange={(e) => setFormData(prev => ({ ...prev, canConfirm: e.target.checked }))}
                  className="form-checkbox"
                />
                <label htmlFor="canConfirm" className="ml-2 text-sm text-gray-700">
                  Can Confirm Parcels
                </label>
              </div>

              <div className="flex items-center">
                <input
                  type="checkbox"
                  id="canDispatch"
                  checked={formData.canDispatch}
                  onChange={(e) => setFormData(prev => ({ ...prev, canDispatch: e.target.checked }))}
                  className="form-checkbox"
                />
                <label htmlFor="canDispatch" className="ml-2 text-sm text-gray-700">
                  Can Dispatch Parcels
                </label>
              </div>
            </div>
            <p className="mt-2 text-sm text-gray-500">
              These permissions control what actions users can perform in the system.
            </p>
          </div>

          {/* Submit Buttons */}
          <div className="flex items-center justify-end space-x-3 pt-4 border-t border-gray-200">
            <button
              type="button"
              onClick={onClose}
              className="btn-secondary"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={loading}
              className="btn-primary flex items-center justify-center min-w-[140px] disabled:opacity-50"
            >
              {loading ? (
                <>
                  <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></div>
                  {editUser ? 'Updating...' : 'Creating...'}
                </>
              ) : (
                <>
                  <Save className="w-4 h-4 mr-2" />
                  {editUser ? 'Update User' : 'Create User'}
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </Modal>
  );
};

const UserManagement: React.FC = () => {
  const [users, setUsers] = useState<User[]>([]);
  const [filteredUsers, setFilteredUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const [creating, setCreating] = useState(false);
  const [error, setError] = useState('');
  const [successMessage, setSuccessMessage] = useState('');
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [editingUser, setEditingUser] = useState<User | null>(null);
  const [showFilters, setShowFilters] = useState(false);
  const [branches, setBranches] = useState<{ id: number; name: string; address?: string; phone?: string; email?: string }[]>([]);

  const [filters, setFilters] = useState<UserFilters>({
    role: '',
    branch: '',
    status: '',
    searchTerm: ''
  });

  const { user: currentUser } = useAuth();

  // Mock data for development (since we don't have a complete users API)
  const generateMockUsers = useCallback((): User[] => {
    return [
      {
        id: '1',
        username: 'admin',
        email: 'admin@wms.com',
        firstName: 'System',
        lastName: 'Administrator',
        role: 'Admin',
        roles: ['Admin'],
        branch: '',
        isActive: true,
        canConfirm: false,
        canDispatch: false,
        lastLogin: new Date().toISOString(),
        dateJoined: '2023-01-01T00:00:00Z',
        phoneNumber: '+254700000001'
      },
      {
        id: '2',
        username: 'nairobi_manager',
        email: 'nairobi.manager@wms.com',
        firstName: 'John',
        lastName: 'Doe',
        role: 'Manager',
        roles: ['Manager'],
        branch: 'Nairobi',
        isActive: true,
        canConfirm: false,
        canDispatch: false,
        lastLogin: new Date(Date.now() - 86400000).toISOString(),
        dateJoined: '2023-02-15T00:00:00Z',
        phoneNumber: '+254700000002'
      },
      {
        id: '3',
        username: 'mombasa_manager',
        email: 'mombasa.manager@wms.com',
        firstName: 'Jane',
        lastName: 'Smith',
        role: 'Manager',
        roles: ['Manager'],
        branch: 'Mombasa',
        isActive: true,
        canConfirm: false,
        canDispatch: false,
        lastLogin: new Date(Date.now() - 172800000).toISOString(),
        dateJoined: '2023-03-01T00:00:00Z',
        phoneNumber: '+254700000003'
      },
      {
        id: '4',
        username: 'clerk_001',
        email: 'clerk001@wms.com',
        firstName: 'Mary',
        lastName: 'Johnson',
        role: 'Clerk',
        roles: ['Clerk'],
        branch: 'Nairobi',
        isActive: true,
        canConfirm: false,
        canDispatch: false,
        lastLogin: new Date(Date.now() - 43200000).toISOString(),
        dateJoined: '2023-04-10T00:00:00Z',
        phoneNumber: '+254700000004'
      },
      {
        id: '5',
        username: 'driver_001',
        email: 'driver001@wms.com',
        firstName: 'Peter',
        lastName: 'Wilson',
        role: 'Driver',
        roles: ['Driver'],
        branch: 'Mombasa',
        isActive: false,
        canConfirm: false,
        canDispatch: false,
        lastLogin: new Date(Date.now() - 604800000).toISOString(),
        dateJoined: '2023-05-20T00:00:00Z',
        phoneNumber: '+254700000005'
      }
    ];
  }, []);

  const fetchBranches = useCallback(async () => {
    try {
      const branchData = await wmsApi.getBranches();
      setBranches(branchData);
    } catch (err) {
      // Fallback to default branches
      setBranches([
        { id: 1, name: 'Nairobi' },
        { id: 2, name: 'Mombasa' },
        { id: 3, name: 'Kisumu' },
        { id: 4, name: 'Nakuru' },
        { id: 5, name: 'Eldoret' }
      ]);
    }
  }, []);

  const fetchUsers = useCallback(async () => {
    try {
      setLoading(true);
      setError('');

      // Fetch real user data from API
      const userData = await wmsApi.getUsers();

      // Transform API data to match component interface
      const transformedUsers: User[] = userData.map(user => ({
        id: user.id.toString(),
        username: user.username || '',
        email: user.email,
        firstName: user.firstName,
        lastName: user.lastName,
        role: user.roles?.[0] || user.role?.name || 'Employee',
        roles: user.roles || (user.role?.name ? [user.role.name] : ['Employee']),
        branch: typeof user.branch === 'string' ? user.branch : user.branch?.name || undefined,
        isActive: user.isActive !== undefined ? user.isActive : true,
        canConfirm: user.canConfirm || user.role?.canConfirm || false,
        canDispatch: user.canDispatch || user.role?.canDispatch || false,
        dateJoined: user.createdAt || new Date().toISOString(),
        lastLogin: undefined
      }));

      setUsers(transformedUsers);
      setFilteredUsers(transformedUsers);

    } catch (err) {
      setError('Failed to load users');
      // Fallback to mock data if API fails
      const mockData = generateMockUsers();
      setUsers(mockData);
      setFilteredUsers(mockData);
    } finally {
      setLoading(false);
    }
  }, [generateMockUsers]);

  // Helper functions to map role/branch names to IDs
  const getRoleId = (roleName: string): number => {
    const roleMap: Record<string, number> = {
      'Admin': 1,
      'Manager': 2,
      'Clerk': 3,
      'Driver': 4,
      'Accountant': 5,
      'Employee': 6
    };
    return roleMap[roleName] || 6; // Default to Employee
  };

  const getBranchId = (branchName?: string): number | undefined => {
    if (!branchName) return undefined;
    const branch = branches.find(b => b.name === branchName);
    return branch?.id;
  };

  const handleCreateUser = async (formData: UserFormData) => {
    try {
      setCreating(true);

      if (editingUser) {
        // Update existing user
        await wmsApi.updateUser(parseInt(editingUser.id), {
          id: parseInt(editingUser.id),
          username: formData.username,
          email: formData.email,
          firstName: formData.firstName,
          lastName: formData.lastName,
          roleId: getRoleId(formData.role),
          branchId: getBranchId(formData.branch),
          isActive: formData.isActive,
          canConfirm: formData.canConfirm,
          canDispatch: formData.canDispatch
        });
        setSuccessMessage('User updated successfully');
      } else {
        // Create new user
        await wmsApi.createUser({
          username: formData.username,
          email: formData.email,
          firstName: formData.firstName,
          lastName: formData.lastName,
          password: formData.password || 'defaultPassword123', // You might want to handle this differently
          roleId: getRoleId(formData.role),
          branchId: getBranchId(formData.branch)
        });
        setSuccessMessage('User created successfully');
      }

      // Refresh the user list
      await fetchUsers();

      setShowCreateModal(false);
      setEditingUser(null);

    } catch (err) {
      setError(editingUser ? 'Failed to update user' : 'Failed to create user');
    } finally {
      setCreating(false);
    }
  };

  const handleDeleteUser = async (userId: string) => {
    if (!window.confirm('Are you sure you want to delete this user? This action cannot be undone.')) {
      return;
    }

    try {
      await wmsApi.deleteUser(parseInt(userId));

      // Remove user from local state
      setUsers(prev => prev.filter(u => u.id !== userId));
      setFilteredUsers(prev => prev.filter(u => u.id !== userId));
      setSuccessMessage('User deleted successfully');
    } catch (err) {
      setError('Failed to delete user');
    }
  };

  const handleToggleUserStatus = async (userId: string) => {
    try {
      const user = users.find(u => u.id === userId);
      if (!user) return;

      if (user.isActive) {
        await wmsApi.deactivateUser(parseInt(userId));
      } else {
        await wmsApi.activateUser(parseInt(userId));
      }

      // Update local state
      const updatedUsers = users.map(u =>
        u.id === userId ? { ...u, isActive: !u.isActive } : u
      );
      setUsers(updatedUsers);
      setFilteredUsers(prev => prev.map(u =>
        u.id === userId ? { ...u, isActive: !u.isActive } : u
      ));

      setSuccessMessage(`User ${user.isActive ? 'deactivated' : 'activated'} successfully`);
    } catch (err) {
      setError('Failed to update user status');
    }
  };

  // Filter users based on current filters
  useEffect(() => {
    let filtered = users;

    // Apply search filter
    if (filters.searchTerm) {
      filtered = filtered.filter(user =>
        user.username.toLowerCase().includes(filters.searchTerm.toLowerCase()) ||
        user.email?.toLowerCase().includes(filters.searchTerm.toLowerCase()) ||
        user.firstName?.toLowerCase().includes(filters.searchTerm.toLowerCase()) ||
        user.lastName?.toLowerCase().includes(filters.searchTerm.toLowerCase())
      );
    }

    // Apply role filter
    if (filters.role) {
      filtered = filtered.filter(user => user.role === filters.role);
    }

    // Apply branch filter
    if (filters.branch) {
      filtered = filtered.filter(user => user.branch === filters.branch);
    }

    // Apply status filter
    if (filters.status) {
      const isActive = filters.status === 'active';
      filtered = filtered.filter(user => user.isActive === isActive);
    }

    setFilteredUsers(filtered);
  }, [users, filters]);

  useEffect(() => {
    fetchBranches();
    fetchUsers();
  }, [fetchBranches, fetchUsers]);

  // Auto-hide success message
  useEffect(() => {
    if (successMessage) {
      const timer = setTimeout(() => setSuccessMessage(''), 5000);
      return () => clearTimeout(timer);
    }
  }, [successMessage]);

  const clearFilters = () => {
    setFilters({
      role: '',
      branch: '',
      status: '',
      searchTerm: ''
    });
  };

  // Get unique values for filters
  const availableRoles = Array.from(new Set(users.map(u => u.role).filter(Boolean)));
  const availableBranchNames = branches.map(b => b.name);

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-500 mx-auto mb-4"></div>
          <p className="text-gray-600">Loading users...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between space-y-4 sm:space-y-0">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 flex items-center">
            <Users className="w-8 h-8 text-primary-500 mr-3" />
            User Management
          </h1>
          <p className="text-gray-600 mt-1">
            Manage system users, roles, and permissions
          </p>
        </div>

        <div className="flex items-center space-x-3">
          <button
            onClick={fetchUsers}
            disabled={loading}
            className="btn-secondary"
          >
            <RefreshCw className={`w-4 h-4 mr-2 ${loading ? 'animate-spin' : ''}`} />
            Refresh
          </button>

          <button
            onClick={() => {
              setEditingUser(null);
              setShowCreateModal(true);
            }}
            className="btn-primary"
          >
            <UserPlus className="w-4 h-4 mr-2" />
            Add User
          </button>
        </div>
      </div>

      {/* Statistics Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <div className="stats-card">
          <div className="flex items-center">
            <div className="p-2 bg-primary-100 rounded-lg">
              <Users className="w-6 h-6 text-primary-600" />
            </div>
            <div className="ml-4">
              <p className="text-sm text-gray-600">Total Users</p>
              <p className="text-2xl font-semibold text-gray-900">{users.length}</p>
            </div>
          </div>
        </div>

        <div className="stats-card">
          <div className="flex items-center">
            <div className="p-2 bg-success-100 rounded-lg">
              <UserCheck className="w-6 h-6 text-success-600" />
            </div>
            <div className="ml-4">
              <p className="text-sm text-gray-600">Active Users</p>
              <p className="text-2xl font-semibold text-gray-900">
                {users.filter(u => u.isActive).length}
              </p>
            </div>
          </div>
        </div>

        <div className="stats-card">
          <div className="flex items-center">
            <div className="p-2 bg-info-100 rounded-lg">
              <Shield className="w-6 h-6 text-info-600" />
            </div>
            <div className="ml-4">
              <p className="text-sm text-gray-600">Admins</p>
              <p className="text-2xl font-semibold text-gray-900">
                {users.filter(u => u.role === 'Admin').length}
              </p>
            </div>
          </div>
        </div>

        <div className="stats-card">
          <div className="flex items-center">
            <div className="p-2 bg-warning-100 rounded-lg">
              <Settings className="w-6 h-6 text-warning-600" />
            </div>
            <div className="ml-4">
              <p className="text-sm text-gray-600">Managers</p>
              <p className="text-2xl font-semibold text-gray-900">
                {users.filter(u => u.role === 'Manager').length}
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Success Message */}
      {successMessage && (
        <div className="flex items-center space-x-3 p-4 bg-success-50 border border-success-200 rounded-lg">
          <CheckCircle className="w-5 h-5 text-success-500 flex-shrink-0" />
          <span className="text-success-700">{successMessage}</span>
          <button
            onClick={() => setSuccessMessage('')}
            className="ml-auto text-success-500 hover:text-success-700"
          >
            <X className="w-4 h-4" />
          </button>
        </div>
      )}

      {/* Error Alert */}
      {error && (
        <div className="flex items-center space-x-3 p-4 bg-error-50 border border-error-200 rounded-lg">
          <AlertCircle className="w-5 h-5 text-error-500 flex-shrink-0" />
          <span className="text-error-700">{error}</span>
          <button
            onClick={() => setError('')}
            className="ml-auto text-error-500 hover:text-error-700"
          >
            <X className="w-4 h-4" />
          </button>
        </div>
      )}

      {/* Search and Filters */}
      <div className="card">
        <div className="card-content">
          <div className="flex flex-col lg:flex-row lg:items-center space-y-4 lg:space-y-0 lg:space-x-4">
            {/* Search */}
            <div className="flex-1 relative">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-5 h-5" />
              <input
                type="text"
                placeholder="Search users by name, username, or email..."
                value={filters.searchTerm}
                onChange={(e) => setFilters(prev => ({ ...prev, searchTerm: e.target.value }))}
                className="form-input pl-10"
              />
            </div>

            {/* Filter Toggle */}
            <button
              onClick={() => setShowFilters(!showFilters)}
              className="btn-secondary flex items-center"
            >
              <Filter className="w-4 h-4 mr-2" />
              Filters
            </button>

            {/* Clear Filters */}
            <button
              onClick={clearFilters}
              className="btn-secondary"
            >
              Clear All
            </button>
          </div>

          {/* Advanced Filters */}
          {showFilters && (
            <div className="mt-4 pt-4 border-t border-gray-200">
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <div>
                  <label className="form-label">Role</label>
                  <select
                    value={filters.role}
                    onChange={(e) => setFilters(prev => ({ ...prev, role: e.target.value }))}
                    className="form-input"
                  >
                    <option value="">All Roles</option>
                    {availableRoles.map(role => (
                      <option key={role} value={role}>{role}</option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="form-label">Branch</label>
                  <select
                    value={filters.branch}
                    onChange={(e) => setFilters(prev => ({ ...prev, branch: e.target.value }))}
                    className="form-input"
                  >
                    <option value="">All Branches</option>
                    {availableBranchNames.map(branch => (
                      <option key={branch} value={branch}>{branch}</option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="form-label">Status</label>
                  <select
                    value={filters.status}
                    onChange={(e) => setFilters(prev => ({ ...prev, status: e.target.value }))}
                    className="form-input"
                  >
                    <option value="">All Statuses</option>
                    <option value="active">Active</option>
                    <option value="inactive">Inactive</option>
                  </select>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Results Summary */}
      <div className="flex items-center justify-between">
        <p className="text-sm text-gray-600">
          Showing {filteredUsers.length} of {users.length} users
        </p>

        {filteredUsers.length !== users.length && (
          <span className="text-sm text-primary-600 bg-primary-50 px-2 py-1 rounded">
            Filtered from {users.length} total
          </span>
        )}
      </div>

      {/* Users Table */}
      <div className="card overflow-hidden">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  User
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Role & Branch
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Contact
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Status
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Last Login
                </th>
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {filteredUsers.map((user) => (
                <tr key={user.id} className="hover:bg-gray-50 transition-colors">
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="flex items-center">
                      <div className="w-10 h-10 bg-primary-500 rounded-full flex items-center justify-center text-white font-medium">
                        {user.firstName?.charAt(0) || user.username.charAt(0).toUpperCase()}
                      </div>
                      <div className="ml-4">
                        <div className="text-sm font-medium text-gray-900">
                          {user.firstName} {user.lastName}
                        </div>
                        <div className="text-sm text-gray-500">@{user.username}</div>
                      </div>
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-sm text-gray-900">
                      <div className="flex items-center">
                        <Shield className="w-4 h-4 text-gray-400 mr-2" />
                        <span className="font-medium">
                          {typeof user.role === 'string' ? user.role : (user.role as any)?.name || 'User'}
                        </span>
                      </div>
                      {user.branch && (
                        <div className="flex items-center mt-1">
                          <MapPin className="w-4 h-4 text-gray-400 mr-2" />
                          <span className="text-gray-500">{user.branch}</span>
                        </div>
                      )}
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-sm text-gray-900">
                      {user.email && (
                        <div className="flex items-center">
                          <Mail className="w-4 h-4 text-gray-400 mr-2" />
                          <span>{user.email}</span>
                        </div>
                      )}
                      {user.phoneNumber && (
                        <div className="flex items-center mt-1">
                          <Phone className="w-4 h-4 text-gray-400 mr-2" />
                          <span className="text-gray-500">{user.phoneNumber}</span>
                        </div>
                      )}
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${user.isActive
                      ? 'bg-success-100 text-success-800'
                      : 'bg-error-100 text-error-800'
                      }`}>
                      {user.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                    {user.lastLogin ? (
                      <div className="flex items-center">
                        <Calendar className="w-4 h-4 text-gray-400 mr-1" />
                        {new Date(user.lastLogin).toLocaleDateString()}
                      </div>
                    ) : (
                      'Never'
                    )}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                    <div className="flex items-center justify-end space-x-2">
                      <button
                        onClick={() => {
                          setEditingUser(user);
                          setShowCreateModal(true);
                        }}
                        className="text-primary-600 hover:text-primary-800 transition-colors"
                        title="Edit User"
                      >
                        <Edit className="w-4 h-4" />
                      </button>

                      <button
                        onClick={() => handleToggleUserStatus(user.id)}
                        className={`transition-colors ${user.isActive
                          ? 'text-warning-600 hover:text-warning-800'
                          : 'text-success-600 hover:text-success-800'
                          }`}
                        title={user.isActive ? 'Deactivate User' : 'Activate User'}
                      >
                        {user.isActive ? <Lock className="w-4 h-4" /> : <Unlock className="w-4 h-4" />}
                      </button>

                      {currentUser?.role === 'admin' && user.id !== currentUser.id && (
                        <button
                          onClick={() => handleDeleteUser(user.id)}
                          className="text-error-600 hover:text-error-800 transition-colors"
                          title="Delete User"
                        >
                          <Trash2 className="w-4 h-4" />
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          {filteredUsers.length === 0 && (
            <div className="text-center py-12">
              <Users className="w-12 h-12 text-gray-400 mx-auto mb-4" />
              <h3 className="text-lg font-medium text-gray-900 mb-2">No users found</h3>
              <p className="text-gray-500">
                {filteredUsers.length !== users.length
                  ? 'No users match your current filters. Try adjusting your search criteria.'
                  : 'No users have been created yet.'
                }
              </p>
              {filteredUsers.length === 0 && users.length === 0 && (
                <button
                  onClick={() => setShowCreateModal(true)}
                  className="btn-primary mt-4"
                >
                  <UserPlus className="w-4 h-4 mr-2" />
                  Create First User
                </button>
              )}
            </div>
          )}
        </div>
      </div>

      {/* Create/Edit User Modal */}
      <CreateUserModal
        isOpen={showCreateModal}
        onClose={() => {
          setShowCreateModal(false);
          setEditingUser(null);
        }}
        onSubmit={handleCreateUser}
        loading={creating}
        editUser={editingUser}
        branches={branches}
      />
    </div>
  );
};

export default UserManagement;