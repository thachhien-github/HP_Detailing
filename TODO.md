Kế hoạch hoàn thiện hệ thống (theo đúng checklist bạn đưa)
0) Khuôn tiến hành (áp dụng cho mọi module)
Chuẩn hóa API + View theo cùng contract (request/response camelCase nhất quán).
Thêm server-side sorting (“mới nhất lên trên”) thay vì chỉ sort ở client.
Chuẩn hóa UI dùng paging component thống nhất (cùng kiểu footer của khối “Tổng …”).
Phase 1 — Bắt đầu từ luồng bạn ưu tiên (Appointments, Tickets, Catalog, Warehouse, Staff, Analytics, Settings, Auth, Nav/Sidebar)
1) /appointments
Mục tiêu:

Danh sách sắp xếp mới nhất lên trên.
Cột trạng thái chỉ cập nhật theo thao tác, lịch mới được tạo thì trạng thái = "Chờ xử lý".
Cột thao tác có 2 nút:
“Tiếp nhận xe” → set status text = đã tiếp nhận => chuyển sao trang /tickets/create?appointmentId=13 kèm dữ liệu tương ứng đổ vào như logic hiện tại.
“Hủy lịch” → set status text = đã hủy
=> sau khi thao tác thì ẩn cả 2 nút đi
Thêm phân trang chuyên nghiệp (mỗi trang 10 dòng).
Thông tin hiện trạng từ code:

AppointmentsController hiện dùng:
Index() sort theo OrderBy(a => a.AppointmentTime)
UpdateStatus(...) cho phép set status theo payload.
View hiện đang có dropdown thay vì 2 nút.
Plan thực thi:

Controller:
đổi OrderByDescending theo AppointmentTime (hoặc CreatedAt nếu có).
thêm API GET /appointments/list?page=...&pageSize=...&search=...&status=... trả JSON + total.
View:
thay dropdown bằng 2 nút bấm gọi:
POST /Appointments/UpdateStatus với Status = arrived (Tiếp nhận) và cancelled (Hủy)
cập nhật hiển thị status text đúng theo mapping:
arrived → “đã tiếp nhận”
cancelled → “đã hủy”
Paging:
thêm component paging (server-side).
giữ UI filter/search nếu bạn muốn (sắp sau).

2) /tickets
Mục tiêu:
Thêm phân trang chuyên nghiệp
Sort mới nhất lên trên (CreatedAt DESC)
/tickets/create?appointmentId=15: thêm gắn phân công nhân viên và thêm cột "nhân viên kỹ thuật" cho list tickets và /tickets/14 gọi "nhân viên kỹ thuật" tương ứng

Plan thực thi:

Controller:
tạo GET /tickets/list JSON với page/pageSize/search/status/sort.
Index() chuyển sang server-side model (hoặc gọi qua AJAX).
View:
tách tbody render theo page.
paging footer dạng “Trang X / TotalPages”.
3) /catalog/services và /catalog/quotas
Mục tiêu:

CRUD backend hoàn chỉnh
Thêm phân trang chuyên nghiệp
với danh sách services
với danh sách quotas (lọc theo serviceId)
Plan thực thi:

CatalogController:
ServiceList chuyển sang server-side paging: GET /catalog/services/list?page=....
quotas: GET /catalog/quotas/list?serviceId=...&page=....
UI:
thêm paging component ở bảng.
giữ modal CRUD.
Phần CRUD “backend” bạn đã có ở CatalogController hiện tại, chủ yếu cần “paging + hoàn thiện contract”.

4) /warehouse
Mục tiêu: 
phân trang chuyên nghiệp.

Thêm danh mục kiểm kê: danh sách lịch sử kỳ tồn, nút lập kỳ kiểm kê tạo báo cáo tình trạng chi tiết vật tư hao hụt (mô tả lý do)

Plan thực thi:

thêm paging cho:
/warehouse (tồn kho/index)
lịch sử/các tab liên quan (nếu UI đang list dài)
5) /warehouse/imports
Mục tiêu:

