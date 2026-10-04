# MAVEN RO Studio 4.2.1

เครื่องมือพัฒนา MAVEN-RO แบบรวม Client + Server + GRF + itemInfo + rAthena DB/Script ไว้ใน Project Catalog เดียว

## 4.2.1 Designer + Item Editor Hotfix

- แก้ Visual Studio Designer re-serialize แล้ว `ItemInspectorPage` ทำคอลัมน์/ข้อความปุ่มหาย โดยเปลี่ยนเป็น Designer code มาตรฐานและคงขนาดที่ปรับไว้ `1634x751`
- `ItemInspectorPage` เก็บ ColumnHeader เป็น designer-owned components: ID / ชื่อไอเทม / Resource / Source
- `Item Editor` ย้าย `Item ID`, Search, Item List, Icon Preview และ splitter ให้ `.Designer.cs` เป็นเจ้าของจริง เพื่อไม่ให้ Visual Studio ลบ control ตอน Save Designer
- แก้ Item Editor left pane จาก 121px เป็น 350px พร้อม MinSize ป้องกันถูกบีบจนอ่านรายการไม่ได้
- แก้ Color Helper จากพื้นสว่าง+ตัวอักษรสว่างเป็น dark surface และแก้ HEX input ให้ใช้ foreground/background ที่อ่านได้
- เพิ่ม regression smoke ตรวจ designer-owned layout, pane width, color contrast และ Item Inspector columns

## 4.2 All Pages Designer

- แยกทุกหน้าใน Sidebar เป็น WinForms UserControl ที่มีไฟล์ `<Page>.cs + <Page>.Designer.cs`
- หน้า Designer ครบ 9/9: `Item Inspector`, `GRF Explorer`, `Item Editor`, `Barter Shop`, `NPC Editor`, `Pet Editor`, `Project Validator`, `Data Sources`, `Diagnostics`
- `Item Editor` ย้ายโครง top toolbar / item list / preview / Client host / Server host เข้า Designer; logic parse/save/sync อยู่ไฟล์เดิม
- `Barter Shop` ย้ายโครง Sold Items / Required Items / Preview / Edit-Hot Week / Raw YAML เข้า Designer
- `NPC Editor`, `Pet Editor`, `Project Validator` มี Designer entry ของตัวเอง
- `Item Inspector`, `GRF Explorer`, `Data Sources`, `Diagnostics` เปลี่ยนจาก Panel ใน `Program.cs` เป็น UserControl Designer แยกจริง
- ทุกหน้ามี parameterless constructor สำหรับ Visual Studio Designer และ runtime ยังคงใช้ control ชุดเดียวกัน
- `.csproj` มี `SubType=UserControl` + `DependentUpon` ครบทุกหน้า
- ผ่าน DesignTimeBuild + runtime smoke: `All Pages Designer: 9/9 ... = PASS`

## 4.1 Visual Studio Designer Ready

- แยก Main Form เป็น `Studio.cs` + `Studio.Designer.cs` ตามโครง WinForms มาตรฐาน
- เพิ่ม public parameterless constructor สำหรับ Visual Studio Designer และ design-time guard เพื่อไม่โหลด GRF / itemInfo / DB / Project Catalog ตอนเปิด Designer
- Header, Client/Server controls, Sidebar, Navigation, `contentHost` และ Status bar เป็น controls ที่ Designer เป็นเจ้าของจริง การลาก/ปรับขนาด/เปลี่ยนสีใน Designer จะมีผลกับ runtime
- Runtime editor modules ถูก inject ลง `contentHost` หลังเปิดโปรแกรม จึงไม่ทำให้ Designer ค้าง
- เพิ่ม metadata ใน `.csproj` ให้ `Studio.Designer.cs` nested ใต้ `Studio.cs` และ `Studio.cs` มี `SubType=Form`
- ผ่าน `DesignTimeBuild` และ runtime smoke test
- คู่มือ: `DESIGNER_GUIDE.md`

## 4.0 Flat Purple UI

