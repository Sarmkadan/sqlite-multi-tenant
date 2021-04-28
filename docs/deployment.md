# Deployment Guide

This guide covers production deployment of SQLite Multi-Tenant applications.

## Prerequisites

- .NET 8.0 Runtime or SDK
- SQLite 3
- Storage for databases and backups (local filesystem or network storage)
- Optional: Docker for containerized deployment

## Deployment Methods

### 1. Local Deployment

#### Publishing

```bash
# Build release configuration
dotnet build -c Release

# Publish as self-contained executable
dotnet publish -c Release -r linux-x64 --self-contained

# Output: bin/Release/net8.0/linux-x64/publish/
```

#### Directory Structure

```
/opt/sqlite-multi-tenant/
├── bin/
│   └── app executable
├── databases/
│   ├── master.db
│   ├── tenant1.db
│   └── ...
├── backups/
│   ├── backup1.db
│   └── ...
├── logs/
│   └── application.log
└── config/
    └── appsettings.json
```

#### appsettings.json

```json
{
  "SqliteMultiTenant": {
    "MaxConnections": 20,
    "ConnectionTimeoutSeconds": 30,
    "BackupRetentionDays": 30,
    "DatabaseDirectory": "/opt/sqlite-multi-tenant/databases",
    "BackupDirectory": "/opt/sqlite-multi-tenant/backups",
    "EnableEncryption": true,
    "EncryptionKey": "your-256-bit-min-encryption-key-here",
    "EnableLogging": true,
    "EnableAuditing": true,
    "CacheExpirationMinutes": 15,
    "AuditRetentionDays": 90
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "SqliteMultiTenant": "Information"
    }
  }
}
```

#### Systemd Service

Create `/etc/systemd/system/sqlite-multi-tenant.service`:

```ini
[Unit]
Description=SQLite Multi-Tenant Service
After=network.target

[Service]
Type=simple
User=www-data
WorkingDirectory=/opt/sqlite-multi-tenant
ExecStart=/opt/sqlite-multi-tenant/SqliteMultiTenant
Restart=on-failure
RestartSec=10

# Permissions
StandardOutput=journal
StandardError=journal
SyslogIdentifier=sqlite-multi-tenant

[Install]
WantedBy=multi-user.target
```

Enable and start:

```bash
sudo systemctl daemon-reload
sudo systemctl enable sqlite-multi-tenant
sudo systemctl start sqlite-multi-tenant
sudo systemctl status sqlite-multi-tenant
```

### 2. Docker Deployment

#### Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app

# Create directories
RUN mkdir -p databases backups logs

# Copy application
COPY --from=build /app/publish .

# Set permissions
RUN chmod -R 755 .

VOLUME ["/app/databases", "/app/backups", "/app/logs"]
EXPOSE 5000

ENTRYPOINT ["dotnet", "SqliteMultiTenant.dll"]
```

#### docker-compose.yml

```yaml
version: '3.8'

services:
  sqlite-multi-tenant:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: sqlite-multi-tenant
    restart: unless-stopped
    ports:
      - "5000:5000"
    volumes:
      - ./databases:/app/databases
      - ./backups:/app/backups
      - ./logs:/app/logs
      - ./config/appsettings.json:/app/appsettings.json:ro
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: http://+:5000
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"

  # Optional: Backup service
  backup-scheduler:
    image: mcr.microsoft.com/dotnet/runtime:8.0
    container_name: backup-scheduler
    depends_on:
      - sqlite-multi-tenant
    volumes:
      - ./backups:/backups
      - ./scripts:/scripts:ro
    entrypoint: /scripts/backup.sh
```

Build and run:

```bash
docker-compose up -d
docker-compose logs -f sqlite-multi-tenant
```

### 3. Kubernetes Deployment

#### Deployment YAML

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: sqlite-multi-tenant
spec:
  replicas: 2
  selector:
    matchLabels:
      app: sqlite-multi-tenant
  template:
    metadata:
      labels:
        app: sqlite-multi-tenant
    spec:
      containers:
      - name: app
        image: sqlite-multi-tenant:latest
        ports:
        - containerPort: 5000
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        volumeMounts:
        - name: databases
          mountPath: /app/databases
        - name: backups
          mountPath: /app/backups
        - name: config
          mountPath: /app/appsettings.json
          subPath: appsettings.json
        resources:
          requests:
            memory: "512Mi"
            cpu: "500m"
          limits:
            memory: "1Gi"
            cpu: "1000m"
        livenessProbe:
          httpGet:
            path: /api/admin/health
            port: 5000
          initialDelaySeconds: 30
          periodSeconds: 10
      volumes:
      - name: databases
        persistentVolumeClaim:
          claimName: databases-pvc
      - name: backups
        persistentVolumeClaim:
          claimName: backups-pvc
      - name: config
        configMap:
          name: app-config

---
apiVersion: v1
kind: Service
metadata:
  name: sqlite-multi-tenant-service
spec:
  selector:
    app: sqlite-multi-tenant
  ports:
  - protocol: TCP
    port: 80
    targetPort: 5000
  type: LoadBalancer
```

