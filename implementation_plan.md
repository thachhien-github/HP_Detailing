# Kế hoạch triển khai chuẩn hóa — HP Auto Detailing

Dự án sử dụng **ASP.NET Core 8.0 MVC + EF Core + SQL Server**, UI dark-theme theo `HPUI.loadPage`.

> [!IMPORTANT]
> **Nguyên tắc xuyên suốt:** giữ nguyên giao diện hiện tại (layout, class, theme), chỉ thay đổi logic, API, dữ liệu và bổ sung paging/filter.

## 1) Rà soát rủi ro & thiếu sót trong bản kế hoạch cũ

### Rủi ro kỹ thuật chính
1. **Thiếu chuẩn API đồng nhất** giữa các module list (nơi có `summary`, nơi không) -> dễ lệch contract frontend.
2. **Thiếu tiêu chí Done (DoD)** từng phase -> hoàn thành “code xong” nhưng chưa chắc pass hành vi nghiệp vụ.
3. **Thiếu kế hoạch migration an toàn** (backup, rollback, kiểm thử sau migration), đặc biệt Phase 2/4/10/11.
4. **Identity migration rủi ro cao**: đổi kiến trúc auth giữa chừng, có thể làm hỏng đăng nhập toàn hệ thống nếu không tách bước.
5. **Notification chưa có fallback** khi SignalR fail (mất kết nối, reconnect) -> dễ mất thông báo phía UI.
6. **Phụ thuộc chéo chưa khóa rõ**: Profile phụ thuộc Identity; Staff Activities phụ thuộc Ticket assignment; nếu làm sai thứ tự sẽ blocked.
7. **Thiếu test gate bắt buộc sau mỗi phase** (build + smoke + regression tối thiểu).

### Thiếu sót nghiệp vụ
1. **Trạng thái lịch hẹn** chưa quy ước rõ tập giá trị hợp lệ (`pending`, `arrived`, `cancelled`) và điều kiện chuyển trạng thái.
2. **Phân công KTV** chưa chốt mô hình dữ liệu, gây ảnh hưởng thẳng đến query, analytics, thưởng nóng.
3. **Warehouse audit** chưa chốt “snapshot bất biến” hay cập nhật tồn trực tiếp.
4. **Role matrix** mới nói vai trò, chưa chỉ rõ quyền theo action/controller.
5. **Open questions quá nhiều** làm chậm triển khai.

## 2) Quyết định mặc định để giảm Open Questions

Để triển khai ngay, chốt mặc định như sau:

1. **Identity migration**: không migrate tài khoản hardcode; tạo mới bằng seed (`admin_hpd`) + bắt buộc đổi mật khẩu lần đầu.
2. **Ticket Staff Assignment**: dùng mô hình **1 ticket - 1 KTV chính** với `Ticket.AssignedStaffId` (nhanh, ít rủi ro). Nếu cần nhiều KTV sẽ mở rộng phase riêng.
3. **Staff Activities & Bonus**: lấy dữ liệu thật từ `Ticket` + `TicketService`, không dùng demo.
4. **Warehouse audit**: dùng bảng mới `AuditSession` + `AuditSessionItem` (lưu lịch sử bất biến), không ghi đè lịch sử.
5. **Notifications**: triển khai SignalR realtime + fallback tải danh sách từ DB khi mở dropdown.

=> Từ đây, **không còn open question blocker** cho các phase 0-12.

## 3) Chuẩn triển khai chung cho mọi phase

### Chuẩn API list
- Contract chuẩn:
  - `{ items, totalCount, page, pageSize, totalPages }`
  - Nếu có tổng hợp: thêm `summary` object (ví dụ imports).
- Sort mặc định: bản ghi mới nhất trước (`DESC` theo trường thời gian phù hợp).
- Validate đầu vào: `page >= 1`, `1 <= pageSize <= 100`.

### Chuẩn kỹ thuật
- Không hardcode text trạng thái; gom hằng số/enums tại 1 nơi.
- Query list dùng `AsNoTracking()` cho read.
- Không phá UI hiện tại; chỉ thay nguồn dữ liệu và event handler.

### Test gate bắt buộc sau mỗi phase
1. `dotnet build` pass.
2. Smoke test luồng chính của module.
3. Kiểm tra không vỡ các màn hình đã hoàn tất trước đó.
4. Nếu có migration: tạo migration, update DB local, test lại CRUD chính.

## 4) Checklist triển khai tuần tự theo phase

## Phase 0 — Shared Paging Infrastructure (bắt buộc làm trước)

### Mục tiêu
Tạo hạ tầng dùng chung để giảm lặp code cho các phase sau.

### Checklist thực thi
- [ ] Tạo `Models/ViewModels/PagedResult.cs` với `Items`, `TotalCount`, `Page`, `PageSize`, `TotalPages`.
- [ ] Thêm helper tính paging trong backend (extension hoặc static helper).
- [ ] Thêm `HPUI.renderPaging(containerId, currentPage, totalPages, onPageChange)` trong `wwwroot/js/site.js`.
- [ ] Chuẩn hóa hàm fetch + render table theo contract paging.

