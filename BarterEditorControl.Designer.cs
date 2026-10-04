#nullable disable
namespace MavenROStudio;

partial class BarterEditorControl
{
    private System.ComponentModel.IContainer barterComponents=null;
    private Button btnRewardAdd,btnRewardClone,btnRewardDelete,btnRewardUp,btnRewardDown;
    private Button btnReqAdd,btnReqDelete,btnReqUp,btnReqDown,btnReqApply;
    private Button btnApplyV2,btnValidateV2,btnHotWeekTemplate;
    private TabControl barterTabs;
    private TabPage visualPage,rawYamlPage,previewPage,editHotWeekPage;

    private void InitializeComponent()
    {
        barterComponents=new System.ComponentModel.Container();
        btnRewardAdd=new Button();btnRewardClone=new Button();btnRewardDelete=new Button();btnRewardUp=new Button();btnRewardDown=new Button();
        btnReqAdd=new Button();btnReqDelete=new Button();btnReqUp=new Button();btnReqDown=new Button();btnReqApply=new Button();
        btnApplyV2=new Button();btnValidateV2=new Button();btnHotWeekTemplate=new Button();
        barterTabs=new TabControl();visualPage=new TabPage();rawYamlPage=new TabPage();previewPage=new TabPage();editHotWeekPage=new TabPage();

        yamlSplit.SplitterDistance=260;
        yamlSplit.Panel2.Controls.Clear();

        Summary.Columns.Clear();
        Summary.Columns.Add("Shop",120);Summary.Columns.Add("Index",50);Summary.Columns.Add("ID",70);Summary.Columns.Add("AegisName",150);
        Summary.Columns.Add("Name",170);Summary.Columns.Add("Stock",65);Summary.Columns.Add("Success",70);Summary.Columns.Add("Zeny",80);
        Summary.Dock=DockStyle.Fill;Summary.Height=180;

        requirements.Columns.Clear();
        requirements.Columns.Add("Index",48);requirements.Columns.Add("ID",68);requirements.Columns.Add("AegisName",145);
        requirements.Columns.Add("Name",160);requirements.Columns.Add("Amount",65);requirements.Columns.Add("Refine",55);

        barterTabs.Dock=DockStyle.Fill;
        visualPage.Text="Visual Editor";rawYamlPage.Text="Raw YAML";
        barterTabs.TabPages.Add(visualPage);barterTabs.TabPages.Add(rawYamlPage);
        Editor.Dock=DockStyle.Fill;rawYamlPage.Controls.Add(Editor);

        var main=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Padding=new Padding(6)};
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,64F));main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,36F));
        visualPage.Controls.Add(main);

        var left=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=2,ColumnCount=1};
        left.RowStyles.Add(new RowStyle(SizeType.Percent,56F));left.RowStyles.Add(new RowStyle(SizeType.Percent,44F));
        main.Controls.Add(left,0,0);

        var soldGroup=new GroupBox{Text="Sold Items",Dock=DockStyle.Fill,Padding=new Padding(8)};
        var soldToolbar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=38,WrapContents=false};
        ConfigureButton(btnRewardAdd,"+ Item");ConfigureButton(btnRewardClone,"Clone");ConfigureButton(btnRewardDelete,"Delete");ConfigureButton(btnRewardUp,"↑");ConfigureButton(btnRewardDown,"↓");
        soldToolbar.Controls.AddRange([btnRewardAdd,btnRewardClone,btnRewardDelete,btnRewardUp,btnRewardDown]);
        soldGroup.Controls.Add(Summary);soldGroup.Controls.Add(soldToolbar);left.Controls.Add(soldGroup,0,0);

        var reqGroup=new GroupBox{Text="Required Items / Craft Recipe",Dock=DockStyle.Fill,Padding=new Padding(8)};
        var reqToolbar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=38,WrapContents=false};
        ConfigureButton(btnReqAdd,"+ Material");ConfigureButton(btnReqDelete,"Delete");ConfigureButton(btnReqUp,"↑");ConfigureButton(btnReqDown,"↓");
        reqToolbar.Controls.AddRange([btnReqAdd,btnReqDelete,btnReqUp,btnReqDown]);
        var reqEdit=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=72,WrapContents=true,Padding=new Padding(2)};
        reqEdit.Controls.Add(new Label{Text="Aegis",AutoSize=true,Padding=new Padding(0,8,0,0)});reqEdit.Controls.Add(requirementItem);
        reqEdit.Controls.Add(new Label{Text="Amount",AutoSize=true,Padding=new Padding(5,8,0,0)});reqEdit.Controls.Add(requirementAmount);
        reqEdit.Controls.Add(new Label{Text="Refine",AutoSize=true,Padding=new Padding(5,8,0,0)});reqEdit.Controls.Add(requirementRefine);
        ConfigureButton(btnReqApply,"Apply Material");reqEdit.Controls.Add(btnReqApply);
        requirements.Dock=DockStyle.Fill;reqGroup.Controls.Add(requirements);reqGroup.Controls.Add(reqEdit);reqGroup.Controls.Add(reqToolbar);left.Controls.Add(reqGroup,0,1);

        var rightTabs=new TabControl{Dock=DockStyle.Fill};
        previewPage.Text="Preview";editHotWeekPage.Text="Edit / Hot Week";
        rightTabs.TabPages.Add(previewPage);rightTabs.TabPages.Add(editHotWeekPage);main.Controls.Add(rightTabs,1,0);

        var previewLayout=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=2,ColumnCount=1,Padding=new Padding(4)};
        previewLayout.RowStyles.Add(new RowStyle(SizeType.Percent,52F));previewLayout.RowStyles.Add(new RowStyle(SizeType.Percent,48F));previewPage.Controls.Add(previewLayout);
        var itemGroup=new GroupBox{Text="Item Preview",Dock=DockStyle.Fill,Padding=new Padding(10)};
        var itemLayout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1};
        itemLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,138F));itemLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100F));
        var iconHost=new Panel{Dock=DockStyle.Fill};itemPreviewIcon.Location=new Point(10,18);iconHost.Controls.Add(itemPreviewIcon);
        var metaFlow=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true};
        metaFlow.Controls.Add(itemPreviewName);metaFlow.Controls.Add(itemPreviewMeta);metaFlow.Controls.Add(itemPreviewSource);
        itemLayout.Controls.Add(iconHost,0,0);itemLayout.Controls.Add(metaFlow,1,0);itemGroup.Controls.Add(itemLayout);previewLayout.Controls.Add(itemGroup,0,0);

        var equipGroup=new GroupBox{Text="Equip Preview",Dock=DockStyle.Fill,Padding=new Padding(10)};
        equipPreview.Dock=DockStyle.Fill;equipGroup.Controls.Add(equipPreview);equipGroup.Controls.Add(equipPreviewInfo);previewLayout.Controls.Add(equipGroup,0,1);

        var tools=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new Padding(12)};
        tools.Controls.Add(new Label{Text="REWARD ITEM",AutoSize=true,Font=new Font("Segoe UI",10F,FontStyle.Bold)});
        tools.Controls.Add(selected);tools.Controls.Add(MakeField("Reward Item • AegisName",rewardItem));tools.Controls.Add(MakeRow("Zeny",zeny));
        tools.Controls.Add(new Label{Text="HOT WEEK / SECURE V2",AutoSize=true,Font=new Font("Segoe UI",10F,FontStyle.Bold),Margin=new Padding(3,10,3,3)});
        tools.Controls.Add(MakeField("Event ID • เปลี่ยนค่าเพื่อ reset limit",eventId));tools.Controls.Add(MakeField("Start • เวลาของ Server",start));tools.Controls.Add(MakeField("End • เวลาของ Server",end));
        tools.Controls.Add(MakeRow("Stock • 0 = unlimited",stock));tools.Controls.Add(MakeRow("SuccessRate • 10000 = 100%",successRate));tools.Controls.Add(consumeOnFail);
        tools.Controls.Add(MakeRow("Account limit • 0 = unlimited",accountLimit));tools.Controls.Add(MakeRow("Character limit • 0 = unlimited",characterLimit));
        var actions=new FlowLayoutPanel{AutoSize=true,Width=320,WrapContents=true};
        ConfigureButton(btnApplyV2,"Apply to selected");ConfigureButton(btnValidateV2,"Validate V2");ConfigureButton(btnHotWeekTemplate,"Hot Week Template");
        actions.Controls.AddRange([btnApplyV2,btnValidateV2,btnHotWeekTemplate]);tools.Controls.Add(actions);tools.Controls.Add(v2Status);
        editHotWeekPage.Controls.Add(tools);

        yamlSplit.Panel2.Controls.Add(barterTabs);
    }

    static Control MakeField(string label,Control input){var p=new FlowLayoutPanel{FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoSize=true,Width=320};p.Controls.Add(new Label{Text=label,AutoSize=true});p.Controls.Add(input);return p;}
    static Control MakeRow(string label,Control input){var p=new FlowLayoutPanel{AutoSize=true,Width=320,WrapContents=false};p.Controls.Add(new Label{Text=label,AutoSize=true,Width=190,Padding=new Padding(0,7,0,0)});p.Controls.Add(input);return p;}
    static void ConfigureButton(Button b,string text){b.Text=text;b.AutoSize=true;b.Height=30;b.FlatStyle=FlatStyle.Flat;b.FlatAppearance.BorderSize=0;}
}
