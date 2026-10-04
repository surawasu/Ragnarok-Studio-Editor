#nullable disable
namespace MavenROStudio;

partial class ItemEditorControl
{
    private System.ComponentModel.IContainer components = null;

    private Button btnLoad;
    private Button btnClientToDb;
    private Button btnDbToClient;
    private Button btnSaveClient;
    private Button btnSaveServer;
    private Button btnSaveBoth;

    private TextBox id;
    private Label state;
    private TextBox itemSearch;
    private ListView itemList;
    private PictureBox itemIcon;
    private Label itemIconInfo;

    private ColumnHeader itemColId;
    private ColumnHeader itemColAegis;
    private ColumnHeader itemColName;
    private ColumnHeader itemColClientName;
    private ColumnHeader itemColStatus;

    private Panel clientHost;
    private Panel serverHost;
    private FlowLayoutPanel top;
    private Label itemIdLabel;
    private SplitContainer main;
    private Panel left;
    private Label listTitle;
    private TableLayoutPanel editorArea;
    private GroupBox preview;
    private TableLayoutPanel previewLayout;
    private Panel iconHost;
    private FlowLayoutPanel previewInfoHost;
    private TableLayoutPanel editSplit;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        iconCancel?.Cancel();
        iconCancel?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        btnLoad = new Button();
        btnClientToDb = new Button();
        btnDbToClient = new Button();
        btnSaveClient = new Button();
        btnSaveServer = new Button();
        btnSaveBoth = new Button();
        id = new TextBox();
        state = new Label();
        itemSearch = new TextBox();
        itemList = new ListView();
        itemColId = new ColumnHeader();
        itemColAegis = new ColumnHeader();
        itemColName = new ColumnHeader();
        itemColClientName = new ColumnHeader();
        itemColStatus = new ColumnHeader();
        itemIcon = new PictureBox();
        itemIconInfo = new Label();
        clientHost = new Panel();
        serverHost = new Panel();
        top = new FlowLayoutPanel();
        itemIdLabel = new Label();
        main = new SplitContainer();
        left = new Panel();
        listTitle = new Label();
        editorArea = new TableLayoutPanel();
        preview = new GroupBox();
        previewLayout = new TableLayoutPanel();
        iconHost = new Panel();
        previewInfoHost = new FlowLayoutPanel();
        editSplit = new TableLayoutPanel();

