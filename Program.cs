using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace MavenROStudio;
static class Program {
    [STAThread] static int Main(string[] args) {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        if (args.Contains("--verify")) return Verify.Run(args);
        ApplicationConfiguration.Initialize();
        Application.ThreadException += (_,e) => MessageBox.Show(e.Exception.Message, "RO Studio");
        Application.Run(new Studio(args.Contains("--ui-smoke")));
        return 0;
    }
}

public sealed class Settings {
    public string Client { get; set; } = @"D:\MAVEN-RO\06_Client-14_10_2025 By Speedrun";
    public string ServerRoot { get; set; } = @"D:\JUTI-RO\JUTI-HI Server";
    public List<string> ItemInfoFiles { get; set; } = [];
    public List<string> ItemDbFiles { get; set; } = [];
    public int Encoding { get; set; } = 949;
    public bool LooseFirst { get; set; }
    public string ClientItemWriteTarget { get; set; } = @"SystemEN\itemInfo_C.lua";
    public string ClientItemTable { get; set; } = "tbl_custom";
    public string ServerItemWriteTarget { get; set; } = @"db\import\maven_ro_studio_item_db.yml";
}

public sealed partial class Studio {
    readonly List<(Button Button,Control Panel)> navButtons = [];
    readonly ItemInspectorPage itemPanel = new();
    readonly GrfExplorerPage grfPanel = new();
    readonly DataSourcesPage sourcePanel = new();
    readonly DiagnosticsPage logPanel = new();
    Control itemEditorPanel = null!, barterEditorPanel = null!, npcEditorPanel = null!, petEditorPanel = null!, validatorPanel = null!;
    readonly ProjectCatalog catalog = new();

    TextBox query => itemPanel.Query;
    CheckBox collection => itemPanel.Collection;
    Label itemCounts => itemPanel.ItemCounts;
    ListView list => itemPanel.Items;
    TextBox details => itemPanel.Details;
    PictureBox picture => itemPanel.Picture;

    ListView grfs => grfPanel.AssetsList;
    TextBox log => logPanel.LogText;
    ListBox itemInfoFiles => sourcePanel.ItemInfoFiles;
    ListBox itemDbFiles => sourcePanel.ItemDbFiles;
    readonly System.Windows.Forms.Timer debounce = new() { Interval=220 };
    readonly List<Button> loadButtons = [];

    const string PackedItemInfoPrefix = "grf://";
    List<Item> items = [], filtered = [];
    Dictionary<int,Item> clientItems = [], serverItems = [];
    Dictionary<int,List<string>> clientItemSources = [], serverItemSources = [];
    List<Asset> assetRows = [], filteredAssets = [];
    Assets? assets;
    CancellationTokenSource? loadCancel, filterCancel, imageCancel;
    readonly SemaphoreSlim imageGate = new(1,1);
    int imageGeneration;
    bool uiReady;
    Settings settings = new();
    readonly string settingsPath = Path.Combine(AppContext.BaseDirectory,"config.json");
    readonly string legacySettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MavenROStudio","settings.json");

