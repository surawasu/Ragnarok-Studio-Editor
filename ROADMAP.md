# MAVEN RO Studio Roadmap

## Vision
MAVEN RO Studio evolves from a GRF/item inspector into a Ragnarok development IDE. All tools share one Project Catalog so Client, Server, GRF resources, scripts, shops and pets resolve against the same item/resource truth.

## v4.2.1 Designer Regression Hotfix
- Harden ItemInspector Designer serialization so manual resize does not drop columns/button metadata
- Move Item Editor top-level visible controls into designer ownership
- Restore Item Editor item-list pane to 350px with safe minimum sizing
- Fix Client Color Helper dark-theme contrast and HEX field colors
- Add smoke assertions for designer persistence + contrast

## v4.2 All Pages Designer
- Designer-backed UserControl for every sidebar page
- Item Inspector / GRF Explorer / Data Sources / Diagnostics extracted from Program.cs
- Item / Barter / NPC / Pet / Validator have dedicated `.cs + .Designer.cs` entry points
- Shared YAML editor base is designer-friendly for inherited Barter/Pet pages
- Parameterless constructors and design-time-safe initialization on all editor pages
- 9/9 sidebar page runtime/designer smoke assertion

## v4.1 Visual Studio Designer Ready
- Standard `Studio.cs` + `Studio.Designer.cs` WinForms structure
- Public parameterless designer constructor with heavy-runtime initialization guard
- Designer owns the real shell controls used at runtime
- Runtime editors injected into designer-owned `contentHost`
- `.csproj` Form/DependentUpon metadata for Solution Explorer nesting and View Designer
- DesignTimeBuild + runtime smoke verification

## v4.0 Flat Purple UI
- Global reusable purple-black flat theme system
- Modern sidebar with active state and grouped navigation
- Project Workspace header and dark status bar
- Consistent flat buttons, inputs, tabs, tables, cards and editor surfaces
- Purple selected rows/tabs and semantic sync status colors
- Preserve light Ragnarok Description Game Preview while the surrounding IDE stays dark
- No functional changes to Item/GRF/Barter/NPC/Pet/Validator workflows

## v3.6 Client Description Color Helper
- Basic Ragnarok `^RRGGBB` color palette + quick swatches
- Custom HEX input with live swatch
- Apply color to selection with automatic `^000000` reset
- Insert color/reset codes at cursor
- Remove all color codes without removing description text
- Live Game Preview integration

## v3.5 Description Preview + Server Script Helper
- Ragnarok-style Description preview with `^RRGGBB` color rendering and preserved line layout
- Raw/Preview split so editing never destroys client color codes
- Editable Script / EquipScript / UnEquipScript tabs in Item Editor
- Source-derived Script Helper from current server `script_constants.hpp` + `pc.cpp`
- Searchable bonus list with syntax/tooltips/source and double-click insertion into selected script target
- Script block round-trip support in server override writer

## v3.4 Item Editor Workflow
- Full virtual item list from Client + Server + Catalog
- Click-to-load editing flow with search and sync status
- GRF-resolved item icon preview
- Mandatory confirmation before Save Client / Save Server / Save Both; default action is No
- Automatic item-list refresh after Client/Server rebuild

## v3.3 Visual Barter Shop Layout
- Sold Items grid on upper-left with Shop / Index / Item ID / AegisName / Name / Stock / Success / Zeny
- Required Items grid on lower-left with Item ID / AegisName / Name / Amount / Refine
- Item Preview on upper-right with GRF-resolved icon and Client/Server metadata
- Equip Preview canvas on lower-right with View/Resource context, ready for future SPR/ACT character composite renderer
- Edit / Hot Week separated into a right-side tab; Raw YAML moved to its own tab
- Reward/recipe CRUD from v3.2 preserved

## v3.2 Barter Shop CRUD + Craft Recipe Editor
- Reward Item CRUD: add / clone / delete / reorder
- Edit reward AegisName, Zeny, Stock and Hot Week fields in one panel
- RequiredItems recipe CRUD: add / edit / delete / reorder multiple materials
- Safe reward auto-indexing; existing reward indices remain stable to preserve SQL stock mapping
- RequiredItems indices auto-renumber after reorder/delete
- Validation for duplicate/missing indices, empty/unknown Aegis and invalid amounts

## v3.1 Hot Week / Secure Barter V2
- Server-native Start / End window and EventId campaign namespace
- Stock 0 = unlimited; atomic limited-stock reservation for Secure V2
- SuccessRate 0..10000 and ConsumeOnFail policy
- Account / Character successful-purchase limits persisted in SQL
- rollback-safe purchase path and audit log
- structured Barter Shop Editor with V2 validation/template
- Project Validator checks schedule, rate, duplicate index, free retry and Aegis references

## v3.0 Editor Suite
1. GRF Explorer 2.0
   - DATA.ini priority and provider/shadow chain
   - resource preview / extract
   - resource-to-item dependency trace
   - missing icon/resource diagnostics
2. Item Editor
   - Client + Server side-by-side
   - MATCH / CLIENT ONLY / SERVER ONLY / MISMATCH / MISSING ICON
   - Client -> DB and DB -> Client conversion
   - safe client override source
   - safe server custom import + backup
3. Barter Shop Editor
   - discover BARTER_DB YAML files
   - load / edit / validate / save with backup
   - shop summary and item-reference validation
4. NPC Editor
   - discover npc/*.txt
   - source editor, search, templates, basic syntax checks
   - item ID reference validation
   - backup on save
5. Pet Editor
   - discover PET_DB YAML files
   - pet summary grid
   - load / edit / validate / save with backup
   - item/Aegis reference validation
6. Project Validator
   - Client-only / Server-only / mismatch counts
   - missing icons/resources
   - duplicate client resources
   - invalid Barter/Pet YAML
   - unknown numeric NPC item references
   - export validation report

## Architecture
DATA.ini + GRF + itemInfo.lua/lub + item_db.yml + NPC/Barter/Pet sources -> ProjectCatalog -> all editors.

Safety rules:
- Never rewrite base GRFs.
- Backup text/YAML before save.
- Validate YAML before save.
- Client binary LUB is read-only; edits target the live textual custom table (`SystemEN/itemInfo_C.lua` / `tbl_custom`) with byte-preserving block updates and backup.
- Server generated item records live in a dedicated custom import.
- config.json ships beside the executable and carries Client/Server roots and editor source settings.

## Later
- SPR/ACT animation preview
- PAL preview
- custom GRF builder/writer
- map coordinate picker
- rAthena script language service/autocomplete
- live map-server reload integration
- diff/undo history across project files