        ((System.ComponentModel.ISupportInitialize)itemIcon).BeginInit();
        top.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)main).BeginInit();
        main.Panel1.SuspendLayout();
        main.Panel2.SuspendLayout();
        main.SuspendLayout();
        left.SuspendLayout();
        editorArea.SuspendLayout();
        preview.SuspendLayout();
        previewLayout.SuspendLayout();
        iconHost.SuspendLayout();
        previewInfoHost.SuspendLayout();
        editSplit.SuspendLayout();
        SuspendLayout();

        // btnLoad
        btnLoad.AutoSize = true;
        btnLoad.Location = new Point(177, 7);
        btnLoad.Name = "btnLoad";
        btnLoad.Size = new Size(75, 32);
        btnLoad.TabIndex = 2;
        btnLoad.Text = "Load";
        btnLoad.UseVisualStyleBackColor = true;

        // btnClientToDb
        btnClientToDb.AutoSize = true;
        btnClientToDb.Location = new Point(258, 7);
        btnClientToDb.Name = "btnClientToDb";
        btnClientToDb.Size = new Size(98, 32);
        btnClientToDb.TabIndex = 3;
        btnClientToDb.Text = "Client → DB";
        btnClientToDb.UseVisualStyleBackColor = true;

        // btnDbToClient
        btnDbToClient.AutoSize = true;
        btnDbToClient.Location = new Point(362, 7);
        btnDbToClient.Name = "btnDbToClient";
        btnDbToClient.Size = new Size(98, 32);
        btnDbToClient.TabIndex = 4;
        btnDbToClient.Text = "DB → Client";
        btnDbToClient.UseVisualStyleBackColor = true;

        // btnSaveClient
        btnSaveClient.AutoSize = true;
        btnSaveClient.Location = new Point(466, 7);
        btnSaveClient.Name = "btnSaveClient";
        btnSaveClient.Size = new Size(104, 32);
        btnSaveClient.TabIndex = 5;
        btnSaveClient.Text = "Save Client";
        btnSaveClient.UseVisualStyleBackColor = true;

        // btnSaveServer
        btnSaveServer.AutoSize = true;
        btnSaveServer.Location = new Point(576, 7);
        btnSaveServer.Name = "btnSaveServer";
        btnSaveServer.Size = new Size(107, 32);
        btnSaveServer.TabIndex = 6;
        btnSaveServer.Text = "Save Server";
        btnSaveServer.UseVisualStyleBackColor = true;

        // btnSaveBoth
        btnSaveBoth.AutoSize = true;
        btnSaveBoth.Location = new Point(689, 7);
        btnSaveBoth.Name = "btnSaveBoth";
        btnSaveBoth.Size = new Size(96, 32);
        btnSaveBoth.TabIndex = 7;
        btnSaveBoth.Text = "Save Both";
        btnSaveBoth.UseVisualStyleBackColor = true;

        // id
        id.Location = new Point(65, 8);
        id.Name = "id";
        id.Size = new Size(106, 27);
        id.TabIndex = 1;

        // state
        state.AutoSize = true;
        state.Location = new Point(791, 4);
        state.Name = "state";
        state.Padding = new Padding(10, 8, 0, 0);
        state.Size = new Size(10, 28);
        state.TabIndex = 8;

        // itemSearch
        itemSearch.Dock = DockStyle.Top;
        itemSearch.Location = new Point(6, 36);
        itemSearch.Name = "itemSearch";
        itemSearch.PlaceholderText = "ค้นหา ID / AegisName / Name / Resource";
        itemSearch.Size = new Size(338, 27);
        itemSearch.TabIndex = 1;

        // itemList
        itemList.Columns.AddRange(new ColumnHeader[] { itemColId, itemColAegis, itemColName, itemColClientName, itemColStatus });
        itemList.Dock = DockStyle.Fill;
        itemList.FullRowSelect = true;
        itemList.GridLines = true;
        itemList.HideSelection = false;
        itemList.Location = new Point(6, 63);
        itemList.Name = "itemList";
        itemList.Size = new Size(338, 638);
        itemList.TabIndex = 2;
        itemList.UseCompatibleStateImageBehavior = false;
        itemList.View = View.Details;
        itemList.VirtualMode = true;

        // itemColId
        itemColId.Text = "ID";
        itemColId.Width = 72;

        // itemColAegis
        itemColAegis.Text = "AegisName";
        itemColAegis.Width = 145;

        // itemColName
        itemColName.Text = "Name";
        itemColName.Width = 190;

        // itemColClientName
        itemColClientName.Text = "Client Name";
        itemColClientName.Width = 190;

        // itemColStatus
        itemColStatus.Text = "Status";
        itemColStatus.Width = 95;

        // itemIcon
        itemIcon.BackColor = Color.FromArgb(38, 38, 44);
        itemIcon.Location = new Point(8, 8);
        itemIcon.Margin = new Padding(10);
        itemIcon.Name = "itemIcon";
        itemIcon.Size = new Size(112, 112);
        itemIcon.SizeMode = PictureBoxSizeMode.Zoom;
        itemIcon.TabIndex = 0;
        itemIcon.TabStop = false;

        // itemIconInfo
        itemIconInfo.AutoSize = true;
        itemIconInfo.Location = new Point(9, 6);
        itemIconInfo.MaximumSize = new Size(900, 0);
        itemIconInfo.Name = "itemIconInfo";
        itemIconInfo.Size = new Size(0, 20);
        itemIconInfo.TabIndex = 0;

        // clientHost
        clientHost.Dock = DockStyle.Fill;
        clientHost.Location = new Point(3, 3);
        clientHost.Name = "clientHost";
        clientHost.Padding = new Padding(2);
        clientHost.Size = new Size(627, 553);
        clientHost.TabIndex = 0;

        // serverHost
        serverHost.Dock = DockStyle.Fill;
        serverHost.Location = new Point(636, 3);
        serverHost.Name = "serverHost";
        serverHost.Padding = new Padding(2);
        serverHost.Size = new Size(628, 553);
        serverHost.TabIndex = 1;

        // top
        top.Controls.Add(itemIdLabel);
        top.Controls.Add(id);
        top.Controls.Add(btnLoad);
        top.Controls.Add(btnClientToDb);
        top.Controls.Add(btnDbToClient);
        top.Controls.Add(btnSaveClient);
        top.Controls.Add(btnSaveServer);
        top.Controls.Add(btnSaveBoth);
        top.Controls.Add(state);
        top.Dock = DockStyle.Top;
        top.Location = new Point(0, 0);
        top.Name = "top";
        top.Padding = new Padding(4, 4, 0, 0);
        top.Size = new Size(1634, 44);
        top.TabIndex = 1;
        top.WrapContents = false;

        // itemIdLabel
        itemIdLabel.AutoSize = true;
        itemIdLabel.Location = new Point(7, 4);
        itemIdLabel.Name = "itemIdLabel";
        itemIdLabel.Padding = new Padding(0, 8, 0, 0);
        itemIdLabel.Size = new Size(52, 28);
        itemIdLabel.TabIndex = 0;
        itemIdLabel.Text = "Item ID";

        // main
        main.Dock = DockStyle.Fill;
        main.FixedPanel = FixedPanel.Panel1;
        main.Location = new Point(0, 44);
        main.Name = "main";
        main.Panel1.Controls.Add(left);
        main.Panel1MinSize = 300;
        main.Panel2.Controls.Add(editorArea);
        main.Panel2MinSize = 520;
        main.Size = new Size(1634, 707);
        main.SplitterDistance = 350;
        main.TabIndex = 0;

        // left
        left.Controls.Add(itemList);
        left.Controls.Add(itemSearch);
        left.Controls.Add(listTitle);
        left.Dock = DockStyle.Fill;
        left.Location = new Point(0, 0);
        left.Name = "left";
        left.Padding = new Padding(6);
        left.Size = new Size(350, 707);
        left.TabIndex = 0;

        // listTitle
        listTitle.Dock = DockStyle.Top;
        listTitle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        listTitle.Location = new Point(6, 6);
        listTitle.Name = "listTitle";
        listTitle.Size = new Size(338, 30);
        listTitle.TabIndex = 0;
        listTitle.Text = "ALL ITEMS";
        listTitle.TextAlign = ContentAlignment.MiddleLeft;

        // editorArea
        editorArea.ColumnCount = 1;
        editorArea.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        editorArea.Controls.Add(preview, 0, 0);
        editorArea.Controls.Add(editSplit, 0, 1);
        editorArea.Dock = DockStyle.Fill;
        editorArea.Location = new Point(0, 0);
        editorArea.Name = "editorArea";
        editorArea.RowCount = 2;
        editorArea.RowStyles.Add(new RowStyle(SizeType.Absolute, 142F));
        editorArea.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        editorArea.Size = new Size(1280, 707);
        editorArea.TabIndex = 0;

        // preview
        preview.Controls.Add(previewLayout);
        preview.Dock = DockStyle.Fill;
        preview.Location = new Point(3, 3);
        preview.Name = "preview";
        preview.Padding = new Padding(8);
        preview.Size = new Size(1274, 136);
        preview.TabIndex = 0;
        preview.TabStop = false;
        preview.Text = "Item Preview";

        // previewLayout
        previewLayout.ColumnCount = 2;
        previewLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 135F));
        previewLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        previewLayout.Controls.Add(iconHost, 0, 0);
        previewLayout.Controls.Add(previewInfoHost, 1, 0);
        previewLayout.Dock = DockStyle.Fill;
        previewLayout.Location = new Point(8, 28);
        previewLayout.Name = "previewLayout";
        previewLayout.RowCount = 1;
        previewLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        previewLayout.Size = new Size(1258, 100);
        previewLayout.TabIndex = 0;

        // iconHost
        iconHost.Controls.Add(itemIcon);
        iconHost.Dock = DockStyle.Fill;
        iconHost.Location = new Point(3, 3);
        iconHost.Name = "iconHost";
        iconHost.Size = new Size(129, 94);
        iconHost.TabIndex = 0;

        // previewInfoHost
        previewInfoHost.AutoScroll = true;
        previewInfoHost.Controls.Add(itemIconInfo);
        previewInfoHost.Dock = DockStyle.Fill;
        previewInfoHost.FlowDirection = FlowDirection.TopDown;
        previewInfoHost.Location = new Point(138, 3);
        previewInfoHost.Name = "previewInfoHost";
        previewInfoHost.Padding = new Padding(6);
        previewInfoHost.Size = new Size(1117, 94);
        previewInfoHost.TabIndex = 1;
        previewInfoHost.WrapContents = false;

        // editSplit
        editSplit.ColumnCount = 2;
        editSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        editSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        editSplit.Controls.Add(clientHost, 0, 0);
        editSplit.Controls.Add(serverHost, 1, 0);
        editSplit.Dock = DockStyle.Fill;
        editSplit.Location = new Point(3, 145);
        editSplit.Name = "editSplit";
        editSplit.RowCount = 1;
        editSplit.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        editSplit.Size = new Size(1274, 559);
        editSplit.TabIndex = 1;

        // ItemEditorControl
        BackColor = Color.FromArgb(25, 21, 36);
        Controls.Add(main);
        Controls.Add(top);
        ForeColor = Color.FromArgb(244, 241, 250);
        Name = "ItemEditorControl";
        Size = new Size(1634, 751);

        ((System.ComponentModel.ISupportInitialize)itemIcon).EndInit();
        top.ResumeLayout(false);
        top.PerformLayout();
        main.Panel1.ResumeLayout(false);
        main.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)main).EndInit();
        main.ResumeLayout(false);
        left.ResumeLayout(false);
        left.PerformLayout();
        editorArea.ResumeLayout(false);
        preview.ResumeLayout(false);
        previewLayout.ResumeLayout(false);
        iconHost.ResumeLayout(false);
        previewInfoHost.ResumeLayout(false);
        previewInfoHost.PerformLayout();
        editSplit.ResumeLayout(false);
        ResumeLayout(false);
    }
}
