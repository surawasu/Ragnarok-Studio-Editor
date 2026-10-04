namespace MavenROStudio;

public sealed partial class NpcModelBrowserForm : Form
{
    ProjectCatalog catalog=null!;
    List<NpcModelInfo> allModels=[];
    List<NpcModelInfo> visibleModels=[];
    CancellationTokenSource? previewCancel;
    readonly System.Windows.Forms.Timer animationTimer=new();
    RagnarokNpcAnimation? animation;
    int frameIndex;
    bool playing=true;

    public NpcModelInfo? SelectedModel { get; private set; }
    public bool InsertOnly { get; private set; }
    public bool AnimationControlsReady =>
        animationMode!=null&&direction!=null&&btnPlay!=null&&btnPrevFrame!=null&&btnNextFrame!=null&&
        speedPercent!=null&&speedPercent.Minimum<=25&&speedPercent.Maximum>=400;
    public int DesignerDirectionCount => direction?.Items.Count??0;

    public NpcModelBrowserForm()
    {
        InitializeComponent();
        if(!DesignerSupport.IsDesignMode(this)){
            StudioTheme.Apply(this);
            InitializeBrowser(new ProjectCatalog());
        }
    }

    public NpcModelBrowserForm(ProjectCatalog catalog,string currentModel="")
    {
        InitializeComponent();
        StudioTheme.Apply(this);
        InitializeBrowser(catalog);
        LoadModels(currentModel);
    }

    void InitializeBrowser(ProjectCatalog project)
    {
        catalog=project;
        animationTimer.Interval=150;
        animationTimer.Tick+=(_,_)=>AdvanceFrame(1);

        search.TextChanged+=(_,_)=>ApplyFilter();
        category.SelectedIndexChanged+=(_,_)=>ApplyFilter();
        onlyAvailable.CheckedChanged+=(_,_)=>ApplyFilter();
        modelList.SelectedIndexChanged+=async(_,_)=>await ShowSelectedPreview();
        modelList.DoubleClick+=(_,_)=>Choose(false);

        animationMode.SelectedIndexChanged+=(_,_)=>ResetAnimationFrame();
        direction.SelectedIndexChanged+=(_,_)=>ResetAnimationFrame();
        speedPercent.ValueChanged+=(_,_)=>UpdateTimerInterval();
        btnPlay.Click+=(_,_)=>TogglePlay();
        btnPrevFrame.Click+=(_,_)=>AdvanceFrame(-1,true);
        btnNextFrame.Click+=(_,_)=>AdvanceFrame(1,true);

        btnUse.Click+=(_,_)=>Choose(false);
        btnInsert.Click+=(_,_)=>Choose(true);
        btnCopy.Click+=(_,_)=>CopyModel();
        btnCopyId.Click+=(_,_)=>CopyId();
        btnClose.Click+=(_,_)=>Close();

        direction.Items.Clear();
        for(int i=0;i<8;i++)direction.Items.Add($"Direction {i}");
        direction.SelectedIndex=0;
        speedPercent.Value=100;
        UpdateAnimationControls(false);
    }

