# 🐳 PostgreSQL + .NET with Docker Compose

A complete guide for setting up a PostgreSQL database using Docker Compose and integrating it with our .NET application on Windows.

---

## 📋 Prerequisites

Before you begin, ensure you have:

- **Windows 10 or 11 (64-bit)** - Required for Docker Desktop
- **Virtualization enabled** in BIOS/UEFI settings
- **Administrative rights** to install software and enable Windows features
- **.NET SDK** (version 8.0 or higher) installed

---

## 🔧 Part 1: Installing Docker Desktop on Windows

### Step 1: Enable Required Windows Features

Docker Desktop uses **WSL 2** (Windows Subsystem for Linux) or **Hyper-V** as its backend. You need to enable these features first.

#### Option A: Using PowerShell (Recommended)

1. Open **PowerShell as Administrator**
2. Run these commands:

```powershell
dism.exe /online /enable-feature /all /featurename:VirtualMachinePlatform
dism.exe /online /enable-feature /all /featurename:Microsoft-Windows-Subsystem-Linux
```

3. (Optional) Enable Hyper-V if you prefer it over WSL 2:

```powershell
dism.exe /online /enable-feature /all /featurename:Hyper-V
```

4. **Restart your computer** after enabling these features

#### Option B: Using Windows Features UI

1. Open **Control Panel** → **Programs** → **Turn Windows features on or off**
2. Enable the following:
   - ☑ Virtual Machine Platform
   - ☑ Windows Subsystem for Linux
   - ☑ Hyper-V (optional)
3. Click **OK** and restart when prompted

### Step 2: Download and Install Docker Desktop

