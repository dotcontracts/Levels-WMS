# WMS React Dispatch - Deployment Guide

## 📦 Production Build

### Prerequisites
- Node.js 16+ installed
- npm or yarn package manager
- Environment variables configured

### Build Steps

1. **Configure Production Environment**
   ```bash
   cd client
   
   # Verify production configuration
   cat .env.production
   # Should show:
   # REACT_APP_API_BASE_URL=https://levelsapi-82i8a.ondigitalocean.app
   # REACT_APP_API_TIMEOUT=30000
   ```

2. **Install Dependencies**
   ```bash
   npm install
   ```

3. **Build for Production**
   ```bash
   npm run build
   ```
   
   This creates an optimized production build in the `build/` folder.

4. **Test Production Build Locally**
   ```bash
   # Install serve if you don't have it
   npm install -g serve
   
   # Serve the production build
   npx serve -s build
   ```
   
   Visit http://localhost:3000 to test.

## 🚀 Deployment Options

### Option 1: Static Hosting (Netlify, Vercel, etc.)

**Netlify:**
```bash
# Install Netlify CLI
npm install -g netlify-cli

# Deploy
cd build
netlify deploy --prod
```

**Vercel:**
```bash
# Install Vercel CLI
npm install -g vercel

# Deploy from root
vercel --prod
```

### Option 2: AWS S3 + CloudFront

1. **Build the app**
   ```bash
   npm run build
   ```

2. **Upload to S3**
   ```bash
   aws s3 sync build/ s3://your-bucket-name --delete
   ```

3. **Invalidate CloudFront Cache**
   ```bash
   aws cloudfront create-invalidation --distribution-id YOUR_ID --paths "/*"
   ```

### Option 3: DigitalOcean App Platform

1. **Connect GitHub Repository**
   - Go to DigitalOcean App Platform
   - Connect your GitHub repository
   - Select the `wms_web.dispatch.react/client` directory

2. **Configure Build Settings**
   - Build Command: `npm run build`
   - Output Directory: `build`
   - Environment Variables:
     ```
     REACT_APP_API_BASE_URL=https://levelsapi-82i8a.ondigitalocean.app
     REACT_APP_API_TIMEOUT=30000
     ```

3. **Deploy**
   - Click "Deploy"
   - App will be available at: `https://your-app.ondigitalocean.app`

### Option 4: Docker Deployment

**Dockerfile:**
```dockerfile
FROM node:16-alpine as build

WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . ./
RUN npm run build

FROM nginx:alpine
COPY --from=build /app/build /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

**Build and Run:**
```bash
# Build image
docker build -t wms-react-dispatch .

# Run container
docker run -p 80:80 wms-react-dispatch
```

## 🔧 Environment Configuration

### Development
```env
REACT_APP_API_BASE_URL=/api
REACT_APP_API_TIMEOUT=15000
```

### Production
```env
REACT_APP_API_BASE_URL=https://levelsapi-82i8a.ondigitalocean.app
REACT_APP_API_TIMEOUT=30000
```

### Custom Deployment
```env
REACT_APP_API_BASE_URL=https://your-custom-api.com
REACT_APP_API_TIMEOUT=30000
```

## ✅ Post-Deployment Checklist

- [ ] Verify API connection in browser console
- [ ] Test login functionality
- [ ] Check all API endpoints work
- [ ] Verify authentication persists
- [ ] Test on multiple browsers
- [ ] Check mobile responsiveness
- [ ] Monitor for errors in console
- [ ] Verify HTTPS is working
- [ ] Test with different user roles

## 🐛 Troubleshooting Deployment

### Issue: White Screen After Deployment

**Cause**: Build path issues or environment variables not loaded

**Solution:**
1. Check browser console for errors
2. Verify environment variables are set correctly
3. Rebuild with correct configuration

### Issue: API Calls Failing

**Cause**: CORS or incorrect API URL

**Solution:**
1. Verify `REACT_APP_API_BASE_URL` is correct
2. Check that API server allows your domain
3. Verify API is accessible from deployment location

### Issue: 404 on Refresh

**Cause**: Single-page app routing not configured

**Solution - Nginx:**
```nginx
location / {
    try_files $uri $uri/ /index.html;
}
```

**Solution - Apache (.htaccess):**
```apache
<IfModule mod_rewrite.c>
  RewriteEngine On
  RewriteBase /
  RewriteRule ^index\.html$ - [L]
  RewriteCond %{REQUEST_FILENAME} !-f
  RewriteCond %{REQUEST_FILENAME} !-d
  RewriteRule . /index.html [L]
</IfModule>
```

## 📊 Performance Optimization

### 1. Enable Gzip Compression

**Nginx:**
```nginx
gzip on;
gzip_types text/plain text/css application/json application/javascript text/xml application/xml;
```

### 2. Set Cache Headers

**Nginx:**
```nginx
location /static {
    expires 1y;
    add_header Cache-Control "public, immutable";
}
```

### 3. Use CDN

Consider using a CDN like:
- CloudFlare
- AWS CloudFront
- Fastly

## 🔐 Security Best Practices

1. **Always use HTTPS** in production
2. **Set security headers** in your web server
3. **Keep dependencies updated**: `npm audit fix`
4. **Don't commit `.env` files** with secrets
5. **Use environment variables** for sensitive data
6. **Implement Content Security Policy (CSP)**

## 📝 CI/CD Pipeline Example

**GitHub Actions (.github/workflows/deploy.yml):**
```yaml
name: Deploy to Production

on:
  push:
    branches: [ main ]

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v2
    
    - name: Setup Node.js
      uses: actions/setup-node@v2
      with:
        node-version: '16'
    
    - name: Install dependencies
      run: |
        cd wms_web.dispatch.react/client
        npm ci
    
    - name: Build
      env:
        REACT_APP_API_BASE_URL: ${{ secrets.API_BASE_URL }}
        REACT_APP_API_TIMEOUT: 30000
      run: |
        cd wms_web.dispatch.react/client
        npm run build
    
    - name: Deploy to S3
      uses: jakejarvis/s3-sync-action@master
      with:
        args: --delete
      env:
        AWS_S3_BUCKET: ${{ secrets.AWS_S3_BUCKET }}
        AWS_ACCESS_KEY_ID: ${{ secrets.AWS_ACCESS_KEY_ID }}
        AWS_SECRET_ACCESS_KEY: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
        SOURCE_DIR: 'wms_web.dispatch.react/client/build'
```

## 🎯 Monitoring

### 1. Add Error Tracking

Consider integrating:
- Sentry
- LogRocket
- Rollbar

### 2. Analytics

Add analytics:
- Google Analytics
- Mixpanel
- Plausible

### 3. Performance Monitoring

Monitor with:
- Lighthouse CI
- Web Vitals
- New Relic

## 📞 Support

For deployment issues:
1. Check [API_CONFIGURATION.md](./client/API_CONFIGURATION.md)
2. Review [README_API.md](./client/README_API.md)
3. Check browser console for errors
4. Verify API server is running

## 🔄 Update Process

1. **Pull latest changes**
   ```bash
   git pull origin main
   ```

2. **Update dependencies**
   ```bash
   npm install
   ```

3. **Run tests** (if available)
   ```bash
   npm test
   ```

4. **Build and deploy**
   ```bash
   npm run build
   # Then deploy using your method
   ```

---

**Last Updated**: October 2025  
**Maintained by**: WMS Development Team


