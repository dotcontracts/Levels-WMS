import React, { useState, useEffect, useCallback, useRef } from 'react';
import { SidebarProvider, useSidebar } from '../context/SidebarContext';
import AppSidebar from './AppSidebar';
import AppHeader from './AppHeader';
import clsx from 'clsx';
import { useAuth } from '../context/AuthContext';
import { wmsApi } from '../services/wmsApi';
import { buildApiUrl } from '../config/api';

interface AppLayoutProps {
  children: React.ReactNode;
}

interface CodLockStatus {
  isLocked: boolean;
  reason: string;
  previousDayCodTotal: number;
  previousDate: string;
  depositRecordId?: number;
  depositStatus?: string;
  depositAmount?: number;
  depositRejectionReason?: string;
  expenseRejectionReason?: string;
}

const POLL_INTERVAL_MS = 60_000;

const CodStatusManager: React.FC<{
  branchName: string;
  onStatusChange: (status: CodLockStatus | null) => void;
}> = ({ branchName, onStatusChange }) => {
  const fetchStatus = useCallback(async () => {
    try {
      const status = await wmsApi.getBranchCodLockStatus(branchName);
      onStatusChange(status as CodLockStatus);
    } catch (err) {
      console.error('[CodLock] Failed to fetch status', err);
      onStatusChange(null);
    }
  }, [branchName, onStatusChange]);

  useEffect(() => {
    fetchStatus();
    const timer = setInterval(fetchStatus, POLL_INTERVAL_MS);
    return () => clearInterval(timer);
  }, [fetchStatus]);

  return null;
};

const CodLockOverlay: React.FC<{ status: CodLockStatus }> = ({ status }) => {
  const rejectionReason = status.depositRejectionReason || status.expenseRejectionReason;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/75 backdrop-blur-sm p-4">
      <div className="bg-white rounded-xl shadow-2xl max-w-md w-full p-6 space-y-4 max-h-[90vh] overflow-y-auto">
        <div className="flex flex-col items-center text-center space-y-2">
          <div className="text-5xl text-red-600">🔒</div>
          <h2 className="text-xl font-bold text-red-600">Account Locked</h2>
          <p className="text-gray-600 text-sm">
            COD balance from{' '}
            <span className="font-semibold">
              {new Date(status.previousDate).toLocaleDateString('en-KE', { day: '2-digit', month: 'short', year: 'numeric' })}
            </span>{' '}
            is unresolved.
          </p>
          <p className="text-gray-800 font-bold text-lg">
            COD Total: Ksh {status.previousDayCodTotal.toLocaleString('en-KE', { minimumFractionDigits: 2 })}
          </p>
        </div>

        {status.reason === 'pending_submission' && (
          <div className="bg-orange-50 border border-orange-400 rounded-lg p-4 text-center space-y-2">
            <p className="text-orange-700 font-semibold text-sm">Declaration Pending</p>
            <p className="text-orange-600 text-sm">
              Please open the WMS Android app to submit your deposit and expense declaration.
            </p>
          </div>
        )}

        {status.reason === 'rejected' && (
          <div className="bg-red-50 border border-red-400 rounded-lg p-4 text-center space-y-2">
            <p className="text-red-700 font-semibold text-sm">Declaration Rejected</p>
            {rejectionReason && (
              <p className="text-red-600 text-sm">{rejectionReason}</p>
            )}
            <p className="text-red-600 text-sm mt-1">
              Please open the WMS Android app to resubmit your declaration.
            </p>
          </div>
        )}
      </div>
    </div>
  );
};

const PendingApprovalToast: React.FC<{
  status: CodLockStatus;
  onDismiss: () => void;
}> = ({ status, onDismiss }) => {
  return (
    <div className="fixed top-4 left-1/2 -translate-x-1/2 z-50 w-full max-w-lg px-4">
      <div className="bg-amber-50 border border-amber-400 rounded-lg shadow-lg px-4 py-3 flex items-start gap-3">
        <span className="text-xl mt-0.5">⏳</span>
        <div className="flex-1 min-w-0">
          <p className="font-semibold text-amber-800 text-sm">COD Declaration Pending Approval</p>
          <p className="text-amber-700 text-xs mt-0.5">
            Your deposit declaration for{' '}
            <span className="font-medium">
              {new Date(status.previousDate).toLocaleDateString('en-KE', { day: '2-digit', month: 'short', year: 'numeric' })}
            </span>{' '}
            (Ksh {status.previousDayCodTotal.toLocaleString('en-KE', { minimumFractionDigits: 2 })}) is awaiting admin approval.
          </p>
        </div>
        <button
          onClick={onDismiss}
          className="text-amber-600 hover:text-amber-900 text-lg leading-none flex-shrink-0 mt-0.5"
          aria-label="Dismiss"
        >
          ✕
        </button>
      </div>
    </div>
  );
};

