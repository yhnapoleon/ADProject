# 项目进度总结

这份文档总结了近期在 EcoLens.Api 项目中完成的主要功能开发和集成工作。

## 工作概要（上一阶段）

- **文档与 API 说明**：更新 `README.md` 和 `NETv0,1.md`，补充运行环境、配置、迁移、认证及 `ActivityController`、`StepController`、`LeaderboardController` 的接口说明与业务逻辑。
- **Climatiq API 集成**：在 `appsettings` 中配置 Climatiq，实现 `ClimatiqService` 及 DTOs；在 `ActivityController.Upload` 中，当本地 `CarbonReference` 无匹配时调用 Climatiq 获取碳排放估算并可选缓存；`CarbonReference` 增加 `Source`、`ClimatiqActivityId`。
- **条形码数据库管理**：新增 `BarcodeReference` 模型与 `BarcodeController` 的 CRUD，集成 Open Food Facts 服务与 DTOs；`GetByBarcode` 支持按条码查询、从 OFF 拉取产品并本地缓存，与 `CarbonReference` 关联。

---

## 1. README.md 和 NETv0,1.md 更新

-   更新了 `README.md` 文件，详细说明了 EcoLens.Api 的运行环境要求、关键配置文件、使用用户机密、数据库迁移与初始化、启动项目、身份认证（JWT）、CORS、常见问题及目录结构。
-   在 `README.md` 中新增了 **API 接口说明**部分，详细描述了 `ActivityController`、`StepController` 和 `LeaderboardController` 下的所有 API 端点，包括其功能、请求/响应 DTO、业务逻辑和权限要求。
-   更新了 `NETv0,1.md` 文件，将新增的 API 接口说明和业务逻辑细节集成到现有表格中，使其更加完善。

## 2. Climatiq API 集成

为了引入外部碳排放因子数据源，我们集成了 Climatiq API。

-   **配置 Climatiq API**：在 `appsettings.json` 中添加了 `Climatiq:ApiKey` 和 `Climatiq:BaseUrl` 配置。
-   **定义 Climatiq DTOs**：创建了 `DTOs/Climatiq` 目录及 `ClimatiqEstimateRequestDto.cs` 和 `ClimatiqEstimateResponseDto.cs` 文件，用于封装 Climatiq API 的请求和响应数据。
-   **创建 Climatiq 服务**：
    -   在 `Services` 目录下创建了 `IClimatiqService.cs` 接口。
    -   创建了 `ClimatiqService.cs` 实现，该服务负责调用 Climatiq API，获取碳排放估算。
-   **注册 Climatiq 服务**：在 `Program.cs` 中通过 `builder.Services.AddHttpClient<IClimatiqService, ClimatiqService>();` 注册了 Climatiq 服务。
-   **修改 CarbonReference 模型**：在 `Models/CarbonReference.cs` 中添加了 `Source` 和 `ClimatiqActivityId` 字段，以支持标记碳排放因子的来源及 Climatiq 对应的 Activity ID。
-   **修改 ActivityController 的 Upload 方法**：
    -   在 `Controllers/ActivityController.cs` 中注入了 `IClimatiqService`。
    -   修改了 `Upload` 逻辑，使其在本地 `CarbonReference` 未找到匹配时，尝试调用 `ClimatiqService` 获取碳排放估算，并可选地将结果缓存到本地数据库。

## 3. 条形码数据库管理功能开发

为了实现产品条形码与碳排放因子的关联管理，我们开发了条形码数据库管理功能。

-   **定义 BarcodeReference 模型**：在 `Models/BarcodeReference.cs` 中创建了新的 `BarcodeReference` 模型，包含 `Barcode`、`ProductName`、`CarbonReferenceId`（关联到 `CarbonReference`）等字段。
-   **在 ApplicationDbContext 中添加 DbSet**：在 `Data/ApplicationDbContext.cs` 中添加了 `DbSet<BarcodeReference>`，并配置了 `Barcode` 字段的唯一索引。
-   **定义 Open Food Facts DTOs**：创建了 `DTOs/OpenFoodFacts` 目录及 `OpenFoodFactsProductResponseDto.cs` 文件，用于解析 Open Food Facts API 的产品响应。
-   **创建 Open Food Facts 服务**：
    -   在 `Services` 目录下创建了 `IOpenFoodFactsService.cs` 接口。
    -   创建了 `OpenFoodFactsService.cs` 实现，该服务负责调用 Open Food Facts API，根据条形码查询产品信息。
-   **配置 Open Food Facts API**：在 `appsettings.json` 中添加了 `OpenFoodFacts:BaseUrl` 配置。
-   **注册 Open Food Facts 服务**：在 `Program.cs` 中通过 `builder.Services.AddHttpClient<IOpenFoodFactsService, OpenFoodFactsService>();` 注册了 Open Food Facts 服务。
-   **定义 BarcodeController 的 DTOs**：创建了 `DTOs/Barcode` 目录及 `CreateBarcodeReferenceDto.cs`、`UpdateBarcodeReferenceDto.cs`、`BarcodeReferenceResponseDto.cs`、`SearchBarcodeReferenceDto.cs` 文件，用于 `BarcodeController` 的请求和响应。
-   **创建 BarcodeController**：在 `Controllers/BarcodeController.cs` 中创建了 `BarcodeController`，提供了对条形码映射的 CRUD (创建、读取、更新、删除) API，并在 `GetByBarcode` 接口中集成了 Open Food Facts API，实现条形码查询与本地缓存。

## 待完成工作

-   **数据库迁移**：目前 `CarbonReference` 模型和 `BarcodeReference` 模型的新字段及新表尚未通过 Entity Framework Core 迁移应用到实际数据库。您需要运行以下命令来更新数据库：
    ```bash
    dotnet ef migrations add AddClimatiqFieldsAndBarcodeTable -p ".NET/EcoLens.Api" -s ".NET/EcoLens.Api"
    dotnet ef database update -p ".NET/EcoLens.Api" -s ".NET/EcoLens.Api"
    ```
-   **Climatiq Activity ID 映射**：`ActivityController` 中 Climatiq 集成逻辑的 `climatiqActivityId` 目前是硬编码示例，需要根据深度学习模型识别的 `dto.Label` 和 `dto.Category` 实现一个实际的映射逻辑。
-   **Open Food Facts CarbonReference 关联**：在 `BarcodeController` 中，从 Open Food Facts 获取产品信息后，目前是关联到“Unknown Food”的默认 `CarbonReference`。后续可能需要更智能的逻辑来查找或创建合适的 `CarbonReference`。
-   **错误处理和日志记录**：对外部 API 调用和数据库操作的错误处理和日志记录可以进一步完善。

