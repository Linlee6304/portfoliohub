PortfolioHub 天藍色首頁與帳號功能更新包
基準版本：ec15edc（前端基底）
製作日期：2026-09-19

一、怎麼套用
1. 解壓這個 ZIP。
2. 將包內 client、server、tests 資料夾與 .gitignore 複製至你的 portfoliohub 專案根目錄。
3. 合併同名資料夾，選擇覆蓋同名檔案。這是更新包，請保留專案中其他原有檔案。
4. 必須刪除的檔案：無。
5. 新增、覆蓋的完整清單在下方。manifest.json 附有每個程式檔案的 SHA-256。
6. 本包沒有包含 appsettings、登入密碼、資料庫、node_modules、bin、obj 或其他建置產物。
7. 新版登入憑證多了密碼版本驗證，上版已登入的使用者套用後需重新登入。

二、完成的功能
- 天藍色首頁版型，左側導覽可收合；最上方顯示當前頁面名稱。
- 右上角未登入顯示登入／註冊；登入後顯示名稱、頭像。
- 點名稱展開修改密碼、基本資料、登出。支援 Escape、點擊外部關閉與鍵盤操作。
- 登入、註冊、基本資料載入與儲存、修改密碼、登出已接 API。
- 頭像使用圖片網址；圖片無法載入時顯示名稱首字。
- 基本資料是創作者資料；管理員可檢視帳號資料及修改密碼。
- 登出失敗保留登入狀態與錯誤訊息，可重試；不會假裝已登出。
- 登入存於目前分頁的 sessionStorage，重整後呼叫 /api/Auth/me 確認。
- 首頁主內容保留空白，作品列表顯示準備中。本次未新增作品 CRUD 功能。
- 手機版預設收合側欄，展開後可選頁面並自動收起。

三、後端 API（均位於 AuthController）
POST /api/Auth/login
  JSON：{ "email": "...", "password": "..." }
  管理員及創作者皆回傳 token、tokenExpiresAt、displayName、avatarUrl、role 等。
POST /api/Auth/register
  JSON：{ "email": "...", "password": "...", "confirmPassword": "...", "displayName": "..." }
GET /api/Auth/me
  取得目前登入者基本資料。
PUT /api/Auth/profile
  JSON：{ "email": "...", "displayName": "...", "contactPhone": "...",
           "avatarUrl": "https://...", "bio": "...", "workStatus": 0 }
  workStatus：0 不接案、1 接案中、2 可接案。
POST /api/Auth/change-password
  JSON：{ "currentPassword": "...", "newPassword": "...", "confirmNewPassword": "..." }
POST /api/Auth/logout
  不需 request body。
  成功：200，{ "success": true, "message": "已登出" }。
  登出後再使用同一憑證：401。

除登入／註冊外，以上 API 都需要：
Authorization: Bearer <token>

登出機制：
- 用 JWT 的 jti 撤銷當次登入，其他獨立登入不受影響。
- 撤銷紀錄存入既有 AspNetUserTokens 資料表，伺服器重啟後仍有效。
- 無須新增資料表或執行新的 migration；需要原有 Identity 資料表已存在。
- JWT 每次驗證都會檢查資料庫中的撤銷紀錄及 SecurityStamp。
- 修改密碼後更新 SecurityStamp，該帳號舊的登入憑證全部失效。
- 更新基本資料及修改密碼一律使用憑證中的使用者 ID，不信任前端傳入的 ID。
- 兩小時有效期維持原設定。過期撤銷紀錄會在該使用者下次登出時清理。

四、如何啟動
環境：Node.js 22.12 以上（或 24 LTS）、.NET 10 正式版 SDK、原本的 SQL Server。

保留你原本的資料庫連線、JWT Key／Issuer／Audience 及管理員設定。
後端啟動原本就會建立 Admin／Creator 角色與預設管理員，請沿用原先配置。
必要設定鍵：
ConnectionStrings:DefaultConnection
Jwt:Key（至少 32 bytes，使用隨機值）
Jwt:Issuer
Jwt:Audience
Admin:Email
Admin:Password

