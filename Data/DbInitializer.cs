using HP_Detailing.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace HP_Detailing.Data
{
    public static class DbInitializer
    {
        // ==========================================
        // PHƯƠNG THỨC: Khởi tạo dữ liệu mẫu (Seeding)
        // NGHIỆP VỤ: Tạo đúng 5 dòng dữ liệu liên kết chặt chẽ cho toàn bộ 12 bảng trong CSDL
        // BẮT LỖI: Try-catch toàn bộ tiến trình để đảm bảo không lỗi khi cập nhật DB
        // ==========================================
        public static async Task Initialize(HP_DetailingDbContext context, IServiceProvider serviceProvider)
        {
            try
            {
                // Chỉ seed bộ dữ liệu mẫu đầy đủ khi chưa có phiếu dịch vụ
                if (!context.Tickets.Any())
                {
                // ----------------------------------------------------
                // 1. BẢNG: ServiceCategory (Danh mục dịch vụ - 5 Dòng)
                // ----------------------------------------------------
                var categories = new ServiceCategory[]
                {
                    new ServiceCategory { Name = "Rửa xe" },
                    new ServiceCategory { Name = "Detailing Chuyên sâu" },
                    new ServiceCategory { Name = "Đánh bóng & Sơn" },
                    new ServiceCategory { Name = "Phủ Ceramic & Graphene" },
                    new ServiceCategory { Name = "Dán Phim cách nhiệt" }
                };
                context.ServiceCategories.AddRange(categories);
                context.SaveChanges();

                // ----------------------------------------------------
                // 2. BẢNG: Service (Dịch vụ chi tiết - 5 Dòng)
                // ----------------------------------------------------
                var services = new Service[]
                {
                    new Service { ServiceCode = "RX-01", Name = "Rửa xe VIP", ServiceCategoryId = categories[0].Id, DurationMinutes = 45, UnitPrice = 150000, IsActive = true },
                    new Service { ServiceCode = "DT-01", Name = "Vệ sinh nội thất sâu", ServiceCategoryId = categories[1].Id, DurationMinutes = 180, UnitPrice = 1200000, IsActive = true },
                    new Service { ServiceCode = "DB-01", Name = "Hiệu chỉnh sơn 3 bước", ServiceCategoryId = categories[2].Id, DurationMinutes = 240, UnitPrice = 2500000, IsActive = true },
                    new Service { ServiceCode = "CR-01", Name = "Phủ Ceramic 9H Double Layer", ServiceCategoryId = categories[3].Id, DurationMinutes = 360, UnitPrice = 5000000, IsActive = true },
                    new Service { ServiceCode = "PM-01", Name = "Dán phim cách nhiệt 3M Crystalline", ServiceCategoryId = categories[4].Id, DurationMinutes = 300, UnitPrice = 8500000, IsActive = true }
                };
                context.Services.AddRange(services);
                context.SaveChanges();

                // ----------------------------------------------------
                // 3. BẢNG: Staff (Kỹ thuật viên & Nhân viên - 5 Dòng)
                // ----------------------------------------------------
                var staffs = new Staff[]
                {
                    new Staff { StaffCode = "NV001", FullName = "Nguyễn Hoàng Hải", Position = "Kỹ thuật viên", Specialty = "Detailing chuyên sâu", Phone = "0901234567", IsActive = true },
                    new Staff { StaffCode = "NV002", FullName = "Trần Minh Quân", Position = "Kỹ thuật viên", Specialty = "Rửa & Đánh bóng", Phone = "0909876543", IsActive = true },
                    new Staff { StaffCode = "NV003", FullName = "Lê Thanh Sơn", Position = "Kỹ thuật viên", Specialty = "Ceramic & Phim cách nhiệt", Phone = "0987654321", IsActive = true },
                    new Staff { StaffCode = "NV004", FullName = "Phạm Thuỳ Linh", Position = "Thu ngân", Specialty = "Kế toán & CSKH", Phone = "0944556677", IsActive = true },
                    new Staff { StaffCode = "NV005", FullName = "Vũ Đức Trọng", Position = "Quản lý vận hành", Specialty = "Giám sát & Kỹ thuật", Phone = "0911223344", IsActive = true }
                };
                context.Staff.AddRange(staffs);
                context.SaveChanges();

                // ----------------------------------------------------
                // 4. BẢNG: Material (Vật tư phụ tùng - 5 Dòng)
                // ----------------------------------------------------
                var materials = new Material[]
                {
                    new Material { MaterialCode = "VT001", Name = "Dung dịch rửa xe bọt tuyết Meguiar's", Unit = "Chai", UnitPrice = 350000, IsActive = true },
                    new Material { MaterialCode = "VT002", Name = "Hóa chất bảo dưỡng da cao cấp Leather Care", Unit = "Chai", UnitPrice = 580000, IsActive = true },
                    new Material { MaterialCode = "VT003", Name = "Dung dịch phủ Ceramic 9H CarPro Quartz", Unit = "Hộp", UnitPrice = 2200000, IsActive = true },
                    new Material { MaterialCode = "VT004", Name = "Phim cách nhiệt 3M Crystalline (Met)", Unit = "Mét", UnitPrice = 450000, IsActive = true },
                    new Material { MaterialCode = "VT005", Name = "Xi đánh bóng phá xước Sonax CutMax", Unit = "Chai", UnitPrice = 650000, IsActive = true }
                };
                context.Materials.AddRange(materials);
                context.SaveChanges();

                // ----------------------------------------------------
                // 5. BẢNG: WarehouseStock (Kho chứa vật tư - 5 Dòng)
                // ----------------------------------------------------
                var stocks = new WarehouseStock[]
                {
                    new WarehouseStock { MaterialId = materials[0].Id, QuantityOnHand = 25.5m, ReorderLevel = 5.0m },
                    new WarehouseStock { MaterialId = materials[1].Id, QuantityOnHand = 12.0m, ReorderLevel = 3.0m },
                    new WarehouseStock { MaterialId = materials[2].Id, QuantityOnHand = 8.0m, ReorderLevel = 2.0m },
                    new WarehouseStock { MaterialId = materials[3].Id, QuantityOnHand = 45.0m, ReorderLevel = 10.0m },
                    new WarehouseStock { MaterialId = materials[4].Id, QuantityOnHand = 15.0m, ReorderLevel = 4.0m }
                };
                context.WarehouseStocks.AddRange(stocks);
                context.SaveChanges();

                // ----------------------------------------------------
                // 6. BẢNG: Appointment (Lịch đặt hẹn - 5 Dòng)
                // ----------------------------------------------------
                var appointments = new Appointment[]
                {
                    new Appointment { AppointmentCode = "LH2405-001", CustomerName = "Anh Trần Tiến Dũng", CustomerPhone = "0912345678", Plate = "30A-123.45", AppointmentTime = DateTime.UtcNow.AddHours(2), Services = "Rửa xe VIP", Status = "confirmed", Note = "Khách hàng VIP, rửa xe nhanh" },
                    new Appointment { AppointmentCode = "LH2405-002", CustomerName = "Chị Nguyễn Mai Anh", CustomerPhone = "0988776655", Plate = "51F-987.65", AppointmentTime = DateTime.UtcNow.AddHours(4), Services = "Phủ Ceramic 9H Double Layer", Status = "arrived", Note = "Khách đã đặt cọc trước 1.000.000 VNĐ" },
                    new Appointment { AppointmentCode = "LH2405-003", CustomerName = "Anh Lê Gia Khánh", CustomerPhone = "0933445566", Plate = "29A-888.88", AppointmentTime = DateTime.UtcNow.AddDays(1), Services = "Vệ sinh nội thất sâu", Status = "pending", Note = "Khách dặn dọn kỹ các khe gió điều hoà" },
                    new Appointment { AppointmentCode = "LH2405-004", CustomerName = "Anh Phạm Hồng Quân", CustomerPhone = "0904556677", Plate = "30H-567.89", AppointmentTime = DateTime.UtcNow.AddDays(-1), Services = "Hiệu chỉnh sơn 3 bước", Status = "completed", Note = "Đã hoàn thành thi công ngày hôm qua" },
                    new Appointment { AppointmentCode = "LH2405-005", CustomerName = "Chị Lê Thuỳ Chi", CustomerPhone = "0977889900", Plate = "30K-444.44", AppointmentTime = DateTime.UtcNow.AddDays(3), Services = "Dán phim cách nhiệt 3M Crystalline", Status = "pending", Note = "Hỏi kỹ chế độ bảo hành bong tróc 10 năm" }
                };
                context.Appointments.AddRange(appointments);
                context.SaveChanges();

                // ----------------------------------------------------
                // 7. BẢNG: Ticket (Phiếu dịch vụ - 5 Dòng)
                // ----------------------------------------------------
                var tickets = new Ticket[]
                {
                    new Ticket { TicketCode = "PDV2405-001", CustomerName = "Anh Trần Tiến Dũng", CustomerPhone = "0912345678", Plate = "30A-123.45", CarModel = "BMW 320i", Status = "in_progress", CreatedAt = DateTime.UtcNow.AddHours(-2) },
                    new Ticket { TicketCode = "PDV2405-002", CustomerName = "Chị Nguyễn Mai Anh", CustomerPhone = "0988776655", Plate = "51F-987.65", CarModel = "Mercedes GLC 300", Status = "waiting_customer", CreatedAt = DateTime.UtcNow.AddHours(-5) },
                    new Ticket { TicketCode = "PDV2405-003", CustomerName = "Anh Lê Gia Khánh", CustomerPhone = "0933445566", Plate = "29A-888.88", CarModel = "Audi Q7", Status = "pending", CreatedAt = DateTime.UtcNow.AddMinutes(-30) },
                    new Ticket { TicketCode = "PDV2405-004", CustomerName = "Anh Phạm Hồng Quân", CustomerPhone = "0904556677", Plate = "30H-567.89", CarModel = "Ford Everest", Status = "completed", CreatedAt = DateTime.UtcNow.AddDays(-1) },
                    new Ticket { TicketCode = "PDV2405-005", CustomerName = "Anh Hoàng Văn Nam", CustomerPhone = "0966778899", Plate = "15A-111.11", CarModel = "Hyundai SantaFe", Status = "cancelled", CreatedAt = DateTime.UtcNow.AddDays(-2) }
                };
                context.Tickets.AddRange(tickets);
                context.SaveChanges();

                // 7b. BẢNG: Car (Hồ sơ xe — khóa chính Plate)
                var seedCars = tickets.Select(t => new Car
                {
                    Plate = t.Plate!,
                    OwnerName = t.CustomerName,
                    OwnerPhone = t.CustomerPhone,
                    Model = t.CarModel,
                    CreatedAt = t.CreatedAt
                }).ToArray();
                context.Cars.AddRange(seedCars);
                context.SaveChanges();

                // ----------------------------------------------------
                // 8. BẢNG: TicketService (Chi tiết dịch vụ trên Phiếu - 5 Dòng)
                // ----------------------------------------------------
                var ticketServices = new TicketService[]
                {
                    new TicketService { TicketId = tickets[0].Id, ServiceId = services[0].Id, PriceSnapshot = services[0].UnitPrice, Status = "completed" }, // Rửa xe VIP hoàn thành
                    new TicketService { TicketId = tickets[0].Id, ServiceId = services[1].Id, PriceSnapshot = services[1].UnitPrice, Status = "in_progress" }, // Vệ sinh nội thất đang xử lý
                    new TicketService { TicketId = tickets[1].Id, ServiceId = services[3].Id, PriceSnapshot = services[3].UnitPrice, Status = "completed" }, // Phủ Ceramic xong
                    new TicketService { TicketId = tickets[2].Id, ServiceId = services[1].Id, PriceSnapshot = services[1].UnitPrice, Status = "not_started" }, // Vệ sinh nội thất chưa làm
                    new TicketService { TicketId = tickets[3].Id, ServiceId = services[2].Id, PriceSnapshot = services[2].UnitPrice, Status = "completed" }  // Hiệu chỉnh sơn hoàn thành
                };
                context.TicketServices.AddRange(ticketServices);
                context.SaveChanges();

                // ----------------------------------------------------
                // 9. BẢNG: Invoice (Hoá đơn thanh toán - 5 Dòng)
                // ----------------------------------------------------
                var invoices = new Invoice[]
                {
                    new Invoice { InvoiceCode = "HD-PDV004-001", TicketId = tickets[3].Id, Status = "PAID", TotalAmount = 2500000, CreatedAt = DateTime.UtcNow.AddDays(-1).AddHours(4) },
                    new Invoice { InvoiceCode = "HD-PDV002-002", TicketId = tickets[1].Id, Status = "UNPAID", TotalAmount = 5000000, CreatedAt = DateTime.UtcNow.AddHours(-1) },
                    new Invoice { InvoiceCode = "HD-PDV001-003", TicketId = tickets[0].Id, Status = "UNPAID", TotalAmount = 1350000, CreatedAt = DateTime.UtcNow.AddHours(-2) },
                    new Invoice { InvoiceCode = "HD-PDV003-004", TicketId = tickets[2].Id, Status = "UNPAID", TotalAmount = 1200000, CreatedAt = DateTime.UtcNow.AddMinutes(-30) },
                    new Invoice { InvoiceCode = "HD-PDV005-005", TicketId = tickets[4].Id, Status = "CANCELLED", TotalAmount = 0, CreatedAt = DateTime.UtcNow.AddDays(-2) }
                };
                context.Invoices.AddRange(invoices);
                context.SaveChanges();

                // ----------------------------------------------------
                // 10. BẢNG: InvoiceService (Chi tiết dịch vụ Hoá đơn - 5 Dòng)
                // ----------------------------------------------------
                var invoiceServices = new InvoiceService[]
                {
                    new InvoiceService { InvoiceId = invoices[0].Id, Name = "Hiệu chỉnh sơn 3 bước", Price = 2500000 },
                    new InvoiceService { InvoiceId = invoices[1].Id, Name = "Phủ Ceramic 9H Double Layer", Price = 5000000 },
                    new InvoiceService { InvoiceId = invoices[2].Id, Name = "Rửa xe VIP", Price = 150000 },
                    new InvoiceService { InvoiceId = invoices[2].Id, Name = "Vệ sinh nội thất sâu", Price = 1200000 },
                    new InvoiceService { InvoiceId = invoices[3].Id, Name = "Vệ sinh nội thất sâu", Price = 1200000 }
                };
                context.InvoiceServices.AddRange(invoiceServices);
                context.SaveChanges();

                // ----------------------------------------------------
                // 11. BẢNG: PaymentMethod (Phương thức thanh toán - 5 Dòng)
                // ----------------------------------------------------
                var paymentMethods = new PaymentMethod[]
                {
                    new PaymentMethod { BankFullName = "Ngân hàng Thương mại Cổ phần Ngoại thương Việt Nam", BankShortName = "Vietcombank", AccountNumber = "1012345678", Owner = "CONG TY HP DETAILING", IsDefault = true, IsActive = true },
                    new PaymentMethod { BankFullName = "Ngân hàng TMCP Kỹ thương Việt Nam", BankShortName = "Techcombank", AccountNumber = "190333444555", Owner = "CONG TY HP DETAILING", IsDefault = false, IsActive = true },
                    new PaymentMethod { BankFullName = "Ví điện tử MoMo Business", BankShortName = "Ví MoMo", AccountNumber = "0909876543", Owner = "VŨ ĐỨC TRỌNG", IsDefault = false, IsActive = true },
                    new PaymentMethod { BankFullName = "Thanh toán bằng Tiền mặt tại Quầy", BankShortName = "Tiền mặt", AccountNumber = "", Owner = "Thu ngân HP", IsDefault = false, IsActive = true },
                    new PaymentMethod { BankFullName = "Thanh toán quẹt thẻ máy POS di động", BankShortName = "Thẻ POS", AccountNumber = "", Owner = "Thu ngân HP", IsDefault = false, IsActive = false }
                };
                context.PaymentMethods.AddRange(paymentMethods);
                context.SaveChanges();

                // ----------------------------------------------------
                // 12. BẢNG: MaterialUsage (Tiêu hao vật tư thi công - 5 Dòng)
                // ----------------------------------------------------
                var usages = new MaterialUsage[]
                {
                    new MaterialUsage { TicketServiceId = ticketServices[0].Id, MaterialId = materials[0].Id, RequiredQty = 0.10m, ActualQty = 0.12m }, // Rửa xe hao nước rửa xe
                    new MaterialUsage { TicketServiceId = ticketServices[1].Id, MaterialId = materials[1].Id, RequiredQty = 0.20m, ActualQty = 0.20m }, // Vệ sinh da hao chai dưỡng da
                    new MaterialUsage { TicketServiceId = ticketServices[2].Id, MaterialId = materials[2].Id, RequiredQty = 1.00m, ActualQty = 1.00m }, // Phủ ceramic hao 1 hộp ceramic
                    new MaterialUsage { TicketServiceId = ticketServices[4].Id, MaterialId = materials[4].Id, RequiredQty = 0.30m, ActualQty = 0.35m }, // Đánh bóng xước hao xi đánh bóng
                    new MaterialUsage { TicketServiceId = ticketServices[3].Id, MaterialId = materials[1].Id, RequiredQty = 0.20m, ActualQty = 0.00m }  // Chưa thi công, chưa dùng vật tư
                };
                context.MaterialUsages.AddRange(usages);
                context.SaveChanges();

                // 13. TicketMaterialUsage — nguồn chính cho nhật ký xuất kho (khớp MaterialUsage mẫu)
                var ticketMaterialUsages = new TicketMaterialUsage[]
                {
                    new TicketMaterialUsage { TicketId = tickets[0].Id, MaterialId = materials[0].Id, Quantity = 0.12m, UnitPrice = materials[0].UnitPrice, IsChargedToCustomer = true },
                    new TicketMaterialUsage { TicketId = tickets[0].Id, MaterialId = materials[1].Id, Quantity = 0.20m, UnitPrice = materials[1].UnitPrice, IsChargedToCustomer = true },
                    new TicketMaterialUsage { TicketId = tickets[1].Id, MaterialId = materials[2].Id, Quantity = 1.00m, UnitPrice = materials[2].UnitPrice, IsChargedToCustomer = true },
                    new TicketMaterialUsage { TicketId = tickets[3].Id, MaterialId = materials[4].Id, Quantity = 0.35m, UnitPrice = materials[4].UnitPrice, IsChargedToCustomer = true },
                };
                context.TicketMaterialUsages.AddRange(ticketMaterialUsages);
                context.SaveChanges();

                } // end full seed when no tickets

                EnsureServiceMaterialQuotas(context);
                BackfillTicketMaterialUsagesFromLegacy(context);
                InvoiceSync.ResyncAllUnpaid(context);
                BackfillPaidInvoiceMetadata(context);
                BackfillCarsFromTickets(context);

                // --- Identity Seeding ---
                var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();

                string[] roleNames = { "Admin", "ThuNgan", "KTV", "QuanLyKho" };
                foreach (var roleName in roleNames)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                    }
                }

                var adminEmail = "admin@hp-auto.vn";
                var adminUser = await userManager.FindByNameAsync("admin_hpd");
                if (adminUser == null)
                {
                    adminUser = new AppUser
                    {
                        UserName = "admin_hpd",
                        Email = adminEmail,
                        FullName = "Nguyễn Văn Admin",
                        RequirePasswordChange = true,
                        IsActive = true
                    };
                    var createPowerUser = await userManager.CreateAsync(adminUser, "admin123");
                    if (createPowerUser.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, "Admin");
                    }
                }
                else
                {
                    // Force reset password to admin123
                    var token = await userManager.GeneratePasswordResetTokenAsync(adminUser);
                    await userManager.ResetPasswordAsync(adminUser, token, "admin123");
                }
            }
            catch (Exception ex)
            {
                // Ghi chú lỗi ra console hoặc logger nếu tiến trình Seeding gặp lỗi
                Console.WriteLine("LỖI KHỞI TẠO SEED DATA: " + ex.Message);
            }
        }

        /// <summary>
        /// Bổ sung định mức vật tư theo dịch vụ nếu bảng còn trống (idempotent).
        /// </summary>
        private static void EnsureServiceMaterialQuotas(HP_DetailingDbContext context)
        {
            if (context.ServiceMaterialQuotas.Any())
                return;

            var services = context.Services.ToList();
            var materials = context.Materials.ToList();
            if (services.Count == 0 || materials.Count == 0)
                return;

            var serviceByCode = services.ToDictionary(s => s.ServiceCode, StringComparer.OrdinalIgnoreCase);
            var materialByCode = materials.ToDictionary(m => m.MaterialCode, StringComparer.OrdinalIgnoreCase);

            var definitions = new (string ServiceCode, string MaterialCode, decimal DefaultQty, string? Notes)[]
            {
                ("RX-01", "VT001", 0.10m, "Dung dịch rửa xe bọt tuyết"),
                ("DT-01", "VT002", 0.20m, "Hóa chất bảo dưỡng da nội thất"),
                ("DB-01", "VT005", 0.50m, "Xi đánh bóng phá xước"),
                ("CR-01", "VT003", 1.00m, "Ceramic 9H — 1 hộp/xe con"),
                ("PM-01", "VT004", 3.00m, "Phim cách nhiệt — mét vuông ước tính"),
            };

            var quotas = new List<ServiceMaterialQuota>();
            foreach (var def in definitions)
            {
                if (!serviceByCode.TryGetValue(def.ServiceCode, out var service))
                    continue;
                if (!materialByCode.TryGetValue(def.MaterialCode, out var material))
                    continue;

                quotas.Add(new ServiceMaterialQuota
                {
                    ServiceId = service.Id,
                    MaterialId = material.Id,
                    DefaultQty = def.DefaultQty,
                    Notes = def.Notes,
                    CreatedAt = DateTime.Now
                });
            }

            if (quotas.Count > 0)
            {
                context.ServiceMaterialQuotas.AddRange(quotas);
                context.SaveChanges();
            }
        }

        /// <summary>
        /// Chuyển dữ liệu MaterialUsage cũ sang TicketMaterialUsage nếu bảng mới còn trống.
        /// </summary>
        private static void BackfillTicketMaterialUsagesFromLegacy(HP_DetailingDbContext context)
        {
            if (context.TicketMaterialUsages.Any())
                return;

            var legacy = context.MaterialUsages
                .Include(u => u.TicketService)
                .Where(u => u.ActualQty > 0)
                .ToList();

            if (legacy.Count == 0)
                return;

            var materialPrices = context.Materials.ToDictionary(m => m.Id, m => m.UnitPrice);
            var rows = new List<TicketMaterialUsage>();

            foreach (var u in legacy)
            {
                if (u.TicketService == null)
                    continue;

                rows.Add(new TicketMaterialUsage
                {
                    TicketId = u.TicketService.TicketId,
                    MaterialId = u.MaterialId,
                    Quantity = u.ActualQty,
                    UnitPrice = materialPrices.TryGetValue(u.MaterialId, out var price) ? price : 0,
                    IsChargedToCustomer = true
                });
            }

            if (rows.Count > 0)
            {
                context.TicketMaterialUsages.AddRange(rows);
                context.SaveChanges();
            }
        }

        /// <summary>
        /// Gán PaidAt và PT mặc định cho hóa đơn PAID cũ (trước khi có cột thanh toán).
        /// </summary>
        private static void BackfillPaidInvoiceMetadata(HP_DetailingDbContext context)
        {
            var paidWithoutDate = context.Invoices
                .Where(i => i.Status == "PAID" && i.PaidAt == null)
                .ToList();

            if (paidWithoutDate.Count == 0)
                return;

            var defaultMethod = context.PaymentMethods
                .FirstOrDefault(p => p.IsDefault && p.IsActive)
                ?? context.PaymentMethods.FirstOrDefault(p => p.IsActive);

            foreach (var inv in paidWithoutDate)
            {
                inv.PaidAt = inv.CreatedAt;
                if (!inv.PaymentMethodId.HasValue && defaultMethod != null)
                    inv.PaymentMethodId = defaultMethod.Id;
            }

            context.SaveChanges();
        }

        private static void BackfillCarsFromTickets(HP_DetailingDbContext context)
        {
            var existingPlates = context.Cars.Select(c => c.Plate).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var groups = context.Tickets
                .Where(t => t.Plate != null && t.Plate != "")
                .AsEnumerable()
                .GroupBy(t => t.Plate!.Trim().ToUpper())
                .Where(g => !existingPlates.Contains(g.Key));

            var toAdd = new List<Car>();
            foreach (var g in groups)
            {
                var latest = g.OrderByDescending(t => t.CreatedAt).First();
                toAdd.Add(new Car
                {
                    Plate = g.Key,
                    OwnerName = latest.CustomerName,
                    OwnerPhone = latest.CustomerPhone,
                    Model = latest.CarModel,
                    CreatedAt = g.Min(t => t.CreatedAt)
                });
            }

            if (toAdd.Count > 0)
            {
                context.Cars.AddRange(toAdd);
                context.SaveChanges();
            }
        }
    }
}
