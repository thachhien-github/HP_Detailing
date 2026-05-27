# HỆ THỐNG QUẢN LÝ TRUNG TÂM CHĂM SÓC XE HƠI  
## HP AUTO DETAILING MANAGEMENT SYSTEM

> **Phiên bản:** 1.0 — Production Release  
> **Ngày phát hành:** 28/05/2026  
> **Công nghệ chính:** ASP.NET Core 8.0 MVC · SQL Server · Tailwind CSS · SignalR  

---

## MỤC LỤC

- [Phần 1: Mô tả Hiện trạng](#phần-1-mô-tả-hiện-trạng)
- [Phần 2: Phân tích](#phần-2-phân-tích)
- [Phần 3: Thiết kế](#phần-3-thiết-kế)
- [Phần 4: Cài đặt Ứng dụng](#phần-4-cài-đặt-ứng-dụng)
- [Phần 5: Kết luận – Kiến nghị](#phần-5-kết-luận--kiến-nghị)

---

# Phần 1: MÔ TẢ HIỆN TRẠNG

## 1.1. Giới thiệu đề tài

Hệ thống **HP Auto Detailing** được xây dựng nhằm tin học hóa toàn bộ quy trình vận hành của một Trung tâm Chăm sóc – Detailing xe hơi chuyên nghiệp, từ khâu tiếp nhận khách hàng, quản lý phiếu dịch vụ, phân công kỹ thuật viên, kiểm soát kho vật tư, đến thanh toán và xuất hóa đơn.

## 1.2. Hiện trạng trước khi có hệ thống

| Vấn đề | Mô tả |
|---|---|
| **Quản lý thủ công** | Phiếu dịch vụ, tồn kho, lương nhân viên được ghi chép trên sổ sách hoặc Excel rời rạc |
| **Mất kiểm soát tồn kho** | Không thể biết chính xác lượng hóa chất, vật tư còn lại tại từng thời điểm |
| **Không có Dashboard** | Chủ doanh nghiệp không có cái nhìn tổng quan về doanh thu, hiệu suất theo thời gian thực |
| **Phân quyền thủ công** | Mọi nhân viên đều truy cập được mọi chức năng, gây rủi ro bảo mật |
| **Liên lạc nội bộ chậm** | Phối hợp giữa Lễ tân – Quản đốc – Kỹ thuật viên phải gọi điện hoặc chạy đi báo trực tiếp |

## 1.3. Mục tiêu của hệ thống

1. **Số hóa toàn bộ quy trình** từ tiếp nhận xe → thi công → nghiệm thu → thanh toán.
2. **Quản lý kho vật tư** chặt chẽ với tính năng nhập kho, xuất kho tự động, kiểm kê định kỳ.
3. **Dashboard thời gian thực** cho phép chủ doanh nghiệp theo dõi doanh thu, số xe đang xử lý.
4. **Phân quyền bảo mật** dựa trên vai trò (Admin, Quản đốc, Lễ tân, Kỹ thuật viên, Kho).
5. **Thông báo tức thì** qua SignalR khi phiếu dịch vụ thay đổi trạng thái.

---

# Phần 2: PHÂN TÍCH

## 2.1. Sơ đồ Use-Case tổng quát

```
                        ┌─────────────────────────────────────────────────┐
                        │          HỆ THỐNG HP AUTO DETAILING             │
                        │                                                 │
    ┌──────────┐        │  ┌──────────────────────────────────────────┐   │
    │          │        │  │         QUẢN LÝ PHIẾU DỊCH VỤ            │   │
    │  KHÁCH   │───────►│  │  • Tạo phiếu mới (Tiếp nhận xe)          │   │
    │  HÀNG    │        │  │  • Chọn dịch vụ & Báo giá                │   │
    │ (Guest)  │        │  │  • Theo dõi trạng thái phiếu             │   │
    └──────────┘        │  │  • In hóa đơn & Thanh toán               │   │
         │              │  └──────────────────────────────────────────┘   │
         │ Đặt lịch     │                                                 │
         ▼              │  ┌──────────────────────────────────────────┐   │
    ┌──────────┐        │  │         QUẢN LÝ LỊCH HẸN                 │   │
    │  LỄ TÂN  │───────►│  │  • Tạo / Sửa / Xóa lịch hẹn              │   │
    │          │        │  │  • Xác nhận / Hủy lịch hẹn               │   │
    └──────────┘        │  │  • Xem lịch hẹn theo ngày                │   │
         │              │  └──────────────────────────────────────────┘   │
         │              │                                                 │
         ▼              │  ┌──────────────────────────────────────────┐   │
    ┌──────────┐        │  │         PHÂN CÔNG & THI CÔNG             │   │
    │ QUẢN ĐỐC │───────►│  │  • Phân công KTV cho phiếu               │   │
    │          │        │  │  • Giám sát tiến độ thi công             │   │
    └──────────┘        │  │  • Nghiệm thu chất lượng (QC)            │   │
         │              │  └──────────────────────────────────────────┘   │
         │              │                                                 │
         ▼              │  ┌──────────────────────────────────────────┐   │
    ┌──────────┐        │  │         QUẢN LÝ KHO VẬT TƯ               │   │
    │   KTV    │        │  │  • Nhập kho (PO) từ nhà cung cấp         │   │
    │  & KHO   │───────►│  │  • Xuất kho tự động theo phiếu DV        │   │
    │          │        │  │  • Định mức vật tư theo dịch vụ          │   │
    └──────────┘        │  │  • Kiểm kê kho định kỳ                   │   │
                        │  └──────────────────────────────────────────┘   │
                        │                                                 │
    ┌──────────┐        │  ┌──────────────────────────────────────────┐   │
    │  ADMIN   │───────►│  │         QUẢN TRỊ HỆ THỐNG                │   │
    │          │        │  │  • Dashboard doanh thu & thống kê        │   │
    │          │        │  │  • Quản lý nhân sự & phân quyền          │   │
    │          │        │  │  • Danh mục dịch vụ & Bảng giá           │   │
    │          │        │  │  • Phân tích & Báo cáo                   │   │
    └──────────┘        │  │  • Cài đặt hệ thống                      │   │
                        │  └──────────────────────────────────────────┘   │
                        └─────────────────────────────────────────────────┘
```

## 2.2. Danh sách Actors (Tác nhân)

| Actor | Vai trò | Quyền truy cập chính |
|---|---|---|
| **Khách hàng (Guest)** | Người dùng chưa đăng nhập | Xem trang chủ công khai, bảng giá dịch vụ, đặt lịch hẹn online |
| **Lễ tân (Receptionist)** | Nhân viên tiếp nhận | Tạo phiếu DV, quản lý lịch hẹn, in hóa đơn, thanh toán |
| **Quản đốc (Foreman)** | Giám sát thi công | Phân công KTV, giám sát tiến độ, nghiệm thu QC |
| **Kỹ thuật viên (Technician)** | Thực hiện dịch vụ | Xem phiếu được giao, cập nhật trạng thái thi công |
| **Thủ kho (Warehouse)** | Quản lý kho vật tư | Nhập kho, xuất kho, kiểm kê, quản lý nhà cung cấp |
| **Admin** | Quản trị toàn quyền | Toàn bộ chức năng + Cài đặt hệ thống + Phân quyền |

## 2.3. Danh sách Use-Case chi tiết

### Nhóm UC: Quản lý Phiếu dịch vụ (Tickets)
| Mã UC | Tên Use-Case | Mô tả |
|---|---|---|
| UC-01 | Tạo phiếu dịch vụ | Lễ tân tạo phiếu mới, ghi nhận biển số xe, thông tin khách, chọn dịch vụ |
| UC-02 | Chọn & gắn dịch vụ | Thêm/xóa dịch vụ vào phiếu, hệ thống tự tính tổng tiền |
| UC-03 | Phân công KTV | Quản đốc chọn KTV phù hợp cho phiếu dịch vụ |
| UC-04 | Cập nhật trạng thái | Chuyển trạng thái: Chờ → Đang thi công → Hoàn thành → Chờ thanh toán |
| UC-05 | Nghiệm thu QC | Quản đốc kiểm tra chất lượng, duyệt hoàn thành |
| UC-06 | Xuất hóa đơn & Thanh toán | Lễ tân in hóa đơn, chọn phương thức thanh toán, xác nhận thu tiền |

### Nhóm UC: Quản lý Kho vật tư (Warehouse)
| Mã UC | Tên Use-Case | Mô tả |
|---|---|---|
| UC-07 | Nhập kho | Tạo phiếu nhập kho từ nhà cung cấp, ghi nhận số lượng & đơn giá |
| UC-08 | Xuất kho tự động | Khi phiếu DV bắt đầu thi công, hệ thống tự trừ kho theo định mức |
| UC-09 | Kiểm kê kho | Thủ kho thực hiện kiểm kê, đối soát hệ thống vs thực tế |
| UC-10 | Quản lý định mức | Admin cấu hình lượng vật tư tiêu hao mặc định cho từng dịch vụ |

### Nhóm UC: Quản lý Nhân sự & Hệ thống
| Mã UC | Tên Use-Case | Mô tả |
|---|---|---|
| UC-11 | Quản lý nhân viên | CRUD nhân viên, hồ sơ, hợp đồng lao động |
| UC-12 | Phân quyền tài khoản | Gán vai trò (Admin, Lễ tân, KTV...) cho tài khoản đăng nhập |
| UC-13 | Cài đặt hệ thống | Cấu hình theme, phương thức thanh toán, danh mục dịch vụ |
| UC-14 | Dashboard & Báo cáo | Xem doanh thu, số phiếu, biểu đồ phân tích theo thời gian |

## 2.4. Sơ đồ Quy trình Thực hiện Dịch vụ (Process Flow)

```
 ┌─────────────────┐
 │ KHÁCH HÀNG ĐẾN  │
 │   TRUNG TÂM     │
 └────────┬────────┘
          │
          ▼
 ┌─────────────────┐     ┌─────────────────────────────────────┐
 │ BƯỚC 1: LỄ TÂN  │     │ • Chụp ảnh tình trạng xe            │
 │ Tiếp nhận &     │────►│ • Tạo Phiếu dịch vụ (PDV-xxxx)      │
 │ Tạo phiếu       │     │ • Chốt báo giá với khách hàng       │
 └────────┬────────┘     └─────────────────────────────────────┘
          │
          │  ♦ Push Notification → Quản đốc
          ▼
 ┌─────────────────┐     ┌─────────────────────────────────────┐
 │ BƯỚC 2: QUẢN ĐỐC│     │ • Xem danh sách phiếu mới           │
 │ Phân công       │────►│ • Chọn KTV phù hợp → Giao việc      │
 └────────┬────────┘     └─────────────────────────────────────┘
          │
          │  ♦ Push Notification → KTV & Thủ kho
          ▼
 ┌─────────────────┐     ┌─────────────────────────────────────┐
 │ BƯỚC 3A: KTV    │     │ • Nhận phiếu trên App               │
 │ Thi công        │────►│ • Thực hiện dịch vụ                 │
 │                 │     │ • Bấm "Hoàn thành" khi xong         │
 └─────────────────┘     └─────────────────────────────────────┘
 ┌─────────────────┐     ┌─────────────────────────────────────┐
 │ BƯỚC 3B: KHO    │     │ • Đối chiếu phiếu DV                │
 │ Xuất vật tư     │────►│ • Duyệt xuất hóa chất & linh kiện   │
 └─────────────────┘     └─────────────────────────────────────┘
          │
          │  ♦ Push Notification → Quản đốc
          ▼
 ┌─────────────────┐     ┌─────────────────────────────────────┐
 │ BƯỚC 4: QUẢN ĐỐC│     │ • Kiểm tra chất lượng (QC)          │
 │ Nghiệm thu      │────►│ • Nếu đạt → Chuyển "Chờ thanh toán" │
 └────────┬────────┘     └─────────────────────────────────────┘
          │
          │  ♦ Cho phép Lễ tân thu tiền
          ▼
 ┌─────────────────┐     ┌─────────────────────────────────────┐
 │ BƯỚC 5: LỄ TÂN  │     │ • In hóa đơn, áp dụng khuyến mãi    │
 │ Thanh toán &    │────►│ • Nhận thanh toán (Tiền mặt / CK)   │
 │ Bàn giao xe     │     │ • Bàn giao chìa khóa xe             │
 └────────┬────────┘     └─────────────────────────────────────┘
          │
          ▼
 ┌─────────────────┐
 │ KHÁCH HÀNG      │
 │   NHẬN XE       │
 └─────────────────┘
```

---

# Phần 3: THIẾT KẾ

## 3.1. Kiến trúc hệ thống

Hệ thống áp dụng mô hình kiến trúc **MVC (Model – View – Controller)** chuẩn của ASP.NET Core:

```
┌──────────────────────────────────────────────────────────┐
│                     CLIENT (Browser)                     │
│         Tailwind CSS · Lucide Icons · SignalR JS         │
└─────────────────────────┬────────────────────────────────┘
                          │ HTTPS
                          ▼
┌──────────────────────────────────────────────────────────┐
│                  ASP.NET Core 8.0 MVC                    │
│  ┌────────────┐  ┌─────────────┐  ┌───────────────────┐  │
│  │ Controllers│  │    Views    │  │  Static Files     │  │
│  │ (C# Logic) │  │ (.cshtml)   │  │ (wwwroot/js,css)  │  │
│  └─────┬──────┘  └─────────────┘  └───────────────────┘  │
│        │                                                 │
│  ┌─────▼──────┐  ┌─────────────┐  ┌───────────────────┐  │
│  │   Models   │  │    Data     │  │   Hubs (SignalR)  │  │
│  │ (Entities) │  │ (DbContext) │  │ (Real-time Notif) │  │
│  └─────┬──────┘  └──────┬──────┘  └───────────────────┘  │
│        │                │                                │
└────────┼────────────────┼────────────────────────────────┘
         │                │
         ▼                ▼
┌──────────────────────────────────────────────────────────┐
│         SQL Server (Entity Framework Core 8.0)           │
│         + ASP.NET Core Identity (Auth & Roles)           │
└──────────────────────────────────────────────────────────┘
```

## 3.2. Công nghệ sử dụng

| Tầng | Công nghệ | Phiên bản | Mục đích |
|---|---|---|---|
| **Backend** | ASP.NET Core MVC | 8.0 | Framework chính xử lý nghiệp vụ |
| **ORM** | Entity Framework Core | 8.0.8 | Ánh xạ đối tượng – cơ sở dữ liệu |
| **Database** | SQL Server | LocalDB / SQL Server | Lưu trữ dữ liệu |
| **Auth** | ASP.NET Core Identity | 8.0 | Xác thực & Phân quyền theo vai trò |
| **Real-time** | SignalR | 8.0 | Thông báo tức thì (Push Notification) |
| **Frontend** | Tailwind CSS (CDN v3) | 3.x | Thiết kế giao diện Darkmode hiện đại |
| **Icons** | Lucide Icons | Latest | Bộ icon SVG nhẹ, sắc nét |
| **Typography** | Google Fonts | — | Inter, Space Grotesk, JetBrains Mono |
| **Compression** | Brotli + Gzip | Built-in | Nén response giảm 50–70% dung lượng |

### 3.2.1. Kỹ thuật nổi bật đã triển khai

Project này không chỉ là một hệ thống CRUD thông thường mà đã được đầu tư theo hướng kiến trúc hiện đại, có thể flex tốt với yêu cầu phát triển dài hạn:

1. **Code First + Migrations**  
   - Toàn bộ schema được xây dựng từ code C# thông qua EF Core `DbContext` và các migration file.
   - Hỗ trợ cập nhật database theo phiên bản một cách kiểm soát, dễ maintain và deploy.
   - Có tính năng tự động migrate + seed dữ liệu mẫu khi khởi động ứng dụng.

2. **ASP.NET Core Identity + Role-Based Authorization**  
   - Tích hợp xác thực người dùng và phân quyền theo vai trò như `Admin`, `Receptionist`, `Foreman`, `Technician`, `Warehouse`.
   - Hỗ trợ kiểm soát truy cập theo từng màn hình, đảm bảo bảo mật và dễ mở rộng.

3. **RESTful MVC + API-friendly structure**  
   - Hệ thống được thiết kế theo hướng MVC chuẩn, đồng thời các thao tác nghiệp vụ được tổ chức rõ ràng, dễ mở rộng thành REST API trong tương lai.
   - Các controller được tách theo module nghiệp vụ: `Tickets`, `Warehouse`, `Financial`, `Appointments`, `Analytics`, ...

4. **SignalR Real-Time Notification**  
   - Tích hợp `NotificationHub` để gửi thông báo tức thì cho các vai trò liên quan khi trạng thái phiếu thay đổi.
   - Đây là điểm nhấn kỹ thuật giúp hệ thống tương tác tốt hơn, gần với môi trường doanh nghiệp thực tế.

5. **Dependency Injection & Service Layer**  
   - Sử dụng DI chuẩn của ASP.NET Core, cấu hình service ở `Startup.cs` / `Program.cs` giúp code dễ test, dễ mở rộng và tách biệt logic.
   - Các nghiệp vụ phức tạp như xuất kho, đồng bộ hóa đơn, khởi tạo dữ liệu được xử lý qua service riêng.

6. **EF Core + LINQ + Business Logic Mapping**  
   - Tận dụng EF Core và LINQ để xử lý truy vấn dữ liệu hiệu quả, kết hợp với các ViewModel cho tầng presentation.
   - Hỗ trợ tối ưu truy vấn, giảm độ ràng buộc giữa dữ liệu và giao diện.

7. **Production-ready Architecture**  
   - Có cấu trúc mô-đun rõ ràng, hỗ trợ triển khai production, caching, compression, và dễ tích hợp thêm tính năng như SMS, payment gateway, báo cáo Excel/PDF trong giai đoạn sau.

## 3.3. Sơ đồ Cơ sở dữ liệu (Database Schema)

### Nhóm: Nhân sự
| Bảng | Mô tả | Quan hệ |
|---|---|---|
| `AppUser` | Tài khoản đăng nhập (kế thừa IdentityUser) | → Staff (1:1) |
| `Staff` | Hồ sơ nhân viên | → Position, StaffProfile, LaborContract, Payroll |
| `Position` | Chức vụ (Quản đốc, KTV, Lễ tân...) | ← Staff (1:N) |
| `StaffProfile` | Thông tin mở rộng (CCCD, dân tộc...) | → Staff (1:1) |
| `LaborContract` | Hợp đồng lao động | → Staff (N:1) |
| `Payroll` | Bảng lương tháng | → Staff (N:1) |

### Nhóm: Dịch vụ & Phiếu
| Bảng | Mô tả | Quan hệ |
|---|---|---|
| `ServiceCategory` | Danh mục nhóm dịch vụ | ← Service (1:N) |
| `Service` | Dịch vụ đơn lẻ (Rửa xe, Phủ Ceramic...) | → ServiceCategory |
| `Car` | Thông tin xe (Biển số = PK) | ← Ticket (1:N) |
| `Ticket` | Phiếu dịch vụ chính | → Car, Staff; ← TicketService, TicketMaterialUsage |
| `TicketService` | Dịch vụ trong phiếu | → Ticket, Service |
| `TicketMaterialUsage` | Vật tư xuất kho cho phiếu | → Ticket, Material |

### Nhóm: Tài chính
| Bảng | Mô tả | Quan hệ |
|---|---|---|
| `Invoice` | Hóa đơn thanh toán | → Ticket, PaymentMethod |
| `InvoiceService` | Chi tiết dòng hóa đơn | → Invoice |
| `PaymentMethod` | Phương thức thanh toán (Ngân hàng, Tiền mặt) | ← Invoice |

### Nhóm: Kho vật tư
| Bảng | Mô tả | Quan hệ |
|---|---|---|
| `Material` | Danh mục vật tư / hóa chất | ← WarehouseStock, StockImportItem |
| `WarehouseStock` | Tồn kho hiện tại | → Material (1:1) |
| `StockImport` | Phiếu nhập kho | → Supplier; ← StockImportItem |
| `StockImportItem` | Chi tiết dòng nhập kho | → StockImport, Material |
| `Supplier` | Nhà cung cấp | ← StockImport |
| `ServiceMaterialQuota` | Định mức vật tư theo dịch vụ | → Service, Material |
| `AuditSession` | Phiên kiểm kê kho | ← AuditSessionItem |
| `AuditSessionItem` | Chi tiết kiểm kê từng vật tư | → AuditSession, Material |

### Nhóm: Hệ thống
| Bảng | Mô tả |
|---|---|
| `Appointment` | Lịch hẹn đặt chỗ |
| `Notification` | Thông báo hệ thống (real-time via SignalR) |

## 3.4. Cấu trúc thư mục dự án

```
HP_Detailing/
├── Controllers/            # 14 Controller xử lý nghiệp vụ
│   ├── AccountController       # Đăng nhập, Đăng xuất, Quên mật khẩu
│   ├── HomeController          # Dashboard (có/không đăng nhập)
│   ├── TicketsController       # CRUD Phiếu dịch vụ (phức tạp nhất)
│   ├── WarehouseController     # Nhập/Xuất kho, Kiểm kê
│   ├── FinancialController     # Hóa đơn, Thanh toán
│   ├── StaffController         # Quản lý nhân sự
│   ├── AppointmentsController  # Quản lý lịch hẹn
│   ├── CarsController          # Quản lý xe
│   ├── AnalyticsController     # Báo cáo & Phân tích
│   ├── CatalogController       # Danh mục Dịch vụ, Vật tư, NCC
│   ├── SettingsController      # Cài đặt hệ thống
│   ├── ProfileController       # Hồ sơ cá nhân
│   ├── SupportController       # Trang trợ giúp
│   └── NotificationsController # API thông báo
├── Models/
│   ├── Entities.cs             # 20+ Entity classes (ORM mapping)
│   └── ViewModels/             # 11 ViewModel cho từng trang
├── Views/                      # 15 thư mục View (Razor .cshtml)
│   ├── Shared/
│   │   ├── _Layout.cshtml      # Layout chính (Darkmode + Theme)
│   │   ├── _Sidebar.cshtml     # Sidebar điều hướng (Role-based)
│   │   ├── _Header.cshtml      # Header bar
│   │   └── _PageHeader.cshtml  # Breadcrumb component
│   └── [Module]/Index.cshtml   # Trang chính mỗi module
├── Data/
│   ├── HP_DetailingDbContext.cs # EF Core DbContext
│   ├── DbInitializer.cs       # Seed Data (Tài khoản, Danh mục mặc định)
│   ├── InvoiceSync.cs         # Đồng bộ hóa đơn tự động
│   └── TicketMaterialService.cs# Service xuất kho theo phiếu
├── Hubs/
│   └── NotificationHub.cs     # SignalR Hub (Real-time)
├── Migrations/                 # EF Core Migration files
├── wwwroot/
│   ├── css/site.css            # CSS bổ sung
│   ├── js/hp-ui.js             # JavaScript UI framework (HPUI)
│   └── lib/                    # jQuery Validation (server-side)
├── Program.cs                  # Entry point
├── Startup.cs                  # DI, Middleware, Routing
└── appsettings.json            # Chuỗi kết nối & cấu hình
```

## 3.5. Thiết kế Giao diện (UI/UX)

Hệ thống sử dụng giao diện **Darkmode** hiện đại với các đặc trưng:

- **Color System**: CSS Variables (`--bg-base`, `--bg-card`, `--text-main`...) cho phép chuyển đổi Dark/Light mode mà không cần reload trang.
- **Primary Color**: Hỗ trợ 5 bảng màu chủ đạo (Blue, Emerald, Amber, Rose, Purple), lưu vào `localStorage`.
- **Typography**: Font Inter (body), Space Grotesk (headings), JetBrains Mono (code/số liệu).
- **Component Library**: `HPUI` — bộ thư viện JS tự xây dựng bao gồm: Toast notification, Tab switching, Modal, Sidebar toggle, Dropdown.
- **Responsive**: Hỗ trợ đầy đủ các breakpoint từ Mobile (sm) đến Desktop (lg).
- **Sidebar phân quyền**: Menu items được ẩn/hiện dựa trên `User.IsInRole()` trong Razor.

---

# Phần 4: CÀI ĐẶT ỨNG DỤNG

## 4.1. Yêu cầu hệ thống

| Thành phần | Yêu cầu tối thiểu |
|---|---|
| **Runtime** | .NET 8.0 SDK hoặc ASP.NET Core 8.0 Runtime |
| **Database** | SQL Server 2019+ hoặc SQL Server LocalDB |
| **OS** | Windows Server 2019+, hoặc Linux (Ubuntu 20.04+) |
| **RAM** | Tối thiểu 2GB (khuyến nghị 4GB) |
| **Web Server** | IIS 10+ (Windows) hoặc Nginx/Apache (Linux) |

## 4.2. Hướng dẫn cài đặt

### Bước 1: Clone mã nguồn
```bash
git clone <repository-url>
cd HP_Detailing
```

### Bước 2: Cấu hình chuỗi kết nối
Mở file `appsettings.json`, chỉnh sửa `DefaultConnection`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=HP_DetailingDb;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True"
  }
}
```

### Bước 3: Khôi phục packages & Build
```bash
dotnet restore
dotnet build -c Release
```

### Bước 4: Chạy ứng dụng
```bash
dotnet run
```
Hệ thống sẽ **tự động**:
- Tạo database nếu chưa tồn tại (Code-First Migration).
- Chạy `DbInitializer` để seed dữ liệu mẫu (tài khoản Admin, danh mục dịch vụ...).

### Bước 5: Truy cập
- Mở trình duyệt: `https://localhost:5001`
- Đăng nhập tài khoản Admin mặc định (được tạo bởi `DbInitializer`).

## 4.3. Publish lên Production

```bash
dotnet publish -c Release -o ./publish
```

Cấu hình biến môi trường trên Host:
```
ASPNETCORE_ENVIRONMENT=Production
```

## 4.4. Tối ưu hóa đã áp dụng

| Kỹ thuật | Chi tiết | Hiệu quả |
|---|---|---|
| **Response Compression** | Brotli + Gzip cho tất cả HTTP response | Giảm ~60% dung lượng tải |
| **Static Files Caching** | `Cache-Control: max-age=604800` (7 ngày) | Trang mở lại gần như tức thì |
| **AsNoTracking()** | Tắt change tracking cho các truy vấn đọc | Giảm RAM & CPU đáng kể |
| **Defer Script Loading** | Lucide Icons tải không chặn DOM | First Paint nhanh hơn |

---

# Phần 5: KẾT LUẬN – KIẾN NGHỊ

## 5.1. Kết quả đạt được

Hệ thống **HP Auto Detailing** đã hoàn thành đầy đủ các chức năng cốt lõi:

✅ **Quản lý phiếu dịch vụ** từ tiếp nhận → phân công → thi công → nghiệm thu → thanh toán  
✅ **Quản lý kho vật tư** chặt chẽ với nhập kho, xuất kho tự động, kiểm kê định kỳ  
✅ **Dashboard thời gian thực** lọc theo ngày/ca trực, hiển thị doanh thu và thống kê  
✅ **Phân quyền bảo mật** dựa trên vai trò ASP.NET Core Identity  
✅ **Thông báo tức thì** qua SignalR khi trạng thái phiếu thay đổi  
✅ **Giao diện Darkmode** chuyên nghiệp, responsive, hỗ trợ 5 bảng màu  
✅ **Tối ưu hiệu suất** với Response Compression và EF Core tuning  

## 5.2. Hạn chế

- Chưa tích hợp **gửi SMS/Email** tự động nhắc lịch hẹn cho khách hàng.
- Chưa có ứng dụng **Mobile App** riêng cho Kỹ thuật viên (hiện dùng web responsive).
- Chưa tích hợp **cổng thanh toán online** (VNPay, Momo).
- Báo cáo phân tích chưa hỗ trợ **xuất file Excel/PDF**.

## 5.3. Hướng phát triển

| Giai đoạn | Tính năng dự kiến |
|---|---|
| **Phase 2** | Tích hợp SMS Brandname (Twilio/VNPT) cho nhắc lịch bảo trì |
| **Phase 3** | Mobile App (React Native) cho KTV scan QR phiếu dịch vụ |
| **Phase 4** | Cổng thanh toán online VNPay/MoMo/ZaloPay |
| **Phase 5** | AI dự đoán nhu cầu nhập kho dựa trên lịch sử tiêu thụ vật tư |

---

> **© 2026 HP Auto Detailing.** Hệ thống được phát triển trên nền tảng ASP.NET Core 8.0 MVC.
