import React, { useState, useEffect, useCallback, useMemo } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { useReportsData } from '../context/ReportsDataContext';
import { wmsApi, Parcel, Dispatch, ParcelDepositDto, DailyExpense } from '../services/wmsApi';
import { DataLoadingState, SkeletonTable, SkeletonCard, ProgressBar } from '../components/SkeletonLoader';
import {
  BarChart,
  LineChart,
  TrendingUp,
  TrendingDown,
  Users,
  User,
  Package,
  FileText,
  Download,
  Filter,
  Calendar,
  DollarSign,
  Truck,
  CheckCircle,
  XCircle,
  AlertCircle,
  Printer,
  RefreshCw,
  Search,
  Eye,
  X,
  ChevronRight,
  MapPin,
  Clock,
  Edit
} from 'lucide-react';
import Chart from 'react-apexcharts';
import { Card, Button, Badge, Table, FilterPanel, FilterField } from '../components/ui';
import { VirtualScrollTable } from '../components/VirtualScrollTable';
import { fmtDate, fmtTime, fmtDateTime } from '../utils/dateUtils';

interface ReportFilters {
  startDate: string;
  endDate: string;
  branch: string;
  destination: string;
  clerk: string;
}

interface SalesPerClerkData {
  clerkId: number;
  clerkName: string;
  clerkUsername: string;
  totalParcels: number;
  totalSales: number;
  averagePerParcel: number;
  performance: 'excellent' | 'good' | 'average' | 'below-average';
}

interface DeliveryRateData {
  destination: string;
  totalParcels: number;
  deliveredParcels: number;
  pendingParcels: number;
  inTransitParcels: number;
  deliveryRate: number;
}

interface UndeliveredParcelData {
  dispatchCode: string;
  destination: string;
  sourceBranch: string;
  driver: string;
  vehicleNumber: string;
  totalParcels: number;
  undeliveredParcels: number;
  totalAmount: number;
  dispatchDate: string;
}

interface BranchDebitData {
  id?: number;
  branch: string;
  date: string;
  codTotal: number;
  depositedAmount: number;
  debt: number;
  createdAt?: string;
  updatedAt?: string;
  createdById?: number;
  createdByName?: string;
  status?: string;
  rejectionReason?: string;
}

interface BranchDebitSummary {
  branch: string;
  totalCodAmount: number;
  totalDeposited: number;
  totalDebt: number;
  totalExpenses: number;
  transactions: BranchDebitData[];
}

interface CreateBranchDepositDto {
  branch: string;
  date: string;
  codTotal: number;
  depositAmount: number;
}


interface ClerkCashInData {
  clerkId: number;
  clerkName: string;
  clerkUsername: string;
  totalPaidAmount: number;
  totalDeposited: number;
  totalExpenses: number;
  remainingDebt: number;
  parcels: ClerkParcelData[];
  clerkDeposits: DailyExpense[];
}

interface ClerkParcelData {
  id: string;
  waybillNumber: string;
  sender: string;
  receiver: string;
  destination: string;
  amount: number;
  depositedAmount: number;
  expenses: number;
  transactionCode?: string;
  createdAt: string;
}

interface ClerkDebitData {
  clerkId: number;
  clerkName: string;
  clerkUsername: string;
  totalPaidAmount: number;
  totalDeposited: number;
  totalExpenses: number;
  currentDebit: number;
  parcelCount: number;
  dateRange: {
    from: string;
    to: string;
  };
  transactions: ClerkDebitTransaction[];
  approvedExpenseItems: DailyExpense[];
}

interface ClerkDebitTransaction {
  date: string;
  parcelId: string;
  waybillNumber: string;
  paidAmount: number;
  depositedAmount: number;
  expenses: number;
  netDebit: number;
  destination: string;
  sender: string;
  receiver: string;
}

interface DailySalesData {
  date: string;
  totalSales: number;
  parcelCount: number;
  averageValue: number;
}

interface UserDailySales {
  userId: number;
  username: string;
  firstName?: string;
  lastName?: string;
  role: string;
  dailySales: DailySalesData[];
}

type ReportType =
  | 'dashboard'
  | 'sales-per-clerk'
  | 'contract-invoices'
  | 'undelivered-parcels'
  | 'cod-delivered'
  | 'delivery-rate'
  | 'parcel-list'
  | 'clerk-debit'
  | 'branch-cod-report';

