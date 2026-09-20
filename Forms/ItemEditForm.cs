using System.ComponentModel;
using AuxCodex.Models;
using AuxCodex.Services;
using AuxCodex.Utils;

namespace AuxCodex.Forms;

public partial class ItemEditForm : Form
{
    private readonly List<SessionEditData> _sessions;
    private readonly List<SecondaryProviderDefinition> _catalog;
    private readonly BatResumeKeyService _resume = new();
    private readonly BatWorkingDirectoryService _workingDirectory = new();
    private readonly ManagedBatPathService? _managedBatPaths;
    private int _selected = -1, _guard;
    private bool _loadingSession;
    private bool _adjustingSplitter;
    private bool _syncingProjectDirectory;
    private string _projectDirectoryDraft = "";
    private readonly List<ProviderEditor> _editors = new();
    private readonly List<TextBox> _projectDirectoryEditors = new();

    public ItemEditForm(string title, string initialName, string initialGptUrl, IReadOnlyList<SessionEditData>? sessions)
        : this(title, initialName, initialGptUrl, sessions, Array.Empty<SecondaryProviderDefinition>(), string.Empty, string.Empty, string.Empty) { }

    public ItemEditForm(string title, string initialName, string initialGptUrl, IReadOnlyList<SessionEditData>? sessions, IReadOnlyList<SecondaryProviderDefinition> catalog, string initialProjectDirectory = "", string initialOpenAiBatTemplate = "", string managedBatDirectory = "")
    {
        _catalog = catalog.ToList(); _sessions = (sessions ?? Array.Empty<SessionEditData>()).Select(x => x.Clone()).ToList(); _projectDirectoryDraft = initialProjectDirectory; NewSessionOpenAiBatContentTemplate = initialOpenAiBatTemplate; if (!string.IsNullOrWhiteSpace(managedBatDirectory)) _managedBatPaths = new ManagedBatPathService(managedBatDirectory);
        Icon = Utils.ApplicationIconProvider.Icon;
        Text = title; Width = 960; Height = 720; MinimumSize = new Size(930, 620); StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable; MaximizeBox = true; MinimizeBox = false; AutoScaleMode = AutoScaleMode.Font;
        BuildLayout(initialName, initialGptUrl, initialProjectDirectory); RefreshSessionList(_sessions.Count > 0 ? 0 : -1);
    }

    private void OnSessionSplitResize(object? sender, EventArgs e)
    {
        if (_adjustingSplitter || _sessionSplit.SplitterDistance == 285 || _sessionSplit.Width < 285 + _sessionSplit.Panel2MinSize)
        {
            return;
        }

        _adjustingSplitter = true;
        try { _sessionSplit.SplitterDistance = 285; }
        finally { _adjustingSplitter = false; }
    }

    public string ProjectName { get; private set; } = "";
    public string GptUrl { get; private set; } = "";
    public string ProjectDirectory { get; private set; } = "";
    public IReadOnlyList<SessionEditData> EditedSessions { get; private set; } = Array.Empty<SessionEditData>();
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public string NewSessionOpenAiBatContentTemplate { get; set; } = "";
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public Func<string?>? NameValidation { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] internal Func<string, string?>? ValidateProjectName { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] internal Func<ItemEditForm, string?>? SaveHandler { get; set; }