在專案根目錄執行：
dotnet restore server/server.csproj
dotnet dev-certs https --trust
dotnet run --project server/server.csproj --launch-profile https

再開一個終端機：
cd client
npm ci
npm run dev

開啟前端顯示的 localhost 網址（預設 http://localhost:5173）。
Vite 已將 /api 代理至 https://localhost:7051，符合原本 launchSettings。
如果你的後端位址不同，可把 client/.env.example 複製成 client/.env.local，
修改 API_PROXY_TARGET，再重啟前端。此變數只控制開發代理。
正式部署請將前端與 /api 放在同一個網域；伺服器需支援 SPA 路由回退。
本包沒有進行網站部署。

五、驗證
已通過：
- 前端正式編譯：npm run build
- 前端程式檢查：npm run lint
- 瀏覽器操作：側欄、跳頁、登入錯誤、登入後轉回受保護頁面、重整還原登入、
  基本資料儲存與名稱更新、下拉選單、登出失敗與重試、註冊、修改密碼後重登入。
- 320／375／768／1365 像素寬度無橫向溢出，瀏覽器無程式執行錯誤。
- 後端 6 項整合測試：
  1. 管理員與創作者均拿到可使用的憑證。
  2. 登出只撤銷當次憑證，且伺服器重啟後仍維持撤銷。
  3. 未帶憑證及密碼錯誤被拒絕。
  4. 基本資料不能冒用別人的 ID，變更信箱後登入仍有效。
  5. 修改密碼使所有舊登入失效。
  6. 重複信箱及不安全的頭像網址被拒絕。

後端測試使用隔離的 SQLite 測試資料庫，沒有存取你的 SQL Server。
瀏覽器操作測試使用測試 API 回應；實際 API 邏輯由上述後端整合測試驗證。
實際 SQL Server 連線與你現有帳號資料，需在你的環境啟動後確認。
後端有一項原有的 AuthReopnsitory.cs 可空值編譯警告，未阻止編譯或測試。

重跑後端測試：
dotnet test tests/Server.Tests/Server.Tests.csproj

previews/ 為這次正式前端的檢查截圖，登入後畫面使用測試帳號資料。
已將原有 Microsoft.OpenApi 相依套件固定到 2.7.5，處理還原套件時檢出的安全公告：
https://github.com/advisories/GHSA-v5pm-xwqc-g5wc
SQLite 僅用於測試，測試專案使用更新後的 SQLitePCLRaw.bundle_e_sqlite3 2.1.13。

六、覆蓋檔案（26 個）
client/index.html
client/public/favicon.svg
client/src/App.css
client/src/App.tsx
client/src/api/axios.ts
client/src/components/Header/Header.css
client/src/components/Header/Header.tsx
client/src/components/ProtectedRoute/ProtectedRoute.tsx
client/src/index.css
client/src/main.tsx
client/src/pages/Auth/Login.tsx
client/src/pages/Auth/Register.tsx
client/src/pages/Home/Home.css
client/src/pages/Home/Home.tsx
client/src/pages/Profile/Profile.tsx
client/src/pages/Works/WorkList.tsx
client/src/router/index.tsx
client/src/services/authService.ts
client/vite.config.ts
server/Controllers/AuthController.cs
server/DTOs/AuthDTOs.cs
server/Program.cs
server/Services/AuthService.cs
server/Services/IAuthService.cs
server/Services/JwtService.cs
server/server.csproj

七、新增檔案（9 個）
.gitignore
client/.env.example
client/src/auth/AuthContext.ts
client/src/auth/AuthProvider.tsx
client/src/components/Icon.tsx
client/src/pages/Auth/ChangePassword.tsx
server/Services/RevokedTokenService.cs
tests/Server.Tests/AuthFlowTests.cs
tests/Server.Tests/Server.Tests.csproj

八、刪除檔案
無。
