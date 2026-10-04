using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;

namespace MavenROStudio;

public sealed class EditorContext {
    public required Settings Settings { get; init; }
    public required ProjectCatalog Catalog { get; init; }
    public required Func<Task> ReloadClient { get; init; }
    public required Func<Task> ReloadServer { get; init; }
    public required Action<string> RegisterClientSource { get; init; }
    public required Action<string> RegisterServerSource { get; init; }
    public required Func<int,IReadOnlyList<string>> ClientSourceChain { get; init; }
    public required Func<int,IReadOnlyList<string>> ServerSourceChain { get; init; }
    public required Action<string> Log { get; init; }
}

public sealed class ItemEditorControl : UserControl {
    readonly EditorContext ctx;
    readonly TextBox id=new(){Width=110};
    readonly Label state=new(){AutoSize=true,Padding=new Padding(10,8,0,0)};
    readonly TextBox cName=new(){Dock=DockStyle.Top};
    readonly TextBox cResource=new(){Dock=DockStyle.Top};
    readonly TextBox cDesc=new(){Dock=DockStyle.Fill,Multiline=true,ScrollBars=ScrollBars.Both,WordWrap=true};
    readonly RichTextBox cDescPreview=new(){Dock=DockStyle.Fill,ReadOnly=true,BorderStyle=BorderStyle.None,ScrollBars=RichTextBoxScrollBars.Vertical,WordWrap=true};
    readonly ComboBox clientColorPreset=new(){Width=175,DropDownStyle=ComboBoxStyle.DropDownList};
    readonly TextBox clientColorHex=new(){Width=74,Text="000000",CharacterCasing=CharacterCasing.Upper,MaxLength=7};
    readonly Panel clientColorSwatch=new(){Width=28,Height=24,BackColor=Color.Black,BorderStyle=BorderStyle.FixedSingle};
    readonly Label clientColorHelp=new(){AutoSize=true,MaximumSize=new Size(420,0),Text="เลือกสี → ครอบข้อความที่เลือก หรือแทรกโค้ดสีที่ cursor"};
    readonly TextBox cSlot=new(){Width=55};
    readonly TextBox cClass=new(){Width=65};
    readonly CheckBox cCostume=new(){Text="costume",AutoSize=true};
    readonly TextBox sName=new(){Dock=DockStyle.Top};
    readonly TextBox sAegis=new(){Dock=DockStyle.Top};
    readonly TextBox sType=new(){Width=90};
    readonly TextBox sWeight=new(){Width=70};
    readonly TextBox sSlots=new(){Width=55};
    readonly TextBox sView=new(){Width=65};
    readonly TextBox sRaw=new(){Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Both,Font=new Font("Consolas",9)};
    readonly RichTextBox sScript=new(){Dock=DockStyle.Fill,AcceptsTab=true,Font=new Font("Consolas",10),WordWrap=false};
    readonly RichTextBox sEquipScript=new(){Dock=DockStyle.Fill,AcceptsTab=true,Font=new Font("Consolas",10),WordWrap=false};
    readonly RichTextBox sUnEquipScript=new(){Dock=DockStyle.Fill,AcceptsTab=true,Font=new Font("Consolas",10),WordWrap=false};
    readonly TextBox bonusSearch=new(){Dock=DockStyle.Top,PlaceholderText="ค้นหา bonus เช่น bAllStats / Drop / Race"};
    readonly ComboBox bonusTarget=new(){Dock=DockStyle.Top,DropDownStyle=ComboBoxStyle.DropDownList};
    readonly ListView bonusList=new(){Dock=DockStyle.Fill,View=View.Details,FullRowSelect=true,GridLines=true,HideSelection=false,ShowItemToolTips=true};
    readonly Label bonusHelp=new(){Dock=DockStyle.Bottom,Height=96,AutoEllipsis=true,Padding=new Padding(6)};
    List<ServerScriptSnippet> bonusSnippets=[],visibleBonusSnippets=[];
    string bonusCatalogRoot="";

    readonly TextBox itemSearch=new(){Dock=DockStyle.Top,PlaceholderText="ค้นหา ID / AegisName / Name / Resource"};
    readonly ListView itemList=new(){Dock=DockStyle.Fill,View=View.Details,VirtualMode=true,FullRowSelect=true,HideSelection=false,GridLines=true};
    readonly PictureBox itemIcon=new(){Size=new Size(112,112),SizeMode=PictureBoxSizeMode.Zoom,BackColor=Color.FromArgb(38,38,44),Margin=new Padding(10)};
    readonly Label itemIconInfo=new(){AutoSize=true,MaximumSize=new Size(300,0)};
    List<int> allItemIds=[],visibleItemIds=[];
    CancellationTokenSource? iconCancel;

    public bool EnhancedLayoutReady=>itemList.Columns.Count>=5&&itemIcon.Width>=96;
    public int ListedItemCount=>visibleItemIds.Count;
    public bool SaveConfirmationRequired=>true;
    public bool DescriptionPreviewReady=>cDescPreview!=null;
    public bool ClientColorHelperReady=>clientColorPreset.Items.Count==RagnarokColorHelper.BasicColors.Count;
    public int ScriptHelperCount=>bonusSnippets.Count;

    public ItemEditorControl(EditorContext context) {
        ctx=context;Dock=DockStyle.Fill;

        var top=new FlowLayoutPanel{Dock=DockStyle.Top,Height=44,WrapContents=false};
        top.Controls.Add(new Label{Text="Item ID",AutoSize=true,Padding=new Padding(0,8,0,0)});top.Controls.Add(id);
        Button(top,"Load",LoadItem);Button(top,"Client → DB",ClientToDb);Button(top,"DB → Client",DbToClient);
        Button(top,"Save Client",async()=>await SaveClient(true));
        Button(top,"Save Server",async()=>await SaveServer(true));
        Button(top,"Save Both",SaveBoth);
        top.Controls.Add(state);

        itemList.Columns.Add("ID",72);
        itemList.Columns.Add("AegisName",145);
        itemList.Columns.Add("Name",190);
        itemList.Columns.Add("Client Name",190);
        itemList.Columns.Add("Status",95);
        itemList.RetrieveVirtualItem+=ItemListRetrieve;
        itemList.SelectedIndexChanged+=(_,_)=>LoadSelectedFromList();
        itemSearch.TextChanged+=(_,_)=>FilterItemList();
        cDesc.TextChanged+=(_,_)=>RagnarokDescriptionPreview.Render(cDescPreview,cDesc.Text);

        foreach(var color in RagnarokColorHelper.BasicColors)clientColorPreset.Items.Add(color);
        clientColorPreset.SelectedIndex=0;
        clientColorPreset.SelectedIndexChanged+=(_,_)=>SelectPresetColor();
        clientColorHex.TextChanged+=(_,_)=>UpdateColorSwatch();

        bonusTarget.Items.AddRange(["Script","EquipScript","UnEquipScript"]);
        bonusTarget.SelectedIndex=0;
        bonusList.Columns.Add("Bonus",145);
        bonusList.Columns.Add("Command",72);
        bonusList.Columns.Add("Syntax",360);
        bonusList.SelectedIndexChanged+=(_,_)=>ShowBonusHelp();
        bonusList.DoubleClick+=(_,_)=>InsertSelectedBonus();
        bonusSearch.TextChanged+=(_,_)=>FilterBonusList();
        LoadBonusCatalog();

        var left=new Panel{Dock=DockStyle.Fill,Padding=new Padding(6)};
        var listTitle=new Label{Text="ALL ITEMS",Dock=DockStyle.Top,Height=30,Font=new Font(Font,FontStyle.Bold),TextAlign=ContentAlignment.MiddleLeft};
        left.Controls.Add(itemList);
        left.Controls.Add(itemSearch);
        left.Controls.Add(listTitle);

        var editorArea=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=2,ColumnCount=1};
        editorArea.RowStyles.Add(new RowStyle(SizeType.Absolute,142));
        editorArea.RowStyles.Add(new RowStyle(SizeType.Percent,100));

