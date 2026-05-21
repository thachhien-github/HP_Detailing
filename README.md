# HP Detailing — Hệ thống quản lý xưởng Detailing

Ứng dụng web **ASP.NET Core 8 MVC** quản lý phiếu dịch vụ, kho vật tư, hóa đơn, lịch hẹn và nhân sự cho xưởng chăm sóc xe (detailing). Giao diện Tailwind CSS, dữ liệu **SQL Server** qua Entity Framework Core.

## Yêu cầu

| Thành phần | Phiên bản |
|------------|-----------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 8.0+ |
| SQL Server hoặc LocalDB | 2019+ / LocalDB |
| (Tuỳ chọn) Visual Studio 2022 | 17.8+ |

## Cài đặt nhanh

### 1. Clone / mở solution

```bash
cd HP_Detailing
```

Mở `HP_Detailing.sln` trong Visual Studio hoặc chạy lệnh tại thư mục project.

### 2. Cấu hình database

Sửa connection string trong `appsettings.json` (hoặc `appsettings.Development.json`):

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER;Database=HP_Detailing_DB;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=true"
}
```

Ví dụ LocalDB:

```json
"DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=HP_Detailing_DB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

> **Lưu ý:** Không commit mật khẩu production lên git. Dùng User Secrets hoặc biến môi trường cho môi trường thật.

### 3. Migration & chạy ứng dụng

Khi chạy lần đầu, `Program.cs` tự **migrate** CSDL và **seed** dữ liệu mẫu (nếu chưa có phiếu dịch vụ).

```bash
dotnet restore
dotnet build
dotnet run
```

Hoặc tạo migration thủ công (khi đổi model):

```bash
dotnet ef migrations add TenMigration --context HP_DetailingDbContext
dotnet ef database update --context HP_DetailingDbContext
```

### 4. Truy cập

| Mục | Giá trị |
|-----|---------|
| URL mặc định | https://localhost:7xxx hoặc http://localhost:5xxx (xem console khi `dotnet run`) |
| Đăng nhập demo | **admin_hpd** / **admin123** |
| Trang login | `/login` |

## Luồng nghiệp vụ chính

```
Lịch hẹn → Tạo phiếu DV → Trừ kho (định mức) → Hóa đơn UNPAID → Thanh toán PAID
                ↑
         Hồ sơ xe (bảng Car)
```

- **Phiếu dịch vụ:** tạo xe, chọn dịch vụ, xuất vật tư, cập nhật tiến độ.
- **Kho:** nhập kho, kiểm kê, nhật ký xuất từ `TicketMaterialUsage`.
- **Tài chính:** danh sách hóa đơn, xác nhận thanh toán (phương thức + thời gian).
- **Cài đặt:** quản lý định mức vật tư theo dịch vụ (`ServiceMaterialQuota`).

## Quy trình vật tư & định mức (3 bước tách bạch)

| Bước | Màn hình | Việc làm | Bảng liên quan |
|------|----------|----------|----------------|
| **0. Cấu hình** | `/catalog/quotas` (menu Danh mục dịch vụ) | Gắn Dịch vụ ↔ Vật tư + số lượng mặc định | `ServiceMaterialQuotas` |
| **1. Nhập kho** | `/warehouse/imports/new` | Mua hàng về → **tăng tồn** | `StockImports`, `WarehouseStocks` |
| **2. Xuất kho** | Tạo / sửa phiếu DV | Làm dịch vụ → **giảm tồn** | `TicketMaterialUsages` |

Logic xuất kho tập trung tại `Data/TicketMaterialService.cs` (dùng chung cho **tạo phiếu** và **thêm dịch vụ**).

### Tạo phiếu mới (`/tickets/new`)