Deploy:

```bash
kubectl apply -f deployment.yaml
kubectl get pods
kubectl logs -f deployment/sqlite-multi-tenant
```

## Storage Configuration

### Local Storage

```bash
# Create directories
mkdir -p /var/sqlite-multi-tenant/databases
mkdir -p /var/sqlite-multi-tenant/backups
mkdir -p /var/sqlite-multi-tenant/logs

# Set permissions
chmod 750 /var/sqlite-multi-tenant
chown www-data:www-data /var/sqlite-multi-tenant
```

### Network Storage (NFS)

```bash
# Mount NFS
sudo mount -t nfs nfs-server:/export/sqlite-multi-tenant \
  /var/sqlite-multi-tenant

# Add to /etc/fstab for persistent mount
nfs-server:/export/sqlite-multi-tenant /var/sqlite-multi-tenant nfs defaults 0 0
```

### Cloud Storage (AWS S3)

```csharp
// Configure S3 backup destination
services.AddSingleton(new AmazonS3Client(
    new BasicAWSCredentials(accessKey, secretKey),
    RegionEndpoint.USEast1));

// Implement S3 backup handler
public class S3BackupHandler : IBackupHandler
{
    public async Task UploadBackupAsync(string backupPath, string tenantId)
    {
        var key = $"backups/{tenantId}/{Path.GetFileName(backupPath)}";
        var request = new PutObjectRequest
        {
            BucketName = "backups-bucket",
            Key = key,
            FilePath = backupPath
        };
        await _s3Client.PutObjectAsync(request);
    }
}
```

## Backup Strategy

### Automated Daily Backups

Create backup script `scripts/backup.sh`:

```bash
#!/bin/bash

BACKUP_DIR="/var/sqlite-multi-tenant/backups"
DATABASES_DIR="/var/sqlite-multi-tenant/databases"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)

# Create daily backup directory
mkdir -p "$BACKUP_DIR/daily/$TIMESTAMP"

# Backup each database
for db_file in "$DATABASES_DIR"/*.db; do
    if [ -f "$db_file" ]; then
        db_name=$(basename "$db_file")
        cp "$db_file" "$BACKUP_DIR/daily/$TIMESTAMP/$db_name"
        echo "Backed up: $db_name"
    fi
done

# Keep only last 7 days of backups
find "$BACKUP_DIR/daily" -mindepth 1 -maxdepth 1 -type d -mtime +7 -exec rm -rf {} \;

echo "Backup complete: $BACKUP_DIR/daily/$TIMESTAMP"
```

Schedule with cron:

```bash
# Edit crontab
crontab -e

# Add: Daily backup at 2 AM
0 2 * * * /scripts/backup.sh >> /var/log/backup.log 2>&1
```

### Backup Verification

```csharp
public async Task VerifyBackups()
{
    var backupService = serviceProvider.GetRequiredService<IBackupService>();
    
    // Get all backups created today
    var allBackups = await backupService.GetDatabaseBackupsAsync("all");
    var todayBackups = allBackups
        .Where(b => b.CreatedAt.Date == DateTime.Today)
        .ToList();
    
    foreach (var backup in todayBackups)
    {
        // Verify each backup
        try
        {
            await backupService.VerifyBackupAsync(backup.BackupId, "system");
            Console.WriteLine($"✓ Verified: {backup.BackupId}");
        }
        catch (BackupException ex)
        {
            Console.WriteLine($"✗ Failed: {backup.BackupId} - {ex.Message}");
            // Alert on failure
        }
    }
}
```

## Monitoring & Alerting

### Health Checks

```csharp
app.MapGet("/api/admin/health", async (IHealthCheckService healthService) =>
{
    var health = await healthService.CheckHealthAsync();
    
    return new
    {
        status = health.Status,
        timestamp = DateTime.UtcNow,
        uptime = health.UptimeMinutes,
        tenants = health.ActiveTenants,
        databases = health.DatabaseCount,
        message = health.Message
    };
});
```

