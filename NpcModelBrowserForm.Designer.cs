#nullable disable
namespace MavenROStudio;

partial class NpcModelBrowserForm
{
    private System.ComponentModel.IContainer components=null;
    private TextBox search;
    private ComboBox category;
    private CheckBox onlyAvailable;
    private Label resultCount;
    private ListView modelList;
    private ColumnHeader colModel,colId,colResource,colCategory,colClient,colAct,colServer;
    private PictureBox preview;
    private Label modelName;
    private Label modelMeta;
    private Label previewMessage;
    private Label status;
    private ComboBox animationMode;
    private ComboBox direction;
    private NumericUpDown speedPercent;
    private Label frameInfo;
    private Button btnPlay,btnPrevFrame,btnNextFrame;
    private Button btnUse,btnInsert,btnCopy,btnCopyId,btnClose;

    protected override void Dispose(bool disposing){if(disposing)components?.Dispose();base.Dispose(disposing);}

    private void InitializeComponent()
    {
        search=new TextBox();category=new ComboBox();onlyAvailable=new CheckBox();resultCount=new Label();
        modelList=new ListView();colModel=new ColumnHeader();colId=new ColumnHeader();colResource=new ColumnHeader();colCategory=new ColumnHeader();colClient=new ColumnHeader();colAct=new ColumnHeader();colServer=new ColumnHeader();
        preview=new PictureBox();modelName=new Label();modelMeta=new Label();previewMessage=new Label();status=new Label();
        animationMode=new ComboBox();direction=new ComboBox();speedPercent=new NumericUpDown();frameInfo=new Label();
        btnPlay=new Button();btnPrevFrame=new Button();btnNextFrame=new Button();
        btnUse=new Button();btnInsert=new Button();btnCopy=new Button();btnCopyId=new Button();btnClose=new Button();

        var filterBar=new FlowLayoutPanel();
        var main=new SplitContainer();
        var previewHost=new Panel();
        var animationBar=new FlowLayoutPanel();
        var actionBar=new FlowLayoutPanel();
        var animLabel=new Label();
        var dirLabel=new Label();
        var speedLabel=new Label();
        ((System.ComponentModel.ISupportInitialize)preview).BeginInit();
        ((System.ComponentModel.ISupportInitialize)speedPercent).BeginInit();
        ((System.ComponentModel.ISupportInitialize)main).BeginInit();
        main.Panel1.SuspendLayout();main.Panel2.SuspendLayout();main.SuspendLayout();
        previewHost.SuspendLayout();animationBar.SuspendLayout();actionBar.SuspendLayout();filterBar.SuspendLayout();SuspendLayout();

        AutoScaleMode=AutoScaleMode.Font;
        BackColor=Color.FromArgb(14,12,20);
        ForeColor=Color.FromArgb(244,241,250);
        Font=new Font("Segoe UI",9.5F);
        ClientSize=new Size(1280,760);
        MinimumSize=new Size(980,620);
        StartPosition=FormStartPosition.CenterParent;
        Text="NPC Model Browser • ACT Animation Preview";

        filterBar.Dock=DockStyle.Top;filterBar.Height=46;filterBar.Padding=new Padding(8,7,8,5);filterBar.WrapContents=false;filterBar.BackColor=Color.FromArgb(25,21,36);
        search.Width=300;search.PlaceholderText="ค้นหา Model / Resource / Class ID";
        category.Width=150;category.DropDownStyle=ComboBoxStyle.DropDownList;
        onlyAvailable.Text="Only models with SPR";onlyAvailable.AutoSize=true;onlyAvailable.Checked=true;onlyAvailable.Padding=new Padding(8,6,0,0);
        resultCount.AutoSize=true;resultCount.Padding=new Padding(12,7,0,0);resultCount.Text="0 / 0";
        filterBar.Controls.Add(search);filterBar.Controls.Add(category);filterBar.Controls.Add(onlyAvailable);filterBar.Controls.Add(resultCount);

        main.Dock=DockStyle.Fill;main.Size=new Size(1280,636);main.Panel1MinSize=320;main.Panel2MinSize=280;main.SplitterDistance=745;
        modelList.Dock=DockStyle.Fill;modelList.View=View.Details;modelList.FullRowSelect=true;modelList.HideSelection=false;modelList.MultiSelect=false;
        modelList.Columns.AddRange(new[]{colModel,colId,colResource,colCategory,colClient,colAct,colServer});
        colModel.Text="Model";colModel.Width=180;
        colId.Text="Class ID";colId.Width=70;
        colResource.Text="Resource";colResource.Width=180;
        colCategory.Text="Category";colCategory.Width=90;
        colClient.Text="SPR";colClient.Width=55;
        colAct.Text="ACT";colAct.Width=55;
        colServer.Text="Server";colServer.Width=65;
        main.Panel1.Controls.Add(modelList);

        previewHost.Dock=DockStyle.Fill;previewHost.Padding=new Padding(14);previewHost.BackColor=Color.FromArgb(25,21,36);
        modelName.Dock=DockStyle.Top;modelName.Height=34;modelName.Font=new Font("Segoe UI",14F,FontStyle.Bold);modelName.Text="NPC MODEL";

        animationBar.Dock=DockStyle.Top;animationBar.Height=74;animationBar.Padding=new Padding(0,5,0,5);animationBar.WrapContents=true;animationBar.BackColor=Color.FromArgb(34,28,48);
        animLabel.Text="Action";animLabel.AutoSize=true;animLabel.Padding=new Padding(4,7,0,0);
        animationMode.Width=155;animationMode.DropDownStyle=ComboBoxStyle.DropDownList;
        dirLabel.Text="Direction";dirLabel.AutoSize=true;dirLabel.Padding=new Padding(8,7,0,0);
        direction.Width=110;direction.DropDownStyle=ComboBoxStyle.DropDownList;
        speedLabel.Text="Speed %";speedLabel.AutoSize=true;speedLabel.Padding=new Padding(8,7,0,0);
        speedPercent.Minimum=25;speedPercent.Maximum=400;speedPercent.Increment=25;speedPercent.Value=100;speedPercent.Width=72;
        ConfigureButton(btnPlay,"Pause",true);
        ConfigureButton(btnPrevFrame,"◀ Frame",false);
        ConfigureButton(btnNextFrame,"Frame ▶",false);
        frameInfo.AutoSize=true;frameInfo.Padding=new Padding(8,7,0,0);frameInfo.Text="Static preview";
        animationBar.Controls.Add(animLabel);animationBar.Controls.Add(animationMode);animationBar.Controls.Add(dirLabel);animationBar.Controls.Add(direction);animationBar.Controls.Add(speedLabel);animationBar.Controls.Add(speedPercent);
        animationBar.Controls.Add(btnPlay);animationBar.Controls.Add(btnPrevFrame);animationBar.Controls.Add(btnNextFrame);animationBar.Controls.Add(frameInfo);

        preview.Dock=DockStyle.Top;preview.Height=300;preview.SizeMode=PictureBoxSizeMode.Zoom;preview.BackColor=Color.FromArgb(18,15,27);
        previewMessage.Dock=DockStyle.Top;previewMessage.Height=34;previewMessage.ForeColor=Color.FromArgb(170,161,187);previewMessage.TextAlign=ContentAlignment.MiddleCenter;previewMessage.Text="Select a model to preview";
        modelMeta.Dock=DockStyle.Fill;modelMeta.AutoSize=false;modelMeta.ForeColor=Color.FromArgb(220,215,230);modelMeta.Padding=new Padding(4,10,4,4);

        previewHost.Controls.Add(modelMeta);previewHost.Controls.Add(previewMessage);previewHost.Controls.Add(preview);previewHost.Controls.Add(animationBar);previewHost.Controls.Add(modelName);
        main.Panel2.Controls.Add(previewHost);

        actionBar.Dock=DockStyle.Bottom;actionBar.Height=50;actionBar.Padding=new Padding(8);actionBar.BackColor=Color.FromArgb(25,21,36);actionBar.WrapContents=false;
        ConfigureButton(btnUse,"Use This Model",true);
        ConfigureButton(btnInsert,"Insert at Cursor",false);
        ConfigureButton(btnCopy,"Copy Model",false);
        ConfigureButton(btnCopyId,"Copy ID",false);
        ConfigureButton(btnClose,"Close",false);
        actionBar.Controls.AddRange(new Control[]{btnUse,btnInsert,btnCopy,btnCopyId,btnClose});

        status.Dock=DockStyle.Bottom;status.Height=28;status.Padding=new Padding(10,0,0,0);status.ForeColor=Color.FromArgb(170,161,187);status.BackColor=Color.FromArgb(18,15,27);status.TextAlign=ContentAlignment.MiddleLeft;status.Text="Designer Ready";

        Controls.Add(main);Controls.Add(filterBar);Controls.Add(actionBar);Controls.Add(status);
        filterBar.ResumeLayout(false);filterBar.PerformLayout();
        previewHost.ResumeLayout(false);animationBar.ResumeLayout(false);animationBar.PerformLayout();
        actionBar.ResumeLayout(false);actionBar.PerformLayout();
        main.Panel1.ResumeLayout(false);main.Panel2.ResumeLayout(false);((System.ComponentModel.ISupportInitialize)main).EndInit();main.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)preview).EndInit();
        ((System.ComponentModel.ISupportInitialize)speedPercent).EndInit();
        ResumeLayout(false);
    }

    static void ConfigureButton(Button b,string text,bool primary)
    {
        b.Text=text;b.AutoSize=true;b.Height=32;b.FlatStyle=FlatStyle.Flat;b.FlatAppearance.BorderSize=0;
        b.BackColor=primary?Color.FromArgb(139,92,246):Color.FromArgb(34,28,48);
        b.ForeColor=Color.White;b.UseVisualStyleBackColor=false;
    }
}
