# Quick Start: API Configuration

## 🚀 Quick Setup

### For Development
```bash
# The project is already configured!
npm start
```
Your app will use `/api` proxy to connect to: `https://levelsapi-82i8a.ondigitalocean.app`

### For Production Build
```bash
npm run build
```
The build will use: `https://levelsapi-82i8a.ondigitalocean.app`

## 📁 Configuration Files

- **`.env.development`** - Development settings (uses proxy)
- **`.env.production`** - Production settings (direct URL)
- **`.env.example`** - Template for reference

## 🔧 Customize API URL

### Option 1: Edit Environment File
```bash
# Edit .env.development
nano .env.development

# Change the URL
REACT_APP_API_BASE_URL=http://your-api-url.com
```

### Option 2: Environment Variable
```bash
# Set for current session
export REACT_APP_API_BASE_URL=http://your-api-url.com
npm start
```

## 🐛 Debugging

Check browser console after starting the app. You should see:
```
🔧 API Configuration: {
  baseUrl: "/api",
  timeout: 15000,
  environment: "development",
  usingProxy: true
}

🌐 WMS API Service initialized with: {
  apiBaseUrl: "/api",
  timeout: 15000,
  environment: "development"
}
```

## 📚 Full Documentation

See [API_CONFIGURATION.md](./API_CONFIGURATION.md) for complete documentation.

## ⚙️ Environment Variables

| Variable | Development | Production |
|----------|-------------|------------|
| `REACT_APP_API_BASE_URL` | `/api` | `https://levelsapi-82i8a.ondigitalocean.app` |
| `REACT_APP_API_TIMEOUT` | `15000` | `30000` |

## 🔐 How Authentication Works

1. User logs in via `/Auth/login`
2. Token is stored in `localStorage` as `token`
3. All subsequent requests include: `Authorization: Bearer {token}`
4. Token is managed automatically by `getAuthHeaders()` function

## ✅ What's Been Fixed

- ✅ Centralized API configuration
- ✅ Environment-based URLs (dev/prod)
- ✅ Configurable timeouts
- ✅ Automatic token injection
- ✅ Debug logging in development
- ✅ Proxy support for CORS
- ✅ All hardcoded values removed

## 🎯 Key Files

- `src/config/api.ts` - Configuration logic
- `src/services/wmsApi.ts` - API service
- `src/context/AuthContext.tsx` - Authentication
- `.env.development` - Dev config
- `.env.production` - Prod config


