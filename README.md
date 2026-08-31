<div align="center">

# 🧠 CORTEX
### Intelligent Codebase Observatory

**English&nbsp;|&nbsp;[فارسی](#فارسی)&nbsp;|&nbsp;[中文](#中文)**

</div>

---

# CORTEX — Intelligent Codebase Observatory

**AI-Powered Codebase Intelligence, Architecture Analysis & Software Impact Analysis Platform for Windows**

CORTEX turns a local or remote Git repository into a searchable, visual, analyzable model of its own architecture — a live Code Knowledge Graph of every file, namespace, type, method, API, dependency, test and configuration key, plus the relationships between them (calls, inherits, implements, depends-on, references, and more). Built natively for Windows 11 with WinUI 3 and Fluent Design, powered by Roslyn for real semantic code analysis and LibGit2Sharp for Git intelligence.

## ✨ Key Features

### Codebase Intelligence & Knowledge Graph
- Deep Roslyn-based semantic analysis: types, members, call graphs, inheritance, interfaces, generics
- Directed, typed Code Knowledge Graph with full source evidence for every edge
- Incremental indexer: only re-analyzes files that actually changed (SHA-256 + timestamp tracking)
- SQLite FTS5-backed symbol, file and full-text search

### Architecture Visualization
- Interactive, pannable/zoomable Architecture Canvas with minimap and Focus Mode
- Dependency Explorer with circular-dependency detection (Tarjan's algorithm)
- Call Graph Analyzer, Inheritance & Interface Explorer, API Surface Analyzer
- Architecture Heatmap, Module & File/Type dashboards, Path Finder, Impact Lens

### Change Impact Analysis — the heart of CORTEX
- Select any file, class, method, interface or commit and see exactly what depends on it
- Direct + indirect dependents, callers, implementations, affected tests and API contracts
- Impact scored Low / Medium / High / Critical, always with a full explanation trail
- Change Simulation Mode (rename, signature change, removal, DTO change, API response change, module move) — **read-only by default**, never touches source without explicit confirmation

### Git Intelligence
- Branches, tags, commit history, diffs and blame via LibGit2Sharp (read-only — CORTEX never force-pushes or rewrites history)
- Commit Impact Analyzer, Code Churn Analyzer, Ownership Analyzer (presented as historical activity, never as judgement)

### AI Codebase Assistant
- Tool-based Retrieval-Augmented Generation: symbol search + graph traversal + git history, never a blind whole-repo prompt dump
- Local-first by design — Cloud AI is off until explicitly enabled per workspace in the Security/Privacy Center
- Per-repository, per-file AI access permissions; secrets and `.env`/`appsettings*` files are excluded by default

### Metrics, Rules & Reports
- Cyclomatic complexity, LOC, fan-in/fan-out, coupling proxy, configurable thresholds
- Architecture Health Score (clearly labelled as an analytical indicator, not a certification)
- User-defined architecture rules ("UI must not depend on Infrastructure") with violation detection
- Export to JSON, CSV, Markdown and HTML, every export stamped with revision + timestamp + data source

### Security & Privacy
- Git tokens and Cloud AI keys encrypted at rest with **Windows DPAPI**, scoped to the current user
- Local, append-only audit log that never records source code, secrets or credentials
- Optional secret-pattern scanner (candidates only, with false-positive warning — never collects or transmits the actual secret)
- Read-only analysis by default; an optional Safe Apply layer previews every diff and requires explicit confirmation before touching a workspace

### Native Windows 11 Experience
- WinUI 3 + Windows App SDK, Fluent Design, **Mica** backdrop, `NavigationView`, `CommandBar`
- **Five themes**: Windows Default (follows OS), Light, Dark, Red accent, Blue accent
- **Three languages**: English, فارسی (full right-to-left layout), 中文 — every UI string comes from localized `.resw` resources, nothing is hardcoded
- Command Palette (`Ctrl+K` / `Ctrl+P`), and shortcuts including `F12` (Go To Definition), `Shift+F12` (Find References), `Ctrl+Shift+F` (Global Search), `Ctrl+Shift+G` (Focus Graph)
- Scheduled ("working hours") background indexing: you set the daily window, active days and interval, and the app always shows the next run time and the countdown until then

## 🏗️ Project Structure

```
CORTEX/
├── src/
│   ├── Cortex.Core            # Domain models, enums and every subsystem's interface contract
│   ├── Cortex.Roslyn          # Real Roslyn semantic analysis: symbols, call graph, API surface, complexity
│   ├── Cortex.Graph           # In-memory Code Knowledge Graph, traversal, cycle detection, NL graph queries
│   ├── Cortex.Git             # LibGit2Sharp-backed read-only Git intelligence
│   ├── Cortex.Indexing        # Incremental indexing orchestration + file watcher
│   ├── Cortex.Search          # SQLite FTS5 full-text/symbol search
│   ├── Cortex.Storage         # EF Core + SQLite persistence layer, versioned schema, generic cache
│   ├── Cortex.Metrics         # Complexity metrics + Architecture Health Score
│   ├── Cortex.Rules           # Architecture Rules Engine + dependency-direction analysis
│   ├── Cortex.Impact          # Change Impact Analyzer, Simulation Engine, Refactoring Preview
│   ├── Cortex.AI              # Local (ONNX) + Cloud AI providers, RAG retrieval, AI Codebase Assistant
│   ├── Cortex.Security        # DPAPI credential store, audit log, secret scanner
│   ├── Cortex.Reports         # Architecture Review report generator + multi-format export
│   ├── Cortex.Build           # Optional `dotnet build` / `dotnet test` inspector (command-allowlisted)
│   ├── Cortex.Infrastructure  # DI wiring, Serilog logging, background Job Scheduler, working-hours scheduler
│   └── Cortex.UI              # WinUI 3 application: shell, canvas, inspector, command palette, settings
└── tests/
    └── Cortex.Tests           # xUnit tests for graph, cycles, impact, rules, indexing, scheduling
```

## 📋 Requirements

- Windows 11 (or Windows 10 version 1809+)
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Windows App SDK](https://learn.microsoft.com/windows/apps/windows-app-sdk/downloads) (restored automatically via NuGet)
- Visual Studio 2026 with the **".NET Desktop Development"** and **"Windows App SDK"** workloads, *or* the .NET 10 SDK + a terminal
- Git (any recent version)

## 🚀 Installation & Setup

1. **Install the .NET 10 SDK** from the link above and confirm it's on your `PATH`:
   ```
   dotnet --version
   ```

2. **Restore every project's NuGet packages** from the solution root:
   ```
   dotnet restore CORTEX.sln
   ```
   If you prefer to restore/add packages project by project, the key commands are:
   ```
   dotnet add src/Cortex.Roslyn package Microsoft.CodeAnalysis.CSharp
   dotnet add src/Cortex.Roslyn package Microsoft.CodeAnalysis.CSharp.Workspaces
   dotnet add src/Cortex.Roslyn package Microsoft.CodeAnalysis.Workspaces.MSBuild
   dotnet add src/Cortex.Roslyn package Microsoft.Build.Locator
   dotnet add src/Cortex.Git package LibGit2Sharp
   dotnet add src/Cortex.Search package Microsoft.Data.Sqlite
   dotnet add src/Cortex.Storage package Microsoft.EntityFrameworkCore.Sqlite
   dotnet add src/Cortex.Metrics package Microsoft.CodeAnalysis.CSharp
   dotnet add src/Cortex.AI package Microsoft.ML.OnnxRuntime
   dotnet add src/Cortex.Security package System.Security.Cryptography.ProtectedData
   dotnet add src/Cortex.Infrastructure package Microsoft.Extensions.DependencyInjection
   dotnet add src/Cortex.Infrastructure package Serilog
   dotnet add src/Cortex.Infrastructure package Serilog.Extensions.Logging
   dotnet add src/Cortex.Infrastructure package Serilog.Sinks.File
   dotnet add src/Cortex.UI package Microsoft.WindowsAppSDK
   dotnet add src/Cortex.UI package CommunityToolkit.WinUI.Controls.SettingsControls
   dotnet add src/Cortex.UI package CommunityToolkit.WinUI.Controls.Sizers
   dotnet add src/Cortex.UI package CommunityToolkit.Mvvm
   dotnet add tests/Cortex.Tests package xunit
   dotnet add tests/Cortex.Tests package xunit.runner.visualstudio
   ```

3. **Build the solution:**
   ```
   dotnet build CORTEX.sln -c Debug
   ```

4. **Run the desktop app** (Windows only — WinUI 3 does not run on Linux/macOS):
   ```
   dotnet run --project src/Cortex.UI/Cortex.UI.csproj
   ```
   Or open `CORTEX.sln` in Visual Studio 2026 and press **F5**.

5. **Run the test suite:**
   ```
   dotnet test tests/Cortex.Tests/Cortex.Tests.csproj
   ```

6. **First use:** launch CORTEX, choose **Add Repository**, point it at a local folder or a Git URL, and let the initial indexing job finish in the Job Center — subsequent runs only re-analyze what changed.

### Local AI models (optional)
`Cortex.AI.LocalOnnxEmbeddingProvider` expects a `.onnx` embedding model file at the directory you configure in Settings → AI Assistant. This scaffold does not bundle a model — download a compatible ONNX embedding model (e.g. a quantized MiniLM/E5-family model) separately and point CORTEX at its folder. Cloud AI is entirely optional and stays off until you turn it on and store an API key.

## 🧭 Implementation Status

This repository is a genuine, working **architectural foundation** for CORTEX: the domain model, the Roslyn analyzer, the graph engine, cycle detection, the impact analyzer, the rule engine, Git intelligence, SQLite-backed search/storage, DPAPI credential storage, the background job scheduler, the working-hours scheduler, and the full WinUI 3 shell (navigation, theming, localization, command palette) are implemented with real logic, not mocks. A few integration points that inherently require a specific deployment choice — the local ONNX model + tokenizer, the Cloud AI endpoint, PDF export — are wired as clearly-marked extension points rather than faked. Treat this as the skeleton and core engine to build the remaining UI polish and provider integrations on top of, not as a finished, store-ready product.

## 📄 License

Released under the [MIT License](LICENSE).

---

<a id="فارسی"></a>

<div align="right">

# CORTEX — رصدخانه هوشمند کدبیس

**پلتفرم هوش مصنوعی تحلیل کدبیس، تحلیل معماری و تحلیل اثر تغییر نرم‌افزار برای ویندوز**

CORTEX یک مخزن Git محلی یا Remote را به یک مدل قابل جستجو، قابل مشاهده و قابل تحلیل از معماری خودش تبدیل می‌کند — یک Code Knowledge Graph زنده از تمام Fileها، Namespaceها، Typeها، Methodها، APIها، Dependencyها، Testها و Configuration Keyها به همراه روابط میان آن‌ها (Calls، Inherits، Implements، DependsOn، References و موارد دیگر). این نرم‌افزار به‌صورت کاملاً Native برای ویندوز ۱۱ با WinUI 3 و Fluent Design ساخته شده و از Roslyn برای تحلیل معنایی واقعی کد و از LibGit2Sharp برای هوش Git استفاده می‌کند.

## ✨ ویژگی‌های کلیدی

### هوش کدبیس و Knowledge Graph
- تحلیل معنایی عمیق مبتنی بر Roslyn: Typeها، Memberها، Call Graph، Inheritance، Interfaceها، Genericها
- Code Knowledge Graph جهت‌دار و Typed با Evidence کامل از Source برای هر Edge
- Indexer تدریجی (Incremental): فقط فایل‌هایی که واقعاً تغییر کرده‌اند دوباره تحلیل می‌شوند (با SHA-256 و Timestamp)
- جستجوی Symbol، File و Full-Text مبتنی بر SQLite FTS5

### تجسم معماری (Architecture Visualization)
- نقشه معماری تعاملی با قابلیت Pan/Zoom، Minimap و Focus Mode
- Dependency Explorer با شناسایی Circular Dependency (الگوریتم Tarjan)
- Call Graph Analyzer، Inheritance & Interface Explorer، API Surface Analyzer
- Architecture Heatmap، Dashboard برای Module و File/Type، Path Finder، Impact Lens

### تحلیل اثر تغییر — قلب CORTEX
- انتخاب هر File، Class، Method، Interface یا Commit و مشاهده دقیق اینکه چه چیزی به آن وابسته است
- وابستگی‌های مستقیم و غیرمستقیم، Callerها، Implementationها، Testهای تحت تأثیر و API Contractها
- سطح اثر با برچسب Low / Medium / High / Critical، همیشه همراه با مسیر توضیح کامل
- حالت Change Simulation (Rename، تغییر Signature، حذف Class، تغییر DTO، تغییر پاسخ API، جابه‌جایی Module) — **به‌صورت پیش‌فرض فقط خواندنی**، هرگز بدون تأیید صریح کاربر Source را تغییر نمی‌دهد

### هوش Git
- Branchها، Tagها، تاریخچه Commit، Diff و Blame از طریق LibGit2Sharp (فقط خواندنی — CORTEX هرگز Force Push یا بازنویسی تاریخچه انجام نمی‌دهد)
- Commit Impact Analyzer، Code Churn Analyzer، Ownership Analyzer (به‌عنوان آمار فعالیت تاریخی نمایش داده می‌شود، نه قضاوت درباره افراد)

### دستیار هوش مصنوعی کدبیس
- بازیابی مبتنی بر Tool (RAG): ترکیب Symbol Search + Graph Traversal + Git History، بدون ارسال کورکورانه کل مخزن در یک Prompt
- طراحی Local-First — هوش مصنوعی ابری تا زمانی که کاربر آن را به‌صورت صریح در Security/Privacy Center فعال نکند، خاموش است
- سیستم مجوز دسترسی AI به هر Repository و هر File؛ فایل‌های `.env` و `appsettings*` به‌صورت پیش‌فرض مستثنا هستند

### متریک‌ها، قوانین و گزارش‌ها
- Cyclomatic Complexity، LOC، Fan-In/Fan-Out، Coupling Proxy با Threshold قابل تنظیم
- Architecture Health Score (به‌وضوح به‌عنوان یک Indicator تحلیلی معرفی می‌شود، نه یک گواهی کیفیت)
- تعریف Ruleهای معماری توسط کاربر («UI نباید به Infrastructure وابسته باشد») همراه با شناسایی نقض قوانین
- Export به JSON، CSV، Markdown و HTML؛ هر Export شامل Revision، Timestamp و منبع داده است

### امنیت و حریم خصوصی
- توکن‌های Git و کلیدهای AI ابری با **DPAPI ویندوز** رمزنگاری و برای کاربر جاری محافظت می‌شوند
- Audit Log محلی و Append-Only که هرگز Source Code، Secret یا Credential را ثبت نمی‌کند
- اسکنر اختیاری الگوی Secret (فقط به‌عنوان Candidate با هشدار False-Positive — هرگز مقدار واقعی Secret را جمع‌آوری یا ارسال نمی‌کند)
- به‌صورت پیش‌فرض حالت تحلیل فقط‌خواندنی؛ لایه اختیاری Safe Apply پیش از هر تغییر، Diff کامل را نمایش داده و نیازمند تأیید صریح است

### تجربه Native ویندوز ۱۱
- WinUI 3 + Windows App SDK، Fluent Design، پس‌زمینه **Mica**، `NavigationView`، `CommandBar`
- **پنج تم**: پیش‌فرض ویندوز (همسو با تنظیمات سیستم)، روشن، تیره، تم قرمز، تم آبی
- **سه زبان**: English، فارسی (چیدمان کامل راست‌به‌چپ)، 中文 — تمام متن‌های رابط کاربری از منابع Localization (فایل‌های `.resw`) می‌آیند و هیچ متن Hardcoded غیرقابل ترجمه‌ای وجود ندارد
- Command Palette (`Ctrl+K` / `Ctrl+P`) و کلیدهای میانبر شامل `F12` (Go To Definition)، `Shift+F12` (Find References)، `Ctrl+Shift+F` (جستجوی سراسری)، `Ctrl+Shift+G` (تمرکز روی Graph)
- ایندکس زمان‌بندی‌شده در «ساعات کاری»: کاربر بازه روزانه، روزهای فعال و فاصله زمانی را تعیین می‌کند و برنامه همیشه زمان اجرای بعدی و شمارش معکوس تا آن را نمایش می‌دهد

## 🏗️ ساختار پروژه

```
CORTEX/
├── src/
│   ├── Cortex.Core            # مدل‌های دامنه، Enumها و قرارداد Interface هر زیرسیستم
│   ├── Cortex.Roslyn          # تحلیل معنایی واقعی Roslyn: Symbolها، Call Graph، API Surface، Complexity
│   ├── Cortex.Graph           # Code Knowledge Graph در حافظه، Traversal، شناسایی Cycle، Query زبان طبیعی
│   ├── Cortex.Git             # هوش Git فقط‌خواندنی مبتنی بر LibGit2Sharp
│   ├── Cortex.Indexing        # هماهنگ‌سازی Indexing تدریجی + File Watcher
│   ├── Cortex.Search          # جستجوی Full-Text/Symbol مبتنی بر SQLite FTS5
│   ├── Cortex.Storage         # لایه Persistence با EF Core و SQLite، Schema نسخه‌بندی‌شده، Cache عمومی
│   ├── Cortex.Metrics         # متریک‌های Complexity و Architecture Health Score
│   ├── Cortex.Rules           # Rule Engine معماری + تحلیل جهت وابستگی
│   ├── Cortex.Impact          # Change Impact Analyzer، Simulation Engine، Refactoring Preview
│   ├── Cortex.AI              # Providerهای AI محلی (ONNX) و ابری، Pipeline بازیابی RAG، دستیار کدبیس
│   ├── Cortex.Security        # ذخیره‌ساز Credential با DPAPI، Audit Log، اسکنر Secret
│   ├── Cortex.Reports         # تولیدکننده گزارش Architecture Review + Export چندفرمته
│   ├── Cortex.Build           # بازرس اختیاری `dotnet build` / `dotnet test` (با Command Allowlist)
│   ├── Cortex.Infrastructure  # اتصال DI، Logging با Serilog، Job Scheduler پس‌زمینه، زمان‌بند ساعات کاری
│   └── Cortex.UI              # اپلیکیشن WinUI 3: Shell، Canvas، Inspector، Command Palette، Settings
└── tests/
    └── Cortex.Tests           # تست‌های xUnit برای Graph، Cycle، Impact، Rules، Indexing، Scheduling
```

## 📋 پیش‌نیازها

- ویندوز ۱۱ (یا ویندوز ۱۰ نسخه 1809 به بالا)
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Windows App SDK](https://learn.microsoft.com/windows/apps/windows-app-sdk/downloads) (به‌صورت خودکار از طریق NuGet بازیابی می‌شود)
- Visual Studio 2026 با Workloadهای **".NET Desktop Development"** و **"Windows App SDK"**، یا فقط .NET 10 SDK به همراه یک Terminal
- Git (هر نسخه اخیر)

## 🚀 نصب و راه‌اندازی

۱. **نصب .NET 10 SDK** از لینک بالا و اطمینان از قرار گرفتن آن در `PATH`:
   ```
   dotnet --version
   ```

۲. **Restore کردن تمام Packageهای NuGet پروژه‌ها** از ریشه Solution:
   ```
   dotnet restore CORTEX.sln
   ```
   در صورت تمایل به افزودن دستی Packageها پروژه به پروژه، دستورات کلیدی به این شرح‌اند:
   ```
   dotnet add src/Cortex.Roslyn package Microsoft.CodeAnalysis.CSharp
   dotnet add src/Cortex.Roslyn package Microsoft.CodeAnalysis.CSharp.Workspaces
   dotnet add src/Cortex.Roslyn package Microsoft.CodeAnalysis.Workspaces.MSBuild
   dotnet add src/Cortex.Roslyn package Microsoft.Build.Locator
   dotnet add src/Cortex.Git package LibGit2Sharp
   dotnet add src/Cortex.Search package Microsoft.Data.Sqlite
   dotnet add src/Cortex.Storage package Microsoft.EntityFrameworkCore.Sqlite
   dotnet add src/Cortex.Metrics package Microsoft.CodeAnalysis.CSharp
   dotnet add src/Cortex.AI package Microsoft.ML.OnnxRuntime
   dotnet add src/Cortex.Security package System.Security.Cryptography.ProtectedData
   dotnet add src/Cortex.Infrastructure package Microsoft.Extensions.DependencyInjection
   dotnet add src/Cortex.Infrastructure package Serilog
   dotnet add src/Cortex.Infrastructure package Serilog.Extensions.Logging
   dotnet add src/Cortex.Infrastructure package Serilog.Sinks.File
   dotnet add src/Cortex.UI package Microsoft.WindowsAppSDK
   dotnet add src/Cortex.UI package CommunityToolkit.WinUI.Controls.SettingsControls
   dotnet add src/Cortex.UI package CommunityToolkit.WinUI.Controls.Sizers
   dotnet add src/Cortex.UI package CommunityToolkit.Mvvm
   dotnet add tests/Cortex.Tests package xunit
   dotnet add tests/Cortex.Tests package xunit.runner.visualstudio
   ```

۳. **Build گرفتن از Solution:**
   ```
   dotnet build CORTEX.sln -c Debug
   ```

۴. **اجرای اپلیکیشن دسکتاپ** (فقط ویندوز — WinUI 3 روی لینوکس/مک اجرا نمی‌شود):
   ```
   dotnet run --project src/Cortex.UI/Cortex.UI.csproj
   ```
   یا فایل `CORTEX.sln` را در Visual Studio 2026 باز کرده و کلید **F5** را بزنید.

۵. **اجرای مجموعه تست‌ها:**
   ```
   dotnet test tests/Cortex.Tests/Cortex.Tests.csproj
   ```

۶. **اولین استفاده:** CORTEX را اجرا کنید، گزینه **Add Repository** را انتخاب کنید، یک پوشه محلی یا آدرس Git را وارد کنید و اجازه دهید Job اولیه Indexing در Job Center تمام شود — اجراهای بعدی فقط بخش‌های تغییرکرده را دوباره تحلیل می‌کنند.

### مدل‌های AI محلی (اختیاری)
کلاس `Cortex.AI.LocalOnnxEmbeddingProvider` انتظار یک فایل مدل Embedding با فرمت `.onnx` در مسیری دارد که در Settings → AI Assistant تنظیم می‌کنید. این نسخه هیچ مدلی را همراه خود ندارد — یک مدل Embedding سازگار با ONNX (مثلاً از خانواده MiniLM/E5 با Quantization) را جداگانه دانلود کرده و پوشه آن را به CORTEX معرفی کنید. هوش مصنوعی ابری کاملاً اختیاری است و تا زمانی که فعال نکنید و یک API Key ذخیره نکنید، غیرفعال باقی می‌ماند.

## 🧭 وضعیت پیاده‌سازی

این مخزن یک **پایه معماری واقعی و کارکردی** برای CORTEX است: مدل دامنه، تحلیلگر Roslyn، موتور Graph، شناسایی Cycle، تحلیلگر Impact، Rule Engine، هوش Git، جستجو/ذخیره‌سازی مبتنی بر SQLite، ذخیره‌سازی Credential با DPAPI، Job Scheduler پس‌زمینه، زمان‌بند ساعات کاری و کل Shell برنامه WinUI 3 (Navigation، Theming، Localization، Command Palette) با منطق واقعی پیاده‌سازی شده‌اند، نه به‌صورت Mock. چند نقطه اتصال که ذاتاً نیازمند یک انتخاب مشخص در محیط استقرار هستند — مدل و Tokenizer محلی ONNX، Endpoint هوش مصنوعی ابری، Export به PDF — به‌صورت نقاط توسعه مشخص‌شده Wire شده‌اند نه به‌صورت جعلی. این پروژه را به‌عنوان اسکلت و موتور هسته‌ای برای تکمیل باقی جزئیات UI و اتصال Providerها در نظر بگیرید، نه یک محصول نهایی و آماده انتشار در فروشگاه.

## 📄 لایسنس

منتشرشده تحت [MIT License](LICENSE).

</div>

---

<a id="中文"></a>

# CORTEX — 智能代码库天文台

**面向 Windows 的 AI 驱动代码库智能、架构分析与软件影响分析平台**

CORTEX 将本地或远程 Git 仓库转化为一个可搜索、可视化、可分析的架构模型——一个实时的代码知识图谱（Code Knowledge Graph），涵盖每一个文件、命名空间、类型、方法、API、依赖项、测试和配置项，以及它们之间的关系（调用、继承、实现、依赖、引用等）。应用完全原生构建于 Windows 11，采用 WinUI 3 与 Fluent Design，底层由 Roslyn 提供真正的语义级代码分析，并通过 LibGit2Sharp 提供 Git 智能。

## ✨ 核心功能

### 代码库智能与知识图谱
- 基于 Roslyn 的深度语义分析：类型、成员、调用图、继承关系、接口、泛型
- 有向、带类型的代码知识图谱，每条边都保留完整的源码证据
- 增量索引器：仅重新分析真正发生变化的文件（基于 SHA-256 与时间戳）
- 基于 SQLite FTS5 的符号、文件与全文搜索

### 架构可视化
- 可平移/缩放的交互式架构画布，内置小地图与聚焦模式（Focus Mode）
- 依赖关系浏览器，支持循环依赖检测（Tarjan 算法）
- 调用图分析器、继承与接口浏览器、API 表面分析器
- 架构热力图、模块与文件/类型仪表盘、路径查找器、影响透镜（Impact Lens）

### 变更影响分析 — CORTEX 的核心
- 选择任意文件、类、方法、接口或提交，精确查看哪些内容依赖于它
- 直接与间接依赖方、调用方、实现类、受影响的测试与 API 契约
- 影响等级分为低/中/高/严重（Low/Medium/High/Critical），并始终附带完整的推理链路
- 变更模拟模式（重命名、修改接口签名、删除类、修改 DTO 属性、修改 API 响应、移动模块）——**默认只读**，未经用户明确确认绝不修改源代码

### Git 智能
- 通过 LibGit2Sharp 提供分支、标签、提交历史、差异对比与 Blame（只读——CORTEX 绝不执行强制推送或重写历史）
- 提交影响分析器、代码变更频率（Churn）分析器、代码归属分析器（仅作为历史活动统计展示，而非对个人的评判）

### AI 代码库助手
- 基于工具调用的检索增强生成（RAG）：结合符号搜索、图遍历与 Git 历史，而非将整个仓库盲目塞入一个提示词
- 默认本地优先——云端 AI 默认关闭，仅在用户于安全/隐私中心为该工作区明确启用后才会调用
- 按仓库、按文件的 AI 访问权限控制；`.env` 与 `appsettings*` 等文件默认被排除在外

### 指标、规则与报告
- 圈复杂度、代码行数、扇入/扇出、耦合度代理指标，阈值均可配置
- 架构健康评分（明确标注为分析性指标，而非质量认证）
- 用户自定义架构规则（例如"UI 层不得依赖基础设施层"），并检测违规情况
- 支持导出为 JSON、CSV、Markdown 与 HTML，每次导出均标注版本、时间戳与数据来源

### 安全与隐私
- Git 令牌与云端 AI 密钥通过 **Windows DPAPI** 加密存储，仅限当前 Windows 用户访问
- 本地、仅追加写入的审计日志，绝不记录源代码、密钥或凭据
- 可选的潜在密钥模式扫描器（仅作为"候选项"展示并附带误报提示——绝不收集或传输实际密钥内容）
- 默认采用只读分析模式；可选的安全应用（Safe Apply）层会在修改工作区前预览完整差异，并要求明确确认

### 原生 Windows 11 体验
- WinUI 3 + Windows App SDK、Fluent Design、**云母（Mica）**背景效果、`NavigationView`、`CommandBar`
- **五种主题**：Windows 默认（跟随系统）、浅色、深色、红色主题、蓝色主题
- **三种语言**：English、فارسی（完整从右到左布局）、中文——所有界面文本均来自本地化 `.resw` 资源，不存在任何硬编码、不可翻译的文本
- 命令面板（`Ctrl+K` / `Ctrl+P`），以及 `F12`（转到定义）、`Shift+F12`（查找所有引用）、`Ctrl+Shift+F`（全局搜索）、`Ctrl+Shift+G`（聚焦图谱）等快捷键
- "工作时间"计划索引：用户设置每日时间窗口、启用的星期与执行间隔，应用始终显示下一次运行时间及倒计时

## 🏗️ 项目结构

```
CORTEX/
├── src/
│   ├── Cortex.Core            # 领域模型、枚举，以及各子系统的接口契约
│   ├── Cortex.Roslyn          # 真实的 Roslyn 语义分析：符号、调用图、API 表面、复杂度
│   ├── Cortex.Graph           # 内存中的代码知识图谱、遍历、环检测、自然语言图查询
│   ├── Cortex.Git             # 基于 LibGit2Sharp 的只读 Git 智能
│   ├── Cortex.Indexing        # 增量索引编排 + 文件监视器
│   ├── Cortex.Search          # 基于 SQLite FTS5 的全文/符号搜索
│   ├── Cortex.Storage         # 基于 EF Core 与 SQLite 的持久化层，带版本化架构与通用缓存
│   ├── Cortex.Metrics         # 复杂度指标与架构健康评分
│   ├── Cortex.Rules           # 架构规则引擎 + 依赖方向分析
│   ├── Cortex.Impact          # 变更影响分析器、模拟引擎、重构影响预览
│   ├── Cortex.AI              # 本地（ONNX）与云端 AI 提供方、RAG 检索管线、AI 代码库助手
│   ├── Cortex.Security        # 基于 DPAPI 的凭据存储、审计日志、密钥扫描器
│   ├── Cortex.Reports         # 架构评审报告生成器 + 多格式导出
│   ├── Cortex.Build           # 可选的 `dotnet build` / `dotnet test` 检查器（命令白名单限制）
│   ├── Cortex.Infrastructure  # 依赖注入配置、Serilog 日志、后台任务调度器、工作时间调度服务
│   └── Cortex.UI              # WinUI 3 应用：外壳、画布、检查器、命令面板、设置
└── tests/
    └── Cortex.Tests           # 针对图谱、循环依赖、影响分析、规则、索引、调度的 xUnit 测试
```

## 📋 环境要求

- Windows 11（或 Windows 10 1809 及以上版本）
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Windows App SDK](https://learn.microsoft.com/windows/apps/windows-app-sdk/downloads)（通过 NuGet 自动还原）
- 安装了 **".NET 桌面开发"** 与 **"Windows App SDK"** 工作负载的 Visual Studio 2026，或仅使用 .NET 10 SDK 加终端
- Git（任意近期版本）

## 🚀 安装与运行步骤

1. **安装 .NET 10 SDK**（见上方链接），并确认已加入 `PATH`：
   ```
   dotnet --version
   ```

2. **还原所有项目的 NuGet 包**（在解决方案根目录执行）：
   ```
   dotnet restore CORTEX.sln
   ```
   如果希望逐项目手动添加依赖包，关键命令如下：
   ```
   dotnet add src/Cortex.Roslyn package Microsoft.CodeAnalysis.CSharp
   dotnet add src/Cortex.Roslyn package Microsoft.CodeAnalysis.CSharp.Workspaces
   dotnet add src/Cortex.Roslyn package Microsoft.CodeAnalysis.Workspaces.MSBuild
   dotnet add src/Cortex.Roslyn package Microsoft.Build.Locator
   dotnet add src/Cortex.Git package LibGit2Sharp
   dotnet add src/Cortex.Search package Microsoft.Data.Sqlite
   dotnet add src/Cortex.Storage package Microsoft.EntityFrameworkCore.Sqlite
   dotnet add src/Cortex.Metrics package Microsoft.CodeAnalysis.CSharp
   dotnet add src/Cortex.AI package Microsoft.ML.OnnxRuntime
   dotnet add src/Cortex.Security package System.Security.Cryptography.ProtectedData
   dotnet add src/Cortex.Infrastructure package Microsoft.Extensions.DependencyInjection
   dotnet add src/Cortex.Infrastructure package Serilog
   dotnet add src/Cortex.Infrastructure package Serilog.Extensions.Logging
   dotnet add src/Cortex.Infrastructure package Serilog.Sinks.File
   dotnet add src/Cortex.UI package Microsoft.WindowsAppSDK
   dotnet add src/Cortex.UI package CommunityToolkit.WinUI.Controls.SettingsControls
   dotnet add src/Cortex.UI package CommunityToolkit.WinUI.Controls.Sizers
   dotnet add src/Cortex.UI package CommunityToolkit.Mvvm
   dotnet add tests/Cortex.Tests package xunit
   dotnet add tests/Cortex.Tests package xunit.runner.visualstudio
   ```

3. **构建解决方案：**
   ```
   dotnet build CORTEX.sln -c Debug
   ```

4. **运行桌面应用**（仅限 Windows —— WinUI 3 无法在 Linux/macOS 上运行）：
   ```
   dotnet run --project src/Cortex.UI/Cortex.UI.csproj
   ```
   或者在 Visual Studio 2026 中打开 `CORTEX.sln`，然后按 **F5**。

5. **运行测试套件：**
   ```
   dotnet test tests/Cortex.Tests/Cortex.Tests.csproj
   ```

6. **首次使用：** 启动 CORTEX，选择"添加仓库"（Add Repository），指向一个本地文件夹或 Git 地址，并在任务中心（Job Center）中等待首次索引任务完成——之后的索引只会重新分析发生变化的部分。

### 本地 AI 模型（可选）
`Cortex.AI.LocalOnnxEmbeddingProvider` 需要在"设置 → AI 助手"中配置的目录下提供一个 `.onnx` 格式的嵌入模型文件。本脚手架项目未内置任何模型——请自行下载一个兼容 ONNX 的嵌入模型（例如量化后的 MiniLM/E5 系列模型），并将其所在文件夹配置给 CORTEX。云端 AI 完全可选，在用户手动开启并保存 API 密钥之前始终处于关闭状态。

## 🧭 实现状态说明

本仓库是 CORTEX 的一个真实、可运行的**架构基础**：领域模型、Roslyn 分析器、图谱引擎、循环依赖检测、影响分析器、规则引擎、Git 智能、基于 SQLite 的搜索/存储、基于 DPAPI 的凭据存储、后台任务调度器、工作时间调度服务，以及完整的 WinUI 3 应用外壳（导航、主题、本地化、命令面板）均已使用真实逻辑实现，而非模拟代码。少数天然依赖于具体部署选择的集成点——本地 ONNX 模型与分词器、云端 AI 接入端点、PDF 导出——被明确标注为待接入的扩展点，而非伪造实现。请将本项目视为可在其上继续完善剩余界面细节与各类服务提供方集成的骨架与核心引擎，而非一个可直接上架发布的完成品。

## 📄 许可证

基于 [MIT 许可证](LICENSE) 发布。