- ปรับหน้าตาโปรแกรมทั้งชุดเป็น Flat UI โทนม่วง-ดำ โดยไม่เปลี่ยน logic การทำงานเดิม
- เพิ่ม Theme System กลาง (`StudioTheme`) สำหรับ Background / Surface / Input / Border / Accent / Status colors
- Sidebar ใหม่กว้างขึ้น แบ่งหมวด Browse / Editors / Tools และมี Active Navigation สีม่วง
- Project Workspace header ใหม่ พร้อม Client / Server / Encoding / Reload Project ในแถบเดียว
- ปุ่ม Save/Reload ใช้ Purple Accent, Delete ใช้ Danger treatment และปุ่มทั่วไปเป็น dark flat surface
- TextBox, ComboBox, NumericUpDown, ListBox, RichTextBox, GroupBox, SplitContainer และ TabControl ใช้ภาษา UI เดียวกัน
- ListView/Table ใช้ owner-draw แบบ flat dark, selected row สีม่วง และ Status `MATCH / MISMATCH / CLIENT ONLY / SERVER ONLY` มี semantic color
- Tab active ใช้ Purple Accent; editor/script code ใช้ dark surface ขณะที่ Description Game Preview ยังคงพื้น tooltip แบบเกม
- Status bar และ diagnostics ปรับเป็น dark flat style พร้อม typography/spacing ใหม่

## 3.6 Client Description Color Helper

- เพิ่ม Color Helper ใน `Description Raw` สำหรับสร้างโค้ดสี Ragnarok `^RRGGBB` โดยไม่ต้องพิมพ์เอง
- มีสีพื้นฐานให้เลือก 19 สี รวม Red, Green, Blue, RO Blue `0000CC`, Link Blue `0033CC`, Yellow, Gold, Orange, Purple, Pink, Gray และสีอื่น ๆ
- มีปุ่มสีด่วน 8 สีที่แสดง swatch จริง
- `Apply to Selection` จะครอบข้อความที่เลือกเป็น `^COLORข้อความ^000000` อัตโนมัติ
- `Insert Code` แทรกโค้ดสี ณ cursor, `Reset ^000000` กลับเป็นสีปกติ และ `Remove Colors` ลบเฉพาะ color code โดยคงข้อความเดิม
- รองรับ HEX custom ทั้ง `FF0000`, `#FF0000`, `^FF0000` และแสดง swatch ตรวจสีทันที
- Game Preview update แบบ live ทุกครั้งที่แก้ Description หรือใส่สี

## 3.5 Description Preview + Server Script Helper

- Client Description มีแท็บ `Description Raw` และ `Game Preview`; Raw เก็บข้อความเดิมสำหรับแก้ไข ส่วน Preview render สี `^RRGGBB` และการขึ้นบรรทัดแบบ itemInfo โดยไม่ทำ color code หาย
- Preview ใช้กรอบ/พื้นหลังแบบ tooltip เพื่อให้ตรวจการจัดข้อความได้ใกล้เคียงในเกมก่อน Save
- Server Item Editor เพิ่มช่องแก้ `Script`, `EquipScript`, `UnEquipScript` และ Raw Source แยกเป็นแท็บ
- Server Script Helper อ่านโบนัสจาก `src/map/script_constants.hpp` + `src/map/pc.cpp` ของ Server ที่เลือกจริง จึงตรงกับ source build ปัจจุบัน
- Helper รองรับค้นหา, tooltip ความหมาย/syntax/source และ double-click เพื่อ insert snippet ลง Script / EquipScript / UnEquipScript ที่เลือก
- ตรวจจริงได้ 383 bonus definitions รวม `bAllStats`, `bDropAddRace`, `bMaxHP`, `bMaxHPrate`, `bSubRace`, `bAddRace`, `bMagicAddRace` และกลุ่ม bonus2/3/4/5
- Server writer รองรับ Script block round-trip; Save แล้ว `Script / EquipScript / UnEquipScript` ไม่ถูกทิ้งจาก override

## 3.4 Item Editor Workflow

- Item Editor แสดงรายการไอเทมทั้งหมดจาก Client + Server + Catalog ด้วย Virtual ListView เพื่อรองรับข้อมูล 29k+ โดยไม่สร้าง control หลายหมื่นแถว
- ตารางมี Item ID / AegisName / Name / Client Name / Sync Status และมีช่องค้นหา
- คลิก Item ในรายการแล้วจึงโหลดข้อมูล Client/Server มาแก้ไข ลดการโหลดรายละเอียดที่ไม่จำเป็น
- เพิ่ม Item Preview พร้อม icon ที่ resolve จาก GRF ตาม DATA.ini winner
- Save Client / Save Server / Save Both ต้องยืนยัน Yes/No ทุกครั้ง โดย default เป็น No และแสดง Item ID/ชื่อ/ไฟล์ปลายทางก่อนเขียน
- หลัง rebuild/reload Client หรือ Server รายการ Item Editor refresh ตาม Project Catalog อัตโนมัติ

