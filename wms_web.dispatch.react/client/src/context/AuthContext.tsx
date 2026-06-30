import React, { createContext, useContext, useState, useEffect } from 'react';
import { buildApiUrl } from '../config/api';

interface User {
  id: string;
  username: string;
  role: string;
  roleId?: number;
  branchId?: number;
  canConfirm?: boolean;
  canDispatch?: boolean;
  branch?: {
    id: number;
    name: string;
  };
  firstName?: string;
  lastName?: string;
  email?: string;
}

interface AuthContextType {
  user: User | null;
  token: string | null;
  sessionId: string | null;
  login: (username: string, password: string) => Promise<void>;
  logout: () => void;
  isAuthenticated: boolean;
  isAdmin: () => boolean;
  isBranchManager: () => boolean;
  isAccountant: () => boolean;
  canAccessMenu: (menuPath: string) => boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState<string | null>(localStorage.getItem('token'));
  const [sessionId, setSessionId] = useState<string | null>(localStorage.getItem('sessionId'));

  useEffect(() => {
    if (token) {
      const userData = localStorage.getItem('user');
      if (userData) {
        setUser(JSON.parse(userData));
      }
    }
  }, [token]);

  const login = async (username: string, password: string) => {
    try {
      // Use new session-based login endpoint
      const loginRequest = {
        username: username,
        password: password,
        clientType: 'Web',
        deviceInfo: navigator.userAgent
      };

      const response = await fetch(buildApiUrl('auth/login'), {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(loginRequest),
      });

      if (!response.ok) {
        const errorText = await response.text();
        try {
          const errorData = JSON.parse(errorText);
          throw new Error(errorData.message || `Login failed: ${response.status}`);
        } catch {
          throw new Error(`Login failed: ${response.status} - ${errorText}`);
        }
      }

      const data = await response.json();
      
      // Handle role field - it might be a string or an object
      let roleValue = data.Role || data.role || '';
      if (typeof roleValue === 'object' && roleValue?.name) {
        roleValue = roleValue.name;
      }

      // Check if role is allowed to access the system
      const userRoleId = data.RoleId || data.roleId;
      if (userRoleId === 3 || userRoleId === 4 || 
          roleValue.toLowerCase() === 'clerk' || roleValue.toLowerCase() === 'client') {
        throw new Error('Access denied. This system is only available to Administrators, Branch Managers, and Accountants.');
      }

      const token = data.Token || data.token;
      const newSessionId = data.SessionId || data.sessionId;
      const sessionExpiresAt = data.SessionExpiresAt || data.sessionExpiresAt;

      // Store token and session information
      setToken(token);
      setSessionId(newSessionId);
      localStorage.setItem('token', token);
      localStorage.setItem('sessionId', newSessionId);
      localStorage.setItem('userId', (data.UserId || data.userId || data.id || '').toString());
      localStorage.setItem('sessionExpiresAt', sessionExpiresAt);

      console.log('Login successful - Session created:', {
        sessionId: newSessionId,
        expiresAt: sessionExpiresAt,
        userId: data.UserId || data.userId || data.id
      });

      // Fetch complete user information including branch details
      try {
        const userResponse = await fetch(buildApiUrl(`Users`), {
          headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json',
            'X-Session-ID': newSessionId,
          },
        });

        if (userResponse.ok) {
          const allUsersResponse = await userResponse.json();
          console.log('Login - All users from API:', allUsersResponse);
          
          // Handle the $values format
          const allUsers = allUsersResponse.$values || allUsersResponse;
          console.log('Login - Users array:', allUsers);
          
          // Find the current user in the list
          const currentUserId = (data.UserId || data.userId || data.id || '').toString();
          console.log('Login - Looking for user ID:', currentUserId);
          console.log('Login - Sample user IDs from array:', allUsers.slice(0, 5).map((u: any) => ({ id: u.Id, idType: typeof u.Id })));
          
          // Debug: Look specifically for user 49
          const user49 = allUsers.find((u: any) => u.id === 49);
          console.log('Login - Direct search for user 49:', user49);
          console.log('Login - User 49 exists:', !!user49);
          
          const userDetails = allUsers.find((u: any) => {
            // Try multiple ID comparison strategies - use lowercase 'id' property
            const apiUserId = u.id; // Changed from u.Id to u.id
            const apiUserIdStr = apiUserId?.toString();
            const currentUserIdNum = parseInt(currentUserId);
            
            // Compare as numbers (most reliable)
            const matchNumber = apiUserId === currentUserIdNum;
            // Compare as strings
            const matchString = apiUserIdStr === currentUserId;
            
            const match = matchNumber || matchString;
            
            if (match) {
              console.log('Login - Found matching user:', u);
              console.log('Login - Match details:', { apiUserId, currentUserId, currentUserIdNum, matchNumber, matchString });
            }
            
            return match;
          });
          console.log('Login - Found user details:', userDetails);
          
          if (userDetails) {
            const userData: User = {
              id: currentUserId,
              username: data.Username || data.username || '',
              role: roleValue,
              roleId: userDetails.role?.id,
              branchId: userDetails.branch?.id,
              canConfirm: userDetails.canConfirm || userDetails.role?.canConfirm || false,
              canDispatch: userDetails.canDispatch || userDetails.role?.canDispatch || false,
              branch: userDetails.branch && userDetails.branch.id ? {
                id: userDetails.branch.id,
                name: userDetails.branch.name
              } : undefined,
              firstName: userDetails.firstName,
              lastName: userDetails.lastName,
              email: userDetails.email
            };

            console.log('Login - Final user data:', userData);
            setUser(userData);
            localStorage.setItem('user', JSON.stringify(userData));
          } else {
            throw new Error('User not found in users list');
          }
        } else {
          throw new Error('Failed to fetch user details');
        }
      } catch (error) {
        // Fallback to basic user data
        const userData: User = {
          id: (data.UserId || data.userId || data.id || '').toString(),
          username: data.Username || data.username || '',
          role: roleValue,
        };
        
        setUser(userData);
        localStorage.setItem('user', JSON.stringify(userData));
      }
    } catch (error) {
      throw error;
    }
  };

  const logout = async () => {
    // Revoke session if we have one
    const currentSessionId = sessionId || localStorage.getItem('sessionId');
    if (currentSessionId) {
      try {
        await fetch(buildApiUrl('auth/logout'), {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'X-Session-ID': currentSessionId,
            'Authorization': `Bearer ${token}`
          },
          body: JSON.stringify({
            sessionId: currentSessionId,
            reason: 'User logout'
          })
        });
      } catch (error) {
        console.warn('Failed to revoke session on server:', error);
      }
    }

    // Clear local state and storage
    setToken(null);
    setUser(null);
    setSessionId(null);
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    localStorage.removeItem('sessionId');
    localStorage.removeItem('userId');
    localStorage.removeItem('sessionExpiresAt');
  };

  // Helper functions for role-based access control
  const isAdmin = (): boolean => {
    if (!user) return false;
    
    // Check by roleId first (most reliable)
    if (user.roleId === 1) return true;
    
    // Check by role name as backup
    const roleName = typeof user.role === 'string' ? user.role : (user.role as any)?.Name || '';
    const roleNames = ['admin', 'administrator'];
    return roleNames.includes(roleName.toLowerCase());
  };

  const isBranchManager = (): boolean => {
    if (!user) return false;
    const roleName = typeof user.role === 'string' ? user.role : (user.role as any)?.Name || '';
    const result = roleName.toLowerCase() === 'manager' || user.roleId === 2;
    console.log('isBranchManager check:', { user, roleName, roleId: user.roleId, result });
    return result;
  };

  const isAccountant = (): boolean => {
    if (!user) return false;
    const roleName = typeof user.role === 'string' ? user.role : (user.role as any)?.Name || '';
    return roleName.toLowerCase() === 'accountant' || user.roleId === 5; // Assuming roleId 5 for Accountant
  };

  const canAccessMenu = (menuPath: string): boolean => {
    if (!user) return false;
    
    // Admin (roleId: 1) can access all menus
    if (isAdmin()) return true;
    
    // Accountant (roleId: 5) can access all menus except User Management
    if (isAccountant()) {
      const restrictedPaths = ['/user-management'];
      return !restrictedPaths.includes(menuPath);
    }
    
    // Branch managers (roleId: 2) can only access specific menus
    if (isBranchManager()) {
      const allowedPaths = [
        '/dashboard',
        '/parcels',
        '/branch-parcels',
        '/dispatches/view',
        '/dispatches/create'
      ];
      return allowedPaths.includes(menuPath);
    }
    
    // Clerk role (roleId: 3) - NO ACCESS to the site
    if (user.roleId === 3 || (typeof user.role === 'string' ? user.role : (user.role as any)?.Name || '').toLowerCase() === 'clerk') {
      return false;
    }
    
    // Client role (roleId: 4) - NO ACCESS to the site
    if (user.roleId === 4 || (typeof user.role === 'string' ? user.role : (user.role as any)?.Name || '').toLowerCase() === 'client') {
      return false;
    }
    
    // Default: deny access for unknown roles (secure fallback)
    return false;
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        token,
        sessionId,
        login,
        logout,
        isAuthenticated: !!token,
        isAdmin,
        isBranchManager,
        isAccountant,
        canAccessMenu,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}; 