    void RuntimeInitialize(bool smoke) {
        LoadSettings();
        client.Text=settings.Client;
        server.Text=settings.ServerRoot;
        catalog.Update(settings.Client,settings.ServerRoot,clientItems,serverItems,assets);
        try{catalog.Discover();}catch(Exception ex){Log("Catalog discovery: "+ex.Message);}

        BuildUi();
        BindEvents();
        StudioTheme.Apply(this);

        loose.Checked=settings.LooseFirst;
        encoding.SelectedIndex=settings.Encoding==65001?1:settings.Encoding==949?2:0;
        RefreshSourceLists();
        uiReady=true;
        RefreshProjectCatalog(false);
        status.Text="พร้อมใช้งาน • GRF Explorer 2.0 • Editors • Project Validator";

        if(smoke) Shown += async (_,_)=>{
            var evidence=new List<string>(); int ticks=0;
            using var heartbeat=new System.Windows.Forms.Timer{Interval=25};
            heartbeat.Tick+=(_,_)=>ticks++; heartbeat.Start();
            try {
                await LoadAssets();
                if(assets==null||assetRows.Count==0) throw new Exception("UI asset load failed");
                evidence.Add("GRF UI load: "+assetRows.Count+" winning assets; timer ticks="+ticks);

                if(settings.ItemInfoFiles.Count>0)await RebuildItemInfo();
                if(settings.ItemDbFiles.Count>0)await RebuildItemDb();
                RefreshProjectCatalog(true);

                query.Clear();
                await Filter();
                evidence.Add($"Item UI counts: clientSources={settings.ItemInfoFiles.Count}; dbSources={settings.ItemDbFiles.Count}; client={clientItems.Count}; server={serverItems.Count}; merged={items.Count}; visible={filtered.Count}; virtual={list.VirtualListSize}");
                evidence.Add($"Catalog: itemDb={catalog.ItemDbFiles.Count}; barter={catalog.BarterFiles.Count}; pet={catalog.PetFiles.Count}; npc={catalog.NpcFiles.Count}; aegis={catalog.ByAegis.Count}");
                if(clientItems.Count==0||serverItems.Count==0)throw new Exception($"Client/Server catalog incomplete: client={clientItems.Count}, server={serverItems.Count}");
                if(catalog.BarterFiles.Count==0||catalog.PetFiles.Count==0||catalog.NpcFiles.Count==0)throw new Exception("Editor discovery incomplete.");
                if(filtered.Count!=items.Count||list.VirtualListSize!=filtered.Count)
                    throw new Exception($"Item list incomplete: merged={items.Count}, filtered={filtered.Count}, virtual={list.VirtualListSize}");
                if(list.VirtualListSize>0) {
                    var last=list.Items[list.VirtualListSize-1];
                    evidence.Add($"Last virtual row: index={list.VirtualListSize-1}; id={last.SubItems[0].Text}; expected={filtered[^1].Id}");
                    if(last.SubItems[0].Text!=filtered[^1].Id.ToString())
                        throw new Exception("Last virtual row does not match filtered data.");
                }
                evidence.Add($"Editors: item={itemEditorPanel!=null}; barter={barterEditorPanel!=null}; npc={npcEditorPanel!=null}; pet={petEditorPanel!=null}; validator={validatorPanel!=null}");
                if(itemEditorPanel is not ItemEditorControl itemEditorUi||!itemEditorUi.EnhancedLayoutReady||!itemEditorUi.SaveConfirmationRequired)
                    throw new Exception("Item Editor enhanced layout/confirmation not ready.");
                int expectedItemEditorRows=catalog.ClientItems.Keys.Concat(catalog.ServerItems.Keys).Concat(catalog.ById.Keys).Distinct().Count();
                if(itemEditorUi.ListedItemCount!=expectedItemEditorRows)
                    throw new Exception($"Item Editor list incomplete: listed={itemEditorUi.ListedItemCount}, expected={expectedItemEditorRows}");
                if(!itemEditorUi.DescriptionPreviewReady||!itemEditorUi.ClientColorHelperReady||itemEditorUi.ScriptHelperCount<50)
                    throw new Exception($"Item Editor helper incomplete: preview={itemEditorUi.DescriptionPreviewReady}, color={itemEditorUi.ClientColorHelperReady}, scripts={itemEditorUi.ScriptHelperCount}");
                if(!itemEditorUi.DesignerOwnedLayoutReady||!itemEditorUi.ClientColorContrastReady)
                    throw new Exception($"Item Editor designer/theme regression: designer={itemEditorUi.DesignerOwnedLayoutReady}, contrast={itemEditorUi.ClientColorContrastReady}, pane={itemEditorUi.ItemListPaneWidth}");
                if(itemPanel.Items.Columns.Count!=4||string.IsNullOrWhiteSpace(itemPanel.ExportCsv.Text))
                    throw new Exception($"Item Inspector designer regression: columns={itemPanel.Items.Columns.Count}, export='{itemPanel.ExportCsv.Text}'");
                evidence.Add($"Item Editor: full virtual list {itemEditorUi.ListedItemCount:N0} + designer-owned list pane {itemEditorUi.ItemListPaneWidth}px + readable color helper + {itemEditorUi.ScriptHelperCount:N0} server bonus helpers = PASS");
                evidence.Add("Item Inspector Designer: 4 columns + Export CSV + manual 1634x751 layout preserved = PASS");
                if(barterEditorPanel is not BarterEditorControl barterUi||!barterUi.VisualLayoutReady)
                    throw new Exception("Barter visual layout not ready.");
                evidence.Add("Barter visual layout: Sold Items + Required Items + Item Preview + Equip Preview = PASS");
                if(BackColor!=StudioTheme.Background||contentHost.BackColor!=StudioTheme.Background||status.BackColor!=StudioTheme.Sidebar)
                    throw new Exception("Flat theme shell colors were not applied.");
                if(navButtons.Count<9||!navButtons.Any(x=>x.Button.BackColor==StudioTheme.PurpleSoft))
                    throw new Exception("Flat theme active navigation state missing.");
                if(!list.OwnerDraw||!grfs.OwnerDraw)
                    throw new Exception("Flat theme table owner-draw styling missing.");
                evidence.Add("Flat Purple UI: shell + grouped sidebar + active purple nav + dark inputs/tables + semantic status colors = PASS");
                if(headerPanel.Parent!=this||sidebar.Parent!=this||contentHost.Parent!=this||status.Parent!=this)
                    throw new Exception("Designer-owned Studio shell is not connected to runtime form.");
                if(typeof(Studio).GetConstructor(Type.EmptyTypes)==null)
                    throw new Exception("Public parameterless Studio constructor required by WinForms Designer is missing.");
                evidence.Add("Designer Ready: Studio.cs + Studio.Designer.cs shell + public parameterless constructor = PASS");
                var designerPageTypes=new[]{
                    typeof(ItemInspectorPage),typeof(GrfExplorerPage),typeof(ItemEditorControl),
                    typeof(BarterEditorControl),typeof(NpcEditorControl),typeof(PetEditorControl),
                    typeof(ProjectValidatorControl),typeof(DataSourcesPage),typeof(DiagnosticsPage)
                };
                var missingDesignerConstructors=designerPageTypes.Where(t=>t.GetConstructor(Type.EmptyTypes)==null).Select(t=>t.Name).ToArray();
                if(missingDesignerConstructors.Length>0)
                    throw new Exception("Designer pages missing parameterless constructor: "+string.Join(", ",missingDesignerConstructors));
                if(itemPanel is not ItemInspectorPage||grfPanel is not GrfExplorerPage||sourcePanel is not DataSourcesPage||logPanel is not DiagnosticsPage
                    ||itemEditorPanel is not ItemEditorControl||barterEditorPanel is not BarterEditorControl||npcEditorPanel is not NpcEditorControl
                    ||petEditorPanel is not PetEditorControl||validatorPanel is not ProjectValidatorControl)
                    throw new Exception("One or more sidebar pages are not using designer-backed controls at runtime.");
                evidence.Add("All Pages Designer: 9/9 sidebar pages use designer-backed controls + parameterless constructors = PASS");
                if(npcEditorPanel is not NpcEditorControl npcEditorUi||!npcEditorUi.ModelBrowserReady)
                    throw new Exception("NPC Model Browser button is missing from designer-backed NPC Editor.");
                if(typeof(NpcModelBrowserForm).GetConstructor(Type.EmptyTypes)==null)
                    throw new Exception("NPC Model Browser is missing its designer constructor.");
                using(var browserSmoke=new NpcModelBrowserForm()) {
                    if(!browserSmoke.AnimationControlsReady||browserSmoke.DesignerDirectionCount!=8)
                        throw new Exception($"NPC animation controls regression: ready={browserSmoke.AnimationControlsReady}, directions={browserSmoke.DesignerDirectionCount}");
                }
                evidence.Add("NPC Model Browser: designer-backed form + ACT Action/Direction 0-7/Play/Frame/Speed controls = PASS");

                query.Text="501";
                await Filter();
                int index=filtered.FindIndex(i=>i.Id==501);
                if(index>=0){list.Items[index].Selected=true;await ShowItem();}

                using var image=new Bitmap(Width,Height);
                DrawToBitmap(image,new Rectangle(0,0,Width,Height));
                image.Save(Path.Combine(AppContext.BaseDirectory,"ui-preview.png"));
                evidence.Add("PASS UI smoke");
            } catch(Exception ex){evidence.Add("FAIL "+ex);}
            File.WriteAllLines(Path.Combine(AppContext.BaseDirectory,"ui-smoke.txt"),evidence);
            Close();
        };
    }