## 3.3 Visual Barter Shop

- หน้า Visual Editor ใหม่จัดเป็น `Sold Items` ด้านซ้ายบน, `Required Items` ด้านซ้ายล่าง, `Item Preview` ด้านขวาบน และ `Equip Preview` ด้านขวาล่าง
- Sold Items แสดง Shop / Index / Item ID / AegisName / Name / Stock / Success / Zeny
- Required Items แสดง Index / Item ID / AegisName / Name / Amount / Refine
- Item Preview ดึง icon จาก GRF ที่ชนะตาม DATA.ini พร้อม Item ID, AegisName, Name, Type, Locations และ Client Resource
- Equip Preview มีพื้นที่ character/outfit preview พร้อม View/Resource context; ขั้นนี้เป็น preview canvas สำหรับเตรียมต่อ sprite/ACT composite renderer
- `Edit / Hot Week` แยกเป็นแท็บด้านขวา และ `Raw YAML` แยกเป็นแท็บต่างหากเพื่อให้หน้าแรกสะอาดขึ้น
- CRUD ของ Reward/Recipe จาก 3.2 ยังคงอยู่ครบ

## 3.2 Barter Craft Editor

- เพิ่ม/ลบ/Clone/ย้ายลำดับ Reward Item ได้จาก GUI
- แก้ Reward `Item` (AegisName), `Zeny`, Stock และ Hot Week fields ในหน้าจอเดียว
- เพิ่ม/แก้/ลบ/ย้ายลำดับ `RequiredItems` ได้หลายวัตถุดิบต่อสินค้า
- Reward Item ใหม่ใช้ Index ว่างอัตโนมัติ แต่ Item Index เดิมไม่ถูก renumber เพื่อรักษา SQL stock mapping
- RequiredItems จะ auto-renumber หลังย้าย/ลบ
- Validation ตรวจ duplicate/missing index, Item ว่าง, Amount=0 และ unknown Aegis
- Save ยังคง Validate YAML + สร้าง backup ก่อนเขียนไฟล์จริง

## 3.1 Hot Week / Secure Barter V2

- Barter Shop Editor มี structured controls สำหรับ EventId, Start/End, Stock, SuccessRate, ConsumeOnFail, AccountLimit และ CharacterLimit
- `Stock: 0` = unlimited, `SuccessRate: 10000` = 100%
- ตรวจช่วงเวลา, duplicate Index, stock overflow, rate, free-retry risk และ unknown Aegis ก่อน save
- Project Validator ตรวจ Hot Week / Secure V2 เพิ่มเติม
- Server source รองรับ field ชุดเดียวกับ Editor จริง พร้อม schedule gate, atomic stock/limit reservation, rollback และ audit SQL
- Secure V2 ซื้อทีละ 1 reward/request เพื่อให้ race/rollback deterministic
- รายละเอียด schema และ deployment อยู่ใน `HOT_WEEK_BARTER_V2.md`

## 3.0.1 Item Sync Fix

- แก้ Item Editor ที่ Save แล้วค้างสถานะ MISMATCH เพราะ write target ไม่ได้ถูกเลื่อนเป็น source priority สูงสุด
- `ClientItemWriteTarget` และ `ServerItemWriteTarget` ที่มีอยู่จริงจะถูกเลื่อนไปเป็น source ลำดับสุดท้าย/ผู้ชนะอัตโนมัติ
- หลัง Save Client/Server โปรแกรมจะแสดงสาเหตุ MISMATCH พร้อมค่าชื่อ Client/Server และ winner source
- แสดงจำนวน definition ซ้ำของ Item ID เพื่อจับ source override ที่ซ่อนอยู่

## 3.0 Ragnarok Development IDE

