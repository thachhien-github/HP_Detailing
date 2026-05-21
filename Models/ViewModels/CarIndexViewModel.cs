using System;
using System.Collections.Generic;

namespace HP_Detailing.Models.ViewModels
{
    public class CarIndexItem
    {
        public string Plate { get; set; } = string.Empty;
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string? Color { get; set; }
        public string CarModel { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime LastVisit { get; set; }
    }

    public class CarIndexViewModel
    {
        public List<CarIndexItem> Cars { get; set; } = new List<CarIndexItem>();
    }

    public class CarDetailDto
    {
        public string Plate { get; set; } = string.Empty;
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string? Color { get; set; }
        public string CarModel { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public List<CarHistoryItemDto> History { get; set; } = new List<CarHistoryItemDto>();
    }

    public class CarHistoryItemDto
    {
        public int TicketId { get; set; }
        public string TicketCode { get; set; } = string.Empty;
        public string CreatedAtStr { get; set; } = string.Empty;
        public string ServicesSummary { get; set; } = string.Empty;
        public string TotalAmountStr { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