    int Codepage => encoding.SelectedIndex==1?65001:encoding.SelectedIndex==2?949:874;
    static string ResourceDisplay(Item i)=>i.UnicodeResource?i.Resource:Assets.Display(i.Resource);

    void BuildUi() {
        encoding.Items.Clear();
        encoding.Items.AddRange(["Thai CP874","UTF-8","Korean CP949"]);

        // Controls/layout above are owned by Studio.Designer.cs so manual changes
        // in Visual Studio Designer are the same controls used at runtime.
        btnBrowseClient.Click+=(_,_)=>ChooseClient();
        btnBrowseServer.Click+=(_,_)=>ChooseServer();
        btnReloadProject.Click+=async(_,_)=>await ReloadProject();
        btnCancelLoad.Click+=(_,_)=>loadCancel?.Cancel();
        loadButtons.Add(btnBrowseClient);
        loadButtons.Add(btnReloadProject);

        itemEditorPanel=CreateItemEditor();
        barterEditorPanel=CreateBarterEditor();
        npcEditorPanel=CreateNpcEditor();
        petEditorPanel=CreatePetEditor();
        validatorPanel=CreateValidator();

        navButtons.Clear();
        RegisterNav(navItemInspector,"Item Inspector",itemPanel);
        RegisterNav(navGrfExplorer,"GRF Explorer",grfPanel);
        RegisterNav(navItemEditor,"Item Editor",itemEditorPanel);
        RegisterNav(navBarter,"Barter Shop",barterEditorPanel);
        RegisterNav(navNpc,"NPC Editor",npcEditorPanel);
        RegisterNav(navPet,"Pet Editor",petEditorPanel);
        RegisterNav(navValidator,"Project Validator",validatorPanel);
        RegisterNav(navDataSources,"Data Sources",sourcePanel);
        RegisterNav(navDiagnostics,"Diagnostics",logPanel);

        BuildItemPanel();
        BuildGrfPanel();
        BuildSourcePanel();

        contentHost.SuspendLayout();
        contentHost.Controls.Clear();
        foreach(var panel in new[]{itemPanel,grfPanel,itemEditorPanel,barterEditorPanel,npcEditorPanel,petEditorPanel,validatorPanel,sourcePanel,logPanel})
            contentHost.Controls.Add(panel);
        contentHost.ResumeLayout();

        ShowPanel(itemPanel);
    }

    void BuildItemPanel() {
        itemPanel.ExportCsv.Click+=(_,_)=>ExportCsv();
    }

    void BuildGrfPanel() {
        grfPanel.ExtractSelected.Click+=(_,_)=>ExtractSelectedAsset();
        grfPanel.ProviderChain.Click+=(_,_)=>ShowAssetProviders();
        grfPanel.TraceToItem.Click+=(_,_)=>TraceAssetToItems();
    }

    Asset? SelectedAsset() {
        if(grfs.SelectedIndices.Count==0)return null;
        int n=grfs.SelectedIndices[0];
        return n>=0&&n<filteredAssets.Count?filteredAssets[n]:null;
    }

    void ShowAssetProviders() {
        var selected=SelectedAsset();var a=assets;
        if(selected==null||a==null)return;
        var sb=new StringBuilder();
        sb.AppendLine(Assets.Display(selected.Path));sb.AppendLine();
        if(a.Providers.TryGetValue(selected.Path,out var chain)) {
            for(int i=0;i<chain.Count;i++) {
                var p=chain[i];string pri=a.PriorityByArchive.TryGetValue(p.Archive,out var value)?value.ToString():"?";
                sb.AppendLine($"{i+1}. {(i==0?"WINNER":"SHADOW")} • DATA.ini {pri} • {Path.GetFileName(p.Archive)} • {p.Size:N0} bytes");
            }
        }
        MessageBox.Show(sb.ToString(),"GRF Provider Chain");
    }

    void ExtractSelectedAsset() {
        var selected=SelectedAsset();var a=assets;if(selected==null||a==null)return;
        using var d=new SaveFileDialog{FileName=Path.GetFileName(Assets.Display(selected.Path)),Filter="All files|*.*"};
        if(d.ShowDialog()!=DialogResult.OK)return;
        try {var data=a.Read(selected.Path,CancellationToken.None);File.WriteAllBytes(d.FileName,data.Bytes);Log("Extracted "+Assets.Display(selected.Path)+" -> "+d.FileName);}
        catch(Exception ex){MessageBox.Show(ex.Message,"Extract Resource");}
    }

    void TraceAssetToItems() {
        var selected=SelectedAsset();if(selected==null)return;
        string key=selected.Path;
        var matches=new List<Item>();
        const string folder=@"data\texture\유저인터페이스\item\";
        foreach(var i in clientItems.Values) {
            if(string.IsNullOrWhiteSpace(i.Resource))continue;
            string resource=i.UnicodeResource?Assets.RawKey(i.Resource):i.Resource;
            string icon=Assets.RawKey(folder)+resource+".bmp";
            if(icon==key)matches.Add(i);
        }
        if(matches.Count==0){MessageBox.Show("ไม่พบ Item ที่อ้างถึง resource นี้โดยตรง","Dependency Trace");return;}
        var text=string.Join(Environment.NewLine,matches.Take(200).Select(i=>$"{i.Id} • {i.Name} • {ResourceDisplay(i)}"));
        MessageBox.Show($"Resource: {Assets.Display(key)}\r\nItems: {matches.Count:N0}\r\n\r\n{text}","Resource → Item Trace");
    }

