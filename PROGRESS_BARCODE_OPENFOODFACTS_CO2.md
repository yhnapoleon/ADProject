# Barcode / Open Food Facts 碳排放因子解析修复

**日期**：2026-01-24  
本文档记录对 `BarcodeController` 及 Open Food Facts 集成中 **co2_total 解析与 Co2Factor 更新** 的修复与增强工作。

---

## 1. 问题背景

- **GetByBarcode** 从 Open Food Facts 拉取产品后，**未正确创建 `BarcodeReference`**，存在未实例化即 `AddAsync(barcodeRef)` 的逻辑错误与多余/残缺代码块。
- **co2_total 解析失败**：`ecoscore_data` 在部分产品中以 **JSON 字符串** 返回，直接反序列化为 `EcoScoreDataDto` 会失败，导致 `EcoScoreData`、`Agribalyse.Co2Total` 等为 null，Co2Factor 退化为默认 0.5 或沿用旧的 0.56。
- **单位换算错误**：曾按 g/100g 对 `co2_total` 除以 100；经核对，Open Food Facts 返回的 `co2_total` 已是 **kg CO2e/kg**，不应再除。
- **缓存无法更新**：一旦写入错误或过时的 Co2Factor，缺少从 OFF 重新拉取并覆盖的机制。

---

## 2. BarcodeController 修复与增强

### 2.1 修复 GetByBarcode 中创建 BarcodeReference 的逻辑

- **问题**：在从 Open Food Facts 获取到有效产品后，只处理了 `CarbonReference` 的查找/创建与更新，**从未 `new BarcodeReference` 并 `AddAsync`**，却对 `barcodeRef` 做 `AddAsync(barcodeRef)`，导致 `NullReferenceException` 或逻辑混乱。
- **修改**：在确定 `carbonRef` 之后，显式创建并持久化 `BarcodeReference`：
  - `Barcode`、`ProductName`（`offProduct.Product.ProductName ?? "Unknown"`）、`CarbonReferenceId`、`Category`（`categoryLabelName`）、`Brand`（`offProduct.Product.Brands`）
  - 设置 `barcodeRef.CarbonReference = carbonRef` 以便构造响应 DTO。
- **删除**：多余且错误的代码块（重复查询 `carbonRef`、未完成的 `if (carbonRef is null)` 及错误的 `AddAsync(barcodeRef)` 等）。

### 2.2 Co2 取值与单位

- **取值**：`co2_total` 从 `ecoscore_data.agribalyse.co2_total` 或 `ecoscore_data.agribalyse_co2_total` 获取，**直接使用**，不再除以 100。
- **单位**：`extractedUnit = "kgCO2e/kg"`，与 OFF 一致。

### 2.3 新增 `refresh` 查询参数

- **用途**：当条形码已存在本地 `BarcodeReference` 时，`?refresh=true` 可**强制从 Open Food Facts 重新拉取**，并用最新 `co2_total` 更新其关联的 `CarbonReference.Co2Factor`、`Unit`、`Source`，解决“缓存了错误或过时数据”的问题。
- **实现**：`GetByBarcode(string barcode, [FromQuery(Name = "refresh")] bool? refresh = null, ...)`；当 `barcodeRef != null && refresh == true` 时，调用 `IOpenFoodFactsService.GetProductByBarcodeAsync`，从 `EcoScoreData` 取 `co2`，若 `barcodeRef.CarbonReference != null` 则更新并 `SaveChanges`。

---

## 3. Open Food Facts 反序列化与 co2_total 提取

### 3.1 EcoScoreDataJsonConverter（新增）

- **文件**：`DTOs/OpenFoodFacts/EcoScoreDataJsonConverter.cs`
- **原因**：Open Food Facts 的 `ecoscore_data` 可能是 **JSON 对象** 或 **JSON 字符串**；若为字符串，直接反序列化为 `EcoScoreDataDto` 会失败，`Product.EcoScoreData` 为 null。
- **逻辑**：
  - `Read`：若为 `JsonTokenType.String`，则对字符串再作 `JsonSerializer.Deserialize<EcoScoreDataDto>(json, options)`；若为 `StartObject`，则按对象反序列化；其余则跳过。
  - `Write`：按 `EcoScoreDataDto` 正常序列化。
