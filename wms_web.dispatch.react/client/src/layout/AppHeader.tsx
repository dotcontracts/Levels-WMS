import React, { useState, useEffect, useRef } from 'react';
import { Menu, Bell, Search, Sun, Moon } from 'lucide-react';
import { useSidebar } from '../context/SidebarContext';
import { useAuth } from '../context/AuthContext';
import { wmsApi } from '../services/wmsApi';
import { useNavigate } from 'react-router-dom';
import clsx from 'clsx';


interface AppHeaderProps {
  darkMode?: boolean;
  toggleDarkMode?: () => void;
}

interface Notification {
  id: string | number;
  type: 'branch_deposit' | 'clerk_deposit' | 'clerk_expense';
  label: string;
  sublabel: string;
  meta?: string;
  href: string;
}

const AppHeader: React.FC<AppHeaderProps> = ({ darkMode, toggleDarkMode }) => {
  const { toggleUnified } = useSidebar();
  const { user, isAdmin, isAccountant } = useAuth();
  const navigate = useNavigate();
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [showDropdown, setShowDropdown] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!user || (!isAdmin() && !isAccountant())) return;

    const fetchNotifications = async () => {
      const [branchDeposits, clerkDeposits, clerkExpenses] = await Promise.all([
        wmsApi.getPendingDeposits(),
        wmsApi.getPendingClerkDeposits(),
        wmsApi.getPendingClerkExpenses(),
      ]);

      const mapped: Notification[] = [
        ...branchDeposits.map((d: any) => ({
          id: `branch-${d.id}`,
          type: 'branch_deposit' as const,
          label: d.branch,
          sublabel: `${new Date(d.date).toLocaleDateString()} · KES ${d.depositAmount?.toLocaleString()}`,
          meta: d.createdByName ? `by ${d.createdByName}` : undefined,
          href: '/reports/cod-delivered',
        })),
        ...clerkDeposits.map((e: any) => ({
          id: `clerk-dep-${e.id}`,
          type: 'clerk_deposit' as const,
          label: e.clerkName || `Clerk #${e.clerkId}`,
          sublabel: `${new Date(e.date).toLocaleDateString()} · KES ${e.amount?.toLocaleString()}`,
          meta: 'Deposit declaration',
          href: `/reports/sales-per-clerk?tab=cash-in&date=${e.date?.split('T')[0]}&clerkId=${e.clerkId}&pendingClerk=${e.clerkId}`,
        })),
        ...clerkExpenses.map((e: any) => ({
          id: `clerk-exp-${e.id}`,
          type: 'clerk_expense' as const,
          label: e.clerkName || `Clerk #${e.clerkId}`,
          sublabel: `${new Date(e.date).toLocaleDateString()} · KES ${e.amount?.toLocaleString()}`,
          meta: e.category?.replace(/_/g, ' '),
          href: `/expenses?status=pending&clerkName=${encodeURIComponent(e.clerkName || '')}`,
        })),
      ];

      setNotifications(mapped);
    };

    fetchNotifications();
    const interval = setInterval(fetchNotifications, 60_000);
    return () => clearInterval(interval);
  }, [user]);

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node)) {
        setShowDropdown(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleNotificationClick = (href: string) => {
    setShowDropdown(false);
    navigate(href);
  };

  const branchCount = notifications.filter(n => n.type === 'branch_deposit').length;
  const clerkDepositCount = notifications.filter(n => n.type === 'clerk_deposit').length;
  const clerkExpenseCount = notifications.filter(n => n.type === 'clerk_expense').length;

  return (
    <header className="bg-white dark:bg-gray-900 border-b border-gray-200 dark:border-gray-800 sticky top-0 z-30">
      <div className="flex items-center justify-between px-4 py-3">
        {/* Left side - Menu and Search */}
        <div className="flex items-center space-x-4">
          {/* Unified menu toggle - works on all devices */}
          <button
            onClick={toggleUnified}
            className="p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors"
            aria-label="Toggle menu"
          >
            <Menu className="w-5 h-5 text-gray-600 dark:text-gray-400" />
          </button>
          

          {/* Search bar */}
          <div className="hidden md:flex items-center relative">
            <Search className="w-4 h-4 text-gray-400 absolute left-3" />
            <input
              type="text"
              placeholder="Search..."
              className="pl-10 pr-4 py-2 w-80 bg-gray-50 dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent text-sm"
            />
          </div>
        </div>

        {/* Right side - Notifications, Theme toggle, User menu */}
        <div className="flex items-center space-x-3">
          {/* Mobile search button */}
          <button className="md:hidden p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors">
            <Search className="w-5 h-5 text-gray-600 dark:text-gray-400" />
          </button>

          {/* Notifications */}
          <div className="relative" ref={dropdownRef}>
            <button
              onClick={() => setShowDropdown(v => !v)}
              className="relative p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors"
            >
              <Bell className="w-5 h-5 text-gray-600 dark:text-gray-400" />
              {notifications.length > 0 && (
                <span className="absolute -top-1 -right-1 min-w-[18px] h-[18px] bg-error-500 rounded-full text-white text-xs flex items-center justify-center px-1">
                  {notifications.length > 99 ? '99+' : notifications.length}
                </span>
              )}
            </button>

            {showDropdown && (
              <div className="absolute right-0 mt-2 w-80 bg-white dark:bg-gray-800 shadow-lg rounded-lg border border-gray-200 dark:border-gray-700 z-50">
                <div className="p-3 border-b border-gray-200 dark:border-gray-700 text-sm font-semibold text-gray-700 dark:text-gray-200">
                  {notifications.length > 0
                    ? `Pending Approvals (${notifications.length})`
                    : 'No pending approvals'}
                </div>

                {notifications.length > 0 && (
                  <ul className="max-h-80 overflow-y-auto divide-y divide-gray-100 dark:divide-gray-700">
                    {branchCount > 0 && (
                      <li className="px-3 py-1.5 bg-gray-50 dark:bg-gray-700/50 text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
                        Branch Deposits ({branchCount})
                      </li>
                    )}
                    {notifications.filter(n => n.type === 'branch_deposit').map(n => (
                      <li key={n.id}>
                        <button onClick={() => handleNotificationClick(n.href)}
                          className="w-full text-left p-3 hover:bg-gray-50 dark:hover:bg-gray-700 text-sm">
                          <div className="font-medium text-gray-800 dark:text-gray-100">{n.label}</div>
                          <div className="text-gray-500 dark:text-gray-400">{n.sublabel}</div>
                          {n.meta && <div className="text-gray-400 dark:text-gray-500 text-xs mt-0.5">{n.meta}</div>}
                        </button>
                      </li>
                    ))}

                    {clerkDepositCount > 0 && (
                      <li className="px-3 py-1.5 bg-gray-50 dark:bg-gray-700/50 text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
                        Clerk Deposits ({clerkDepositCount})
                      </li>
                    )}
                    {notifications.filter(n => n.type === 'clerk_deposit').map(n => (
                      <li key={n.id}>
                        <button onClick={() => handleNotificationClick(n.href)}
                          className="w-full text-left p-3 hover:bg-gray-50 dark:hover:bg-gray-700 text-sm">
                          <div className="font-medium text-gray-800 dark:text-gray-100">{n.label}</div>
                          <div className="text-gray-500 dark:text-gray-400">{n.sublabel}</div>
                          {n.meta && <div className="text-gray-400 dark:text-gray-500 text-xs mt-0.5">{n.meta}</div>}
                        </button>
                      </li>
                    ))}

                    {clerkExpenseCount > 0 && (
                      <li className="px-3 py-1.5 bg-gray-50 dark:bg-gray-700/50 text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
                        Clerk Expenses ({clerkExpenseCount})
                      </li>
                    )}
                    {notifications.filter(n => n.type === 'clerk_expense').map(n => (
                      <li key={n.id}>
                        <button onClick={() => handleNotificationClick(n.href)}
                          className="w-full text-left p-3 hover:bg-gray-50 dark:hover:bg-gray-700 text-sm">
                          <div className="font-medium text-gray-800 dark:text-gray-100">{n.label}</div>
                          <div className="text-gray-500 dark:text-gray-400">{n.sublabel}</div>
                          {n.meta && <div className="text-gray-400 dark:text-gray-500 text-xs mt-0.5 capitalize">{n.meta}</div>}
                        </button>
                      </li>
                    ))}
                  </ul>
                )}

              </div>
            )}
          </div>

          {/* Dark mode toggle */}
          {toggleDarkMode && (
            <button
              onClick={toggleDarkMode}
              className="p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors"
            >
              {darkMode ? (
                <Sun className="w-5 h-5 text-gray-600 dark:text-gray-400" />
              ) : (
                <Moon className="w-5 h-5 text-gray-600 dark:text-gray-400" />
              )}
            </button>
          )}

          {/* User avatar and name */}
          {user && (
            <div className="flex items-center space-x-3 pl-3 border-l border-gray-200 dark:border-gray-700">
              <div className="hidden sm:flex flex-col items-end">
                <span className="text-sm font-medium text-gray-900 dark:text-white">
                  {user.username}
                </span>
                <span className="text-xs text-gray-500 dark:text-gray-400">
                  {typeof user.role === 'string' ? user.role : (user.role as any)?.name || 'User'}
                </span>
              </div>
              <div className="w-8 h-8 bg-brand-100 rounded-full flex items-center justify-center">
                <span className="text-brand-600 font-semibold text-sm">
                  {user.username.charAt(0).toUpperCase()}
                </span>
              </div>
            </div>
          )}
        </div>
      </div>
    </header>
  );
};

export default AppHeader;
