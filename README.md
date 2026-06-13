# IOCL Fleet Compliance Management System

> **Panipat Refinery — Fleet Compliance & Validity Monitor**  
> Built with **ASP.NET 4.8 Web Forms (VB.NET)** + **SQLite**

---

## 📋 Overview

A full-featured internal fleet management and compliance tracking portal for IOCL Panipat Refinery. It manages vehicle registrations, compliance document tracking (8 document types), expiry alerts, gate QR-scanning, audit trails, and automated email notifications.

### Key Features

| Feature | Description |
|---|---|
| 🚗 **Vehicle Registry** | Register vehicles with all 8 compliance documents |
| 🚙 **Ownership Types** | Support for **Personal** (simplified form, auto-assigned as Car, hides driver/vendor info) vs **Contractual** vehicles |
| 📄 **Document Uploads** | PDF upload for Road Permit, PUC, Fitness, Insurance, etc. |
| ⏰ **Expiry Tracking** | Automatic status: ACTIVE / WARNING / CRITICAL / EXPIRED |
| 📧 **Daily Expiry Alerts** | Background service emails owners & admins daily about warning/expired statuses with anti-spam daily throttling |
| 📨 **Always-Attach Digest** | Daily admin summary emails always attach a compiled compliance PDF, even with 0 expiries |
| 🔑 **Gate Scanner** | QR code scan for gate entry pass verification |
| 🔐 **OTP Security Flow** | Secure Forgot Password flow + mandatory first-time login email verification OTP |
| 📊 **Reports** | PDF/Excel compliance reports |
| 🔒 **Role-Based Access** | SuperAdmin (view all), Employee (own vehicles) |
| 📝 **Audit Trail** | Full audit log of all system actions |
| 🗄️ **Document Vault** | Centralized compliance document storage |

---

## 🗂️ Project Structure

```
IOCL-WebForms/
├── App_Code/               # VB.NET backend modules
│   ├── Database.vb         # SQLite data access layer
│   ├── Compliance.vb       # Compliance engine + background scheduler
│   ├── EmailService.vb     # SMTP email notifications
│   ├── Seeder.vb           # Database seed / SuperAdmin setup
│   └── ...
├── App_Data/
│   ├── iocl_compliance_forms.db   # SQLite database
│   └── Uploads/            # Uploaded compliance PDFs
├── bin/                    # Required DLLs (SQLite, QRCoder, iTextSharp)
├── Login.aspx              # Login page
├── Default.aspx            # Dashboard
├── Vehicles.aspx           # Fleet registry
├── Expiry.aspx             # Expiry management & renewals
├── Renewals.aspx           # Renewal logs
├── Vault.aspx              # Document vault
├── Users.aspx              # User management (SuperAdmin only)
├── Audit.aspx              # Audit trail
├── Gate.aspx               # Gate QR scanner
├── Reports.aspx            # Compliance reports
├── Site.Master             # Master layout page
├── web.config              # App configuration (SMTP, DB)
├── RunServer.exe           # One-click dev server launcher
└── run.bat                 # Batch launcher alternative
```

---

## ⚙️ Prerequisites

| Requirement | Version |
|---|---|
| Windows OS | Windows 10 / 11 / Server 2019+ |
| .NET Framework | **4.8** (pre-installed on Win10+) |
| ASP.NET | Included with .NET 4.8 |
| SQLite | Bundled in `bin/` |
| Browser | Chrome / Edge (modern) |

> ✅ **No installation required.** All dependencies are bundled in the `bin/` folder.

---

## 🚀 How to Run (Local Development)

### Option 1 — One Click (Recommended)

Double-click **`RunServer.exe`** in the project folder.

```
IOCL-WebForms/
└── RunServer.exe   ← Double-click this
```

The server will start and print:
```
Physical Directory: C:\...\IOCL-WebForms\
Server started successfully on http://localhost:8090/
```

Then open your browser and go to:
```
http://localhost:8090/
```

---

### Option 2 — Batch File

Double-click **`run.bat`** or run from command prompt:

```bat
cd C:\Users\Lenovo\Downloads\IOCL-WebForms
run.bat
```

---

### Option 3 — PowerShell

```powershell
cd C:\Users\Lenovo\Downloads\IOCL-WebForms
.\RunServer.exe
```

---