        var preview=new GroupBox{Text="Item Preview",Dock=DockStyle.Fill,Padding=new Padding(8)};
        var previewLayout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1};
        previewLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,135));
        previewLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        var iconHost=new Panel{Dock=DockStyle.Fill};
        itemIcon.Location=new Point(8,8);iconHost.Controls.Add(itemIcon);
        var previewInfoHost=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new Padding(6)};
        previewInfoHost.Controls.Add(itemIconInfo);
        previewLayout.Controls.Add(iconHost,0,0);previewLayout.Controls.Add(previewInfoHost,1,0);
        preview.Controls.Add(previewLayout);
        editorArea.Controls.Add(preview,0,0);

        var editSplit=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2};
        editSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
        editSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
        editSplit.Controls.Add(ClientPanel(),0,0);
        editSplit.Controls.Add(ServerPanel(),1,0);
        editorArea.Controls.Add(editSplit,0,1);

        var main=new SplitContainer{Dock=DockStyle.Fill,SplitterDistance=390};
        main.Panel1.Controls.Add(left);
        main.Panel2.Controls.Add(editorArea);

        Controls.Add(main);Controls.Add(top);
        RefreshItemList();
    }

    Panel ClientPanel() {
        var p=new Panel{Dock=DockStyle.Fill,Padding=new Padding(8)};

        var descTabs=new TabControl{Dock=DockStyle.Fill};
        var rawPage=new TabPage("Description Raw");
        var previewPage=new TabPage("Game Preview");

        var colorTools=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=82,WrapContents=true,AutoScroll=true,Padding=new Padding(4),BackColor=Color.FromArgb(245,245,248)};
        colorTools.Controls.Add(new Label{Text="Color",AutoSize=true,Padding=new Padding(0,7,0,0)});
        colorTools.Controls.Add(clientColorPreset);
        colorTools.Controls.Add(clientColorSwatch);
        colorTools.Controls.Add(new Label{Text="HEX",AutoSize=true,Padding=new Padding(6,7,0,0)});
        colorTools.Controls.Add(clientColorHex);
        var applyColor=new Button{Text="Apply to Selection",AutoSize=true,Height=28};
        applyColor.Click+=(_,_)=>ApplyClientColorToSelection();
        colorTools.Controls.Add(applyColor);
        var insertColor=new Button{Text="Insert Code",AutoSize=true,Height=28};
        insertColor.Click+=(_,_)=>InsertClientColorCode();
        colorTools.Controls.Add(insertColor);
        var resetColor=new Button{Text="Reset ^000000",AutoSize=true,Height=28};
        resetColor.Click+=(_,_)=>InsertClientColorReset();
        colorTools.Controls.Add(resetColor);
        var removeColors=new Button{Text="Remove Colors",AutoSize=true,Height=28};
        removeColors.Click+=(_,_)=>StripClientColorCodes();
        colorTools.Controls.Add(removeColors);
        foreach(var quick in RagnarokColorHelper.BasicColors.Where(x=>new[]{"FF0000","00AA00","0000CC","0033CC","FFFF00","FF9900","AA00FF","808080"}.Contains(x.Hex))){
            int rgb=Convert.ToInt32(quick.Hex,16);
            var qb=new Button{
                Text=" ",Width=28,Height=28,
                BackColor=Color.FromArgb((rgb>>16)&255,(rgb>>8)&255,rgb&255),
                FlatStyle=FlatStyle.Flat,
                Tag=quick
            };
            qb.FlatAppearance.BorderColor=Color.DimGray;
            qb.Click+=(_,_)=>{
                if(qb.Tag is not RagnarokColorChoice c)return;
                clientColorHex.Text=c.Hex;
                int idx=RagnarokColorHelper.BasicColors.ToList().FindIndex(x=>x.Hex==c.Hex);
                if(idx>=0)clientColorPreset.SelectedIndex=idx;
                clientColorHelp.Text=$"{c.Name} • {c.Help} • Code: ^{c.Hex}";
            };
            colorTools.Controls.Add(qb);
        }
        colorTools.Controls.Add(clientColorHelp);
        rawPage.Controls.Add(cDesc);
        rawPage.Controls.Add(colorTools);
        colorTools.BringToFront();

        var previewFrame=new Panel{Dock=DockStyle.Fill,Padding=new Padding(10),BackColor=Color.FromArgb(226,223,210),Tag="theme:preserve"};
        cDescPreview.BackColor=Color.FromArgb(250,248,239);
        cDescPreview.ForeColor=Color.FromArgb(32,32,32);
        cDescPreview.Tag="theme:preserve";
        previewFrame.Controls.Add(cDescPreview);
        previewPage.Controls.Add(previewFrame);
        descTabs.TabPages.Add(rawPage);
        descTabs.TabPages.Add(previewPage);
        p.Controls.Add(descTabs);
        p.Controls.Add(Labeled("Description • ^RRGGBB colors are rendered in Game Preview",44));
        var meta=new FlowLayoutPanel{Dock=DockStyle.Top,Height=36,WrapContents=false};
        meta.Controls.Add(new Label{Text="slotCount",AutoSize=true,Padding=new Padding(0,7,0,0)});meta.Controls.Add(cSlot);
        meta.Controls.Add(new Label{Text="ClassNum",AutoSize=true,Padding=new Padding(8,7,0,0)});meta.Controls.Add(cClass);meta.Controls.Add(cCostume);
        p.Controls.Add(meta);
        p.Controls.Add(cResource);p.Controls.Add(Labeled("Resource",28));
        p.Controls.Add(cName);p.Controls.Add(Labeled("Display Name",28));
        p.Controls.Add(new Label{Text="CLIENT itemInfo (.lua/.lub)",Dock=DockStyle.Top,Height=34,Font=new Font(Font,FontStyle.Bold)});
        return p;
    }
    Panel ServerPanel() {
        var p=new Panel{Dock=DockStyle.Fill,Padding=new Padding(8)};

        var scriptSplit=new SplitContainer{Dock=DockStyle.Fill,SplitterDistance=520};
        var scriptTabs=new TabControl{Dock=DockStyle.Fill};
        var scriptPage=new TabPage("Script");
        var equipPage=new TabPage("EquipScript");
        var unequipPage=new TabPage("UnEquipScript");
        var rawPage=new TabPage("Raw Source");
        scriptPage.Controls.Add(sScript);
        equipPage.Controls.Add(sEquipScript);
        unequipPage.Controls.Add(sUnEquipScript);
        rawPage.Controls.Add(sRaw);
        scriptTabs.TabPages.Add(scriptPage);
        scriptTabs.TabPages.Add(equipPage);
        scriptTabs.TabPages.Add(unequipPage);
        scriptTabs.TabPages.Add(rawPage);
        scriptSplit.Panel1.Controls.Add(scriptTabs);

        var helper=new Panel{Dock=DockStyle.Fill,Padding=new Padding(6)};
        var helperTitle=new Label{Text="SERVER SCRIPT HELPER • double-click = insert",Dock=DockStyle.Top,Height=28,Font=new Font(Font,FontStyle.Bold)};
        helper.Controls.Add(bonusList);
        helper.Controls.Add(bonusHelp);
        helper.Controls.Add(bonusTarget);
        helper.Controls.Add(bonusSearch);
        helper.Controls.Add(helperTitle);
        scriptSplit.Panel2.Controls.Add(helper);
        p.Controls.Add(scriptSplit);
        p.Controls.Add(Labeled("Script / EquipScript / UnEquipScript",36));

        var meta=new FlowLayoutPanel{Dock=DockStyle.Top,Height=36,WrapContents=false};
        meta.Controls.Add(new Label{Text="Type",AutoSize=true,Padding=new Padding(0,7,0,0)});meta.Controls.Add(sType);
        meta.Controls.Add(new Label{Text="Weight",AutoSize=true,Padding=new Padding(8,7,0,0)});meta.Controls.Add(sWeight);
        meta.Controls.Add(new Label{Text="Slots",AutoSize=true,Padding=new Padding(8,7,0,0)});meta.Controls.Add(sSlots);
        meta.Controls.Add(new Label{Text="View",AutoSize=true,Padding=new Padding(8,7,0,0)});meta.Controls.Add(sView);
        p.Controls.Add(meta);
        p.Controls.Add(sAegis);p.Controls.Add(Labeled("AegisName / Resource",28));
        p.Controls.Add(sName);p.Controls.Add(Labeled("Name",28));
        p.Controls.Add(new Label{Text="SERVER item_db.yml",Dock=DockStyle.Top,Height=34,Font=new Font(Font,FontStyle.Bold)});
        return p;
    }
    static Label Labeled(string text,int h)=>new(){Text=text,Dock=DockStyle.Top,Height=h,TextAlign=ContentAlignment.BottomLeft};
    static void Button(FlowLayoutPanel p,string text,Action action){var b=new Button{Text=text,AutoSize=true,Height=32};b.Click+=(_,_)=>action();p.Controls.Add(b);}
    static void Button(FlowLayoutPanel p,string text,Func<Task> action){var b=new Button{Text=text,AutoSize=true,Height=32};b.Click+=async(_,_)=>await action();p.Controls.Add(b);}

    void SelectPresetColor(){
        if(clientColorPreset.SelectedItem is not RagnarokColorChoice color)return;
        clientColorHex.Text=color.Hex;
        clientColorHelp.Text=$"{color.Name} • {color.Help} • Code: ^{color.Hex}";
    }

    void UpdateColorSwatch(){
        try{
            string hex=RagnarokColorHelper.NormalizeHex(clientColorHex.Text);
            int rgb=Convert.ToInt32(hex,16);
            clientColorSwatch.BackColor=Color.FromArgb((rgb>>16)&255,(rgb>>8)&255,rgb&255);
            clientColorHex.BackColor=SystemColors.Window;
        }catch{
            clientColorHex.BackColor=Color.MistyRose;
        }
    }

    string CurrentColorHex(){
        try{return RagnarokColorHelper.NormalizeHex(clientColorHex.Text);}
        catch(Exception ex){MessageBox.Show(ex.Message,"Client Color Helper");return "";}
    }

    void ApplyClientColorToSelection(){
        string hex=CurrentColorHex();if(hex.Length==0)return;
        int start=cDesc.SelectionStart;
        string selected=cDesc.SelectedText;
        if(selected.Length==0){
            cDesc.SelectedText=RagnarokColorHelper.Code(hex);
            cDesc.SelectionStart=start+7;
            cDesc.SelectionLength=0;
            clientColorHelp.Text=$"Inserted ^{hex} at cursor • ใช้ Reset เพื่อกลับเป็น ^000000";
            return;
        }
        string wrapped=RagnarokColorHelper.Wrap(selected,hex,true);
        cDesc.SelectedText=wrapped;
        cDesc.SelectionStart=start+wrapped.Length;
        cDesc.SelectionLength=0;
        clientColorHelp.Text=$"Applied ^{hex} to selected text and appended ^000000 reset";
    }

    void InsertClientColorCode(){
        string hex=CurrentColorHex();if(hex.Length==0)return;
        int start=cDesc.SelectionStart;
        cDesc.SelectedText=RagnarokColorHelper.Code(hex);
        cDesc.SelectionStart=start+7;cDesc.SelectionLength=0;cDesc.Focus();
        clientColorHelp.Text=$"Inserted color code ^{hex}";
    }

    void InsertClientColorReset(){
        int start=cDesc.SelectionStart;
        string code=RagnarokColorHelper.Code(RagnarokColorHelper.ResetHex);
        cDesc.SelectedText=code;
        cDesc.SelectionStart=start+code.Length;cDesc.SelectionLength=0;cDesc.Focus();
        clientColorHelp.Text="Inserted reset code ^000000";
    }

    void StripClientColorCodes(){
        if(MessageBox.Show("ลบโค้ดสี ^RRGGBB ทั้งหมดออกจาก Description?\r\nข้อความจะยังอยู่เหมือนเดิม","Remove color codes",MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)!=DialogResult.Yes)return;
        int caret=Math.Min(cDesc.SelectionStart,cDesc.TextLength);
        cDesc.Text=Regex.Replace(cDesc.Text,@"\^[0-9A-Fa-f]{6}","");
        cDesc.SelectionStart=Math.Min(caret,cDesc.TextLength);
        clientColorHelp.Text="Removed all ^RRGGBB color codes";
    }

    void LoadBonusCatalog(){
        try{
            bonusCatalogRoot=ctx.Settings.ServerRoot;
            bonusSnippets=ServerScriptCatalog.Load(ctx.Settings.ServerRoot);
            FilterBonusList();
            bonusHelp.Text=bonusSnippets.Count==0
                ?"ไม่พบ src/map/pc.cpp + script_constants.hpp ใน Server root"
                :$"Loaded {bonusSnippets.Count:N0} bonus definitions from current Server source.";
        }catch(Exception ex){
            bonusSnippets=[];visibleBonusSnippets=[];bonusList.Items.Clear();
            bonusHelp.Text="Script Helper: "+ex.Message;
        }
    }

    void FilterBonusList(){
        string q=bonusSearch.Text.Trim();
        visibleBonusSnippets=bonusSnippets.Where(x=>q.Length==0
            ||x.Bonus.Contains(q,StringComparison.OrdinalIgnoreCase)
            ||x.Command.Contains(q,StringComparison.OrdinalIgnoreCase)
            ||x.Syntax.Contains(q,StringComparison.OrdinalIgnoreCase)
            ||x.Help.Contains(q,StringComparison.OrdinalIgnoreCase)).ToList();
        bonusList.BeginUpdate();
        try{
            bonusList.Items.Clear();
            foreach(var s in visibleBonusSnippets){
                var row=new ListViewItem([s.Bonus,s.Command,s.Syntax]){Tag=s,ToolTipText=s.Help+"\r\nSource: "+s.Source};
                bonusList.Items.Add(row);
            }
        }finally{bonusList.EndUpdate();}
    }

    void ShowBonusHelp(){
        if(bonusList.SelectedItems.Count==0||bonusList.SelectedItems[0].Tag is not ServerScriptSnippet s)return;
        bonusHelp.Text=s.Help+"\r\nSource: "+s.Source+"\r\nDouble-click เพื่อแทรกลง "+bonusTarget.Text;
    }

    RichTextBox SelectedScriptEditor()=>bonusTarget.SelectedIndex switch{
        1=>sEquipScript,
        2=>sUnEquipScript,
        _=>sScript
    };

    void InsertSelectedBonus(){
        if(bonusList.SelectedItems.Count==0||bonusList.SelectedItems[0].Tag is not ServerScriptSnippet s)return;
        var box=SelectedScriptEditor();
        string insert=s.Syntax;
        if(box.SelectionStart>0&&box.TextLength>0&&!box.Text[..box.SelectionStart].EndsWith("\n",StringComparison.Ordinal))insert=Environment.NewLine+insert;
        box.SelectedText=insert+Environment.NewLine;
        box.Focus();
    }

    static string RawScriptBlock(string raw,string key){
        if(string.IsNullOrWhiteSpace(raw))return "";
        var lines=raw.Replace("\r","").Split('\n');
        for(int i=0;i<lines.Length;i++){
            var header=Regex.Match(lines[i],$@"^(?<indent>\s*){Regex.Escape(key)}:\s*(?<rest>.*)$");
            if(!header.Success)continue;
            string rest=header.Groups["rest"].Value.Trim();
            if(rest!="|")return rest.Trim('"','\'');
            int headerIndent=header.Groups["indent"].Value.Length;
            var body=new List<string>();
            for(int j=i+1;j<lines.Length;j++){
                string line=lines[j];
                if(line.Length==0){body.Add("");continue;}
                int indent=line.TakeWhile(char.IsWhiteSpace).Count();
                if(indent<=headerIndent)break;
                int remove=Math.Min(line.Length,headerIndent+2);
                body.Add(line[remove..]);
            }
            return string.Join(Environment.NewLine,body).TrimEnd();
        }
        return "";
    }

    static void AppendRawScript(StringBuilder raw,string key,string script){
        if(string.IsNullOrWhiteSpace(script))return;
        raw.AppendLine(key+": |");
        foreach(var line in script.Replace("\r","").Split('\n'))
            raw.AppendLine("  "+line);
    }

    public void RefreshItemList(){
        if(!string.Equals(bonusCatalogRoot,ctx.Settings.ServerRoot,StringComparison.OrdinalIgnoreCase))LoadBonusCatalog();
        allItemIds=ctx.Catalog.ClientItems.Keys
            .Concat(ctx.Catalog.ServerItems.Keys)
            .Concat(ctx.Catalog.ById.Keys)
            .Distinct()
            .OrderBy(x=>x)
            .ToList();
        FilterItemList();
    }

    (string Aegis,string Name,string ClientName,string Status) ListRow(int itemId){
        ctx.Catalog.ClientItems.TryGetValue(itemId,out var clientItem);
        ctx.Catalog.ServerItems.TryGetValue(itemId,out var serverItem);
        ctx.Catalog.ById.TryGetValue(itemId,out var serverRef);
        string aegis=serverRef?.AegisName??serverItem?.Resource??"";
        string name=serverRef?.Name??serverItem?.Name??clientItem?.Name??"";
        return(aegis,name,clientItem?.Name??"",ctx.Catalog.StatusFor(itemId));
    }

    void FilterItemList(){
        string q=itemSearch.Text.Trim();
        if(q.Length==0)visibleItemIds=[..allItemIds];
        else {
            visibleItemIds=allItemIds.Where(itemId=>{
                if(itemId.ToString().Contains(q,StringComparison.OrdinalIgnoreCase))return true;
                var row=ListRow(itemId);
                return row.Aegis.Contains(q,StringComparison.OrdinalIgnoreCase)
                    ||row.Name.Contains(q,StringComparison.OrdinalIgnoreCase)
                    ||row.ClientName.Contains(q,StringComparison.OrdinalIgnoreCase);
            }).ToList();
        }
        itemList.VirtualListSize=visibleItemIds.Count;
        itemList.Invalidate();
    }

    void ItemListRetrieve(object? sender,RetrieveVirtualItemEventArgs e){
        if(e.ItemIndex<0||e.ItemIndex>=visibleItemIds.Count){e.Item=new ListViewItem("");return;}
        int itemId=visibleItemIds[e.ItemIndex];
        var row=ListRow(itemId);
        e.Item=new ListViewItem([
            itemId.ToString(),
            row.Aegis,
            row.Name,
            row.ClientName,
            row.Status
        ]);
    }

    void LoadSelectedFromList(){
        if(itemList.SelectedIndices.Count==0)return;
        int index=itemList.SelectedIndices[0];
        if(index<0||index>=visibleItemIds.Count)return;
        id.Text=visibleItemIds[index].ToString();
        LoadItem();
    }

    async Task LoadIconAsync(Item? clientItem,int itemId){
        iconCancel?.Cancel();iconCancel?.Dispose();iconCancel=new CancellationTokenSource();
        var token=iconCancel.Token;
        var old=itemIcon.Image;itemIcon.Image=null;old?.Dispose();

        ctx.Catalog.ById.TryGetValue(itemId,out var serverRef);
        string title=serverRef?.Name??clientItem?.Name??$"Item {itemId}";
        itemIconInfo.Text=$"{title}\r\nItem ID: {itemId}\r\nAegis: {serverRef?.AegisName??"-"}\r\nResource: {clientItem?.Resource??"-"}";

        if(clientItem==null){itemIconInfo.Text+="\r\nIcon: ไม่มีข้อมูล Client itemInfo";return;}
        if(ctx.Catalog.Assets==null){itemIconInfo.Text+="\r\nIcon: กรุณา Reload Project / โหลด GRF";return;}

        try{
            var assets=ctx.Catalog.Assets;
            var result=await Task.Run(()=>{
                var data=assets.ItemImage(clientItem,false,token);
                using var ms=new MemoryStream(data.Bytes);
                using var source=Image.FromStream(ms);
                if(source.Width>4096||source.Height>4096)throw new InvalidDataException("Image dimensions too large.");
                var bmp=new Bitmap(source);bmp.MakeTransparent(Color.Magenta);
                return(Image:bmp,Source:data.Source);
            },token);
            if(token.IsCancellationRequested){result.Image.Dispose();return;}
            old=itemIcon.Image;itemIcon.Image=result.Image;old?.Dispose();
            itemIconInfo.Text+=$"\r\nIcon: {Path.GetFileName(result.Source.Split(new[]{" :: "},StringSplitOptions.None)[0])}";
        }catch(OperationCanceledException){}
        catch(Exception ex){if(!token.IsCancellationRequested)itemIconInfo.Text+="\r\nIcon: "+ex.Message;}
    }

    bool ConfirmSave(string scope,int itemId,string details){
        var answer=MessageBox.Show(
            $"ยืนยันการบันทึก {scope}?\r\n\r\nItem ID: {itemId}\r\n{details}\r\n\r\nระบบจะสร้าง backup ก่อนเขียนไฟล์จริง",
            "ยืนยันการบันทึก Item Editor",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
        return answer==DialogResult.Yes;
    }

    async Task SaveBoth(){
        if(!TryId(out int itemId)){MessageBox.Show("กรุณาเลือก Item จากรายการก่อน");return;}
        if(!ConfirmSave("Client + Server",itemId,$"Client: {cName.Text}\r\nServer: {sName.Text}"))return;
        await SaveClient(false);
        await SaveServer(false);
    }

    bool TryId(out int value)=>int.TryParse(id.Text.Trim(),out value)&&value>0;
    string ClientTarget() {
        string configured=ctx.Settings.ClientItemWriteTarget;
        return Path.IsPathRooted(configured)?configured:Path.Combine(ctx.Settings.Client,configured);
    }
    static string RawField(string raw,string key) {
        if(string.IsNullOrWhiteSpace(raw))return "";
        var m=Regex.Match(raw,$@"(?mi)^\s*(?:-\s*)?{Regex.Escape(key)}:\s*([^\r\n#]+)");
        return m.Success?m.Groups[1].Value.Trim().Trim('"','\''):"";
    }
    static int IntValue(TextBox box)=>int.TryParse(box.Text.Trim(),out var value)&&value>=0?value:0;
    static string SourceName(string source) {
        if(string.IsNullOrWhiteSpace(source))return "";
        if(source.StartsWith("GRF :: ",StringComparison.OrdinalIgnoreCase))return source;
        return Path.GetFileName(source);
    }
    void RefreshState(int itemId,string prefix="") {
        var cmp=ctx.Catalog.CompareFor(itemId);
        var parts=new List<string>();
        if(prefix.Length>0)parts.Add(prefix);
        parts.Add(cmp.Status);
        if(cmp.Status=="MISMATCH")parts.Add(cmp.Reason);
        var clientChain=ctx.ClientSourceChain(itemId);
        if(clientChain.Count>1)parts.Add($"Client definitions {clientChain.Count} • winner {SourceName(cmp.ClientSource)}");
        var serverChain=ctx.ServerSourceChain(itemId);
        if(serverChain.Count>1)parts.Add($"Server definitions {serverChain.Count} • winner {SourceName(cmp.ServerSource)}");
        state.Text=string.Join(" • ",parts);
        StudioTheme.StyleStatus(state,cmp.Status);
    }

    void LoadItem() {
        if(!TryId(out int itemId)){MessageBox.Show("กรุณาใส่ Item ID");return;}
        ctx.Catalog.ClientItems.TryGetValue(itemId,out var ci);
        ctx.Catalog.ServerItems.TryGetValue(itemId,out var si);
        if(ci!=null){cName.Text=ci.Name;cResource.Text=ci.Resource;cDesc.Text=ci.Description;}
        else {cName.Clear();cResource.Clear();cDesc.Clear();}

        var meta=SafeProjectFile.ReadClientTableMetadata(ClientTarget(),ctx.Settings.ClientItemTable,itemId);
        cSlot.Text=meta.SlotCount.ToString();cClass.Text=meta.ClassNum.ToString();cCostume.Checked=meta.Costume;

        string raw="",source="";
        if(si!=null){sName.Text=si.Name;sAegis.Text=si.Resource;raw=si.Raw;source=si.Source;}
        else if(ctx.Catalog.ById.TryGetValue(itemId,out var sr)){sName.Text=sr.Name;sAegis.Text=sr.AegisName;raw=sr.Raw;source=sr.Source;}
        else {sName.Clear();sAegis.Clear();}
        if(string.IsNullOrWhiteSpace(sAegis.Text))sAegis.Text=ci!=null?SafeProjectFile.Aegis(string.IsNullOrWhiteSpace(ci.Resource)?ci.Name:ci.Resource,itemId):"";
        sType.Text=RawField(raw,"Type");if(string.IsNullOrWhiteSpace(sType.Text))sType.Text="Etc";
        sWeight.Text=RawField(raw,"Weight");sSlots.Text=RawField(raw,"Slots");sView.Text=RawField(raw,"View");
        sScript.Text=RawScriptBlock(raw,"Script");
        sEquipScript.Text=RawScriptBlock(raw,"EquipScript");
        sUnEquipScript.Text=RawScriptBlock(raw,"UnEquipScript");
        sRaw.Text=raw+(source.Length>0?"\r\n\r\nSOURCE: "+source:"");
        RefreshState(itemId);
        if(ci!=null&&!ctx.Catalog.HasItemIcon(ci))state.Text+=" • MISSING ICON";
        _=LoadIconAsync(ci,itemId);
    }
    void ClientToDb(){
        if(!TryId(out int itemId))return;
        sName.Text=cName.Text;
        sAegis.Text=SafeProjectFile.Aegis(string.IsNullOrWhiteSpace(cResource.Text)?cName.Text:cResource.Text,itemId);
        if(string.IsNullOrWhiteSpace(sType.Text))sType.Text="Etc";
        if(string.IsNullOrWhiteSpace(sSlots.Text))sSlots.Text=cSlot.Text;
        state.Text="Converted Client → DB • review then Save Server";
    }
    void DbToClient(){
        if(!TryId(out int itemId))return;
        cName.Text=sName.Text;
        cResource.Text=string.IsNullOrWhiteSpace(sAegis.Text)?SafeProjectFile.Aegis(sName.Text,itemId):sAegis.Text;
        if(string.IsNullOrWhiteSpace(cDesc.Text))cDesc.Text=sName.Text;
        if(string.IsNullOrWhiteSpace(cSlot.Text))cSlot.Text=sSlots.Text;
        state.Text="Converted DB → Client • review then Save Client";
    }

    Item ClientRecord(int itemId)=>new(itemId,cName.Text.Trim(),cResource.Text.Trim(),cDesc.Text,"Maven RO Studio override","");
    Item ServerRecord(int itemId) {
        var raw=new StringBuilder();
        raw.AppendLine("Type: "+(string.IsNullOrWhiteSpace(sType.Text)?"Etc":sType.Text.Trim()));
        if(!string.IsNullOrWhiteSpace(sWeight.Text))raw.AppendLine("Weight: "+sWeight.Text.Trim());
        if(!string.IsNullOrWhiteSpace(sSlots.Text))raw.AppendLine("Slots: "+sSlots.Text.Trim());
        if(!string.IsNullOrWhiteSpace(sView.Text))raw.AppendLine("View: "+sView.Text.Trim());
        AppendRawScript(raw,"Script",sScript.Text);
        AppendRawScript(raw,"EquipScript",sEquipScript.Text);
        AppendRawScript(raw,"UnEquipScript",sUnEquipScript.Text);
        return new Item(itemId,sName.Text.Trim(),sAegis.Text.Trim(),"","Maven RO Studio override",raw.ToString());
    }

    async Task SaveClient(bool askConfirm) {
        if(!TryId(out int itemId))return;
        if(string.IsNullOrWhiteSpace(cName.Text)){MessageBox.Show("Client Display Name ห้ามว่าง");return;}
        string root=ctx.Settings.Client;
        if(!Directory.Exists(root)){MessageBox.Show("Client root ไม่ถูกต้อง");return;}
        string configured=ctx.Settings.ClientItemWriteTarget;
        string liveTarget=Path.IsPathRooted(configured)?configured:Path.Combine(root,configured);
        string file=File.Exists(liveTarget)?liveTarget:Path.Combine(root,"System","maven_ro_studio_iteminfo.lua");
        if(askConfirm&&!ConfirmSave("Client",itemId,$"Name: {cName.Text}\r\nTarget: {file}"))return;
        try {
            if(File.Exists(liveTarget))
                SafeProjectFile.UpsertClientTableEntry(liveTarget,ctx.Settings.ClientItemTable,ClientRecord(itemId),ctx.Settings.Encoding,IntValue(cSlot),IntValue(cClass),cCostume.Checked);
            else
                SafeProjectFile.UpsertClientOverride(file,ClientRecord(itemId),ctx.Settings.Encoding);
            ctx.RegisterClientSource(file);ctx.Log("Item Editor saved client itemInfo: "+file);
            await ctx.ReloadClient();
            RefreshState(itemId,File.Exists(liveTarget)?"Saved Client LIVE":"Saved Client override");
        } catch(Exception ex){MessageBox.Show(ex.Message,"Save Client");}
    }

    async Task SaveServer(bool askConfirm) {
        if(!TryId(out int itemId))return;
        if(string.IsNullOrWhiteSpace(sName.Text)){MessageBox.Show("Server Name ห้ามว่าง");return;}
        foreach(var field in new[]{("Weight",sWeight),("Slots",sSlots),("View",sView)}) {
            if(field.Item2.TextLength>0&&(!int.TryParse(field.Item2.Text.Trim(),out var n)||n<0)){MessageBox.Show(field.Item1+" ต้องเป็นเลข 0 ขึ้นไป");return;}
        }
        string root=ctx.Settings.ServerRoot;
        if(!Directory.Exists(root)){MessageBox.Show("Server root ไม่ถูกต้อง");return;}
        string configured=ctx.Settings.ServerItemWriteTarget;
        string file=Path.IsPathRooted(configured)?configured:Path.Combine(root,configured);
        if(askConfirm&&!ConfirmSave("Server",itemId,$"Name: {sName.Text}\r\nTarget: {file}"))return;
        try {
            SafeProjectFile.UpsertServerOverride(file,ServerRecord(itemId));
            string relative=Path.GetRelativePath(root,file).Replace('\\','/');
            SafeProjectFile.EnsureServerImport(root,relative);
            ctx.RegisterServerSource(file);ctx.Log("Item Editor saved server override: "+file);
            await ctx.ReloadServer();RefreshState(itemId,"Saved Server");
        } catch(Exception ex){MessageBox.Show(ex.Message,"Save Server");}
    }
}

