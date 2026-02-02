# 🔒 Security Configuration Guide

## ⚠️ CRITICAL SECURITY NOTICE

Environment and configuration files containing sensitive data have been removed from version control to prevent security vulnerabilities.

## 🚫 Files Removed from Git

The following files contained sensitive information and have been removed:

### API Configuration Files:
- `wms_android.api/appsettings.Production.json`
- `wms_android.api/appsettings.Development.json` 
- `wms_android.api/appsettings.Deployment.json`
- `wms_android.shared/appsettings.Production.json`
- `wms_android.shared/appsettings.Development.json`
- `wms_android.data/appsettings.Production.json`
- `wms_android.data/appsettings.Development.json`

### React Environment Files:
- `wms_web.dispatch.react/client/.env.production`
- `wms_web.dispatch.react/client/.env.development`

## 🔑 Sensitive Information Identified

These files contained:
- **Database connection strings** with credentials
- **JWT secrets** for token signing
- **API URLs** and endpoints
- **Database IP addresses** and ports

## 📋 Deployment Instructions

### 1. API Configuration

Copy the template file and configure with your environment variables:

```bash
# Copy template
cp wms_android.api/appsettings.Production.json.template wms_android.api/appsettings.Production.json

# Set environment variables or directly edit the file
```

**Required Environment Variables:**
```bash
DATABASE_CONNECTION_STRING="Host=YOUR_HOST;Port=5432;Database=YOUR_DB;Username=YOUR_USER;Password=YOUR_PASSWORD;SSL Mode=Prefer;Trust Server Certificate=true"
JWT_SECRET="your-secure-jwt-secret-key-here"
API_BASE_URL="https://wmsandroidapi-w74du.ondigitalocean.app"
FRONTEND_URL="https://wms-ficma-dispatch-l5z4y.ondigitalocean.app"
```

### 2. React Configuration

Copy the template and configure:

```bash
# Copy template
cp wms_web.dispatch.react/client/.env.production.template wms_web.dispatch.react/client/.env.production

# Edit with actual values
```

**Required Values:**
```bash
REACT_APP_API_BASE_URL=https://wmsandroidapi-w74du.ondigitalocean.app/api
REACT_APP_API_TIMEOUT=30000
NODE_ENV=production
```

### 3. Security Best Practices

1. **Never commit these files** to version control
2. **Use environment variables** in production deployment platforms
3. **Rotate secrets regularly** (JWT secrets, database passwords)
4. **Use secrets management** (Azure Key Vault, AWS Secrets Manager, etc.)
5. **Restrict database access** by IP whitelist
6. **Use strong passwords** for all credentials

### 4. Digital Ocean App Platform Deployment

For Digital Ocean, set environment variables in the App Platform dashboard:

1. Go to your app in Digital Ocean
2. Navigate to Settings → Environment Variables
3. Add the required variables:
   - `DATABASE_CONNECTION_STRING`
   - `JWT_SECRET`
   - `API_BASE_URL`
   - `FRONTEND_URL`

### 5. CORS Configuration

The API automatically configures CORS based on environment variables if no configuration file is present:

- `FRONTEND_URL`: Your frontend domain (e.g., `https://wms-ficma-dispatch-l5z4y.ondigitalocean.app`)
- `API_BASE_URL`: Your API domain (e.g., `https://wmsandroidapi-w74du.ondigitalocean.app`)

**Common CORS Errors:**
- `No 'Access-Control-Allow-Origin' header`: FRONTEND_URL not set correctly
- `CORS preflight request failed`: API not deployed with updated CORS settings

### 6. Generate New Secrets

**JWT Secret Generation:**
```bash
# Generate a secure 32-character secret
openssl rand -hex 32
```

**Database Password:**
- Use a strong password generator
- Include uppercase, lowercase, numbers, and symbols
- Minimum 16 characters

## 🔍 Security Checklist

- [ ] All sensitive files removed from Git history
- [ ] Environment variables configured in deployment platform
- [ ] New JWT secret generated and deployed
- [ ] Database password rotated
- [ ] CORS origins properly configured
- [ ] SSL/TLS enabled on all endpoints
- [ ] Database access restricted by IP
- [ ] Monitoring and logging configured

## 🆘 If Credentials Were Compromised

If the exposed credentials are still in use:

1. **Immediately rotate all passwords**
2. **Generate new JWT secrets**
3. **Review database access logs**
4. **Update all deployed applications**
5. **Monitor for suspicious activity**

## 📞 Support

For security concerns or deployment help, contact the development team.