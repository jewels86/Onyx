ChatGPT wrote this demo for testing with `Onyx.Attack` and `Onyx.Defense`:

Here’s a **clean, working Blazor Server demo** with a real **server-side plugin system** (unsandboxed by design), plus accounts, projects/tasks, encrypted secret storage, and webhook/email plumbing.

### What you get

* **Solution layout**

    * `ProjectDashboard.Abstractions` — `IPlugin`, `ProjectContext` + DTOs for plugins.
    * `ProjectDashboard.Web` — Blazor Server app (Identity + EF Core SQLite, Projects/Tasks, Secrets, Webhooks, PluginHost).
    * `SamplePlugin.HelloWorld` — builds a plugin DLL and **auto-copies** it into `ProjectDashboard.Web/Plugins`.

* **Plugin loader**

    * Loads every `*.dll` in `ProjectDashboard.Web/Plugins` at startup.
    * Finds `IPlugin` implementations and exposes them in the UI.
    * Execution is **UNSANDBOXED** (intentionally), so you can test Onyx.Attack.

* **Secrets**

    * AES-CBC with a 32-byte key in `appsettings.json` (`Security:EncryptionKeyBase64`) — randomly generated for you.
    * Secrets are encrypted at rest; **decrypted values are passed to plugins** (realistic, juicy target).

* **Identity**

    * Built-in UI (Register/Login). We call `app.MapRazorPages()` and wrap with `CascadingAuthenticationState`, so it “just works”.

* **Webhooks & Email**

    * Webhooks fire on **task completion**.
    * SMTP sender service wired; add your SMTP in `appsettings.json`.

### Run it

```bash
# in the root of the zip:
cd ProjectDashboard_Plugins/ProjectDashboard.Web
dotnet restore
dotnet run
```

Open the printed URL (usually [https://localhost:5001](https://localhost:5001)), **Register**, then:

* Create a Project (Projects page).
* Add a Secret or two (Secrets page).
* Build the sample plugin:

  ```bash
  cd ../SamplePlugin.HelloWorld
  dotnet build
  ```

  That drops `SamplePlugin.HelloWorld.dll` into `ProjectDashboard.Web/Plugins/`.
* Stop & re-run the web app to load plugins. Go to **Plugins** page, select a project ID, and click **Run**.

### Key files (so you can zero in fast)

* **Abstractions/IPlugin & context**
  `ProjectDashboard.Abstractions/PluginAbstractions/IPlugin.cs`
  `ProjectDashboard.Abstractions/PluginAbstractions/ProjectContext.cs`
* **Plugin host**
  `ProjectDashboard.Web/Services/Plugins/PluginHost.cs`
* **Context builder (decrypts & surfaces secrets to plugins)**
  `ProjectDashboard.Web/Services/ProjectContextBuilder.cs`
* **Secrets**
  `ProjectDashboard.Web/Services/EncryptionService.cs`
  `ProjectDashboard.Web/Services/SensitiveDataService.cs`
* **UI pages**
  `Pages/Projects.razor`, `Pages/ProjectTasks.razor`, `Pages/Secrets.razor`, `Pages/Plugins.razor`
* **Sample plugin**
  `SamplePlugin.HelloWorld/HelloWorldPlugin.cs`
  (Post-build copies DLL into `/Plugins` folder.)

### Notes on reliability (I fixed the usual pitfalls)

* Identity UI is enabled and mapped (`AddDefaultIdentity().AddDefaultUI(); app.MapRazorPages();`).
* EF Core uses **SQLite** and calls `EnsureCreated()` at startup (no migrations necessary for the demo).
* We keep services DI-scoped correctly; no DbContext leaking into singletons.
* The plugin assembly references **Abstractions** (shared type identity with the host), so reflection/activation is stable.

### Where to poke with Onyx.Attack

* The **PluginHost** entrypoint (unsandboxed execution path).
* The **ProjectContext** that passes **decrypted secrets** into plugins.
* The **webhook caller** (SSR-F-ish angles if a plugin can write URLs).
* Email sender (exfil via SMTP if misconfigured).
* Any assumptions around **project ownership** (currently permissive on purpose).