1. Download **Docker Desktop for Windows** from the [official Docker website](https://www.docker.com/products/docker-desktop)
2. Run the installer (`Docker Desktop Installer.exe`)
3. When prompted, select **"Use WSL 2 instead of Hyper-V"** (recommended for most users)
4. Complete the installation and start Docker Desktop

### Step 3: Configure User Permissions

To run Docker without administrator privileges every time:

1. Open **Computer Management** (right-click Start → Computer Management)
2. Navigate to **Local Users and Groups** → **Groups**
3. Double-click **docker-users**
4. Click **Add** and add your Windows user account
5. Click **OK** and restart Docker Desktop

### Step 4: Verify Installation

Open a terminal (PowerShell or Command Prompt) and run:

```bash
docker version
docker run hello-world
```

If you see the **"Hello from Docker!"** message, your installation is successful! 🎉

#### Common Issues & Solutions

- **Docker won't start**: Check that virtualization is enabled in BIOS and required Windows features are active
- **Blank window or services not starting**: Ensure WSL 2 is properly installed and updated
- **Permission errors**: Make sure your user is added to the `docker-users` group

---

## 🔧 Part 2: Setup postgresql on our project

### 1. Create the `.env` File

In our project root directory, we created a file named `.env` with these contents:

```env
POSTGRES_USER=hepta
POSTGRES_PASSWORD=HEPTAhepta123
POSTGRES_DB=mydatabase
POSTGRES_PORT=5492
```

> **⚠️ Security Note**: Never commit `.env` files to version control! Add it to your `.gitignore` file.

### 2. Create the `docker-compose.yml` File

In the same directory as `.env`, create `docker-compose.yml`:

```yaml
version: '3.8'

services:
  db:
    image: postgres:15
    container_name: my-postgres
    environment:
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
      POSTGRES_DB: ${POSTGRES_DB}
    ports:
      - "${POSTGRES_PORT}:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER}"]
      interval: 10s
      timeout: 5s
      retries: 5
  adminer:
    image: adminer
    container_name: adminer
    ports:
      - "8082:8080"
    depends_on:
      - db
    restart: always

volumes:
  pgdata:
```

### 3. Update `.gitignore` File

Update the `.gitignore` file to exclude sensitive files:

```gitignore
# Environment variables
.env

# Docker volumes
pgdata/

# .NET
bin/
obj/
*.user
*.suo
```

---

## 🚀 Part 3: Running Docker Compose

### Start the PostgreSQL Container

1. Open a terminal and navigate to your project directory (where `docker-compose.yml` is located)
2. Run the following command:

```bash
docker-compose up -d
```

This command will:
- Pull the `postgres:15` image (if not already downloaded)
- Create the `pgdata` volume for persistent storage
- Start the PostgreSQL container in detached mode
- Map port 5492 on your host to port 5432 in the container

### Useful Docker Commands

```bash
# View running containers
docker ps

# View container logs
docker logs my-postgres

# Stop containers (keeps data)
docker-compose down

# Stop containers and remove volumes (deletes data!)
docker-compose down -v

# Restart containers
docker-compose restart
```

### Verify PostgreSQL is Running

You can connect to PostgreSQL using any database client (pgAdmin, DBeaver, etc.):

- **Host**: `localhost`
- **Port**: `5492`
- **Database**: `mydatabase`
- **Username**: `hepta`
- **Password**: `HEPTAhepta123`

---
## 🧩 Part 4:  Connecting to Adminer

### Step 1: Ensure the containers are running 
   ```bash
   docker-compose up -d
   ```
2. Open a browser and go to:
   http://localhost:8082:
   
- **System**: PostgreSQL
- **Server**: `db` (or `localhost` when accessing from outside Docker)
- **Username**: `hepta`
- **Password**: `HEPTAhepta123`
- **Database**: `mydatabase`

3. login:

This ensures you can use adminer now.

## 🧩 Part 5: Integrating with .NET Application

### Step 1: Install Required NuGet Package

Navigate to your .NET project directory and run:

```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

This package enables Entity Framework Core to work with PostgreSQL.

### Step 2: Configure Connection String

In your `appsettings.json`, add the connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5492;Database=mydatabase;Username=hepta;Password=HEPTAhepta123;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### Step 3: Configure DbContext

In your `Program.cs` (for .NET 6+):

```csharp
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext with PostgreSQL
builder.Services.AddDbContext<MyAppContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure middleware
app.MapGet("/", () => "Hello World!");

app.Run();
```

Replace `MyAppContext` with your actual DbContext class name.

### Step 4: Create Your DbContext

Create a `DbContext` class (if you haven't already):

```csharp
using Microsoft.EntityFrameworkCore;

public class MyAppContext : DbContext
{
    public MyAppContext(DbContextOptions<MyAppContext> options)
        : base(options)
    {
    }

    // Define your DbSets here
    // public DbSet<User> Users { get; set; }
}
```
### Step 5: Build application

```bash
# 1. Clean everything
dotnet clean

# 2. Restore packages with correct versions
dotnet restore

# 3. Build to verify compatibility
dotnet build
```


### Step 6: Run Migrations

Create and apply your initial database migration:

```bash
# Create migration
dotnet ef migrations add InitialCreateWithAllModels

# Apply migration to database
dotnet ef database update
```

If you don't have the EF Core tools installed, run:

```bash
dotnet tool install --global dotnet-ef
```

---

## 🎯 Quick Start Summary

For future reference, here's the quick start sequence:

```bash
# 1. Start Docker containers
docker-compose up -d

# 2. Verify containers are running
docker ps

# 3. Run your .NET application
dotnet run

# 4. When done, stop containers
docker-compose down
```
---

## 📝 Additional Notes

### Containerizing Your .NET Application

When you're ready to containerize your .NET app (run it in Docker too), change the connection string host from `localhost` to `db` (the service name in docker-compose.yml):

```
Host=db;Port=5432;Database=mydatabase;Username=hepta;Password=HEPTAhepta123;
```

### Environment Variables Best Practices

- Use `.env.example` with placeholder values for version control
- Keep `.env` in `.gitignore`
- Use different `.env` files for different environments (dev, staging, prod)

### Data Persistence

The `pgdata` volume ensures your database data persists even when containers are stopped. To completely reset:

```bash
docker-compose down -v
```

---

## 🆘 Troubleshooting if Issues encountered

### Docker Desktop won't start
- Verify virtualization is enabled in BIOS
- Check that WSL 2 is installed: `wsl --status`
- Ensure Windows features are enabled

### Port 5492 already in use
- Change `POSTGRES_PORT` in `.env` to a different port
- Or stop the conflicting service

### Cannot connect to database
- Verify container is running: `docker ps`
- Check logs: `docker logs my-postgres`
- Ensure connection string matches `.env` values

### EF Core migrations fail
- Verify database is accessible
- Check connection string in `appsettings.json`
- Ensure `dotnet ef` tools are installed