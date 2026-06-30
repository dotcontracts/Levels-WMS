import React, { createContext, useContext, useEffect, ReactNode } from 'react';
import { useSession } from '../hooks/useSession';
import { useAuth } from './AuthContext';

interface SessionContextType {
  isSessionValid: boolean;
  sessionInfo: {
    sessionId: string;
    userId: number;
    username: string;
    expiresAt: string;
    remainingMinutes: number;
  } | null;
  validateSession: () => Promise<boolean>;
  updateActivity: () => Promise<boolean>;
  createSession: (sessionData: any) => Promise<void>;
  revokeSession: (reason?: string) => Promise<void>;
  clearSession: () => void;
  getSessionStatistics: () => Promise<any>;
  hasActiveSession: () => boolean;
}

const SessionContext = createContext<SessionContextType | undefined>(undefined);

interface SessionProviderProps {
  children: ReactNode;
}

export const SessionProvider: React.FC<SessionProviderProps> = ({ children }) => {
  const sessionHook = useSession();
  const { isAuthenticated, logout } = useAuth();

  // Handle session expiry events
  useEffect(() => {
    const handleSessionExpired = () => {
      console.warn('Session expired - logging out user');
      logout();
    };

    // Listen for session expiry events from the session hook
    if (!sessionHook.isSessionValid && isAuthenticated && sessionHook.sessionInfo) {
      handleSessionExpired();
    }
  }, [sessionHook.isSessionValid, isAuthenticated, sessionHook.sessionInfo, logout]);

  // Auto-update session activity on user interaction
  useEffect(() => {
    if (!isAuthenticated || !sessionHook.hasActiveSession()) return;

    const updateActivityOnInteraction = () => {
      sessionHook.updateActivity();
    };

    const events = [
      'mousedown',
      'mousemove', 
      'keypress',
      'scroll',
      'touchstart',
      'click'
    ];

    // Throttle activity updates to avoid excessive API calls
    let lastUpdate = 0;
    const throttleDelay = 30000; // 30 seconds

    const throttledUpdate = () => {
      const now = Date.now();
      if (now - lastUpdate > throttleDelay) {
        lastUpdate = now;
        updateActivityOnInteraction();
      }
    };

    events.forEach(event => {
      document.addEventListener(event, throttledUpdate, true);
    });

    return () => {
      events.forEach(event => {
        document.removeEventListener(event, throttledUpdate, true);
      });
    };
  }, [isAuthenticated, sessionHook]);

  return (
    <SessionContext.Provider value={sessionHook}>
      {children}
    </SessionContext.Provider>
  );
};

export const useSessionContext = (): SessionContextType => {
  const context = useContext(SessionContext);
  if (context === undefined) {
    throw new Error('useSessionContext must be used within a SessionProvider');
  }
  return context;
};