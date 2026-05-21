using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HP_Detailing.Data;
using HP_Detailing.Models;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using HP_Detailing.Hubs;
using HP_Detailing.Extensions;

namespace HP_Detailing.Controllers
{
    [Authorize(Roles = "Admin, QuanLyKho")]
    public class WarehouseController : Controller
    {
        private readonly HP_DetailingDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public WarehouseController(HP_DetailingDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // GET /warehouse
        public IActionResult Index()
        {
            var stocks = _context.WarehouseStocks
                .Include(w => w.Material)
                .OrderBy(w => w.Material!.MaterialCode)
                .ToList();

            ViewBag.TotalStockValue = stocks.Sum(s => s.QuantityOnHand * (s.Material?.UnitPrice ?? 0));
            ViewBag.LowStockCount = stocks.Count(s => s.QuantityOnHand > 0 && s.QuantityOnHand <= s.ReorderLevel);
            ViewBag.OutOfStockCount = stocks.Count(s => s.QuantityOnHand == 0);

            var recentUsage = _context.TicketMaterialUsages
                .Include(u => u.Material)
                .Include(u => u.Ticket)
                .Where(u => u.Quantity > 0)
                .OrderByDescending(u => u.Id)
                .Take(8)
                .ToList();
            ViewBag.RecentUsage = recentUsage;

            return View(stocks);
        }

        // GET /warehouse/imports
        [Route("warehouse/imports")]
        public IActionResult Imports(DateTime? startDate, DateTime? endDate)
        {
            var query = _context.StockImports
                .Include(i => i.Items)
                .Include(i => i.Supplier)
                .AsQueryable();

            if (startDate.HasValue)
            {
                var sDate = startDate.Value.Date;
                query = query.Where(i => i.ImportDate >= sDate);
            }
            if (endDate.HasValue)
            {
                var eDate = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(i => i.ImportDate <= eDate);
            }

            var imports = query.OrderByDescending(i => i.ImportDate).ToList();

            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View(imports);
        }

        // GET /warehouse/imports/new
        [Route("warehouse/imports/new")]
        public IActionResult ImportCreate()
        {
            var materials = _context.Materials.Where(m => m.IsActive).OrderBy(m => m.Name).ToList();
            var suppliers = _context.Suppliers.OrderByDescending(s => s.LastUsedAt).ToList();
            ViewBag.Materials = materials;
            ViewBag.Suppliers = suppliers;
            return View();
        }

        // GET /warehouse/imports/{id}
        [Route("warehouse/imports/{id:int}")]
        public IActionResult ImportDetail(int id)
        {
            var import = _context.StockImports
                .Include(i => i.Items).ThenInclude(item => item.Material)
                .Include(i => i.Supplier)
                .FirstOrDefault(i => i.Id == id);
            if (import == null) return NotFound();
            return View(import);
        }

        // === AJAX ===

        // POST /warehouse/materials/create
        public class CreateMaterialRequest
        {
            public string MaterialCode { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string? Unit { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal ReorderLevel { get; set; } = 5;
        }

        [HttpPost("warehouse/materials/create")]
        public IActionResult CreateMaterial([FromBody] CreateMaterialRequest model)
        {
            if (string.IsNullOrWhiteSpace(model.MaterialCode) || string.IsNullOrWhiteSpace(model.Name))
                return Json(new { success = false, message = "Mã và Tên vật tư là bắt buộc." });

            if (_context.Materials.Any(m => m.MaterialCode == model.MaterialCode))
                return Json(new { success = false, message = "Mã vật tư đã tồn tại." });

            var mat = new Material { MaterialCode = model.MaterialCode, Name = model.Name, Unit = model.Unit, UnitPrice = model.UnitPrice, IsActive = true };
            _context.Materials.Add(mat);
            _context.SaveChanges();

            _context.WarehouseStocks.Add(new WarehouseStock
            {
                MaterialId = mat.Id,
                QuantityOnHand = 0,
                ReorderLevel = model.ReorderLevel
            });
            _context.SaveChanges();

            return Json(new { success = true, data = new { mat.Id, mat.MaterialCode, mat.Name, mat.Unit, mat.UnitPrice } });
        }

        // GET /warehouse/materials/{id}
        [Route("warehouse/materials/{id:int}")]
        public IActionResult MaterialDetail(int id)
        {
            var stock = _context.WarehouseStocks
                .Include(s => s.Material)
                .FirstOrDefault(s => s.MaterialId == id);
            if (stock == null) return NotFound();

            var usageLog = _context.TicketMaterialUsages
                .Include(u => u.Ticket)
                .Where(u => u.MaterialId == id && u.Quantity > 0)
                .OrderByDescending(u => u.Id)
                .Take(20)
                .ToList();
            ViewBag.UsageLog = usageLog;
            return View(stock);
        }

        // POST /warehouse/materials/update
        [HttpPost("warehouse/materials/update")]
        public IActionResult UpdateMaterial([FromBody] UpdateMaterialRequest req)
        {
            var mat = _context.Materials.Find(req.Id);
            if (mat == null) return Json(new { success = false, message = "Không tìm thấy vật tư." });

            mat.Name = req.Name;
            mat.Unit = req.Unit;
            mat.UnitPrice = req.UnitPrice;

            var stock = _context.WarehouseStocks.FirstOrDefault(s => s.MaterialId == req.Id);
            if (stock != null) stock.ReorderLevel = req.ReorderLevel;

            _context.SaveChanges();
            return Json(new { success = true });
        }
        public class UpdateMaterialRequest
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? Unit { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal ReorderLevel { get; set; }
        }

        // POST /warehouse/materials/delete
        [HttpPost("warehouse/materials/delete")]
        public IActionResult DeleteMaterial([FromBody] int id)
        {
            var mat = _context.Materials.Find(id);
            if (mat == null) return Json(new { success = false, message = "Không tìm thấy vật tư." });
            mat.IsActive = false;
            _context.SaveChanges();
            return Json(new { success = true });
        }

        // POST /warehouse/imports/save
        public class ImportSaveRequest
        {
            public int? SupplierId { get; set; }
            public string? SupplierName { get; set; }   // if new supplier
            public string? SupplierPhone { get; set; }
            public string? Notes { get; set; }
            public string? InvoiceLink { get; set; }
            public string? ReceivedBy { get; set; }
            public string? PaymentStatus { get; set; }
            public List<ImportLineItem> Lines { get; set; } = new();
        }
        public class ImportLineItem
        {
            public int MaterialId { get; set; }
            public decimal Quantity { get; set; }
            public decimal UnitPrice { get; set; }
        }

        [HttpPost("warehouse/imports/save")]
        public async Task<IActionResult> SaveImport([FromBody] ImportSaveRequest req)
        {
            if (req.Lines == null || req.Lines.Count == 0)
                return Json(new { success = false, message = "Phiếu nhập phải có ít nhất 1 mặt hàng." });

            // Resolve or create supplier
            Supplier? supplier = null;
            if (req.SupplierId.HasValue && req.SupplierId > 0)
            {
                supplier = _context.Suppliers.FirstOrDefault(s => s.Id == req.SupplierId);
                if (supplier != null) supplier.LastUsedAt = DateTime.Now;
            }
            else if (!string.IsNullOrWhiteSpace(req.SupplierName))
            {
                // Check if supplier with same name already exists
                supplier = _context.Suppliers.FirstOrDefault(s => s.Name.ToLower() == req.SupplierName.ToLower());
                if (supplier != null)
                {
                    supplier.LastUsedAt = DateTime.Now;
                }
                else
                {
                    supplier = new Supplier
                    {
                        Name = req.SupplierName,
                        Phone = req.SupplierPhone,
                        CreatedAt = DateTime.Now,
                        LastUsedAt = DateTime.Now
                    };
                    _context.Suppliers.Add(supplier);
                    await _context.SaveChangesAsync();
                }
            }

            var now = DateTime.Now;
            var code = $"PN-{now:yyMM}-{(_context.StockImports.Count() + 1):D3}";

            var import = new StockImport
            {
                ImportCode = code,
                ImportDate = now,
                SupplierId = supplier?.Id,
                CreatedBy = User.Identity?.Name ?? "Admin",
                Notes = req.Notes,
                Status = "Hoàn tất",
                TotalAmount = req.Lines.Sum(l => l.Quantity * l.UnitPrice),
                InvoiceLink = req.InvoiceLink,
                ReceivedBy = req.ReceivedBy ?? User.Identity?.Name ?? "Admin",
                PaymentStatus = req.PaymentStatus ?? "Chưa thanh toán"
            };
            _context.StockImports.Add(import);
            await _context.SaveChangesAsync();

            foreach (var line in req.Lines)
            {
                _context.StockImportItems.Add(new StockImportItem
                {
                    StockImportId = import.Id,
                    MaterialId = line.MaterialId,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice
                });

                var stock = _context.WarehouseStocks.FirstOrDefault(s => s.MaterialId == line.MaterialId);
                if (stock != null)
                    stock.QuantityOnHand += line.Quantity;
                else
                    _context.WarehouseStocks.Add(new WarehouseStock { MaterialId = line.MaterialId, QuantityOnHand = line.Quantity, ReorderLevel = 5 });
            }

            await _context.SaveChangesAsync();

            // Gửi thông báo nhập kho thành công
            await NotificationHelper.SendNotificationAsync(
                _context,
                _hubContext,
                "Nhập kho thành công",
                $"Đã nhập kho phiếu {import.ImportCode} trị giá {import.TotalAmount:N0}đ.",
                "Warehouse",
                $"/warehouse/imports/{import.Id}"
            );

            return Json(new { success = true, importId = import.Id, importCode = import.ImportCode });
        }

        // GET /warehouse/materials/list
        [HttpGet("warehouse/materials/list")]
        public IActionResult MaterialsList()
        {
            var list = _context.Materials
                .Where(m => m.IsActive)
                .Select(m => new { m.Id, m.MaterialCode, m.Name, m.Unit, m.UnitPrice })
                .OrderBy(m => m.Name)
                .ToList();
            return Json(list);
        }

        // GET /warehouse/suppliers/list
        [HttpGet("warehouse/suppliers/list")]
        public IActionResult SuppliersList()
        {
            var list = _context.Suppliers
                .OrderByDescending(s => s.LastUsedAt)
                .Select(s => new { s.Id, s.Name, s.Phone, s.Address })
                .ToList();
            return Json(list);
        }

        // GET /warehouse/imports/export-report
        [HttpGet("warehouse/imports/export-report")]
        public IActionResult ExportImportsReport(DateTime? startDate, DateTime? endDate)
        {
            var query = _context.StockImports
                .Include(i => i.Supplier)
                .Include(i => i.Items)
                .ThenInclude(it => it.Material)
                .AsQueryable();

            if (startDate.HasValue)
            {
                var sDate = startDate.Value.Date;
                query = query.Where(i => i.ImportDate >= sDate);
            }
            if (endDate.HasValue)
            {
                var eDate = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(i => i.ImportDate <= eDate);
            }

            var imports = query.OrderByDescending(i => i.ImportDate).ToList();

            var csv = new System.Text.StringBuilder();
            // Add UTF-8 BOM so Excel opens it with correct encoding
            csv.Append('\uFEFF');
            csv.AppendLine("Mã phiếu nhập,Ngày nhập,Nhà cung cấp,Người lập,Người nhận hàng,Tổng tiền phiếu,Trạng thái thanh toán,Trạng thái phiếu,Mã vật tư,Tên vật tư,Số lượng,Đơn giá,Thành tiền,Ghi chú");

            foreach (var imp in imports)
            {
                if (imp.Items == null || imp.Items.Count == 0)
                {
                    csv.AppendLine($"\"{imp.ImportCode}\",\"{imp.ImportDate:dd/MM/yyyy HH:mm}\",\"{imp.Supplier?.Name ?? ""}\",\"{imp.CreatedBy ?? ""}\",\"{imp.ReceivedBy ?? ""}\",{imp.TotalAmount},\"{imp.PaymentStatus ?? ""}\",\"{imp.Status ?? ""}\",\"\",\"\",0,0,0,\"{imp.Notes ?? ""}\"");
                }
                else
                {
                    foreach (var item in imp.Items)
                    {
                        var lineTotal = item.Quantity * item.UnitPrice;
                        csv.AppendLine($"\"{imp.ImportCode}\",\"{imp.ImportDate:dd/MM/yyyy HH:mm}\",\"{imp.Supplier?.Name ?? ""}\",\"{imp.CreatedBy ?? ""}\",\"{imp.ReceivedBy ?? ""}\",{imp.TotalAmount},\"{imp.PaymentStatus ?? ""}\",\"{imp.Status ?? ""}\",\"{item.Material?.MaterialCode ?? ""}\",\"{item.Material?.Name ?? ""}\",{item.Quantity},{item.UnitPrice},{lineTotal},\"{imp.Notes ?? ""}\"");
                    }
                }
            }

            var fileBytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            var fileName = $"BaoCao_NhapKho_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            return File(fileBytes, "text/csv", fileName);
        }
        // ==========================================
        // PHASE 4: AUDIT (KIỂM KÊ KHO) — CÓ LƯU LỊCH SỬ
        // ==========================================

        // GET /warehouse/audit — Trang form kiểm kê (giữ nguyên)
        [Route("warehouse/audit")]
        public IActionResult Audit()
        {
            var stocks = _context.WarehouseStocks
                .Include(w => w.Material)
                .OrderBy(w => w.Material!.Name)
                .ToList();

            return View(stocks);
        }

        // GET /warehouse/audit/history-page — Trang UI xem lịch sử kiểm kê
        [Route("warehouse/audit/history-page")]
        public IActionResult AuditHistoryPage()
        {
            return View("AuditHistory");
        }

        // GET /warehouse/list — API phân trang danh sách tồn kho + tìm kiếm
        [HttpGet("warehouse/list")]
        public IActionResult WarehouseList(int page = 1, int pageSize = 5, string? search = null, string status = "all")
        {
            var query = _context.WarehouseStocks
                .Include(w => w.Material)
                .Where(w => w.Material != null && w.Material.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var q = search.ToLower().Trim();
                query = query.Where(w =>
                    w.Material!.Name.ToLower().Contains(q) ||
                    w.Material.MaterialCode.ToLower().Contains(q));
            }

            if (status == "out")
            {
                query = query.Where(w => w.QuantityOnHand == 0);
            }
            else if (status == "low")
            {
                query = query.Where(w => w.QuantityOnHand > 0 && w.QuantityOnHand <= w.ReorderLevel);
            }
            else if (status == "ok")
            {
                query = query.Where(w => w.QuantityOnHand > w.ReorderLevel);
            }

            var totalCount = query.Count();
            var items = query
                .OrderBy(w => w.Material!.MaterialCode)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(w => new
                {
                    materialId = w.MaterialId,
                    materialCode = w.Material!.MaterialCode,
                    name = w.Material.Name,
                    unit = w.Material.Unit ?? "—",
                    unitPrice = w.Material.UnitPrice,
                    quantityOnHand = w.QuantityOnHand,
                    reorderLevel = w.ReorderLevel,
                    status = w.QuantityOnHand == 0 ? "out" : w.QuantityOnHand <= w.ReorderLevel ? "low" : "ok"
                })
                .ToList();

            return Json(new { items, totalCount, page, pageSize });
        }

        // GET /warehouse/audit/history — API phân trang lịch sử phiên kiểm kê
        [HttpGet("warehouse/audit/history")]
        public IActionResult AuditHistory(int page = 1, int pageSize = 5)
        {
            var query = _context.AuditSessions.AsQueryable();
            var totalCount = query.Count();

            var items = query
                .OrderByDescending(a => a.AuditDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new
                {
                    a.Id,
                    a.AuditCode,
                    auditDate = a.AuditDate.ToString("HH:mm, dd/MM/yyyy"),
                    a.AuditedBy,
                    a.TotalItems,
                    a.DiscrepancyCount,
                    a.Notes
                })
                .ToList();

            return Json(new { items, totalCount, page, pageSize });
        }

        // GET /warehouse/audit/{id} — Chi tiết 1 phiên kiểm kê (JSON)
        [HttpGet("warehouse/audit/{id:int}")]
        public IActionResult AuditDetail(int id)
        {
            var session = _context.AuditSessions
                .Include(a => a.Items).ThenInclude(i => i.Material)
                .FirstOrDefault(a => a.Id == id);

            if (session == null)
                return Json(new { success = false, message = "Không tìm thấy phiên kiểm kê." });

            return Json(new
            {
                success = true,
                data = new
                {
                    session.Id,
                    session.AuditCode,
                    auditDate = session.AuditDate.ToString("HH:mm, dd/MM/yyyy"),
                    session.AuditedBy,
                    session.TotalItems,
                    session.DiscrepancyCount,
                    session.Notes,
                    items = session.Items.OrderBy(i => i.Material?.MaterialCode).Select(i => new
                    {
                        materialCode = i.Material?.MaterialCode ?? "",
                        materialName = i.Material?.Name ?? "",
                        unit = i.Material?.Unit ?? "—",
                        systemQuantity = i.SystemQuantity,
                        actualQuantity = i.ActualQuantity,
                        discrepancy = i.Discrepancy,
                        note = i.Note ?? ""
                    })
                }
            });
        }

        // POST /warehouse/audit/create — Tạo snapshot kiểm kê mới
        // Cũng giữ route cũ /warehouse/SaveAudit cho backward compatibility
        [HttpPost("warehouse/audit/create")]
        [HttpPost("warehouse/SaveAudit")]
        public IActionResult CreateAudit([FromBody] AuditSaveRequest request)
        {
            try
            {
                if (request == null || request.Items == null || !request.Items.Any())
                {
                    return Json(new { success = false, message = "Không có dữ liệu kiểm kê!" });
                }

                // Lấy toàn bộ tồn kho hiện tại để snapshot
                var allStocks = _context.WarehouseStocks.ToList();
                var stockDict = allStocks.ToDictionary(s => s.MaterialId, s => s);

                // Tạo mã phiên kiểm kê
                var now = DateTime.Now;
                var sessionCount = _context.AuditSessions.Count() + 1;
                var auditCode = $"KK-{now:yyMM}-{sessionCount:D3}";

                // Tạo phiên kiểm kê
                var session = new AuditSession
                {
                    AuditCode = auditCode,
                    AuditDate = now,
                    AuditedBy = User.Identity?.Name ?? "Admin",
                    Notes = request.Notes,
                    TotalItems = request.Items.Count,
                    DiscrepancyCount = 0
                };
                _context.AuditSessions.Add(session);
                _context.SaveChanges(); // Lưu để lấy Id

                int discrepancyCount = 0;

                foreach (var item in request.Items)
                {
                    decimal systemQty = 0;
                    if (stockDict.TryGetValue(item.MaterialId, out var stock))
                    {
                        systemQty = stock.QuantityOnHand;
                    }

                    var discrepancy = item.ActualQuantity - systemQty;

                    // Lưu snapshot cho MỌI vật tư trong danh sách kiểm
                    _context.AuditSessionItems.Add(new AuditSessionItem
                    {
                        AuditSessionId = session.Id,
                        MaterialId = item.MaterialId,
                        SystemQuantity = systemQty,
                        ActualQuantity = item.ActualQuantity,
                        Discrepancy = discrepancy,
                        Note = item.Note
                    });

                    // Chỉ cập nhật stock nếu có chênh lệch
                    if (Math.Abs(discrepancy) > 0.001m)
                    {
                        discrepancyCount++;
                        if (stock != null)
                        {
                            stock.QuantityOnHand = item.ActualQuantity;
                        }
                    }
                }

                session.DiscrepancyCount = discrepancyCount;
                _context.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = $"Lưu phiếu kiểm kê {auditCode} thành công! {discrepancyCount} mặt hàng có chênh lệch đã được cập nhật.",
                    auditCode,
                    sessionId = session.Id,
                    discrepancyCount
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        // ==========================================
        // PHASE 5: IMPORTS PAGING + SUMMARY
        // ==========================================

        // GET /warehouse/imports/list — API phân trang imports + summary toàn filter
        [HttpGet("warehouse/imports/list")]
        public IActionResult ImportsList(int page = 1, int pageSize = 5, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.StockImports
                .Include(i => i.Items)
                .Include(i => i.Supplier)
                .AsQueryable();

            if (startDate.HasValue)
            {
                var sDate = startDate.Value.Date;
                query = query.Where(i => i.ImportDate >= sDate);
            }
            if (endDate.HasValue)
            {
                var eDate = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(i => i.ImportDate <= eDate);
            }

            // Summary tính trên TOÀN BỘ kết quả filter (không theo page)
            var totalCount = query.Count();
            var totalValue = query.Sum(i => (decimal?)i.TotalAmount) ?? 0;

            // Phân trang
            var items = query
                .OrderByDescending(i => i.ImportDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(i => new
                {
                    i.Id,
                    i.ImportCode,
                    importDate = i.ImportDate.ToString("HH:mm, dd/MM/yyyy"),
                    supplierName = i.Supplier != null ? i.Supplier.Name : "—",
                    createdBy = i.CreatedBy ?? "—",
                    itemCount = i.Items.Count,
                    i.TotalAmount,
                    i.Status
                })
                .ToList();

            return Json(new
            {
                items,
                totalCount,
                page,
                pageSize,
                summary = new
                {
                    totalReceipts = totalCount,
                    totalValue
                }
            });
        }

        // ==========================================
        // REQUEST MODELS
        // ==========================================

        public class AuditSaveRequest
        {
            public string? Notes { get; set; }
            public List<AuditItemRequest> Items { get; set; } = new List<AuditItemRequest>();
        }

        public class AuditItemRequest
        {
            public int MaterialId { get; set; }
            public decimal ActualQuantity { get; set; }
            public string? Note { get; set; }
        }
    }
}
