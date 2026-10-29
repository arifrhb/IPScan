# IPScan - Lightweight Standalone Desktop IP Scanner

A lightweight, high-performance Windows desktop IP scanner built with **C#**, **.NET 9**, and **Windows Forms**. Designed for systems administrators, network engineers, and authorized security professionals to audit, inventory, and assess local and remote networks they own or have explicit authorization to assess.

---

## Highlights

- **Lightweight & Standalone**: Minimal memory and CPU usage; available as a single-file executable requiring zero external runtime installations (or a sub-1MB framework-dependent build).
- **Instant Scan Stop / Cancellation**:
  - Dedicated **"Stop (Esc)"** button and global `Escape` key support to immediately halt in-progress scans without UI freeze or lag.
  - Partial scan results collected prior to cancellation are preserved, viewable, filterable, and exportable.
- **Enter Key to Scan**: Pressing `Enter` in the IP target field begins scanning immediately, suppressing standard Windows textbox ding sounds (`e.SuppressKeyPress = true`).
- **Flexible Input Formats**:
  - Octet dash ranges (e.g., `192.168.201.1-254, 192.168.101.1-254`)
  - Full IP ranges (e.g., `192.168.201.1-192.168.201.254`)
  - CIDR subnets (e.g., `192.168.201.0/24`)
  - Subnet wildcards with `x` or `*` placeholders (e.g., `192.168.201.x/24` or `192.168.201.x`)
  - Single IP addresses (e.g., `192.168.201.1`)
  - Comma-separated mixtures (e.g., `192.168.201.1-254, 192.168.101.1-254, 10.0.0.1, 10.0.1.0/24`)
  - Text files (`.txt`) with one entry per line, with automatic comment (`#`) and blank-line skipping.
- **Upload File & Drag-and-Drop Scanning**:
  - "Upload File..." button and full window Drag-and-Drop for `.txt` files containing IP addresses line by line.
  - Automatically asks: *"Would you like to start scanning for live hosts now?"* to start scanning immediately.
- **Accidental Runaway Protection**: Enforces safety thresholds (minimum `/16` CIDR prefix, 65,536 target ceiling) and prompts for confirmation on large scans (configurable threshold, default 256).
- **Safe & Standard Discovery**: Standard ICMP Echo pings, reverse DNS PTR queries, and RFC 1001/1002 NetBIOS Name Service status queries over UDP 137.
- **Sanitized Multi-Tier Username Discovery**:
  - Fast-path local host detection (`Environment.UserDomainName\Environment.UserName`).
  - Active Directory Kerberos querying via clean DNS hostnames, NetBIOS computer names, and IP fallback (NTLM).
  - NetBIOS packet buffer sanitization: strips null bytes (`\0`) and unprintable control characters, discarding garbage single characters.
  - Bulletproof WMI safeguard: validates remote target UNC paths and strictly rejects Windows native fallback to `\\.\root\cimv2` (localhost) on remote queries.
  - NetBIOS `<03> Unique` registered username fallback for legacy Windows workstations.
- **Non-Invasive**: No stealth, persistence, credential guessing, exploit payloads, or antivirus evasion.
- **Embedded Icon & Version Metadata**: Multi-resolution Windows icon (`app.ico` covering 256x256, 48x48, 32x32, 16x16) embedded both as PE native resource (Desktop/File Explorer) and .NET manifest resource (`AppIconHelper`) for the Taskbar and Titlebar. Full PE assembly metadata (Company, Description, Copyright, Version `1.2.0`).
- **About Dialog**: Dedicated About dialog (`AboutDialog.cs`) displaying dynamic assembly version, runtime environment, developer info, and authorized-use policies.
- **Filter View Options (Online / Offline / Both)**:
  - Top-bar dropdown filter to instantly toggle between **Both**, **Online**, and **Offline** views.
  - Right-click Context Menu submenu (**Filter Results** -> **Both**, **Online**, **Offline**) for fast access.
  - Live filtering during scans and instant, zero-flicker switching on completed results without requiring a re-scan.
  - Export respects active filter (e.g. export only online targets when "Online" view is active).