- **挂载**：在 `ProductDto.EcoScoreData` 上添加 `[JsonConverter(typeof(EcoScoreDataJsonConverter))]`。

### 3.2 OpenFoodFactsProductResponseDto 补充

- **EcoScoreDataDto**：新增 `AgribalyseCo2Total`，用于承接 `ecoscore_data.agribalyse_co2_total`，作为 `Agribalyse.Co2Total` 的备用。
- **Controller / Service**：取值顺序为  
  `EcoScoreData?.Agribalyse?.Co2Total ?? EcoScoreData?.AgribalyseCo2Total`。

### 3.3 OpenFoodFactsService 原始 JSON 兜底

- **场景**：在 `JsonPropertyNamingPolicy.SnakeCaseLower` 等正常反序列化后，若仍无法从 `EcoScoreData` 得到 `co2_total`（`TryGetCo2FromEcoScore` 为 false），则从**原始 JSON** 再解析并补全。
- **实现**：`TryPatchEcoScoreFromRaw(string json, ProductDto product)`  
  - 用 `JsonDocument` 解析整段响应；  
  - 取 `product.ecoscore_data`：若为 **字符串**，则再 `JsonDocument.Parse` 该字符串，对其 `RootElement` 查找 `co2_total`；若为 **对象**，则直接在该对象上查找；  
  - 查找路径：`agribalyse.co2_total`，若无则 `agribalyse_co2_total`；  
  - 若得到 `co2`，则创建或补全 `product.EcoScoreData`、`Agribalyse`，并写入 `Co2Total`。
- **辅助**：`TryGetCo2FromElement(JsonElement el, out decimal? co2)` 统一从 `JsonElement` 中按上述路径取 `co2_total` / `agribalyse_co2_total`。

---

## 4. 涉及文件

| 文件 | 修改类型 |
|------|----------|
| `Controllers/BarcodeController.cs` | 修复 GetByBarcode 创建 BarcodeReference、Co2 取值与单位；新增 `refresh`；删除错误逻辑块 |
| `DTOs/OpenFoodFacts/OpenFoodFactsProductResponseDto.cs` | 为 `EcoScoreData` 添加 `[JsonConverter(...)]`；`EcoScoreDataDto` 增加 `AgribalyseCo2Total` |
| `DTOs/OpenFoodFacts/EcoScoreDataJsonConverter.cs` | **新增**，支持 `ecoscore_data` 为字符串或对象 |
| `Services/OpenFoodFactsService.cs` | 使用统一 `JsonOptions`；反序列化后 `TryGetCo2FromEcoScore`、`TryPatchEcoScoreFromRaw`、`TryGetCo2FromElement` |

---

## 5. 验证结果

- **条形码 7622210449283**（Prince Goût Chocolat）：Open Food Facts 的 `co2_total` 为 **3.59** kg CO2e/kg；修复后 `GET /api/Barcode/7622210449283` 及 `GET /api/Barcode/7622210449283?refresh=true` 均能返回 `co2Factor: 3.59`，`unit: "kgCO2e/kg"`。
- 若之前因解析失败而写入 0.56 等错误值，通过 `?refresh=true` 可覆盖为 3.59。

---

## 6. 与 PROGRESS_SUMMARY.md 中“待完成”的对应关系

- **“Open Food Facts CarbonReference 关联”**：本次在保留按 `categoryLabelName` 查找/创建 `CarbonReference` 的基础上，修正了 **Co2 因子的来源与单位**，并从 `ecoscore_data`（含字符串形式）稳定解析 `co2_total`，使关联到的 `CarbonReference` 具备正确的 `Co2Factor`。
- **“需要更智能的逻辑”**：本次未改动按类别映射 `CarbonReference` 的规则，但通过 `refresh` 与多重 co2 取值路径，显著提高了 Co2Factor 的**正确性与可纠错性**。