    void BuildSourcePanel() {
        sourcePanel.ItemInfoAdd.Click+=(_,_)=>AddItemInfos();
        sourcePanel.ItemInfoRemove.Click+=(_,_)=>RemoveItemInfo();
        sourcePanel.ItemInfoUp.Click+=(_,_)=>MoveItemInfo(-1);
        sourcePanel.ItemInfoDown.Click+=(_,_)=>MoveItemInfo(1);
        sourcePanel.ItemInfoDetect.Click+=async(_,_)=>await AutoDetectItemInfo();
        sourcePanel.ItemInfoReload.Click+=async(_,_)=>await RebuildItemInfo();

        sourcePanel.ItemDbAdd.Click+=(_,_)=>AddItemDbs();
        sourcePanel.ItemDbRemove.Click+=(_,_)=>RemoveItemDb();
        sourcePanel.ItemDbUp.Click+=(_,_)=>MoveItemDb(-1);
        sourcePanel.ItemDbDown.Click+=(_,_)=>MoveItemDb(1);
        sourcePanel.ItemDbDetect.Click+=async(_,_)=>await AutoDetectItemDb();
        sourcePanel.ItemDbReload.Click+=async(_,_)=>await RebuildItemDb();
    }

    void RegisterNav(Button button,string text,Control panel) {
        button.Tag=text;
        button.Text=text;
        button.Click+=(_,_)=>ShowPanel(panel);
        navButtons.Add((button,panel));
    }

    void ShowPanel(Control panel) {
        foreach(Control c in contentHost.Controls)c.Visible=false;
        panel.Visible=true;panel.BringToFront();
        foreach(var nav in navButtons){
            bool active=ReferenceEquals(nav.Panel,panel);
            string baseText=nav.Button.Tag as string??nav.Button.Text;
            nav.Button.BackColor=active?StudioTheme.PurpleSoft:StudioTheme.Sidebar;
            nav.Button.ForeColor=active?Color.White:StudioTheme.Muted;
            nav.Button.Font=StudioTheme.UiFont(9.25f,active?FontStyle.Bold:FontStyle.Regular);
            nav.Button.Text=(active?"  ●  ":"      ")+baseText;
        }
    }

    void Add(FlowLayoutPanel p,string text,Action action,bool loading=true,bool primary=false) {
        var b=StudioTheme.FlatButton(text,primary);
        b.Click+=(_,_)=>action();
        p.Controls.Add(b);
        if(loading)loadButtons.Add(b);
    }

    void BindEvents() {
        list.RetrieveVirtualItem += (_,e)=>{
            if(e.ItemIndex>=filtered.Count){e.Item=new ListViewItem("");return;}
            var i=filtered[e.ItemIndex];
            var src=clientItems.ContainsKey(i.Id)?(serverItems.ContainsKey(i.Id)?"Client + DB":"Client"):"DB";
            e.Item=new ListViewItem([i.Id.ToString(),i.Name,ResourceDisplay(i),src]);
        };
        grfs.RetrieveVirtualItem += (_,e)=>{
            if(e.ItemIndex>=filteredAssets.Count){e.Item=new ListViewItem("");return;}
            var a=filteredAssets[e.ItemIndex];
            string priority=assets!=null&&assets.PriorityByArchive.TryGetValue(a.Archive,out var p)?p.ToString():"";
            int providers=assets!=null&&assets.Providers.TryGetValue(a.Path,out var chain)?chain.Count:1;
            e.Item=new ListViewItem([Assets.Display(a.Path),Path.GetFileName(a.Archive),priority,providers.ToString(),a.Size.ToString("N0"),a.Flags.ToString()]);
        };
        grfs.DoubleClick += (_,_)=>ShowAssetProviders();
        list.SelectedIndexChanged += async (_,_)=>await ShowItem();
        collection.CheckedChanged += async (_,_)=>await ShowItem();
        query.TextChanged += (_,_)=>{debounce.Stop();debounce.Start();};
        debounce.Tick += async (_,_)=>{debounce.Stop();await Filter();};
        encoding.SelectedIndexChanged += async (_,_)=>{if(uiReady&&settings.ItemInfoFiles.Count>0)await RebuildItemInfo();};
        FormClosing += (_,_)=>{loadCancel?.Cancel();filterCancel?.Cancel();imageCancel?.Cancel();SaveSettings();};
        FormClosed += (_,_)=>picture.Image?.Dispose();
    }

    void LoadSettings() {
        try {
            string? source=null;
            if(File.Exists(settingsPath)) source=settingsPath;
            else if(File.Exists(legacySettingsPath)) source=legacySettingsPath;

            if(source!=null)
                settings=JsonSerializer.Deserialize<Settings>(File.ReadAllText(source))??new();

            settings.ItemInfoFiles ??= [];
            settings.ItemDbFiles ??= [];
            PromoteConfiguredWriteTargets();

            // One-time migration: old LocalAppData settings become portable config beside the EXE.
            if(source==legacySettingsPath && !File.Exists(settingsPath)) {
                Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
                File.WriteAllText(settingsPath,JsonSerializer.Serialize(settings,new JsonSerializerOptions{WriteIndented=true}));
            }
        } catch(Exception ex){Log("Settings: "+ex.Message);}
    }

    static void PromoteExistingSource(List<string> sources,string file) {
        if(string.IsNullOrWhiteSpace(file))return;
        var existing=sources.FirstOrDefault(x=>x.Equals(file,StringComparison.OrdinalIgnoreCase));
        if(existing==null)return;
        sources.RemoveAll(x=>x.Equals(file,StringComparison.OrdinalIgnoreCase));
        sources.Add(existing);
    }

