# MAVEN RO Studio – Visual Studio Designer Guide (4.4)

## เปิด Designer

1. เปิด `MavenROStudio.csproj` ด้วย Visual Studio 2022
2. ใน Solution Explorer หาไฟล์หน้าที่ต้องการแก้
3. ขยายไฟล์ `<Page>.cs` จะเห็น `<Page>.Designer.cs` ซ้อนอยู่ด้านล่าง
4. คลิกขวาไฟล์ `<Page>.cs` แล้วเลือก **View Designer**

## หน้าที่เปิด Designer ได้

- `Studio.cs` — Main Shell
- `ItemInspectorPage.cs` — Item Inspector
- `GrfExplorerPage.cs` — GRF Explorer
- `ItemEditorControl.cs` — Item Editor
- `BarterEditorControl.cs` — Barter Shop
- `NpcEditorControl.cs` — NPC Editor
- `NpcModelBrowserForm.cs` — NPC Model Browser / SPR + ACT Animation Preview
- `PetEditorControl.cs` — Pet Editor
- `ProjectValidatorControl.cs` — Project Validator
- `DataSourcesPage.cs` — Data Sources
- `DiagnosticsPage.cs` — Diagnostics

`YamlEditorControl.cs` เป็น designer-friendly base control ของ Barter/Pet และเปิด Designer ได้เช่นกัน

## โครงไฟล์

แต่ละหน้าหลักใช้รูปแบบ WinForms มาตรฐาน:

- `<Page>.cs` — entry point / constructor สำหรับ Designer
- `<Page>.Designer.cs` — layout และ controls ที่ปรับด้วย Visual Studio Designer
- runtime/parser/save logic ยังคงแยกจาก layout เพื่อป้องกัน Designer โหลด GRF/DB โดยไม่จำเป็น

## สิ่งที่แก้ผ่าน Designer ได้

### Main Studio
- Header / Client / Server controls
- Sidebar / Branding / Navigation
- Content host
- Status bar
- ขนาดหน้าต่าง

### Item Inspector
- Search / Collection checkbox / Export / counters
- Item table
- Icon preview / Details area
- สัดส่วนซ้าย-ขวา

### GRF Explorer
- Header
- Toolbar buttons
- Resource table และ column widths

### Item Editor
- Main toolbar
- All Items area
- Item Preview area
- Client / Server host sizing
- Splitter และสัดส่วน layout

รายละเอียด Client/Server editor ภายใน host ยังคงถูกประกอบจาก runtime logic เพื่อรักษา parser/sync/save behavior เดิม แต่ตำแหน่งและขนาด host ปรับใน Designer ได้

### Barter Shop
- Sold Items
- Required Items
- Item / Equip Preview
- Edit / Hot Week
- Raw YAML
- สัดส่วนซ้าย-ขวาและบน-ล่าง

### NPC / Pet / Validator
- Toolbar
- File list / editor / report area
- Splitter / status bar

### Data Sources
- itemInfo stack
- Item DB stack
- ปุ่ม Add / Remove / Up / Down / Auto Detect / Reload

### Diagnostics
- Log surface และ spacing

## Design-time safety

ทุก editor มี parameterless constructor สำหรับ Visual Studio Designer และใช้ design-time guard เพื่อไม่ให้โหลด:

- GRF
- itemInfo / LUB / Lua
- Item DB
- NPC/Pet/Barter discovery
- Project Catalog ขนาดใหญ่

ตอน runtime โปรแกรมยังใช้ control ตัวเดียวกับที่ Designer แก้จริง

## ข้อควรระวัง

- แนะนำให้แก้ layout ผ่าน **View Designer** มากกว่าแก้ `.Designer.cs` ด้วยมือ
- อย่าย้าย GRF/DB loading เข้า constructor ก่อน design-time guard
- Logic CRUD / parser / save ควรอยู่ไฟล์ logic เดิม ไม่ควรย้ายเข้า Designer
- ถ้า Designer เปิดไม่ขึ้น ให้ Build Solution หนึ่งครั้งก่อน แล้วปิด/เปิด Designer ใหม่

## Verification

เวอร์ชัน 4.2 มี smoke assertion:

`All Pages Designer: 9/9 sidebar pages use designer-backed controls + parameterless constructors = PASS`

และผ่าน `DesignTimeBuild=true`