const AppLayoutContent: React.FC<AppLayoutProps> = ({ children }) => {
  const { isExpanded, isMobileOpen, setIsMobileOpen } = useSidebar();
  const [darkMode, setDarkMode] = useState(false);
  const { user } = useAuth();

  const [codStatus, setCodStatus] = useState<CodLockStatus | null>(null);
  const [toastDismissed, setToastDismissed] = useState(false);
  const [resolvedBranchName, setResolvedBranchName] = useState<string | undefined>(undefined);
  const prevReasonRef = useRef<string | undefined>(undefined);

  useEffect(() => {
    const savedDarkMode = localStorage.getItem('darkMode') === 'true';
    setDarkMode(savedDarkMode);
    if (savedDarkMode) {
      document.documentElement.classList.add('dark');
    }
  }, []);

  useEffect(() => {
    if (!user) return;
    const roleName = typeof user.role === 'string' ? user.role : (user.role as any)?.Name || '';
    const isManager = roleName.toLowerCase() === 'manager' || user.roleId === 2;
    console.log('[CodLock] user effect:', { role: user.role, roleName, isManager, branch: user.branch, branchId: user.branchId });
    if (!isManager) return;

    const branchFromUser = user.branch?.name;
    const branchIdFromUser = user.branchId || (user.branch as any)?.id;
    console.log('[CodLock] branchFromUser:', branchFromUser, 'branchIdFromUser:', branchIdFromUser);

    if (branchFromUser) {
      setResolvedBranchName(branchFromUser);
    } else if (branchIdFromUser) {
      const token = localStorage.getItem('token');
      const sessionId = localStorage.getItem('sessionId');
      console.log('[CodLock] fetching branches for branchId:', branchIdFromUser);
      fetch(buildApiUrl('Branches'), {
        headers: { 'Authorization': `Bearer ${token}`, 'X-Session-ID': sessionId || '', 'Content-Type': 'application/json' },
      })
        .then(r => { console.log('[CodLock] Branches response:', r.status); return r.ok ? r.json() : null; })
        .then(data => {
          console.log('[CodLock] Branches data:', data);
          const branches = data?.$values || (Array.isArray(data) ? data : []);
          const branch = branches.find((b: any) => b.id === branchIdFromUser || b.Id === branchIdFromUser);
          const name = branch?.name || branch?.Name;
          console.log('[CodLock] matched branch:', branch, 'name:', name);
          if (name) setResolvedBranchName(name);
        })
        .catch(err => console.error('[CodLock] Branches fetch error:', err));
    } else {
      console.warn('[CodLock] manager has no branch info, skipping lock check');
    }
  }, [user]);

  const handleStatusChange = useCallback((status: CodLockStatus | null) => {
    setCodStatus(status);
    if (status?.reason === 'pending_approval' && prevReasonRef.current !== 'pending_approval') {
      setToastDismissed(false);
    }
    if (status?.reason === 'rejected') {
      setToastDismissed(false);
    }
    prevReasonRef.current = status?.reason;
  }, []);

  const toggleDarkMode = () => {
    const newDarkMode = !darkMode;
    setDarkMode(newDarkMode);
    localStorage.setItem('darkMode', newDarkMode.toString());
    if (newDarkMode) {
      document.documentElement.classList.add('dark');
    } else {
      document.documentElement.classList.remove('dark');
    }
  };

  const isBlocking = codStatus?.reason === 'pending_submission' || codStatus?.reason === 'rejected';
  const showToast = codStatus?.reason === 'pending_approval' && !toastDismissed;

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      {resolvedBranchName && (
        <CodStatusManager branchName={resolvedBranchName} onStatusChange={handleStatusChange} />
      )}

      {isBlocking && codStatus && <CodLockOverlay status={codStatus} />}

      {showToast && codStatus && (
        <PendingApprovalToast status={codStatus} onDismiss={() => setToastDismissed(true)} />
      )}

      <div className={clsx({ 'blur-sm pointer-events-none select-none': isBlocking })}>
        <AppSidebar />

        {isMobileOpen && (
          <div
            className="fixed inset-0 z-30 bg-gray-900/50 lg:hidden"
            onClick={() => setIsMobileOpen(false)}
          />
        )}

        <div
          className={clsx(
            "transition-all duration-300",
            {
              "lg:ml-64": isExpanded,
              "lg:ml-20": !isExpanded,
              "ml-0": isMobileOpen,
            }
          )}
        >
          <AppHeader darkMode={darkMode} toggleDarkMode={toggleDarkMode} />
          <main className="p-4 lg:p-6">
            {children}
          </main>
        </div>
      </div>
    </div>
  );
};

const AppLayout: React.FC<AppLayoutProps> = ({ children }) => {
  return (
    <SidebarProvider>
      <AppLayoutContent>{children}</AppLayoutContent>
    </SidebarProvider>
  );
};

export default AppLayout;