- **Accessible UI**: Accessible row and cell styling for Online/Offline states that **never relies on color alone** (explicit status badges `[+] Online` and `[-] Offline`).
- **External Integration**: Context menu to launch Web browsers (`http`/`https`), Windows File Explorer shares (`\\<IP>\`), and auto-detected or custom VNC viewers.
- **Export Options**: Human-readable aligned `.txt` reports and native `.xlsx` Excel spreadsheets (powered by the zero-dependency, high-speed [MiniExcel](https://github.com/mini-software/MiniExcel) library).

---

## System Requirements

- **Operating System**: Windows 10, Windows 11, or Windows Server 2016+ (x64)
- **Development Tooling**: Visual Studio 2022 (v17.8 or newer) with the **.NET desktop development** workload, or the .NET 8 / 9 SDK.

---

## Project Structure

```
d:\IPScan\
├── IPScan.sln                              # Visual Studio 2022 solution
├── IPScan.csproj                           # Project file targeting net8.0-windows
├── Program.cs                              # Application entry point (no auto-scan on start)
├── targets_example.txt                     # Sample target list with comments & CIDR ranges
│
├── Models/
│   ├── ScanTarget.cs                       # IP address target model with numeric conversion
│   ├── ScanResult.cs                       # Per-host discovery result with accessible text
│   ├── ScanOptions.cs                      # Concurrency, timeout, and discovery toggles
│   └── ScanProgressReport.cs               # Real-time progress DTO for asynchronous reporting
│
├── Services/
│   ├── Input/
│   │   ├── IpRangeParser.cs                # CIDR, wildcard 'x', comma-separated parser & validator
│   │   └── FileInputParser.cs              # Text file reader (ignoring blank lines and # comments)
│   ├── Scanning/
│   │   ├── NetworkScanner.cs               # Bounded concurrency engine (SemaphoreSlim & CancellationToken)
│   │   ├── PingService.cs                  # ICMP Echo ping with configurable timeouts
│   │   ├── DnsService.cs                   # Asynchronous reverse DNS resolver
│   │   ├── NetBiosService.cs               # RFC 1001/1002 NetBIOS NBNS node status query (UDP 137)
│   │   └── WmiUserService.cs               # Authorized Windows WMI logged-in user lookup
│   ├── External/
│   │   ├── ExternalToolLauncher.cs         # Safe process launcher using ArgumentList (no shell injection)
│   │   └── VncDetector.cs                  # Standard registry & path detector for installed VNC clients
│   └── Export/
│       ├── IResultExporter.cs              # Exporter interface
│       ├── TxtExporter.cs                  # Formatted tabular text report generator
│       └── ExcelExporter.cs                # Native Excel (.xlsx) generator via MiniExcel
│
├── UI/
│   ├── MainForm.cs / .Designer.cs          # Main responsive WinForms interface with DataGridView
│   └── SettingsForm.cs / .Designer.cs      # Concurrency, timeout, WMI, and VNC configuration dialog
│
├── Properties/
│   └── PublishProfiles/
│       ├── FolderProfile-SelfContained.pubxml
│       └── FolderProfile-FrameworkDependent.pubxml
│
└── tests/
    └── IPScan.Tests/                       # Automated xUnit test suite (17 tests)
```

---

## Building and Publishing

### 1. Build and Run in Visual Studio 2022
1. Open `IPScan.sln` in **Visual Studio 2022**.
2. Select **Release** or **Debug** configuration with **Any CPU** or **x64**.
3. Press `F5` to build and run, or `Ctrl+F5` to run without debugging.

### 2. Run Automated Unit Tests
From the repository root:
```powershell
dotnet test IPScan.sln
```

### 3. Publishing a Standalone Single-File Executable

#### Option A: Self-Contained Single-File (Recommended for portability)
Bundles the .NET 8 runtime and native libraries into a single `.exe` file that runs on any compatible 64-bit Windows system without requiring .NET to be installed:
```powershell
dotnet publish IPScan.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o ./publish/self-contained
```
- **Output Executable**: `./publish/self-contained/IPScan.exe`
- **File Size**: ~71 MB (compressed bundle containing runtime + WPF/WinForms base class libraries).

#### Option B: Framework-Dependent Single-File (Ultra-lightweight)
Produces a tiny single executable that relies on the .NET 8 Desktop Runtime installed on the machine:
```powershell
dotnet publish IPScan.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o ./publish/framework-dependent
```
- **Output Executable**: `./publish/framework-dependent/IPScan.exe`
- **File Size**: **~980 KB (< 1 MB!)**

---

## Publishing Tradeoffs: Single-File, Trimming, and Size

| Strategy | File Size | Dependencies | Tradeoffs & Considerations |
| :--- | :--- | :--- | :--- |
| **Self-Contained Single-File** | ~71 MB | None (runs out of the box on Windows 10/11) | Larger file size because the entire .NET runtime and Windows desktop subsystem are embedded. Fast startup via memory compression. |
| **Framework-Dependent Single-File** | ~980 KB | Requires .NET 8 Desktop Runtime | Extremely fast download and tiny footprint; perfect for managed enterprise fleets where .NET 8 is already deployed. |
| **Trimming (`PublishTrimmed=true`)** | ~35-45 MB | None | **Not recommended for WinForms**: Windows Forms and WMI rely on reflection, GDI+ Win32 handles, and dynamic COM activation. Aggressive IL trimming can strip critical metadata and cause runtime crashes in DataGridView or WMI querying. |

---

## Discovery Modules & Technical Disclosures

### 1. ICMP Ping
- Uses .NET `System.Net.NetworkInformation.Ping` with a standard 32-byte payload.
- Configurable timeout (default: 1000 ms).
- Gracefully handles unreachable destinations, packet drops, and socket errors.

### 2. Reverse DNS
- Executes asynchronous PTR lookups via `Dns.GetHostEntryAsync`.
- Wrapped in a strict timeout to ensure unresolvable hosts do not stall the scan pipeline.

### 3. NetBIOS Name Query (RFC 1001 / RFC 1002)
- Dispatches a standard 50-byte NBNS Node Status wildcard query (`*` padded with nulls: `CKAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA`) over UDP port 137.
- Parses the returned resource records for Workstation Name (`<00> Unique`) and Domain / Workgroup Name (`<00> Group`).
- Completely passive and unauthenticated; does not interact with SMB shares or sessions.

### 4. Logged-in Username Discovery (WMI / Win32_ComputerSystem)
> [!IMPORTANT]
> **Limitations & Security Disclosure**:
> - Ping, DNS, and NetBIOS **cannot** discover the logged-in user under any circumstances.
> - IPScan queries the logged-in username using standard Windows Management Instrumentation (`Win32_ComputerSystem.UserName`) over RPC/DCOM (TCP port 135 and dynamic RPC ports).
> - This feature **requires**:
>   1. The target host must be running Microsoft Windows.
>   2. The user running IPScan must possess administrative or domain administrator privileges on the target computer.
>   3. The target's Windows Firewall must permit remote WMI / RPC management traffic.
>   4. An interactive user must be logged into the local console or an active Remote Desktop session.
> - If any requirement is missing, IPScan displays `-` and continues without failing or crashing. IPScan **never** attempts password guessing, credential harvesting, or exploitation.

---

## Context Menu & External Tool Launcher

Right-clicking any host row exposes:
- **Open HTTP (`http://<IP>`)**: Opens host in the default browser.
- **Open HTTPS (`https://<IP>`)**: Opens host over HTTPS in the default browser.
- **Launch VNC Viewer**:
  - Automatically searches standard installation directories and registry `App Paths` for:
    - TightVNC (`tvnviewer.exe`)
    - RealVNC (`vncviewer.exe`)
    - UltraVNC (`vncviewer.exe`)
    - TigerVNC (`vncviewer.exe`)
  - Allows specifying a custom viewer binary under **Settings**.
  - Does **not** bundle or download third-party viewers.
  - Passes the IP safely via `ProcessStartInfo.ArgumentList` to eliminate command-line injection risks.
- **Open Shared Folders (`\\<IP>\`)**: Opens Windows File Explorer to browse network shares.
- **Copy IP / Row Details**: Copies IP address or full tab-delimited row to clipboard.

---

## Code Signing & Application Reputation

### Why Code Sign?
Signing the executable embeds a digital signature that verifies the publisher's identity and ensures the binary has not been tampered with since compilation.

> [!WARNING]
> **Antivirus & SmartScreen Reality**:
> - Code signing identifies the publisher and maintains binary integrity; **it does not guarantee automatic antivirus approval or eliminate Microsoft Defender SmartScreen warnings**.
> - SmartScreen builds reputation based on certificate history, download volume, and prevalence across Windows systems. Extended Validation (EV) certificates establish reputation faster than standard OV certificates, but new binaries may still prompt until known.
> - IPScan strictly adheres to legitimate discovery APIs and contains **no evasion techniques**. If an endpoint protection solution triggers on network probing activities, submit the signed binary to your security team or vendor for reputation whitelisting.

### Signing Instructions

#### 1. Signing with the Windows SDK (`signtool.exe`)
The Windows SDK provides `signtool.exe` (found in `C:\Program Files (x86)\Windows Kits\10\bin\<version>\x64\signtool.exe`).

**Using a PFX Certificate file:**
```powershell
$signtool = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe"

& $signtool sign /f "C:\Path\To\YourCert.pfx" `
                 /p "YourCertificatePassword" `
                 /fd SHA256 `
                 /tr "http://timestamp.digicert.com" `
                 /td SHA256 `
                 /d "IPScan Network Scanner" `
                 "./publish/self-contained/IPScan.exe"
```

**Using a Certificate from the Windows Certificate Store:**
```powershell
& $signtool sign /n "Your Company or Publisher Name" `
                 /fd SHA256 `
                 /tr "http://timestamp.digicert.com" `
                 /td SHA256 `
                 /d "IPScan Network Scanner" `
                 "./publish/self-contained/IPScan.exe"
```

**Verify the signature:**
```powershell
& $signtool verify /pa /v "./publish/self-contained/IPScan.exe"
```

#### 2. Configuring Signing in Visual Studio 2022
1. Right-click the **IPScan** project in Solution Explorer and select **Properties**.
2. Navigate to the **Build** -> **Strong naming** or create a **Post-build event** / **Publish Profile** hook.
3. In `IPScan.csproj`, you can add a post-publish target:
```xml
<Target Name="SignOutput" AfterTargets="Publish" Condition="'$(Configuration)' == 'Release'">
  <Exec Command="&quot;C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe&quot; sign /n &quot;Your Certificate Subject&quot; /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 &quot;$(PublishDir)IPScan.exe&quot;" />
</Target>
```

---

## Authorized Use & Ethics Notice

**IPScan is created solely for legitimate network diagnostics, administrative management, and authorized network security assessments.**

Scanning networks without explicit authorization from the network owner may violate organizational policies, acceptable use terms, or local and federal computer crime statutes (e.g., the U.S. Computer Fraud and Abuse Act or regional equivalents). Ensure you have documented permission before scanning networks outside your personal or laboratory environment.

