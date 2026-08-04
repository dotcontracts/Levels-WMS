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

const AppHeader: React.FC<AppHeaderProps> = ({ darkMode, toggleDarkMode }) => {
  const { toggleUnified } = useSidebar();
  const { user, isAdmin, isAccountant } = useAuth();
  const navigate = useNavigate();
  const [pendingDeposits, setPendingDeposits] = useState<any[]>([]);
  const [showDropdown, setShowDropdown] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!user || (!isAdmin() && !isAccountant())) return;

    const fetchPending = () => {
      wmsApi.getPendingDeposits().then(setPendingDeposits).catch(() => {});
    };

    fetchPending();
    const interval = setInterval(fetchPending, 60_000);
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

  const handleViewAll = () => {
    setShowDropdown(false);
    navigate('/deposits');
  };

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
              {pendingDeposits.length > 0 && (
                <span className="absolute -top-1 -right-1 min-w-[18px] h-[18px] bg-error-500 rounded-full text-white text-xs flex items-center justify-center px-1">
                  {pendingDeposits.length > 99 ? '99+' : pendingDeposits.length}
                </span>
              )}
            </button>

            {showDropdown && (
              <div className="absolute right-0 mt-2 w-80 bg-white dark:bg-gray-800 shadow-lg rounded-lg border border-gray-200 dark:border-gray-700 z-50">
                <div className="p-3 border-b border-gray-200 dark:border-gray-700 text-sm font-semibold text-gray-700 dark:text-gray-200">
                  {pendingDeposits.length > 0
                    ? `Pending Deposits (${pendingDeposits.length})`
                    : 'No pending deposits'}
                </div>
                {pendingDeposits.length > 0 && (
                  <>
                    <ul className="max-h-72 overflow-y-auto divide-y divide-gray-100 dark:divide-gray-700">
                      {pendingDeposits.map(d => (
                        <li key={d.id} className="p-3 hover:bg-gray-50 dark:hover:bg-gray-700 text-sm">
                          <div className="font-medium text-gray-800 dark:text-gray-100">{d.branch}</div>
                          <div className="text-gray-500 dark:text-gray-400">
                            {new Date(d.date).toLocaleDateString()} &middot; KES {d.depositAmount?.toLocaleString()}
                          </div>
                          {d.createdByName && (
                            <div className="text-gray-400 dark:text-gray-500 text-xs mt-0.5">by {d.createdByName}</div>
                          )}
                        </li>
                      ))}
                    </ul>
                    <button
                      onClick={handleViewAll}
                      className="block w-full p-3 text-center text-sm text-brand-600 dark:text-brand-400 hover:bg-gray-50 dark:hover:bg-gray-700 border-t border-gray-200 dark:border-gray-700 rounded-b-lg"
                    >
                      View all deposits →
                    </button>
                  </>
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