public abstract class YamlEditorControl : UserControl {
    protected readonly EditorContext Ctx;
    protected readonly ListBox Files=new(){Dock=DockStyle.Fill,HorizontalScrollbar=true};
    protected readonly TextBox Editor=new(){Dock=DockStyle.Fill,Multiline=true,AcceptsTab=true,ScrollBars=ScrollBars.Both,WordWrap=false,Font=new Font("Consolas",10)};
    protected readonly ListView Summary=new(){Dock=DockStyle.Bottom,Height=180,View=View.Details,FullRowSelect=true};
    readonly Label status=new(){Dock=DockStyle.Bottom,Height=26};
    protected string CurrentFile="";

    protected YamlEditorControl(EditorContext ctx,string title){Ctx=ctx;Dock=DockStyle.Fill;
        var top=new FlowLayoutPanel{Dock=DockStyle.Top,Height=42,WrapContents=false};
        Btn(top,"Refresh",RefreshFiles);Btn(top,"Load",LoadSelected);Btn(top,"Validate",ValidateYamlEditor);Btn(top,"Save",Save);Btn(top,"New Template",NewTemplate);
        top.Controls.Add(new Label{Text=title,AutoSize=true,Padding=new Padding(14,8,0,0),Font=new Font(Font,FontStyle.Bold)});
        var split=new SplitContainer{Dock=DockStyle.Fill,SplitterDistance=310};split.Panel1.Controls.Add(Files);split.Panel2.Controls.Add(Editor);split.Panel2.Controls.Add(Summary);
        Controls.Add(split);Controls.Add(top);Controls.Add(status);
        Files.DoubleClick+=(_,_)=>LoadSelected();
    }
    static void Btn(FlowLayoutPanel p,string text,Action a){var b=new Button{Text=text,AutoSize=true,Height=30};b.Click+=(_,_)=>a();p.Controls.Add(b);}
    protected abstract IEnumerable<string> Discover();
    protected abstract void BuildSummary(string text);
    protected abstract string TemplateText();
    public void RefreshFiles(){Files.Items.Clear();foreach(var f in Discover())Files.Items.Add(f);}
    void LoadSelected(){if(Files.SelectedItem is not string f)return;CurrentFile=f;Editor.Text=File.ReadAllText(f);BuildSummary(Editor.Text);status.Text=f;}
    void ValidateYamlEditor(){try{SafeProjectFile.ValidateYaml(Editor.Text);BuildSummary(Editor.Text);status.Text="YAML VALID • "+CurrentFile;}catch(Exception ex){status.Text="INVALID • "+ex.Message;}}
    void Save(){if(string.IsNullOrWhiteSpace(CurrentFile)){using var d=new SaveFileDialog{Filter="YAML|*.yml;*.yaml",InitialDirectory=Directory.Exists(Ctx.Settings.ServerRoot)?Ctx.Settings.ServerRoot:""};if(d.ShowDialog()!=DialogResult.OK)return;CurrentFile=d.FileName;}
        try{SafeProjectFile.Write(CurrentFile,Editor.Text,true);BuildSummary(Editor.Text);status.Text="SAVED + BACKUP • "+CurrentFile;Ctx.Log("Saved "+CurrentFile);RefreshFiles();}catch(Exception ex){MessageBox.Show(ex.Message,"YAML Save");}}
    void NewTemplate(){CurrentFile="";Editor.Text=TemplateText();BuildSummary(Editor.Text);status.Text="New template • choose Save";}
}