phân trang chuyên nghiệp nhưng vẫn giữ footer:
“Tổng: 7 phiếu nhập”
“Tổng giá trị: 933,750,000đ”
Plan thực thi:

Controller:
API list imports page.
query tổng toàn bộ theo filter hiện tại (không phụ thuộc page).
View:
footer hiển thị tổng theo filter, còn bảng chỉ hiển thị page.
6) /staff
Mục tiêu:

xử lý lỗi kết nối khi thêm
hoàn thiện “Hoạt động & Thưởng nóng (Demo)”
Plan thực thi (bước chẩn đoán trước):

Tìm error từ logs/response của request create staff.
Kiểm tra:
connection string/DbContext migration
mapping Staff entity (có field nào nullable sai)
endpoint đang dùng SQL/EF truy cập vào bảng nào thiếu
Sau khi fix create thành công:
triển khai 2 phần “Hoạt động” và “Thưởng nóng” theo dữ liệu demo (hoặc theo bảng hiện có).
7) /analytics
Mục tiêu: thêm nhiều bộ lọc:

theo năm
theo tháng
theo khoảng thời gian cụ thể
Plan thực thi:

Controller Analytics:
thêm params query: year, month, fromDate, toDate.
thống nhất nguồn dữ liệu doanh thu theo PaidAt (đã có logic fallback).
View:
thêm filter UI (select year/month + date range picker).
call API cập nhật biểu đồ.
8) /settings
Mục tiêu:

lỗi font chữ
loại bỏ tab “Danh mục dịch vụ”
Plan thực thi:

Font:
kiểm tra CSS load (site.css / font import)
rà lại class font trong _Layout/tailwind config nếu có.
Tab:
xoá/disable tab “Danh mục dịch vụ” khỏi Views/Settings/Index.cshtml.
đảm bảo redirect vẫn về /catalog/services.
9) /profile
Mục tiêu: hoàn thiện backend:

cập nhật tài khoản
các chức năng khác
Plan thực thi:

kiểm tra ProfileController hiện làm gì (data read hay update)
thêm endpoint:
POST /profile/update (name/phone/avatar… tùy model)
validate server-side
gắn theo user hiện đăng nhập.

=> dùng thư viện framework Identity trong ASP.NET EF Core MVC
10) /login (Auth thật + phân quyền + admin)
Mục tiêu:

login thật với account
phân quyền
chỉ admin được cập nhật hệ thống
Plan thực thi:

xem lại hiện LoginController đang hardcode hay có store user thật
chuyển qua:
bảng AppUser/Staff map login
hash password
roles: Admin, ThuNgan, KTV, QuanLyKho…
áp policy [Authorize(Roles="Admin")] cho endpoints settings/admin.

=> dùng thư viện framework Identity trong ASP.NET EF Core MVC
11) Topbar notification realtime
Mục tiêu: chuông thông báo nhận thông báo thực realtime, activity gần đây.

Plan thực thi:

hiện tại chắc đang mock/JS poll.
nâng cấp bằng:
SignalR (khuyến nghị) hoặc SSE/poll có debounce.
tạo “Notifications” nguồn từ DB (hoạt động gần đây).
12) Sidebar “Đăng xuất” chưa chuyển sang /login
Mục tiêu: nút Logout phải redirect tới /login.

Plan thực thi:

rà _Sidebar.cshtml / logout route.
đảm bảo:
href="/Account/Logout" (hoặc route đúng)
Logout action return RedirectToAction("Login","Account" or "LoginController")
Bước tiếp theo (đề xuất chạy tuần tự theo ít rủi ro)
Appointments paging + 2 nút thao tác (dễ thấy, dễ test)
Tickets paging + sort mới nhất
Các list paging còn lại (Catalog/Warehouse/Imports)
Staff lỗi kết nối (cần log trước)
Analytics filter + Settings font/tab
Auth thật + Profile
Realtime notifications + Logout fix