    void LoadModels(string currentModel)
    {
        status.Text="Loading model catalog...";
        try
        {
            allModels=NpcModelCatalog.Load(catalog);
            category.Items.Clear();
            category.Items.Add("All");
            foreach(var c in allModels.Select(x=>x.Category).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x=>x))
                category.Items.Add(c);
            category.SelectedIndex=0;
            ApplyFilter();

            if(!string.IsNullOrWhiteSpace(currentModel))
            {
                var wanted=currentModel.Trim();
                if(wanted.StartsWith("JT_",StringComparison.OrdinalIgnoreCase))wanted=wanted[3..];
                for(int i=0;i<modelList.Items.Count;i++)
                {
                    if(modelList.Items[i].Tag is NpcModelInfo m&&m.Model.Equals(wanted,StringComparison.OrdinalIgnoreCase))
                    {
                        modelList.Items[i].Selected=true;
                        modelList.EnsureVisible(i);
                        break;
                    }
                }
            }

            int client=allModels.Count(x=>x.HasSprite);
            int server=allModels.Count(x=>x.ServerSupported);
            status.Text=$"Models {allModels.Count:N0} • Client sprite {client:N0} • Server supported {server:N0}";
        }
        catch(Exception ex)
        {
            status.Text="Model catalog error: "+ex.Message;
        }
    }

    void ApplyFilter()
    {
        if(catalog==null)return;
        string q=search.Text.Trim();
        string cat=category.SelectedItem?.ToString()??"All";
        visibleModels=allModels.Where(m=>
            (!onlyAvailable.Checked||m.HasSprite)&&
            (cat=="All"||m.Category.Equals(cat,StringComparison.OrdinalIgnoreCase))&&
            (q.Length==0||
             m.Model.Contains(q,StringComparison.OrdinalIgnoreCase)||
             m.Constant.Contains(q,StringComparison.OrdinalIgnoreCase)||
             m.Resource.Contains(q,StringComparison.OrdinalIgnoreCase)||
             (m.ClassId>=0&&m.ClassId.ToString().Contains(q))))
            .ToList();

        modelList.BeginUpdate();
        modelList.Items.Clear();
        foreach(var m in visibleModels)
        {
            var row=new ListViewItem(m.Model){Tag=m};
            row.SubItems.Add(m.ClassId>=0?m.ClassId.ToString():"—");
            row.SubItems.Add(m.Resource);
            row.SubItems.Add(m.Category);
            row.SubItems.Add(m.HasSprite?"SPR":"—");
            row.SubItems.Add(m.HasAct?"ACT":"—");
            row.SubItems.Add(m.ServerSupported?"YES":"—");
            modelList.Items.Add(row);
        }
        modelList.EndUpdate();
        resultCount.Text=$"{visibleModels.Count:N0} / {allModels.Count:N0}";
    }

    async Task ShowSelectedPreview()
    {
        previewCancel?.Cancel();
        previewCancel?.Dispose();
        previewCancel=new CancellationTokenSource();
        var ct=previewCancel.Token;

        StopAndDisposeAnimation();
        SetPreviewImage(null);
        previewMessage.Text="Select a model to preview";
        modelMeta.Text="";
        SelectedModel=null;
        animationMode.Items.Clear();
        UpdateAnimationControls(false);

        if(modelList.SelectedItems.Count==0||modelList.SelectedItems[0].Tag is not NpcModelInfo model)return;
        SelectedModel=model;
        modelName.Text=model.Model;
        string nl=Environment.NewLine;
        modelMeta.Text=
            $"Class ID: {(model.ClassId>=0?model.ClassId.ToString():"Unknown")}" + nl +
            $"Constant: {model.Constant}" + nl +
            $"Resource: {model.Resource}" + nl +
            $"Category: {model.Category}" + nl +
            $"SPR: {(model.HasSprite?"FOUND":"MISSING")}" + nl +
            $"ACT: {(model.HasAct?"FOUND":"MISSING")}" + nl +
            $"GRF: {(string.IsNullOrWhiteSpace(model.Archive)?"—":model.Archive)}" + nl +
            $"SPR Path: {model.SprPath}" + nl +
            $"ACT Path: {model.ActPath}";
        previewMessage.Text=model.HasSprite?"Loading SPR / ACT...":"No client SPR for this model";

        if(!model.HasSprite||catalog.Assets==null)return;
        try
        {
            var loaded=await Task.Run(()=>{
                ct.ThrowIfCancellationRequested();
                var sprBytes=catalog.Assets.Read(model.SprPath,ct).Bytes;
                var sheet=RagnarokSprSheet.Parse(sprBytes);
                try
                {
                    if(model.HasAct)
                    {
                        var actBytes=catalog.Assets.Read(model.ActPath,ct).Bytes;
                        var act=RagnarokActFile.Parse(actBytes);
                        return (Animation:new RagnarokNpcAnimation(sheet,act),Fallback:(Bitmap?)null,Error:"");
                    }

                    Bitmap? fallback=sheet.GetImage(0,0)!=null?new Bitmap(sheet.GetImage(0,0)!):
                        sheet.GetImage(1,0)!=null?new Bitmap(sheet.GetImage(1,0)!):null;
                    sheet.Dispose();
                    return (Animation:(RagnarokNpcAnimation?)null,Fallback:fallback,Error:"ACT missing");
                }
                catch(Exception ex)
                {
                    Bitmap? fallback=sheet.GetImage(0,0)!=null?new Bitmap(sheet.GetImage(0,0)!):
                        sheet.GetImage(1,0)!=null?new Bitmap(sheet.GetImage(1,0)!):null;
                    sheet.Dispose();
                    return (Animation:(RagnarokNpcAnimation?)null,Fallback:fallback,Error:ex.Message);
                }
            },ct);

            if(ct.IsCancellationRequested)
            {
                loaded.Animation?.Dispose();
                loaded.Fallback?.Dispose();
                return;
            }

            animation=loaded.Animation;
            if(animation!=null)
            {
                foreach(var choice in NpcAnimationActions.Groups(animation.Act))animationMode.Items.Add(choice);
                if(animationMode.Items.Count>0)animationMode.SelectedIndex=0;
                frameIndex=0;
                playing=true;
                btnPlay.Text="Pause";
                UpdateAnimationControls(true);
                RenderCurrentFrame();
                animationTimer.Start();
            }
            else
            {
                SetPreviewImage(loaded.Fallback);
                previewMessage.Text=string.IsNullOrWhiteSpace(loaded.Error)
                    ?"SPR static preview"
                    :"SPR static preview • ACT unavailable: "+loaded.Error;
                UpdateAnimationControls(false);
            }
        }
        catch(OperationCanceledException){}
        catch(Exception ex)
        {
            if(!ct.IsCancellationRequested)
            {
                previewMessage.Text="Preview error: "+ex.Message;
                UpdateAnimationControls(false);
            }
        }
    }

    void ResetAnimationFrame()
    {
        if(animation==null)return;
        frameIndex=0;
        RenderCurrentFrame();
    }

    void TogglePlay()
    {
        if(animation==null)return;
        playing=!playing;
        btnPlay.Text=playing?"Pause":"Play";
        if(playing)animationTimer.Start();else animationTimer.Stop();
    }

    void AdvanceFrame(int delta,bool manual=false)
    {
        if(animation==null)return;
        int actionIndex=CurrentActionIndex();
        if(actionIndex<0||actionIndex>=animation.Act.Actions.Count)return;
        int count=animation.Act.Actions[actionIndex].Frames.Count;
        if(count<=0)return;
        frameIndex=(frameIndex+delta)%count;
        if(frameIndex<0)frameIndex+=count;
        if(manual){playing=false;animationTimer.Stop();btnPlay.Text="Play";}
        RenderCurrentFrame();
    }

    int CurrentActionIndex()
    {
        if(animation==null)return -1;
        int group=animationMode.SelectedItem is NpcAnimationChoice choice?choice.GroupIndex:0;
        int dir=Math.Max(0,direction.SelectedIndex);
        return NpcAnimationActions.ResolveAction(animation.Act,group,dir);
    }

    void RenderCurrentFrame()
    {
        if(animation==null)return;
        int actionIndex=CurrentActionIndex();
        if(actionIndex<0||actionIndex>=animation.Act.Actions.Count)return;
        var action=animation.Act.Actions[actionIndex];
        if(action.Frames.Count==0)
        {
            SetPreviewImage(null);
            previewMessage.Text=$"ACT {animation.Act.Version} • action {actionIndex} has no frames";
            frameInfo.Text="Frame 0 / 0";
            return;
        }
        if(frameIndex>=action.Frames.Count)frameIndex=0;
        try
        {
            SetPreviewImage(animation.Render(actionIndex,frameIndex));
            string group=animationMode.SelectedItem?.ToString()??"Action";
            previewMessage.Text=$"ACT {animation.Act.Version} • {group} • Dir {direction.SelectedIndex} • Action {actionIndex}";
            frameInfo.Text=$"Frame {frameIndex+1} / {action.Frames.Count} • Base {action.DelayMs} ms";
            UpdateTimerInterval();
        }
        catch(Exception ex)
        {
            previewMessage.Text="Animation render error: "+ex.Message;
        }
    }

    void UpdateTimerInterval()
    {
        if(animation==null)return;
        int actionIndex=CurrentActionIndex();
        if(actionIndex<0||actionIndex>=animation.Act.Actions.Count)return;
        decimal pct=Math.Max(25,speedPercent.Value);
        int delay=(int)Math.Round(animation.Act.Actions[actionIndex].DelayMs*100m/pct);
        animationTimer.Interval=Math.Clamp(delay,15,5000);
    }

    void UpdateAnimationControls(bool enabled)
    {
        animationMode.Enabled=enabled;
        direction.Enabled=enabled;
        btnPlay.Enabled=enabled;
        btnPrevFrame.Enabled=enabled;
        btnNextFrame.Enabled=enabled;
        speedPercent.Enabled=enabled;
        if(!enabled)
        {
            frameInfo.Text="Static preview";
            btnPlay.Text="Play";
            animationTimer.Stop();
        }
    }

    void SetPreviewImage(Bitmap? image)
    {
        var old=preview.Image;
        preview.Image=image;
        old?.Dispose();
    }

    void StopAndDisposeAnimation()
    {
        animationTimer.Stop();
        animation?.Dispose();
        animation=null;
        frameIndex=0;
    }

    void Choose(bool insertOnly)
    {
        if(SelectedModel==null)
        {
            MessageBox.Show("เลือก NPC model ก่อน","NPC Model Browser");
            return;
        }
        InsertOnly=insertOnly;
        DialogResult=DialogResult.OK;
        Close();
    }

    void CopyModel(){if(SelectedModel!=null)Clipboard.SetText(SelectedModel.Model);}
    void CopyId(){if(SelectedModel?.ClassId>=0)Clipboard.SetText(SelectedModel.ClassId.ToString());}

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        previewCancel?.Cancel();
        previewCancel?.Dispose();
        StopAndDisposeAnimation();
        SetPreviewImage(null);
        animationTimer.Dispose();
        base.OnFormClosed(e);
    }
}
