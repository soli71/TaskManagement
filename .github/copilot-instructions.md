# Copilot Instructions for TaskManagementMvc

## Project Overview
- **Type:** ASP.NET Core MVC application for task, project, and team management (RTL, Persian localization)
- **Key Domains:** Users, Roles, Companies, Projects, Tasks, Invoices, Email Templates
- **Data:** Uses Entity Framework Core with migrations in `Migrations/` and context in `Data/TaskManagementContext.cs`. SQLite DB by default (`taskmanagement.db`).
- **UI:** Bootstrap 5 (RTL), custom CSS in `wwwroot/css/`, Persian font (Vazirmatn), and right-to-left layout. Main layout: `Views/Shared/_Layout.cshtml`.

## Architecture & Patterns
- **Controllers:** All business logic in `Controllers/`, one per domain (e.g., `TasksController`, `UsersController`).
- **Models:** Domain models in `Models/`, view models in `Models/ViewModels/`.
- **Services:** Business logic/services in `Services/` (authorization, etc.).
- **Views:** Razor views in `Views/`, organized by controller. Shared UI in `Views/Shared/`.
- **Identity:** Uses ASP.NET Core Identity with custom `ApplicationUser`/`ApplicationRole`.
- **Migrations:** All DB schema changes tracked in `Migrations/`.

## Developer Workflows
- **Build:**
  ```powershell
  dotnet build TaskManagementMvc.sln
  ```
- **Run:**
  ```powershell
  dotnet run --project TaskManagementMvc.csproj
  ```
- **Migrate DB:**
  ```powershell
  dotnet ef database update
  ```
- **Add Migration:**
  ```powershell
  dotnet ef migrations add <MigrationName>
  ```
- **Debug:** Use Visual Studio/VS Code debugger with launch profile in `Properties/launchSettings.json`.

## Project-Specific Conventions
- **RTL & Persian:** All UI is right-to-left and uses Persian labels/messages. Use Bootstrap RTL and Vazirmatn font.
- **Enums:** Fully qualify enums in Razor views to avoid ambiguity (e.g., `TaskManagementMvc.Models.TaskStatus`).
- **User IDs:** All user/role IDs are `int` (not string/Guid).
- **Soft Delete:** Users are deactivated by setting `IsActive = false` (not deleted from DB).
- **Role Management:** User roles are managed both via ASP.NET Identity and a custom `UserRoles` table for audit/history.

## Integration Points
- **Bootstrap/Icons:** Uses CDN for Bootstrap 5 RTL and Bootstrap Icons.
- **Fonts:** Loads Vazirmatn from Google Fonts CDN.
- **Theme:** Theme switching via `wwwroot/js/theme.js` and CSS variables in `wwwroot/css/theme.css`.

## Examples
- **Add a new domain:** Create model in `Models/`, controller in `Controllers/`, views in `Views/<Domain>/`, and update context in `Data/TaskManagementContext.cs`.
- **Add a new migration:**
  ```powershell
  dotnet ef migrations add AddNewFeature
  dotnet ef database update
  ```

---
For more, see:
- `Controllers/` for business logic
- `Views/Shared/_Layout.cshtml` for layout and theming
- `wwwroot/css/` for custom styles
- `Data/TaskManagementContext.cs` for DB context
