// API Configuration for WMS React App
// This configuration uses environment variables for flexibility across environments

/**
 * Get the API Base URL based on environment
 * Priority:
 * 1. REACT_APP_API_BASE_URL environment variable (from .env files)
 * 2. Fallback to production URL
 */
const getApiBaseUrl = (): string => {
  const envUrl = process.env.REACT_APP_API_BASE_URL;
  
  if (envUrl) {
    return envUrl;
  }
  
  // Fallback: use localhost in development, production URL otherwise
  if (process.env.NODE_ENV === 'development') {
    return 'http://localhost:5053';  // Local .NET API
  }

  // Production API - includes /api path base
  return 'https://levelsapi-82i8a.ondigitalocean.app/api';  // Production URL
};

/**
 * Get API timeout from environment or use default
 */
const getApiTimeout = (): number => {
  const envTimeout = process.env.REACT_APP_API_TIMEOUT;
  return envTimeout ? parseInt(envTimeout, 10) : 30000; // Increased to 30 seconds for better reliability
};

export const API_CONFIG = {
  BASE_URL: getApiBaseUrl(),
  TIMEOUT: getApiTimeout(),
  HEADERS: {
    'Content-Type': 'application/json',
    'Accept': 'application/json'
  }
};

/**
 * Build a complete API URL from an endpoint
 * @param endpoint - API endpoint (e.g., 'Auth/login' or '/Parcels')
 * @returns Complete API URL
 */
export const buildApiUrl = (endpoint: string): string => {
  const baseUrl = API_CONFIG.BASE_URL.endsWith('/') 
    ? API_CONFIG.BASE_URL.slice(0, -1) 
    : API_CONFIG.BASE_URL;
  
  const cleanEndpoint = endpoint.startsWith('/') 
    ? endpoint.slice(1) 
    : endpoint;
  
  return `${baseUrl}/${cleanEndpoint}`;
};

/**
 * Get headers with authentication token and session ID if available
 * @returns Headers object with Content-Type, Accept, Authorization, and X-Session-ID
 */
export const getAuthHeaders = (): Record<string, string> => {
  const token = localStorage.getItem('token') || 
                localStorage.getItem('auth_token') || 
                sessionStorage.getItem('auth_token');
  
  const sessionId = localStorage.getItem('sessionId');
  
  const headers: Record<string, string> = { ...API_CONFIG.HEADERS };
  
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }
  
  if (sessionId) {
    headers['X-Session-ID'] = sessionId;
  }
  
  return headers;
};

/**
 * Check if endpoints should skip session validation
 * @param endpoint - API endpoint to check
 * @returns true if endpoint is public and doesn't need session headers
 */
export const isPublicEndpoint = (endpoint: string): boolean => {
  const publicEndpoints = [
    'auth/login',
    'session/create',
    'health',
    'swagger'
  ];
  
  const normalizedEndpoint = endpoint.toLowerCase();
  return publicEndpoints.some(pub => normalizedEndpoint.includes(pub));
};

/**
 * Create fetch wrapper that automatically handles sessions and retries on 401
 * @param url - Request URL
 * @param options - Fetch options
 * @returns Promise with fetch response
 */
export const sessionAwareFetch = async (url: string, options: RequestInit = {}): Promise<Response> => {
  // Add session headers for authenticated endpoints
  const headers: Record<string, string> = {
    ...getAuthHeaders(),
    ...(options.headers as Record<string, string> || {})
  };

  // Don't add session header for public endpoints
  if (isPublicEndpoint(url)) {
    delete headers['X-Session-ID'];
  }

  const fetchOptions: RequestInit = {
    ...options,
    headers
  };

  const response = await fetch(url, fetchOptions);
  
  // Handle session expiry
  if (response.status === 401 && !isPublicEndpoint(url)) {
    console.warn('Session expired - redirecting to login');
    
    // Clear session data
    localStorage.removeItem('token');
    localStorage.removeItem('sessionId');
    localStorage.removeItem('userId');
    localStorage.removeItem('user');
    localStorage.removeItem('sessionExpiresAt');
    
    // Redirect to login
    window.location.href = '/login';
  }

  return response;
};

