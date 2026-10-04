using System.Diagnostics;
using System.Text;
using System.IO.Compression;
using YamlDotNet.RepresentationModel;
namespace MavenROStudio;
static class Verify {
    public static int Run(string[] args) {
        var log=new List<string>();int exit=0;
        try {
            var ct=CancellationToken.None;
            var rows=LuaData.Parse(Encoding.UTF8.GetBytes("tbl = { [501] = { identifiedDisplayName = \"Potion\", identifiedResourceName = \"포션\", identifiedDescriptionName = {\"a } [999] =\", \"b\"} }, -- comment\n [502] = { identifiedDisplayName = \"Second\", identifiedResourceName = \"test\", identifiedDescriptionName = {\"\"} } }"),"test",65001,ct);
            if(rows.Count!=2||rows[0].Id!=501||!rows[0].Description.Contains("a }"))throw new Exception("Lua string/braces regression");
            var textualLub=LuaData.Parse(Encoding.UTF8.GetBytes("tbl={[777]={identifiedDisplayName=\"Text LUB\",identifiedResourceName=\"text_lub\",identifiedDescriptionName={\"\"}}}"),"textual.lub",949,ct);
            if(textualLub.Count!=1||textualLub[0].Id!=777)throw new Exception("Textual .lub detection regression");
            var utf8Auto=LuaData.Parse(Encoding.UTF8.GetBytes("tbl={[778]={identifiedDisplayName=\"โพชั่น\",identifiedResourceName=\"utf8_item\",identifiedDescriptionName={\"\"}}}"),"utf8.lua",949,ct);
            if(utf8Auto.Count!=1||utf8Auto[0].Name!="โพชั่น")throw new Exception("UTF-8 auto-detection regression");
            try{LuaData.Parse([27,76,117,97],"compiled",874,ct);throw new Exception("Bytecode was not rejected");}catch(InvalidDataException){}
            try{LuaData.Parse(Encoding.ASCII.GetBytes("x = {[1]={identifiedDisplayName=\"unterminated"),"bad",874,ct);throw new Exception("Bad Lua accepted");}catch(InvalidDataException){}
            log.Add("PASS Lua/LUB content detection: text .lua, textual .lub, UTF-8 auto, bytecode signature, empty strings");
            var cancelled=new CancellationToken(true);
            try {LuaData.Parse(Encoding.ASCII.GetBytes("x = {}"),"cancel",874,cancelled);throw new Exception("Cancellation ignored");} catch(OperationCanceledException){}
            log.Add("PASS parser cancellation");
            using(var descriptionPreview=new RichTextBox()){
                RagnarokDescriptionPreview.Render(descriptionPreview,"Normal ^FF0000Red^000000 text\n^0000CCBlue^000000");
                if(descriptionPreview.Text!="Normal Red text\nBlue")throw new Exception("Description preview stripped text incorrectly");
                int redAt=descriptionPreview.Text.IndexOf("Red",StringComparison.Ordinal);
                descriptionPreview.Select(redAt,3);
                if(descriptionPreview.SelectionColor.ToArgb()!=Color.FromArgb(255,0,0).ToArgb())throw new Exception("Description preview color regression");
            }
            log.Add("PASS Ragnarok Description preview: line breaks + ^RRGGBB color rendering");
            string wrapped=RagnarokColorHelper.Wrap("Warning","ff0000",true);
            if(wrapped!="^FF0000Warning^000000"||RagnarokColorHelper.Code("#0033cc")!="^0033CC"||RagnarokColorHelper.BasicColors.Count<12)
                throw new Exception("Client color helper regression");
            log.Add($"PASS Client Color Helper: {RagnarokColorHelper.BasicColors.Count} basic colors + selection wrap + reset code");

            var barterYaml=new YamlStream();
            using(var sr=new StringReader("Header:\n  Type: BARTER_DB\n  Version: 1\nBody:\n  - Name: VERIFY_SHOP\n    Items:\n      - Index: 7\n        Item: Red_Potion\n        Stock: 5\n        RequiredItems:\n          - Index: 0\n            Item: Jellopy\n            Amount: 2\n"))barterYaml.Load(sr);
            var barterRoot=(YamlMappingNode)barterYaml.Documents[0].RootNode;
            var barterBody=(YamlSequenceNode)barterRoot.Children[new YamlScalarNode("Body")];
            var barterShop=(YamlMappingNode)barterBody.Children[0];
            var cloned=BarterYamlOps.CloneItem(barterShop,(YamlMappingNode)BarterYamlOps.Items(barterShop).Children[0]);
            if(BarterYamlOps.U(cloned,"Index")==7)throw new Exception("Barter clone reused item index");
            var added=BarterYamlOps.AddItem(barterShop,"Orange_Potion");
            var reqAdded=BarterYamlOps.AddRequirement(added,"Jellopy",3,-1);
            var reqs=BarterYamlOps.Requirements(added);
            BarterYamlOps.AddRequirement(added,"Fluff",4,5);
            if(reqs.Children.Count!=2||BarterYamlOps.U(reqAdded,"Amount")!=3)throw new Exception("Barter recipe add regression");
            if(!BarterYamlOps.Move(reqs,reqs.Children[1],-1,true)||BarterYamlOps.U((YamlMappingNode)reqs.Children[0],"Index")!=0)throw new Exception("Barter recipe reorder/reindex regression");
            reqs.Children.RemoveAt(1);BarterYamlOps.Reindex(reqs);
            if(reqs.Children.Count!=1||BarterYamlOps.U((YamlMappingNode)reqs.Children[0],"Index")!=0)throw new Exception("Barter recipe delete/reindex regression");
            log.Add("PASS Barter CRUD: add/clone reward, add/delete/reorder recipe, safe auto-index");

            string temp=Path.Combine(Path.GetTempPath(),"MavenROStudioVerify-"+Guid.NewGuid().ToString("N"));
            try {
                Directory.CreateDirectory(Path.Combine(temp,"client","System"));
                Directory.CreateDirectory(Path.Combine(temp,"server","db","import"));
                string clientOverride=Path.Combine(temp,"client","System","maven_ro_studio_iteminfo.lua");
                string serverOverride=Path.Combine(temp,"server","db","import","maven_ro_studio_item_db.yml");
                string master=Path.Combine(temp,"server","db","item_db.yml");
                File.WriteAllText(master,"Header:\n  Type: ITEM_DB\n  Version: 3\n\nFooter:\n  Imports:\n  - Path: db/import/item_db.yml\n");
                var sample=new Item(990001,"Maven Test","Maven_Test","line 1\nline 2","verify","Type: Armor\nWeight: 10\nScript: |\n  bonus bAllStats,7;\n  bonus2 bDropAddRace,RC_All,20;\nEquipScript: |\n  bonus bMaxHP,2000;\nUnEquipScript: |\n  heal -100,0;");
                SafeProjectFile.UpsertClientOverride(clientOverride,sample,949);
                SafeProjectFile.UpsertServerOverride(serverOverride,sample);
                SafeProjectFile.EnsureServerImport(Path.Combine(temp,"server"),"db/import/maven_ro_studio_item_db.yml");
                var cRound=LuaData.Parse(File.ReadAllBytes(clientOverride),clientOverride,949,ct);
                var sRound=ItemDb.Load(serverOverride,ct);
                string savedServer=File.ReadAllText(serverOverride);
                if(cRound.Count!=1||cRound[0].Id!=sample.Id||sRound.Count!=1||sRound[0].Id!=sample.Id||!File.ReadAllText(master).Contains("maven_ro_studio_item_db.yml")
                    ||!savedServer.Contains("Script: |")||!savedServer.Contains("bonus bAllStats,7;")||!savedServer.Contains("EquipScript: |")||!savedServer.Contains("UnEquipScript: |"))
                    throw new Exception("Editor/script round-trip regression");

                string live=Path.Combine(temp,"client","System","itemInfo_C.lua");
                string legacy="-- sentinel:"+((char)0xA1)+"\r\ntbl_custom = {\r\n  [990001] = { identifiedDisplayName = \"Old\", identifiedResourceName = \"OLD\", identifiedDescriptionName = {\"old\"}, slotCount = 2, ClassNum = 77, costume = true },\r\n}\r\n";
                File.WriteAllBytes(live,Encoding.Latin1.GetBytes(legacy));
                SafeProjectFile.UpsertClientTableEntry(live,"tbl_custom",sample,949);
                byte[] liveBytes=File.ReadAllBytes(live);
                string liveRaw=Encoding.Latin1.GetString(liveBytes);
                if(!liveBytes.Contains((byte)0xA1)||!liveRaw.Contains("ClassNum = 77")||!liveRaw.Contains("slotCount = 2")||!liveRaw.Contains("costume = true"))throw new Exception("Client block upsert did not preserve legacy bytes/metadata");
                log.Add("PASS Item Editor safe override round-trip + live tbl_custom block update + server import installation");
            } finally {try{Directory.Delete(temp,true);}catch{}}
            int p=Array.IndexOf(args,"--client");
            if(p>=0&&p+1<args.Length) {
                var sw=Stopwatch.StartNew();var a=new Assets();a.Load(args[p+1],false,ct,_=>{});
                log.Add($"GRF index: {a.Entries.Count:N0} winning entries; {sw.Elapsed.TotalSeconds:N2}s; managed memory {GC.GetTotalMemory(false)/1048576:N0} MiB");
                int multiProviders=a.Providers.Count(x=>x.Value.Count>1);
                if(multiProviders==0)throw new Exception("GRF provider/shadow chain is empty");
                log.Add($"PASS GRF provider chains: {multiProviders:N0} resources have 2+ providers");
                var largeArchives=Directory.EnumerateFiles(args[p+1],"*.grf",SearchOption.TopDirectoryOnly)
                    .Select(f=>new FileInfo(f))
                    .Where(f=>f.Length>int.MaxValue)
                    .OrderByDescending(f=>f.Length)
                    .ToList();
                foreach(var f in largeArchives)log.Add($"PASS large GRF >2GB indexed: {f.Name} = {f.Length:N0} bytes ({f.Length/1073741824d:N3} GiB)");
                log.AddRange(a.Messages);
                if(a.PriorityByArchive.Count==0)throw new Exception("DATA.ini priority map is empty");
                log.Add("DATA.ini priority map: "+string.Join(", ",a.PriorityByArchive.OrderBy(x=>x.Value).Select(x=>$"{x.Value}={Path.GetFileName(x.Key)}")));
                var packedInfos=a.Entries.Values
                    .Where(e=>e.Path.Contains("iteminfo",StringComparison.OrdinalIgnoreCase)
                        && (e.Path.EndsWith(".lua",StringComparison.OrdinalIgnoreCase)||e.Path.EndsWith(".lub",StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(e=>a.PriorityByArchive.TryGetValue(e.Archive,out var pri)?pri:int.MaxValue)
                    .ToList();
                foreach(var entry in packedInfos)log.Add("Packed itemInfo candidate: "+Assets.Display(entry.Path)+" @ "+Path.GetFileName(entry.Archive));
                if(packedInfos.Count>0) {
                    var packed=packedInfos[0];
                    try {
                        sw.Restart();
                        var data=a.Read(packed.Path,ct);
                        var packedItems=LuaData.Parse(data.Bytes,data.Source,949,ct);
                        log.Add($"Packed itemInfo parse: {packedItems.Count:N0} entries, {sw.Elapsed.TotalSeconds:N2}s; source={data.Source}");
                        if(packedItems.Count==0)exit=1;
                    } catch(Exception ex) {
                        log.Add("Packed itemInfo parse failed: "+ex.Message);
                        exit=1;
                    }
                }
                if(Assets.RawKey(Assets.Display(Assets.RawKey(@"data\texture\유저인터페이스\item\빨간포션.bmp")))!=Assets.RawKey(@"data\texture\유저인터페이스\item\빨간포션.bmp"))throw new Exception("CP949 path roundtrip failed");
                log.Add("PASS Korean path byte preservation");
                int found=0, encrypted=0, failed=0;
                foreach(var asset in a.Entries.Values.Where(x=>x.Path.EndsWith(".bmp",StringComparison.OrdinalIgnoreCase)&&Assets.Display(x.Path).Contains(@"\item\",StringComparison.OrdinalIgnoreCase)).Take(100)) {
                    try {var data=a.Read(asset.Path,ct);using var ms=new MemoryStream(data.Bytes);using var img=System.Drawing.Image.FromStream(ms);found++;}
                    catch(NotSupportedException){encrypted++;}
                    catch(Exception ex){failed++;log.Add("IMAGE FAIL "+Assets.Display(asset.Path)+" "+ex.Message);}
                }
                log.Add($"Image sample: decoded {found}; unsupported encrypted {encrypted}; failures {failed}");
                if(failed>0)exit=1;
                var info=Path.Combine(args[p+1],"System","itemInfo_true.lub");
                if(File.Exists(info)) {
                    try{
                        sw.Restart();var items=LuaData.Parse(File.ReadAllBytes(info),info,949,ct);
                        log.Add($"Real itemInfo: {items.Count:N0} entries, {sw.Elapsed.TotalSeconds:N2}s");
                        foreach(var i in items.Where(x=>x.Id==54000||x.Id==501)) {
                            log.Add($"Item {i.Id}: {i.Name}; resource={Assets.Display(i.Resource)}");
                            try{var data=a.ItemImage(i,false,ct);using var ms=new MemoryStream(data.Bytes);using var img=Image.FromStream(ms);log.Add($"Item {i.Id} image: {img.Width}x{img.Height}; {data.Source}");}
                            catch(Exception ex){log.Add($"Item {i.Id} image unresolved: {ex.Message}");}
                        }
                    }catch(Exception ex){log.Add("Real itemInfo unsupported or invalid: "+ex.Message);exit=1;}
                }
            }
            int s=Array.IndexOf(args,"--server");
            if(s>=0&&s+1<args.Length) {
                var catalog=new ProjectCatalog();
                catalog.Update("",args[s+1],new Dictionary<int,Item>(),new Dictionary<int,Item>(),null);
                catalog.Discover(ct);
                var active=catalog.RecommendedItemDbFiles();
                var activeItems=new Dictionary<int,Item>();
                foreach(var file in active)foreach(var item in ItemDb.Load(file,ct))activeItems[item.Id]=item;
                log.Add($"SERVER catalog: activeDB={active.Count}, items={activeItems.Count:N0}, aegis={catalog.ByAegis.Count:N0}, barter={catalog.BarterFiles.Count:N0}, pet={catalog.PetFiles.Count:N0}, npc={catalog.NpcFiles.Count:N0}");
                if(active.Count==0||activeItems.Count==0||catalog.BarterFiles.Count==0||catalog.PetFiles.Count==0||catalog.NpcFiles.Count==0)throw new Exception("Server project discovery incomplete");
                foreach(var file in catalog.BarterFiles.Take(5))SafeProjectFile.ValidateYaml(File.ReadAllText(file));
                foreach(var file in catalog.PetFiles.Take(5))SafeProjectFile.ValidateYaml(File.ReadAllText(file));
                var scriptHelper=ServerScriptCatalog.Load(args[s+1]);
                string[] requiredBonusHelpers=["bAllStats","bDropAddRace","bMaxHP","bMaxHPrate","bSubRace","bAddRace","bMagicAddRace"];
                var missingBonusHelpers=requiredBonusHelpers.Where(name=>!scriptHelper.Any(x=>x.Bonus.Equals(name,StringComparison.OrdinalIgnoreCase))).ToArray();
                if(scriptHelper.Count<50||missingBonusHelpers.Length>0)
                    throw new Exception($"Server Script Helper incomplete: {scriptHelper.Count} definitions; missing={string.Join(",",missingBonusHelpers)}");
                log.Add($"PASS Server Script Helper: {scriptHelper.Count:N0} source-derived bonus definitions including {string.Join(", ",requiredBonusHelpers)}");
                log.Add("PASS Server editor discovery + active Item DB + Barter/Pet YAML validation");
                string projectReport=ProjectValidatorControl.BuildReport(catalog);
                if(!projectReport.Contains("Server Items",StringComparison.Ordinal)||!projectReport.Contains("Unknown numeric NPC item refs",StringComparison.Ordinal)||!projectReport.Contains("Hot Week / Secure V2 issues",StringComparison.Ordinal))throw new Exception("Project Validator report regression");
                log.Add("PASS Project Validator headless report + Hot Week V2 diagnostics");
            }

            var infoArgs=new List<string>();
            for(int i=0;i<args.Length-1;i++)if(args[i]=="--info")infoArgs.Add(args[i+1]);
            if(infoArgs.Count>0) {
                var merged=new Dictionary<int,Item>();
                foreach(var file in infoArgs) {
                    var sw=Stopwatch.StartNew();
                    var fileBytes=File.ReadAllBytes(file);
                    string format=LuaData.Describe(fileBytes,949);
                    var parsed=LuaData.Parse(fileBytes,file,949,ct);
                    log.Add($"INFO {Path.GetFileName(file)}: {format}; parsed={parsed.Count:N0}, min={parsed.Min(x=>x.Id)}, max={parsed.Max(x=>x.Id)}, {sw.Elapsed.TotalSeconds:N2}s");
                    foreach(var item in parsed)merged[item.Id]=item;
                }
                log.Add($"INFO UNION: configured={infoArgs.Count}, unique={merged.Count:N0}, min={merged.Keys.Min()}, max={merged.Keys.Max()}");
                log.Add("INFO high IDs sample: "+string.Join(",",merged.Keys.Where(x=>x>=50000).OrderBy(x=>x).Take(30)));
                int itemArg=Array.IndexOf(args,"--item");
                if(itemArg>=0&&itemArg+1<args.Length&&int.TryParse(args[itemArg+1],out var inspectId)) {
                    if(!merged.TryGetValue(inspectId,out var inspected))throw new Exception($"Requested item {inspectId} missing from merged itemInfo stack");
                    log.Add($"INFO ITEM {inspectId}: winnerName={inspected.Name}; resource={Assets.Display(inspected.Resource)}; source={inspected.Source}");
                    int serverArg=Array.IndexOf(args,"--server");
                    if(serverArg>=0&&serverArg+1<args.Length) {
                        var compareCatalog=new ProjectCatalog();
                        compareCatalog.Update("",args[serverArg+1],merged,new Dictionary<int,Item>(),null);
                        compareCatalog.Discover(ct);
                        var serverMap=new Dictionary<int,Item>();
                        foreach(var dbFile in compareCatalog.RecommendedItemDbFiles())
                            foreach(var serverItem in ItemDb.Load(dbFile,ct))serverMap[serverItem.Id]=serverItem;
                        compareCatalog.Update("",args[serverArg+1],merged,serverMap,null);
                        var comparison=compareCatalog.CompareFor(inspectId);
                        log.Add($"INFO ITEM {inspectId} SYNC: {comparison.Status} • {comparison.Reason} • client={Path.GetFileName(comparison.ClientSource)} • server={Path.GetFileName(comparison.ServerSource)}");
                        if(comparison.Status=="MISMATCH")throw new Exception($"Item {inspectId} still mismatched after source-priority resolution: {comparison.Reason}");
                    }
                }
            }

            int d=Array.IndexOf(args,"--db");
            if(d>=0&&d+1<args.Length){var sw=Stopwatch.StartNew();var items=ItemDb.Load(args[d+1],ct);log.Add($"Real YAML: {items.Count:N0} items, {sw.Elapsed.TotalSeconds:N2}s");if(items.Count==0)exit=1;}
        }catch(Exception ex){log.Add(ex.ToString());exit=1;}
        log.Add("Exit="+exit);
        File.WriteAllLines(Path.Combine(AppContext.BaseDirectory,"verification.txt"),log,new UTF8Encoding(false));
        return exit;
    }
}
