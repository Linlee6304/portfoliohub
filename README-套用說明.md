# PortfolioHub 帳戶功能更新

## 套用方式

這是接續前一份首頁／登入修正版本的更新包。請將 ZIP 內的 `portfoliohub/` 內容，合併到你原本專案根目錄，依相同路徑新增或覆蓋檔案。不要把整個 client 或 server 資料夾刪除。

1. 停止前後端程式，保留自己的專案備份。
2. 覆蓋／新增下方清單中的檔案。
3. **手動刪除下方 4 個舊檔**。解壓縮不會自動刪除它們；舊 RevokedTokenService 已整合到 AuthService → AuthReopnsitory。
4. 前端執行 `npm run build`；後端執行 `dotnet build server/server.csproj`，完成後重新啟動。

不用執行 migration，也不用刪資料表。未包含資料庫連線設定、密碼、node_modules、bin、obj 或編譯產物。

## 功能與資料規則

- 基本資料進入後直接編輯，只提供「儲存」按鈕，成功顯示「儲存成功」小卡片。
- 只更新暱稱、電話、頭像網址、簡介四項。暱稱對應 `CreatorProfiles.DisplayName`；Identity 的 `UserName` 仍維持原登入信箱。
- 登入 Email、Identity UserName、聯絡 Email、角色、帳戶識別碼均不接受此 API 修改。後端只從登入憑證取得帳戶識別碼。
- 頭像沿用圖片 URL 方式；可留白，非空值只接受 HTTP(S)。
- 接案狀態在右上角姓名選單中以三段滑桿調整：0 不接案、1 接案中、2 可接案；變動直接儲存，不必按按鈕。
- 連續滑動依序處理，保存最後選擇；失敗提示原因並回復已確認的狀態。
- 基本資料與接案狀態各自只更新自己的欄位，避免同時操作互相覆寫。
- 密碼維持獨立頁面與 API，修改成功後舊登入憑證全部失效；登出只撤銷本次登入。
- 註冊角色固定為既有的 `Creator`。沒有修改角色的 API／介面。
- 啟動時只補齊 Admin／Creator 角色定義，不再依 Admin 設定自動建立管理員帳戶或提升既有使用者權限。既有資料庫角色保留，管理員由你直接在資料庫維護。
- 管理員若已有 CreatorProfiles，可修改自己的四項基本資料；若沒有，只顯示信箱及「尚無可更新資料」，不會自動建立個人資料。

## 三層分工

`AuthController → IAuthService / AuthService → IAuthReopnsitory / AuthReopnsitory`

Controller 接收 DTO、取得登入身分並回傳結果；Service 處理驗證、業務規則與 DTO；Repository 處理 Identity、資料庫讀寫與交易。沿用原專案 `Reopnsitory` 拼字及資料夾，避免不必要的路徑變更。所有本次使用的介面方法均有中文功能註解。

## 帳戶 API（全部在 AuthController）

| 方法 | 路徑 | 用途 |
|---|---|---|
| POST | /api/Auth/register | 註冊 Creator |
| POST | /api/Auth/login | 登入 |
| GET | /api/Auth/me | 取得目前帳戶 |
| PUT | /api/Auth/profile | 更新 displayName、contactPhone、avatarUrl、bio |
| PATCH | /api/Auth/work-status | 更新 workStatus：0／1／2 |
| POST | /api/Auth/change-password | 單獨修改密碼 |
| POST | /api/Auth/logout | 登出目前憑證 |

除了登入與註冊，其餘均需 Bearer JWT。

## 驗證結果

- 前端正式編譯與 ESLint 通過。
- 9 項後端整合測試通過，使用獨立 SQLite 測試資料庫：角色防修改、信箱防修改、帳戶身分隔離、狀態範圍與獨立更新、停用帳戶、登入、登出持久撤銷、修改密碼失效。
- Edge 瀏覽器搭配模擬 API：四欄位儲存、成功小卡片、連續切換、失敗回復、保留未儲存輸入、並行操作、重載與登出通過；檢查 320／375／768／1365 寬度。
- 未使用或修改你的正式帳戶資料，尚未在你的 SQL Server 實際登入操作。本次沒有資料庫結構變更。

## 必須刪除

- `server/Controllers/CreatorAdminController.cs`
- `server/Controllers/CreatorScheduleController.cs`
- `server/Controllers/ProfileController.cs`
- `server/Services/RevokedTokenService.cs`

## 覆蓋檔案

- `client/src/App.tsx`
- `client/src/api/axios.ts`
- `client/src/auth/AuthContext.ts`
- `client/src/auth/AuthProvider.tsx`
- `client/src/components/Header/Header.css`
- `client/src/components/Header/Header.tsx`
- `client/src/pages/Profile/Profile.css`
- `client/src/pages/Profile/Profile.tsx`
- `client/src/services/authService.ts`
- `server/Controllers/AuthController.cs`
- `server/DTOs/AuthDTOs.cs`
- `server/Models/Entities/CreatorProfiles.cs`
- `server/Program.cs`
- `server/Repositories/AuthReopnsitory.cs`
- `server/Repositories/IAuthReopnsitory.cs`
- `server/Services/AuthService.cs`
- `server/Services/IAuthService.cs`
- `server/Services/IJwtService.cs`
- `tests/Server.Tests/AuthFlowTests.cs`
- `tests/Server.Tests/Server.Tests.csproj`

## 新增檔案

- `client/src/components/Header/WorkStatus.tsx`
- `client/src/components/Toast.css`
- `client/src/components/Toast.tsx`
- `client/src/components/ToastContext.ts`
- `client/src/components/ToastProvider.tsx`

ZIP 的 screenshots/ 為桌面與手機實際測試畫面，不必放進專案。manifest.json 列出檔案與 SHA-256，方便核對。