public static class BarterYamlOps {
    static YamlScalarNode Key(string value)=>new(value);
    public static uint U(YamlMappingNode n,string key,uint fallback=0)=>uint.TryParse(n.Children.TryGetValue(Key(key),out var v)?v.ToString():"",out var x)?x:fallback;
    public static string S(YamlMappingNode n,string key,string fallback="")=>n.Children.TryGetValue(Key(key),out var v)?v.ToString():fallback;
    public static void Set(YamlMappingNode n,string key,string value)=>n.Children[Key(key)]=new YamlScalarNode(value);

    public static YamlSequenceNode Items(YamlMappingNode shop,bool create=true){
        if(shop.Children.TryGetValue(Key("Items"),out var node)&&node is YamlSequenceNode seq)return seq;
        if(!create)throw new InvalidOperationException("Shop has no Items sequence.");
        var created=new YamlSequenceNode();shop.Children[Key("Items")]=created;return created;
    }
    public static YamlSequenceNode Requirements(YamlMappingNode item,bool create=true){
        if(item.Children.TryGetValue(Key("RequiredItems"),out var node)&&node is YamlSequenceNode seq)return seq;
        if(!create)throw new InvalidOperationException("Item has no RequiredItems sequence.");
        var created=new YamlSequenceNode();item.Children[Key("RequiredItems")]=created;return created;
    }
    public static uint NextIndex(YamlSequenceNode seq){
        var used=seq.OfType<YamlMappingNode>().Select(x=>U(x,"Index",uint.MaxValue)).Where(x=>x!=uint.MaxValue).ToHashSet();
        uint i=0;while(used.Contains(i))i++;return i;
    }
    public static void Reindex(YamlSequenceNode seq){
        uint i=0;foreach(var n in seq.OfType<YamlMappingNode>())Set(n,"Index",(i++).ToString());
    }
    public static YamlMappingNode AddItem(YamlMappingNode shop,string aegis="Red_Potion"){
        var items=Items(shop);
        var item=new YamlMappingNode{
            {Key("Index"),new YamlScalarNode(NextIndex(items).ToString())},
            {Key("Item"),new YamlScalarNode(aegis)},
            {Key("Stock"),new YamlScalarNode("0")},
            {Key("SuccessRate"),new YamlScalarNode("10000")},
            {Key("ConsumeOnFail"),new YamlScalarNode("true")},
            {Key("AccountLimit"),new YamlScalarNode("0")},
            {Key("CharacterLimit"),new YamlScalarNode("0")},
            {Key("Zeny"),new YamlScalarNode("0")}
        };
        items.Add(item);return item;
    }
    public static YamlMappingNode CloneItem(YamlMappingNode shop,YamlMappingNode source){
        var clone=new YamlMappingNode();
        foreach(var kv in source.Children)clone.Add(kv.Key,new YamlScalarNode(kv.Value is YamlScalarNode s?s.Value:kv.Value.ToString()));
        // Preserve nested requirements with a real deep copy.
        if(source.Children.TryGetValue(Key("RequiredItems"),out var reqNode)&&reqNode is YamlSequenceNode reqs){
            var copied=new YamlSequenceNode();
            foreach(var req in reqs.OfType<YamlMappingNode>()){
                var c=new YamlMappingNode();
                foreach(var kv in req.Children)c.Add(kv.Key,new YamlScalarNode(kv.Value.ToString()));
                copied.Add(c);
            }
            clone.Children[Key("RequiredItems")]=copied;
        }
        var items=Items(shop);Set(clone,"Index",NextIndex(items).ToString());items.Add(clone);return clone;
    }
    public static YamlMappingNode AddRequirement(YamlMappingNode item,string aegis="Jellopy",uint amount=1,int refine=-1){
        var reqs=Requirements(item);
        var req=new YamlMappingNode{
            {Key("Index"),new YamlScalarNode(NextIndex(reqs).ToString())},
            {Key("Item"),new YamlScalarNode(aegis)},
            {Key("Amount"),new YamlScalarNode(Math.Max(1,amount).ToString())}
        };
        if(refine>=0)req.Add(Key("Refine"),new YamlScalarNode(refine.ToString()));
        reqs.Add(req);return req;
    }
    public static bool Move(YamlSequenceNode seq,YamlNode node,int delta,bool reindex=true){
        int index=seq.Children.IndexOf(node);int target=index+delta;
        if(index<0||target<0||target>=seq.Children.Count)return false;
        seq.Children.RemoveAt(index);seq.Children.Insert(target,node);
        if(reindex)Reindex(seq);
        return true;
    }
}

public sealed class BarterEditorControl : YamlEditorControl {
    sealed record Selection(string Shop,int Index);
    sealed record RequirementSelection(string Shop,int ItemIndex,int RequirementIndex);

    readonly TextBox rewardItem=new(){Width=315};
    readonly NumericUpDown zeny=new(){Width=120,Maximum=int.MaxValue};
    readonly ListView requirements=new(){Dock=DockStyle.Fill,View=View.Details,FullRowSelect=true,GridLines=true,MultiSelect=false,HideSelection=false};

    readonly PictureBox itemPreviewIcon=new(){Size=new Size(112,112),SizeMode=PictureBoxSizeMode.Zoom,BackColor=Color.FromArgb(40,40,46),Margin=new Padding(12)};
    readonly Label itemPreviewName=new(){AutoSize=true,Font=new Font("Segoe UI",12,FontStyle.Bold),MaximumSize=new Size(260,0)};
    readonly Label itemPreviewMeta=new(){AutoSize=true,MaximumSize=new Size(300,0)};
    readonly Label itemPreviewSource=new(){AutoSize=true,MaximumSize=new Size(300,0),ForeColor=Color.DimGray};
    readonly Panel equipPreview=new(){Dock=DockStyle.Fill,BackColor=Color.FromArgb(242,242,246)};
    readonly Label equipPreviewInfo=new(){Dock=DockStyle.Bottom,Height=64,TextAlign=ContentAlignment.MiddleCenter};
    CancellationTokenSource? previewCancel;
    readonly TextBox requirementItem=new(){Width=185};
    readonly NumericUpDown requirementAmount=new(){Width=90,Minimum=1,Maximum=65535,Value=1};
    readonly NumericUpDown requirementRefine=new(){Width=90,Minimum=-1,Maximum=100,Value=-1};

    readonly TextBox eventId=new(){Width=315};
    readonly TextBox start=new(){Width=315,PlaceholderText="YYYY-MM-DD HH:MM:SS"};
    readonly TextBox end=new(){Width=315,PlaceholderText="YYYY-MM-DD HH:MM:SS"};
    readonly NumericUpDown stock=new(){Width=120,Maximum=65535};
    readonly NumericUpDown successRate=new(){Width=120,Maximum=10000,Value=10000};
    readonly NumericUpDown accountLimit=new(){Width=120,Maximum=100000000};
    readonly NumericUpDown characterLimit=new(){Width=120,Maximum=100000000};
    readonly CheckBox consumeOnFail=new(){Text="Consume requirements / Zeny on failure",AutoSize=true,Checked=true};
    readonly Label selected=new(){AutoSize=true,MaximumSize=new Size(320,0),Text="Select an item row below"};
    readonly Label v2Status=new(){AutoSize=true,MaximumSize=new Size(320,0)};

    public bool VisualLayoutReady =>
        Summary.Columns.Count>=8 &&
        requirements.Columns.Count>=6 &&
        itemPreviewIcon.Width>=96 &&
        Controls.OfType<SplitContainer>().Any();

