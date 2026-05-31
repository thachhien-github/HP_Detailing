# 📋 HP_Detailing - Staff Positions Management Implementation Guide

## 🎯 Overview
Complete staff positions/chức vụ management system with data reset capability for HP_Detailing ASP.NET Core application.

---

## 📁 Files Modified/Created

### 1. **DbInitializer.cs** - Data Reset Capability
**Location:** `Data/DbInitializer.cs`

**Changes:**
- Added `clearOldData` parameter to `Initialize()` method
- Created `ClearAllData()` method to safely delete all data and reset identity seeds
- Supports MySQL foreign key management during cleanup

**Key Methods:**
```csharp
public static async Task Initialize(HP_DetailingDbContext context, 
    IServiceProvider serviceProvider, 
    bool clearOldData = false)
```

### 2. **PositionsController.cs** - RESTful API for Positions
**Location:** `Controllers/PositionsController.cs` (NEW FILE)

**Endpoints:**
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/positions` | Get all positions with staff count |
| GET | `/api/positions/{id}` | Get position details with staff list |
| POST | `/api/positions` | Create new position |
| PUT | `/api/positions/{id}` | Update position |
| DELETE | `/api/positions/{id}` | Delete position (if no staff assigned) |
| POST | `/api/positions/bulk` | Bulk create positions |

**Request/Response Examples:**

```json
// POST /api/positions - Create Position
REQUEST:
{
    "positionCode": "CV01",
    "name": "Kỹ thuật viên",
    "description": "Nhân viên kỹ thuật thi công"
}

RESPONSE:
{
    "success": true,
    "message": "Thêm vị trí 'Kỹ thuật viên' thành công.",
    "data": {
        "id": 1,
        "positionCode": "CV01",
        "name": "Kỹ thuật viên",
        "description": "Nhân viên kỹ thuật thi công"
    }
}
```

### 3. **StaffController.cs** - Enhanced Position Management
**Location:** `Controllers/StaffController.cs`

**New Methods:**

| Method | Route | Purpose |
|--------|-------|---------|
| `PositionsList()` | GET `/staff/positions-list` | Get all positions for dropdowns |
| `UpdateStaffPositionAjax()` | POST `/staff/update-position` | Update individual staff position |
| `ReassignPositionAjax()` | POST `/staff/reassign-position` | Bulk reassign positions |
| `ResetAllDataAjax()` | POST `/staff/reset-all-data` | Full data reset (requires confirmation) |

**Usage Examples:**

```json
// POST /staff/update-position - Update Staff Position
{
    "staffId": 1,
    "positionId": 2
}

// POST /staff/reassign-position - Reassign All Staff from One Position to Another
{
    "fromPositionId": 1,
    "toPositionId": 2
}

// POST /staff/reset-all-data - Full Data Reset
{
    "confirmed": true,
    "confirmationCode": "XOA_HAY_DU_LIEU"
}
```

### 4. **_PositionManagerModal.cshtml** - UI Component
**Location:** `Views/Shared/_PositionManagerModal.cshtml` (NEW FILE)

**Features:**
- ✅ Add new positions with form validation
- ✅ Edit existing positions inline
- ✅ Delete positions (validation: cannot delete if staff assigned)
- ✅ Bulk position creation
- ✅ Data reset with double confirmation
- ✅ Responsive design with Tailwind CSS

### 5. **Index.cshtml** - Updated Staff View
**Location:** `Views/Staff/Index.cshtml`

**Changes:**
- Added "Quản lý vị trí" (Manage Positions) button to header
- Integrated Position Manager Modal
- Button color: Purple (#9333EA)

---

## 🚀 How to Use

### **Feature 1: Add New Position**

1. Go to **Staff Management** page (`/Staff`)
2. Click **"Quản lý vị trí"** (Manage Positions) button
3. Fill in the form:
   - **Mã chức vụ** (Position Code): e.g., `CV07`
   - **Tên chức vụ** (Position Name): e.g., `Trưởng phòng`
   - **Mô tả** (Description): Optional
4. Click **"Thêm vị trí"** button
5. Toast notification confirms success

### **Feature 2: Edit Position**

1. In Position Manager Modal, hover over a position
2. Click ✏️ **Edit** button
3. Modify the details
4. Click **"Lưu"** button
5. Changes auto-sync with all staff in that position

### **Feature 3: Delete Position**

1. In Position Manager Modal, hover over a position
2. Click 🗑️ **Delete** button
3. If position has no staff, it's deleted immediately
4. If position has staff, error message appears: "Cannot delete - X staff members assigned"

### **Feature 4: Update Staff Position**

**Via UI:**
1. Click on a staff member to open detail page
2. Find the Position dropdown
3. Select new position
4. Save changes

**Via API:**
```bash
curl -X POST http://localhost:5000/staff/update-position \
  -H "Content-Type: application/json" \
  -d '{
    "staffId": 1,
    "positionId": 2
  }'
```

### **Feature 5: Bulk Reassign Positions**

Replace all staff from one position with another:

```bash
curl -X POST http://localhost:5000/staff/reassign-position \
  -H "Content-Type: application/json" \
  -d '{
    "fromPositionId": 1,
    "toPositionId": 2
  }'