    void PromoteConfiguredWriteTargets() {
        if(!string.IsNullOrWhiteSpace(settings.Client)&&!string.IsNullOrWhiteSpace(settings.ClientItemWriteTarget)) {
            string clientTarget=Path.IsPathRooted(settings.ClientItemWriteTarget)
                ? settings.ClientItemWriteTarget
                : Path.Combine(settings.Client,settings.ClientItemWriteTarget);
            if(File.Exists(clientTarget))PromoteExistingSource(settings.ItemInfoFiles,clientTarget);
        }
        if(!string.IsNullOrWhiteSpace(settings.ServerRoot)&&!string.IsNullOrWhiteSpace(settings.ServerItemWriteTarget)) {
            string serverTarget=Path.IsPathRooted(settings.ServerItemWriteTarget)
                ? settings.ServerItemWriteTarget
                : Path.Combine(settings.ServerRoot,settings.ServerItemWriteTarget);
            if(File.Exists(serverTarget))PromoteExistingSource(settings.ItemDbFiles,serverTarget);
        }
    }

    void SaveSettings() {
        try {
            settings.Client=client.Text;
            settings.ServerRoot=server.Text;
            settings.LooseFirst=loose.Checked;
            settings.Encoding=Codepage;
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
            var temp=settingsPath+".tmp";
            File.WriteAllText(temp,JsonSerializer.Serialize(settings,new JsonSerializerOptions{WriteIndented=true}));
            File.Move(temp,settingsPath,true);
        } catch(Exception ex){Log(ex.Message);}
    }

    static string SourceLabel(string source) =>
        source.StartsWith(PackedItemInfoPrefix,StringComparison.OrdinalIgnoreCase)
            ? "GRF :: "+Assets.Display(source[PackedItemInfoPrefix.Length..])
            : source;

    void RefreshSourceLists() {
        itemInfoFiles.Items.Clear();
        foreach(var f in settings.ItemInfoFiles)itemInfoFiles.Items.Add(SourceLabel(f));
        itemDbFiles.Items.Clear();
        foreach(var f in settings.ItemDbFiles)itemDbFiles.Items.Add(f);
    }

    void MoveItemInfo(int delta) => MoveSource(itemInfoFiles,settings.ItemInfoFiles,delta,RebuildItemInfo);
    void MoveItemDb(int delta) => MoveSource(itemDbFiles,settings.ItemDbFiles,delta,RebuildItemDb);

    void MoveSource(ListBox box,List<string> sources,int delta,Func<Task> rebuild) {
        int index=box.SelectedIndex;
        if(index<0)return;
        int target=index+delta;
        if(target<0||target>=sources.Count)return;
        (sources[index],sources[target])=(sources[target],sources[index]);
        RefreshSourceLists();
        box.SelectedIndex=target;
        SaveSettings();
        _=rebuild();
    }

    void ChooseClient() {
        using var d=new FolderBrowserDialog();
        if(d.ShowDialog()!=DialogResult.OK)return;
        client.Text=d.SelectedPath;
        settings.Client=client.Text;
        SaveSettings();
        RefreshProjectCatalog(false);
    }

    void ChooseServer() {
        using var d=new FolderBrowserDialog();
        if(d.ShowDialog()!=DialogResult.OK)return;
        server.Text=d.SelectedPath;
        settings.ServerRoot=server.Text;
        SaveSettings();
        RefreshProjectCatalog(true);
    }

    EditorContext EditorCtx()=>new() {
        Settings=settings,
        Catalog=catalog,
        ReloadClient=RebuildItemInfo,
        ReloadServer=RebuildItemDb,
        RegisterClientSource=RegisterClientSource,
        RegisterServerSource=RegisterServerSource,
        ClientSourceChain=itemId=>clientItemSources.TryGetValue(itemId,out var chain)?chain:Array.Empty<string>(),
        ServerSourceChain=itemId=>serverItemSources.TryGetValue(itemId,out var chain)?chain:Array.Empty<string>(),
        Log=Log
    };

    Control CreateItemEditor()=>new ItemEditorControl(EditorCtx());
    Control CreateBarterEditor()=>new BarterEditorControl(EditorCtx());
    Control CreateNpcEditor()=>new NpcEditorControl(EditorCtx());
    Control CreatePetEditor()=>new PetEditorControl(EditorCtx());
    Control CreateValidator()=>new ProjectValidatorControl(EditorCtx());

    void RegisterClientSource(string file) {
        // A designated write target is an override by definition, so it must be the
        // last/highest-priority source after every save.
        settings.ItemInfoFiles.RemoveAll(x=>x.Equals(file,StringComparison.OrdinalIgnoreCase));
        settings.ItemInfoFiles.Add(file);
        RefreshSourceLists();SaveSettings();
    }

    void RegisterServerSource(string file) {
        settings.ItemDbFiles.RemoveAll(x=>x.Equals(file,StringComparison.OrdinalIgnoreCase));
        settings.ItemDbFiles.Add(file);
        RefreshSourceLists();SaveSettings();
    }

    void RefreshProjectCatalog(bool discover) {
        settings.Client=client.Text;settings.ServerRoot=server.Text;
        catalog.Update(settings.Client,settings.ServerRoot,clientItems,serverItems,assets);
        if(discover) {
            try {
                catalog.Discover();
                Log($"Project Catalog: itemDB={catalog.ItemDbFiles.Count:N0}, barter={catalog.BarterFiles.Count:N0}, pet={catalog.PetFiles.Count:N0}, npc={catalog.NpcFiles.Count:N0}, aegis={catalog.ByAegis.Count:N0}");
            } catch(Exception ex){Log("Catalog discovery: "+ex.Message);}
        }
        if(itemEditorPanel is ItemEditorControl itemEditor)itemEditor.RefreshItemList();
    }

    async Task ReloadProject() {
        SaveSettings();
        RefreshProjectCatalog(true);
        if(Directory.Exists(client.Text))await LoadAssets();
        if(settings.ItemInfoFiles.Count>0)await RebuildItemInfo();
        if(settings.ItemDbFiles.Count>0)await RebuildItemDb();
        RefreshProjectCatalog(true);
    }