    public BarterEditorControl(EditorContext ctx):base(ctx,"Barter Shop Editor • Visual Craft Layout"){
        Summary.Columns.Add("Shop",120);
        Summary.Columns.Add("Index",50);
        Summary.Columns.Add("ID",70);
        Summary.Columns.Add("AegisName",150);
        Summary.Columns.Add("Name",170);
        Summary.Columns.Add("Stock",65);
        Summary.Columns.Add("Success",70);
        Summary.Columns.Add("Zeny",80);
        Summary.SelectedIndexChanged+=(_,_)=>LoadSelection();

        requirements.Columns.Add("Index",48);
        requirements.Columns.Add("ID",68);
        requirements.Columns.Add("AegisName",145);
        requirements.Columns.Add("Name",160);
        requirements.Columns.Add("Amount",65);
        requirements.Columns.Add("Refine",55);
        requirements.SelectedIndexChanged+=(_,_)=>LoadRequirementSelection();

        var host=Controls.OfType<SplitContainer>().FirstOrDefault();
        if(host!=null){
            host.SplitterDistance=260;
            host.Panel2.Controls.Clear();

            var tabs=new TabControl{Dock=DockStyle.Fill};
            var visualPage=new TabPage("Visual Editor");
            var rawPage=new TabPage("Raw YAML");
            rawPage.Controls.Add(Editor);
            Editor.Dock=DockStyle.Fill;
            tabs.TabPages.Add(visualPage);
            tabs.TabPages.Add(rawPage);

            var main=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Padding=new Padding(6)};
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,64));
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,36));
            visualPage.Controls.Add(main);

            var left=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=2,ColumnCount=1};
            left.RowStyles.Add(new RowStyle(SizeType.Percent,56));
            left.RowStyles.Add(new RowStyle(SizeType.Percent,44));
            main.Controls.Add(left,0,0);

            var soldGroup=new GroupBox{Text="Sold Items",Dock=DockStyle.Fill,Padding=new Padding(8)};
            var soldToolbar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=38,WrapContents=false};
            Btn(soldToolbar,"+ Item",AddReward);
            Btn(soldToolbar,"Clone",CloneReward);
            Btn(soldToolbar,"Delete",RemoveReward);
            Btn(soldToolbar,"↑",()=>MoveReward(-1));
            Btn(soldToolbar,"↓",()=>MoveReward(1));
            Summary.Dock=DockStyle.Fill;
            soldGroup.Controls.Add(Summary);
            soldGroup.Controls.Add(soldToolbar);
            left.Controls.Add(soldGroup,0,0);

            var reqGroup=new GroupBox{Text="Required Items / Craft Recipe",Dock=DockStyle.Fill,Padding=new Padding(8)};
            var reqToolbar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=38,WrapContents=false};
            Btn(reqToolbar,"+ Material",AddRequirement);
            Btn(reqToolbar,"Delete",RemoveRequirement);
            Btn(reqToolbar,"↑",()=>MoveRequirement(-1));
            Btn(reqToolbar,"↓",()=>MoveRequirement(1));
            var reqEdit=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=72,WrapContents=true,Padding=new Padding(2)};
            reqEdit.Controls.Add(new Label{Text="Aegis",AutoSize=true,Padding=new Padding(0,8,0,0)});
            reqEdit.Controls.Add(requirementItem);
            reqEdit.Controls.Add(new Label{Text="Amount",AutoSize=true,Padding=new Padding(5,8,0,0)});
            reqEdit.Controls.Add(requirementAmount);
            reqEdit.Controls.Add(new Label{Text="Refine",AutoSize=true,Padding=new Padding(5,8,0,0)});
            reqEdit.Controls.Add(requirementRefine);
            var applyReq=new Button{Text="Apply Material",AutoSize=true,Height=30};
            applyReq.Click+=(_,_)=>ApplyRequirement();reqEdit.Controls.Add(applyReq);
            reqGroup.Controls.Add(requirements);
            reqGroup.Controls.Add(reqEdit);
            reqGroup.Controls.Add(reqToolbar);
            left.Controls.Add(reqGroup,0,1);

            var rightTabs=new TabControl{Dock=DockStyle.Fill};
            var previewPage=new TabPage("Preview");
            var editPage=new TabPage("Edit / Hot Week");
            rightTabs.TabPages.Add(previewPage);
            rightTabs.TabPages.Add(editPage);
            main.Controls.Add(rightTabs,1,0);

            var previewLayout=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=2,ColumnCount=1,Padding=new Padding(4)};
            previewLayout.RowStyles.Add(new RowStyle(SizeType.Percent,52));
            previewLayout.RowStyles.Add(new RowStyle(SizeType.Percent,48));
            previewPage.Controls.Add(previewLayout);

            var itemGroup=new GroupBox{Text="Item Preview",Dock=DockStyle.Fill,Padding=new Padding(10)};
            var itemPreviewLayout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1};
            itemPreviewLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,138));
            itemPreviewLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            var iconHost=new Panel{Dock=DockStyle.Fill};
            itemPreviewIcon.Location=new Point(10,18);iconHost.Controls.Add(itemPreviewIcon);
            var metaFlow=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true};
            metaFlow.Controls.Add(itemPreviewName);metaFlow.Controls.Add(itemPreviewMeta);metaFlow.Controls.Add(itemPreviewSource);
            itemPreviewLayout.Controls.Add(iconHost,0,0);itemPreviewLayout.Controls.Add(metaFlow,1,0);
            itemGroup.Controls.Add(itemPreviewLayout);
            previewLayout.Controls.Add(itemGroup,0,0);

            var equipGroup=new GroupBox{Text="Equip Preview",Dock=DockStyle.Fill,Padding=new Padding(10)};
            equipPreview.Paint+=DrawEquipPreview;
            equipGroup.Controls.Add(equipPreview);
            equipGroup.Controls.Add(equipPreviewInfo);
            previewLayout.Controls.Add(equipGroup,0,1);

            var tools=new FlowLayoutPanel{
                Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,
                AutoScroll=true,Padding=new Padding(12),BackColor=Color.FromArgb(247,247,250)
            };
            tools.Controls.Add(new Label{Text="REWARD ITEM",AutoSize=true,Font=new Font(Font,FontStyle.Bold)});
            tools.Controls.Add(selected);
            tools.Controls.Add(Field("Reward Item • AegisName",rewardItem));
            tools.Controls.Add(Row("Zeny",zeny));
            tools.Controls.Add(new Label{Text="HOT WEEK / SECURE V2",AutoSize=true,Font=new Font(Font,FontStyle.Bold),Margin=new Padding(3,10,3,3)});
            tools.Controls.Add(Field("Event ID • เปลี่ยนค่าเพื่อ reset limit",eventId));
            tools.Controls.Add(Field("Start • เวลาของ Server",start));
            tools.Controls.Add(Field("End • เวลาของ Server",end));
            tools.Controls.Add(Row("Stock • 0 = unlimited",stock));
            tools.Controls.Add(Row("SuccessRate • 10000 = 100%",successRate));
            tools.Controls.Add(consumeOnFail);
            tools.Controls.Add(Row("Account limit • 0 = unlimited",accountLimit));
            tools.Controls.Add(Row("Character limit • 0 = unlimited",characterLimit));
            var buttons=new FlowLayoutPanel{AutoSize=true,Width=320,WrapContents=true};
            Btn(buttons,"Apply to selected",ApplyV2);
            Btn(buttons,"Validate V2",()=>BuildSummary(Editor.Text));
            Btn(buttons,"Hot Week Template",NewHotWeekTemplate);
            tools.Controls.Add(buttons);
            tools.Controls.Add(v2Status);
            editPage.Controls.Add(tools);

            host.Panel2.Controls.Add(tabs);
        }
        RefreshFiles();
    }

    static Control Field(string label,Control input){
        var p=new FlowLayoutPanel{FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoSize=true,Width=320};
        p.Controls.Add(new Label{Text=label,AutoSize=true});
        p.Controls.Add(input);
        return p;
    }
    static Control Row(string label,Control input){
        var p=new FlowLayoutPanel{AutoSize=true,Width=320,WrapContents=false};
        p.Controls.Add(new Label{Text=label,AutoSize=true,Width=190,Padding=new Padding(0,7,0,0)});
        p.Controls.Add(input);
        return p;
    }
    static void Btn(FlowLayoutPanel p,string text,Action action){
        var b=new Button{Text=text,AutoSize=true,Height=30};
        b.Click+=(_,_)=>action();
        p.Controls.Add(b);
    }

    protected override IEnumerable<string> Discover()=>Ctx.Catalog.BarterFiles;

    protected override string TemplateText()=>@"Header:
  Type: BARTER_DB
  Version: 1

Body:
  - Name: MAVEN_HOT_WEEK
    EventId: HOT_WEEK_202610
    Start: ""2026-10-04 20:00:00""
    End: ""2026-10-11 23:59:59""
    Map: morocc03
    X: 150
    Y: 100
    Direction: South
    Sprite: 4_F_KAFRA6
    Items:
      - Index: 0
        Item: Red_Potion
        Stock: 0
        SuccessRate: 10000
        ConsumeOnFail: true
        AccountLimit: 0
        CharacterLimit: 0
        Zeny: 100
        RequiredItems:
          - Index: 0
            Item: Jellopy
            Amount: 1