Monitor health endpoint:

```bash
# Simple monitoring script
while true; do
    response=$(curl -s http://localhost:5000/api/admin/health)
    echo "$(date): $response"
    sleep 300  # Check every 5 minutes
done
```

### Logging

Configure structured logging:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "SqliteMultiTenant": "Debug"
    },
    "Console": {
      "IncludeScopes": true,
      "TimestampFormat": "yyyy-MM-dd HH:mm:ss"
    }
  }
}
```

View logs:

```bash
# Live logs
tail -f /var/log/sqlite-multi-tenant/app.log

# Search for errors
grep ERROR /var/log/sqlite-multi-tenant/app.log | tail -100
```

### Metrics

```csharp
// Get system metrics
var metricsService = serviceProvider.GetRequiredService<MetricsService>();
var metrics = metricsService.GetMetrics();

Console.WriteLine($"Total Requests: {metrics.TotalRequests}");
Console.WriteLine($"Avg Response Time: {metrics.AverageResponseTime}ms");
Console.WriteLine($"Error Rate: {metrics.ErrorRate:P}");
Console.WriteLine($"Cache Hit Rate: {metrics.CacheHitRate:P}");
```

## Security Checklist

- [ ] Enable HTTPS with valid certificate
- [ ] Configure firewall rules (only allow API port)
- [ ] Enable AES-256 encryption for sensitive data
- [ ] Use strong encryption keys (min 32 bytes)
- [ ] Enable audit logging for compliance
- [ ] Set backup retention policies
- [ ] Configure rate limiting (100 req/sec default)
- [ ] Regular security patching of .NET runtime
- [ ] Monitor audit logs for suspicious activity

## Performance Tuning

### Connection Pooling

```csharp
options.MaxConnections = 50;      // Increase for high concurrency
options.ConnectionTimeoutSeconds = 60;  // Increase if timeout errors
```

### Caching

```csharp
options.EnableCaching = true;
options.CacheExpirationMinutes = 30;  // Longer TTL for stable data
options.MaxCacheItems = 5000;         // More items if memory available
```

### Batch Operations

```csharp
options.EnableBatchOperations = true;
options.BatchSize = 500;              // Larger batches for throughput
options.MaxDegreeOfParallelism = Environment.ProcessorCount;
```

## Disaster Recovery

### Database Corruption

```bash
# Verify database integrity
sqlite3 databases/tenant1.db "PRAGMA integrity_check;"

# If corrupted, restore from backup
cp backups/tenant1_backup.db databases/tenant1.db
```

### Full System Restore

```bash
# 1. Stop application
sudo systemctl stop sqlite-multi-tenant

# 2. Restore databases from backup
rm -rf /var/sqlite-multi-tenant/databases
cp -r /backups/latest/databases/* /var/sqlite-multi-tenant/databases

# 3. Verify restore
sqlite3 /var/sqlite-multi-tenant/databases/master.db \
  "SELECT COUNT(*) FROM Tenants;"

# 4. Restart application
sudo systemctl start sqlite-multi-tenant
```

## Scaling Guidelines

### When to Scale Horizontally

- API requests > 1000 req/sec
- Multiple tenants (> 100)
- Geographic distribution needed
- High availability required

### When to Scale Vertically

- Database queries slow
- Cache hit rate < 70%
- CPU utilization > 80%
- Memory usage > 80%

## Troubleshooting

### Database Locks

```bash
# Check for locked files
lsof | grep sqlite-multi-tenant/databases

# Kill blocking process
kill -9 <PID>

# Restart service
sudo systemctl restart sqlite-multi-tenant
```

### Low Disk Space

```bash
# Find large files
find /var/sqlite-multi-tenant -type f -size +100M

# Clean old backups
find /var/sqlite-multi-tenant/backups -mtime +30 -delete

# Check disk usage
du -sh /var/sqlite-multi-tenant/*
```

### High Memory Usage

Reduce cache size and batch size:

```csharp
options.MaxCacheItems = 1000;
options.BatchSize = 100;
```

## Maintenance

### Monthly Tasks

- Review application logs for errors
- Verify backup integrity
- Check disk usage trends
- Update .NET runtime if patches available

### Quarterly Tasks

- Performance baseline review
- Scaling assessment
- Security audit
- Disaster recovery test

## Conclusion

Follow this guide for production deployments. Customize based on your infrastructure and requirements.