```

**Response:**
```json
{
    "success": true,
    "message": "Đã gán lại 3 nhân viên từ vị trí 'Kỹ thuật viên' sang vị trí 'Quản lý vận hành'.",
    "data": {
        "staffCount": 3
    }
}
```

### **Feature 6: Full Data Reset** ⚠️ DANGER

**Steps:**
1. Click **"Quản lý vị trí"** (Manage Positions)
2. Scroll to **"Vùng Nguy hiểm"** (Danger Zone)
3. Click **"Xóa toàn bộ dữ liệu & khởi tạo lại"**
4. Modal appears asking for confirmation
5. **Enter confirmation code:** `XOA_HAY_DU_LIEU`
6. Click **"Xóa toàn bộ"**

**What Gets Deleted:**
- ✓ All Staff, Positions, Contracts, Payroll
- ✓ All Service Tickets, Appointments, Invoices
- ✓ All Materials, Warehouse Stock, Audit Logs
- ✓ All related transaction data

**What Gets Created:**
- ✓ 6 default positions (CV01-CV06)
- ✓ 5 sample staff members
- ✓ 5 service categories with 5 services
- ✓ 5 sample materials with warehouse stock
- ✓ 5 sample appointments and tickets
- ✓ 5 sample invoices

---

## 🗄️ Default Positions (After Data Reset)

| Code | Name | Description |
|------|------|-------------|
| CV01 | Kỹ thuật viên | Nhân viên kỹ thuật thi công dịch vụ detailing |
| CV02 | Cố vấn dịch vụ | Tư vấn dịch vụ, tiếp khách và báo giá |
| CV03 | Thu ngân | Thu ngân, thanh toán và xuất hóa đơn |
| CV04 | Kế toán / Lễ tân | Kế toán tài chính và lễ tân tiếp khách |
| CV05 | Quản lý vận hành | Giám sát vận hành, điều phối nhân sự |
| CV06 | Quản lý / Quản đốc | Quản đốc xưởng, phụ trách kỹ thuật tổng |

---

## 🔧 API Reference

### Get All Positions
```bash
GET /api/positions

Response:
{
    "success": true,
    "data": [
        {
            "id": 1,
            "positionCode": "CV01",
            "name": "Kỹ thuật viên",
            "description": "...",
            "staffCount": 3
        }
    ]
}
```

### Create Position
```bash
POST /api/positions

Body:
{
    "positionCode": "CV07",
    "name": "Giám sát chất lượng",
    "description": "Kiểm tra chất lượng công việc"
}
```

### Update Position
```bash
PUT /api/positions/{id}

Body:
{
    "positionCode": "CV07",
    "name": "Giám sát chất lượng (Cập nhật)",
    "description": "..."
}
```

### Delete Position
```bash
DELETE /api/positions/{id}

Response:
{
    "success": true,
    "message": "Xóa vị trí 'Giám sát chất lượng' thành công."
}
```

### Bulk Create Positions
```bash
POST /api/positions/bulk

Body:
[
    { "positionCode": "CV07", "name": "Vị trí 1", "description": "..." },
    { "positionCode": "CV08", "name": "Vị trí 2", "description": "..." }
]
```

---

## 🔐 Security & Permissions

- **All endpoints require**: `[Authorize(Roles = "Admin")]`
- **Exception**: GET endpoints allow anonymous access (`[AllowAnonymous]`)
- **Data Reset**: Requires confirmation code: `XOA_HAY_DU_LIEU`
- **Validation**: Position codes must be unique, cannot delete if staff assigned

---

## 📊 Database Impact

### Tables Modified:
- ✓ `Positions` - Main position records
- ✓ `Staff` - Position foreign key relationships
- ✓ `StaffProfiles`, `LaborContracts`, `Payrolls` - Reset to empty
- ✓ `Tickets`, `Appointments`, `Invoices` - Reset to empty

### Identity Seeds Reset:
```sql
ALTER TABLE Positions AUTO_INCREMENT = 1;
ALTER TABLE Staff AUTO_INCREMENT = 1;
ALTER TABLE StaffProfiles AUTO_INCREMENT = 1;
ALTER TABLE LaborContracts AUTO_INCREMENT = 1;
ALTER TABLE Payrolls AUTO_INCREMENT = 1;
```

---

## ⚙️ Configuration

### Program.cs Initialization
```csharp
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<HP_DetailingDbContext>();
    context.Database.Migrate();
    
    // Standard initialization (preserves data)
    DbInitializer.Initialize(context, scope.ServiceProvider).GetAwaiter().GetResult();
    
    // Full reset initialization
    // DbInitializer.Initialize(context, scope.ServiceProvider, clearOldData: true)
    //     .GetAwaiter().GetResult();
}
```

---

## 🐛 Troubleshooting

### Issue: "Cannot delete position - X staff members assigned"
**Solution:** Use Reassign Positions feature to move staff to different position first

### Issue: Position code already exists
**Solution:** Each position code must be unique. Choose different code like CV99, CV100, etc.

### Issue: Data reset failed
**Solution:** 
1. Check database connection
2. Ensure MySQL foreign key checks are enabled
3. Verify admin user has sufficient permissions

---

## 📝 Code Examples

### Adding a Position Programmatically
```csharp
using (var context = new HP_DetailingDbContext(options))
{
    var position = new Position
    {
        PositionCode = "CV99",
        Name = "Custom Position",
        Description = "Custom description"
    };
    context.Positions.Add(position);
    context.SaveChanges();
}
```

### Updating Staff Position
```csharp
var staff = context.Staff.Find(staffId);
staff.PositionId = newPositionId;
staff.Position = context.Positions.Find(newPositionId).Name; // Sync legacy field
context.SaveChanges();
```

---

## 📞 Support

For issues or questions:
1. Check browser console for JavaScript errors
2. Check server logs at `{VSCODE_TARGET_SESSION_LOG}`
3. Verify database connection in `appsettings.json`
4. Ensure all migrations are applied: `dotnet ef database update`

---

**Last Updated:** May 30, 2026
**Version:** 1.0
**Status:** ✅ Production Ready
