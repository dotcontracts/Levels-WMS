import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import { SessionProvider } from './context/SessionContext';
import { ReportsDataProvider } from './context/ReportsDataContext';
import { ParcelsDataProvider } from './context/ParcelsDataContext';
import { useAuth } from './context/AuthContext';
import AppLayout from './layout/AppLayout';
import Login from './pages/Login';
import Dashboard from './pages/Dashboard';
import Parcels from './pages/Parcels';
import BranchParcels from './pages/BranchParcels';
import CreateDispatch from './pages/CreateDispatch';
import ViewDispatch from './pages/ViewDispatch';
import ContractCustomers from './pages/ContractCustomers';
import Invoices from './pages/Invoices';
import CreateInvoice from './pages/CreateInvoice';
import FinancialDashboard from './pages/FinancialDashboard';
import PaymentsManagement from './pages/PaymentsManagement';
import ExpensesManagement from './pages/ExpensesManagement';
import Reports from './pages/Reports';
import SMSMessaging from './pages/SMSMessaging';
import UserManagement from './pages/UserManagement';
import AdminParcelManagement from './pages/AdminParcelManagement';
import NotFound from './pages/NotFound';

// Protected Route component
const ProtectedRoute: React.FC<{ 
  children: React.ReactNode;
  allowedRoles?: string[];
}> = ({ children, allowedRoles }) => {
  const { isAuthenticated, user, canAccessMenu } = useAuth();

  if (!isAuthenticated) {
    return <Navigate to="/login" />;
  }

  // Use the canAccessMenu function instead of manual role checking
  if (allowedRoles && user) {
    const currentPath = window.location.pathname;
    if (!canAccessMenu(currentPath)) {
      return <Navigate to="/dashboard" />;
    }
  }

  return <>{children}</>;
};


const App: React.FC = () => {
  return (
    <AuthProvider>
      <SessionProvider>
        <Router future={{ v7_startTransition: true, v7_relativeSplatPath: true }}>
          <Routes>
          {/* Login page without layout */}
          <Route path="/login" element={<Login />} />
          
          {/* All authenticated pages with modern green layout */}
          <Route
            path="/dashboard"
            element={
              <ProtectedRoute>
                <AppLayout>
                  <Dashboard />
                </AppLayout>
              </ProtectedRoute>
            }
          />
          <Route
            path="/parcels"
            element={
              <ProtectedRoute>
                <AppLayout>
                  <ParcelsDataProvider>
                    <Parcels />
                  </ParcelsDataProvider>
                </AppLayout>
              </ProtectedRoute>
            }
          />
          <Route
            path="/branch-parcels"
            element={
              <ProtectedRoute>
                <AppLayout>
                  <BranchParcels />
                </AppLayout>
              </ProtectedRoute>
            }
          />
          <Route
            path="/dispatches/create"
            element={
              <ProtectedRoute>
                <AppLayout>
                  <CreateDispatch />
                </AppLayout>
              </ProtectedRoute>
            }
          />
          <Route
            path="/dispatches/view"
            element={
              <ProtectedRoute>
                <AppLayout>
                  <ViewDispatch />
                </AppLayout>
              </ProtectedRoute>
            }
          />
          <Route
            path="/contract-customers"
            element={
              <ProtectedRoute>
                <AppLayout>
                  <ContractCustomers />
                </AppLayout>
              </ProtectedRoute>
            }
          />
          <Route
            path="/financial-dashboard"
            element={
              <ProtectedRoute>
                <AppLayout>
                  <FinancialDashboard />
                </AppLayout>
              </ProtectedRoute>
            }
          />
          <Route
            path="/invoices"
            element={
              <ProtectedRoute>
                <AppLayout>
                  <Invoices />
                </AppLayout>
              </ProtectedRoute>
            }
          />
          <Route
            path="/invoices/create"
            element={
              <ProtectedRoute>
                <AppLayout>
                  <CreateInvoice />
                </AppLayout>
              </ProtectedRoute>
            }
          />
          <Route
            path="/payments"
            element={
              <ProtectedRoute>
                <AppLayout>
                  <PaymentsManagement />
                </AppLayout>
              </ProtectedRoute>
            }
          />
          <Route
            path="/expenses"
            element={
              <ProtectedRoute>
                <AppLayout>
                  <ExpensesManagement />
                </AppLayout>
              </ProtectedRoute>
            }
          />
          <Route
            path="/reports"
            element={
              <ProtectedRoute>
                <AppLayout>
                  <ReportsDataProvider>
                    <Reports />
                  </ReportsDataProvider>
                </AppLayout>
              </ProtectedRoute>
            }
          />
          <Route
            path="/sms-messaging"
            element={
              <ProtectedRoute>
                <AppLayout>
                  <SMSMessaging />
                </AppLayout>
              </ProtectedRoute>
            }
          />
          <Route
            path="/user-management"
            element={
              <ProtectedRoute>
                <AppLayout>
                  <UserManagement />
                </AppLayout>
              </ProtectedRoute>
            }
          />
          <Route
            path="/admin/parcels"
            element={
              <ProtectedRoute>
                <AppLayout>
                  <ParcelsDataProvider>
                    <AdminParcelManagement />
                  </ParcelsDataProvider>
                </AppLayout>
              </ProtectedRoute>
            }
          />
          <Route
            path="/settings"
            element={
              <ProtectedRoute>
                <AppLayout>
                  <NotFound />
                </AppLayout>
              </ProtectedRoute>
            }
          />
          <Route path="/" element={<Navigate to="/dashboard" />} />
          <Route path="*" element={<NotFound />} />
          </Routes>
        </Router>
      </SessionProvider>
    </AuthProvider>
  );
};

export default App;
