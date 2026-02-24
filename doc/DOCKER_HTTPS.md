# Using HTTPS Only - Docker Setup

This setup uses .NET development certificates to enable HTTPS in local Docker development, matching production configuration.

## Overview

- **API**: HTTPS (port 44370) and HTTP (port 44369)
- **Frontend**: HTTP only (port 4200) - can call HTTPS API via CORS
- **Certificate**: .NET development certificate for `localhost`
- **Approach**: Export .NET dev cert from host and mount into Docker

**Note:** Angular runs on HTTP locally for simplicity but can call HTTPS API endpoints (CORS is configured to allow both).

## Quick Start

### 1. Create and Trust .NET Dev Certificate (One-time setup)

**Windows:**
```bash
dotnet dev-certs https --trust
```

**macOS:**
```bash
dotnet dev-certs https --trust
```

**Linux:**
```bash
dotnet dev-certs https
# Manually trust the certificate in your system
```

### 2. Export Dev Certificate for Docker

**Windows (PowerShell):**
```powershell
dotnet dev-certs https -ep $env:APPDATA\ASP.NET\https\aspnetapp.pfx -p YourCertPassword
```

**Windows (Command Prompt):**
```cmd
dotnet dev-certs https -ep %APPDATA%\ASP.NET\https\aspnetapp.pfx -p YourCertPassword
```

**macOS/Linux:**
```bash
dotnet dev-certs https -ep ~/ASP.NET/https/aspnetapp.pfx -p YourCertPassword
```

### 3. Set Certificate Password (Optional)

The certificate password is configured in `docker-compose.override.yml` for security (this file is not committed to git).
To use a custom password, either:

**Option 1 - Set environment variable (Recommended):**

**Windows PowerShell:**
```powershell
$env:ASPNETCORE_CERT_PASSWORD="YourCustomPassword"
docker-compose up
```

**Windows Command Prompt:**
```cmd
set ASPNETCORE_CERT_PASSWORD=YourCustomPassword
docker-compose up
```

**macOS/Linux:**
```bash
export ASPNETCORE_CERT_PASSWORD="YourCustomPassword"
docker-compose up
```

**Option 2 - Edit docker-compose.override.yml:**

The password is already in `docker-compose.override.yml`. You can change it there:
```yaml
services:
  api:
    environment:
      - ASPNETCORE_Kestrel__Certificates__Default__Password=YourCustomPassword
```

**Note:** `docker-compose.override.yml` is excluded from version control (see `.gitignore`), so your password won't be committed to the repository.

**Windows Command Prompt:**
```cmd
set ASPNETCORE_CERT_PASSWORD=YourCustomPassword
docker-compose up
```

**macOS/Linux:**
```bash
export ASPNETCORE_CERT_PASSWORD=YourCustomPassword
docker-compose up
```

### 4. Start Docker Compose

```bash
docker-compose up --build
```

Or for development with Angular:
```bash
docker-compose --profile backend-dev up --build
```

## Accessing Services

### API Endpoints

- **HTTP**: http://localhost:44369
- **HTTPS**: https://localhost:44370
- **Swagger**: https://localhost:44370/swagger
- **Health Checks**: https://localhost:44370/healthz (Headers["X-Health-Check-Key"])

### Frontend (when using backend-dev profile)

- **HTTP**: http://localhost:4200
- Angular can call HTTPS API: https://localhost:44370

### Other Services

- **Seq UI**: http://localhost:8081
- **Aspire Dashboard**: http://localhost:18888
- **Database**: localhost:1433
- **Redis**: localhost:6379

## CORS Configuration

The following origins are configured in `docker-compose.yml`:

- `http://localhost:4200` - Angular HTTP (localhost access)
- `http://angular:4200` - Angular HTTP (Docker network access)

**Note:** Angular always runs on HTTP port 4200 in the container. 
The API is HTTPS-enabled, and Angular can call it via HTTPS (CORS allows cross-protocol access).

## Troubleshooting

### Certificate Not Trusted

If you see "NET::ERR_CERT_AUTHORITY_INVALID":

**Windows:**
```bash
dotnet dev-certs https --trust
```

**macOS:**
```bash
dotnet dev-certs https --trust
```

Then restart Docker containers:
```bash
docker-compose down
docker-compose up
```

### Certificate Not Found

If API fails to start with "The specified file does not contain a valid certificate":

1. Verify certificate was exported:
   ```bash
   # Windows
   Test-Path $env:APPDATA\ASP.NET\https\aspnetapp.pfx
   
   # macOS/Linux
   test -f ~/ASP.NET/https/aspnetapp.pfx
   ```

2. Export certificate if missing (see Step 2 in Quick Start)

3. Verify Docker has access to the path:
   ```bash
   docker-compose config | grep -A 10 volumes
   ```

### Certificate Password Mismatch

If you see "The password provided for certificate file is incorrect":

1. Check your password environment variable:
   ```bash
   # Windows
   echo $env:ASPNETCORE_CERT_PASSWORD
   
   # macOS/Linux
   echo $ASPNETCORE_CERT_PASSWORD
   ```

2. Set password before starting Docker:
   ```bash
   # Windows
   $env:ASPNETCORE_CERT_PASSWORD="YourCertPassword123"
   
   # macOS/Linux
   export ASPNETCORE_CERT_PASSWORD="YourCertPassword123"
   ```

