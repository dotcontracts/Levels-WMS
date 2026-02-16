# API Configuration Guide

## Overview

The WMS React application uses environment-based configuration to manage API base URLs and timeouts for both development and production environments.

## Configuration Files

### 1. `.env.example`
Template file showing all available environment variables. Copy this file to create your own environment files.

### 2. `.env.development`
Used when running `npm start` (development mode).

**Default Configuration:**
```env
REACT_APP_API_BASE_URL=/api
REACT_APP_API_TIMEOUT=15000
```

This uses the proxy configured in `package.json` to avoid CORS issues during development.

### 3. `.env.production`
Used when running `npm run build` (production mode).

**Default Configuration:**
```env
REACT_APP_API_BASE_URL=https://levelsapi-82i8a.ondigitalocean.app
REACT_APP_API_TIMEOUT=30000
```

## Environment Variables

### `REACT_APP_API_BASE_URL`
The base URL for all API requests.

- **Development**: `/api` (uses proxy)
- **Production**: `https://levelsapi-82i8a.ondigitalocean.app`
- **Type**: String
- **Required**: No (has sensible defaults)

### `REACT_APP_API_TIMEOUT`
Timeout for API requests in milliseconds.

- **Development**: `15000` (15 seconds)
- **Production**: `30000` (30 seconds)
- **Type**: Number
- **Required**: No (defaults to 15000)

## How It Works

### 1. Configuration Loading (`src/config/api.ts`)

The application uses a centralized configuration system:

```typescript
const getApiBaseUrl = (): string => {
  const envUrl = process.env.REACT_APP_API_BASE_URL;
  
  if (envUrl) {
    return envUrl;
  }
  
  // Fallback: use proxy in development, production URL otherwise
  if (process.env.NODE_ENV === 'development') {
    return '/api';
  }
  
  return 'https://levelsapi-82i8a.ondigitalocean.app';
};
```

### 2. API Service (`src/services/wmsApi.ts`)

All API calls use the centralized configuration:

```typescript
import { API_CONFIG, getAuthHeaders } from '../config/api';

const response = await fetch(`${API_BASE_URL}/Parcels`, {
  headers: getAuthHeaders(),
  signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
});
```

### 3. Proxy Configuration (`package.json`)

For development, the proxy forwards `/api` requests to the backend:

```json
{
  "proxy": "https://levelsapi-82i8a.ondigitalocean.app"
}
```

## Setup Instructions

### For Development

1. **Using Proxy (Recommended)**
   ```bash
   # Copy the example file
   cp .env.example .env.development
   
   # Use the proxy configuration (default)
   # REACT_APP_API_BASE_URL=/api
   
   # Start development server
   npm start
   ```

2. **Using Direct URL**
   ```bash
   # Edit .env.development
   # Change to direct URL
   REACT_APP_API_BASE_URL=https://levelsapi-82i8a.ondigitalocean.app
   
   # Or use local backend
   REACT_APP_API_BASE_URL=http://localhost:5000
   
   # Start development server
   npm start
   ```

### For Production

1. **Build for Production**
   ```bash
   # Ensure .env.production is configured
   cat .env.production
   # Should contain:
   # REACT_APP_API_BASE_URL=https://levelsapi-82i8a.ondigitalocean.app
   # REACT_APP_API_TIMEOUT=30000
   
   # Build the application
   npm run build
   ```

2. **Deploy**
   ```bash
   # The build folder contains production-ready files
   # Deploy the contents of the 'build' folder to your web server
   ```

## Troubleshooting

### Issue: CORS Errors in Development

**Solution**: Use the proxy configuration
```env
# .env.development
REACT_APP_API_BASE_URL=/api
```

### Issue: API Timeout Errors

**Solution**: Increase the timeout value
```env
# .env.production
REACT_APP_API_TIMEOUT=60000  # 60 seconds
```

### Issue: Cannot Connect to API

1. **Check your environment file**
   ```bash
   cat .env.development  # or .env.production
   ```

2. **Verify API is running**
   ```bash
   curl https://levelsapi-82i8a.ondigitalocean.app/health
   ```

3. **Check browser console**
   - Open DevTools (F12)
   - Look for API configuration logs:
     ```
     🔧 API Configuration: {
       baseUrl: "/api",
       timeout: 15000,
       environment: "development",
       envVariable: "/api",
       usingProxy: true
     }
     ```

### Issue: Environment Variables Not Loading

**Solution**: Restart the development server
```bash
# Stop the server (Ctrl+C)
# Start it again
npm start
```

**Note**: Environment variables are loaded at build time. Changes require a restart.

## Advanced Configuration

### Custom API Endpoint

To use a different API endpoint:

```bash
# Development
export REACT_APP_API_BASE_URL=http://custom-api.example.com
npm start

# Production build
export REACT_APP_API_BASE_URL=https://production-api.example.com
npm run build
```

### Environment-Specific Builds

Create custom environment files:

```bash
# .env.staging
REACT_APP_API_BASE_URL=https://staging-api.example.com
REACT_APP_API_TIMEOUT=20000

# Build for staging
cp .env.staging .env.production
npm run build
```

## API Endpoints

All API requests are automatically prefixed with the base URL:

| Endpoint | Full URL (Production) |
|----------|----------------------|
| `/Auth/login` | `https://levelsapi-82i8a.ondigitalocean.app/Auth/login` |
| `/Parcels` | `https://levelsapi-82i8a.ondigitalocean.app/Parcels` |
| `/Dispatches` | `https://levelsapi-82i8a.ondigitalocean.app/Dispatches` |
| `/Users` | `https://levelsapi-82i8a.ondigitalocean.app/Users` |

## Security Notes

1. **Never commit `.env.development` or `.env.production`** with sensitive data
2. The `.env.example` file should only contain non-sensitive placeholders
3. API tokens are stored in localStorage and automatically included in requests
4. All production API calls should use HTTPS

## Testing Configuration

To verify your configuration is working:

1. **Development Mode**
   ```bash
   npm start
   ```
   - Check browser console for: `🔧 API Configuration:`
   - Try logging in
   - Check Network tab (F12) to see API requests

2. **Production Build**
   ```bash
   npm run build
   npx serve -s build
   ```
   - Visit http://localhost:3000
   - Check that API calls use the production URL
   - Verify functionality works as expected

## Summary

- ✅ Environment-based configuration for development and production
- ✅ Centralized API configuration in `src/config/api.ts`
- ✅ Proxy support for development to avoid CORS issues
- ✅ Configurable timeouts for different environments
- ✅ Automatic authentication token injection
- ✅ Debug logging in development mode
- ✅ Easy to customize and extend

For more information, see:
- `src/config/api.ts` - Configuration implementation
- `src/services/wmsApi.ts` - API service implementation
- `src/context/AuthContext.tsx` - Authentication context