- GRF Explorer 2.0: provider/shadow chain, extract resource, resource → item dependency trace
- Item Editor: แก้ Client/Server คู่กัน, Client → DB, DB → Client, Save Client/Server/Both
- Client edit เขียนลง `SystemEN/itemInfo_C.lua` / `tbl_custom` ที่ client loader ใช้งานจริงแบบ block-level พร้อม backup และ preserve bytes; ไม่แก้ LUB binary ตรง ๆ
- Server edit เขียนลง `db/import/maven_ro_studio_item_db.yml` และติดตั้ง import ใน `db/item_db.yml` พร้อม backup
- Barter Shop Editor: ค้นหา BARTER_DB, summary, YAML validation, backup/save, template
- NPC Editor: ค้นหา npc/*.txt, source editor, basic script check, numeric item reference scan, template, backup/save
- Pet Editor: ค้นหา PET_DB, summary, YAML validation, backup/save, template
- Project Validator: Client/Server diff, missing icon, duplicate resource, YAML errors, unknown Barter/Pet Aegis refs, unknown NPC item IDs
- Auto Detect Item DB ตามโหมด Server; เครื่องปัจจุบันตรวจเป็น Pre-Renewal
- rAthena Item DB loader มี lenient fallback สำหรับ custom YAML ที่มี duplicate keys ซึ่ง server ใช้งานได้แต่ strict YAML parser ไม่ยอมรับ
- รายละเอียด roadmap อยู่ใน `ROADMAP.md`

## Lua / LUB Reader

- รองรับ `.lua` แบบ text
- รองรับ `.lub` แบบ Lua 5.1 bytecode
- รองรับ `.lub` ที่จริงเป็น Lua text และ `.lua` ที่เป็น bytecode โดยตรวจจาก content signature ไม่ยึดนามสกุลไฟล์
- Auto detect UTF-8 BOM, UTF-16 LE/BE และ UTF-8 ที่ valid
- fallback สำหรับ client เก่าเป็น CP949/CP874 ตาม Encoding ที่เลือก
- legacy text ใช้ replacement fallback เพื่อไม่ให้ mixed/invalid byte บางตัวทำให้ทั้งไฟล์โหลดล้ม
- Diagnostics แสดง format ที่ตรวจได้ต่อ source เช่น `Lua 5.1 bytecode` หรือ `Lua text (CP949)`

## Portable Config

- `config.json` อยู่ข้าง `MavenROStudio.exe` และถูก copy ไปกับทุก release
- เก็บ Client path, ItemInfoFiles, ItemDbFiles, Encoding และ LooseFirst
- ถ้าไม่พบ `config.json` จะ import ค่าเดิมจาก `%LocalAppData%\MavenROStudio\settings.json` ครั้งแรก
- release 4.0 ใส่ config ปัจจุบันของ MAVEN-RO Client + Server มาให้แล้ว

## Complete Item List

- แก้ Lua parser ที่เคยมอง empty string เช่น `{ "" }` เป็น EOF ทำให้ itemInfo.lua โหลดไม่ครบ
- source itemInfo แต่ละไฟล์โหลดแยกกัน ถ้าไฟล์หนึ่งเสียจะข้ามเฉพาะไฟล์นั้น ไม่ทำให้รายการจาก source ที่อ่านสำเร็จหาย
- ตัด async Filter ซ้ำหลัง Merge เพื่อลด race ที่ทำให้ VirtualListSize ค้างค่าชุดเก่า
- เพิ่มตัวนับ `Client / DB / Merged / Visible` บน Item Inspector
- smoke test ตรวจทั้งจำนวนข้อมูลและแถวสุดท้ายของ Virtual List
- ทดสอบ source จริง 3 ไฟล์แล้วได้ 29,237 Item ID ไม่ซ้ำ และ UI แสดงครบ 29,237 รายการ

## Large GRF > 2GB

- เปลี่ยน metadata ของ GRF entry จาก `int32` เป็น `long` เพื่อไม่ให้ archive ขนาดใหญ่ล้มเพราะ signed 2GB boundary
- อ่าน archive ด้วย `FileStream` ตำแหน่งแบบ 64-bit และตรวจ bounds ด้วย `long`
- จำกัด 64 MiB เฉพาะ asset ที่กำลัง preview เท่านั้น ไม่จำกัดขนาด GRF ทั้งไฟล์
- ทดสอบจริงกับ `data.grf` ขนาด 4,229,842,210 bytes (3.939 GiB) ผ่าน
- GRF 2.0 มาตรฐานยังมี offset field แบบ 32-bit ดังนั้น archive ที่ต้อง address ข้อมูลเกินช่วงประมาณ 4 GiB อาจเกินขอบเขตของ format เอง

## ฟังก์ชันหลัก

- อ่าน GRF ตามลำดับตัวเลขใน `[Data]` ของ `DATA.ini`
- เมื่อ resource ชื่อเดียวกันซ้ำกัน GRF ที่มี priority สูงกว่าจะเป็นตัวที่ชนะ
- รองรับ itemInfo หลายไฟล์ `.lua/.lub`
- รองรับ Item DB หลายไฟล์ `.yml/.yaml`
- ใน itemInfo และ Item DB ไฟล์ที่อยู่ล่างกว่าใน Source Stack จะ override ID ซ้ำจากไฟล์ด้านบน
- เพิ่ม/ลบ source และเลื่อนลำดับด้วยปุ่ม ↑ / ↓
- Auto Detect itemInfo จาก Client:
  - `System`
  - `data\luafiles514\lua files\datainfo`
  - itemInfo ที่อยู่ภายใน GRF stack
- source ที่ผู้ใช้เพิ่มเองจะยังอยู่ท้ายรายการเพื่อคง priority สูงกว่า source ที่ Auto Detect พบ
- อ่าน itemInfo ที่อยู่ภายใน GRF ได้โดยตรง ไม่ต้อง extract ก่อน
- Item Inspector แสดง source chain ทั้ง Client itemInfo และ Server Item DB พร้อมระบุ WINNER
- รูปไอเทม resolve จาก GRF stack ชุดเดียวกับที่โหลดจาก `DATA.ini`
- แสดง GRF ที่เป็นเจ้าของ icon และค่า priority จาก `DATA.ini`
- GRF Explorer แสดง resource ที่ชนะ พร้อมชื่อ GRF, priority, ขนาด และ flags
- ใช้ virtual list, background loading, debounce และ cancellation เพื่อลดอาการค้าง

## หน้าจอ

Sidebar ซ้าย:
- Item Inspector
- GRF Explorer
- Data Sources
- Diagnostics

พื้นที่กลางใช้แสดงรายการและรายละเอียดของหน้าที่เลือก

## วิธีใช้งาน

1. เปิด `release-4.2.1\MavenROStudio.exe`
2. เลือก Client folder ที่มี `DATA.ini`
3. กด **โหลด DATA.ini / GRF**
4. ไปที่ **Data Sources**
5. ที่ itemInfo สามารถกด **Auto Detect** หรือเพิ่มไฟล์เอง
6. จัดลำดับด้วย **↑ ขึ้น / ↓ ลง** โดย source ที่อยู่ล่างสุดมี priority สูงสุดเมื่อ Item ID ซ้ำ
7. เพิ่ม Item DB ของ Server ได้หลายไฟล์และจัดลำดับแบบเดียวกัน
8. ไปที่ **Item Inspector** แล้วค้นหาด้วย Item ID / ชื่อ / Resource
9. เลือกไอเทมเพื่อดู source chain, winner, description, server DB และ GRF ที่ใช้แสดง icon

## Encoding

เลือก encoding ให้ตรงกับ itemInfo:
- Korean CP949
- Thai CP874
- UTF-8

โปรแกรมไม่ execute Lua script แต่ decode เฉพาะข้อมูล literal และ Lua 5.1 bytecode ที่อยู่ในขอบเขตที่รองรับ

## การยืนยันกับ MAVEN-RO Client

ทดสอบกับ:
`D:\MAVEN-RO\06_Client-14_10_2025 By Speedrun`

ลำดับ DATA.ini ที่ตรวจพบ:
- 0 = maven-main.grf
- 1 = en.grf
- 2 = official_data.grf
- 3 = data.grf

ผลตรวจ:
- 357,397 winning GRF resources
- packed `iteminfo.lub` จาก `data.grf` อ่านได้ 9,647 รายการ
- `System\itemInfo_true.lub` อ่านได้ 21,482 รายการ
- sample icon 100 รูป decode สำเร็จทั้งหมด
- Item 501 resolve icon 24x24 จาก `data.grf`
- verification Exit=0

## Build / Verify

Build:

`dotnet build MavenROStudio.csproj -c Release`

Publish:

`dotnet publish MavenROStudio.csproj -c Release -o release-4.2.1`

Verify:

`MavenROStudio.exe --verify --client "CLIENT_FOLDER"`

UI smoke:

`MavenROStudio.exe --ui-smoke`

ผลตรวจจะถูกเขียนเป็น `verification.txt`, `ui-smoke.txt` และ `ui-preview.png`

## ขอบเขตปัจจุบัน

ยังไม่รองรับ GRF ที่เข้ารหัส, Lua ที่ต้อง execute logic จริง, preview SPR/ACT แบบสวมใส่, และ editor สำหรับ Monster/NPC/Barter แบบเต็มรูปแบบ
