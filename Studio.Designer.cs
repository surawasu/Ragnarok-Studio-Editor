#nullable disable

namespace MavenROStudio;

partial class Studio
{
    private System.ComponentModel.IContainer components = null;

    private Panel headerPanel = null!;
    private Label headerTitle = null!;
    private FlowLayoutPanel topToolbar = null!;
    private Label clientLabel = null!;
    private TextBox client = null!;
    private Button btnBrowseClient = null!;
    private Label serverLabel = null!;
    private TextBox server = null!;
    private Button btnBrowseServer = null!;
    private Button btnReloadProject = null!;
    private CheckBox loose = null!;
    private ComboBox encoding = null!;
    private Button btnCancelLoad = null!;

    private FlowLayoutPanel sidebar = null!;
    private Label brandTitle = null!;
    private Label brandVersion = null!;
    private Label navBrowseTitle = null!;
    private Label navEditorsTitle = null!;
    private Label navToolsTitle = null!;
    private Button navItemInspector = null!;
    private Button navGrfExplorer = null!;
    private Button navItemEditor = null!;
    private Button navBarter = null!;
    private Button navNpc = null!;
    private Button navPet = null!;
    private Button navValidator = null!;
    private Button navDataSources = null!;
    private Button navDiagnostics = null!;

    private Panel contentHost = null!;
    private Label designerHint = null!;
    private Label status = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        headerPanel = new Panel();
        topToolbar = new FlowLayoutPanel();
        clientLabel = new Label();
        client = new TextBox();
        btnBrowseClient = new Button();
        serverLabel = new Label();
        server = new TextBox();
        btnBrowseServer = new Button();
        btnReloadProject = new Button();
        loose = new CheckBox();
        encoding = new ComboBox();
        btnCancelLoad = new Button();
        headerTitle = new Label();

        sidebar = new FlowLayoutPanel();
        brandTitle = new Label();
        brandVersion = new Label();
        navBrowseTitle = new Label();
        navItemInspector = new Button();
        navGrfExplorer = new Button();
        navEditorsTitle = new Label();
        navItemEditor = new Button();
        navBarter = new Button();
        navNpc = new Button();
        navPet = new Button();
        navToolsTitle = new Label();
        navValidator = new Button();
        navDataSources = new Button();
        navDiagnostics = new Button();

        contentHost = new Panel();
        designerHint = new Label();
        status = new Label();

        SuspendLayout();
        headerPanel.SuspendLayout();
        topToolbar.SuspendLayout();
        sidebar.SuspendLayout();
        contentHost.SuspendLayout();

        // Form
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(14, 12, 20);
        ClientSize = new Size(1500, 900);
        ForeColor = Color.FromArgb(244, 241, 250);
        Font = new Font("Segoe UI", 10F);
        MinimumSize = new Size(1180, 700);
        StartPosition = FormStartPosition.CenterScreen;
        Text = "MAVEN RO Studio 4.2.1 • Designer Hotfix";

        // Header
        headerPanel.BackColor = Color.FromArgb(25, 21, 36);
        headerPanel.Dock = DockStyle.Top;
        headerPanel.Height = 82;
        headerPanel.Padding = new Padding(16, 10, 14, 8);

        headerTitle.AutoSize = false;
        headerTitle.Dock = DockStyle.Top;
        headerTitle.Height = 24;
        headerTitle.ForeColor = Color.FromArgb(244, 241, 250);
        headerTitle.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        headerTitle.Text = "PROJECT WORKSPACE";
        headerTitle.TextAlign = ContentAlignment.MiddleLeft;

        topToolbar.Dock = DockStyle.Fill;
        topToolbar.FlowDirection = FlowDirection.LeftToRight;
        topToolbar.WrapContents = false;
        topToolbar.AutoScroll = true;
        topToolbar.BackColor = Color.FromArgb(25, 21, 36);
        topToolbar.Padding = new Padding(0, 5, 0, 0);

