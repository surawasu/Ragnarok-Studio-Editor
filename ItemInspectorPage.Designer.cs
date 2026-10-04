#nullable disable
namespace MavenROStudio;

partial class ItemInspectorPage
{
    private System.ComponentModel.IContainer components = null;
    internal TextBox Query;
    internal CheckBox Collection;
    internal Button ExportCsv;
    internal Label ItemCounts;
    internal ListView Items;
    internal TextBox Details;
    internal PictureBox Picture;
    private ColumnHeader colId;
    private ColumnHeader colName;
    private ColumnHeader colResource;
    private ColumnHeader colSource;
    private FlowLayoutPanel toolbar;
    private TableLayoutPanel split;
    private Panel right;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        Query = new TextBox();
        Collection = new CheckBox();
        ExportCsv = new Button();
        ItemCounts = new Label();
        Items = new ListView();
        colId = new ColumnHeader();
        colName = new ColumnHeader();
        colResource = new ColumnHeader();
        colSource = new ColumnHeader();
        Details = new TextBox();
        Picture = new PictureBox();
        toolbar = new FlowLayoutPanel();
        split = new TableLayoutPanel();
        right = new Panel();
        ((System.ComponentModel.ISupportInitialize)Picture).BeginInit();
        toolbar.SuspendLayout();
        split.SuspendLayout();
        right.SuspendLayout();
        SuspendLayout();
        // 
        // Query
        // 
        Query.Location = new Point(3, 8);
        Query.Name = "Query";
        Query.PlaceholderText = "ค้นหา ID / ชื่อ / Resource";
        Query.Size = new Size(300, 27);
        Query.TabIndex = 0;
        // 
        // Collection
        // 
        Collection.AutoSize = true;
        Collection.Location = new Point(309, 8);
        Collection.Name = "Collection";
        Collection.Padding = new Padding(4, 7, 0, 0);
        Collection.Size = new Size(148, 31);
        Collection.TabIndex = 1;
        Collection.Text = "Collection image";
        Collection.UseVisualStyleBackColor = true;
        // 
        // ExportCsv
        // 
        ExportCsv.AutoSize = true;
        ExportCsv.Location = new Point(463, 8);
        ExportCsv.Name = "ExportCsv";
        ExportCsv.Size = new Size(92, 32);
        ExportCsv.TabIndex = 2;
        ExportCsv.Text = "Export CSV";
        ExportCsv.UseVisualStyleBackColor = true;
        // 
        // ItemCounts
        // 
        ItemCounts.AutoSize = true;
        ItemCounts.Location = new Point(561, 5);
        ItemCounts.Name = "ItemCounts";
        ItemCounts.Padding = new Padding(14, 7, 0, 0);
        ItemCounts.Size = new Size(267, 27);
        ItemCounts.TabIndex = 3;
        ItemCounts.Text = "Client 0 • DB 0 • Merged 0 • Visible 0";
        // 
        // Items
        // 
        Items.Columns.AddRange(new ColumnHeader[] { colId, colName, colResource, colSource });
        Items.Dock = DockStyle.Fill;
        Items.FullRowSelect = true;
        Items.HideSelection = false;
        Items.Location = new Point(3, 3);
        Items.Name = "Items";
        Items.Size = new Size(941, 699);
        Items.TabIndex = 0;
        Items.UseCompatibleStateImageBehavior = false;
        Items.View = View.Details;
        Items.VirtualMode = true;
        // 
        // colId
        // 
        colId.Text = "ID";
        colId.Width = 80;
        // 
        // colName
        // 
        colName.Text = "ชื่อไอเทม";
        colName.Width = 250;
        // 
        // colResource
        // 
        colResource.Text = "Resource";
        colResource.Width = 220;
        // 
        // colSource
        // 
        colSource.Text = "Source";
        colSource.Width = 145;
        // 
        // Details
        // 
        Details.Dock = DockStyle.Fill;
        Details.Location = new Point(10, 240);
        Details.Multiline = true;
        Details.Name = "Details";
        Details.ReadOnly = true;
        Details.ScrollBars = ScrollBars.Both;
        Details.Size = new Size(671, 459);
        Details.TabIndex = 0;
        Details.WordWrap = false;
        // 
        // Picture
        // 
        Picture.BackColor = Color.FromArgb(35, 35, 42);
        Picture.Dock = DockStyle.Top;
        Picture.Location = new Point(10, 0);
        Picture.Name = "Picture";
        Picture.Size = new Size(671, 240);
        Picture.SizeMode = PictureBoxSizeMode.Zoom;
        Picture.TabIndex = 1;
        Picture.TabStop = false;
        // 
        // toolbar
        // 
        toolbar.Controls.Add(Query);
        toolbar.Controls.Add(Collection);
        toolbar.Controls.Add(ExportCsv);
        toolbar.Controls.Add(ItemCounts);
        toolbar.Dock = DockStyle.Top;
        toolbar.Location = new Point(0, 0);
        toolbar.Name = "toolbar";
        toolbar.Padding = new Padding(0, 5, 0, 5);
        toolbar.Size = new Size(1634, 46);
        toolbar.TabIndex = 1;
        toolbar.WrapContents = false;
        // 
        // split
        // 
        split.ColumnCount = 2;
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
        split.Controls.Add(Items, 0, 0);
        split.Controls.Add(right, 1, 0);
        split.Dock = DockStyle.Fill;
        split.Location = new Point(0, 46);
        split.Name = "split";
        split.RowCount = 1;
        split.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        split.Size = new Size(1634, 705);
        split.TabIndex = 0;
        // 
        // right
        // 
        right.Controls.Add(Details);
        right.Controls.Add(Picture);
        right.Dock = DockStyle.Fill;
        right.Location = new Point(950, 3);
        right.Name = "right";
        right.Padding = new Padding(10, 0, 0, 0);
        right.Size = new Size(681, 699);
        right.TabIndex = 1;
        // 
        // ItemInspectorPage
        // 
        BackColor = Color.FromArgb(25, 21, 36);
        Controls.Add(split);
        Controls.Add(toolbar);
        ForeColor = Color.FromArgb(244, 241, 250);
        Name = "ItemInspectorPage";
        Size = new Size(1634, 751);
        ((System.ComponentModel.ISupportInitialize)Picture).EndInit();
        toolbar.ResumeLayout(false);
        toolbar.PerformLayout();
        split.ResumeLayout(false);
        right.ResumeLayout(false);
        right.PerformLayout();
        ResumeLayout(false);
    }
}