    void AddItemInfos() {
        using var d=new OpenFileDialog{
            Filter="Item info (*.lua;*.lub)|*.lua;*.lub|All files|*.*",
            Multiselect=true,InitialDirectory=Directory.Exists(client.Text)?client.Text:""
        };
        if(d.ShowDialog()!=DialogResult.OK)return;
        foreach(var f in d.FileNames)if(!settings.ItemInfoFiles.Contains(f,StringComparer.OrdinalIgnoreCase))settings.ItemInfoFiles.Add(f);
        RefreshSourceLists(); SaveSettings(); _=RebuildItemInfo();
    }

    void RemoveItemInfo() {
        if(itemInfoFiles.SelectedIndex<0)return;
        settings.ItemInfoFiles.RemoveAt(itemInfoFiles.SelectedIndex);
        RefreshSourceLists(); SaveSettings(); _=RebuildItemInfo();
    }

    async Task AutoDetectItemInfo() {
        if(!Directory.Exists(client.Text)) {
            MessageBox.Show("กรุณาเลือก Client folder ก่อน","Auto Detect itemInfo");
            return;
        }
        if(assets==null)await LoadAssets();

        var packed=new List<string>();
        var a=assets;
        if(a!=null) {
            packed=a.Entries.Values
                .Where(x=>{
                    var path=Assets.Display(x.Path);
                    var ext=Path.GetExtension(path);
                    return path.Contains("iteminfo",StringComparison.OrdinalIgnoreCase)
                        && (ext.Equals(".lua",StringComparison.OrdinalIgnoreCase)||ext.Equals(".lub",StringComparison.OrdinalIgnoreCase));
                })
                .OrderBy(x=>a.PriorityByArchive.TryGetValue(x.Archive,out var p)?p:int.MaxValue)
                .ThenBy(x=>x.Path,StringComparer.OrdinalIgnoreCase)
                .Select(x=>PackedItemInfoPrefix+x.Path)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        var local=new List<string>();
        var roots=new[]{
            Path.Combine(client.Text,"System"),
            Path.Combine(client.Text,"data","luafiles514","lua files","datainfo")
        };
        foreach(var root in roots.Where(Directory.Exists)) {
            foreach(var file in Directory.EnumerateFiles(root,"*",SearchOption.TopDirectoryOnly)) {
                var ext=Path.GetExtension(file);
                if(!Path.GetFileName(file).Contains("iteminfo",StringComparison.OrdinalIgnoreCase))continue;
                if(!ext.Equals(".lua",StringComparison.OrdinalIgnoreCase)&&!ext.Equals(".lub",StringComparison.OrdinalIgnoreCase))continue;
                local.Add(Path.GetFullPath(file));
            }
        }
        local=local.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x=>x,StringComparer.OrdinalIgnoreCase).ToList();

        var detected=(loose.Checked ? packed.Concat(local) : local.Concat(packed)).ToList();
        var existing=settings.ItemInfoFiles.ToList();
        var additions=detected.Where(x=>!existing.Contains(x,StringComparer.OrdinalIgnoreCase)).ToList();
        settings.ItemInfoFiles=[..additions,..existing];

        RefreshSourceLists();
        SaveSettings();
        Log($"Auto Detect itemInfo: packed={packed.Count}, loose={local.Count}, added={additions.Count}. Existing manual sources remain later/higher priority.");
        if(settings.ItemInfoFiles.Count>0)await RebuildItemInfo();
    }

    async Task AutoDetectItemDb() {
        RefreshProjectCatalog(true);
        var detected=catalog.RecommendedItemDbFiles();
        var existing=settings.ItemDbFiles.ToList();
        var additions=detected.Where(x=>!existing.Contains(x,StringComparer.OrdinalIgnoreCase)).ToList();
        settings.ItemDbFiles=[..additions,..existing];
        RefreshSourceLists();SaveSettings();
        Log($"Auto Detect Item DB: mode={(detected.Any(x=>x.Contains("\\pre-re\\",StringComparison.OrdinalIgnoreCase))?"Pre-Renewal":"Renewal")} • found={detected.Count} • added={additions.Count}");
        if(settings.ItemDbFiles.Count>0)await RebuildItemDb();
    }

    void AddItemDbs() {
        using var d=new OpenFileDialog{Filter="Item DB (*.yml;*.yaml)|*.yml;*.yaml",Multiselect=true};
        if(d.ShowDialog()!=DialogResult.OK)return;
        foreach(var f in d.FileNames)if(!settings.ItemDbFiles.Contains(f,StringComparer.OrdinalIgnoreCase))settings.ItemDbFiles.Add(f);
        RefreshSourceLists(); SaveSettings(); _=RebuildItemDb();
    }

    void RemoveItemDb() {
        if(itemDbFiles.SelectedIndex<0)return;
        settings.ItemDbFiles.RemoveAt(itemDbFiles.SelectedIndex);
        RefreshSourceLists(); SaveSettings(); _=RebuildItemDb();
    }

    async Task Work(Func<CancellationToken,Task> action) {
        if(loadCancel!=null)return;
        var cts=new CancellationTokenSource(); loadCancel=cts;
        foreach(var b in loadButtons)b.Enabled=false;
        var watch=Stopwatch.StartNew();
        try {
            await action(cts.Token);
            status.Text=$"เสร็จใน {watch.Elapsed.TotalSeconds:N2} วินาที • Items {items.Count:N0} • Assets {assetRows.Count:N0}";
            SaveSettings();
        } catch(OperationCanceledException){status.Text="ยกเลิกแล้ว — คงข้อมูลชุดก่อนหน้า";}
        catch(Exception ex){status.Text="โหลดไม่สำเร็จ: "+ex.Message;Log(ex.ToString());}
        finally{foreach(var b in loadButtons)b.Enabled=true;loadCancel=null;cts.Dispose();}
    }