        clientLabel.AutoSize = true;
        clientLabel.ForeColor = Color.FromArgb(170, 161, 187);
        clientLabel.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        clientLabel.Padding = new Padding(0, 7, 0, 0);
        clientLabel.Text = "CLIENT";

        client.Width = 285;
        client.BackColor = Color.FromArgb(22, 19, 31);
        client.ForeColor = Color.FromArgb(244, 241, 250);
        client.BorderStyle = BorderStyle.FixedSingle;

        ConfigureToolbarButton(btnBrowseClient, "Browse", false);
        serverLabel.AutoSize = true;
        serverLabel.ForeColor = Color.FromArgb(170, 161, 187);
        serverLabel.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        serverLabel.Padding = new Padding(12, 7, 0, 0);
        serverLabel.Text = "SERVER";

        server.Width = 285;
        server.BackColor = Color.FromArgb(22, 19, 31);
        server.ForeColor = Color.FromArgb(244, 241, 250);
        server.BorderStyle = BorderStyle.FixedSingle;

        ConfigureToolbarButton(btnBrowseServer, "Browse", false);
        ConfigureToolbarButton(btnReloadProject, "Reload Project", true);

        loose.AutoSize = true;
        loose.ForeColor = Color.FromArgb(244, 241, 250);
        loose.Margin = new Padding(8, 8, 4, 0);
        loose.Text = "Loose data first";

        encoding.Width = 135;
        encoding.DropDownStyle = ComboBoxStyle.DropDownList;
        encoding.BackColor = Color.FromArgb(22, 19, 31);
        encoding.ForeColor = Color.FromArgb(244, 241, 250);
        encoding.FlatStyle = FlatStyle.Flat;

        ConfigureToolbarButton(btnCancelLoad, "Cancel", false);

        topToolbar.Controls.Add(clientLabel);
        topToolbar.Controls.Add(client);
        topToolbar.Controls.Add(btnBrowseClient);
        topToolbar.Controls.Add(serverLabel);
        topToolbar.Controls.Add(server);
        topToolbar.Controls.Add(btnBrowseServer);
        topToolbar.Controls.Add(btnReloadProject);
        topToolbar.Controls.Add(loose);
        topToolbar.Controls.Add(encoding);
        topToolbar.Controls.Add(btnCancelLoad);

        headerPanel.Controls.Add(topToolbar);
        headerPanel.Controls.Add(headerTitle);

        // Sidebar
        sidebar.BackColor = Color.FromArgb(18, 15, 27);
        sidebar.Dock = DockStyle.Left;
        sidebar.Width = 224;
        sidebar.FlowDirection = FlowDirection.TopDown;
        sidebar.WrapContents = false;
        sidebar.AutoScroll = true;
        sidebar.Padding = new Padding(12, 14, 12, 12);

        brandTitle.Width = 196;
        brandTitle.Height = 34;
        brandTitle.ForeColor = Color.White;
        brandTitle.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
        brandTitle.Text = "MAVEN";
        brandTitle.TextAlign = ContentAlignment.MiddleLeft;

        brandVersion.Width = 196;
        brandVersion.Height = 26;
        brandVersion.ForeColor = Color.FromArgb(157, 112, 255);
        brandVersion.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        brandVersion.Margin = new Padding(0, 0, 0, 12);
        brandVersion.Text = "RO STUDIO  •  4.2.1";
        brandVersion.TextAlign = ContentAlignment.MiddleLeft;

        ConfigureSectionLabel(navBrowseTitle, "BROWSE", false);
        ConfigureNavButton(navItemInspector, "Item Inspector");
        ConfigureNavButton(navGrfExplorer, "GRF Explorer");

        ConfigureSectionLabel(navEditorsTitle, "EDITORS", true);
        ConfigureNavButton(navItemEditor, "Item Editor");
        ConfigureNavButton(navBarter, "Barter Shop");
        ConfigureNavButton(navNpc, "NPC Editor");
        ConfigureNavButton(navPet, "Pet Editor");