    private void BuildLayout(string initialName, string url, string projectDirectory)
    {
        var root = _rootLayout; root.Controls.Clear(); root.Dock = DockStyle.Fill; root.Padding = new Padding(14); root.ColumnCount = 1; root.RowCount = 4;
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42)); Controls.Add(root);
        var common = _commonLayout; common.Controls.Clear(); common.Dock = DockStyle.Fill; common.ColumnCount = 2; common.RowCount = 2; common.ColumnStyles.Clear(); common.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130)); common.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); common.RowStyles.Clear(); common.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); common.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        common.Controls.Add(new Label { Text = "Nome do projeto:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 0); _projectName.Text = initialName; _projectName.Dock = DockStyle.Fill; common.Controls.Add(_projectName, 1, 0);
        common.Controls.Add(new Label { Text = "URL do GPT:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 1); _url.Text = url; _url.Dock = DockStyle.Fill; common.Controls.Add(_url, 1, 1);
        root.Controls.Add(common, 0, 0);
        var split = _sessionSplit; split.Dock = DockStyle.Fill; split.FixedPanel = FixedPanel.Panel1; split.Panel1MinSize = 275; split.Panel2MinSize = 0; split.Resize -= OnSessionSplitResize; split.Resize += OnSessionSplitResize;
        var left = _leftLayout; left.Controls.Clear(); left.Dock = DockStyle.Fill; left.RowCount = 2; left.ColumnCount = 1; left.RowStyles.Clear(); left.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); left.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        _sessionList.Name = "sessionList"; _sessionList.Dock = DockStyle.Fill; _sessionList.HorizontalScrollbar = true; left.Controls.Add(_sessionList, 0, 0);
        var actions = _sessionActions; actions.Controls.Clear(); actions.Dock = DockStyle.Fill; actions.WrapContents = false; actions.AutoScroll = false; actions.Padding = new Padding(2, 3, 2, 2);
        ConfigureButton(_newSessionButton, "Nova", (_, _) => AddSession(), 70); ConfigureButton(_renameSessionButton, "Renomear", (_, _) => RenameSession(), 94); ConfigureButton(_removeSessionButton, "Remover", (_, _) => DeleteSession(), 84);
        actions.Controls.Add(_newSessionButton); actions.Controls.Add(_renameSessionButton); actions.Controls.Add(_removeSessionButton); left.Controls.Add(actions, 0, 1); split.Panel1.Controls.Add(left);
        var right = _rightLayout; right.Controls.Clear(); right.Dock = DockStyle.Fill; right.RowCount = 3; right.ColumnCount = 1; right.RowStyles.Clear(); right.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); right.RowStyles.Add(new RowStyle(SizeType.Absolute, 36)); right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var sessionHead = _sessionHeader; sessionHead.Controls.Clear(); sessionHead.Dock = DockStyle.Fill; sessionHead.Controls.Add(new Label { Text = "Sessão:", AutoSize = true, Padding = new Padding(0, 7, 8, 0) }); _sessionName.Name = "sessionName"; _sessionName.Width = 260; sessionHead.Controls.Add(_sessionName); right.Controls.Add(sessionHead, 0, 0);
        var providerActions = _providerActions; providerActions.Controls.Clear(); providerActions.Dock = DockStyle.Fill; ConfigureButton(_addSecondaryButton, "+ Adicionar secundária", (_, _) => AddSecondary(), 180); providerActions.Controls.Add(_addSecondaryButton); right.Controls.Add(providerActions, 0, 1);
        _tabs.Dock = DockStyle.Fill; right.Controls.Add(_tabs, 0, 2); split.Panel2.Controls.Add(right); root.Controls.Add(split, 0, 1);
        _error.ForeColor = Color.Firebrick; _error.Dock = DockStyle.Fill; root.Controls.Add(_error, 0, 2);
        var bottom = _bottomActions; bottom.Controls.Clear(); bottom.Dock = DockStyle.Fill; bottom.FlowDirection = FlowDirection.RightToLeft; ConfigureButton(_saveButton, "Salvar", OnSaveClick, 86); ConfigureButton(_cancelButton, "Cancelar", (_, _) => { DialogResult = DialogResult.Cancel; Close(); }, 86); bottom.Controls.Add(_saveButton); bottom.Controls.Add(_cancelButton); root.Controls.Add(bottom, 0, 3);
        _sessionList.SelectedIndexChanged += OnSessionChanged; _projectName.TextChanged += (_, _) => UpdateManagedBatPaths(); _sessionName.TextChanged += (_, _) => { if (!_loadingSession && _selected >= 0) { CommitCurrent(); _sessions[_selected].Name = _sessionName.Text; var index=_selected; _sessionList.Items[index]=_sessionName.Text; UpdateManagedBatPaths(); } };
    }

    private static void ConfigureButton(Button button, string text, EventHandler click, int width) { button.Text = text; button.Name = text.Replace("+ ", "").Replace(" ", "").ToLowerInvariant() + "Button"; button.AutoSize = false; button.Width = width; button.Height = 30; button.Padding = new Padding(5, 2, 5, 2); button.Margin = new Padding(3); button.Click -= click; button.Click += click; }
    private void RefreshSessionList(int selected) { _sessionList.Items.Clear(); foreach (var session in _sessions) _sessionList.Items.Add(session.Name); _selected = -1; if (selected >= 0 && selected < _sessions.Count) { _sessionList.SelectedIndex = selected; LoadSession(selected); } else { _sessionName.Clear(); _tabs.TabPages.Clear(); } }
    private void OnSessionChanged(object? sender, EventArgs e) { if (!_loadingSession && _sessionList.SelectedIndex >= 0 && _sessionList.SelectedIndex != _selected) { CommitCurrent(); LoadSession(_sessionList.SelectedIndex); } }
    private void LoadSession(int index)
    {
        _selected = index; var session = _sessions[index]; _loadingSession = true; _sessionName.Text = session.Name; _loadingSession = false; _tabs.TabPages.Clear(); _editors.Clear(); _projectDirectoryEditors.Clear();
        AddProviderTab("OpenAI", null, session.OpenAiResumeKey, session.OpenAi, session.OpenAiBatContent, string.IsNullOrWhiteSpace(NewSessionOpenAiBatContentTemplate) ? BatTemplateDefaults.OpenAi : NewSessionOpenAiBatContentTemplate);
        foreach (var secondary in session.SecondaryProviders.OrderBy(p=>_catalog.FirstOrDefault(d=>d.Id==p.ProviderDefinitionId)?.Name,StringComparer.CurrentCultureIgnoreCase)) AddProviderTab(secondary.ProviderName, secondary.ProviderDefinitionId, secondary.ResumeKey, secondary.LaunchConfiguration, secondary.BatContent, _catalog.FirstOrDefault(d=>d.Id==secondary.ProviderDefinitionId)?.BatTemplate ?? string.Empty);
    }
    private void AddProviderTab(string name, Guid? id, string key, ProviderLaunchConfiguration launch, string content, string template)
    {
        // Um BAT sem caminho, ausente ou vazio deve nascer do template. Um BAT físico
        // não vazio continua sendo a fonte do rascunho e não é reformatado.
        var generatedContent = string.IsNullOrWhiteSpace(launch.BatFilePath) || string.IsNullOrWhiteSpace(content);
        if (generatedContent) content = _resume.RemoveResumeKey(template);
        if (generatedContent) content = _workingDirectory.ApplyGeneratedProjectDirectory(content, _projectDirectoryDraft);
        if (generatedContent && !string.IsNullOrWhiteSpace(key)) content = _resume.ReplaceResumeKey(content, key);
        if (_resume.TryExtractResumeKey(content, out var batKey)) key = batKey;
        var page = new TabPage(name); var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(10), ColumnCount = 3, RowCount = 7 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var path = launch.BatFilePath;
        var managed = string.IsNullOrWhiteSpace(path);
        if (managed && _managedBatPaths is not null) path = _managedBatPaths.GetPath(_projectName.Text, _sessionName.Text, name, id is null, _sessions.Count);
        var state = new ProviderEditor { Name=name, DefinitionId=id, Template=template, ManagedPath=managed, GeneratedFromTemplate=generatedContent, Resume=new TextBox { Dock=DockStyle.Fill, Text=key }, Path=new TextBox { Dock=DockStyle.Fill, Text=path }, Admin=new CheckBox { Text="Executar como administrador", Checked=launch.RunAsAdministrator, AutoSize=true }, Content=new TextBox { Dock=DockStyle.Fill, Multiline=true, ScrollBars=ScrollBars.Both, AcceptsReturn=true, AcceptsTab=true, WordWrap=false, Font=new Font("Consolas", 10), Text=content } };
        layout.Controls.Add(new Label { Text="Chave de resume:", Dock=DockStyle.Fill, TextAlign=ContentAlignment.MiddleLeft },0,0); layout.Controls.Add(state.Resume,1,0);
        layout.Controls.Add(new Label { Text="Arquivo BAT:", Dock=DockStyle.Fill, TextAlign=ContentAlignment.MiddleLeft },0,1); layout.Controls.Add(state.Path,1,1);
        var batActions = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = Padding.Empty };
        var openExisting = new Button { Text = "Abrir...", AutoSize = true };
        openExisting.Click += (_, _) => SelectExistingBat(state);
        var chooseNew = new Button { Text = "Novo...", AutoSize = true };
        chooseNew.Click += (_, _) => SelectNewBatPath(state);
        batActions.Controls.Add(openExisting);
        batActions.Controls.Add(chooseNew);
        layout.Controls.Add(batActions,2,1);
        layout.Controls.Add(state.Admin,1,2);
        if(id.HasValue) { var transfer=new Button { Text="Usar contexto OpenAI", AutoSize=true }; transfer.Click += (_,_) => TransferContext(state); layout.Controls.Add(transfer,1,3); var remove=new Button { Text="Remover secundária", AutoSize=true }; remove.Click += (_,_) => RemoveSecondary(id.Value); layout.Controls.Add(remove,2,3); }
        var projectDirectoryRow = new TableLayoutPanel { Name = "projectDirectoryRow", Dock = DockStyle.Fill, Margin = Padding.Empty, Padding = Padding.Empty, ColumnCount = 3, RowCount = 1 };
        projectDirectoryRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
        projectDirectoryRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        projectDirectoryRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        projectDirectoryRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var projectDirectoryEditor = new TextBox { Name = "projectDirectoryTextBox", Dock = DockStyle.Fill, Text = _projectDirectoryDraft, Visible = true };
        var selectProjectDirectory = new Button { Name = "selectProjectDirectoryButton", Text = "Selecionar...", AutoSize = true, MinimumSize = new Size(100, 30), Margin = new Padding(6, 2, 0, 2), Visible = true };
        selectProjectDirectory.Click += (_, _) => SelectProjectDirectory(projectDirectoryEditor);
        projectDirectoryEditor.TextChanged += (_, _) => UpdateProjectDirectory(projectDirectoryEditor);
        _projectDirectoryEditors.Add(projectDirectoryEditor);
        projectDirectoryRow.Controls.Add(new Label { Name = "projectDirectoryLabel", Text = "Pasta do projeto:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        projectDirectoryRow.Controls.Add(projectDirectoryEditor, 1, 0);
        projectDirectoryRow.Controls.Add(selectProjectDirectory, 2, 0);
        layout.Controls.Add(projectDirectoryRow, 0, 4); layout.SetColumnSpan(projectDirectoryRow, 3);
        layout.Controls.Add(new Label { Text = "Conteúdo do BAT:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 5);
        layout.Controls.Add(state.Content,0,6); layout.SetColumnSpan(state.Content,3); page.Controls.Add(layout); _tabs.TabPages.Add(page); _editors.Add(state);
        state.Resume.TextChanged += (_,_) => { if (_guard>0)return; _guard++; try { EnsureTemplateContent(state); state.Content.Text=_resume.ReplaceResumeKey(state.Content.Text,state.Resume.Text.Trim()); } finally {_guard--;} };
        state.Content.TextChanged += (_,_) => { if (_guard>0)return; if(_resume.TryExtractResumeKey(state.Content.Text,out var found)){_guard++; try {state.Resume.Text=found;} finally {_guard--;}} };
        state.Path.TextChanged += (_, _) => { if (_guard == 0) state.ManagedPath = false; };
    }
    private void SelectExistingBat(ProviderEditor state)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Arquivos BAT (*.bat)|*.bat|Todos os arquivos (*.*)|*.*",
            CheckFileExists = true,
            CheckPathExists = true,
            Multiselect = false,
            FileName = state.Path.Text
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            var content = new BatFileService().ReadContent(dialog.FileName);
            state.ManagedPath = false;
            state.GeneratedFromTemplate = false;
            state.Path.Text = dialog.FileName;
            state.Content.Text = content;
            _error.Text = string.Empty;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or PathTooLongException or System.Security.SecurityException)
        {
            MessageBox.Show(this, "Não foi possível ler o arquivo BAT selecionado.", "AuxCodex", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void SelectNewBatPath(ProviderEditor state)
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "Arquivos BAT (*.bat)|*.bat|Todos os arquivos (*.*)|*.*",
            DefaultExt = "bat",
            AddExtension = true,
            CheckPathExists = true,
            OverwritePrompt = false,
            FileName = state.Path.Text
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            state.ManagedPath = false;
            state.Path.Text = dialog.FileName;
            _guard++;
            try
            {
                EnsureTemplateContent(state);
                state.Content.Text = state.GeneratedFromTemplate
                    ? _workingDirectory.ApplyGeneratedProjectDirectory(state.Content.Text, _projectDirectoryDraft)
                    : _workingDirectory.UpdateProjectDirectory(state.Content.Text, _projectDirectoryDraft);
            }
            finally { _guard--; }
        }
    }

    private void UpdateProjectDirectory(TextBox source)
    {
        if (_syncingProjectDirectory) return;
        _projectDirectoryDraft = source.Text;
        _syncingProjectDirectory = true;
        try
        {
            foreach (var editor in _projectDirectoryEditors)
                if (!ReferenceEquals(editor, source) && editor.Text != source.Text) editor.Text = source.Text;
            _guard++;
            try
            {
                foreach (var editor in _editors)
                {
                    EnsureTemplateContent(editor);
                    editor.Content.Text = editor.GeneratedFromTemplate
                        ? _workingDirectory.ApplyGeneratedProjectDirectory(editor.Content.Text, _projectDirectoryDraft)
                        : _workingDirectory.UpdateProjectDirectory(editor.Content.Text, _projectDirectoryDraft);
                }
            }
            finally { _guard--; }
        }
        finally { _syncingProjectDirectory = false; }
    }

    private void SelectProjectDirectory(TextBox target)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Selecione a pasta do projeto.",
            ShowNewFolderButton = false,
            SelectedPath = Directory.Exists(_projectDirectoryDraft.Trim()) ? _projectDirectoryDraft.Trim() : string.Empty
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            target.Text = dialog.SelectedPath;
        }
    }

    private void EnsureTemplateContent(ProviderEditor state)
    {
        if (state.Content.Text.Length == 0 && state.Template.Length > 0)
        {
            var template = _resume.RemoveResumeKey(state.Template);
            state.Content.Text = state.GeneratedFromTemplate
                ? _workingDirectory.ApplyGeneratedProjectDirectory(template, _projectDirectoryDraft)
                : template;
        }
    }

    private void UpdateManagedBatPaths()
    {
        if (_managedBatPaths is null || _editors.Count == 0 || _guard > 0) return;
        _guard++;
        try
        {
            var sessionName = _selected >= 0 && _selected < _sessions.Count ? _sessions[_selected].Name : _sessionName.Text;
            foreach (var state in _editors)
            {
                if (!state.ManagedPath) continue;
                state.Path.Text = _managedBatPaths.GetPath(_projectName.Text, sessionName, state.Name, state.DefinitionId is null, _sessions.Count);
            }
        }
        finally { _guard--; }
    }

    private void CommitCurrent()
    {
        if (_selected<0 || _selected>=_sessions.Count) return; var session=_sessions[_selected]; session.Name=_sessionName.Text.Trim();
        foreach(var state in _editors) { var config=new ProviderLaunchConfiguration { BatFilePath=state.Path.Text.Trim(), RunAsAdministrator=state.Admin.Checked };
            if(state.DefinitionId is null){session.OpenAi=config; session.OpenAiResumeKey=state.Resume.Text.Trim(); session.OpenAiBatContent=state.Content.Text;}
            else {var target=session.SecondaryProviders.FirstOrDefault(x=>x.ProviderDefinitionId==state.DefinitionId); if(target is not null){target.ProviderName=state.Name;target.ResumeKey=state.Resume.Text.Trim();target.LaunchConfiguration=config;target.BatContent=state.Content.Text;}}
        }
    }
    private void AddSession() { CommitCurrent(); var session=new SessionEditData { Name="Nova sessão", OpenAiBatContent=new BatResumeKeyService().RemoveResumeKey(NewSessionOpenAiBatContentTemplate) }; _sessions.Add(session); RefreshSessionList(_sessions.Count-1); }
    private void RenameSession() { if(_selected<0)return; _sessionName.Focus(); _sessionName.SelectAll(); }
    private void DeleteSession() { if(_selected<0)return; if(MessageBox.Show(this,$"Apagar a sessão '{_sessions[_selected].Name}'? Os BATs físicos serão mantidos.","Confirmar",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return; _sessions.RemoveAt(_selected); RefreshSessionList(Math.Min(_selected,_sessions.Count-1)); }
    private void AddSecondary()
    {
        if(_selected<0)return; var available=_catalog.Where(d=>_sessions[_selected].SecondaryProviders.All(p=>p.ProviderDefinitionId!=d.Id)).OrderBy(d=>d.Name).ToList(); if(available.Count==0){_error.Text="Não há provedores secundários disponíveis para adicionar.";return;}
        using var picker=new Form { Text="Adicionar secundária", Width=360, Height=150, StartPosition=FormStartPosition.CenterParent, FormBorderStyle=FormBorderStyle.FixedDialog };
        var list=new ComboBox { Dock=DockStyle.Top, DropDownStyle=ComboBoxStyle.DropDownList, DataSource=available, DisplayMember=nameof(SecondaryProviderDefinition.Name) }; var ok=new Button { Text="Adicionar", DialogResult=DialogResult.OK, Dock=DockStyle.Bottom }; picker.Controls.Add(list);picker.Controls.Add(ok);picker.AcceptButton=ok;
        if(picker.ShowDialog(this)!=DialogResult.OK || list.SelectedItem is not SecondaryProviderDefinition def)return; CommitCurrent(); _sessions[_selected].SecondaryProviders.Add(new SessionSecondaryProviderEditData { ProviderDefinitionId=def.Id, ProviderName=def.Name, LaunchConfiguration=new(), BatContent=_resume.RemoveResumeKey(def.BatTemplate) }); LoadSession(_selected);
    }
    private void RemoveSecondary(Guid id) { if(_selected<0)return; var p=_sessions[_selected].SecondaryProviders.FirstOrDefault(x=>x.ProviderDefinitionId==id); if(p is null)return; if(MessageBox.Show(this,$"Remover {p.ProviderName} desta sessão? O arquivo BAT será mantido.","Confirmar",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return; CommitCurrent(); _sessions[_selected].SecondaryProviders.RemoveAll(x=>x.ProviderDefinitionId==id);LoadSession(_selected); }
    private void TransferContext(ProviderEditor target)
    {
        CommitCurrent(); var session=_sessions[_selected]; var source=session.OpenAiResumeKey.Trim(); if(source.Length==0){_error.Text="Não há chave de resume OpenAI para transferir.";return;}
        if(source==target.Resume.Text.Trim()){_error.Text=$"{target.Name} já usa esse contexto.";return;}
        var msg=$"Deseja substituir o contexto atual de {target.Name} pelo contexto do OpenAI?"+(target.Resume.Text.Length>0?$"{Environment.NewLine}A chave atual de {target.Name} será substituída.":"");
        if(MessageBox.Show(this,msg,"Transferir contexto",MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)!=DialogResult.Yes)return;
        target.Resume.Text=source; if(!_resume.TryExtractResumeKey(target.Content.Text,out _))_error.Text=$"A chave foi definida, mas o BAT {target.Name} não contém um comando resume para atualizar.";
    }
    private void OnSaveClick(object? sender,EventArgs e)
    {
        UpdateManagedBatPaths(); CommitCurrent(); _error.Text=""; ProjectName=_projectName.Text.Trim(); GptUrl=_url.Text.Trim();
        if(ProjectName.Length==0){_error.Text="Informe o nome do projeto.";return;} var conflict=ValidateProjectName?.Invoke(ProjectName); if(conflict is not null){_error.Text=conflict;return;}
        var projectDirectory = _projectDirectoryDraft.Trim();
        if (projectDirectory.Length > 0)
        {
            try
            {
                projectDirectory = Path.GetFullPath(projectDirectory);
                if (!Directory.Exists(projectDirectory)) { _error.Text = "A pasta do projeto não existe."; return; }
            }
            catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException or System.Security.SecurityException)
            {
                _error.Text = "Informe uma pasta de projeto válida.";
                return;
            }
        }
        ProjectDirectory = projectDirectory;
        if(GptUrl.Length>0 && (!Uri.TryCreate(GptUrl,UriKind.Absolute,out var uri)|| (uri.Scheme!="http"&&uri.Scheme!="https"))){_error.Text="Informe uma URL HTTP ou HTTPS válida.";return;}
        foreach(var editor in _editors) if(!ValidBatPath(editor.Path.Text)){_error.Text=$"O caminho BAT de {editor.Name} deve terminar em .bat e apontar para uma pasta existente.";return;}
        var pathConflict=BatPathConflictValidator.Validate(_sessions); if(pathConflict is not null){_error.Text=pathConflict;return;}
        EditedSessions=_sessions.Select(x=>x.Clone()).ToList(); var saveError=SaveHandler?.Invoke(this); if(saveError is not null){_error.Text=saveError;return;} DialogResult=DialogResult.OK; Close();
    }
    private static bool ValidBatPath(string path) { if(string.IsNullOrWhiteSpace(path))return true; try { var full=Path.GetFullPath(path.Trim()); return string.Equals(Path.GetExtension(full),".bat",StringComparison.OrdinalIgnoreCase) && Directory.Exists(Path.GetDirectoryName(full)); } catch(Exception ex) when(ex is ArgumentException or NotSupportedException or PathTooLongException or System.Security.SecurityException){return false;} }
    private sealed class ProviderEditor { public string Name=""; public string Template=""; public bool ManagedPath; public bool GeneratedFromTemplate; public Guid? DefinitionId; public TextBox Resume=null!,Path=null!,Content=null!; public CheckBox Admin=null!; }
}