3. Ensure password matches what you used when exporting:
   ```bash
   # Re-export with same password
   dotnet dev-certs https -ep ~/ASP.NET/https/aspnetapp.pfx -p YourCertPassword123
   ```

### Port Already in Use

If ports 443, 44370, or 4200 are already in use:

1. Find the process using the port:
   ```bash
   # Windows
   netstat -ano | findstr :443
   
   # macOS/Linux
   lsof -i :443
   ```

2. Kill the process or change port mapping in `docker-compose.yml`

### Angular Can't Connect to HTTPS API

If Angular can't reach the HTTPS API:

1. Ensure .NET dev cert is trusted (see "Certificate Not Trusted" above)
2. Check that the API is running:
   ```bash
   docker-compose logs api
   ```

3. Verify CORS origins include your frontend URL
4. Check browser console for specific error messages
5. Test API directly:
   ```bash
   curl -k https://localhost:44370/swagger
   ```

### Docker Volume Mount Issues

If Docker can't access `~/ASP.NET/https`:

**Windows:**
1. Open Docker Desktop Settings
2. Go to Resources → File Sharing
3. Add your user profile directory (e.g., `C:\Users\YourName`)
4. Restart Docker Desktop

**macOS/Linux:**
1. Docker for Desktop should have access by default
2. If using Docker Machine, add volume in VirtualBox:
   ```bash
   docker-machine ssh
   sudo mkdir -p /hosthome/ASP.NET/https
   sudo mount -t vboxsf -o uid=1000,gid=1000 HostHome /hosthome
   ```

## Updating Certificate

If you need to recreate the certificate:

1. Remove existing certificate:
   ```bash
   dotnet dev-certs https --clean
   ```

2. Create new certificate:
   ```bash
   dotnet dev-certs https --trust
   ```

3. Export new certificate:
   ```bash
   dotnet dev-certs https -ep ~/ASP.NET/https/aspnetapp.pfx -p YourCertPassword123
   ```

4. Restart Docker containers:
   ```bash
   docker-compose down
   docker-compose up
   ```

## Production Deployment

For production, you'll want to use proper SSL certificates:

### Options:

1. **Let's Encrypt** (Free, automated)
   - Use certbot or cert-manager
   - Automatically renews certificates
   - Recommended for production

2. **Cloudflare** (Easy setup, includes SSL)
   - Cloudflare handles SSL termination
   - No certificate management needed
   - Good for web applications

3. **Paid Certificates** (DigiCert, Comodo, etc.)
   - Better support and warranties
   - Suitable for enterprise applications
   - Upload certificate to your hosting provider

4. **Reverse Proxy** (nginx, Traefik, Caddy)
   - Handles SSL termination
   - Can use Let's Encrypt automatically
   - Common in microservices architectures

### Production Environment Variables

In production, you'll need to update environment variables:

```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Production
  - ASPNETCORE_URLS=https://+:443
  - ASPNETCORE_Kestrel__Certificates__Default__Path=/path/to/prod/cert.pfx
  - ASPNETCORE_Kestrel__Certificates__Default__Password=${PROD_CERT_PASSWORD}
```

Or use certificate stores:
```yaml
environment:
  - ASPNETCORE_Kestrel__Certificates__Default__StoreName=My
  - ASPNETCORE_Kestrel__Certificates__Default__StoreLocation=LocalMachine
  - ASPNETCORE_Kestrel__Certificates__Default__Subject=CN=yourdomain.com
```

## Security Notes

⚠️ **Important:**

- The default password `YourCertPassword123` is for development only
- **Never use .NET dev certificates in production**
- Development certificates are not CA-signed
- Development certificates will show security warnings in browsers
- Always use proper CA-signed certificates in production

## Development Workflow

### First Time Setup:
```bash
# 1. Create and trust dev certificate
dotnet dev-certs https --trust

# 2. Export certificate for Docker
dotnet dev-certs https -ep ~/ASP.NET/https/aspnetapp.pfx -p YourCertPassword123

# 3. Start Docker
docker-compose up --build
```

### Daily Development:
```bash
docker-compose up
# The certificate is already trusted and exported
```

### When Certificate Expires (.NET dev certs expire after 1 year):
```bash
# 1. Clean old certificate
dotnet dev-certs https --clean

# 2. Create and trust new certificate
dotnet dev-certs https --trust

# 3. Export new certificate
dotnet dev-certs https -ep ~/ASP.NET/https/aspnetapp.pfx -p YourCertPassword123

# 4. Restart Docker
docker-compose down
docker-compose up
```

## Files Modified

- `docker-compose.yml` - Added HTTPS configuration with .NET dev cert mounting

## Additional Resources

- [ASP.NET Core HTTPS Configuration](https://docs.microsoft.com/en-us/aspnet/core/security/enforcing-ssl)
- [.NET Developer Certificates](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-dev-certs)
- [Docker Compose Networking](https://docs.docker.com/compose/networking/)
- [Kestrel HTTPS Configuration](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-8.0)

## Team Onboarding

For new team members, share these steps:

1. **Clone repository**
2. **Setup .NET dev certificate:**
   ```bash
   dotnet dev-certs https --trust
   dotnet dev-certs https -ep ~/ASP.NET/https/aspnetapp.pfx -p YourCertPassword
   ```
3. **Start Docker:**
   ```bash
   docker-compose up --build
   ```
4. **Access API at:** https://localhost:44370

That's it! No certificate files to track in version control, everything is handled by .NET CLI.
