# EcoLens ERD（由代码提取生成）

本文件夹下的 ERD 基于 `.NET/EcoLens.Api` 中的 EF Core 模型（`Data/ApplicationDbContext.cs`、`Models/*.cs`、`Migrations/ApplicationDbContextModelSnapshot.cs`）自动整理。

## 文件说明

- `EcoLens.erd.mmd`：Mermaid ER 图（推荐，VSCode / GitHub 可直接预览或用插件渲染）
- `EcoLens.erd.puml`：PlantUML ER 图（适合 draw.io / JetBrains / VSCode PlantUML 插件）
- `EcoLens.arch.mmd`：Mermaid 项目架构图（容器/组件视角）
- `EcoLens.arch.puml`：PlantUML 项目架构图（容器/组件视角）

## 如何预览

### Mermaid

- VSCode 安装 Mermaid 预览插件后打开 `EcoLens.erd.mmd` 或 `EcoLens.arch.mmd`
- 或将其内容复制到支持 Mermaid 的 Markdown 中渲染

### PlantUML

- VSCode 安装 PlantUML 插件并打开 `EcoLens.erd.puml` 或 `EcoLens.arch.puml`
- 或用 draw.io（diagrams.net）的 PlantUML 功能导入

## 备注（约束/索引）

以下约束来自 `ApplicationDbContext` / ModelSnapshot：

- `ApplicationUsers.Email` 唯一索引
- `CarbonReferences(LabelName, Category, Region)` 唯一索引
- `UserFollows(FollowerId, FolloweeId)` 唯一索引

*** End Patch}