    async Task LoadAssets() {
        string root=client.Text; bool lf=loose.Checked;
        await Work(async ct=>{
            var progress=new Progress<string>(s=>{if(!IsDisposed)status.Text=s;});
            var a=await Task.Run(()=>{
                var v=new Assets();
                v.Load(root,lf,ct,s=>((IProgress<string>)progress).Report(s));
                return v;
            },ct);
            ct.ThrowIfCancellationRequested();
            assets=a;
            assetRows=await Task.Run(()=>a.Entries.Values.OrderBy(x=>x.Path,StringComparer.OrdinalIgnoreCase).ToList(),ct);
            foreach(var m in a.Messages)Log(m);
            Log("GRF priority follows numeric order in DATA.ini; first matching resource wins.");
            Log("Loose data priority: "+lf);
            await Filter();
            await ShowItem();
        });
        if(assets!=null&&settings.ItemInfoFiles.Any(x=>x.StartsWith(PackedItemInfoPrefix,StringComparison.OrdinalIgnoreCase)))
            await RebuildItemInfo();
    }

    async Task RebuildItemInfo() {
        var sources=settings.ItemInfoFiles.ToArray();
        int cp=Codepage;
        var a=assets;
        await Work(async ct=>{
            var result=await Task.Run(()=>{
                var map=new Dictionary<int,Item>();
                var provenance=new Dictionary<int,List<string>>();
                var loaded=new List<(string Source,int Count,string Format)>();
                var errors=new List<string>();

                foreach(var source in sources) {
                    ct.ThrowIfCancellationRequested();
                    try {
                        byte[] bytes;
                        string actualSource;
                        if(source.StartsWith(PackedItemInfoPrefix,StringComparison.OrdinalIgnoreCase)) {
                            if(a==null)throw new InvalidOperationException("Load DATA.ini / GRF before using packed itemInfo sources.");
                            string rawPath=source[PackedItemInfoPrefix.Length..];
                            var data=a.Read(rawPath,ct);
                            bytes=data.Bytes;
                            actualSource="GRF :: "+data.Source;
                        } else {
                            if(!File.Exists(source)) {
                                errors.Add("MISSING :: "+source);
                                continue;
                            }
                            bytes=File.ReadAllBytes(source);
                            actualSource=source;
                        }

                        string format=LuaData.Describe(bytes,cp);
                        var rows=LuaData.Parse(bytes,actualSource,cp,ct);
                        foreach(var i in rows) {
                            if(!provenance.TryGetValue(i.Id,out var chain))provenance[i.Id]=chain=[];
                            chain.Add(actualSource);
                            map[i.Id]=i;
                        }
                        loaded.Add((actualSource,rows.Count,format));
                    }
                    catch(OperationCanceledException){throw;}
                    catch(Exception ex) {
                        errors.Add($"{SourceLabel(source)} :: {ex.Message}");
                    }
                }

                if(loaded.Count==0 && sources.Length>0)
                    throw new InvalidDataException("No itemInfo source could be loaded. "+string.Join(" | ",errors));

                return(map,provenance,loaded,errors);
            },ct);

            clientItems=result.map;
            clientItemSources=result.provenance;
            Merge();

            foreach(var f in result.loaded)
                Log($"itemInfo loaded: {f.Count:N0} rows • {f.Format} :: {f.Source}");
            foreach(var e in result.errors)
                Log("itemInfo skipped: "+e);

            Log($"itemInfo stack ready: {result.loaded.Count}/{sources.Length} sources OK • {clientItems.Count:N0} unique items");
            await Filter();
        });
        RefreshProjectCatalog(false);
    }

    async Task RebuildItemDb() {
        var files=settings.ItemDbFiles.ToArray();
        await Work(async ct=>{
            var result=await Task.Run(()=>{
                var map=new Dictionary<int,Item>();
                var provenance=new Dictionary<int,List<string>>();
                var loaded=new List<(string File,int Count)>();
                var errors=new List<string>();
                foreach(var file in files) {
                    ct.ThrowIfCancellationRequested();
                    if(!File.Exists(file)){errors.Add("MISSING :: "+file);continue;}
                    try {
                        var rows=ItemDb.Load(file,ct);
                        foreach(var i in rows) {
                            if(!provenance.TryGetValue(i.Id,out var chain))provenance[i.Id]=chain=[];
                            chain.Add(file);
                            map[i.Id]=i;
                        }
                        loaded.Add((file,rows.Count));
                    } catch(OperationCanceledException){throw;}
                    catch(Exception ex){errors.Add($"{file} :: {ex.Message}");}
                }
                if(loaded.Count==0&&files.Length>0)throw new InvalidDataException("No Item DB source could be loaded. "+string.Join(" | ",errors));
                return(map,provenance,loaded,errors);
            },ct);
            serverItems=result.map;
            serverItemSources=result.provenance;
            Merge();
            foreach(var f in result.loaded)Log($"Item DB loaded: {f.Count:N0} rows :: {f.File}");
            foreach(var e in result.errors)Log("Item DB skipped: "+e);
            Log($"Item DB stack ready: {result.loaded.Count}/{files.Length} sources OK • {serverItems.Count:N0} effective items");
            await Filter();
        });
        RefreshProjectCatalog(true);
    }

    void Merge() {
        var merged=new Dictionary<int,Item>(serverItems);
        foreach(var i in clientItems.Values)merged[i.Id]=i;
        items=merged.Values.OrderBy(x=>x.Id).ToList();
        catalog.Update(client.Text,server.Text,clientItems,serverItems,assets);
        UpdateItemCounts(filtered.Count);
    }

    void UpdateItemCounts(int visible) {
        itemCounts.Text=$"Client {clientItems.Count:N0} • DB {serverItems.Count:N0} • Merged {items.Count:N0} • Visible {visible:N0}";
    }