";

    static string S(YamlMappingNode n,string key,string fallback="") =>
        n.Children.TryGetValue(new YamlScalarNode(key),out var v)?v.ToString():fallback;
    static uint U(YamlMappingNode n,string key,uint fallback=0) =>
        uint.TryParse(S(n,key),out var v)?v:fallback;
    static bool B(YamlMappingNode n,string key,bool fallback=true) =>
        bool.TryParse(S(n,key),out var v)?v:fallback;

    static bool TryDate(string value,out DateTime date){
        string[] formats=["yyyy-MM-dd HH:mm:ss","yyyy-MM-dd HH:mm"];
        return DateTime.TryParseExact(value,formats,System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AllowWhiteSpaces|System.Globalization.DateTimeStyles.AssumeLocal,out date);
    }

    static void Set(YamlMappingNode node,string key,string? value,bool removeWhenBlank=false){
        var existing=node.Children.Keys.OfType<YamlScalarNode>().FirstOrDefault(k=>string.Equals(k.Value,key,StringComparison.Ordinal));
        if(removeWhenBlank&&string.IsNullOrWhiteSpace(value)){
            if(existing!=null)node.Children.Remove(existing);
            return;
        }
        var k=existing??new YamlScalarNode(key);
        node.Children[k]=new YamlScalarNode(value??"");
    }

    bool TryDocument(out YamlStream yaml,out YamlSequenceNode body){
        yaml=new YamlStream();
        body=null!;
        try{
            using var sr=new StringReader(Editor.Text);
            yaml.Load(sr);
            if(yaml.Documents.Count==0||yaml.Documents[0].RootNode is not YamlMappingNode root||
               !root.Children.TryGetValue(new YamlScalarNode("Body"),out var bodyNode)||bodyNode is not YamlSequenceNode seq)return false;
            body=seq;return true;
        }catch{return false;}
    }

    (YamlMappingNode Shop,YamlMappingNode Item)? Find(Selection target,YamlSequenceNode body){
        foreach(var shop in body.OfType<YamlMappingNode>()){
            if(!S(shop,"Name").Equals(target.Shop,StringComparison.Ordinal))continue;
            if(!shop.Children.TryGetValue(new YamlScalarNode("Items"),out var itemsNode)||itemsNode is not YamlSequenceNode items)continue;
            foreach(var item in items.OfType<YamlMappingNode>())
                if((int)U(item,"Index",uint.MaxValue)==target.Index)return(shop,item);
        }
        return null;
    }

    YamlMappingNode? FindShop(string name,YamlSequenceNode body)=>
        body.OfType<YamlMappingNode>().FirstOrDefault(x=>S(x,"Name").Equals(name,StringComparison.Ordinal));

    Selection? CurrentSelection()=>Summary.SelectedItems.Count>0?Summary.SelectedItems[0].Tag as Selection:null;

    (ServerItemRef? Server,Item? Client) ResolveItem(string aegis){
        if(string.IsNullOrWhiteSpace(aegis))return(null,null);
        Ctx.Catalog.ByAegis.TryGetValue(aegis,out var server);
        Item? client=null;
        if(server!=null)Ctx.Catalog.ClientItems.TryGetValue(server.Id,out client);
        if(client==null)
            client=Ctx.Catalog.ClientItems.Values.FirstOrDefault(x=>x.Resource.Equals(aegis,StringComparison.OrdinalIgnoreCase));
        return(server,client);
    }

    static string RawField(string raw,string key,string fallback="-"){
        var m=Regex.Match(raw,$@"(?mi)^\s*{Regex.Escape(key)}:\s*(.+?)\s*$");
        return m.Success?m.Groups[1].Value.Trim().Trim('"','\''):fallback;
    }

    static string RawLocations(string raw){
        var lines=raw.Replace("\r","").Split('\n');
        bool inside=false;var result=new List<string>();
        foreach(var line in lines){
            if(Regex.IsMatch(line,@"^\s+Locations:\s*$")){inside=true;continue;}
            if(!inside)continue;
            if(Regex.IsMatch(line,@"^\s{4}\S")&&!Regex.IsMatch(line,@"^\s{6,}"))break;
            var m=Regex.Match(line,@"^\s+([^:#]+):\s*(true|yes|1)\s*$",RegexOptions.IgnoreCase);
            if(m.Success)result.Add(m.Groups[1].Value.Trim());
        }
        return result.Count==0?"-":string.Join(", ",result);
    }

    async Task RefreshItemPreviewAsync(string aegis,bool equip=true){
        previewCancel?.Cancel();previewCancel?.Dispose();previewCancel=new CancellationTokenSource();
        var token=previewCancel.Token;
        var resolved=ResolveItem(aegis);
        var server=resolved.Server;var client=resolved.Client;
        int id=server?.Id??client?.Id??0;
        string name=server?.Name??client?.Name??aegis;
        string type=server==null?"-":RawField(server.Raw,"Type");
        string loc=server==null?"-":RawLocations(server.Raw);
        string view=server==null?"-":RawField(server.Raw,"View");
        string resource=client?.Resource??"-";

        itemPreviewName.Text=name;
        itemPreviewMeta.Text=$"Item ID: {(id==0?"-":id)}\r\nAegis: {(server?.AegisName??aegis)}\r\nType: {type}\r\nLoc: {loc}\r\nResource: {resource}";
        itemPreviewSource.Text=server==null?"Server source: -":"Server source: "+Path.GetFileName(server.Source);
        equipPreviewInfo.Text=equip
            ? $"View: {view}   •   Resource: {resource}\r\nEquip composite area"
            : $"Material preview • Resource: {resource}";
        equipPreview.Tag=new[]{name,view,resource};
        equipPreview.Invalidate();

        var old=itemPreviewIcon.Image;itemPreviewIcon.Image=null;old?.Dispose();
        if(client==null||Ctx.Catalog.Assets==null)return;
        try{
            var assets=Ctx.Catalog.Assets;
            var bmp=await Task.Run(()=>{
                var data=assets.ItemImage(client,false,token);
                using var ms=new MemoryStream(data.Bytes);
                using var source=Image.FromStream(ms);
                var copy=new Bitmap(source);copy.MakeTransparent(Color.Magenta);
                return copy;
            },token);
            if(token.IsCancellationRequested){bmp.Dispose();return;}
            old=itemPreviewIcon.Image;itemPreviewIcon.Image=bmp;old?.Dispose();
            itemPreviewSource.Text+="\r\nIcon: GRF resolved";
            equipPreview.Invalidate();
        }catch(OperationCanceledException){}
        catch(Exception ex){if(!token.IsCancellationRequested)itemPreviewSource.Text+="\r\nIcon: "+ex.Message;}
    }

    void DrawEquipPreview(object? sender,PaintEventArgs e){
        var r=equipPreview.ClientRectangle;
        using var pen=new Pen(Color.Gray,2);
        using var brush=new SolidBrush(Color.FromArgb(205,205,214));
        int cx=r.Width/2;
        int head=Math.Max(34,Math.Min(58,r.Height/5));
        var headRect=new Rectangle(cx-head/2,22,head,head);
        e.Graphics.FillEllipse(brush,headRect);e.Graphics.DrawEllipse(pen,headRect);
        var body=new Rectangle(cx-42,22+head+12,84,Math.Max(70,r.Height-150));
        e.Graphics.FillRectangle(brush,body);e.Graphics.DrawRectangle(pen,body);
        if(itemPreviewIcon.Image!=null){
            int size=Math.Min(72,Math.Max(48,head+12));
            e.Graphics.DrawImage(itemPreviewIcon.Image,new Rectangle(cx-size/2,4,size,size));
        }
        string title=equipPreview.Tag is string[] meta&&meta.Length>0?meta[0]:"Select an equip item";
        using var f=new Font("Segoe UI",9,FontStyle.Bold);
        var sz=e.Graphics.MeasureString(title,f);
        e.Graphics.DrawString(title,f,SystemBrushes.ControlText,cx-sz.Width/2,Math.Max(8,r.Height-86));
    }

    void CommitYaml(YamlStream yaml,Selection? selectItem=null,int? selectRequirement=null){
        using var sw=new StringWriter();
        yaml.Save(sw,false);
        Editor.Text=sw.ToString();
        BuildSummary(Editor.Text);
        if(selectItem!=null){
            foreach(ListViewItem row in Summary.Items)
                if(row.Tag is Selection s&&s==selectItem){row.Selected=true;row.EnsureVisible();break;}
            if(selectRequirement.HasValue){
                foreach(ListViewItem row in requirements.Items)
                    if(row.Tag is RequirementSelection rs&&rs.Shop==selectItem.Shop&&rs.ItemIndex==selectItem.Index&&rs.RequirementIndex==selectRequirement.Value){
                        row.Selected=true;row.EnsureVisible();break;
                    }
            }
        }
    }

    void LoadSelection(){
        if(Summary.SelectedItems.Count==0||Summary.SelectedItems[0].Tag is not Selection target)return;
        if(!TryDocument(out _,out var body))return;
        var found=Find(target,body);if(found==null)return;
        var (shopNode,itemNode)=found.Value;
        selected.Text=$"{target.Shop} • Index {target.Index} • {S(itemNode,"Item")}";
        rewardItem.Text=S(itemNode,"Item");
        zeny.Value=Math.Min(zeny.Maximum,U(itemNode,"Zeny"));
        eventId.Text=S(shopNode,"EventId",target.Shop);
        start.Text=S(shopNode,"Start");
        end.Text=S(shopNode,"End");
        stock.Value=Math.Min(stock.Maximum,U(itemNode,"Stock"));
        successRate.Value=Math.Min(successRate.Maximum,U(itemNode,"SuccessRate",10000));
        accountLimit.Value=Math.Min(accountLimit.Maximum,U(itemNode,"AccountLimit"));
        characterLimit.Value=Math.Min(characterLimit.Maximum,U(itemNode,"CharacterLimit"));
        consumeOnFail.Checked=B(itemNode,"ConsumeOnFail",true);

        requirements.Items.Clear();
        if(itemNode.Children.TryGetValue(new YamlScalarNode("RequiredItems"),out var reqNode)&&reqNode is YamlSequenceNode reqs){
            foreach(var req in reqs.OfType<YamlMappingNode>()){
                uint rawReqIndex=U(req,"Index",uint.MaxValue);
                int reqIndex=rawReqIndex==uint.MaxValue?-1:(int)rawReqIndex;
                string reqAegis=S(req,"Item");
                var resolved=ResolveItem(reqAegis);
                int reqId=resolved.Server?.Id??resolved.Client?.Id??0;
                string reqName=resolved.Server?.Name??resolved.Client?.Name??"";
                var row=new ListViewItem([
                    reqIndex<0?"?":reqIndex.ToString(),
                    reqId==0?"-":reqId.ToString(),
                    reqAegis,
                    reqName,
                    U(req,"Amount",1).ToString(),
                    S(req,"Refine","-1")
                ]){Tag=new RequirementSelection(target.Shop,target.Index,reqIndex)};
                requirements.Items.Add(row);
            }
        }
        requirementItem.Text="";
        requirementAmount.Value=1;
        requirementRefine.Value=-1;
        _=RefreshItemPreviewAsync(rewardItem.Text,true);
    }

    void LoadRequirementSelection(){
        if(requirements.SelectedItems.Count==0||requirements.SelectedItems[0].Tag is not RequirementSelection rs)return;
        var itemSelection=new Selection(rs.Shop,rs.ItemIndex);
        if(!TryDocument(out _,out var body))return;
        var found=Find(itemSelection,body);if(found==null)return;
        var (_,itemNode)=found.Value;
        if(!itemNode.Children.TryGetValue(new YamlScalarNode("RequiredItems"),out var reqNode)||reqNode is not YamlSequenceNode reqs)return;
        var req=reqs.OfType<YamlMappingNode>().FirstOrDefault(x=>(int)U(x,"Index",uint.MaxValue)==rs.RequirementIndex);
        if(req==null)return;
        requirementItem.Text=S(req,"Item");
        requirementAmount.Value=Math.Min(requirementAmount.Maximum,Math.Max(requirementAmount.Minimum,U(req,"Amount",1)));
        int refine=int.TryParse(S(req,"Refine","-1"),out var rv)?rv:-1;
        requirementRefine.Value=Math.Min(requirementRefine.Maximum,Math.Max(requirementRefine.Minimum,refine));
        _=RefreshItemPreviewAsync(requirementItem.Text,false);
    }

    (YamlMappingNode Item,YamlSequenceNode Requirements,YamlMappingNode Requirement)? FindRequirement(RequirementSelection target,YamlSequenceNode body){
        var found=Find(new Selection(target.Shop,target.ItemIndex),body);if(found==null)return null;
        var (_,item)=found.Value;
        if(!item.Children.TryGetValue(new YamlScalarNode("RequiredItems"),out var reqNode)||reqNode is not YamlSequenceNode reqs)return null;
        var req=reqs.OfType<YamlMappingNode>().FirstOrDefault(x=>(int)U(x,"Index",uint.MaxValue)==target.RequirementIndex);
        return req==null?null:(item,reqs,req);
    }

    void AddReward(){
        if(!TryDocument(out var yaml,out var body)){MessageBox.Show("YAML ไม่ถูกต้อง","Barter Editor");return;}
        string? shopName=CurrentSelection()?.Shop;
        var shops=body.OfType<YamlMappingNode>().ToList();
        if(string.IsNullOrWhiteSpace(shopName)){
            if(shops.Count!=1){MessageBox.Show("เลือกรายการสินค้าในร้านที่ต้องการเพิ่มก่อน","Barter Editor");return;}
            shopName=S(shops[0],"Name");
        }
        var shop=FindShop(shopName!,body);if(shop==null)return;
        var item=BarterYamlOps.AddItem(shop);
        var target=new Selection(shopName!,(int)BarterYamlOps.U(item,"Index"));
        CommitYaml(yaml,target);
        v2Status.Text="Added reward item • แก้ AegisName/สูตร แล้วกด Apply";
    }

    void CloneReward(){
        var target=CurrentSelection();if(target==null){MessageBox.Show("เลือกรายการที่จะ Clone ก่อน","Barter Editor");return;}
        if(!TryDocument(out var yaml,out var body))return;
        var found=Find(target,body);if(found==null)return;
        var (shop,item)=found.Value;
        var clone=BarterYamlOps.CloneItem(shop,item);
        var next=new Selection(target.Shop,(int)BarterYamlOps.U(clone,"Index"));
        CommitYaml(yaml,next);
        v2Status.Text="Cloned reward + crafting recipe";
    }

    void RemoveReward(){
        var target=CurrentSelection();if(target==null){MessageBox.Show("เลือกรายการที่จะลบก่อน","Barter Editor");return;}
        if(MessageBox.Show($"ลบ {target.Shop} Index {target.Index} และสูตรคราฟทั้งหมด?","Delete Barter Item",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;
        if(!TryDocument(out var yaml,out var body))return;
        var found=Find(target,body);if(found==null)return;
        var (shop,item)=found.Value;
        BarterYamlOps.Items(shop).Children.Remove(item);
        CommitYaml(yaml);
        requirements.Items.Clear();selected.Text="Item deleted • กด Save เพื่อเขียนไฟล์จริง";
        v2Status.Text="Deleted from YAML buffer • Item indices เดิมไม่ถูก renumber เพื่อรักษา SQL stock mapping";
    }

    void MoveReward(int delta){
        var target=CurrentSelection();if(target==null)return;
        if(!TryDocument(out var yaml,out var body))return;
        var found=Find(target,body);if(found==null)return;
        var (shop,item)=found.Value;
        if(BarterYamlOps.Move(BarterYamlOps.Items(shop),item,delta,false))CommitYaml(yaml,target);
    }

    void AddRequirement(){
        var target=CurrentSelection();if(target==null){MessageBox.Show("เลือก Reward Item ก่อนเพิ่มวัตถุดิบ","Craft Recipe");return;}
        if(!TryDocument(out var yaml,out var body))return;
        var found=Find(target,body);if(found==null)return;
        var (_,item)=found.Value;
        string aegis=string.IsNullOrWhiteSpace(requirementItem.Text)?"Jellopy":requirementItem.Text.Trim();
        var req=BarterYamlOps.AddRequirement(item,aegis,(uint)requirementAmount.Value,(int)requirementRefine.Value);
        int reqIndex=(int)BarterYamlOps.U(req,"Index");
        CommitYaml(yaml,target,reqIndex);
        v2Status.Text="Added crafting material";
    }

    void ApplyRequirement(){
        if(requirements.SelectedItems.Count==0||requirements.SelectedItems[0].Tag is not RequirementSelection target){
            MessageBox.Show("เลือกวัตถุดิบในตารางก่อน","Craft Recipe");return;
        }
        if(string.IsNullOrWhiteSpace(requirementItem.Text)){MessageBox.Show("Required Item ห้ามว่าง","Craft Recipe");return;}
        if(!TryDocument(out var yaml,out var body))return;
        var found=FindRequirement(target,body);if(found==null)return;
        var (_,_,req)=found.Value;
        Set(req,"Item",requirementItem.Text.Trim());
        Set(req,"Amount",((uint)requirementAmount.Value).ToString());
        Set(req,"Refine",requirementRefine.Value<0?"":((int)requirementRefine.Value).ToString(),true);
        CommitYaml(yaml,new Selection(target.Shop,target.ItemIndex),target.RequirementIndex);
        v2Status.Text="Updated crafting material";
    }

    void RemoveRequirement(){
        if(requirements.SelectedItems.Count==0||requirements.SelectedItems[0].Tag is not RequirementSelection target)return;
        if(!TryDocument(out var yaml,out var body))return;
        var found=FindRequirement(target,body);if(found==null)return;
        var (_,reqs,req)=found.Value;
        reqs.Children.Remove(req);
        BarterYamlOps.Reindex(reqs);
        CommitYaml(yaml,new Selection(target.Shop,target.ItemIndex));
        v2Status.Text="Removed crafting material";
    }

    void MoveRequirement(int delta){
        if(requirements.SelectedItems.Count==0||requirements.SelectedItems[0].Tag is not RequirementSelection target)return;
        if(!TryDocument(out var yaml,out var body))return;
        var found=FindRequirement(target,body);if(found==null)return;
        var (_,reqs,req)=found.Value;
        if(BarterYamlOps.Move(reqs,req,delta,true)){
            int newIndex=(int)BarterYamlOps.U(req,"Index");
            CommitYaml(yaml,new Selection(target.Shop,target.ItemIndex),newIndex);
        }
    }

    void ApplyV2(){
        if(Summary.SelectedItems.Count==0||Summary.SelectedItems[0].Tag is not Selection target){
            MessageBox.Show("เลือกรายการ Item ในตารางด้านล่างก่อน","Hot Week V2");return;
        }
        if(!string.IsNullOrWhiteSpace(start.Text)&&!TryDate(start.Text,out var startAt)){
            MessageBox.Show("Start ต้องเป็น YYYY-MM-DD HH:MM[:SS]","Hot Week V2");return;
        }
        if(!string.IsNullOrWhiteSpace(end.Text)&&!TryDate(end.Text,out var endAt)){
            MessageBox.Show("End ต้องเป็น YYYY-MM-DD HH:MM[:SS]","Hot Week V2");return;
        }
        if(!string.IsNullOrWhiteSpace(start.Text)&&!string.IsNullOrWhiteSpace(end.Text)&&
           TryDate(start.Text,out startAt)&&TryDate(end.Text,out endAt)&&startAt>=endAt){
            MessageBox.Show("Start ต้องมาก่อน End","Hot Week V2");return;
        }
        if(!TryDocument(out var yaml,out var body)){MessageBox.Show("YAML ไม่ถูกต้อง","Hot Week V2");return;}
        var found=Find(target,body);if(found==null)return;
        var (shopNode,itemNode)=found.Value;
        if(string.IsNullOrWhiteSpace(rewardItem.Text)){MessageBox.Show("Reward Item / AegisName ห้ามว่าง","Barter Editor");return;}

        Set(itemNode,"Item",rewardItem.Text.Trim());
        Set(itemNode,"Zeny",((uint)zeny.Value).ToString());
        Set(shopNode,"EventId",string.IsNullOrWhiteSpace(eventId.Text)?target.Shop:eventId.Text.Trim());
        Set(shopNode,"Start",start.Text.Trim(),true);
        Set(shopNode,"End",end.Text.Trim(),true);
        Set(itemNode,"Stock",((uint)stock.Value).ToString());
        Set(itemNode,"SuccessRate",((uint)successRate.Value).ToString());
        Set(itemNode,"ConsumeOnFail",consumeOnFail.Checked?"true":"false");
        Set(itemNode,"AccountLimit",((uint)accountLimit.Value).ToString());
        Set(itemNode,"CharacterLimit",((uint)characterLimit.Value).ToString());

        CommitYaml(yaml,target);
        v2Status.Text="Applied Reward + Hot Week fields to YAML • กด Save เพื่อเขียนไฟล์จริง (มี backup ก่อน save)";
    }

    void NewHotWeekTemplate(){
        CurrentFile="";
        Editor.Text=TemplateText();
        BuildSummary(Editor.Text);
        v2Status.Text="Hot Week template ready • ตรวจ Item/เวลา แล้วกด Save";
    }

    protected override void BuildSummary(string text){
        Summary.Items.Clear();
        var issues=new List<string>();
        try{
            using var sr=new StringReader(text);
            var y=new YamlStream();y.Load(sr);
            if(y.Documents.Count==0||y.Documents[0].RootNode is not YamlMappingNode root||
               !root.Children.TryGetValue(new YamlScalarNode("Body"),out var body)||body is not YamlSequenceNode seq){
                v2Status.Text="INVALID • missing Body";return;
            }

            foreach(var shop in seq.OfType<YamlMappingNode>()){
                string name=S(shop,"Name");
                string map=S(shop,"Map");
                string evt=S(shop,"EventId",name);
                string starts=S(shop,"Start");
                string ends=S(shop,"End");

                if(evt.Length>64)issues.Add($"{name}: EventId > 64 chars");
                DateTime? startAt=null,endAt=null;
                if(starts.Length>0){if(TryDate(starts,out var d))startAt=d;else issues.Add($"{name}: invalid Start");}
                if(ends.Length>0){if(TryDate(ends,out var d))endAt=d;else issues.Add($"{name}: invalid End");}
                if(startAt.HasValue&&endAt.HasValue&&startAt.Value>=endAt.Value)issues.Add($"{name}: Start >= End");

                string window=(starts.Length==0&&ends.Length==0)?"Always":$"{starts} → {ends}";
                if(!shop.Children.TryGetValue(new YamlScalarNode("Items"),out var itemsNode)||itemsNode is not YamlSequenceNode items)continue;
                var seen=new HashSet<uint>();
                foreach(var item in items.OfType<YamlMappingNode>()){
                    uint index=U(item,"Index",uint.MaxValue);
                    string aegis=S(item,"Item");
                    uint st=U(item,"Stock");
                    uint rate=U(item,"SuccessRate",10000);
                    uint acc=U(item,"AccountLimit");
                    uint chr=U(item,"CharacterLimit");
                    bool consume=B(item,"ConsumeOnFail",true);

                    if(index==uint.MaxValue)issues.Add($"{name}: item missing Index");
                    else if(!seen.Add(index))issues.Add($"{name}: duplicate Index {index}");
                    if(st>65535)issues.Add($"{name}[{index}]: Stock > 65535 (legacy barter table limit)");
                    if(rate>10000)issues.Add($"{name}[{index}]: SuccessRate > 10000");
                    if(rate<10000&&!consume)issues.Add($"{name}[{index}]: free retry enabled (ConsumeOnFail=false)");
                    if((acc>0||chr>0)&&string.IsNullOrWhiteSpace(evt))issues.Add($"{name}[{index}]: limits require EventId");
                    if(string.IsNullOrWhiteSpace(aegis))issues.Add($"{name}[{index}]: reward Item is empty");
                    else if(Ctx.Catalog.ByAegis.Count>0&&!Ctx.Catalog.ByAegis.ContainsKey(aegis))issues.Add($"{name}[{index}]: unknown Item {aegis}");

                    if(item.Children.TryGetValue(new YamlScalarNode("RequiredItems"),out var reqNode)&&reqNode is YamlSequenceNode reqs){
                        var reqSeen=new HashSet<uint>();
                        foreach(var req in reqs.OfType<YamlMappingNode>()){
                            uint reqIndex=U(req,"Index",uint.MaxValue);
                            string reqAegis=S(req,"Item");
                            uint amount=U(req,"Amount",0);
                            if(reqIndex==uint.MaxValue)issues.Add($"{name}[{index}]: requirement missing Index");
                            else if(!reqSeen.Add(reqIndex))issues.Add($"{name}[{index}]: duplicate requirement Index {reqIndex}");
                            if(string.IsNullOrWhiteSpace(reqAegis))issues.Add($"{name}[{index}][{reqIndex}]: requirement Item is empty");
                            else if(Ctx.Catalog.ByAegis.Count>0&&!Ctx.Catalog.ByAegis.ContainsKey(reqAegis))
                                issues.Add($"{name}[{index}]: unknown requirement {reqAegis}");
                            if(amount==0)issues.Add($"{name}[{index}][{reqIndex}]: Amount must be >= 1");
                        }
                    }

                    var resolved=ResolveItem(aegis);
                    int itemId=resolved.Server?.Id??resolved.Client?.Id??0;
                    string itemName=resolved.Server?.Name??resolved.Client?.Name??"";
                    uint price=U(item,"Zeny");
                    var row=new ListViewItem([
                        name,
                        index==uint.MaxValue?"?":index.ToString(),
                        itemId==0?"-":itemId.ToString(),
                        aegis,
                        itemName,
                        st==0?"∞":st.ToString(),
                        $"{rate/100.0:0.##}%",
                        price.ToString()
                    ]){Tag=new Selection(name,(int)index)};
                    Summary.Items.Add(row);
                }
            }
            v2Status.Text=issues.Count==0
                ? $"V2 VALID • {Summary.Items.Count:N0} items • Stock 0 = unlimited"
                : $"V2 WARNINGS {issues.Count:N0}\r\n"+string.Join("\r\n",issues.Take(8));
        }catch(Exception ex){
            v2Status.Text="INVALID YAML • "+ex.Message;
        }
    }
}

public sealed class PetEditorControl : YamlEditorControl {
    public PetEditorControl(EditorContext ctx):base(ctx,"Pet Editor"){Summary.Columns.Add("Mob",150);Summary.Columns.Add("Egg",150);Summary.Columns.Add("Food",150);Summary.Columns.Add("Capture",80);RefreshFiles();}
    protected override IEnumerable<string> Discover()=>Ctx.Catalog.PetFiles;
    protected override string TemplateText()=>@"Header:
  Type: PET_DB
  Version: 1

Body:
  - Mob: PORING
    TameItem: Unripe_Apple
    EggItem: Poring_Egg
    FoodItem: Apple_Juice
    Fullness: 3
    IntimacyFed: 50
    CaptureRate: 2000
";
    protected override void BuildSummary(string text){Summary.Items.Clear();try{using var sr=new StringReader(text);var y=new YamlStream();y.Load(sr);if(y.Documents[0].RootNode is not YamlMappingNode root||!root.Children.TryGetValue(new YamlScalarNode("Body"),out var body)||body is not YamlSequenceNode seq)return;foreach(var n in seq.OfType<YamlMappingNode>()){string S(string k)=>n.Children.TryGetValue(new YamlScalarNode(k),out var v)?v.ToString():"";Summary.Items.Add(new ListViewItem([S("Mob"),S("EggItem"),S("FoodItem"),S("CaptureRate")]));}}catch{}}
}

public sealed class NpcEditorControl : UserControl {
    readonly EditorContext ctx;readonly ListBox files=new(){Dock=DockStyle.Fill,HorizontalScrollbar=true};readonly TextBox editor=new(){Dock=DockStyle.Fill,Multiline=true,AcceptsTab=true,ScrollBars=ScrollBars.Both,WordWrap=false,Font=new Font("Consolas",10)};readonly TextBox search=new(){Width=240,PlaceholderText="filter filename"};readonly Label status=new(){Dock=DockStyle.Bottom,Height=26};string current="";
    public NpcEditorControl(EditorContext c){ctx=c;Dock=DockStyle.Fill;var top=new FlowLayoutPanel{Dock=DockStyle.Top,Height=42,WrapContents=false};top.Controls.Add(search);Btn(top,"Refresh",RefreshFileList);Btn(top,"Load",LoadSelectedNpc);Btn(top,"Check",Check);Btn(top,"Save",Save);Btn(top,"New NPC",Template);var split=new SplitContainer{Dock=DockStyle.Fill,SplitterDistance=310};split.Panel1.Controls.Add(files);split.Panel2.Controls.Add(editor);Controls.Add(split);Controls.Add(top);Controls.Add(status);search.TextChanged+=(_,_)=>RefreshFileList();files.DoubleClick+=(_,_)=>LoadSelectedNpc();RefreshFileList();}
    static void Btn(FlowLayoutPanel p,string t,Action a){var b=new Button{Text=t,AutoSize=true,Height=30};b.Click+=(_,_)=>a();p.Controls.Add(b);}
    void RefreshFileList(){files.Items.Clear();string q=search.Text.Trim();foreach(var f in ctx.Catalog.NpcFiles.Where(x=>q.Length==0||x.Contains(q,StringComparison.OrdinalIgnoreCase)))files.Items.Add(f);}
    void LoadSelectedNpc(){if(files.SelectedItem is not string f)return;current=f;editor.Text=File.ReadAllText(f);Check();}
    void Check(){string t=editor.Text;int braces=t.Count(c=>c=='{')-t.Count(c=>c=='}');int refs=Regex.Matches(t,@"\b(?:getitem|delitem|countitem)\s+\d+",RegexOptions.IgnoreCase).Count;status.Text=braces==0?$"CHECK OK • numeric item refs {refs:N0} • {current}":$"CHECK WARNING • brace balance {braces} • refs {refs:N0}";}
    void Save(){if(string.IsNullOrWhiteSpace(current)){using var d=new SaveFileDialog{Filter="NPC script|*.txt",InitialDirectory=Path.Combine(ctx.Settings.ServerRoot,"npc")};if(d.ShowDialog()!=DialogResult.OK)return;current=d.FileName;}Check();if(status.Text.StartsWith("CHECK WARNING")){MessageBox.Show("แก้ brace balance ก่อน Save");return;}SafeProjectFile.Write(current,editor.Text);status.Text="SAVED + BACKUP • "+current;ctx.Log("NPC saved "+current);ctx.Catalog.Discover();RefreshFileList();}
    void Template(){current="";editor.Text="morocc03,150,100,4\tscript\tMaven NPC\t4_F_KAFRA6,{\r\n\tmes \"[ Maven NPC ]\";\r\n\tmes \"Hello.\";\r\n\tclose;\r\n}\r\n";Check();}
}

public sealed class ProjectValidatorControl : UserControl {
    readonly EditorContext ctx;readonly TextBox report=new(){Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Both,WordWrap=false,Font=new Font("Consolas",10)};readonly Label status=new(){Dock=DockStyle.Bottom,Height=26};
    public ProjectValidatorControl(EditorContext c){ctx=c;Dock=DockStyle.Fill;var top=new FlowLayoutPanel{Dock=DockStyle.Top,Height=42};var scan=new Button{Text="Scan Project",AutoSize=true};scan.Click+=async(_,_)=>await Scan();var export=new Button{Text="Export Report",AutoSize=true};export.Click+=(_,_)=>Export();top.Controls.Add(scan);top.Controls.Add(export);Controls.Add(report);Controls.Add(top);Controls.Add(status);}
    async Task Scan(){status.Text="Scanning...";var text=await Task.Run(()=>BuildReport(ctx.Catalog));report.Text=text;status.Text="Scan complete";}
    public static string BuildReport(ProjectCatalog c){
        var sb=new StringBuilder();
        sb.AppendLine("MAVEN RO Studio • Project Validation");
        sb.AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));sb.AppendLine();

        var ci=c.ClientItems;
        var serverIds=(c.ServerItems.Count>0?c.ServerItems.Keys:c.ById.Keys).ToHashSet();
        string ServerName(int itemId)=>c.ServerItems.TryGetValue(itemId,out var loaded)?loaded.Name:c.ById.TryGetValue(itemId,out var indexed)?indexed.Name:"";
        var clientOnly=ci.Keys.Where(id=>!serverIds.Contains(id)).ToList();
        var serverOnly=serverIds.Where(id=>!ci.ContainsKey(id)).ToList();
        var mismatched=ci.Keys.Where(serverIds.Contains).Where(id=>{
            string sn=ServerName(id);
            return sn.Length>0&&!ci[id].Name.Equals(sn,StringComparison.OrdinalIgnoreCase);
        }).ToList();
        var missingIcons=ci.Values.Where(x=>!c.HasItemIcon(x)).Select(x=>x.Id).ToList();
        var dupResources=ci.Values.Where(x=>!string.IsNullOrWhiteSpace(x.Resource))
            .GroupBy(x=>x.Resource,StringComparer.OrdinalIgnoreCase).Where(g=>g.Count()>1).ToList();

        sb.AppendLine($"Client Items : {ci.Count:N0}");
        sb.AppendLine($"Server Items : {serverIds.Count:N0}");
        sb.AppendLine($"Client Only  : {clientOnly.Count:N0}");
        sb.AppendLine($"Server Only  : {serverOnly.Count:N0}");
        sb.AppendLine($"Name mismatch: {mismatched.Count:N0}");
        sb.AppendLine($"Missing icons: {missingIcons.Count:N0}");
        sb.AppendLine($"Duplicate resources: {dupResources.Count:N0}");
        sb.AppendLine($"GRF winning resources: {c.Assets?.Entries.Count??0:N0}");
        sb.AppendLine($"Barter DB files: {c.BarterFiles.Count:N0}");
        sb.AppendLine($"Pet DB files: {c.PetFiles.Count:N0}");
        sb.AppendLine($"NPC files: {c.NpcFiles.Count:N0}");
        sb.AppendLine();

        int badYaml=0,unknownAegis=0,hotWeekIssues=0;
        foreach(var f in c.BarterFiles) {
            try {
                string text=File.ReadAllText(f);
                SafeProjectFile.ValidateYaml(text);
                unknownAegis+=CountUnknownAegis(text,f,sb,c);
                hotWeekIssues+=CountHotWeekIssues(text,f,sb);
            } catch(Exception ex) {
                badYaml++;sb.AppendLine("YAML ERROR "+f+" :: "+ex.Message);
            }
        }
        foreach(var f in c.PetFiles) {
            try {
                string text=File.ReadAllText(f);
                SafeProjectFile.ValidateYaml(text);
                unknownAegis+=CountUnknownAegis(text,f,sb,c);
            } catch(Exception ex) {
                badYaml++;sb.AppendLine("YAML ERROR "+f+" :: "+ex.Message);
            }
        }

        int unknownNpc=0;
        var rx=new Regex(@"\b(?:getitem|delitem|countitem)\s+(\d+)",RegexOptions.IgnoreCase);
        foreach(var f in c.NpcFiles) {
            string t;try{t=File.ReadAllText(f);}catch{continue;}
            foreach(Match m in rx.Matches(t)) {
                if(!int.TryParse(m.Groups[1].Value,out var itemId))continue;
                if(ci.ContainsKey(itemId)||serverIds.Contains(itemId))continue;
                unknownNpc++;
                if(unknownNpc<=100)sb.AppendLine($"NPC UNKNOWN ITEM {itemId} :: {f}");
            }
        }

        sb.AppendLine();
        sb.AppendLine($"Invalid YAML files: {badYaml:N0}");
        sb.AppendLine($"Hot Week / Secure V2 issues: {hotWeekIssues:N0}");
        sb.AppendLine($"Unknown Barter/Pet Aegis refs: {unknownAegis:N0}");
        sb.AppendLine($"Unknown numeric NPC item refs: {unknownNpc:N0}");
        if(missingIcons.Count>0)sb.AppendLine("Missing icon sample: "+string.Join(", ",missingIcons.Take(100)));
        if(clientOnly.Count>0)sb.AppendLine("Client-only sample: "+string.Join(", ",clientOnly.Take(100)));
        if(serverOnly.Count>0)sb.AppendLine("Server-only sample: "+string.Join(", ",serverOnly.Take(100)));
        return sb.ToString();
    }

    static int CountHotWeekIssues(string text,string file,StringBuilder report) {
        using var sr=new StringReader(text);var yaml=new YamlStream();yaml.Load(sr);
        if(yaml.Documents.Count==0||yaml.Documents[0].RootNode is not YamlMappingNode root||
           !root.Children.TryGetValue(new YamlScalarNode("Body"),out var bodyNode)||bodyNode is not YamlSequenceNode body)return 0;
        int count=0;
        void Issue(string message){count++;if(count<=100)report.AppendLine("BARTER V2 "+message+" :: "+file);}
        string S(YamlMappingNode n,string k)=>n.Children.TryGetValue(new YamlScalarNode(k),out var v)?v.ToString():"";
        uint U(YamlMappingNode n,string k,uint fallback=0)=>uint.TryParse(S(n,k),out var v)?v:fallback;
        bool Bool(YamlMappingNode n,string k,bool fallback=true)=>bool.TryParse(S(n,k),out var v)?v:fallback;
        bool Date(string value,out DateTime dt)=>DateTime.TryParseExact(value,
            new[]{"yyyy-MM-dd HH:mm:ss","yyyy-MM-dd HH:mm"},System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AllowWhiteSpaces|System.Globalization.DateTimeStyles.AssumeLocal,out dt);

        foreach(var shop in body.OfType<YamlMappingNode>()){
            string name=S(shop,"Name");
            string start=S(shop,"Start"),end=S(shop,"End");
            DateTime? startAt=null,endAt=null;
            if(start.Length>0){if(Date(start,out var d))startAt=d;else Issue($"{name}: invalid Start");}
            if(end.Length>0){if(Date(end,out var d))endAt=d;else Issue($"{name}: invalid End");}
            if(startAt.HasValue&&endAt.HasValue&&startAt.Value>=endAt.Value)Issue($"{name}: Start >= End");
            bool hasEvent=!string.IsNullOrWhiteSpace(S(shop,"EventId"));

            if(!shop.Children.TryGetValue(new YamlScalarNode("Items"),out var itemsNode)||itemsNode is not YamlSequenceNode items)continue;
            var seen=new HashSet<uint>();
            foreach(var item in items.OfType<YamlMappingNode>()){
                uint index=U(item,"Index",uint.MaxValue),stock=U(item,"Stock"),rate=U(item,"SuccessRate",10000);
                uint acc=U(item,"AccountLimit"),chr=U(item,"CharacterLimit");
                if(index==uint.MaxValue)Issue($"{name}: missing Index");
                else if(!seen.Add(index))Issue($"{name}: duplicate Index {index}");
                if(stock>65535)Issue($"{name}[{index}]: Stock > 65535");
                if(rate>10000)Issue($"{name}[{index}]: SuccessRate > 10000");
                if(rate<10000&&!Bool(item,"ConsumeOnFail",true))Issue($"{name}[{index}]: ConsumeOnFail=false enables free retries");
                if((acc>0||chr>0)&&!hasEvent)Issue($"{name}[{index}]: set EventId so limits can be intentionally reset");
                if(string.IsNullOrWhiteSpace(S(item,"Item")))Issue($"{name}[{index}]: reward Item is empty");

                if(item.Children.TryGetValue(new YamlScalarNode("RequiredItems"),out var reqNode)&&reqNode is YamlSequenceNode reqs){
                    var reqSeen=new HashSet<uint>();
                    foreach(var req in reqs.OfType<YamlMappingNode>()){
                        uint reqIndex=U(req,"Index",uint.MaxValue);
                        if(reqIndex==uint.MaxValue)Issue($"{name}[{index}]: requirement missing Index");
                        else if(!reqSeen.Add(reqIndex))Issue($"{name}[{index}]: duplicate requirement Index {reqIndex}");
                        if(string.IsNullOrWhiteSpace(S(req,"Item")))Issue($"{name}[{index}][{reqIndex}]: requirement Item is empty");
                        if(U(req,"Amount",0)==0)Issue($"{name}[{index}][{reqIndex}]: Amount must be >= 1");
                    }
                }
            }
        }
        return count;
    }

    static int CountUnknownAegis(string text,string file,StringBuilder report,ProjectCatalog catalog) {
        using var sr=new StringReader(text);var yaml=new YamlStream();yaml.Load(sr);
        int count=0;
        foreach(var doc in yaml.Documents)Walk(doc.RootNode);
        return count;

        void Walk(YamlNode node) {
            if(node is YamlMappingNode map) {
                foreach(var kv in map.Children) {
                    string key=kv.Key.ToString();
                    if((key.Equals("Item",StringComparison.OrdinalIgnoreCase)
                        ||key.EndsWith("Item",StringComparison.OrdinalIgnoreCase))
                        && kv.Value is YamlScalarNode scalar
                        && !string.IsNullOrWhiteSpace(scalar.Value)
                        && !scalar.Value.All(char.IsDigit)
                        && !catalog.ByAegis.ContainsKey(scalar.Value)) {
                        count++;
                        if(count<=40)report.AppendLine($"UNKNOWN AEGIS {scalar.Value} :: {file}");
                    }
                    Walk(kv.Value);
                }
            } else if(node is YamlSequenceNode seq)foreach(var child in seq.Children)Walk(child);
        }
    }
    void Export(){using var d=new SaveFileDialog{Filter="Text|*.txt",FileName="maven-project-validation.txt"};if(d.ShowDialog()==DialogResult.OK)File.WriteAllText(d.FileName,report.Text,new UTF8Encoding(true));}
}