### DoD
- [ ] Có ít nhất 1 màn hình mẫu chạy được paging end-to-end.
- [ ] UI paging thống nhất dark theme.

---

## Phase 1 — Appointments

### Checklist thực thi
- [ ] `AppointmentsController.Index()` đổi sang `OrderByDescending(AppointmentTime)`.
- [ ] Thêm API `GET /appointments/list?page=&pageSize=&search=&status=`.
- [ ] Filter `search` trên `CustomerName`, `Plate`, `Phone`; filter `status` chính xác.
- [ ] View `Appointments/Index` thay dropdown bằng 2 nút:
  - [ ] `Tiếp nhận xe` -> `arrived` -> redirect `/tickets/create?appointmentId=...`
  - [ ] `Hủy lịch` -> `cancelled`
- [ ] Đã thao tác thì ẩn nút, hiện badge trạng thái.
- [ ] Tích hợp AJAX render tbody + paging footer.

### DoD
- [ ] Không còn lịch mới hiển thị cuối trang.
- [ ] Không thể bấm lặp gây sai trạng thái.

---

## Phase 2 — Tickets

### Checklist thực thi
- [ ] Thêm `AssignedStaffId` + navigation `AssignedStaff` vào `Ticket`.
- [ ] Tạo migration `AddTicketStaffAssignment`.
- [ ] `TicketsController`:
  - [ ] `List` có paging/filter.
  - [ ] `CreateAjax` nhận `AssignedStaffId`.
  - [ ] `Index` include `AssignedStaff`.
  - [ ] `Detail` hiển thị KTV phụ trách.
- [ ] `Views/Tickets/Create` thêm dropdown phân công KTV.
- [ ] `Views/Tickets/Index` thêm cột KTV + paging AJAX.

### DoD
- [ ] Tạo phiếu có/không có KTV đều hợp lệ.
- [ ] List ticket hiển thị đúng KTV và phân trang.

---

## Phase 3 — Catalog (Services & Quotas)

### Checklist thực thi
- [ ] `CatalogController.ServiceList()` thêm `page`, `pageSize`, `search`.
- [ ] `CatalogController.QuotaList()` thêm `serviceId`, `page`, `pageSize`.
- [ ] `Views/Settings/Services.cshtml` chuyển sang AJAX + paging.
- [ ] `Views/Settings/QuotasUi.cshtml` AJAX + paging + filter service.

### DoD
- [ ] CRUD hiện tại không regression.
- [ ] Mọi list trong catalog theo chuẩn API list.

---

## Phase 4 — Warehouse (Stock + Audit History)

### Checklist thực thi
- [ ] Thêm entities `AuditSession`, `AuditSessionItem`.
- [ ] Cập nhật `HP_DetailingDbContext` với DbSet mới.
- [ ] Tạo migration `AddWarehouseAuditHistory`.
- [ ] `GET /warehouse/list` hỗ trợ paging/search.
- [ ] `GET /warehouse/audit/history` phân trang lịch sử kiểm kê.
- [ ] `POST /warehouse/audit/create` tạo snapshot kiểm kê.
- [ ] Thêm UI danh sách kỳ kiểm kê + xem chi tiết.

### DoD
- [ ] Tạo kỳ kiểm kê không làm mất dữ liệu tồn cũ.
- [ ] Truy vết được chênh lệch theo từng vật tư.

---

## Phase 5 — Warehouse Imports

### Checklist thực thi
- [ ] Thêm API `GET /warehouse/imports/list?page=&pageSize=&startDate=&endDate=`.
- [ ] Trả payload gồm paging + `summary`:
  - [ ] `summary.totalReceipts`
  - [ ] `summary.totalValue`
- [ ] `Views/Warehouse/Imports.cshtml` chuyển AJAX tbody.
- [ ] Footer tổng hợp lấy từ `summary` toàn bộ filter (không theo page).

### DoD
- [ ] Đổi trang không làm thay đổi tổng theo filter.

---

## Phase 6 — Staff

### Checklist thực thi
- [ ] Debug lỗi `POST /Staff/CreateAjax`:
  - [ ] Bắt exception chi tiết.
  - [ ] So khớp schema entity và DB.
  - [ ] Fix nullability/constraint vi phạm.
- [ ] Thêm API `GET /staff/activities?staffId=&page=`.
- [ ] Query từ ticket đã gán KTV + dịch vụ đã làm.
- [ ] `Views/Staff/Detail.cshtml` thêm:
  - [ ] Hoạt động gần đây.
  - [ ] Thưởng nóng tháng hiện tại (nếu chưa có payroll thì hiển thị `0` an toàn).

### DoD
- [ ] Tạo nhân sự không lỗi kết nối.
- [ ] Màn hình chi tiết nhân sự hiển thị dữ liệu thật.