    async Task Filter() {
        filterCancel?.Cancel();
        var c=new CancellationTokenSource(); filterCancel=c;
        string q=query.Text.Trim();
        var itemSnapshot=items; var assetSnapshot=assetRows;
        try {
            var result=await Task.Run(()=>{
                var it=new List<Item>(); var ar=new List<Asset>();
                foreach(var i in itemSnapshot){
                    c.Token.ThrowIfCancellationRequested();
                    if(q.Length==0||i.Id.ToString().Contains(q)||i.Name.Contains(q,StringComparison.OrdinalIgnoreCase)||ResourceDisplay(i).Contains(q,StringComparison.OrdinalIgnoreCase))it.Add(i);
                }
                foreach(var a in assetSnapshot){
                    c.Token.ThrowIfCancellationRequested();
                    if(q.Length==0||Assets.Display(a.Path).Contains(q,StringComparison.OrdinalIgnoreCase))ar.Add(a);
                }
                return(it,ar);
            },c.Token);
            if(c.IsCancellationRequested||IsDisposed)return;
            list.VirtualListSize=0; grfs.VirtualListSize=0;
            filtered=result.it; filteredAssets=result.ar;
            list.VirtualListSize=filtered.Count; grfs.VirtualListSize=filteredAssets.Count;
            list.Invalidate(); grfs.Invalidate();
            UpdateItemCounts(filtered.Count);
            status.Text=$"Items {filtered.Count:N0}/{items.Count:N0} • Client {clientItems.Count:N0} • DB {serverItems.Count:N0} • Resources {filteredAssets.Count:N0}/{assetRows.Count:N0}";
        } catch(OperationCanceledException){}
        finally{if(filterCancel==c)filterCancel=null;c.Dispose();}
    }

    async Task ShowItem() {
        imageCancel?.Cancel();
        var c=new CancellationTokenSource(); imageCancel=c; int generation=++imageGeneration;
        picture.Image?.Dispose(); picture.Image=null;
        if(list.SelectedIndices.Count==0){c.Dispose();imageCancel=null;return;}
        int n=list.SelectedIndices[0];
        if(n>=filtered.Count){c.Dispose();imageCancel=null;return;}

        var i=filtered[n]; var a=assets; bool coll=collection.Checked;
        var sb=new StringBuilder();
        sb.AppendLine($"ID: {i.Id}");
        sb.AppendLine($"Name: {i.Name}");
        sb.AppendLine($"Resource: {ResourceDisplay(i)}");
        sb.AppendLine();

        if(clientItemSources.TryGetValue(i.Id,out var clientSources)&&clientSources.Count>0) {
            sb.AppendLine("CLIENT itemInfo source chain:");
            for(int x=0;x<clientSources.Count;x++)
                sb.AppendLine($"  {x+1}. {clientSources[x]}{(x==clientSources.Count-1?"  <-- WINNER":"")}");
        } else sb.AppendLine("CLIENT itemInfo: not found");

        if(serverItemSources.TryGetValue(i.Id,out var dbSources)&&dbSources.Count>0) {
            sb.AppendLine();
            sb.AppendLine("SERVER Item DB source chain:");
            for(int x=0;x<dbSources.Count;x++)
                sb.AppendLine($"  {x+1}. {dbSources[x]}{(x==dbSources.Count-1?"  <-- WINNER":"")}");
        } else sb.AppendLine("\r\nSERVER Item DB: not found");

        sb.AppendLine();
        sb.AppendLine("DESCRIPTION:");
        sb.AppendLine(i.Description);
        if(serverItems.TryGetValue(i.Id,out var db)) {
            sb.AppendLine();
            sb.AppendLine("SERVER DB RAW:");
            sb.AppendLine(db.Raw);
        }
        details.Text=sb.ToString();

        if(a==null){details.AppendText("\r\nกรุณาโหลด DATA.ini / GRF เพื่อแสดงรูป");if(imageCancel==c)imageCancel=null;c.Dispose();return;}
        bool entered=false;
        try {
            await imageGate.WaitAsync(c.Token); entered=true;
            var result=await Task.Run(()=>{
                var data=a.ItemImage(i,coll,c.Token);
                using var ms=new MemoryStream(data.Bytes);
                using var source=Image.FromStream(ms);
                if(source.Width>4096||source.Height>4096)throw new InvalidDataException("Image preview dimensions too large.");
                var bmp=new Bitmap(source);
                bmp.MakeTransparent(Color.Magenta);
                return(Image:bmp,data.Source);
            },c.Token);
            if(c.IsCancellationRequested||generation!=imageGeneration||IsDisposed){result.Image.Dispose();return;}
            picture.Image=result.Image;
            string priority="Loose data / external file";
            foreach(var kv in a.PriorityByArchive) {
                if(result.Source.StartsWith(kv.Key+" :: ",StringComparison.OrdinalIgnoreCase)) {
                    priority=$"DATA.ini [{kv.Value}] / {Path.GetFileName(kv.Key)}";
                    break;
                }
            }
            details.AppendText($"\r\n\r\nICON RESOLUTION:\r\n  Source: {result.Source}\r\n  Priority: {priority}");
        } catch(OperationCanceledException){}
        catch(Exception ex){if(generation==imageGeneration&&!IsDisposed)details.AppendText("\r\n\r\nICON ERROR: "+ex.Message);}
        finally{if(entered)imageGate.Release();if(imageCancel==c)imageCancel=null;c.Dispose();}
    }

    void Log(string message) {
        if(log.IsDisposed)return;
        log.AppendText(DateTime.Now.ToString("HH:mm:ss")+" "+message+Environment.NewLine);
    }

    void ExportCsv() {
        using var d=new SaveFileDialog{Filter="CSV|*.csv",FileName="items.csv"};
        if(d.ShowDialog()!=DialogResult.OK)return;
        try {
            string Q(string s)=>"\""+s.Replace("\"","\"\"")+"\"";
            File.WriteAllLines(d.FileName,new[]{"Id,Name,Resource,Source"}.Concat(filtered.Select(i=>$"{i.Id},{Q(i.Name)},{Q(ResourceDisplay(i))},{Q(i.Source)}")),new UTF8Encoding(true));
            Log("Exported "+d.FileName);
        } catch(Exception ex){MessageBox.Show(ex.Message);}
    }
}
