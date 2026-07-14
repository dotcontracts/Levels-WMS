import { useState, useEffect, useCallback } from 'react';
import { useAuth } from '../context/AuthContext';
import { buildApiUrl } from '../config/api';

interface SessionInfo {
  sessionId: string;
  userId: number;
  username: string;
  expiresAt: string;
  remainingMinutes: number;
}

interface SessionValidationResponse {
  isValid: boolean;
  session?: SessionInfo;
  errorMessage?: string;
  requiresReauthentication: boolean;
}

export const useSession = () => {
  const [isSessionValid, setIsSessionValid] = useState<boolean>(true);
  const [sessionInfo, setSessionInfo] = useState<SessionInfo | null>(null);
  const { isAuthenticated, logout } = useAuth();

  const getSessionId = useCallback((): string | null => {
    return localStorage.getItem('sessionId');
  }, []);

  const getUserId = useCallback((): number | null => {
    const userId = localStorage.getItem('userId');
    return userId ? parseInt(userId) : null;
  }, []);

  const validateSession = useCallback(async (): Promise<boolean> => {
    if (!isAuthenticated) return false;

    const sessionId = getSessionId();
    const userId = getUserId();

    if (!sessionId || !userId) {
      console.warn('No session information found');
      setIsSessionValid(false);
      return false;
    }

    try {
      const response = await fetch(buildApiUrl('session/validate'), {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Session-ID': sessionId,
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        },
        body: JSON.stringify({
          sessionId,
          userId
        })
      });

      if (response.ok) {
        const result: SessionValidationResponse = await response.json();

        if (result.isValid && result.session) {
          setSessionInfo(result.session);
          setIsSessionValid(true);
          return true;
        } else {
          console.warn('Session validation failed:', result.errorMessage);
          setIsSessionValid(false);

          if (result.requiresReauthentication) {
            logout();
          }
          return false;
        }
      } else if (response.status === 401) {
        console.warn('Session expired - redirecting to login');
        setIsSessionValid(false);
        logout();
        return false;
      } else {
        console.error('Session validation failed:', response.statusText);
        setIsSessionValid(false);
        return false;
      }
    } catch (error) {
      console.error('Error validating session:', error);
      setIsSessionValid(false);
      return false;
    }
  }, [isAuthenticated, getSessionId, getUserId, logout]);

  const updateActivity = useCallback(async (): Promise<boolean> => {
    const sessionId = getSessionId();
    if (!sessionId || !isAuthenticated) return false;

    try {
      const response = await fetch(buildApiUrl('session/activity'), {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Session-ID': sessionId,
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        },
        body: JSON.stringify({
          sessionId
        })
      });

      if (response.ok) {
        console.debug('Session activity updated successfully');
        return true;
      } else if (response.status === 401) {
        console.warn('Session expired during activity update');
        setIsSessionValid(false);
        logout();
        return false;
      } else {
        console.warn('Failed to update session activity:', response.statusText);
        return false;
      }
    } catch (error) {
      console.error('Error updating session activity:', error);
      return false;
    }
  }, [getSessionId, isAuthenticated, logout]);

  const createSession = useCallback(async (sessionData: SessionInfo): Promise<void> => {
    try {
      // Store session information
      localStorage.setItem('sessionId', sessionData.sessionId);
      localStorage.setItem('userId', sessionData.userId.toString());
      localStorage.setItem('username', sessionData.username);
      localStorage.setItem('sessionExpiresAt', sessionData.expiresAt);

      setSessionInfo(sessionData);
      setIsSessionValid(true);

      console.log('Session created successfully:', sessionData.sessionId);
    } catch (error) {
      console.error('Error creating session:', error);
      throw error;
    }
  }, []);

  const revokeSession = useCallback(async (reason?: string): Promise<void> => {
    const sessionId = getSessionId();
    if (!sessionId) return;

    try {
      const response = await fetch(buildApiUrl('auth/logout'), {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Session-ID': sessionId,
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        },
        body: JSON.stringify({
          sessionId,
          reason: reason || 'User logout'
        })
      });

      if (!response.ok) {
        console.warn('Logout request failed:', response.statusText);
      }
    } catch (error) {
      console.error('Error during logout:', error);
    } finally {
      // Clear session data regardless of API call result
      clearSession();
    }
  }, [getSessionId]);

  const clearSession = useCallback((): void => {
    localStorage.removeItem('sessionId');
    localStorage.removeItem('userId');
    localStorage.removeItem('username');
    localStorage.removeItem('sessionExpiresAt');

    setSessionInfo(null);
    setIsSessionValid(false);

    console.log('Session data cleared');
  }, []);

  const getSessionStatistics = useCallback(async () => {
    try {
      const response = await fetch(buildApiUrl('session/statistics'), {
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`,
          'X-Session-ID': getSessionId() || ''
        }
      });

      if (response.ok) {
        return await response.json();
      }
      return null;
    } catch (error) {
      console.error('Error fetching session statistics:', error);
      return null;
    }
  }, [getSessionId]);

  // Validate session on mount and when authentication changes
  useEffect(() => {
    if (isAuthenticated && getSessionId()) {
      validateSession();
    }
  }, [isAuthenticated, validateSession, getSessionId]);

  // Set up periodic session validation (every 5 minutes)
  useEffect(() => {
    if (!isAuthenticated || !isSessionValid) return;

    const interval = setInterval(() => {
      validateSession();
    }, 5 * 60 * 1000); // 5 minutes

    return () => clearInterval(interval);
  }, [isAuthenticated, isSessionValid, validateSession]);

  return {
    isSessionValid,
    sessionInfo,
    validateSession,
    updateActivity,
    createSession,
    revokeSession,
    clearSession,
    getSessionStatistics,
    hasActiveSession: () => !!getSessionId() && isSessionValid
  };
};