## 🔐 Default Login

| Field | Value |
|---|---|
| **Employee No** | `00000001` |
| **Password** | `Admin@123` |
| **Role** | SuperAdmin |

> ⚠️ Change the password after first login via **User Accounts** page.

---

## 📧 Email Configuration (SMTP)

Edit **`web.config`** and fill in the AppSettings section:

```xml
<appSettings>
  <add key="EmailHost"        value="smtp.gmail.com" />
  <add key="EmailPort"        value="587" />
  <add key="EmailUser"        value="your-email@gmail.com" />
  <add key="EmailPass"        value="your-app-password" />
  <add key="EmailFromName"    value="IOCL Fleet Compliance" />
  <add key="EmailFromAddress" value="your-email@gmail.com" />
</appSettings>
```

> 💡 For Gmail: Use an **App Password** (not your regular password).  
> Go to: Google Account → Security → 2-Step Verification → App Passwords

---

## 🏛️ Compliance Document Types

| # | Document | Purpose |
|---|---|---|
| 1 | Road Permit (RTO) | Permit to operate on roads |
| 2 | Age Determination / DOM | Vehicle age certificate |
| 3 | Pollution Under Control (PUC) | Emission compliance |
| 4 | Fitness Certificate (RTO) | Vehicle roadworthiness |
| 5 | Explosive License | Hazardous material transport |
| 6 | Green Card | Environmental compliance |
| 7 | Vehicle Insurance | Third-party insurance |
| 8 | Calibration Certificate | Meter/equipment calibration |

---

## 👥 User Roles

| Role | Permissions |
|---|---|
| **SuperAdmin** | View all vehicles, verify vehicles, manage users, access all reports & audit logs |
| **Employee** | Register vehicles (own), upload documents, view own vehicle compliance |

> ℹ️ Only **Employees** can register new vehicles. SuperAdmin has read/verify/manage access only.

---

## 🗃️ Database

- **Type:** SQLite (file-based, no server needed)
- **Location:** `App_Data/iocl_compliance_forms.db`
- **Auto-initialized:** On first run, the database schema and SuperAdmin account are created automatically via `Global.asax → Application_Start`

---

## 🔄 Background Compliance Scheduler

The app runs an automatic compliance check every **12 hours**:
- Scans all vehicle documents for expiry
- Updates status: ACTIVE / WARNING (≤60 days) / CRITICAL (≤30 days) / EXPIRED
- Sends email alerts to vehicle owner + SuperAdmin

---

## 📦 Key Dependencies (in `bin/`)

| Library | Version | Purpose |
|---|---|---|
| System.Data.SQLite | 1.0.118 | SQLite database driver |
| QRCoder | 1.4.3 | QR code generation for gate passes |
| iTextSharp | 5.5.13.3 | PDF report generation |
| BouncyCastle | Latest | Cryptography for PDF signing |

---

## 🛠️ Building from Source

If you need to rebuild `RunServer.exe`:

```powershell
cd C:\Users\Lenovo\Downloads\IOCL-WebForms
.\build.ps1
```

Or compile manually with the .NET compiler:

```powershell
C:\Windows\Microsoft.NET\Framework\v4.0.30319\aspnet_compiler.exe `
  -v / -p "C:\Users\Lenovo\Downloads\IOCL-WebForms" `
  "C:\Users\Lenovo\Downloads\IOCL-WebForms\precompiled" -f
```

---

## 🌐 Production Deployment (IIS)

1. Open **IIS Manager**
2. Create a new website pointing to this folder
3. Set Application Pool to **.NET CLR v4.0**, **Integrated Pipeline**
4. Ensure `App_Data/` and `App_Data/Uploads/` have **write permissions** for the IIS user
5. Update `web.config` with your SMTP settings

---

## 📸 Screenshots

| Page | Description |
|---|---|
| Login | Dark-mode login with IOCL logo, 8-digit employee number |
| Dashboard | Compliance overview, stats, recent alerts |
| Vehicles | Fleet registry with compliance slot indicators |
| Registration | Vehicle + 8-document registration form |
| Gate | QR scanner for gate entry verification |

---

## 📝 License

Internal use only — Indian Oil Corporation Ltd. (IOCL), Panipat Refinery.

---

*Built and maintained for IOCL Panipat Refinery Fleet Operations.*