1. Chọn dịch vụ → UI gọi `GET /tickets/service-quotas` (xem trước định mức).
2. User có thể sửa số lượng / thêm vật tư **ngoài danh mục**.
3. `POST /Tickets/CreateAjax`:
   - Lưu `Ticket` + `TicketService`.
   - **Server** áp định mức từ DB (`ApplyQuotasForServices`) — SL/giá lấy từ form nếu đã chỉnh.
   - Vật tư `IsExtra=true` → `ApplyExtraMaterials`.
   - `InvoiceSync` → hóa đơn UNPAID.

### Thêm dịch vụ trên phiếu (`/tickets/{id}`)

1. `POST /Tickets/AddService` → lưu `TicketService`.
2. **Server** áp định mức của dịch vụ đó (`ApplyQuotasForService`).
3. `InvoiceSync` cập nhật tổng tiền.

> **Lưu ý:** Nếu chưa cấu hình định mức tại `/catalog/quotas`, thêm dịch vụ sẽ **không** tự trừ kho.

## Cấu trúc thư mục

```
HP_Detailing/
├── Controllers/      # MVC + API AJAX
├── Data/             # DbContext, DbInitializer, InvoiceSync, TicketMaterialService
├── Models/           # Entities + ViewModels
├── Migrations/       # EF Core migrations
├── Views/            # Razor (Tailwind)
├── wwwroot/          # CSS, JS (hp-ui.js)
├── Program.cs
└── Startup.cs        # DI, routing, auth cookie
```

## Các route chính

| Đường dẫn | Chức năng |
|-----------|-----------|
| `/` | Dashboard |
| `/tickets`, `/tickets/new`, `/tickets/{id}` | Phiếu dịch vụ |
| `/appointments` | Lịch hẹn |
| `/cars` | Hồ sơ xe |
| `/warehouse`, `/warehouse/imports` | Kho & nhập kho |
| `/financial`, `/financial/{id}` | Hóa đơn |
| `/staff`, `/staff/{id}` | Nhân sự |
| `/analytics` | Báo cáo |
| `/catalog/services`, `/catalog/quotas` | Danh mục dịch vụ & định mức vật tư |
| `/settings` | Cấu hình hệ thống (logo, TK, thanh toán, máy in) |
| `/login` | Đăng nhập |

## Dữ liệu mẫu (seed)

Lần chạy đầu (DB trống phiếu DV), hệ thống tạo mẫu:

- 5 danh mục / dịch vụ / nhân viên / vật tư / phiếu / hóa đơn
- 5 định mức vật tư–dịch vụ (`ServiceMaterialQuota`)
- 5 hồ sơ xe (`Car`)
- Phương thức thanh toán (Vietcombank, MoMo, tiền mặt, …)

DB đã có dữ liệu cũ: app vẫn **backfill** định mức, xe, nhật ký vật tư và đồng bộ hóa đơn UNPAID khi khởi động.

## Xử lý sự cố

| Triệu chứng | Gợi ý |
|-------------|--------|
| Lỗi kết nối SQL | Kiểm tra server đang chạy, tên DB, firewall, `TrustServerCertificate=True` |
| Migrate thất bại | Xóa DB test và chạy lại, hoặc `dotnet ef database update` |
| Trang trắng sau login | Xem log console; thường do CSDL chưa migrate |
| Kho không có nhật ký xuất | Tạo phiếu mới có vật tư; dữ liệu cũ dùng `MaterialUsage` sẽ được backfill sang `TicketMaterialUsage` |

## Build Release

```bash
dotnet build -c Release
```

## Giai đoạn tiếp theo (Phase 2+)

- CRUD dịch vụ / phương thức thanh toán trên Settings (một phần UI còn mock)
- Đăng nhập thật (bảng User, phân quyền KTV / Thu ngân)
- User Secrets cho connection string production
- CI/CD & unit test

Chi tiết tiến độ: xem [TODO.md](TODO.md).

## License / học tập

Dự án phục vụ môn **ASP.NET nâng cao** — HP Auto Detailing (demo).