const Reports: React.FC = () => {
  const { 
    cachedData, 
    loading: cacheLoading, 
    error: cacheError, 
    getCachedData,
    getStreamingData, 
    invalidateCache,
    isDataFresh,
    getDataAge,
    getLoadProgress
  } = useReportsData();
  
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [parcels, setParcels] = useState<Parcel[]>([]);
  const [dispatches, setDispatches] = useState<Dispatch[]>([]);
  const [reportData, setReportData] = useState<any>(null);
  
  // Tab-specific data caching
  const [tabDataCache, setTabDataCache] = useState<{ [key: string]: { data: any; timestamp: number; filters: any } }>({});
  const [tabLoadingStates, setTabLoadingStates] = useState<{ [key: string]: boolean }>({});
  const [showFilters, setShowFilters] = useState(false);
  const [filteredParcels, setFilteredParcels] = useState<Parcel[]>([]);
  const [parcelListLoading, setParcelListLoading] = useState(false);
  const [showParcelFilters, setShowParcelFilters] = useState(false);
  const [destinations, setDestinations] = useState<string[]>([]);
  const [clerks, setClerks] = useState<{ id: number, username: string }[]>([]);
  const [userCache, setUserCache] = useState<Map<number, string>>(new Map());
  const [showDepositModal, setShowDepositModal] = useState(false);
  const [selectedBranchDebit, setSelectedBranchDebit] = useState<BranchDebitData | null>(null);
  const [depositAmount, setDepositAmount] = useState('');
  const [branchDebitData, setBranchDebitData] = useState<{ [key: string]: BranchDebitSummary }>({});

  // Cash-in related state
  const [salesPerClerkTab, setSalesPerClerkTab] = useState<'performance' | 'cash-in' | 'daily-sales'>('performance');
  const [clerkCashInData, setClerkCashInData] = useState<ClerkCashInData[]>([]);
  const [selectedClerk, setSelectedClerk] = useState<ClerkCashInData | null>(null);
  const [showClerkDetailsModal, setShowClerkDetailsModal] = useState(false);
  const [selectedClerkParcel, setSelectedClerkParcel] = useState<ClerkParcelData | null>(null);
  const [showParcelModal, setShowParcelModal] = useState(false);
  const [parcelDepositAmount, setParcelDepositAmount] = useState('');
  const [updatingParcel, setUpdatingParcel] = useState(false);
  const [cashInDate, setCashInDate] = useState(new Date().toISOString().split('T')[0]); // Single date filter for cash-in
  const [activeBranchTab, setActiveBranchTab] = useState<string>('');
  const [loadingDeposits, setLoadingDeposits] = useState(false);
  const [savingDeposit, setSavingDeposit] = useState(false);
  const [approvingDepositId, setApprovingDepositId] = useState<number | null>(null);
  const [showRejectModal, setShowRejectModal] = useState(false);
  const [rejectDepositId, setRejectDepositId] = useState<number | null>(null);
  const [rejectReason, setRejectReason] = useState('');

  const [approvedExpenses, setApprovedExpenses] = useState<DailyExpense[]>([]);

  // Clerk Debit related state
  const [clerkDebitData, setClerkDebitData] = useState<ClerkDebitData[]>([]);
  const [debitDateRange, setDebitDateRange] = useState({
    from: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString().split('T')[0], // Last 7 days
    to: new Date().toISOString().split('T')[0]
  });
  const [selectedDebitClerk, setSelectedDebitClerk] = useState<ClerkDebitData | null>(null);
  const [showDebitDetailsModal, setShowDebitDetailsModal] = useState(false);

  const [undeliveredPage, setUndeliveredPage] = useState(1);
  const UNDELIVERED_PAGE_SIZE = 15;

  // Daily Sales related state
  const [usersList, setUsersList] = useState<UserDailySales[]>([]);
  const [selectedUser, setSelectedUser] = useState<UserDailySales | null>(null);
  const [dailySalesDateRange, setDailySalesDateRange] = useState({
    from: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0], // Last 30 days
    to: new Date().toISOString().split('T')[0]
  });
  const [loadingDailySales, setLoadingDailySales] = useState(false);

  const [filters, setFilters] = useState<ReportFilters>({
    startDate: '',
    endDate: '',
    branch: '',
    destination: '',
    clerk: ''
  });

  const [parcelFilters, setParcelFilters] = useState({
    dateFrom: '',
    dateTo: '',
    destination: '',
    status: 'all',
    paymentMethod: ''
  });

  const { user, isAccountant, isBranchManager } = useAuth();
  const { reportType: reportTypeParam } = useParams<{ reportType: string }>();
  const navigate = useNavigate();
  const currentReport: ReportType = (reportTypeParam as ReportType) || (isBranchManager() ? 'branch-cod-report' : 'dashboard');
  const setCurrentReport = (report: ReportType) => navigate(`/reports/${report}`);

  const [managerDeposits, setManagerDeposits] = useState<any[]>([]);
  const [managerCodLoading, setManagerCodLoading] = useState(false);
  const [managerCodMonth, setManagerCodMonth] = useState(() => {
    const now = new Date();
    return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;
  });
  const [managerDepositInput, setManagerDepositInput] = useState('');
  const [managerSubmitting, setManagerSubmitting] = useState(false);
  const [managerSubmitError, setManagerSubmitError] = useState('');
  const [managerSubmitSuccess, setManagerSubmitSuccess] = useState('');

  useEffect(() => {
    if (isBranchManager() && !reportTypeParam) {
      navigate('/reports/branch-cod-report', { replace: true });
    } else if (!isBranchManager() && !reportTypeParam) {
      navigate('/reports/dashboard', { replace: true });
    }
  }, [user, reportTypeParam]);

  // Format currency helper
  const formatCurrency = useCallback((amount: number) => {
    return new Intl.NumberFormat('en-KE', {
      style: 'currency',
      currency: 'KES',
      minimumFractionDigits: 0
    }).format(amount).replace('KES', 'KSh');
  }, []);

  // Enhanced parcels with cached user data and client-side filtering for better performance
  const enhancedFilteredParcels = useMemo(() => {
    let enhanced = filteredParcels.map(parcel => ({
      ...parcel,
      // Use cached user display name if available
      clerkDisplayName: userCache.get(parcel.createdById) ||
        parcel.createdBy?.username ||
        `User ${parcel.createdById}`,
      // Pre-format currency for better performance
      formattedAmount: formatCurrency(parcel.totalAmount)
    }));

    // Apply client-side filters for better responsiveness
    if (parcelFilters.paymentMethod) {
      enhanced = enhanced.filter(parcel =>
        parcel.paymentMethods?.toLowerCase().includes(parcelFilters.paymentMethod.toLowerCase())
      );
    }

    return enhanced;
  }, [filteredParcels, userCache, formatCurrency, parcelFilters.paymentMethod]);

  // Chart configurations
  const salesPerClerkChartOptions = {
    chart: {
      type: 'bar' as const,
      height: 350,
      toolbar: {
        show: true,
        tools: {
          download: true,
          selection: true,
          zoom: true,
          zoomin: true,
          zoomout: true,
          pan: true,
          reset: true
        }
      }
    },
    plotOptions: {
      bar: {
        horizontal: false,
        columnWidth: '55%',
        endingShape: 'rounded'
      }
    },
    dataLabels: {
      enabled: false
    },
    stroke: {
      show: true,
      width: 2,
      colors: ['transparent']
    },
    xaxis: {
      categories: [] as string[],
      title: {
        text: 'Clerks'
      },
      labels: {
        rotate: -45,
        rotateAlways: true,
        style: {
          fontSize: '12px'
        }
      }
    },
    yaxis: {
      title: {
        text: 'Sales Amount (KSh)'
      }
    },
    fill: {
      opacity: 1
    },
    tooltip: {
      y: {
        formatter: function (val: number) {
          return "KSh " + val.toLocaleString()
        }
      }
    },
    colors: ['#10B981', '#3B82F6', '#8B5CF6', '#F59E0B', '#EF4444']
  };

  const deliveryRateChartOptions = {
    chart: {
      type: 'donut' as const,
      height: 350
    },
    labels: [] as string[],
    colors: ['#10B981', '#F59E0B', '#EF4444', '#6B7280'],
    plotOptions: {
      pie: {
        donut: {
          size: '70%',
          labels: {
            show: true,
            name: {
              show: true,
              fontSize: '16px',
              fontWeight: 600,
              color: '#374151'
            },
            value: {
              show: true,
              fontSize: '14px',
              fontWeight: 400,
              color: '#6B7280',
              formatter: function (val: string) {
                return val + '%'
              }
            },
            total: {
              show: true,
              showAlways: false,
              label: 'Total',
              fontSize: '16px',
              fontWeight: 600,
              color: '#374151',
              formatter: function (w: any) {
                return w.globals.seriesTotals.reduce((a: number, b: number) => a + b, 0)
              }
            }
          }
        }
      }
    },
    legend: {
      position: 'bottom' as const,
      horizontalAlign: 'center' as const
    },
    responsive: [{
      breakpoint: 480,
      options: {
        chart: {
          width: 200
        },
        legend: {
          position: 'bottom'
        }
      }
    }]
  };

  const monthlyTrendsChartOptions = {
    chart: {
      type: 'line' as const,
      height: 350,
      toolbar: {
        show: true
      }
    },
    stroke: {
      curve: 'smooth' as const,
      width: 3
    },
    xaxis: {
      categories: [] as string[],
      title: {
        text: 'Month'
      }
    },
    yaxis: {
      title: {
        text: 'Number of Parcels'
      }
    },
    colors: ['#10B981', '#3B82F6', '#8B5CF6'],
    markers: {
      size: 6,
      hover: {
        size: 8
      }
    },
    tooltip: {
      shared: true,
      intersect: false
    }
  };

  // Fetch clerk cash-in data - optimized version
  const fetchClerkCashInData = useCallback(async (filterDate?: string): Promise<ClerkCashInData[]> => {
    try {
      // Use direct paginated API call for better performance
      const response = await wmsApi.getParcelsPaged({
        page: 1,
        pageSize: 300, // Smaller chunks for cash-in calculations
        ...(filterDate && { fromDate: filterDate, toDate: filterDate })
      });
      
      let allParcels = response.data;
      
      // Load additional pages if needed (limit to reasonable amount)
      if (response.hasNextPage && response.totalPages <= 5) {
        const remainingPages = [];
        for (let page = 2; page <= response.totalPages; page++) {
          remainingPages.push(
            wmsApi.getParcelsPaged({
              page,
              pageSize: 300,
              ...(filterDate && { fromDate: filterDate, toDate: filterDate })
            })
          );
        }
        
        const remainingResponses = await Promise.all(remainingPages);
        remainingResponses.forEach(resp => {
          allParcels = allParcels.concat(resp.data);
        });
      }

      // Filter for paid parcels and by date if specified
      let paidParcels = allParcels.filter(parcel =>
        parcel.paymentMethods?.toLowerCase().includes('paid')
      );

      if (filterDate) {
        paidParcels = paidParcels.filter(parcel => {
          if (parcel.createdAt) {
            try {
              const parcelDate = new Date(parcel.createdAt);
              return parcelDate.toISOString().split('T')[0] === filterDate;
            } catch (error) {
              return false;
            }
          }
          return false;
        });
      }

      // Get existing deposits from database (if ParcelDeposits API is available)
      let existingDeposits: ParcelDepositDto[] = [];
      try {
        existingDeposits = await wmsApi.getParcelDeposits(filterDate);
        console.log('Loaded existing deposits from database:', existingDeposits.length);
      } catch (apiError) {
        console.log('ParcelDeposits API not available, will use localStorage fallback');
      }

      // Create a map of existing deposits by parcelId
      const depositsMap: Record<string, ParcelDepositDto> = {};
      existingDeposits.forEach(deposit => {
        depositsMap[deposit.parcelId] = deposit;
      });

      // Get users to populate clerk information
      const users = await wmsApi.getUsers();

      // Group parcels by clerk
      const clerkData: Record<number, ClerkCashInData> = {};

      paidParcels.forEach(parcel => {
        const clerkId = parcel.createdById;
        if (!clerkId) return;

        const clerk = users.find(u => u.id === clerkId);
        if (!clerk) return;

        if (!clerkData[clerkId]) {
          clerkData[clerkId] = {
            clerkId: clerkId,
            clerkName: clerk.firstName && clerk.lastName
              ? `${clerk.firstName} ${clerk.lastName}`
              : clerk.username,
            clerkUsername: clerk.username,
            totalPaidAmount: 0,
            totalDeposited: 0,
            totalExpenses: 0,
            remainingDebt: 0,
            parcels: [],
            clerkDeposits: []
          };
        }

        // Check for deposit data from database only
        let depositedAmount = 0;

        const dbDeposit = depositsMap[parcel.id];
        if (dbDeposit) {
          depositedAmount = dbDeposit.depositedAmount;
        }

        // Add parcel to clerk's data
        const clerkParcelData: ClerkParcelData = {
          id: parcel.id,
          waybillNumber: parcel.waybillNumber,
          sender: parcel.sender,
          receiver: parcel.receiver,
          destination: parcel.destination,
          amount: parcel.totalAmount,
          depositedAmount: depositedAmount,
          expenses: 0,
          transactionCode: parcel.transactionCode || '',
          createdAt: parcel.createdAt
        };

        clerkData[clerkId].parcels.push(clerkParcelData);
        clerkData[clerkId].totalPaidAmount += parcel.totalAmount;
      });

      // Fetch all expenses (clerk_deposit + clerk_expense categories) filtered by date
      let allClerkExpenses: DailyExpense[] = [];
      try {
        const fetched = await wmsApi.getDailyExpenses();
        allClerkExpenses = fetched.filter(e => {
          if (filterDate) {
            return e.date.split('T')[0] === filterDate;
          }
          return true;
        });
      } catch {}

      const clerkDepositRecords = allClerkExpenses.filter(e => e.category === ('clerk_deposit' as any));
      const approvedClerkExpenses = allClerkExpenses.filter(e =>
        e.category !== ('clerk_deposit' as any) && e.status === 'approved'
      );

      // Attach clerk_deposit records to each clerk
      clerkDepositRecords.forEach(dep => {
        if (dep.clerkId && clerkData[dep.clerkId]) {
          clerkData[dep.clerkId].clerkDeposits.push(dep);
        }
      });

      // Calculate totals for each clerk
      Object.values(clerkData).forEach(clerk => {
        clerk.totalDeposited = clerk.clerkDeposits
          .filter(d => d.status === 'approved')
          .reduce((sum, d) => sum + d.amount, 0);
        clerk.totalExpenses = approvedClerkExpenses
          .filter(e => e.clerkId === clerk.clerkId)
          .reduce((sum, e) => sum + e.amount, 0);
        clerk.remainingDebt = clerk.totalPaidAmount - clerk.totalDeposited - clerk.totalExpenses;
      });

      return Object.values(clerkData);
    } catch (error) {
      console.error('Error fetching clerk cash-in data:', error);
      return [];
    }
  }, []);

  // Fetch clerk debit data with date range filtering - optimized version
  const fetchClerkDebitData = useCallback(async (dateRange: { from: string; to: string }): Promise<ClerkDebitData[]> => {
    try {
      // Use isolated instance for clerk debit to prevent cache interference
      console.log(`[Clerk Debit Debug] Fetching data for date range:`, dateRange);
      const allParcels = await wmsApi.getAllParcelsProgressively({
        dateRange: { from: dateRange.from, to: dateRange.to },
        chunkSize: 1200, // Use updated chunk size to match server optimizations
        fields: [
          'id', 'waybillNumber', 'receiver', 'totalAmount', 'status', 
          'createdAt', 'createdById', 'paymentMethods', 'branchDeposits', 'expenses'
        ],
      });
      
      console.log(`[Clerk Debit Debug] Received ${allParcels.length} total parcels`);
      console.log(`[Clerk Debit Debug] Sample parcel paymentMethods:`, allParcels.slice(0, 3).map(p => ({
        waybill: p.waybillNumber, 
        paymentMethods: p.paymentMethods,
        createdAt: p.createdAt
      })));

      // Filter parcels by date range and prepaid status
      // Clerk Debit Management tracks prepaid parcels where clerks collected money in advance
      const paidParcels = allParcels.filter(parcel => {
        const paymentMethod = (parcel.paymentMethods || '').toLowerCase();
        // Include only prepaid parcels (paid/advance/prepaid) - exclude COD
        const isPrepaid = (paymentMethod.includes('paid') || 
                          paymentMethod.includes('advance') || 
                          paymentMethod.includes('prepaid')) &&
                         !paymentMethod.includes('cod');
        
        if (!isPrepaid) return false;

        if (parcel.createdAt) {
          try {
            const parcelDate = new Date(parcel.createdAt).toISOString().split('T')[0];
            return parcelDate >= dateRange.from && parcelDate <= dateRange.to;
          } catch (error) {
            return false;
          }
        }
        return false;
      });
      
      console.log(`[Clerk Debit Debug] After prepaid filtering: ${paidParcels.length} paid parcels`);
      console.log(`[Clerk Debit Debug] Prepaid sample:`, paidParcels.slice(0, 3).map(p => ({
        waybill: p.waybillNumber, 
        paymentMethods: p.paymentMethods,
        clerk: p.createdById
      })));

      // Get existing deposits from database for the date range
      let existingDeposits: ParcelDepositDto[] = [];
      try {
        // Get all deposits - we'll filter by parcel date, not deposit date
        const allDeposits = await wmsApi.getParcelDeposits();

        // Don't filter deposits by date here - we'll match them to parcels by parcelId
        // and let the parcel date filtering handle the date range
        existingDeposits = allDeposits;
      } catch (apiError) {
        console.log('ParcelDeposits API not available for debit calculation');
      }

      // Create a map of existing deposits by parcelId
      const depositsMap: Record<string, ParcelDepositDto> = {};
      existingDeposits.forEach(deposit => {
        depositsMap[deposit.parcelId] = deposit;
      });


      // Get users to populate clerk information
      const users = await wmsApi.getUsers();

      // Group transactions by clerk
      const clerkDebitMap: Record<number, ClerkDebitData> = {};

      paidParcels.forEach(parcel => {
        const clerkId = parcel.createdById;
        if (!clerkId) return;

        const clerk = users.find(u => u.id === clerkId);
        if (!clerk) return;

        if (!clerkDebitMap[clerkId]) {
          clerkDebitMap[clerkId] = {
            clerkId: clerkId,
            clerkName: clerk.firstName && clerk.lastName
              ? `${clerk.firstName} ${clerk.lastName}`
              : clerk.username,
            clerkUsername: clerk.username,
            totalPaidAmount: 0,
            totalDeposited: 0,
            totalExpenses: 0,
            currentDebit: 0,
            parcelCount: 0,
            dateRange: dateRange,
            transactions: [],
            approvedExpenseItems: []
          };
        }

        // Check for deposit data from database only
        let depositedAmount = 0;

        const dbDeposit = depositsMap[parcel.id];
        if (dbDeposit) {
          depositedAmount = dbDeposit.depositedAmount;
        }

        // Calculate net debit for this transaction (expenses handled separately via DailyExpense)
        const netDebit = parcel.totalAmount - depositedAmount;

        // Add transaction to clerk's data
        const transaction: ClerkDebitTransaction = {
          date: new Date(parcel.createdAt).toISOString().split('T')[0],
          parcelId: parcel.id,
          waybillNumber: parcel.waybillNumber,
          paidAmount: parcel.totalAmount,
          depositedAmount: depositedAmount,
          expenses: 0,
          netDebit: netDebit,
          destination: parcel.destination,
          sender: parcel.sender,
          receiver: parcel.receiver
        };

        clerkDebitMap[clerkId].transactions.push(transaction);
        clerkDebitMap[clerkId].totalPaidAmount += parcel.totalAmount;
        clerkDebitMap[clerkId].totalDeposited += depositedAmount;
        clerkDebitMap[clerkId].parcelCount += 1;
      });

      // Fetch approved expenses and apply per-clerk within date range
      let allApprovedExpenses: DailyExpense[] = [];
      try {
        const allExpenses = await wmsApi.getDailyExpenses();
        allApprovedExpenses = allExpenses.filter(e => {
          if (e.status !== 'approved') return false;
          const expDate = e.date.split('T')[0];
          return expDate >= dateRange.from && expDate <= dateRange.to;
        });
      } catch {}

      // Calculate current debit for each clerk, subtracting approved clerk expenses
      Object.values(clerkDebitMap).forEach(clerk => {
        const clerkExpenseItems = allApprovedExpenses.filter(e => e.clerkId === clerk.clerkId);
        const clerkApprovedExpenses = clerkExpenseItems.reduce((sum, e) => sum + e.amount, 0);
        clerk.totalExpenses = clerkApprovedExpenses;
        clerk.currentDebit = clerk.totalPaidAmount - clerk.totalDeposited - clerkApprovedExpenses;
        clerk.approvedExpenseItems = clerkExpenseItems.sort(
          (a, b) => new Date(a.date).getTime() - new Date(b.date).getTime()
        );
        clerk.transactions.sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime());
      });

      // Also include clerks who only have expenses but no parcel transactions in the date range
      allApprovedExpenses.forEach(expense => {
        if (!expense.clerkId) return;
        if (clerkDebitMap[expense.clerkId]) return; // already handled
        const clerkUser = users.find(u => u.id === expense.clerkId);
        if (!clerkUser) return;
        clerkDebitMap[expense.clerkId] = {
          clerkId: expense.clerkId,
          clerkName: clerkUser.firstName && clerkUser.lastName
            ? `${clerkUser.firstName} ${clerkUser.lastName}`
            : clerkUser.username,
          clerkUsername: clerkUser.username,
          totalPaidAmount: 0,
          totalDeposited: 0,
          totalExpenses: 0,
          currentDebit: 0,
          parcelCount: 0,
          dateRange: dateRange,
          transactions: [],
          approvedExpenseItems: []
        };
      });

      // Second pass: populate expense-only clerks
      Object.values(clerkDebitMap).forEach(clerk => {
        if (clerk.approvedExpenseItems.length > 0) return;
        const clerkExpenseItems = allApprovedExpenses.filter(e => e.clerkId === clerk.clerkId);
        if (clerkExpenseItems.length === 0) return;
        const total = clerkExpenseItems.reduce((sum, e) => sum + e.amount, 0);
        clerk.totalExpenses = total;
        clerk.currentDebit = -total;
        clerk.approvedExpenseItems = clerkExpenseItems.sort(
          (a, b) => new Date(a.date).getTime() - new Date(b.date).getTime()
        );
      });

      // Sort clerks by highest debit first
      return Object.values(clerkDebitMap).sort((a, b) => b.currentDebit - a.currentDebit);
    } catch (error) {
      console.error('Error fetching clerk debit data:', error);
      return [];
    }
  }, []);

  // Fetch daily sales data for all users
  const fetchDailySalesData = useCallback(async (dateRange: { from: string; to: string }): Promise<UserDailySales[]> => {
    try {
      setLoadingDailySales(true);

      // Get all parcels and users
      const [allParcels, allUsers] = await Promise.all([
        wmsApi.getAllParcelsForReports({
          startDate: dateRange.from,
          endDate: dateRange.to
        }),
        wmsApi.getUsers()
      ]);

      // Filter parcels by date range
      const filteredParcels = allParcels.filter(parcel => {
        if (parcel.createdAt) {
          try {
            const parcelDate = new Date(parcel.createdAt).toISOString().split('T')[0];
            return parcelDate >= dateRange.from && parcelDate <= dateRange.to;
          } catch (error) {
            return false;
          }
        }
        return false;
      });

      // Group parcels by user and date
      const userSalesMap: Record<number, UserDailySales> = {};

      allUsers.forEach(user => {
        userSalesMap[user.id] = {
          userId: user.id,
          username: user.username,
          firstName: user.firstName,
          lastName: user.lastName,
          role: typeof user.role === 'object' ? user.role?.name || 'Unknown' : user.role || 'Unknown',
          dailySales: []
        };
      });

      // Group parcels by user and date
      filteredParcels.forEach(parcel => {
        if (parcel.createdById) {
          const userId = parcel.createdById;
          const parcelDate = parcel.createdAt ? new Date(parcel.createdAt).toISOString().split('T')[0] : '';
          const amount = parcel.totalAmount || parcel.amount || 0;

          if (userSalesMap[userId] && parcelDate) {
            const existingDay = userSalesMap[userId].dailySales.find(day => day.date === parcelDate);

            if (existingDay) {
              existingDay.totalSales += amount;
              existingDay.parcelCount += 1;
              existingDay.averageValue = existingDay.totalSales / existingDay.parcelCount;
            } else {
              userSalesMap[userId].dailySales.push({
                date: parcelDate,
                totalSales: amount,
                parcelCount: 1,
                averageValue: amount
              });
            }
          }
        }
      });

      // Sort daily sales by date and filter out users with no sales
      const result = Object.values(userSalesMap)
        .filter(user => user.dailySales.length > 0)
        .map(user => ({
          ...user,
          dailySales: user.dailySales.sort((a, b) => a.date.localeCompare(b.date))
        }))
        .sort((a, b) => a.username.localeCompare(b.username));

      return result;
    } catch (error) {
      console.error('Error fetching daily sales data:', error);
      return [];
    } finally {
      setLoadingDailySales(false);
    }
  }, []);

  // Generate cash-in data for clerks with paid parcels (fallback method)
  const generateClerkCashInData = useCallback((parcels: Parcel[], filterDate?: string): ClerkCashInData[] => {
    const clerkData: Record<number, ClerkCashInData> = {};

    // Filter for paid parcels only and by date if specified
    let paidParcels = parcels.filter(parcel =>
      parcel.paymentMethods?.toLowerCase().includes('paid')
    );

    // Apply date filter for cash-in feature
    if (filterDate) {
      paidParcels = paidParcels.filter(parcel => {
        if (parcel.createdAt) {
          try {
            const parcelDate = new Date(parcel.createdAt);
            return parcelDate.toISOString().split('T')[0] === filterDate;
          } catch (error) {
            return false;
          }
        }
        return false;
      });
    }

    paidParcels.forEach(parcel => {
      const clerkId = parcel.createdById;
      if (!clerkId) return;

      let clerkName = `User ${clerkId}`;
      let clerkUsername = `user_${clerkId}`;

      // Try to get clerk name from createdBy object
      if (parcel.createdBy) {
        if (typeof parcel.createdBy === 'object') {
          clerkName = parcel.createdBy.firstName && parcel.createdBy.lastName
            ? `${parcel.createdBy.firstName} ${parcel.createdBy.lastName}`
            : parcel.createdBy.username || `User ${clerkId}`;
          clerkUsername = parcel.createdBy.username || `user_${clerkId}`;
        } else if (typeof parcel.createdBy === 'string') {
          clerkName = parcel.createdBy;
          clerkUsername = parcel.createdBy;
        }
      } else if (userCache.has(clerkId)) {
        clerkName = userCache.get(clerkId)!;
        // If we have the name in cache, we might not have the username easily available if it wasn't stored separately
        // But usually userCache stores the display name which we want to use as clerkName
        clerkUsername = `user_${clerkId}`; // Fallback
      }

      if (!clerkData[clerkId]) {
        clerkData[clerkId] = {
          clerkId,
          clerkName,
          clerkUsername,
          totalPaidAmount: 0,
          totalDeposited: 0,
          totalExpenses: 0,
          remainingDebt: 0,
          parcels: [],
          clerkDeposits: []
        };
      }

      const parcelData: ClerkParcelData = {
        id: parcel.id,
        waybillNumber: parcel.waybillNumber || '',
        sender: parcel.sender,
        receiver: parcel.receiver,
        destination: parcel.destination,
        amount: parcel.totalAmount || 0,
        depositedAmount: 0,
        expenses: 0,
        transactionCode: parcel.transactionCode,
        createdAt: parcel.createdAt
      };

      clerkData[clerkId].parcels.push(parcelData);
      clerkData[clerkId].totalPaidAmount += parcelData.amount;
    });

    // Calculate totals for each clerk (expenses come from approvedExpenses state)
    Object.values(clerkData).forEach(clerk => {
      clerk.totalDeposited = clerk.parcels.reduce((sum, p) => sum + p.depositedAmount, 0);
      const clerkExpenses = approvedExpenses
        .filter(e => {
          if (e.clerkId !== clerk.clerkId) return false;
          if (filterDate) return e.date.split('T')[0] === filterDate;
          return true;
        })
        .reduce((sum, e) => sum + e.amount, 0);
      clerk.totalExpenses = clerkExpenses;
      clerk.remainingDebt = clerk.totalPaidAmount - clerk.totalDeposited - clerkExpenses;
    });

    return Object.values(clerkData).sort((a, b) => b.totalPaidAmount - a.totalPaidAmount);
  }, [userCache, approvedExpenses]);

  // Generate sales per clerk data with proper user resolution
  const generateSalesPerClerkData = useCallback((parcels: Parcel[]): SalesPerClerkData[] => {
    const clerkData: Record<number, { parcels: number; sales: number; name: string; username: string }> = {};

    parcels.forEach(parcel => {
      const clerkId = parcel.createdById;
      if (!clerkId) return;

      let clerkName = `User ${clerkId}`;
      let clerkUsername = `user_${clerkId}`;

      // Try to get clerk name from createdBy object
      if (parcel.createdBy) {
        if (typeof parcel.createdBy === 'object') {
          clerkName = parcel.createdBy.firstName && parcel.createdBy.lastName
            ? `${parcel.createdBy.firstName} ${parcel.createdBy.lastName}`
            : parcel.createdBy.username || `User ${clerkId}`;
          clerkUsername = parcel.createdBy.username || `user_${clerkId}`;
        } else if (typeof parcel.createdBy === 'string') {
          clerkName = parcel.createdBy;
          clerkUsername = parcel.createdBy;
        }
      } else if (userCache.has(clerkId)) {
        // Use cached user name
        clerkName = userCache.get(clerkId)!;
        clerkUsername = `user_${clerkId}`; // Fallback
      }

      if (!clerkData[clerkId]) {
        clerkData[clerkId] = {
          parcels: 0,
          sales: 0,
          name: clerkName,
          username: clerkUsername
        };
      }
      clerkData[clerkId].parcels++;
      clerkData[clerkId].sales += parcel.totalAmount || 0;
    });

    return Object.entries(clerkData).map(([clerkIdStr, data]) => ({
      clerkId: parseInt(clerkIdStr),
      clerkName: data.name,
      clerkUsername: data.username,
      totalParcels: data.parcels,
      totalSales: data.sales,
      averagePerParcel: data.parcels > 0 ? data.sales / data.parcels : 0,
      performance: (data.sales >= 50000 ? 'excellent' :
        data.sales >= 25000 ? 'good' :
          data.sales >= 10000 ? 'average' : 'below-average') as 'excellent' | 'good' | 'average' | 'below-average'
    })).sort((a, b) => b.totalSales - a.totalSales); // Sort by highest sales first
  }, [userCache]);

  const generateDeliveryRateData = useCallback((parcels: Parcel[]): DeliveryRateData[] => {
    const destinationData: Record<string, { total: number; delivered: number; pending: number; inTransit: number }> = {};

    parcels.forEach(parcel => {
      const dest = parcel.destination || 'Unknown';
      if (!destinationData[dest]) {
        destinationData[dest] = { total: 0, delivered: 0, pending: 0, inTransit: 0 };
      }
      destinationData[dest].total++;

      if (parcel.status === 3) destinationData[dest].delivered++;
      else if (parcel.status === 0) destinationData[dest].pending++;
      else if (parcel.status === 2) destinationData[dest].inTransit++;
    });

    return Object.entries(destinationData).map(([destination, data]) => ({
      destination,
      totalParcels: data.total,
      deliveredParcels: data.delivered,
      pendingParcels: data.pending,
      inTransitParcels: data.inTransit,
      deliveryRate: data.total > 0 ? (data.delivered / data.total) * 100 : 0
    }));
  }, []);

  // Helper function to normalize parcelIds
  const normalizeParcelIds = useCallback((parcelIds: string[] | { $values: string[] } | undefined): string[] => {
    if (!parcelIds) return [];
    if (Array.isArray(parcelIds)) return parcelIds;
    if (typeof parcelIds === 'object' && parcelIds.$values && Array.isArray(parcelIds.$values)) {
      return parcelIds.$values;
    }
    return [];
  }, []);

  const generateUndeliveredParcelData = useCallback((dispatches: Dispatch[], parcels: Parcel[]): UndeliveredParcelData[] => {
    return dispatches.map(dispatch => {
      const normalizedParcelIds = normalizeParcelIds(dispatch.parcelIds);
      const allDispatchParcels = parcels.filter(p => normalizedParcelIds.includes(p.id));
      const dispatchParcels = allDispatchParcels.filter(p => p.status !== 3);

      const uniqueDestinations = allDispatchParcels.map(p => p.destination).filter(Boolean).filter((d, i, arr) => arr.indexOf(d) === i);
      const destination = dispatch.destination || uniqueDestinations.join(', ') || 'N/A';

      return {
        dispatchCode: dispatch.dispatchCode,
        destination,
        sourceBranch: dispatch.sourceBranch || 'N/A',
        driver: dispatch.driver,
        vehicleNumber: dispatch.vehicleNumber,
        totalParcels: normalizedParcelIds.length || dispatch.totalParcels || 0,
        undeliveredParcels: dispatchParcels.length,
        totalAmount: dispatchParcels.reduce((sum, p) => sum + (p.totalAmount || 0), 0),
        dispatchDate: dispatch.dispatchTime
      };
    }).filter(d => d.undeliveredParcels > 0);
  }, [normalizeParcelIds]);

  const generateBranchDebitData = useCallback((parcels: Parcel[]): { [key: string]: BranchDebitSummary } => {
    const branchData: { [key: string]: BranchDebitSummary } = {};

    // Filter for COD parcels (all statuses)
    const codParcels = parcels.filter(parcel => {
      const paymentMethod = (parcel.paymentMethods || '').toLowerCase();
      return (paymentMethod.includes('cod') || paymentMethod.includes('cash on delivery'));
    });

    // Filter for Paid parcels (all statuses)
    const paidParcels = parcels.filter(parcel => {
      const paymentMethod = (parcel.paymentMethods || '').toLowerCase();
      return (paymentMethod.includes('paid') || paymentMethod.includes('advance') || paymentMethod.includes('prepaid'));
    });

    // Group COD parcels by destination (branch) and date
    codParcels.forEach(parcel => {
      const branch = parcel.destination;
      const date = new Date(parcel.dispatchedAt || parcel.createdAt).toISOString().split('T')[0];
      const amount = parcel.totalAmount || 0;

      if (!branchData[branch]) {
        branchData[branch] = {
          branch,
          totalCodAmount: 0,
          totalDeposited: 0,
          totalExpenses: 0,
          totalDebt: 0,
          transactions: []
        };
      }

      // Find existing transaction for this date
      let dayTransaction = branchData[branch].transactions.find(t => t.date === date);
      if (!dayTransaction) {
        dayTransaction = {
          branch,
          date,
          codTotal: 0,
          depositedAmount: 0,
          debt: 0
        };
        branchData[branch].transactions.push(dayTransaction);
      }

      dayTransaction.codTotal += amount;
      branchData[branch].totalCodAmount += amount;
    });

    // Add Nairobi-Paids as a special branch for paid parcels
    if (paidParcels.length > 0) {
      const nairobiPaidsBranch = 'Nairobi-Paids';

      if (!branchData[nairobiPaidsBranch]) {
        branchData[nairobiPaidsBranch] = {
          branch: nairobiPaidsBranch,
          totalCodAmount: 0,
          totalDeposited: 0,
          totalExpenses: 0,
          totalDebt: 0,
          transactions: []
        };
      }

      // Group paid parcels by date
      paidParcels.forEach(parcel => {
        const date = new Date(parcel.dispatchedAt || parcel.createdAt).toISOString().split('T')[0];
        const amount = parcel.totalAmount || 0;

        // Find existing transaction for this date
        let dayTransaction = branchData[nairobiPaidsBranch].transactions.find(t => t.date === date);
        if (!dayTransaction) {
          dayTransaction = {
            branch: nairobiPaidsBranch,
            date,
            codTotal: 0,
            depositedAmount: 0,
            debt: 0
          };
          branchData[nairobiPaidsBranch].transactions.push(dayTransaction);
        }

        dayTransaction.codTotal += amount;
        branchData[nairobiPaidsBranch].totalCodAmount += amount;
      });
    }

    // Calculate debt for each transaction (will be overridden by API data if available)
    Object.values(branchData).forEach(branchSummary => {
      branchSummary.transactions.sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime());

      let runningDebt = 0;
      branchSummary.transactions.forEach(transaction => {
        runningDebt += transaction.codTotal - transaction.depositedAmount;
        transaction.debt = runningDebt;
        branchSummary.totalDebt = runningDebt;
      });

      branchSummary.totalDebt = Math.max(0, runningDebt);
    });

    return branchData;
  }, []);

  const loadAndMergeBranchDeposits = useCallback(async (generatedData: { [key: string]: BranchDebitSummary }, approvedExp: DailyExpense[] = []) => {
    try {
      setLoadingDeposits(true);

      // Load existing deposit data from API
      const apiDeposits = await wmsApi.getBranchDeposits(
        undefined, // Get all branches
        filters.startDate || undefined,
        filters.endDate || undefined
      );

      console.log('API deposits response:', apiDeposits);
      console.log('Sample API deposit structure:', apiDeposits[0]);

      // Merge API data with generated COD data
      const mergedData = { ...generatedData };

      if (apiDeposits && Array.isArray(apiDeposits)) {
        // Group API records by branch and aggregate them
        const groupedApiData: { [key: string]: any } = {};

        apiDeposits.forEach((record: any) => {
          console.log('Processing deposit record:', record);
          if (!record || !record.branch) {
            console.warn('Invalid deposit record:', record);
            return;
          }

          const branch = record.branch;

          if (!groupedApiData[branch]) {
            groupedApiData[branch] = {
              branch: branch,
              totalDeposited: 0,
              totalDebt: 0,
              transactions: []
            };
          }

          // Add this record as a transaction
          const transaction = {
            id: record.id,
            branch: record.branch,
            date: record.date.split('T')[0],
            codTotal: record.codTotal || 0,
            depositedAmount: parseFloat(record.depositAmount) || 0,
            debt: record.runningDebt || 0,
            createdAt: record.createdAt,
            updatedAt: record.updatedAt,
            createdById: record.createdById,
            createdByName: record.createdByName,
            status: record.status,
            rejectionReason: record.rejectionReason
          };

          groupedApiData[branch].transactions.push(transaction);
          groupedApiData[branch].totalDeposited += transaction.depositedAmount;
        });

        // After processing all transactions, calculate the correct total debt for each branch
        Object.keys(groupedApiData).forEach(branch => {
          const branchData = groupedApiData[branch];
          // Sort transactions by date to get the latest running debt
          branchData.transactions.sort((a: any, b: any) => new Date(a.date).getTime() - new Date(b.date).getTime());
          // The total debt should be the running debt from the latest transaction
          const latestTransaction = branchData.transactions[branchData.transactions.length - 1];
          branchData.totalDebt = latestTransaction ? latestTransaction.debt : 0;
        });

        // Now merge the grouped API data with generated data
        Object.keys(groupedApiData).forEach(branch => {
          const branchApiData = groupedApiData[branch];

          if (mergedData[branch]) {
            // Update existing branch with API deposit data
            mergedData[branch].totalDeposited = branchApiData.totalDeposited;

            // Calculate total debt: COD total from generated data minus total deposited from API
            const codTotal = mergedData[branch].totalCodAmount || 0;
            const totalDeposited = branchApiData.totalDeposited || 0;
            mergedData[branch].totalDebt = codTotal - totalDeposited;

            console.log(`Branch ${branch}: COD=${codTotal}, Deposited=${totalDeposited}, Debt=${mergedData[branch].totalDebt}`);

            // Update transactions with deposit amounts and IDs.
            // Deposit date matches the parcel booking day (T) directly.
            branchApiData.transactions.forEach((apiTransaction: any) => {
              const parcelDate = apiTransaction.date.split('T')[0];
              const localTransaction = mergedData[branch].transactions.find(
                t => t.date === parcelDate
              );
              if (localTransaction) {
                localTransaction.id = apiTransaction.id;
                localTransaction.depositedAmount = apiTransaction.depositedAmount;
                localTransaction.debt = apiTransaction.debt;
                localTransaction.createdAt = apiTransaction.createdAt;
                localTransaction.updatedAt = apiTransaction.updatedAt;
                localTransaction.createdById = apiTransaction.createdById;
                localTransaction.createdByName = apiTransaction.createdByName;
                localTransaction.status = apiTransaction.status;
                localTransaction.rejectionReason = apiTransaction.rejectionReason;
              }
            });
          } else {
            // This branch only exists in API (no COD data for current period)
            mergedData[branch] = {
              branch: branchApiData.branch,
              totalCodAmount: 0,
              totalDeposited: branchApiData.totalDeposited,
              totalExpenses: 0,
              totalDebt: branchApiData.totalDebt,
              transactions: branchApiData.transactions
            };
          }
        });
      }

      // Apply approved branch expenses to each branch's debt
      Object.keys(mergedData).forEach(branch => {
        const branchExpenses = approvedExp
          .filter(e => e.branchName === branch || e.branchName?.toLowerCase() === branch.toLowerCase())
          .reduce((sum, e) => sum + e.amount, 0);
        mergedData[branch].totalExpenses = branchExpenses;
        mergedData[branch].totalDebt = mergedData[branch].totalDebt - branchExpenses;
      });

      return mergedData;
    } catch (error) {
      console.error('Failed to load branch deposits:', error);
      setError('Failed to load deposit data. Using generated data only.');
      return generatedData || {};
    } finally {
      setLoadingDeposits(false);
    }
  }, [filters.startDate, filters.endDate]);

  const generateMonthlyTrendsData = useCallback((parcels: Parcel[]) => {
    const monthlyData: Record<string, { total: number; delivered: number; pending: number }> = {};

    parcels.forEach(parcel => {
      const date = new Date(parcel.createdAt);
      const monthKey = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`;

      if (!monthlyData[monthKey]) {
        monthlyData[monthKey] = { total: 0, delivered: 0, pending: 0 };
      }

      monthlyData[monthKey].total++;
      if (parcel.status === 3) monthlyData[monthKey].delivered++;
      else if (parcel.status === 0) monthlyData[monthKey].pending++;
    });

    const sortedMonths = Object.keys(monthlyData).sort();

    return {
      categories: sortedMonths.map(month => {
        const [year, monthNum] = month.split('-');
        const date = new Date(parseInt(year), parseInt(monthNum) - 1);
        return date.toLocaleDateString('en-US', { month: 'short', year: 'numeric' });
      }),
      series: [
        {
          name: 'Total Parcels',
          data: sortedMonths.map(month => monthlyData[month].total)
        },
        {
          name: 'Delivered',
          data: sortedMonths.map(month => monthlyData[month].delivered)
        },
        {
          name: 'Pending',
          data: sortedMonths.map(month => monthlyData[month].pending)
        }
      ]
    };
  }, []);

  const fetchReportData = useCallback(async (useStreaming: boolean = false, forceRefresh: boolean = false) => {
    // For clerk-debit, use a specific cache key that includes the debit date range to prevent interference
    const cacheKey = currentReport === 'clerk-debit' 
      ? `clerk-debit-${debitDateRange.from}-${debitDateRange.to}`
      : `${currentReport}-${JSON.stringify(filters)}-${cashInDate}-${JSON.stringify(debitDateRange)}`;
    
    // Check if we have fresh cached data for this tab
    if (!forceRefresh && tabDataCache[cacheKey]) {
      const cached = tabDataCache[cacheKey];
      const age = Date.now() - cached.timestamp;
      const CACHE_DURATION = 5 * 60 * 1000; // 5 minutes
      
      // For clerk-debit, only validate date range since clerk filtering happens at UI level
      let filtersMatch = false;
      if (currentReport === 'clerk-debit') {
        // Only check if the cached debit date range matches current debit date range
        const cachedDebitRange = cached.filters.debitDateRange || {};
        filtersMatch = JSON.stringify(cachedDebitRange) === JSON.stringify(debitDateRange);
      } else {
        filtersMatch = JSON.stringify(cached.filters) === JSON.stringify(filters);
      }
      
      if (age < CACHE_DURATION && filtersMatch) {
        console.log(`Reports: Using cached data for ${currentReport} (${Math.round(age / 1000)}s old)`);
        setReportData(cached.data);
        setLoading(false);
        return;
      }
    }

    if (loading || tabLoadingStates[cacheKey]) return; // Prevent concurrent calls

    try {
      setLoading(true);
      setTabLoadingStates(prev => ({ ...prev, [cacheKey]: true }));
      setError('');

      // Use cached data or fetch fresh data
      const reportFilters = {
        startDate: filters.startDate,
        endDate: filters.endDate,
        branch: filters.branch,
        destination: filters.destination,
        clerk: filters.clerk
      };

      let cachedResult: any;
      
      // For clerk-debit, skip global data loading since it uses isolated cache
      if (currentReport === 'clerk-debit') {
        console.log('Reports: Skipping global data load for clerk-debit (using isolated cache)');
        // Skip to the clerk-debit specific processing - don't load global data
        const clerkDateRange = {
          from: debitDateRange.from,
          to: debitDateRange.to
        };
        const data: any = {};
        const [clerkDebitResult, allExpenses] = await Promise.all([
          fetchClerkDebitData(clerkDateRange),
          wmsApi.getDailyExpenses().catch(() => [] as DailyExpense[])
        ]);
        const approvedExp = allExpenses.filter(e => e.status === 'approved');
        setApprovedExpenses(approvedExp);
        data.clerkDebit = clerkDebitResult;
        setClerkDebitData(data.clerkDebit);
        setReportData(data);
        
        // Cache the result for this tab
        setTabDataCache(prev => ({
          ...prev,
          [cacheKey]: {
            data,
            timestamp: Date.now(),
            filters: { ...reportFilters, debitDateRange }
          }
        }));
        return; // Early return - don't process global data
      } else if (useStreaming) {
        // Use streaming data with progress updates
        cachedResult = await getStreamingData(reportFilters, (progressData) => {
          // Update parcels and dispatches as data streams in
          setParcels(progressData.parcels);
          setDispatches(progressData.dispatches);
          console.log(`Reports: Streaming progress - ${progressData.loadProgress?.loaded}/${progressData.loadProgress?.total} records`);
        });
      } else {
        // Check for cached data first
        if (cachedData && isDataFresh()) {
          const filtersMatch = !reportFilters || (
            JSON.stringify(cachedData.filters) === JSON.stringify(reportFilters)
          );
          
          if (filtersMatch) {
            console.log('Reports: Using existing cached data');
            cachedResult = cachedData;
          } else {
            cachedResult = await getCachedData(reportFilters);
          }
        } else {
          cachedResult = await getCachedData(reportFilters);
        }
      }

      const parcelsData = cachedResult.parcels;
      const dispatchesData = cachedResult.dispatches;

      console.log(`Reports: Using ${isDataFresh() ? 'fresh' : 'cached'} data (${getDataAge()})`);

      setParcels(parcelsData);
      setDispatches(dispatchesData);

      // Filter data based on filters
      let filteredParcels = parcelsData;

      if (filters.startDate) {
        filteredParcels = filteredParcels.filter((p: Parcel) =>
          p.createdAt && new Date(p.createdAt) >= new Date(filters.startDate)
        );
      }

      if (filters.endDate) {
        filteredParcels = filteredParcels.filter((p: Parcel) =>
          p.createdAt && new Date(p.createdAt) <= new Date(filters.endDate)
        );
      }

      if (filters.destination) {
        filteredParcels = filteredParcels.filter((p: Parcel) => p.destination === filters.destination);
      }

      if (filters.clerk) {
        filteredParcels = filteredParcels.filter((p: Parcel) =>
          p.createdBy?.toLowerCase().includes(filters.clerk.toLowerCase())
        );
      }

      // Generate report data based on current report type
      let data: any = {};

      switch (currentReport) {
        case 'sales-per-clerk':
          data.salesPerClerk = generateSalesPerClerkData(filteredParcels);
          // Load real cash-in data from API
          data.clerkCashIn = await fetchClerkCashInData(cashInDate);
          setClerkCashInData(data.clerkCashIn);
          break;
        case 'delivery-rate':
          data.deliveryRate = generateDeliveryRateData(filteredParcels);
          break;
        case 'undelivered-parcels':
          data.undeliveredParcels = generateUndeliveredParcelData(dispatchesData, filteredParcels);
          setUndeliveredPage(1);
          break;
        case 'cod-delivered': {
          const [generatedData, allExpenses] = await Promise.all([
            Promise.resolve(generateBranchDebitData(filteredParcels)),
            wmsApi.getDailyExpenses().catch(() => [] as DailyExpense[])
          ]);
          const approvedExp = allExpenses.filter(e => e.status === 'approved');
          setApprovedExpenses(approvedExp);
          const mergedData = await loadAndMergeBranchDeposits(generatedData, approvedExp);
          data.branchDebit = mergedData;
          setBranchDebitData(mergedData || {});
          break;
        }
        case 'dashboard':
          data.salesPerClerk = generateSalesPerClerkData(filteredParcels);
          data.deliveryRate = generateDeliveryRateData(filteredParcels);
          data.monthlyTrends = generateMonthlyTrendsData(filteredParcels);
          break;
        default:
          data.parcels = filteredParcels;
      }

      setReportData(data);
      
      // Cache the result for this tab
      setTabDataCache(prev => ({
        ...prev,
        [cacheKey]: {
          data,
          timestamp: Date.now(),
          filters: reportFilters
        }
      }));

    } catch (err) {
      const errorMessage = cacheError || 'Failed to fetch report data';
      setError(errorMessage);
      console.error('Reports: Error fetching data:', err);
    } finally {
      setLoading(false);
      setTabLoadingStates(prev => ({ ...prev, [cacheKey]: false }));
    }
  }, [currentReport, filters, user, cashInDate, debitDateRange, getCachedData, getStreamingData, cacheLoading, tabDataCache, tabLoadingStates]); // Removed loading, isDataFresh, getDataAge, cachedData to prevent re-fetches on tab switch

  const fetchFilteredParcels = useCallback(async () => {
    if (!parcelFilters.dateFrom && !parcelFilters.dateTo && !parcelFilters.destination) {
      // Don't fetch if no filters are applied to avoid loading all parcels
      setFilteredParcels([]);
      return;
    }

    try {
      setParcelListLoading(true);
      setError('');

      // Build filters for API call
      const apiFilters: any = {};

      if (parcelFilters.dateFrom) {
        apiFilters.dateFrom = parcelFilters.dateFrom;
      }

      if (parcelFilters.dateTo) {
        apiFilters.dateTo = parcelFilters.dateTo;
      }

      if (parcelFilters.destination) {
        apiFilters.destination = parcelFilters.destination;
      }

      if (parcelFilters.status !== 'all') {
        apiFilters.status = parcelFilters.status;
      }

      const parcelsData = await wmsApi.getAllParcelsForReports({
        startDate: parcelFilters.dateFrom,
        endDate: parcelFilters.dateTo,
        destination: parcelFilters.destination !== 'All' ? parcelFilters.destination : undefined
      });

      // Apply client-side filtering based on the filters
      let filtered = parcelsData;

      if (parcelFilters.dateFrom) {
        filtered = filtered.filter(p =>
          new Date(p.createdAt) >= new Date(parcelFilters.dateFrom)
        );
      }

      if (parcelFilters.dateTo) {
        filtered = filtered.filter(p =>
          new Date(p.createdAt) <= new Date(parcelFilters.dateTo)
        );
      }

      if (parcelFilters.destination) {
        filtered = filtered.filter(p => p.destination === parcelFilters.destination);
      }

      if (parcelFilters.status !== 'all') {
        filtered = filtered.filter(p => p.status === parseInt(parcelFilters.status));
      }

      // Populate user information for parcels that don't have createdBy populated
      try {
        const users = await wmsApi.getUsers();

        const updatedParcels = filtered.map(parcel => {
          if (!parcel.createdBy && parcel.createdById) {
            const creator = users.find(u => u.id === parcel.createdById);
            if (creator) {
              return {
                ...parcel,
                ...parcel,
                createdBy: {
                  username: creator.username,
                  firstName: creator.firstName,
                  lastName: creator.lastName
                }
              };
            }
          }
          return parcel;
        });

        setFilteredParcels(updatedParcels);
      } catch (userErr) {
        // If users API fails, still display parcels without user info
        setFilteredParcels(filtered);
      }
    } catch (err) {
      setError('Failed to fetch parcels. Please try again.');
    } finally {
      setParcelListLoading(false);
    }
  }, [parcelFilters, user]);

  const fetchDestinations = useCallback(async () => {
    try {
      // Use Promise.allSettled to handle API failures gracefully
      const results = await Promise.allSettled([
        wmsApi.getAllParcelsForReports(),
        wmsApi.getUsers()
      ]);

      const parcelsData = results[0].status === 'fulfilled' ? results[0].value : [];
      const usersData = results[1].status === 'fulfilled' ? results[1].value : [];

      // Extract unique destinations from successfully loaded parcels
      if (parcelsData.length > 0) {
        const destinationSet = new Set(parcelsData.map(p => p.destination).filter(Boolean));
        const uniqueDestinations = Array.from(destinationSet);
        setDestinations(uniqueDestinations.sort());
      }

      // Extract clerks (users who have created parcels) from successfully loaded users
      if (usersData.length > 0 && parcelsData.length > 0) {
        const clerkUsers = usersData.filter(u =>
          u.role?.name === 'Clerk' ||
          u.roles?.includes('clerk') ||
          parcelsData.some(p => p.createdById === u.id)
        );
        setClerks(clerkUsers.map(u => ({
          id: u.id,
          username: u.firstName && u.lastName ? `${u.firstName} ${u.lastName}` : u.username
        })));
      }
    } catch (err) {
      // Silently fail and leave destinations/clerks as empty arrays
      setDestinations([]);
      setClerks([]);
    }
  }, [user]);

  const getUserDisplayName = useCallback(async (userId: number): Promise<string> => {
    // Check cache first
    if (userCache.has(userId)) {
      return userCache.get(userId)!;
    }

    try {
      const user = await wmsApi.getUserById(userId);
      const displayName = user ?
        (user.firstName && user.lastName ? `${user.firstName} ${user.lastName}` : user.username || `User ${userId}`) :
        `User ${userId}`;

      // Cache the result
      setUserCache(prev => new Map(prev.set(userId, displayName)));
      return displayName;
    } catch (error) {
      const fallbackName = `User ${userId}`;
      setUserCache(prev => new Map(prev.set(userId, fallbackName)));
      return fallbackName;
    }
  }, [userCache]);

  const handleDepositSubmit = useCallback(async () => {
    if (!selectedBranchDebit || !depositAmount) return;

    const amount = parseFloat(depositAmount);
    if (isNaN(amount) || amount < 0) {
      setError('Please enter a valid deposit amount');
      return;
    }

    try {
      setSavingDeposit(true);
      setError('');

      if (selectedBranchDebit.id) {
        // Update existing deposit
        await wmsApi.updateBranchDeposit(selectedBranchDebit.id, amount);
        alert('Deposit updated successfully!');
      } else {
        // Create new deposit
        await wmsApi.createBranchDeposit({
          branch: selectedBranchDebit.branch,
          date: selectedBranchDebit.date,
          codTotal: selectedBranchDebit.codTotal,
          depositAmount: amount
        });
        alert('Deposit created successfully!');
      }

      // Refresh the deposit data after successful save
      // Add a small delay to ensure the database has been updated
      await new Promise(resolve => setTimeout(resolve, 500));

      console.log('Refreshing deposit data after update...');
      const generatedData = generateBranchDebitData(parcels);
      const mergedData = await loadAndMergeBranchDeposits(generatedData);
      console.log('Updated merged data:', mergedData);
      setBranchDebitData(mergedData);

      setShowDepositModal(false);
      setSelectedBranchDebit(null);
      setDepositAmount('');
    } catch (error) {
      console.error('Failed to save deposit:', error);
      setError('Failed to save deposit. Please try again.');
    } finally {
      setSavingDeposit(false);
    }
  }, [selectedBranchDebit, depositAmount, generateBranchDebitData, loadAndMergeBranchDeposits, parcels]);

  const openDepositModal = useCallback((branchDebit: BranchDebitData) => {
    setSelectedBranchDebit(branchDebit);
    setDepositAmount(branchDebit.depositedAmount.toString());
    setShowDepositModal(true);
  }, []);

  // Cash-in related functions
  const openClerkDetailsModal = useCallback((clerk: ClerkCashInData) => {
    setSelectedClerk(clerk);
    setShowClerkDetailsModal(true);
  }, []);

  const openParcelModal = useCallback((parcel: ClerkParcelData) => {
    setSelectedClerkParcel(parcel);
    setParcelDepositAmount(parcel.depositedAmount.toString());
    setShowParcelModal(true);
  }, []);

  const handleClerkDepositApproval = useCallback(async (depositId: number, approved: boolean, rejectionReason?: string) => {
    try {
      setApprovingDepositId(depositId);
      await wmsApi.approveExpense({ expenseId: depositId, approved, rejectionReason });
      const refreshed = await fetchClerkCashInData(cashInDate);
      setClerkCashInData(refreshed);
      const updatedClerk = refreshed.find(c => c.clerkDeposits.some(d => d.id === depositId));
      if (updatedClerk) setSelectedClerk(updatedClerk);
    } catch (err) {
      console.error('Failed to update clerk deposit:', err);
    } finally {
      setApprovingDepositId(null);
    }
  }, [fetchClerkCashInData, cashInDate]);

  const handleParcelUpdate = useCallback(async () => {
    if (!selectedClerkParcel || !selectedClerk) return;

    const depositAmount = parseFloat(parcelDepositAmount) || 0;

    if (depositAmount < 0) {
      setError('Amount cannot be negative');
      return;
    }

    try {
      setUpdatingParcel(true);

      // Try to update via ParcelDeposits API first
      try {
        console.log('Updating parcel deposit via API...');

        await wmsApi.updateOrCreateParcelDeposit(selectedClerkParcel.id, {
          depositedAmount: depositAmount,
          expenses: 0,
          updatedById: user?.id ? parseInt(user.id.toString()) : undefined
        });

        // If API call succeeds, refresh data efficiently - only update affected clerk's data locally

        // Update the local clerk cash-in data without full refresh
        const updatedClerkData = clerkCashInData.map(clerk => {
          if (clerk.clerkId === selectedClerk.clerkId) {
            const updatedParcels = clerk.parcels.map(parcel => {
              if (parcel.id === selectedClerkParcel.id) {
                return {
                  ...parcel,
                  depositedAmount: depositAmount,
                  expenses: 0
                };
              }
              return parcel;
            });

            // Recalculate totals for this clerk only (expenses come from approved DailyExpenses, not parcels)
            const totalDeposited = updatedParcels.reduce((sum, p) => sum + p.depositedAmount, 0);
            const totalExpenses = clerk.totalExpenses;
            const remainingDebt = clerk.totalPaidAmount - totalDeposited - totalExpenses;

            return {
              ...clerk,
              parcels: updatedParcels,
              totalDeposited,
              totalExpenses,
              remainingDebt
            };
          }
          return clerk;
        });

        setClerkCashInData(updatedClerkData);

        // Only refresh Clerk Debit Management data (lighter operation)
        const refreshedDebitData = await fetchClerkDebitData(debitDateRange);
        setClerkDebitData(refreshedDebitData);

        // Update the selected clerk data
        const updatedSelectedClerk = updatedClerkData.find(c => c.clerkId === selectedClerk.clerkId);
        if (updatedSelectedClerk) {
          setSelectedClerk(updatedSelectedClerk);
        }

        console.log('Successfully updated parcel deposit via API');
        alert('Parcel updated successfully and saved to database!');
        setShowParcelModal(false);
        setSelectedClerkParcel(null);
        setParcelDepositAmount('');
        return;
      } catch (apiError) {
        console.error('Failed to update parcel deposit via API:', apiError);
        console.error('Error details:', {
          message: apiError instanceof Error ? apiError.message : 'Unknown error',
          stack: apiError instanceof Error ? apiError.stack : undefined
        });

        alert(`Failed to update parcel deposit: ${apiError instanceof Error ? apiError.message : 'Unknown error'}. Please check the console for details.`);
        return;
      }
      setShowParcelModal(false);
      setSelectedClerkParcel(null);
      setParcelDepositAmount('');

    } catch (err) {
      setError('Failed to update parcel information');
    } finally {
      setUpdatingParcel(false);
    }
  }, [selectedClerkParcel, selectedClerk, parcelDepositAmount, user, fetchClerkCashInData, cashInDate, fetchClerkDebitData, debitDateRange]);

  const downloadBranchPDF = useCallback((branchSummary: BranchDebitSummary) => {
    const printWindow = window.open('', '_blank');
    if (printWindow) {
      const filterInfo = [];
      if (filters.startDate) filterInfo.push(`From: ${filters.startDate}`);
      if (filters.endDate) filterInfo.push(`To: ${filters.endDate}`);

      const isNairobiPaids = branchSummary.branch === 'Nairobi-Paids';
      const reportTitle = isNairobiPaids ? 'Paid Parcels Report' : 'Branch Debit Report';
      const amountLabel = isNairobiPaids ? 'Paid Amount' : 'COD Amount';

      printWindow.document.write(`
        <html>
          <head>
            <title>${reportTitle} - ${branchSummary.branch}</title>
            <style>
              body { 
                font-family: Arial, sans-serif; 
                padding: 20px; 
                margin: 0; 
                font-size: 12px; 
                color: #333;
              }
              .header {
                text-align: center;
                margin-bottom: 30px;
                border-bottom: 2px solid #333;
                padding-bottom: 10px;
              }
              h1 { 
                color: #333; 
                margin-bottom: 5px; 
                font-size: 18px;
              }
              .branch-name {
                font-size: 16px;
                color: #666;
                margin-bottom: 10px;
              }
              .filters { 
                margin-bottom: 20px; 
                color: #666; 
                font-style: italic;
              }
              .summary {
                display: grid;
                grid-template-columns: 1fr 1fr 1fr;
                gap: 15px;
                margin-bottom: 30px;
              }
              .summary-card {
                background: #f8f9fa;
                padding: 15px;
                border-radius: 5px;
                text-align: center;
                border: 1px solid #ddd;
              }
              .summary-label {
                font-size: 10px;
                color: #666;
                margin-bottom: 5px;
                text-transform: uppercase;
              }
              .summary-value {
                font-size: 14px;
                font-weight: bold;
                color: #333;
              }
              table { 
                width: 100%; 
                border-collapse: collapse; 
                margin-top: 20px; 
              }
              th, td { 
                border: 1px solid #ddd; 
                padding: 8px; 
                text-align: left; 
              }
              th { 
                background-color: #f5f5f5; 
                font-weight: bold; 
                font-size: 11px;
              }
              td {
                font-size: 10px;
              }
              .amount {
                text-align: right;
                font-weight: bold;
              }
              .debt-positive {
                color: #d32f2f;
              }
              .debt-negative {
                color: #2e7d32;
              }
              .footer {
                margin-top: 30px;
                text-align: center;
                font-size: 10px;
                color: #666;
                border-top: 1px solid #ddd;
                padding-top: 10px;
              }
              @media print { 
                body { print-color-adjust: exact; }
                .summary { page-break-inside: avoid; }
              }
            </style>
          </head>
          <body>
            <div class="header">
              <h1>${reportTitle}</h1>
              <div class="branch-name">${branchSummary.branch}</div>
              <div class="filters">
                ${filterInfo.length > 0 ? `Filters: ${filterInfo.join(', ')}` : 'All Data'}
              </div>
            </div>
            
            <div class="summary">
              <div class="summary-card">
                <div class="summary-label">Total ${amountLabel}</div>
                <div class="summary-value">KSh ${branchSummary.totalCodAmount.toLocaleString()}</div>
              </div>
              <div class="summary-card">
                <div class="summary-label">Total Deposited</div>
                <div class="summary-value">KSh ${branchSummary.totalDeposited.toLocaleString()}</div>
              </div>
              <div class="summary-card">
                <div class="summary-label">${isNairobiPaids ? 'Net Balance' : 'Outstanding Debt'}</div>
                <div class="summary-value ${branchSummary.totalDebt > 0 ? 'debt-positive' : 'debt-negative'}">
                  KSh ${Math.abs(branchSummary.totalDebt).toLocaleString()}
                  ${branchSummary.totalDebt < 0 ? ' (Credit)' : ''}
                </div>
              </div>
            </div>

            <table>
              <thead>
                <tr>
                  <th>Date</th>
                  <th>${amountLabel}</th>
                  <th>Deposited Amount</th>
                  <th>Running Balance</th>
                </tr>
              </thead>
              <tbody>
                ${branchSummary.transactions.map(transaction => `
                  <tr>
                    <td>${fmtDate(transaction.date)}</td>
                    <td class="amount">KSh ${transaction.codTotal.toLocaleString()}</td>
                    <td class="amount">KSh ${transaction.depositedAmount.toLocaleString()}</td>
                    <td class="amount ${(transaction.codTotal - transaction.depositedAmount) > 0 ? 'debt-positive' : 'debt-negative'}">
                      KSh ${Math.abs(transaction.codTotal - transaction.depositedAmount).toLocaleString()}
                      ${(transaction.codTotal - transaction.depositedAmount) < 0 ? ' (Credit)' : ''}
                    </td>
                  </tr>
                `).join('')}
              </tbody>
            </table>

            <div class="footer">
              <p>Generated on ${fmtDateTime(new Date())} EAT | Ficma Home Logistics Branch Management System</p>
              <p>Total Transactions: ${branchSummary.transactions.length}</p>
            </div>
          </body>
        </html>
      `);
      printWindow.document.close();
      printWindow.print();
    }
  }, [filters.startDate, filters.endDate, formatCurrency]);

  // Download Clerk Debit Details as PDF
  const downloadClerkDebitPDF = useCallback((clerkData: ClerkDebitData) => {
    const printWindow = window.open('', '_blank');
    if (printWindow) {
      const dateRange = `${debitDateRange.from} to ${debitDateRange.to}`;

      printWindow.document.write(`
        <html>
          <head>
            <title>Clerk Debit Details - ${clerkData.clerkName}</title>
            <style>
              body { 
                font-family: Arial, sans-serif; 
                padding: 20px; 
                margin: 0; 
                font-size: 12px; 
                color: #333;
              }
              .header {
                text-align: center;
                margin-bottom: 30px;
                border-bottom: 2px solid #333;
                padding-bottom: 10px;
              }
              h1 { 
                color: #333; 
                margin-bottom: 5px; 
                font-size: 18px;
              }
              .clerk-info {
                font-size: 14px;
                color: #666;
                margin-bottom: 10px;
              }
              .date-range { 
                margin-bottom: 20px; 
                color: #666; 
                font-style: italic;
                text-align: center;
              }
              .summary {
                display: grid;
                grid-template-columns: 1fr 1fr 1fr 1fr;
                gap: 15px;
                margin-bottom: 30px;
              }
              .summary-card {
                background: #f8f9fa;
                padding: 15px;
                border-radius: 5px;
                text-align: center;
                border: 1px solid #ddd;
              }
              .summary-label {
                font-size: 10px;
                color: #666;
                margin-bottom: 5px;
                text-transform: uppercase;
              }
              .summary-value {
                font-size: 14px;
                font-weight: bold;
                color: #333;
              }
              .summary-card.collected { background: #e3f2fd; }
              .summary-card.deposited { background: #e8f5e8; }
              .summary-card.expenses { background: #fff3e0; }
              .summary-card.debit { background: #ffebee; }
              .row-expense { background: #fff8f0; }
              .row-expense td { color: #b45309; }
              .badge { display: inline-block; padding: 2px 7px; border-radius: 9999px; font-size: 9px; font-weight: bold; text-transform: uppercase; }
              .badge-parcel { background: #dbeafe; color: #1e40af; }
              .badge-expense { background: #fed7aa; color: #92400e; }
              .expense-amount { color: #d97706; font-weight: bold; }
              
              table { 
                width: 100%; 
                border-collapse: collapse; 
                margin-top: 20px; 
              }
              th, td { 
                border: 1px solid #ddd; 
                padding: 8px; 
                text-align: left; 
              }
              th { 
                background-color: #f5f5f5; 
                font-weight: bold; 
                font-size: 11px;
              }
              td {
                font-size: 10px;
              }
              .amount {
                text-align: right;
                font-weight: bold;
              }
              .debit-positive {
                color: #d32f2f;
              }
              .debit-negative {
                color: #2e7d32;
              }
              .footer {
                margin-top: 30px;
                text-align: center;
                font-size: 10px;
                color: #666;
                border-top: 1px solid #ddd;
                padding-top: 10px;
              }
              @media print {
                body { margin: 0; }
                .header { margin-bottom: 20px; }
              }
            </style>
          </head>
          <body>
            <div class="header">
              <h1>Clerk Debit Details</h1>
              <div class="clerk-info">
                <strong>${clerkData.clerkName}</strong> (@${clerkData.clerkUsername})
              </div>
              <div class="date-range">Period: ${dateRange}</div>
            </div>

            <div class="summary">
              <div class="summary-card collected">
                <div class="summary-label">Total Collected</div>
                <div class="summary-value">KSh ${clerkData.totalPaidAmount.toLocaleString()}</div>
              </div>
              <div class="summary-card deposited">
                <div class="summary-label">Total Deposited</div>
                <div class="summary-value">KSh ${clerkData.totalDeposited.toLocaleString()}</div>
              </div>
              <div class="summary-card expenses">
                <div class="summary-label">Approved Expenses</div>
                <div class="summary-value">KSh ${clerkData.totalExpenses.toLocaleString()}</div>
              </div>
              <div class="summary-card debit">
                <div class="summary-label">Current Debit</div>
                <div class="summary-value ${clerkData.currentDebit > 0 ? 'debit-positive' : 'debit-negative'}">
                  KSh ${Math.abs(clerkData.currentDebit).toLocaleString()}
                  ${clerkData.currentDebit < 0 ? ' (Credit)' : ''}
                </div>
              </div>
            </div>

            <h3>Transaction &amp; Expense Details</h3>
            <table>
              <thead>
                <tr>
                  <th>Date</th>
                  <th>Type</th>
                  <th>Reference / Description</th>
                  <th>Destination</th>
                  <th>Paid Amount</th>
                  <th>Deposited</th>
                  <th>Expense</th>
                  <th>Net Debit</th>
                </tr>
              </thead>
              <tbody>
                ${(() => {
                  const rows = [
                    ...clerkData.transactions.map(t => ({ kind: 'parcel', date: t.date, data: t })),
                    ...(clerkData.approvedExpenseItems || []).map(e => ({ kind: 'expense', date: e.date.split('T')[0], data: e }))
                  ].sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime());

                  return rows.map(row => {
                    if (row.kind === 'parcel') {
                      const t = row.data as ClerkDebitTransaction;
                      return `
                        <tr>
                          <td>${fmtDate(t.date)}</td>
                          <td><span class="badge badge-parcel">Parcel</span></td>
                          <td>${t.waybillNumber}<br/><span style="font-size:9px;color:#666">${t.sender} &rarr; ${t.receiver}</span></td>
                          <td>${t.destination}</td>
                          <td class="amount">KSh ${t.paidAmount.toLocaleString()}</td>
                          <td class="amount">KSh ${t.depositedAmount.toLocaleString()}</td>
                          <td>&mdash;</td>
                          <td class="amount ${t.netDebit > 0 ? 'debit-positive' : 'debit-negative'}">
                            KSh ${Math.abs(t.netDebit).toLocaleString()}${t.netDebit < 0 ? ' (Cr)' : ''}
                          </td>
                        </tr>`;
                    } else {
                      const e = row.data as DailyExpense;
                      return `
                        <tr class="row-expense">
                          <td>${fmtDate(e.date)}</td>
                          <td><span class="badge badge-expense">Expense</span></td>
                          <td>${e.description}<br/><span style="font-size:9px;color:#92400e">${e.category.replace('_',' ')}${e.vendor ? ' &middot; ' + e.vendor : ''}</span></td>
                          <td>&mdash;</td>
                          <td>&mdash;</td>
                          <td>&mdash;</td>
                          <td class="amount expense-amount">-KSh ${e.amount.toLocaleString()}</td>
                          <td>&mdash;</td>
                        </tr>`;
                    }
                  }).join('');
                })()}
              </tbody>
            </table>

            <div class="footer">
              <p>Generated on ${fmtDateTime(new Date())} EAT | Ficma Home Logistics Clerk Management System</p>
              <p>Parcels: ${clerkData.parcelCount} | Approved Expenses: ${(clerkData.approvedExpenseItems || []).length}</p>
            </div>
          </body>
        </html>
      `);
      printWindow.document.close();
      printWindow.print();
    }
  }, [debitDateRange, formatCurrency]);

  useEffect(() => {
    fetchReportData();
    fetchDestinations();
  }, [filters, user, cashInDate, debitDateRange, fetchReportData]); // Removed currentReport to prevent re-fetching on tab switch

  useEffect(() => {
    if (currentReport === 'parcel-list') {
      fetchFilteredParcels();
    }
  }, [parcelFilters]); // Removed currentReport to prevent re-fetching on tab switch

  // Prefetch user data when parcels are loaded for detailed parcel list
  useEffect(() => {
    if (filteredParcels.length > 0) {
      const userIdSet = new Set(filteredParcels.map(p => p.createdById).filter(Boolean));
      const uniqueUserIds = Array.from(userIdSet);
      uniqueUserIds.forEach(userId => {
        if (!userCache.has(userId)) {
          getUserDisplayName(userId);
        }
      });
    }
  }, [filteredParcels]); // Remove userCache and function dependencies

  // Prefetch user data when parcels are loaded for reports
  useEffect(() => {
    if (parcels.length > 0) {
      const userIdSet = new Set(parcels.map(p => p.createdById).filter(Boolean));
      const uniqueUserIds = Array.from(userIdSet);
      uniqueUserIds.forEach(userId => {
        if (!userCache.has(userId)) {
          getUserDisplayName(userId);
        }
      });
    }
  }, [parcels]); // Remove userCache and function dependencies

  // Load cash-in data when date changes or when on cash-in tab
  useEffect(() => {
    if (currentReport === 'sales-per-clerk' && salesPerClerkTab === 'cash-in') {
      const loadCashInData = async () => {
        try {
          const data = await fetchClerkCashInData(cashInDate);
          setClerkCashInData(data);
        } catch (err) {
          setError('Failed to load cash-in data');
        }
      };
      loadCashInData();
    }
  }, [salesPerClerkTab, cashInDate, fetchClerkCashInData]); // Removed currentReport to prevent re-fetching on tab switch

  // Load daily sales data when date range changes or when on daily-sales tab
  useEffect(() => {
    if (currentReport === 'sales-per-clerk' && salesPerClerkTab === 'daily-sales') {
      const loadDailySalesData = async () => {
        try {
          const data = await fetchDailySalesData(dailySalesDateRange);
          setUsersList(data);
          if (data.length > 0) {
            setSelectedUser(data[0]); // Select first user by default
          }
        } catch (err) {
          setError('Failed to load daily sales data');
        }
      };
      loadDailySalesData();
    }
  }, [salesPerClerkTab, dailySalesDateRange, fetchDailySalesData]); // Removed currentReport to prevent re-fetching on tab switch

  const handleParcelFilterChange = (key: string, value: string | { from: string; to: string }) => {
    if (typeof value === 'string') {
      setParcelFilters(prev => ({ ...prev, [key]: value }));
    }
  };

  const clearParcelFilters = () => {
    setParcelFilters({
      dateFrom: '',
      dateTo: '',
      destination: '',
      status: 'all',
      paymentMethod: ''
    });
    setFilteredParcels([]);
  };

  const handleFilterChange = (key: string, value: string | { from: string; to: string }) => {
    setFilters(prev => ({ ...prev, [key]: value }));
  };

  const clearFilters = () => {
    setFilters({
      startDate: '',
      endDate: '',
      branch: '',
      destination: '',
      clerk: ''
    });
  };

  const getActiveFilterChips = () => {
    const chips: any[] = [];

    if (filters.startDate) {
      chips.push({
        key: 'startDate',
        label: 'Start Date',
        value: filters.startDate,
        onRemove: () => handleFilterChange('startDate', '')
      });
    }

    if (filters.endDate) {
      chips.push({
        key: 'endDate',
        label: 'End Date',
        value: filters.endDate,
        onRemove: () => handleFilterChange('endDate', '')
      });
    }

    if (filters.destination) {
      chips.push({
        key: 'destination',
        label: 'Destination',
        value: filters.destination,
        onRemove: () => handleFilterChange('destination', '')
      });
    }

    if (filters.clerk) {
      chips.push({
        key: 'clerk',
        label: 'Clerk',
        value: filters.clerk,
        onRemove: () => handleFilterChange('clerk', '')
      });
    }

    return chips;
  };

  const filterFields: FilterField[] = useMemo(() => [
    {
      key: 'startDate',
      label: 'Start Date',
      type: 'date',
      placeholder: 'Select start date',
      icon: <Calendar className="w-4 h-4" />
    },
    {
      key: 'endDate',
      label: 'End Date',
      type: 'date',
      placeholder: 'Select end date',
      icon: <Calendar className="w-4 h-4" />
    },
    {
      key: 'destination',
      label: 'Destination',
      type: 'select',
      placeholder: 'Select destination',
      icon: <MapPin className="w-4 h-4" />,
      options: [
        { value: '', label: 'All Destinations' },
        ...destinations.map(dest => ({ value: dest, label: dest }))
      ]
    },
    {
      key: 'clerk',
      label: 'Created By',
      type: 'select',
      placeholder: 'Select clerk',
      icon: <Users className="w-4 h-4" />,
      options: [
        { value: '', label: 'All Clerks' },
        ...clerks.map(clerk => ({ value: clerk.username, label: clerk.username }))
      ]
    }
  ], [destinations, clerks]);


  const formatDate = (dateString: string) => fmtDate(dateString);

  const getPerformanceBadge = (performance: string) => {
    const variants = {
      'excellent': 'success',
      'good': 'primary',
      'average': 'warning',
      'below-average': 'error'
    } as const;

    return (
      <Badge variant={variants[performance as keyof typeof variants] || 'gray'}>
        {performance.replace('-', ' ').toUpperCase()}
      </Badge>
    );
  };

  const renderDashboard = () => {
    if (!reportData) return null;

    const { salesPerClerk, deliveryRate, monthlyTrends } = reportData;

    // Prepare chart data
    const salesChartData = {
      series: [{
        name: 'Sales Amount',
        data: salesPerClerk?.map((clerk: SalesPerClerkData) => clerk.totalSales) || []
      }],
      options: {
        ...salesPerClerkChartOptions,
        xaxis: {
          ...salesPerClerkChartOptions.xaxis,
          categories: salesPerClerk?.map((clerk: SalesPerClerkData) => clerk.clerkName) || []
        }
      }
    };

    const deliveryChartData = {
      series: deliveryRate?.map((dest: DeliveryRateData) => dest.deliveryRate) || [],
      options: {
        ...deliveryRateChartOptions,
        labels: deliveryRate?.map((dest: DeliveryRateData) => dest.destination) || []
      }
    };

    const trendsChartData = {
      series: monthlyTrends?.series || [],
      options: {
        ...monthlyTrendsChartOptions,
        xaxis: {
          ...monthlyTrendsChartOptions.xaxis,
          categories: monthlyTrends?.categories || []
        }
      }
    };

    return (
      <div className="space-y-6">
        {/* Key Metrics - Hidden for Accountants */}
        {!isAccountant() && (
          <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
            <Card>
              <div className="flex items-center">
                <div className="p-3 rounded-lg bg-brand-100 dark:bg-brand-500/20">
                  <Package className="w-6 h-6 text-brand-600 dark:text-brand-400" />
                </div>
                <div className="ml-4">
                  <p className="text-sm font-medium text-gray-600 dark:text-gray-400">
                    Total Parcels
                  </p>
                  <p className="text-2xl font-bold text-gray-900 dark:text-white">
                    {parcels.length}
                  </p>
                </div>
              </div>
            </Card>

            <Card>
              <div className="flex items-center">
                <div className="p-3 rounded-lg bg-success-100 dark:bg-success-500/20">
                  <DollarSign className="w-6 h-6 text-success-600 dark:text-success-400" />
                </div>
                <div className="ml-4">
                  <p className="text-sm font-medium text-gray-600 dark:text-gray-400">
                    Total Sales
                  </p>
                  <p className="text-2xl font-bold text-gray-900 dark:text-white">
                    {formatCurrency(parcels.reduce((sum, p) => sum + (p.totalAmount || 0), 0))}
                  </p>
                </div>
              </div>
            </Card>

            <Card>
              <div className="flex items-center">
                <div className="p-3 rounded-lg bg-warning-100 dark:bg-warning-500/20">
                  <Truck className="w-6 h-6 text-warning-600 dark:text-warning-400" />
                </div>
                <div className="ml-4">
                  <p className="text-sm font-medium text-gray-600 dark:text-gray-400">
                    Dispatches
                  </p>
                  <p className="text-2xl font-bold text-gray-900 dark:text-white">
                    {dispatches.length}
                  </p>
                </div>
              </div>
            </Card>

            <Card>
              <div className="flex items-center">
                <div className="p-3 rounded-lg bg-error-100 dark:bg-error-500/20">
                  <AlertCircle className="w-6 h-6 text-error-600 dark:text-error-400" />
                </div>
                <div className="ml-4">
                  <p className="text-sm font-medium text-gray-600 dark:text-gray-400">
                    Pending Delivery
                  </p>
                  <p className="text-2xl font-bold text-gray-900 dark:text-white">
                    {parcels.filter(p => p.status === 0).length}
                  </p>
                </div>
              </div>
            </Card>
          </div>
        )}

        {/* Charts */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Sales Per Clerk Chart */}
          <Card>
            <div className="p-6">
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
                Sales Performance by Clerk
              </h3>
              <Chart
                options={salesChartData.options}
                series={salesChartData.series}
                type="bar"
                height={350}
              />
            </div>
          </Card>

          {/* Delivery Rate Chart */}
          <Card>
            <div className="p-6">
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
                Delivery Rate by Destination
              </h3>
              <Chart
                options={deliveryChartData.options}
                series={deliveryChartData.series}
                type="donut"
                height={350}
              />
            </div>
          </Card>
        </div>

        {/* Monthly Trends Chart */}
        <Card>
          <div className="p-6">
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
              Monthly Parcel Trends
            </h3>
            <Chart
              options={trendsChartData.options}
              series={trendsChartData.series}
              type="line"
              height={350}
            />
          </div>
        </Card>
      </div>
    );
  };

  const renderSalesPerClerk = () => {
    if (!reportData?.salesPerClerk) return null;

    const totalSales = reportData.salesPerClerk.reduce((sum: number, clerk: SalesPerClerkData) => sum + clerk.totalSales, 0);
    const totalParcels = reportData.salesPerClerk.reduce((sum: number, clerk: SalesPerClerkData) => sum + clerk.totalParcels, 0);

    return (
      <div className="space-y-6">
        {/* Filter Info Card */}
        <Card>
          <div className="p-6">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
                Sales per Clerk Report
              </h3>
              <Button
                variant="outline"
                onClick={() => setShowFilters(!showFilters)}
              >
                <Filter className="w-4 h-4 mr-2" />
                {showFilters ? 'Hide' : 'Show'} Filters
              </Button>
            </div>

            {/* Tab Navigation */}
            <div className="flex space-x-1 mb-6 border-b border-gray-200 dark:border-gray-700">
              <button
                onClick={() => setSalesPerClerkTab('performance')}
                className={`px-4 py-2 text-sm font-medium rounded-t-lg ${salesPerClerkTab === 'performance'
                  ? 'bg-blue-50 text-blue-600 border-b-2 border-blue-600 dark:bg-blue-500/20 dark:text-blue-400'
                  : 'text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300'
                  }`}
              >
                Performance Overview
              </button>
              <button
                onClick={() => setSalesPerClerkTab('cash-in')}
                className={`px-4 py-2 text-sm font-medium rounded-t-lg ${salesPerClerkTab === 'cash-in'
                  ? 'bg-blue-50 text-blue-600 border-b-2 border-blue-600 dark:bg-blue-500/20 dark:text-blue-400'
                  : 'text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300'
                  }`}
              >
                Cash-in Management
              </button>
              <button
                onClick={() => setSalesPerClerkTab('daily-sales')}
                className={`px-4 py-2 text-sm font-medium rounded-t-lg ${salesPerClerkTab === 'daily-sales'
                  ? 'bg-blue-50 text-blue-600 border-b-2 border-blue-600 dark:bg-blue-500/20 dark:text-blue-400'
                  : 'text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300'
                  }`}
              >
                Daily Sales
              </button>
            </div>

            {/* Cash-in Date Filter */}
            {salesPerClerkTab === 'cash-in' && (
              <div className="mb-6 p-4 bg-gray-50 dark:bg-gray-700 rounded-lg">
                <div className="flex items-center gap-4">
                  <div className="flex items-center gap-2">
                    <Calendar className="w-4 h-4 text-gray-600 dark:text-gray-400" />
                    <label className="text-sm font-medium text-gray-700 dark:text-gray-300">
                      Filter by Date:
                    </label>
                  </div>
                  <input
                    type="date"
                    value={cashInDate}
                    onChange={(e) => setCashInDate(e.target.value)}
                    className="px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                  />
                  <Button
                    variant="primary"
                    size="sm"
                    onClick={() => {
                      // Trigger data refresh with new date
                      if (currentReport === 'sales-per-clerk') {
                        fetchReportData(false, true); // Force refresh when applying filters
                      }
                    }}
                    className="ml-2"
                  >
                    <RefreshCw className="w-4 h-4 mr-1" />
                    Apply Filter
                  </Button>
                </div>
                <p className="text-xs text-gray-500 dark:text-gray-400 mt-2">
                  Select a specific date to view cash-in data for that day only.
                </p>
              </div>
            )}

            {salesPerClerkTab === 'performance' && (
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-4">
                <div className="bg-blue-50 dark:bg-blue-500/20 p-4 rounded-lg">
                  <div className="text-sm font-medium text-blue-600 dark:text-blue-400">Total Sales</div>
                  <div className="text-2xl font-bold text-blue-900 dark:text-blue-300">{formatCurrency(totalSales)}</div>
                </div>
                <div className="bg-green-50 dark:bg-green-500/20 p-4 rounded-lg">
                  <div className="text-sm font-medium text-green-600 dark:text-green-400">Total Parcels</div>
                  <div className="text-2xl font-bold text-green-900 dark:text-green-300">{totalParcels}</div>
                </div>
                <div className="bg-purple-50 dark:bg-purple-500/20 p-4 rounded-lg">
                  <div className="text-sm font-medium text-purple-600 dark:text-purple-400">Active Clerks</div>
                  <div className="text-2xl font-bold text-purple-900 dark:text-purple-300">{reportData.salesPerClerk.length}</div>
                </div>
              </div>
            )}

            {salesPerClerkTab === 'cash-in' && clerkCashInData.length > 0 && !isAccountant() && (
              <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-4">
                <div className="bg-green-50 dark:bg-green-500/20 p-4 rounded-lg">
                  <div className="text-sm font-medium text-green-600 dark:text-green-400">Total Collected</div>
                  <div className="text-2xl font-bold text-green-900 dark:text-green-300">
                    {formatCurrency(clerkCashInData.reduce((sum, clerk) => sum + clerk.totalPaidAmount, 0))}
                  </div>
                </div>
                <div className="bg-blue-50 dark:bg-blue-500/20 p-4 rounded-lg">
                  <div className="text-sm font-medium text-blue-600 dark:text-blue-400">Total Deposited</div>
                  <div className="text-2xl font-bold text-blue-900 dark:text-blue-300">
                    {formatCurrency(clerkCashInData.reduce((sum, clerk) => sum + clerk.totalDeposited, 0))}
                  </div>
                </div>
                <div className="bg-orange-50 dark:bg-orange-500/20 p-4 rounded-lg">
                  <div className="text-sm font-medium text-orange-600 dark:text-orange-400">Total Expenses</div>
                  <div className="text-2xl font-bold text-orange-900 dark:text-orange-300">
                    {formatCurrency(clerkCashInData.reduce((sum, clerk) => sum + clerk.totalExpenses, 0))}
                  </div>
                </div>
                <div className="bg-red-50 dark:bg-red-500/20 p-4 rounded-lg">
                  <div className="text-sm font-medium text-red-600 dark:text-red-400">Outstanding Debt</div>
                  <div className="text-2xl font-bold text-red-900 dark:text-red-300">
                    {formatCurrency(clerkCashInData.reduce((sum, clerk) => sum + clerk.remainingDebt, 0))}
                  </div>
                </div>
              </div>
            )}

            {(filters.startDate || filters.endDate || filters.clerk || filters.destination) && (
              <div className="bg-gray-50 dark:bg-gray-800 p-4 rounded-lg">
                <div className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Active Filters:</div>
                <div className="flex flex-wrap gap-2">
                  {filters.startDate && (
                    <Badge variant="primary">From: {filters.startDate}</Badge>
                  )}
                  {filters.endDate && (
                    <Badge variant="primary">To: {filters.endDate}</Badge>
                  )}
                  {filters.clerk && (
                    <Badge variant="success">Clerk: {filters.clerk}</Badge>
                  )}
                  {filters.destination && (
                    <Badge variant="warning">Destination: {filters.destination}</Badge>
                  )}
                </div>
              </div>
            )}

            {!filters.startDate && !filters.endDate && (
              <div className="bg-amber-50 dark:bg-amber-500/20 border border-amber-200 dark:border-amber-500/30 p-4 rounded-lg">
                <div className="flex items-center gap-2 text-amber-800 dark:text-amber-300">
                  <Calendar className="w-5 h-5" />
                  <span className="font-medium">Tip: Use date filters for specific periods</span>
                </div>
                <p className="text-amber-700 dark:text-amber-400 mt-1 text-sm">
                  Set start and end dates in the filters above to analyze sales performance for specific time periods.
                </p>
              </div>
            )}
          </div>
        </Card>

        {salesPerClerkTab === 'performance' && (
          <>
            <Card>
              <div className="p-6">
                <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
                  Sales Performance Chart
                </h3>
                <Chart
                  options={{
                    ...salesPerClerkChartOptions,
                    xaxis: {
                      ...salesPerClerkChartOptions.xaxis,
                      categories: reportData.salesPerClerk.map((clerk: SalesPerClerkData) => clerk.clerkName)
                    }
                  }}
                  series={[{
                    name: 'Sales Amount',
                    data: reportData.salesPerClerk.map((clerk: SalesPerClerkData) => clerk.totalSales)
                  }]}
                  type="bar"
                  height={350}
                />
              </div>
            </Card>

            <Card padding={false}>
              <div className="p-6 border-b border-gray-200">
                <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
                  Detailed Clerk Performance
                </h3>
              </div>
              <Table>
                <Table.Header>
                  <Table.Row>
                    <Table.Head>Clerk Name</Table.Head>
                    <Table.Head>Total Parcels</Table.Head>
                    <Table.Head>Total Sales</Table.Head>
                    <Table.Head>Average per Parcel</Table.Head>
                    <Table.Head>Performance</Table.Head>
                  </Table.Row>
                </Table.Header>
                <Table.Body>
                  {reportData.salesPerClerk.map((clerk: SalesPerClerkData, index: number) => (
                    <Table.Row key={index}>
                      <Table.Cell>
                        <div className="font-medium text-gray-900 dark:text-white">
                          {clerk.clerkName}
                        </div>
                        <div className="text-sm text-gray-500 dark:text-gray-400">
                          @{clerk.clerkUsername}
                        </div>
                      </Table.Cell>
                      <Table.Cell>{clerk.totalParcels}</Table.Cell>
                      <Table.Cell>{formatCurrency(clerk.totalSales)}</Table.Cell>
                      <Table.Cell>{formatCurrency(clerk.averagePerParcel)}</Table.Cell>
                      <Table.Cell>{getPerformanceBadge(clerk.performance)}</Table.Cell>
                    </Table.Row>
                  ))}
                </Table.Body>
              </Table>
            </Card>
          </>
        )}

        {salesPerClerkTab === 'cash-in' && (
          <>
            {/* Cash-in Summary by Clerk */}
            <Card padding={false}>
              <div className="p-6 border-b border-gray-200">
                <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
                  Clerk Cash-in Summary for {fmtDate(cashInDate)}
                </h3>
                <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
                  Showing paid parcels and deposit tracking for clerks on the selected date
                </p>
              </div>
              <Table>
                <Table.Header>
                  <Table.Row>
                    <Table.Head>Clerk Name</Table.Head>
                    <Table.Head>Total Collected</Table.Head>
                    <Table.Head>Total Deposited</Table.Head>
                    <Table.Head>Total Expenses</Table.Head>
                    <Table.Head>Outstanding Debt</Table.Head>
                    <Table.Head>Actions</Table.Head>
                  </Table.Row>
                </Table.Header>
                <Table.Body>
                  {clerkCashInData.map((clerk: ClerkCashInData, index: number) => (
                    <Table.Row key={index}>
                      <Table.Cell>
                        <div className="font-medium text-gray-900 dark:text-white">
                          {clerk.clerkName}
                        </div>
                        <div className="text-sm text-gray-500 dark:text-gray-400">
                          @{clerk.clerkUsername} ({clerk.parcels.length} parcels)
                        </div>
                      </Table.Cell>
                      <Table.Cell>{formatCurrency(clerk.totalPaidAmount)}</Table.Cell>
                      <Table.Cell>
                        <span className={clerk.totalDeposited > 0 ? 'text-green-600 font-medium' : ''}>
                          {formatCurrency(clerk.totalDeposited)}
                        </span>
                      </Table.Cell>
                      <Table.Cell>
                        <span className={clerk.totalExpenses > 0 ? 'text-orange-600 font-medium' : ''}>
                          {formatCurrency(clerk.totalExpenses)}
                        </span>
                      </Table.Cell>
                      <Table.Cell>
                        <span className={`font-medium ${clerk.remainingDebt > 0 ? 'text-red-600' :
                          clerk.remainingDebt < 0 ? 'text-green-600' : ''
                          }`}>
                          {formatCurrency(Math.abs(clerk.remainingDebt))}
                          {clerk.remainingDebt < 0 ? ' (Credit)' : ''}
                        </span>
                      </Table.Cell>
                      <Table.Cell>
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => openClerkDetailsModal(clerk)}
                        >
                          View Details
                        </Button>
                      </Table.Cell>
                    </Table.Row>
                  ))}
                </Table.Body>
              </Table>
            </Card>
          </>
        )}
        {salesPerClerkTab === 'daily-sales' && (
          <>
            {/* Daily Sales Date Range Filter */}
            <Card>
              <div className="p-6">
                <div className="flex items-center gap-4 mb-4">
                  <div className="flex items-center gap-2">
                    <Calendar className="w-4 h-4 text-gray-600 dark:text-gray-400" />
                    <label className="text-sm font-medium text-gray-700 dark:text-gray-300">
                      Date Range:
                    </label>
                  </div>
                  <input
                    type="date"
                    value={dailySalesDateRange.from}
                    onChange={(e) => setDailySalesDateRange(prev => ({ ...prev, from: e.target.value }))}
                    className="px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                  />
                  <span className="text-gray-500">to</span>
                  <input
                    type="date"
                    value={dailySalesDateRange.to}
                    onChange={(e) => setDailySalesDateRange(prev => ({ ...prev, to: e.target.value }))}
                    className="px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                  />
                </div>
                <p className="text-xs text-gray-500 dark:text-gray-400">
                  Select a date range to view daily sales data for all users.
                </p>
              </div>
            </Card>

            {/* Daily Sales Content */}
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
              {/* User List - Left Side */}
              <Card>
                <div className="p-6">
                  <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
                    Users ({usersList.length})
                  </h3>
                  {loadingDailySales ? (
                    <div className="flex items-center justify-center py-8">
                      <RefreshCw className="w-6 h-6 animate-spin text-blue-500" />
                      <span className="ml-2 text-sm text-gray-500">Loading users...</span>
                    </div>
                  ) : usersList.length === 0 ? (
                    <div className="text-center py-8">
                      <Users className="w-12 h-12 text-gray-400 mx-auto mb-4" />
                      <p className="text-gray-500 dark:text-gray-400">No users with sales data found</p>
                    </div>
                  ) : (
                    <div className="space-y-2 max-h-96 overflow-y-auto">
                      {usersList.map((user) => (
                        <button
                          key={user.userId}
                          onClick={() => setSelectedUser(user)}
                          className={`w-full text-left p-3 rounded-lg border transition-all ${selectedUser?.userId === user.userId
                            ? 'bg-blue-50 border-blue-500 text-blue-900 dark:bg-blue-500/20 dark:border-blue-400 dark:text-blue-300'
                            : 'bg-white border-gray-200 hover:bg-gray-50 dark:bg-gray-800 dark:border-gray-700 dark:hover:bg-gray-700'
                            }`}
                        >
                          <div className="font-medium text-sm">
                            {user.firstName && user.lastName ? `${user.firstName} ${user.lastName}` : user.username}
                          </div>
                          <div className="text-xs text-gray-500 dark:text-gray-400">
                            @{user.username} • {user.role}
                          </div>
                          <div className="text-xs text-gray-600 dark:text-gray-300 mt-1">
                            {user.dailySales.length} days • {formatCurrency(user.dailySales.reduce((sum, day) => sum + day.totalSales, 0))} total
                          </div>
                        </button>
                      ))}
                    </div>
                  )}
                </div>
              </Card>

              {/* Calendar and Sales Data - Right Side */}
              <div className="lg:col-span-2 space-y-6">
                {selectedUser ? (
                  <>
                    {/* Selected User Info */}
                    <Card>
                      <div className="p-6">
                        <div className="flex items-center justify-between mb-4">
                          <div>
                            <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
                              {selectedUser.firstName && selectedUser.lastName
                                ? `${selectedUser.firstName} ${selectedUser.lastName}`
                                : selectedUser.username}
                            </h3>
                            <p className="text-sm text-gray-500 dark:text-gray-400">
                              @{selectedUser.username} • {selectedUser.role}
                            </p>
                          </div>
                        </div>

                        {/* Summary Stats */}
                        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                          <div className="bg-blue-50 dark:bg-blue-500/20 p-4 rounded-lg">
                            <div className="text-sm font-medium text-blue-600 dark:text-blue-400">Total Sales</div>
                            <div className="text-2xl font-bold text-blue-900 dark:text-blue-300">
                              {formatCurrency(selectedUser.dailySales.reduce((sum, day) => sum + day.totalSales, 0))}
                            </div>
                          </div>
                          <div className="bg-green-50 dark:bg-green-500/20 p-4 rounded-lg">
                            <div className="text-sm font-medium text-green-600 dark:text-green-400">Total Parcels</div>
                            <div className="text-2xl font-bold text-green-900 dark:text-green-300">
                              {selectedUser.dailySales.reduce((sum, day) => sum + day.parcelCount, 0)}
                            </div>
                          </div>
                          <div className="bg-purple-50 dark:bg-purple-500/20 p-4 rounded-lg">
                            <div className="text-sm font-medium text-purple-600 dark:text-purple-400">Active Days</div>
                            <div className="text-2xl font-bold text-purple-900 dark:text-purple-300">
                              {selectedUser.dailySales.length}
                            </div>
                          </div>
                        </div>
                      </div>
                    </Card>

                    {/* Daily Sales Calendar View */}
                    <Card>
                      <div className="p-6">
                        <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
                          Daily Sales Calendar
                        </h3>

                        {selectedUser.dailySales.length === 0 ? (
                          <div className="text-center py-8">
                            <Calendar className="w-12 h-12 text-gray-400 mx-auto mb-4" />
                            <p className="text-gray-500 dark:text-gray-400">No sales data for the selected period</p>
                          </div>
                        ) : (
                          <div className="space-y-4">
                            {/* Simple list view of daily sales */}
                            <div className="grid gap-2">
                              {selectedUser.dailySales.map((dayData) => (
                                <div
                                  key={dayData.date}
                                  className="flex items-center justify-between p-3 bg-gray-50 dark:bg-gray-800 rounded-lg"
                                >
                                  <div className="flex items-center gap-3">
                                    <Calendar className="w-4 h-4 text-gray-400" />
                                    <div>
                                      <div className="font-medium text-gray-900 dark:text-white">
                                        {fmtDate(dayData.date)}
                                      </div>
                                      <div className="text-sm text-gray-500 dark:text-gray-400">
                                        {dayData.parcelCount} parcel{dayData.parcelCount !== 1 ? 's' : ''}
                                      </div>
                                    </div>
                                  </div>
                                  <div className="text-right">
                                    <div className="font-bold text-green-600 dark:text-green-400">
                                      {formatCurrency(dayData.totalSales)}
                                    </div>
                                    <div className="text-sm text-gray-500 dark:text-gray-400">
                                      Avg: {formatCurrency(dayData.averageValue)}
                                    </div>
                                  </div>
                                </div>
                              ))}
                            </div>
                          </div>
                        )}
                      </div>
                    </Card>
                  </>
                ) : (
                  <Card>
                    <div className="p-6 text-center">
                      <User className="w-12 h-12 text-gray-400 mx-auto mb-4" />
                      <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">
                        Select a User
                      </h3>
                      <p className="text-gray-500 dark:text-gray-400">
                        Choose a user from the list to view their daily sales data.
                      </p>
                    </div>
                  </Card>
                )}
              </div>
            </div>
          </>
        )}
      </div>
    );
  };

  const renderDeliveryRate = () => {
    if (!reportData?.deliveryRate) return null;

    return (
      <div className="space-y-6">
        <Card>
          <div className="p-6">
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
              Delivery Rate by Destination
            </h3>
            <Chart
              options={{
                ...deliveryRateChartOptions,
                labels: reportData.deliveryRate.map((dest: DeliveryRateData) => dest.destination)
              }}
              series={reportData.deliveryRate.map((dest: DeliveryRateData) => dest.deliveryRate)}
              type="donut"
              height={350}
            />
          </div>
        </Card>

        <Card padding={false}>
          <div className="p-6 border-b border-gray-200">
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
              Delivery Rate Details
            </h3>
          </div>
          <Table>
            <Table.Header>
              <Table.Row>
                <Table.Head>Destination</Table.Head>
                <Table.Head>Total Parcels</Table.Head>
                <Table.Head>Delivered</Table.Head>
                <Table.Head>Pending</Table.Head>
                <Table.Head>In Transit</Table.Head>
                <Table.Head>Delivery Rate</Table.Head>
              </Table.Row>
            </Table.Header>
            <Table.Body>
              {reportData.deliveryRate.map((dest: DeliveryRateData, index: number) => (
                <Table.Row key={index}>
                  <Table.Cell>
                    <div className="flex items-center text-gray-900 dark:text-white">
                      <MapPin className="w-4 h-4 mr-2 text-gray-400" />
                      {dest.destination}
                    </div>
                  </Table.Cell>
                  <Table.Cell>{dest.totalParcels}</Table.Cell>
                  <Table.Cell>
                    <div className="flex items-center text-success-600">
                      <CheckCircle className="w-4 h-4 mr-1" />
                      {dest.deliveredParcels}
                    </div>
                  </Table.Cell>
                  <Table.Cell>
                    <div className="flex items-center text-warning-600">
                      <Clock className="w-4 h-4 mr-1" />
                      {dest.pendingParcels}
                    </div>
                  </Table.Cell>
                  <Table.Cell>
                    <div className="flex items-center text-primary-600">
                      <Truck className="w-4 h-4 mr-1" />
                      {dest.inTransitParcels}
                    </div>
                  </Table.Cell>
                  <Table.Cell>
                    <Badge variant={dest.deliveryRate >= 80 ? 'success' : dest.deliveryRate >= 60 ? 'warning' : 'error'}>
                      {dest.deliveryRate.toFixed(1)}%
                    </Badge>
                  </Table.Cell>
                </Table.Row>
              ))}
            </Table.Body>
          </Table>
        </Card>
      </div>
    );
  };

  const renderUndeliveredParcels = () => {
    if (!reportData?.undeliveredParcels) return null;

    const allItems: UndeliveredParcelData[] = reportData.undeliveredParcels;
    const totalPages = Math.ceil(allItems.length / UNDELIVERED_PAGE_SIZE);
    const safePage = Math.min(undeliveredPage, totalPages || 1);
    const pageItems = allItems.slice((safePage - 1) * UNDELIVERED_PAGE_SIZE, safePage * UNDELIVERED_PAGE_SIZE);

    return (
      <Card padding={false}>
        <div className="p-6 border-b border-gray-200 flex items-center justify-between">
          <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
            Undelivered Parcels by Dispatch
          </h3>
          <span className="text-sm text-gray-500 dark:text-gray-400">
            {allItems.length} dispatch{allItems.length !== 1 ? 'es' : ''}
          </span>
        </div>
        <Table>
          <Table.Header>
            <Table.Row>
              <Table.Head>Dispatch Code</Table.Head>
              <Table.Head>Source</Table.Head>
              <Table.Head>Destination</Table.Head>
              <Table.Head>Driver</Table.Head>
              <Table.Head>Vehicle</Table.Head>
              <Table.Head>Total Parcels</Table.Head>
              <Table.Head>Undelivered</Table.Head>
              <Table.Head>Total Amount</Table.Head>
              <Table.Head>Dispatch Date</Table.Head>
            </Table.Row>
          </Table.Header>
          <Table.Body>
            {pageItems.map((dispatch: UndeliveredParcelData, index: number) => (
              <Table.Row key={index}>
                <Table.Cell>
                  <div className="font-medium text-gray-900 dark:text-white">
                    {dispatch.dispatchCode}
                  </div>
                </Table.Cell>
                <Table.Cell>
                  <div className="text-sm text-gray-700 dark:text-gray-300">
                    {dispatch.sourceBranch}
                  </div>
                </Table.Cell>
                <Table.Cell>
                  <div className="flex items-center text-gray-900 dark:text-white">
                    <MapPin className="w-4 h-4 mr-2 text-gray-400" />
                    {dispatch.destination}
                  </div>
                </Table.Cell>
                <Table.Cell>{dispatch.driver}</Table.Cell>
                <Table.Cell>{dispatch.vehicleNumber}</Table.Cell>
                <Table.Cell>{dispatch.totalParcels}</Table.Cell>
                <Table.Cell>
                  <Badge variant="error">
                    {dispatch.undeliveredParcels}
                  </Badge>
                </Table.Cell>
                <Table.Cell>{formatCurrency(dispatch.totalAmount)}</Table.Cell>
                <Table.Cell>{formatDate(dispatch.dispatchDate)}</Table.Cell>
              </Table.Row>
            ))}
          </Table.Body>
        </Table>
        {totalPages > 1 && (
          <div className="flex items-center justify-between px-6 py-3 border-t border-gray-200 dark:border-gray-700">
            <span className="text-sm text-gray-500 dark:text-gray-400">
              Page {safePage} of {totalPages} &mdash; showing {(safePage - 1) * UNDELIVERED_PAGE_SIZE + 1}–{Math.min(safePage * UNDELIVERED_PAGE_SIZE, allItems.length)} of {allItems.length}
            </span>
            <div className="flex items-center gap-2">
              <Button
                variant="outline"
                size="sm"
                disabled={safePage <= 1}
                onClick={() => setUndeliveredPage(p => Math.max(1, p - 1))}
              >
                Previous
              </Button>
              <Button
                variant="outline"
                size="sm"
                disabled={safePage >= totalPages}
                onClick={() => setUndeliveredPage(p => Math.min(totalPages, p + 1))}
              >
                Next
              </Button>
            </div>
          </div>
        )}
      </Card>
    );
  };

  const renderBranchManagerCodReport = () => {
    const branchName = user && typeof (user as any).branch === 'object' ? (user as any).branch?.name : undefined;

    const loadManagerDeposits = async () => {
      if (!branchName) return;
      setManagerCodLoading(true);
      setManagerSubmitError('');
      setManagerSubmitSuccess('');
      try {
        const [year, month] = managerCodMonth.split('-');
        const deposits = await wmsApi.getBranchDeposits(branchName);
        const filtered = (deposits || []).filter((d: any) => {
          const date = new Date(d.date);
          return date.getFullYear() === parseInt(year) && (date.getMonth() + 1) === parseInt(month);
        });
        setManagerDeposits(filtered);
      } catch (e) {
        console.error('Failed to load COD report', e);
      } finally {
        setManagerCodLoading(false);
      }
    };

    const monthlyCodTotal = managerDeposits.reduce((s: number, d: any) => s + (d.codTotal || 0), 0);
    const monthlyDeposited = managerDeposits.reduce((s: number, d: any) => s + (d.depositAmount || 0), 0);
    const monthlyBalance = monthlyCodTotal - monthlyDeposited;

    const handleManagerDepositSubmit = async (e: React.FormEvent) => {
      e.preventDefault();
      setManagerSubmitError('');
      setManagerSubmitSuccess('');
      const amount = parseFloat(managerDepositInput);
      if (isNaN(amount) || amount < 0) {
        setManagerSubmitError('Please enter a valid deposit amount.');
        return;
      }
      setManagerSubmitting(true);
      try {
        const previousDate = new Date();
        previousDate.setDate(previousDate.getDate() - 1);
        const dateStr = previousDate.toISOString().split('T')[0];
        await wmsApi.createBranchDeposit({
          branch: branchName!,
          date: dateStr,
          codTotal: 0,
          depositAmount: amount,
        });
        setManagerDepositInput('');
        setManagerSubmitSuccess('Deposit submitted for approval.');
        loadManagerDeposits();
      } catch {
        setManagerSubmitError('Submission failed. Please try again.');
      } finally {
        setManagerSubmitting(false);
      }
    };

    return (
      <div className="space-y-6">
        <div className="flex items-center gap-4">
          <input
            type="month"
            value={managerCodMonth}
            onChange={e => setManagerCodMonth(e.target.value)}
            className="border border-gray-300 rounded-lg px-3 py-2 text-sm dark:bg-gray-800 dark:border-gray-600 dark:text-white"
          />
          <Button onClick={loadManagerDeposits} disabled={managerCodLoading}>
            <RefreshCw className={`w-4 h-4 mr-2 ${managerCodLoading ? 'animate-spin' : ''}`} />
            Load Report
          </Button>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <Card>
            <div className="flex items-center">
              <div className="p-3 rounded-lg bg-blue-100 dark:bg-blue-500/20">
                <DollarSign className="w-6 h-6 text-blue-600 dark:text-blue-400" />
              </div>
              <div className="ml-4">
                <p className="text-sm font-medium text-gray-600 dark:text-gray-400">COD Total</p>
                <p className="text-2xl font-bold text-gray-900 dark:text-white">{formatCurrency(monthlyCodTotal)}</p>
              </div>
            </div>
          </Card>
          <Card>
            <div className="flex items-center">
              <div className="p-3 rounded-lg bg-green-100 dark:bg-green-500/20">
                <CheckCircle className="w-6 h-6 text-green-600 dark:text-green-400" />
              </div>
              <div className="ml-4">
                <p className="text-sm font-medium text-gray-600 dark:text-gray-400">Deposited</p>
                <p className="text-2xl font-bold text-gray-900 dark:text-white">{formatCurrency(monthlyDeposited)}</p>
              </div>
            </div>
          </Card>
          <Card>
            <div className="flex items-center">
              <div className="p-3 rounded-lg bg-red-100 dark:bg-red-500/20">
                <AlertCircle className="w-6 h-6 text-red-600 dark:text-red-400" />
              </div>
              <div className="ml-4">
                <p className="text-sm font-medium text-gray-600 dark:text-gray-400">Balance</p>
                <p className="text-2xl font-bold text-red-600">{formatCurrency(monthlyBalance)}</p>
              </div>
            </div>
          </Card>
        </div>

        <Card>
          <div className="p-6">
            <h3 className="text-base font-semibold text-gray-900 dark:text-white mb-4">Submit Yesterday's Deposit</h3>
            <form onSubmit={handleManagerDepositSubmit} className="flex flex-col sm:flex-row gap-3 max-w-lg">
              <input
                type="number"
                min="0"
                step="0.01"
                value={managerDepositInput}
                onChange={e => setManagerDepositInput(e.target.value)}
                placeholder="Deposit amount (Ksh)"
                className="flex-1 border border-gray-300 rounded-lg px-3 py-2 text-sm dark:bg-gray-800 dark:border-gray-600 dark:text-white"
                required
              />
              <Button type="submit" disabled={managerSubmitting}>
                {managerSubmitting ? 'Submitting...' : 'Submit Deposit'}
              </Button>
            </form>
            {managerSubmitError && <p className="text-red-600 text-sm mt-2">{managerSubmitError}</p>}
            {managerSubmitSuccess && <p className="text-green-600 text-sm mt-2">{managerSubmitSuccess}</p>}
          </div>
        </Card>

        <Card>
          <div className="p-6">
            <h3 className="text-base font-semibold text-gray-900 dark:text-white mb-4">Monthly Records</h3>
            {managerCodLoading ? (
              <div className="text-center py-8 text-gray-500">Loading...</div>
            ) : managerDeposits.length === 0 ? (
              <div className="text-center py-8 text-gray-500">No records for this month.</div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-gray-200 dark:border-gray-700">
                      <th className="text-left py-3 px-4 font-medium text-gray-600 dark:text-gray-400">Date</th>
                      <th className="text-right py-3 px-4 font-medium text-gray-600 dark:text-gray-400">COD Total</th>
                      <th className="text-right py-3 px-4 font-medium text-gray-600 dark:text-gray-400">Deposited</th>
                      <th className="text-right py-3 px-4 font-medium text-gray-600 dark:text-gray-400">Running Debt</th>
                      <th className="text-center py-3 px-4 font-medium text-gray-600 dark:text-gray-400">Status</th>
                    </tr>
                  </thead>
                  <tbody>
                    {managerDeposits.map((d: any) => (
                      <tr key={d.id} className="border-b border-gray-100 dark:border-gray-800 hover:bg-gray-50 dark:hover:bg-gray-800/50">
                        <td className="py-3 px-4 text-gray-900 dark:text-white">
                          {new Date(d.date).toLocaleDateString('en-KE', { day: '2-digit', month: 'short', year: 'numeric' })}
                        </td>
                        <td className="py-3 px-4 text-right text-gray-700 dark:text-gray-300">{formatCurrency(d.codTotal)}</td>
                        <td className="py-3 px-4 text-right text-green-600 dark:text-green-400">{formatCurrency(d.depositAmount)}</td>
                        <td className="py-3 px-4 text-right text-red-600 dark:text-red-400">{formatCurrency(d.runningDebt ?? (d.codTotal - d.depositAmount))}</td>
                        <td className="py-3 px-4 text-center">
                          <span className={`inline-block px-2 py-0.5 rounded-full text-xs font-medium ${
                            d.status === 'approved' ? 'bg-green-100 text-green-700' :
                            d.status === 'rejected' ? 'bg-red-100 text-red-700' :
                            'bg-yellow-100 text-yellow-700'
                          }`}>
                            {d.status ?? 'pending'}
                          </span>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </Card>
      </div>
    );
  };

  const renderBranchDebit = () => {
    const branchSummaries = Object.values(branchDebitData);

    if (branchSummaries.length === 0) {
      return (
        <Card>
          <div className="p-6 text-center">
            <DollarSign className="w-12 h-12 text-gray-400 mx-auto mb-4" />
            <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">
              No COD Data Found
            </h3>
            <p className="text-gray-500 dark:text-gray-400">
              No COD or Paid parcels found for the selected time period.
            </p>
          </div>
        </Card>
      );
    }

    // Set initial active tab if not set
    if (!activeBranchTab && branchSummaries.length > 0) {
      setActiveBranchTab(branchSummaries[0].branch);
    }

    const activeBranchSummary = branchSummaries.find(b => b.branch === activeBranchTab);

    return (
      <div className="space-y-6">
        {/* Summary Cards - Hidden for Accountants */}
        {!isAccountant() && (
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <Card>
              <div className="flex items-center">
                <div className="p-3 rounded-lg bg-blue-100 dark:bg-blue-500/20">
                  <DollarSign className="w-6 h-6 text-blue-600 dark:text-blue-400" />
                </div>
                <div className="ml-4">
                  <p className="text-sm font-medium text-gray-600 dark:text-gray-400">
                    Total COD/Paid Amount
                  </p>
                  <p className="text-2xl font-bold text-gray-900 dark:text-white">
                    {formatCurrency(branchSummaries.reduce((sum, b) => sum + b.totalCodAmount, 0))}
                  </p>
                </div>
              </div>
            </Card>

            <Card>
              <div className="flex items-center">
                <div className="p-3 rounded-lg bg-green-100 dark:bg-green-500/20">
                  <CheckCircle className="w-6 h-6 text-green-600 dark:text-green-400" />
                </div>
                <div className="ml-4">
                  <p className="text-sm font-medium text-gray-600 dark:text-gray-400">
                    Total Deposited
                  </p>
                  <p className="text-2xl font-bold text-gray-900 dark:text-white">
                    {formatCurrency(branchSummaries.reduce((sum, b) => sum + b.totalDeposited, 0))}
                  </p>
                </div>
              </div>
            </Card>

            <Card>
              <div className="flex items-center">
                <div className="p-3 rounded-lg bg-red-100 dark:bg-red-500/20">
                  <AlertCircle className="w-6 h-6 text-red-600 dark:text-red-400" />
                </div>
                <div className="ml-4">
                  <p className="text-sm font-medium text-gray-600 dark:text-gray-400">
                    Total Outstanding Debt
                  </p>
                  <p className="text-2xl font-bold text-gray-900 dark:text-white">
                    {formatCurrency(branchSummaries.reduce((sum, b) => sum + b.totalDebt, 0))}
                  </p>
                </div>
              </div>
            </Card>
          </div>
        )}

        {/* Branch Tabs */}
        <Card>
          <div className="p-6">
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
              Branch Details
            </h3>

            {/* Tab Navigation */}
            <div className="border-b border-gray-200 dark:border-gray-700 mb-6">
              <nav className="-mb-px flex space-x-8 overflow-x-auto">
                {branchSummaries.map((branchSummary) => {
                  const isActive = activeBranchTab === branchSummary.branch;
                  const isNairobiPaids = branchSummary.branch === 'Nairobi-Paids';

                  return (
                    <button
                      key={branchSummary.branch}
                      onClick={() => setActiveBranchTab(branchSummary.branch)}
                      className={`whitespace-nowrap py-2 px-1 border-b-2 font-medium text-sm transition-colors ${isActive
                        ? 'border-brand-500 text-brand-600 dark:text-brand-400'
                        : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300 dark:text-gray-400 dark:hover:text-gray-300'
                        }`}
                    >
                      <div className="flex items-center gap-2">
                        {isNairobiPaids ? (
                          <CheckCircle className="w-4 h-4" />
                        ) : (
                          <MapPin className="w-4 h-4" />
                        )}
                        <span>{branchSummary.branch}</span>
                        <Badge
                          variant={branchSummary.totalDebt > 0 ? 'error' : 'success'}
                          size="sm"
                        >
                          {formatCurrency(Math.abs(branchSummary.totalDebt))}
                        </Badge>
                      </div>
                    </button>
                  );
                })}
              </nav>
            </div>

            {/* Loading State */}
            {loadingDeposits && (
              <div className="flex items-center justify-center py-8">
                <RefreshCw className="w-6 h-6 animate-spin mr-2" />
                <span>Loading deposit data...</span>
              </div>
            )}

            {/* Active Tab Content */}
            {activeBranchSummary && !loadingDeposits && (
              <div className="space-y-6">
                {/* Branch Summary Header */}
                <div className="flex items-center justify-between p-4 bg-gray-50 dark:bg-gray-800 rounded-lg">
                  <div className="flex items-center gap-6">
                    <div className="text-center">
                      <div className="text-sm text-gray-600 dark:text-gray-400">
                        {activeBranchSummary.branch === 'Nairobi-Paids' ? 'Total Paid' : 'Total COD'}
                      </div>
                      <div className="text-lg font-semibold text-blue-600">
                        {formatCurrency(activeBranchSummary.totalCodAmount)}
                      </div>
                    </div>
                    <div className="text-center">
                      <div className="text-sm text-gray-600 dark:text-gray-400">Deposited</div>
                      <div className="text-lg font-semibold text-green-600">
                        {formatCurrency(activeBranchSummary.totalDeposited)}
                      </div>
                    </div>
                    {activeBranchSummary.totalExpenses > 0 && (
                      <div className="text-center">
                        <div className="text-sm text-gray-600 dark:text-gray-400">Approved Expenses</div>
                        <div className="text-lg font-semibold text-orange-600">
                          -{formatCurrency(activeBranchSummary.totalExpenses)}
                        </div>
                      </div>
                    )}
                    <div className="text-center">
                      <div className="text-sm text-gray-600 dark:text-gray-400">
                        {activeBranchSummary.branch === 'Nairobi-Paids' ? 'Balance' : 'Debt'}
                      </div>
                      <div className={`text-lg font-semibold ${activeBranchSummary.totalDebt > 0 ? 'text-red-600' : 'text-green-600'}`}>
                        {formatCurrency(Math.abs(activeBranchSummary.totalDebt))}
                        {activeBranchSummary.totalDebt < 0 && ' (Credit)'}
                      </div>
                    </div>
                  </div>
                  <Button
                    variant="outline"
                    onClick={() => downloadBranchPDF(activeBranchSummary)}
                  >
                    <Download className="w-4 h-4 mr-2" />
                    Download PDF
                  </Button>
                </div>

                {/* Transactions Table */}
                <div className="overflow-hidden border border-gray-200 dark:border-gray-700 rounded-lg">
                  <Table>
                    <Table.Header>
                      <Table.Row>
                        <Table.Head>Date</Table.Head>
                        <Table.Head>{activeBranchSummary.branch === 'Nairobi-Paids' ? 'Paid Total' : 'COD Total'}</Table.Head>
                        <Table.Head>Deposited Amount</Table.Head>
                        <Table.Head>{activeBranchSummary.branch === 'Nairobi-Paids' ? 'Daily Balance' : 'Daily Debt'}</Table.Head>
                        <Table.Head>Actions</Table.Head>
                      </Table.Row>
                    </Table.Header>
                    <Table.Body>
                      {activeBranchSummary.transactions.map((transaction, index) => (
                        <Table.Row key={index}>
                          <Table.Cell>
                            <div className="text-gray-900 dark:text-white">
                              {formatDate(transaction.date)}
                            </div>
                          </Table.Cell>
                          <Table.Cell>{formatCurrency(transaction.codTotal)}</Table.Cell>
                          <Table.Cell>
                            <div className="flex items-center gap-2">
                              {formatCurrency(transaction.depositedAmount)}
                              {transaction.depositedAmount === 0 && (
                                <Badge variant="warning" size="sm">Not Deposited</Badge>
                              )}
                              {transaction.id && (
                                <Badge variant="success" size="sm">Saved</Badge>
                              )}
                            </div>
                          </Table.Cell>
                          <Table.Cell>
                            {(() => {
                              // Calculate actual daily debt: COD Total - Deposited Amount for this day only
                              const dailyDebt = transaction.codTotal - transaction.depositedAmount;
                              return (
                                <Badge variant={dailyDebt > 0 ? 'error' : 'success'}>
                                  {formatCurrency(Math.abs(dailyDebt))}
                                  {dailyDebt < 0 && ' (Credit)'}
                                </Badge>
                              );
                            })()}
                          </Table.Cell>
                          <Table.Cell>
                            <div className="flex items-center gap-2 flex-wrap">
                              <Button
                                variant="outline"
                                size="sm"
                                onClick={() => openDepositModal(transaction)}
                              >
                                <DollarSign className="w-4 h-4 mr-1" />
                                Update
                              </Button>
                              {transaction.id && !isAccountant() && (
                                <>
                                  {transaction.status !== 'approved' && (
                                    <Button
                                      size="sm"
                                      variant="outline"
                                      disabled={approvingDepositId === transaction.id}
                                      onClick={async () => {
                                        if (!transaction.id) return;
                                        setApprovingDepositId(transaction.id);
                                        try {
                                          await wmsApi.approveBranchDeposit(transaction.id);
                                          await fetchReportData();
                                        } finally {
                                          setApprovingDepositId(null);
                                        }
                                      }}
                                      className="text-green-700 border-green-400 hover:bg-green-50"
                                    >
                                      {approvingDepositId === transaction.id ? '...' : 'Approve'}
                                    </Button>
                                  )}
                                  {transaction.status !== 'rejected' && (
                                    <Button
                                      size="sm"
                                      variant="outline"
                                      onClick={() => {
                                        setRejectDepositId(transaction.id!);
                                        setRejectReason('');
                                        setShowRejectModal(true);
                                      }}
                                      className="text-red-700 border-red-400 hover:bg-red-50"
                                    >
                                      Reject
                                    </Button>
                                  )}
                                  {transaction.status && (
                                    <span className={`inline-block px-2 py-0.5 rounded-full text-xs font-medium ${
                                      transaction.status === 'approved' ? 'bg-green-100 text-green-700' :
                                      transaction.status === 'rejected' ? 'bg-red-100 text-red-700' :
                                      'bg-yellow-100 text-yellow-700'
                                    }`}>{transaction.status}</span>
                                  )}
                                </>
                              )}
                            </div>
                          </Table.Cell>
                        </Table.Row>
                      ))}
                    </Table.Body>
                  </Table>
                </div>
              </div>
            )}
          </div>
        </Card>

        {/* Reject Deposit Modal */}
        {showRejectModal && rejectDepositId && (
          <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
            <div className="bg-white dark:bg-gray-800 rounded-lg p-6 w-full max-w-md">
              <div className="flex items-center justify-between mb-4">
                <h3 className="text-lg font-semibold text-gray-900 dark:text-white">Reject Deposit</h3>
                <button onClick={() => setShowRejectModal(false)} className="text-gray-400 hover:text-gray-600">
                  <X className="w-5 h-5" />
                </button>
              </div>
              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                    Rejection Reason
                  </label>
                  <textarea
                    value={rejectReason}
                    onChange={e => setRejectReason(e.target.value)}
                    rows={3}
                    placeholder="Enter reason for rejection..."
                    className="w-full border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm dark:bg-gray-700 dark:text-white focus:outline-none focus:ring-2 focus:ring-red-500"
                  />
                </div>
                <div className="flex gap-3 justify-end">
                  <Button variant="outline" onClick={() => setShowRejectModal(false)}>Cancel</Button>
                  <Button
                    onClick={async () => {
                      if (!rejectDepositId) return;
                      try {
                        await wmsApi.rejectBranchDeposit(rejectDepositId, rejectReason);
                        setShowRejectModal(false);
                        await fetchReportData();
                      } catch (err) {
                        console.error('Failed to reject deposit', err);
                      }
                    }}
                    className="bg-red-600 text-white hover:bg-red-700"
                  >
                    Confirm Rejection
                  </Button>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Deposit Modal */}
        {showDepositModal && selectedBranchDebit && (
          <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
            <div className="bg-white dark:bg-gray-800 rounded-lg p-6 w-full max-w-md">
              <div className="flex items-center justify-between mb-4">
                <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
                  Update Deposit Amount
                </h3>
                <button
                  onClick={() => setShowDepositModal(false)}
                  className="text-gray-400 hover:text-gray-600"
                >
                  <X className="w-5 h-5" />
                </button>
              </div>

              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                    Branch: {selectedBranchDebit.branch}
                  </label>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                    Date: {formatDate(selectedBranchDebit.date)}
                  </label>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                    {selectedBranchDebit.branch === 'Nairobi-Paids' ? 'Paid' : 'COD'} Total: {formatCurrency(selectedBranchDebit.codTotal)}
                  </label>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                    Deposit Amount
                  </label>
                  <input
                    type="number"
                    value={depositAmount}
                    onChange={(e) => setDepositAmount(e.target.value)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                    placeholder="Enter deposit amount"
                    min="0"
                    step="0.01"
                  />
                </div>

                <div className="flex items-center justify-end gap-3 pt-4">
                  <Button
                    variant="outline"
                    onClick={() => setShowDepositModal(false)}
                  >
                    Cancel
                  </Button>
                  <Button
                    variant="primary"
                    onClick={handleDepositSubmit}
                    disabled={savingDeposit}
                  >
                    {savingDeposit ? (
                      <>
                        <RefreshCw className="w-4 h-4 mr-2 animate-spin" />
                        Saving...
                      </>
                    ) : selectedBranchDebit?.id ? (
                      'Update Deposit'
                    ) : (
                      'Create Deposit'
                    )}
                  </Button>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    );
  };

  const renderClerkDebit = () => {
    if (clerkDebitData.length === 0) {
      return (
        <Card>
          <div className="p-6 text-center">
            <User className="w-12 h-12 text-gray-400 mx-auto mb-4" />
            <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">
              No Clerk Debit Data Found
            </h3>
            <p className="text-gray-500 dark:text-gray-400">
              No paid parcels found for the selected date range.
            </p>
          </div>
        </Card>
      );
    }

    const totalDebit = clerkDebitData.reduce((sum, clerk) => sum + clerk.currentDebit, 0);
    const totalCollected = clerkDebitData.reduce((sum, clerk) => sum + clerk.totalPaidAmount, 0);
    const totalDeposited = clerkDebitData.reduce((sum, clerk) => sum + clerk.totalDeposited, 0);
    const totalExpenses = clerkDebitData.reduce((sum, clerk) => sum + clerk.totalExpenses, 0);

    return (
      <div className="space-y-6">
        {/* Date Range Controls */}
        <Card>
          <div className="p-4">
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
              Filter by Date Range
            </h3>
            <div className="flex items-center gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  From Date
                </label>
                <input
                  type="date"
                  value={debitDateRange.from}
                  onChange={(e) => setDebitDateRange(prev => ({ ...prev, from: e.target.value }))}
                  className="px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg focus:ring-2 focus:ring-brand-500 focus:border-brand-500 bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  To Date
                </label>
                <input
                  type="date"
                  value={debitDateRange.to}
                  onChange={(e) => setDebitDateRange(prev => ({ ...prev, to: e.target.value }))}
                  className="px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg focus:ring-2 focus:ring-brand-500 focus:border-brand-500 bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
                />
              </div>
              <Button
                variant="primary"
                onClick={() => {
                  console.log('[Clerk Debit Debug] Manual refresh clicked');
                  fetchClerkDebitData(debitDateRange).then(data => {
                    console.log('[Clerk Debit Debug] Manual refresh complete, setting data:', data.length, 'clerks');
                    setClerkDebitData(data);
                  });
                }}
                disabled={loading}
              >
                <RefreshCw className={`w-4 h-4 mr-2 ${loading ? 'animate-spin' : ''}`} />
                Update Data
              </Button>
            </div>
          </div>
        </Card>

        {/* Summary Cards - Hidden for Accountants */}
        {!isAccountant() && (
          <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
            <Card>
              <div className="flex items-center">
                <div className="p-3 rounded-lg bg-blue-100 dark:bg-blue-500/20">
                  <DollarSign className="w-6 h-6 text-blue-600 dark:text-blue-400" />
                </div>
                <div className="ml-4">
                  <p className="text-sm font-medium text-gray-600 dark:text-gray-400">
                    Total Collected
                  </p>
                  <p className="text-2xl font-bold text-gray-900 dark:text-white">
                    {formatCurrency(totalCollected)}
                  </p>
                </div>
              </div>
            </Card>
            <Card>
              <div className="flex items-center">
                <div className="p-3 rounded-lg bg-green-100 dark:bg-green-500/20">
                  <CheckCircle className="w-6 h-6 text-green-600 dark:text-green-400" />
                </div>
                <div className="ml-4">
                  <p className="text-sm font-medium text-gray-600 dark:text-gray-400">
                    Total Deposited
                  </p>
                  <p className="text-2xl font-bold text-gray-900 dark:text-white">
                    {formatCurrency(totalDeposited)}
                  </p>
                </div>
              </div>
            </Card>
            <Card>
              <div className="flex items-center">
                <div className="p-3 rounded-lg bg-orange-100 dark:bg-orange-500/20">
                  <AlertCircle className="w-6 h-6 text-orange-600 dark:text-orange-400" />
                </div>
                <div className="ml-4">
                  <p className="text-sm font-medium text-gray-600 dark:text-gray-400">
                    Total Expenses
                  </p>
                  <p className="text-2xl font-bold text-gray-900 dark:text-white">
                    {formatCurrency(totalExpenses)}
                  </p>
                </div>
              </div>
            </Card>
            <Card>
              <div className="flex items-center">
                <div className="p-3 rounded-lg bg-red-100 dark:bg-red-500/20">
                  <User className="w-6 h-6 text-red-600 dark:text-red-400" />
                </div>
                <div className="ml-4">
                  <p className="text-sm font-medium text-gray-600 dark:text-gray-400">
                    Total Debit Owed
                  </p>
                  <p className="text-2xl font-bold text-gray-900 dark:text-white">
                    {formatCurrency(totalDebit)}
                  </p>
                </div>
              </div>
            </Card>
          </div>
        )}

        {/* Clerk Debit Table */}
        <Card>
          <Card.Header>
            <div className="flex items-center justify-between">
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
                Clerk Debit Summary ({debitDateRange.from} to {debitDateRange.to})
              </h3>
              <div className="text-sm text-gray-500">
                {clerkDebitData.length} clerk(s) with debit
              </div>
            </div>
          </Card.Header>

          <div className="overflow-x-auto">
            <Table>
              <Table.Header>
                <Table.Row>
                  <Table.Head>Clerk</Table.Head>
                  <Table.Head>Parcels</Table.Head>
                  <Table.Head>Total Collected</Table.Head>
                  <Table.Head>Total Deposited</Table.Head>
                  <Table.Head>Total Expenses</Table.Head>
                  <Table.Head>Current Debit</Table.Head>
                  <Table.Head>Actions</Table.Head>
                </Table.Row>
              </Table.Header>
              <Table.Body>
                {clerkDebitData.map((clerk) => (
                  <Table.Row key={clerk.clerkId}>
                    <Table.Cell>
                      <div>
                        <div className="font-medium text-gray-900 dark:text-white">
                          {clerk.clerkName}
                        </div>
                        <div className="text-sm text-gray-500">
                          @{clerk.clerkUsername}
                        </div>
                      </div>
                    </Table.Cell>
                    <Table.Cell>
                      <Badge variant="gray" size="sm">
                        {clerk.parcelCount}
                      </Badge>
                    </Table.Cell>
                    <Table.Cell>
                      <span className="font-medium">
                        {formatCurrency(clerk.totalPaidAmount)}
                      </span>
                    </Table.Cell>
                    <Table.Cell>
                      <span className="text-green-600 font-medium">
                        {formatCurrency(clerk.totalDeposited)}
                      </span>
                    </Table.Cell>
                    <Table.Cell>
                      <span className="text-orange-600 font-medium">
                        {formatCurrency(clerk.totalExpenses)}
                      </span>
                    </Table.Cell>
                    <Table.Cell>
                      <div className="flex items-center gap-2">
                        <span className={`font-bold ${clerk.currentDebit > 0 ? 'text-red-600' : 'text-green-600'
                          }`}>
                          {formatCurrency(Math.abs(clerk.currentDebit))}
                        </span>
                        {clerk.currentDebit < 0 && (
                          <Badge variant="success" size="sm">Credit</Badge>
                        )}
                        {clerk.currentDebit > 0 && (
                          <Badge variant="error" size="sm">Debit</Badge>
                        )}
                      </div>
                    </Table.Cell>
                    <Table.Cell>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => {
                          setSelectedDebitClerk(clerk);
                          setShowDebitDetailsModal(true);
                        }}
                      >
                        <Eye className="w-4 h-4 mr-1" />
                        View Details
                      </Button>
                    </Table.Cell>
                  </Table.Row>
                ))}
              </Table.Body>
            </Table>
          </div>
        </Card>

        {/* Debit Details Modal */}
        {showDebitDetailsModal && selectedDebitClerk && (
          <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
            <div className="bg-white dark:bg-gray-800 rounded-lg shadow-lg w-full max-w-6xl max-h-[95vh] overflow-hidden flex flex-col">
              {/* Modal Header */}
              <div className="flex items-center justify-between p-4 border-b border-gray-200 dark:border-gray-700">
                <h2 className="text-xl font-semibold text-gray-900 dark:text-white">
                  Debit Details: {selectedDebitClerk.clerkName}
                </h2>
                <button
                  onClick={() => {
                    setShowDebitDetailsModal(false);
                    setSelectedDebitClerk(null);
                  }}
                  className="p-1.5 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg transition-colors"
                >
                  <X className="w-5 h-5 text-gray-500" />
                </button>
              </div>

              {/* Modal Content */}
              <div className="p-4 overflow-y-auto flex-1">
                {/* Summary */}
                <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-6">
                  <div className="bg-blue-50 dark:bg-blue-900/20 p-4 rounded-lg">
                    <div className="text-sm text-blue-600 dark:text-blue-400">Total Collected</div>
                    <div className="text-lg font-bold text-blue-700 dark:text-blue-300">
                      {formatCurrency(selectedDebitClerk.totalPaidAmount)}
                    </div>
                  </div>
                  <div className="bg-green-50 dark:bg-green-900/20 p-4 rounded-lg">
                    <div className="text-sm text-green-600 dark:text-green-400">Total Deposited</div>
                    <div className="text-lg font-bold text-green-700 dark:text-green-300">
                      {formatCurrency(selectedDebitClerk.totalDeposited)}
                    </div>
                  </div>
                  <div className="bg-orange-50 dark:bg-orange-900/20 p-4 rounded-lg">
                    <div className="text-sm text-orange-600 dark:text-orange-400">Approved Expenses</div>
                    <div className="text-lg font-bold text-orange-700 dark:text-orange-300">
                      {formatCurrency(selectedDebitClerk.totalExpenses)}
                    </div>
                    <div className="text-xs text-orange-500 mt-1">Deducted from debit</div>
                  </div>
                  <div className="bg-red-50 dark:bg-red-900/20 p-4 rounded-lg">
                    <div className="text-sm text-red-600 dark:text-red-400">Current Debit</div>
                    <div className="text-lg font-bold text-red-700 dark:text-red-300">
                      {formatCurrency(Math.abs(selectedDebitClerk.currentDebit))}
                      {selectedDebitClerk.currentDebit < 0 ? ' (Credit)' : ''}
                    </div>
                  </div>
                </div>

                {/* Transactions + Expenses Table interleaved by date */}
                <div className="overflow-x-auto">
                  <Table>
                    <Table.Header>
                      <Table.Row>
                        <Table.Head>Date</Table.Head>
                        <Table.Head>Type</Table.Head>
                        <Table.Head>Reference / Description</Table.Head>
                        <Table.Head>Destination</Table.Head>
                        <Table.Head>Paid Amount</Table.Head>
                        <Table.Head>Deposited</Table.Head>
                        <Table.Head>Expense</Table.Head>
                        <Table.Head>Net Debit</Table.Head>
                      </Table.Row>
                    </Table.Header>
                    <Table.Body>
                      {(() => {
                        type Row =
                          | { kind: 'parcel'; date: string; data: ClerkDebitTransaction }
                          | { kind: 'expense'; date: string; data: DailyExpense };

                        const rows: Row[] = [
                          ...selectedDebitClerk.transactions.map(t => ({
                            kind: 'parcel' as const,
                            date: t.date,
                            data: t
                          })),
                          ...(selectedDebitClerk.approvedExpenseItems || []).map(e => ({
                            kind: 'expense' as const,
                            date: e.date.split('T')[0],
                            data: e
                          }))
                        ].sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime());

                        return rows.map((row, idx) => {
                          if (row.kind === 'parcel') {
                            const t = row.data as ClerkDebitTransaction;
                            return (
                              <Table.Row key={`p-${t.parcelId}`}>
                                <Table.Cell>
                                  <div className="text-sm text-gray-900 dark:text-white">
                                    {fmtDate(t.date)}
                                  </div>
                                </Table.Cell>
                                <Table.Cell>
                                  <Badge variant="primary" size="sm">Parcel</Badge>
                                </Table.Cell>
                                <Table.Cell>
                                  <div className="font-medium text-gray-900 dark:text-white">{t.waybillNumber}</div>
                                  <div className="text-xs text-gray-500">{t.sender} → {t.receiver}</div>
                                </Table.Cell>
                                <Table.Cell>
                                  <div className="text-sm text-gray-600 dark:text-gray-400">{t.destination}</div>
                                </Table.Cell>
                                <Table.Cell>
                                  <span className="font-medium">{formatCurrency(t.paidAmount)}</span>
                                </Table.Cell>
                                <Table.Cell>
                                  <span className="text-green-600 font-medium">{formatCurrency(t.depositedAmount)}</span>
                                </Table.Cell>
                                <Table.Cell>—</Table.Cell>
                                <Table.Cell>
                                  <span className={`font-bold ${t.netDebit > 0 ? 'text-red-600' : 'text-green-600'}`}>
                                    {formatCurrency(Math.abs(t.netDebit))}
                                    {t.netDebit < 0 ? ' (Cr)' : ''}
                                  </span>
                                </Table.Cell>
                              </Table.Row>
                            );
                          } else {
                            const e = row.data as DailyExpense;
                            return (
                              <Table.Row key={`e-${e.id}`} className="bg-orange-50 dark:bg-orange-900/10">
                                <Table.Cell>
                                  <div className="text-sm text-gray-900 dark:text-white">
                                    {fmtDate(e.date)}
                                  </div>
                                </Table.Cell>
                                <Table.Cell>
                                  <Badge variant="warning" size="sm">Expense</Badge>
                                </Table.Cell>
                                <Table.Cell>
                                  <div className="font-medium text-orange-700 dark:text-orange-300">{e.description}</div>
                                  <div className="text-xs text-gray-500 capitalize">{e.category.replace('_', ' ')}{e.vendor ? ` · ${e.vendor}` : ''}</div>
                                </Table.Cell>
                                <Table.Cell>—</Table.Cell>
                                <Table.Cell>—</Table.Cell>
                                <Table.Cell>—</Table.Cell>
                                <Table.Cell>
                                  <span className="text-orange-600 font-medium">-{formatCurrency(e.amount)}</span>
                                </Table.Cell>
                                <Table.Cell>—</Table.Cell>
                              </Table.Row>
                            );
                          }
                        });
                      })()}
                    </Table.Body>
                  </Table>
                </div>
              </div>

              {/* Modal Footer */}
              <div className="flex items-center justify-end p-4 border-t border-gray-200 dark:border-gray-700 space-x-3">
                <Button
                  variant="outline"
                  onClick={() => {
                    setShowDebitDetailsModal(false);
                    setSelectedDebitClerk(null);
                  }}
                >
                  Close
                </Button>
                <Button
                  variant="primary"
                  onClick={() => downloadClerkDebitPDF(selectedDebitClerk)}
                >
                  <Download className="w-4 h-4 mr-2" />
                  Export Details
                </Button>
              </div>
            </div>
          </div>
        )}
      </div>
    );
  };

  const getClerkName = (parcel: Parcel): string => {
    if (parcel.createdBy) {
      if (typeof parcel.createdBy === 'object') {
        return parcel.createdBy.username || parcel.createdBy.firstName || `User ${parcel.createdById}`;
      }
      return parcel.createdBy;
    }

    // Try to get from cache
    if (userCache.has(parcel.createdById)) {
      return userCache.get(parcel.createdById)!;
    }

    // Fetch user data in background and return fallback for now
    getUserDisplayName(parcel.createdById);
    return `User ${parcel.createdById}`;
  };

  // Virtual table columns for parcel list report
  const parcelListColumns = [
    {
      key: 'waybillNumber',
      header: 'Waybill #',
      width: 140,
      sortable: true,
      render: (parcel: Parcel) => (
        <div className="font-medium text-gray-900 dark:text-white">
          {parcel.waybillNumber}
        </div>
      )
    },
    {
      key: 'destination',
      header: 'Destination',
      width: 120,
      sortable: true,
      render: (parcel: Parcel) => (
        <div className="flex items-center text-gray-900 dark:text-white">
          <MapPin className="w-4 h-4 mr-1 text-gray-400" />
          {parcel.destination}
        </div>
      )
    },
    {
      key: 'sender',
      header: 'Sender',
      width: 160,
      sortable: true,
      render: (parcel: Parcel) => (
        <div>
          <div className="text-gray-900 dark:text-white">{parcel.sender}</div>
          <div className="text-sm text-gray-500">{parcel.senderTelephone}</div>
        </div>
      )
    },
    {
      key: 'receiver',
      header: 'Receiver',
      width: 160,
      sortable: true,
      render: (parcel: Parcel) => (
        <div>
          <div className="text-gray-900 dark:text-white">{parcel.receiver}</div>
          <div className="text-sm text-gray-500">{parcel.receiverTelephone}</div>
        </div>
      )
    },
    {
      key: 'quantity',
      header: 'Qty',
      width: 80,
      sortable: true,
      render: (parcel: Parcel) => (
        <span className="text-gray-900 dark:text-white">{parcel.quantity}</span>
      )
    },
    {
      key: 'description',
      header: 'Description',
      width: 180,
      sortable: true,
      render: (parcel: Parcel) => (
        <div className="text-gray-900 dark:text-white max-w-xs truncate" title={parcel.description}>
          {parcel.description || 'No description'}
        </div>
      )
    },
    {
      key: 'totalAmount',
      header: 'Amount',
      width: 120,
      sortable: true,
      render: (parcel: any) => (
        <span className="text-gray-900 dark:text-white">
          {parcel.formattedAmount || formatCurrency(parcel.totalAmount)}
        </span>
      )
    },
    {
      key: 'paymentMethods',
      header: 'Payment Method',
      width: 130,
      sortable: true,
      render: (parcel: Parcel) => (
        <Badge variant="gray">
          {parcel.paymentMethods || 'N/A'}
        </Badge>
      )
    },
    {
      key: 'status',
      header: 'Status',
      width: 120,
      sortable: true,
      render: (parcel: Parcel) => {
        const statusConfig = {
          0: { variant: 'warning', text: 'Pending' },
          1: { variant: 'info', text: 'Confirmed' },
          2: { variant: 'primary', text: 'In Transit' },
          3: { variant: 'success', text: 'Delivered' },
          4: { variant: 'error', text: 'Cancelled' }
        };
        const config = statusConfig[parcel.status as keyof typeof statusConfig] || { variant: 'secondary', text: 'Unknown' };
        return <Badge variant={config.variant as any}>{config.text}</Badge>;
      }
    },
    {
      key: 'createdBy',
      header: 'Clerk',
      width: 140,
      sortable: true,
      render: (parcel: any) => (
        <div className="text-gray-900 dark:text-white">
          {parcel.clerkDisplayName || getClerkName(parcel)}
        </div>
      )
    },
    {
      key: 'createdAt',
      header: 'Created',
      width: 140,
      sortable: true,
      render: (parcel: Parcel) => (
        <div>
          <div className="text-sm text-gray-500">
            {fmtDate(parcel.createdAt)}
          </div>
          <div className="text-sm text-gray-400">
            {fmtTime(parcel.createdAt)}
          </div>
        </div>
      )
    }
  ];

  const renderParcelList = () => {
    const getStatusText = (status: number) => {
      const statusMap = {
        0: 'Pending',
        1: 'Confirmed',
        2: 'In Transit',
        3: 'Delivered',
        4: 'Cancelled'
      };
      return statusMap[status as keyof typeof statusMap] || 'Unknown';
    };

    const getStatusBadge = (status: number) => {
      const statusConfig = {
        0: { variant: 'warning', text: 'Pending' },
        1: { variant: 'info', text: 'Confirmed' },
        2: { variant: 'primary', text: 'In Transit' },
        3: { variant: 'success', text: 'Delivered' },
        4: { variant: 'error', text: 'Cancelled' }
      };
      const config = statusConfig[status as keyof typeof statusConfig] || { variant: 'secondary', text: 'Unknown' };
      return <Badge variant={config.variant as any}>{config.text}</Badge>;
    };

    const printParcelList = () => {
      const printWindow = window.open('', '_blank');
      if (printWindow) {
        const filterInfo = [];
        if (parcelFilters.dateFrom) filterInfo.push(`From: ${parcelFilters.dateFrom}`);
        if (parcelFilters.dateTo) filterInfo.push(`To: ${parcelFilters.dateTo}`);
        if (parcelFilters.destination) filterInfo.push(`Destination: ${parcelFilters.destination}`);
        if (parcelFilters.status !== 'all') filterInfo.push(`Status: ${getStatusText(parseInt(parcelFilters.status))}`);

        printWindow.document.write(`
          <html>
            <head>
              <title>Parcel List Report</title>
              <style>
                body { font-family: Arial, sans-serif; padding: 20px; margin: 0; font-size: 12px; }
                h1 { color: #333; margin-bottom: 10px; }
                .filters { margin-bottom: 20px; color: #666; }
                table { width: 100%; border-collapse: collapse; margin-top: 20px; }
                th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
                th { background-color: #f5f5f5; font-weight: bold; }
                .status { padding: 2px 6px; border-radius: 3px; font-size: 10px; }
                .status-pending { background: #fef3c7; color: #92400e; }
                .status-confirmed { background: #dbeafe; color: #1e40af; }
                .status-transit { background: #e0e7ff; color: #3730a3; }
                .status-delivered { background: #d1fae5; color: #065f46; }
                .status-cancelled { background: #fee2e2; color: #991b1b; }
                @media print { body { print-color-adjust: exact; } }
              </style>
            </head>
            <body>
              <h1>Parcel List Report</h1>
              <div class="filters">Filters Applied: ${filterInfo.length > 0 ? filterInfo.join(', ') : 'None'}</div>
              <div>Total Parcels: ${filteredParcels.length}</div>
              <table>
                <thead>
                  <tr>
                    <th>Waybill Number</th>
                    <th>Destination</th>
                    <th>Sender</th>
                    <th>Receiver</th>
                    <th>Quantity</th>
                    <th>Description</th>
                    <th>Amount</th>
                    <th>Status</th>
                    <th>Clerk</th>
                    <th>Created Date</th>
                  </tr>
                </thead>
                <tbody>
                  ${filteredParcels.map(parcel => `
                    <tr>
                      <td>${parcel.waybillNumber}</td>
                      <td>${parcel.destination}</td>
                      <td>${parcel.sender}</td>
                      <td>${parcel.receiver}</td>
                      <td>${parcel.quantity}</td>
                      <td>${parcel.description || 'No description'}</td>
                      <td>KSh ${parcel.totalAmount.toLocaleString()}</td>
                      <td><span class="status status-${getStatusText(parcel.status).toLowerCase()}">${getStatusText(parcel.status)}</span></td>
                      <td>${getClerkName(parcel)}</td>
                      <td>${fmtDate(parcel.createdAt)}</td>
                    </tr>
                  `).join('')}
                </tbody>
              </table>
            </body>
          </html>
        `);
        printWindow.document.close();
        printWindow.print();
      }
    };

    const parcelFilterFields = [
      {
        key: 'dateFrom',
        label: 'Date From',
        type: 'date' as const,
        placeholder: 'Select start date'
      },
      {
        key: 'dateTo',
        label: 'Date To',
        type: 'date' as const,
        placeholder: 'Select end date'
      },
      {
        key: 'destination',
        label: 'Destination',
        type: 'select' as const,
        placeholder: 'Select destination',
        options: [
          { value: '', label: 'All Destinations' },
          ...destinations.map(dest => ({ value: dest, label: dest }))
        ]
      },
      {
        key: 'status',
        label: 'Status',
        type: 'select' as const,
        placeholder: 'Filter by status',
        options: [
          { value: 'all', label: 'All Status' },
          { value: '0', label: 'Pending' },
          { value: '1', label: 'Confirmed' },
          { value: '2', label: 'In Transit' },
          { value: '3', label: 'Delivered' },
          { value: '4', label: 'Cancelled' }
        ]
      },
      {
        key: 'paymentMethod',
        label: 'Payment Method',
        type: 'text' as const,
        placeholder: 'Filter by payment method',
        icon: <DollarSign className="w-4 h-4" />
      }
    ];

    const getActiveParcelFilterChips = () => {
      const chips: any[] = [];

      if (parcelFilters.dateFrom) {
        chips.push({
          key: 'dateFrom',
          label: 'Date From',
          value: parcelFilters.dateFrom,
          onRemove: () => handleParcelFilterChange('dateFrom', '')
        });
      }

      if (parcelFilters.dateTo) {
        chips.push({
          key: 'dateTo',
          label: 'Date To',
          value: parcelFilters.dateTo,
          onRemove: () => handleParcelFilterChange('dateTo', '')
        });
      }

      if (parcelFilters.destination) {
        chips.push({
          key: 'destination',
          label: 'Destination',
          value: parcelFilters.destination,
          onRemove: () => handleParcelFilterChange('destination', '')
        });
      }

      if (parcelFilters.status !== 'all') {
        chips.push({
          key: 'status',
          label: 'Status',
          value: getStatusText(parseInt(parcelFilters.status)),
          onRemove: () => handleParcelFilterChange('status', 'all')
        });
      }

      if (parcelFilters.paymentMethod) {
        chips.push({
          key: 'paymentMethod',
          label: 'Payment Method',
          value: parcelFilters.paymentMethod,
          onRemove: () => handleParcelFilterChange('paymentMethod', '')
        });
      }

      return chips;
    };

    return (
      <div className="space-y-6">
        {/* Filters */}
        <Card>
          <div className="p-6">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
                Parcel List Filters
              </h3>
              <div className="flex items-center gap-2">
                <Button
                  variant="outline"
                  onClick={() => setShowParcelFilters(!showParcelFilters)}
                >
                  <Filter className="w-4 h-4 mr-2" />
                  {showParcelFilters ? 'Hide' : 'Show'} Filters
                </Button>
                {filteredParcels.length > 0 && (
                  <Button
                    variant="outline"
                    onClick={printParcelList}
                  >
                    <Printer className="w-4 h-4 mr-2" />
                    Print List
                  </Button>
                )}
              </div>
            </div>

            {showParcelFilters && (
              <FilterPanel
                fields={parcelFilterFields}
                filters={parcelFilters as any}
                onFilterChange={handleParcelFilterChange}
                onClearFilters={clearParcelFilters}
                activeFilters={getActiveParcelFilterChips()}
                collapsible={false}
                defaultExpanded={true}
              />
            )}

            {!parcelFilters.dateFrom && !parcelFilters.dateTo && !parcelFilters.destination && (
              <div className="mt-4 p-4 bg-blue-50 border border-blue-200 rounded-lg">
                <div className="flex items-center gap-2 text-blue-800">
                  <AlertCircle className="w-5 h-5" />
                  <span className="font-medium">Apply filters to load parcels</span>
                </div>
                <p className="text-blue-600 mt-1 text-sm">
                  Please select a date range or destination to fetch and display parcels. This helps avoid loading too much data at once.
                </p>
              </div>
            )}
          </div>
        </Card>

        {/* Parcel List */}
        {(parcelFilters.dateFrom || parcelFilters.dateTo || parcelFilters.destination) && (
          <Card padding={false}>
            <div className="p-6 border-b border-gray-200">
              <div className="flex items-center justify-between">
                <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
                  Parcel List ({enhancedFilteredParcels.length} parcels)
                </h3>
                {parcelListLoading && (
                  <div className="flex items-center gap-2 text-gray-500">
                    <RefreshCw className="w-4 h-4 animate-spin" />
                    Loading...
                  </div>
                )}
              </div>
            </div>

            {enhancedFilteredParcels.length === 0 && !parcelListLoading ? (
              <div className="p-6 text-center">
                <Package className="w-12 h-12 text-gray-400 mx-auto mb-4" />
                <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">
                  No parcels found
                </h3>
                <p className="text-gray-500 dark:text-gray-400">
                  No parcels match your current filter criteria.
                </p>
              </div>
            ) : (
              <VirtualScrollTable
                data={enhancedFilteredParcels}
                columns={parcelListColumns}
                rowHeight={64}
                containerHeight={600}
                loading={parcelListLoading}
                emptyMessage="No parcels found matching your filter criteria"
                onRowClick={(parcel) => {
                  // Optional: Add row click functionality if needed
                }}
              />
            )}
          </Card>
        )}
      </div>
    );
  };

  const renderReportContent = () => {
    switch (currentReport) {
      case 'dashboard':
        return renderDashboard();
      case 'sales-per-clerk':
        return renderSalesPerClerk();
      case 'delivery-rate':
        return renderDeliveryRate();
      case 'undelivered-parcels':
        return renderUndeliveredParcels();
      case 'cod-delivered':
        return renderBranchDebit();
      case 'clerk-debit':
        return renderClerkDebit();
      case 'parcel-list':
        return renderParcelList();
      case 'branch-cod-report':
        return renderBranchManagerCodReport();
      default:
        return (
          <Card>
            <div className="p-6 text-center">
              <FileText className="w-12 h-12 text-gray-400 mx-auto mb-4" />
              <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">
                Report Not Available
              </h3>
              <p className="text-gray-500 dark:text-gray-400">
                This report type is not yet implemented.
              </p>
            </div>
          </Card>
        );
    }
  };

  const reportTypes = [
    ...(isBranchManager() ? [{ key: 'branch-cod-report', label: 'COD Report', icon: DollarSign }] : []),
    ...(!isBranchManager() ? [
      { key: 'dashboard', label: 'Dashboard', icon: BarChart },
      { key: 'sales-per-clerk', label: 'Sales per Clerk', icon: Users },
      { key: 'delivery-rate', label: 'Delivery Rate', icon: CheckCircle },
      { key: 'undelivered-parcels', label: 'Undelivered Parcels', icon: AlertCircle },
      { key: 'contract-invoices', label: 'Contract Invoices', icon: FileText },
      { key: 'cod-delivered', label: 'Branch debit', icon: DollarSign },
      { key: 'clerk-debit', label: 'Clerk Debit Management', icon: User },
      { key: 'parcel-list', label: 'Parcel List', icon: Package },
    ] : [])
  ];

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-96">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-brand-500"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">
            Reports & Analytics
          </h1>
          <p className="text-gray-600 dark:text-gray-400 mt-1">
            Comprehensive business intelligence and reporting
          </p>
        </div>
        <div className="flex items-center gap-3">
          <Button
            variant="outline"
            onClick={() => setShowFilters(!showFilters)}
          >
            <Filter className="w-4 h-4 mr-2" />
            Filters
          </Button>
          <div className="flex items-center gap-2">
            {cachedData && (
              <div className="flex items-center gap-1 text-xs text-gray-500 dark:text-gray-400">
                <Clock className="w-3 h-3" />
                <span>Data: {getDataAge()}</span>
                {isDataFresh() && (
                  <Badge variant="success" size="sm">Fresh</Badge>
                )}
                {!isDataFresh() && (
                  <Badge variant="warning" size="sm">Cached</Badge>
                )}
              </div>
            )}
            <Button
              variant="outline"
              onClick={() => invalidateCache(filters)}
              disabled={loading || cacheLoading}
              size="sm"
            >
              Clear Cache
            </Button>
            <Button
              variant="outline"
              onClick={() => fetchReportData(true)}
              disabled={loading || cacheLoading}
              size="sm"
              title="Load data progressively with live updates"
            >
              Progressive Load
            </Button>
            <Button
              variant="primary"
              onClick={() => fetchReportData(false, true)} // Force refresh on manual refresh
              disabled={loading || cacheLoading}
            >
              <RefreshCw className={`w-4 h-4 mr-2 ${(loading || cacheLoading) ? 'animate-spin' : ''}`} />
              Refresh
            </Button>
          </div>
        </div>
      </div>

      {/* Error Message */}
      {(error || cacheError) && (
        <div className="bg-error-50 border border-error-200 text-error-800 px-4 py-3 rounded-lg flex items-center gap-2">
          <AlertCircle className="w-5 h-5" />
          {error || cacheError}
        </div>
      )}

      {/* Loading Progress Indicator */}
      {(loading || cacheLoading) && getLoadProgress() && (
        <div className="bg-blue-50 border border-blue-200 p-4 rounded-lg">
          <ProgressBar
            progress={getLoadProgress()!.percentage}
            label={`Loading data (${getLoadProgress()!.loaded.toLocaleString()} of ${getLoadProgress()!.total.toLocaleString()} records)`}
            size="md"
            className="mb-2"
          />
          <div className="text-sm text-blue-600">
            This may take a moment for large datasets. Data will appear as it loads.
          </div>
        </div>
      )}

      {/* Filters */}
      {showFilters && (
        <FilterPanel
          fields={filterFields}
          filters={filters as any}
          onFilterChange={handleFilterChange}
          onClearFilters={clearFilters}
          activeFilters={getActiveFilterChips()}
          collapsible={true}
          defaultExpanded={true}
        />
      )}

      {/* Report Content */}
      <DataLoadingState
        isLoading={loading || cacheLoading}
        hasData={parcels.length > 0 || dispatches.length > 0}
        progress={getLoadProgress()}
        fallback={
          <div className="space-y-6">
            {currentReport === 'dashboard' ? (
              <>
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
                  {Array.from({ length: 4 }).map((_, i) => (
                    <SkeletonCard key={i} showTitle showSubtitle={false} lines={1} />
                  ))}
                </div>
                <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                  {Array.from({ length: 2 }).map((_, i) => (
                    <SkeletonCard key={i} showTitle lines={8} />
                  ))}
                </div>
              </>
            ) : currentReport === 'parcel-list' ? (
              <SkeletonTable rows={15} columns={8} />
            ) : (
              <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                <SkeletonCard showTitle lines={6} />
                <SkeletonTable rows={8} columns={4} />
              </div>
            )}
          </div>
        }
      >
        {renderReportContent()}
      </DataLoadingState>

      {/* Clerk Details Modal */}
      {showClerkDetailsModal && selectedClerk && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white dark:bg-gray-800 rounded-lg w-full max-w-6xl mx-4 max-h-[90vh] overflow-hidden">
            <div className="flex justify-between items-center p-6 border-b border-gray-200 dark:border-gray-700">
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
                Cash-in Details - {selectedClerk.clerkName}
              </h3>
              <button
                onClick={() => setShowClerkDetailsModal(false)}
                className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            {/* Clerk Summary */}
            <div className="p-6 border-b border-gray-200 dark:border-gray-700">
              <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
                <div className="bg-green-50 dark:bg-green-500/20 p-4 rounded-lg">
                  <div className="text-sm font-medium text-green-600 dark:text-green-400">Total Collected</div>
                  <div className="text-xl font-bold text-green-900 dark:text-green-300">
                    {formatCurrency(selectedClerk.totalPaidAmount)}
                  </div>
                </div>
                <div className="bg-blue-50 dark:bg-blue-500/20 p-4 rounded-lg">
                  <div className="text-sm font-medium text-blue-600 dark:text-blue-400">Total Deposited</div>
                  <div className="text-xl font-bold text-blue-900 dark:text-blue-300">
                    {formatCurrency(selectedClerk.totalDeposited)}
                  </div>
                  <div className="text-xs text-blue-500 mt-1">Approved declarations only</div>
                </div>
                <div className="bg-orange-50 dark:bg-orange-500/20 p-4 rounded-lg">
                  <div className="text-sm font-medium text-orange-600 dark:text-orange-400">Approved Expenses</div>
                  <div className="text-xl font-bold text-orange-900 dark:text-orange-300">
                    {formatCurrency(selectedClerk.totalExpenses)}
                  </div>
                  <div className="text-xs text-orange-500 mt-1">Deducted from debt</div>
                </div>
                <div className="bg-red-50 dark:bg-red-500/20 p-4 rounded-lg">
                  <div className="text-sm font-medium text-red-600 dark:text-red-400">Outstanding Debt</div>
                  <div className={`text-xl font-bold ${selectedClerk.remainingDebt > 0 ? 'text-red-900 dark:text-red-300' :
                    selectedClerk.remainingDebt < 0 ? 'text-green-900 dark:text-green-300' :
                      'text-gray-900 dark:text-gray-300'
                    }`}>
                    {formatCurrency(Math.abs(selectedClerk.remainingDebt))}
                    {selectedClerk.remainingDebt < 0 ? ' (Credit)' : ''}
                  </div>
                </div>
              </div>
            </div>

            {/* Parcels Table */}
            <div className="overflow-auto max-h-96">
              <Table>
                <Table.Header>
                  <Table.Row>
                    <Table.Head>Waybill</Table.Head>
                    <Table.Head>Sender</Table.Head>
                    <Table.Head>Receiver</Table.Head>
                    <Table.Head>Destination</Table.Head>
                    <Table.Head>Amount</Table.Head>
                    <Table.Head>Deposited</Table.Head>
                    <Table.Head>Expenses</Table.Head>
                    <Table.Head>Net</Table.Head>
                    <Table.Head>Actions</Table.Head>
                  </Table.Row>
                </Table.Header>
                <Table.Body>
                  {selectedClerk.parcels.map((parcel: ClerkParcelData, index: number) => (
                    <Table.Row key={index}>
                      <Table.Cell>
                        <div className="font-medium text-gray-900 dark:text-white">
                          {parcel.waybillNumber}
                        </div>
                        <div className="text-xs text-gray-500 dark:text-gray-400">
                          {fmtDate(parcel.createdAt)}
                        </div>
                      </Table.Cell>
                      <Table.Cell>{parcel.sender}</Table.Cell>
                      <Table.Cell>{parcel.receiver}</Table.Cell>
                      <Table.Cell>{parcel.destination}</Table.Cell>
                      <Table.Cell>{formatCurrency(parcel.amount)}</Table.Cell>
                      <Table.Cell>
                        <span className={parcel.depositedAmount > 0 ? 'text-green-600 font-medium' : ''}>
                          {formatCurrency(parcel.depositedAmount)}
                        </span>
                      </Table.Cell>
                      <Table.Cell>
                        <span className={parcel.expenses > 0 ? 'text-orange-600 font-medium' : ''}>
                          {formatCurrency(parcel.expenses)}
                        </span>
                      </Table.Cell>
                      <Table.Cell>
                        <span className={`font-medium ${(parcel.amount - parcel.depositedAmount - parcel.expenses) > 0 ? 'text-red-600' : 'text-green-600'
                          }`}>
                          {formatCurrency(parcel.amount - parcel.depositedAmount - parcel.expenses)}
                        </span>
                      </Table.Cell>
                      <Table.Cell>
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => openParcelModal(parcel)}
                        >
                          <Edit className="w-4 h-4 mr-1" />
                          Edit
                        </Button>
                      </Table.Cell>
                    </Table.Row>
                  ))}
                </Table.Body>
              </Table>
            </div>

            {/* Clerk Deposit Declarations */}
            {selectedClerk.clerkDeposits.length > 0 && (
              <div className="px-6 py-4 border-t border-gray-200 dark:border-gray-700">
                <h4 className="text-sm font-semibold text-gray-900 dark:text-white mb-3">
                  Deposit Declarations (from Android app)
                </h4>
                <div className="space-y-2">
                  {selectedClerk.clerkDeposits.map(dep => (
                    <div key={dep.id} className="flex items-center justify-between p-3 rounded-lg bg-gray-50 dark:bg-gray-700">
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2">
                          <span className="text-sm font-medium text-gray-900 dark:text-white">
                            {formatCurrency(dep.amount)}
                          </span>
                          <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${
                            dep.status === 'approved'
                              ? 'bg-green-100 text-green-700 dark:bg-green-900/40 dark:text-green-400'
                              : dep.status === 'rejected'
                              ? 'bg-red-100 text-red-700 dark:bg-red-900/40 dark:text-red-400'
                              : 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/40 dark:text-yellow-400'
                          }`}>
                            {dep.status}
                          </span>
                        </div>
                        <div className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
                          {dep.description} &bull; {new Date(dep.date).toLocaleDateString()}
                        </div>
                        {dep.status === 'rejected' && dep.rejectionReason && (
                          <div className="text-xs text-red-600 dark:text-red-400 mt-0.5">
                            Reason: {dep.rejectionReason}
                          </div>
                        )}
                      </div>
                      {dep.status === 'pending' && (
                        <div className="flex gap-2 ml-4 shrink-0">
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => handleClerkDepositApproval(dep.id, true)}
                            disabled={approvingDepositId === dep.id}
                          >
                            <CheckCircle className="w-4 h-4 mr-1 text-green-600" />
                            Approve
                          </Button>
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={async () => {
                              const reason = prompt('Rejection reason:');
                              if (reason !== null) handleClerkDepositApproval(dep.id, false, reason);
                            }}
                            disabled={approvingDepositId === dep.id}
                          >
                            <XCircle className="w-4 h-4 mr-1 text-red-600" />
                            Reject
                          </Button>
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              </div>
            )}

            <div className="p-6 border-t border-gray-200 dark:border-gray-700">
              <div className="flex justify-end">
                <Button
                  variant="outline"
                  onClick={() => setShowClerkDetailsModal(false)}
                >
                  Close
                </Button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Parcel Update Modal */}
      {showParcelModal && selectedClerkParcel && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white dark:bg-gray-800 rounded-lg p-6 w-full max-w-md mx-4">
            <div className="flex justify-between items-center mb-4">
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
                Update Parcel - {selectedClerkParcel.waybillNumber}
              </h3>
              <button
                onClick={() => setShowParcelModal(false)}
                className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <div className="space-y-4">
              <div className="bg-gray-50 dark:bg-gray-700 p-4 rounded-lg">
                <div className="text-sm text-gray-600 dark:text-gray-400">Parcel Details</div>
                <div className="text-sm font-medium text-gray-900 dark:text-white">
                  {selectedClerkParcel.sender} → {selectedClerkParcel.receiver}
                </div>
                <div className="text-sm text-gray-600 dark:text-gray-400">
                  Destination: {selectedClerkParcel.destination}
                </div>
                <div className="text-sm font-medium text-green-600">
                  Total Amount: {formatCurrency(selectedClerkParcel.amount)}
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  Deposited Amount
                </label>
                <input
                  type="number"
                  value={parcelDepositAmount}
                  onChange={(e) => setParcelDepositAmount(e.target.value)}
                  min="0"
                  step="0.01"
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  placeholder="Enter deposited amount"
                />
              </div>

              {selectedClerkParcel.transactionCode && (
                <div className="bg-gray-50 dark:bg-gray-700 p-3 rounded-lg">
                  <div className="text-xs text-gray-500 dark:text-gray-400 mb-1">Transaction Code</div>
                  <div className="text-sm font-mono font-medium text-gray-900 dark:text-white">
                    {selectedClerkParcel.transactionCode}
                  </div>
                </div>
              )}

              <div className="bg-blue-50 dark:bg-blue-500/20 p-3 rounded-lg">
                <div className="text-sm text-blue-600 dark:text-blue-400">
                  Remaining: {formatCurrency(
                    selectedClerkParcel.amount - (parseFloat(parcelDepositAmount) || 0)
                  )}
                </div>
              </div>

              {error && (
                <div className="bg-red-50 dark:bg-red-500/20 border border-red-200 dark:border-red-500/30 p-3 rounded-lg">
                  <div className="text-red-600 dark:text-red-400 text-sm">{error}</div>
                </div>
              )}

              <div className="flex space-x-3 pt-4">
                <Button
                  variant="outline"
                  onClick={() => setShowParcelModal(false)}
                  className="flex-1"
                >
                  Cancel
                </Button>
                <Button
                  onClick={handleParcelUpdate}
                  disabled={updatingParcel}
                  className="flex-1 bg-blue-600 hover:bg-blue-700 text-white"
                >
                  {updatingParcel ? 'Updating...' : 'Update Parcel'}
                </Button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default Reports;