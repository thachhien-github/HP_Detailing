using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace HP_Detailing.Models
{
    public class AppUser : IdentityUser
    {
        public string? FullName { get; set; }
        
        public int? StaffId { get; set; }
        public Staff? Staff { get; set; }
        
        public string? AvatarUrl { get; set; }
        
        public bool IsActive { get; set; } = true;
        public bool RequirePasswordChange { get; set; } = false;
    }

    public class Position
    {
        public int Id { get; set; }
        [Required]
        public string PositionCode { get; set; } = string.Empty; // MaChucVu
        [Required]
        public string Name { get; set; } = string.Empty; // TenChucVu
        public string? Description { get; set; } // MoTa

        public ICollection<Staff> Staffs { get; set; } = new List<Staff>();
    }

    public class Staff
    {
        public int Id { get; set; }

        [Required]
        public string StaffCode { get; set; } = string.Empty; // MaNhanVien (NV001)

        [Required]
        public string FullName { get; set; } = string.Empty; // TenNhanVien
        
        public DateTime? DateOfBirth { get; set; } // NgaySinh
        public bool Gender { get; set; } // GioiTinh (True: Nam, False: Nu)
        public string? Address { get; set; } // DiaChi
        public string? Phone { get; set; } // SoDienThoai
        public DateTime? HireDate { get; set; } // NgayVaoLam
        public string Status { get; set; } = "Hoạt động"; // TrangThai (Hoạt động, Nghỉ phép, Nghỉ việc)
        public bool IsActive { get; set; } = true; // Flag for soft delete

        // Legacy string fields for backward compatibility during transition
        public string? Specialty { get; set; }
        public string? Position { get; set; } 

        // Relationships
        public int? PositionId { get; set; } // MaChucVu
        public Position? PositionEntity { get; set; }

        public StaffProfile? Profile { get; set; }
        public ICollection<LaborContract> LaborContracts { get; set; } = new List<LaborContract>();
        public ICollection<Payroll> Payrolls { get; set; } = new List<Payroll>();
    }

    public class StaffProfile
    {
        public int Id { get; set; }

        public int StaffId { get; set; } // MaNhanVien
        public Staff? Staff { get; set; }

        public string? IdentityCard { get; set; } // SoCCCD
        public DateTime? IssueDate { get; set; } // NgayCap
        public string? IssuePlace { get; set; } // NoiCap
        public string? Ethnicity { get; set; } // DanToc
        public string? Nationality { get; set; } // QuocTich
        public string? Religion { get; set; } // TonGiao
        public bool MaritalStatus { get; set; } // TinhTrangHonNhan
        public string? Notes { get; set; } // GhiChu
    }

    public class LaborContract
    {
        public int Id { get; set; }

        [Required]
        public string ContractCode { get; set; } = string.Empty; // MaHopDong

        public int StaffId { get; set; } // MaNhanVien
        public Staff? Staff { get; set; }

        public DateTime? StartDate { get; set; } // NgayBatDau
        public DateTime? EndDate { get; set; } // NgayKetThuc

        public decimal BasicSalary { get; set; } // LuongCoBan

        public string? ContractType { get; set; } // LoaiHopDong
        public string? Status { get; set; } = "Hiệu lực"; // TrangThai
    }

    public class Payroll
    {
        public int Id { get; set; }

        [Required]
        public string PayrollCode { get; set; } = string.Empty; // MaBangLuong

        public int StaffId { get; set; } // MaNhanVien
        public Staff? Staff { get; set; }

        public int Month { get; set; }
        public int Year { get; set; }

        public decimal Bonus { get; set; } // Thuong
        public decimal Deduction { get; set; } // Phat
        public string? Notes { get; set; } // GhiChu
    }

    public class ServiceCategory
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;
    }

    public class Service
    {
        public int Id { get; set; }

        [Required]
        public string ServiceCode { get; set; } = string.Empty; // DV-001

        [Required]
        public string Name { get; set; } = string.Empty;

        public int ServiceCategoryId { get; set; }
        public ServiceCategory? ServiceCategory { get; set; }

        public int DurationMinutes { get; set; }
        public decimal UnitPrice { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class Car
    {
        [Key]
        [Required]
        [StringLength(20)]
        public string Plate { get; set; } = string.Empty; // Biển số xe làm Khóa chính (Primary Key)

        public string? Brand { get; set; } // Hãng xe (Toyota, Mazda...)
        public string? Model { get; set; } // Dòng xe (CX-5, Vios...)
        public string? Color { get; set; } // Màu xe
        
        public string? OwnerName { get; set; } // Tên chủ xe
        public string? OwnerPhone { get; set; } // SĐT chủ xe

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Ticket
    {
        public int Id { get; set; }

        [Required]
        public string TicketCode { get; set; } = string.Empty; // PDV-...

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }

        public string? Plate { get; set; } // License plate
        public Car? Car { get; set; }

        public string? CarModel { get; set; } // Legacy field kept for backward compatibility

        public string Status { get; set; } = "pending"; // free-form

        public int? AssignedStaffId { get; set; }
        public Staff? AssignedStaff { get; set; }

        public ICollection<TicketService> TicketServices { get; set; } = new List<TicketService>();
        public ICollection<TicketMaterialUsage> TicketMaterialUsages { get; set; } = new List<TicketMaterialUsage>();
    }

    public class TicketMaterialUsage
    {
        public int Id { get; set; }

        public int TicketId { get; set; }
        public Ticket? Ticket { get; set; }

        public int MaterialId { get; set; }
        public Material? Material { get; set; }

        public decimal Quantity { get; set; }           // Số lượng thực dùng xuất kho
        public decimal UnitPrice { get; set; }           // Giá bán vật tư snapshot lúc xuất
        public bool IsChargedToCustomer { get; set; } = true; // true: Tính tiền khách, false: Hao hụt miễn phí đi kèm dịch vụ
    }

    public class TicketService
    {
        public int Id { get; set; }

        public int TicketId { get; set; }
        public Ticket? Ticket { get; set; }

        public int ServiceId { get; set; }
        public Service? Service { get; set; }

        public decimal PriceSnapshot { get; set; }
        public string Status { get; set; } = "not_started";
    }

    public class Material
    {
        public int Id { get; set; }

        [Required]
        public string MaterialCode { get; set; } = string.Empty; // VT001

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Unit { get; set; }

        public decimal UnitPrice { get; set; }

        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class WarehouseStock
    {
        public int Id { get; set; }

        public int MaterialId { get; set; }
        public Material? Material { get; set; }

        public decimal QuantityOnHand { get; set; }

        public decimal ReorderLevel { get; set; }
    }

    public class Appointment
    {
        public int Id { get; set; }

        [Required]
        public string AppointmentCode { get; set; } = string.Empty; // LH001

        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? Plate { get; set; }

        public DateTime AppointmentTime { get; set; }

        public string? Services { get; set; }
        public string? Note { get; set; }

        public string Status { get; set; } = "pending";
    }

    public class Invoice
    {
        public int Id { get; set; }

        [Required]
        public string InvoiceCode { get; set; } = string.Empty; // HD-...

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? TicketId { get; set; }
        public Ticket? Ticket { get; set; }

        public string Status { get; set; } = "UNPAID";

        public decimal TotalAmount { get; set; }

        public DateTime? PaidAt { get; set; }

        public int? PaymentMethodId { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }

        public string? PaymentNote { get; set; }
    }

    public class InvoiceService
    {
        public int Id { get; set; }

        public int InvoiceId { get; set; }
        public Invoice? Invoice { get; set; }

        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public class PaymentMethod
    {
        public int Id { get; set; }

        [Required]
        public string BankFullName { get; set; } = string.Empty;

        [Required]
        public string BankShortName { get; set; } = string.Empty;

        public string? AccountNumber { get; set; }
        public string? Owner { get; set; }

        public bool IsDefault { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class MaterialUsage
    {
        public int Id { get; set; }

        public int TicketServiceId { get; set; }
        public TicketService? TicketService { get; set; }

        public int MaterialId { get; set; }
        public Material? Material { get; set; }

        public decimal RequiredQty { get; set; }
        public decimal ActualQty { get; set; }
    }

    public class StockImport
    {
        public int Id { get; set; }

        [Required]
        public string ImportCode { get; set; } = string.Empty; // PN-2406-001

        public DateTime ImportDate { get; set; } = DateTime.Now;

        public int? SupplierId { get; set; }
        public Supplier? Supplier { get; set; }
        public string? CreatedBy { get; set; }
        public string? Notes { get; set; }
        public string Status { get; set; } = "Hoàn tất";

        public decimal TotalAmount { get; set; }

        // Mở rộng thông tin minh chứng hóa đơn (Phase 2)
        public string? InvoiceLink { get; set; } // Link ảnh hóa đơn giấy hoặc link hóa đơn điện tử
        public string? ReceivedBy { get; set; } // Người nhận nhập hàng tại thời điểm đó
        public string? PaymentStatus { get; set; } = "Chưa thanh toán"; // Trạng thái thanh toán (Chưa thanh toán, Đã thanh toán)

        public ICollection<StockImportItem> Items { get; set; } = new List<StockImportItem>();
    }

    public class StockImportItem
    {
        public int Id { get; set; }

        public int StockImportId { get; set; }
        public StockImport? StockImport { get; set; }

        public int MaterialId { get; set; }
        public Material? Material { get; set; }

        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class Supplier
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;   // Tên nhà cung cấp

        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastUsedAt { get; set; }

        public ICollection<StockImport> StockImports { get; set; } = new List<StockImport>();
    }

    /// <summary>
    /// ĐỊNH MỨC VẬT TƯ THEO DỊCH VỤ — Nghiệp vụ quan trọng:
    /// Khi một Ticket chọn Dịch vụ X, hệ thống tự động trừ kho theo DefaultQty của từng vật tư trong bảng này.
    /// </summary>
    public class ServiceMaterialQuota
    {
        public int Id { get; set; }

        [Required]
        public int ServiceId { get; set; }
        public Service? Service { get; set; }

        [Required]
        public int MaterialId { get; set; }
        public Material? Material { get; set; }

        /// <summary>Số lượng tiêu hao mặc định mỗi lần thực hiện dịch vụ này.</summary>
        public decimal DefaultQty { get; set; } = 1;

        public string? Notes { get; set; }   // Ghi chú nghiệp vụ (VD: "1 lọ Ceramic cho xe con, 1.5 lọ xe bán tải")

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// PHIÊN KIỂM KÊ KHO — Mỗi lần kiểm kê tạo 1 AuditSession.
    /// Lưu snapshot toàn bộ tồn kho tại thời điểm kiểm, cho phép truy vết chênh lệch theo từng vật tư.
    /// </summary>
    public class AuditSession
    {
        public int Id { get; set; }

        [Required]
        public string AuditCode { get; set; } = string.Empty; // KK-2406-001

        public DateTime AuditDate { get; set; } = DateTime.Now;

        public string? AuditedBy { get; set; }  // Người thực hiện kiểm kê
        public string? Notes { get; set; }       // Ghi chú chung cho phiên

        public int TotalItems { get; set; }          // Tổng số mặt hàng kiểm
        public int DiscrepancyCount { get; set; }    // Số mặt hàng có chênh lệch

        public ICollection<AuditSessionItem> Items { get; set; } = new List<AuditSessionItem>();
    }

    /// <summary>
    /// CHI TIẾT TỪNG VẬT TƯ TRONG PHIÊN KIỂM KÊ.
    /// SystemQuantity = tồn kho hệ thống TRƯỚC khi điều chỉnh → luôn truy vết được.
    /// </summary>
    public class AuditSessionItem
    {
        public int Id { get; set; }

        public int AuditSessionId { get; set; }
        public AuditSession? AuditSession { get; set; }

        public int MaterialId { get; set; }
        public Material? Material { get; set; }

        public decimal SystemQuantity { get; set; }  // Tồn kho hệ thống tại thời điểm kiểm
        public decimal ActualQuantity { get; set; }  // Số lượng thực tế đếm được
        public decimal Discrepancy { get; set; }     // = ActualQuantity - SystemQuantity

        public string? Note { get; set; }            // Ghi chú riêng cho vật tư này
    }

    public class Notification
    {
        public int Id { get; set; }
        [Required]
        public string Title { get; set; } = string.Empty;
        [Required]
        public string Message { get; set; } = string.Empty;
        public string? Type { get; set; } // e.g. "Ticket", "Appointment", "Warehouse"
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? ActionUrl { get; set; } // Link to open details
        public string? TargetUserId { get; set; } // User that this notification is for (null for general admin/all)
    }
}
