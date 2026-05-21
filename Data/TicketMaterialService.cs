using HP_Detailing.Models;
using Microsoft.EntityFrameworkCore;

namespace HP_Detailing.Data
{
    /// <summary>
    /// Xuất vật tư theo phiếu DV: định mức từ ServiceMaterialQuotas + vật tư phụ thêm, trừ kho, cảnh báo tồn thấp.
    /// </summary>
    public static class TicketMaterialService
    {
        public sealed class MaterialLine
        {
            public int MaterialId { get; set; }
            public decimal Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public bool IsChargedToCustomer { get; set; } = true;
        }

        /// <summary>
        /// Áp định mức cho một dịch vụ (dùng khi AddService trên phiếu chi tiết).
        /// </summary>
        public static void ApplyQuotasForService(
            HP_DetailingDbContext context,
            int ticketId,
            int serviceId,
            IList<string> warnings)
        {
            var quotas = context.ServiceMaterialQuotas
                .Include(q => q.Material)
                .Where(q => q.ServiceId == serviceId)
                .ToList();

            foreach (var quota in quotas)
            {
                if (quota.Material == null) continue;
                RecordUsage(
                    context,
                    ticketId,
                    quota.MaterialId,
                    quota.DefaultQty,
                    quota.Material.UnitPrice,
                    isChargedToCustomer: true,
                    warnings);
            }
        }

        /// <summary>
        /// Gộp định mức của nhiều dịch vụ (tạo phiếu mới). Cho phép ghi đè SL/giá từ form (user chỉnh tay).
        /// </summary>
        public static void ApplyQuotasForServices(
            HP_DetailingDbContext context,
            int ticketId,
            IEnumerable<int> serviceIds,
            IReadOnlyList<MaterialLine>? overrides,
            IList<string> warnings)
        {
            var ids = serviceIds.Distinct().Where(id => id > 0).ToList();
            if (ids.Count == 0) return;

            var quotas = context.ServiceMaterialQuotas
                .Include(q => q.Material)
                .Where(q => ids.Contains(q.ServiceId))
                .ToList();

            var aggregated = new Dictionary<int, (decimal Qty, Material Material)>();
            foreach (var q in quotas)
            {
                if (q.Material == null) continue;
                if (aggregated.TryGetValue(q.MaterialId, out var existing))
                    aggregated[q.MaterialId] = (existing.Qty + q.DefaultQty, q.Material);
                else
                    aggregated[q.MaterialId] = (q.DefaultQty, q.Material);
            }

            var overrideByMaterial = overrides?
                .GroupBy(m => m.MaterialId)
                .ToDictionary(g => g.Key, g => g.Last()) ?? new Dictionary<int, MaterialLine>();

            foreach (var (materialId, (defaultQty, material)) in aggregated)
            {
                decimal qty = defaultQty;
                decimal unitPrice = material.UnitPrice;
                var isCharged = true;

                if (overrideByMaterial.TryGetValue(materialId, out var ov))
                {
                    qty = ov.Quantity > 0 ? ov.Quantity : defaultQty;
                    unitPrice = ov.UnitPrice > 0 ? ov.UnitPrice : material.UnitPrice;
                    isCharged = ov.IsChargedToCustomer;
                }

                RecordUsage(context, ticketId, materialId, qty, unitPrice, isCharged, warnings);
            }

            // Dòng gửi từ form nhưng không thuộc định mức DV đã chọn (phòng trường hợp gắn nhầm cờ)
            foreach (var ov in overrideByMaterial)
            {
                if (aggregated.ContainsKey(ov.Key)) continue;
                RecordUsage(
                    context,
                    ticketId,
                    ov.Key,
                    ov.Value.Quantity,
                    ov.Value.UnitPrice,
                    ov.Value.IsChargedToCustomer,
                    warnings);
            }
        }

        /// <summary>
        /// Vật tư ngoài định mức (user thêm thủ công trên form tạo phiếu).
        /// </summary>
        public static void ApplyExtraMaterials(
            HP_DetailingDbContext context,
            int ticketId,
            IEnumerable<MaterialLine> extras,
            IList<string> warnings)
        {
            foreach (var line in extras)
            {
                if (line.MaterialId <= 0 || line.Quantity <= 0) continue;
                RecordUsage(
                    context,
                    ticketId,
                    line.MaterialId,
                    line.Quantity,
                    line.UnitPrice,
                    line.IsChargedToCustomer,
                    warnings);
            }
        }

        private static void RecordUsage(
            HP_DetailingDbContext context,
            int ticketId,
            int materialId,
            decimal quantity,
            decimal unitPrice,
            bool isChargedToCustomer,
            IList<string> warnings)
        {
            if (quantity <= 0) return;

            var material = context.Materials.Find(materialId);
            if (material == null) return;

            context.TicketMaterialUsages.Add(new TicketMaterialUsage
            {
                TicketId = ticketId,
                MaterialId = materialId,
                Quantity = quantity,
                UnitPrice = unitPrice,
                IsChargedToCustomer = isChargedToCustomer
            });

            var stock = context.WarehouseStocks.FirstOrDefault(w => w.MaterialId == materialId);
            if (stock != null)
            {
                stock.QuantityOnHand = Math.Max(0, stock.QuantityOnHand - quantity);
                if (stock.QuantityOnHand <= stock.ReorderLevel)
                    warnings.Add($"⚠ {material.Name}: tồn còn {stock.QuantityOnHand} {material.Unit}");
            }
        }
    }
}