        ConfigureSectionLabel(navToolsTitle, "TOOLS", true);
        ConfigureNavButton(navValidator, "Project Validator");
        ConfigureNavButton(navDataSources, "Data Sources");
        ConfigureNavButton(navDiagnostics, "Diagnostics");

        sidebar.Controls.Add(brandTitle);
        sidebar.Controls.Add(brandVersion);
        sidebar.Controls.Add(navBrowseTitle);
        sidebar.Controls.Add(navItemInspector);
        sidebar.Controls.Add(navGrfExplorer);
        sidebar.Controls.Add(navEditorsTitle);
        sidebar.Controls.Add(navItemEditor);
        sidebar.Controls.Add(navBarter);
        sidebar.Controls.Add(navNpc);
        sidebar.Controls.Add(navPet);
        sidebar.Controls.Add(navToolsTitle);
        sidebar.Controls.Add(navValidator);
        sidebar.Controls.Add(navDataSources);
        sidebar.Controls.Add(navDiagnostics);

        // Content host: runtime editor modules are injected here.
        contentHost.BackColor = Color.FromArgb(14, 12, 20);
        contentHost.Dock = DockStyle.Fill;
        contentHost.Padding = new Padding(14);

        designerHint.Dock = DockStyle.Fill;
        designerHint.BackColor = Color.FromArgb(25, 21, 36);
        designerHint.ForeColor = Color.FromArgb(170, 161, 187);
        designerHint.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
        designerHint.TextAlign = ContentAlignment.MiddleCenter;
        designerHint.Text = "DESIGNER CONTENT HOST\r\n\r\nRuntime editors are injected into this panel.\r\nResize/reposition Header, Sidebar and Content Host here.";
        contentHost.Controls.Add(designerHint);

        // Status bar
        status.BackColor = Color.FromArgb(18, 15, 27);
        status.Dock = DockStyle.Bottom;
        status.Height = 32;
        status.ForeColor = Color.FromArgb(170, 161, 187);
        status.Font = new Font("Segoe UI", 8.75F);
        status.Padding = new Padding(12, 0, 0, 0);
        status.Text = "Designer Ready";
        status.TextAlign = ContentAlignment.MiddleLeft;

        Controls.Add(contentHost);
        Controls.Add(sidebar);
        Controls.Add(headerPanel);
        Controls.Add(status);

        contentHost.ResumeLayout(false);
        sidebar.ResumeLayout(false);
        headerPanel.ResumeLayout(false);
        topToolbar.ResumeLayout(false);
        topToolbar.PerformLayout();
        ResumeLayout(false);
    }

    private static void ConfigureToolbarButton(Button button, string text, bool accent)
    {
        button.AutoSize = true;
        button.Height = 34;
        button.Text = text;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = accent ? Color.FromArgb(139, 92, 246) : Color.FromArgb(34, 28, 48);
        button.ForeColor = Color.FromArgb(244, 241, 250);
        button.Padding = new Padding(10, 0, 10, 0);
        button.Margin = new Padding(4, 3, 4, 3);
        button.UseVisualStyleBackColor = false;
    }

    private static void ConfigureSectionLabel(Label label, string text, bool spaced)
    {
        label.Width = 196;
        label.Height = spaced ? 30 : 24;
        label.ForeColor = Color.FromArgb(170, 161, 187);
        label.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        label.Text = text;
        label.TextAlign = spaced ? ContentAlignment.BottomLeft : ContentAlignment.MiddleLeft;
        label.Margin = spaced ? new Padding(0, 8, 0, 2) : new Padding(0);
    }

    private static void ConfigureNavButton(Button button, string text)
    {
        button.Width = 196;
        button.Height = 40;
        button.Text = text;
        button.TextAlign = ContentAlignment.MiddleLeft;
        button.Padding = new Padding(14, 0, 8, 0);
        button.Margin = new Padding(0, 2, 0, 2);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = Color.FromArgb(18, 15, 27);
        button.ForeColor = Color.FromArgb(170, 161, 187);
        button.UseVisualStyleBackColor = false;
    }
}