---

## Phase 7 — Analytics

### Checklist thực thi
- [ ] Thêm API `GET /analytics/data?year=&month=&fromDate=&toDate=`.
- [ ] Chuẩn hóa time-source: ưu tiên `PaidAt`, fallback `CreatedAt`.
- [ ] Trả đủ KPI: doanh thu, tăng trưởng, tickets, top services, top staff...
- [ ] `Views/Analytics/Index.cshtml` thêm filter bar (năm/tháng/from-to).
- [ ] Thay đổi filter -> gọi AJAX -> cập nhật cards/charts.

### DoD
- [ ] Số liệu thay đổi đúng theo filter.
- [ ] Không lỗi khi thiếu `PaidAt`.

---

## Phase 8 — Settings (text encoding + tab cleanup)

### Checklist thực thi
- [ ] Sửa toàn bộ text lỗi encoding trong `Views/Settings/Index.cshtml`.
- [ ] Xóa tab link `Danh mục dịch vụ` khỏi thanh tab theo yêu cầu.

### DoD
- [ ] Không còn chuỗi tiếng Việt lỗi font.

---

## Phase 9 — Profile (sau Identity)

### Checklist thực thi
- [ ] `ProfileController.Index()` load đúng user hiện tại.
- [ ] `POST /profile/update` cập nhật tên/sđt/avatar.
- [ ] `POST /profile/change-password` dùng Identity.
- [ ] `Views/Profile/Index.cshtml` có form thông tin + đổi mật khẩu.

### DoD
- [ ] Cập nhật profile không ảnh hưởng role/claims.

---

## Phase 10 — Auth + Authorization (khối rủi ro cao)

> [!CAUTION]
> Phase này bắt buộc có backup DB trước khi chạy migration.

### Checklist thực thi
- [ ] Thêm package `Microsoft.AspNetCore.Identity.EntityFrameworkCore`.
- [ ] Tạo `AppUser : IdentityUser` (`FullName`, `StaffId`, `AvatarUrl`, `IsActive`).
- [ ] Đổi DbContext sang `IdentityDbContext<AppUser, IdentityRole, string>`.
- [ ] Cấu hình `AddIdentity` + cookie options.
- [ ] Seed roles: `Admin`, `ThuNgan`, `KTV`, `QuanLyKho`.
- [ ] Seed admin mặc định và bắt đổi mật khẩu khi đăng nhập lần đầu.
- [ ] Refactor `AccountController` dùng `SignInManager`/`UserManager`.
- [ ] Gắn `[Authorize(Roles = ...)]` theo ma trận quyền.
- [ ] Tạo migration `AddIdentity`.

### DoD
- [ ] Login/logout hoạt động ổn định.
- [ ] Role-based access đúng theo từng khu vực.
- [ ] Không còn hardcode credential trong code.

---

## Phase 11 — Notifications Realtime

### Checklist thực thi
- [ ] Tạo entity `Notification`.
- [ ] Tạo `NotificationHub` SignalR.
- [ ] Cấu hình `services.AddSignalR()` + map hub endpoint.
- [ ] Tích hợp `_Header.cshtml`:
  - [ ] badge unread
  - [ ] dropdown list
  - [ ] reconnect SignalR
- [ ] Tạo trigger khi: tạo ticket, tạo appointment, hoàn thành ticket, import kho.
- [ ] Fallback: khi hub mất kết nối vẫn gọi API lấy danh sách thông báo.

### DoD
- [ ] Có thông báo realtime trong phiên đang mở.
- [ ] Reload trang vẫn xem lại lịch sử thông báo.

---

## Phase 12 — Sidebar Logout Fix

### Checklist thực thi
- [ ] `Views/Shared/_Sidebar.cshtml`: đổi link logout về `/Account/Logout`.
- [ ] Kiểm tra redirect về `/login`.

### DoD
- [ ] Không còn link logout “giả” chỉ điều hướng trang.

---

## 5) Thứ tự triển khai khuyến nghị (đã tối ưu phụ thuộc)

1. Phase 0  
2. Phase 1  
3. Phase 2  
4. Phase 3  
5. Phase 4  
6. Phase 5  
7. Phase 8  
8. Phase 12  
9. Phase 6  
10. Phase 7  
11. Phase 10  
12. Phase 9  
13. Phase 11

## 6) Kế hoạch kiểm thử tổng hợp

### Sau mỗi phase
- [ ] `dotnet build`
- [ ] chạy app và smoke test module vừa sửa
- [ ] regression nhanh các module gần kề

### Sau mỗi phase có migration
- [ ] tạo migration
- [ ] update DB local
- [ ] test create/update/list/delete dữ liệu chính

### Trước khi chuyển qua phase rủi ro cao (10, 11)
- [ ] backup DB
- [ ] tag source code tại mốc ổn định
- [ ] chuẩn bị checklist rollback
