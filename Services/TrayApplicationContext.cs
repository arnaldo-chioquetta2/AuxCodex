using System.Drawing;
using System.Text.Json;
using AuxCodex.Forms;
using AuxCodex.Models;
using AuxCodex.Utils;

namespace AuxCodex.Services;

/// <summary>
/// Controls the lifetime of the notification-area application.
/// </summary>
internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly ContextMenuStrip _trayMenu;
    private readonly NotifyIcon _notifyIcon;
    private readonly TrayMenuBuilder _trayMenuBuilder;
    private readonly ConfigurationService _configurationService;
    private readonly MenuTreeService _menuTreeService;
    private readonly BatFileService _batFileService;
    private readonly IFileSystemService _fileSystem;
    private readonly BatResumeKeyService _resumeKeyService;
    private readonly ProviderLaunchService _providerLaunchService;
    private readonly ActivityLogService _activityLog;
    private readonly Icon _trayIcon;
    private ContextMenuStrip? _folderContextMenu;
    private bool _trayResourcesDisposed;

    public TrayApplicationContext(
        AppConfiguration configuration,
        ConfigurationService configurationService,
        bool testMode = false,
        ActivityLogService? activityLogService = null)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _trayMenu = new ContextMenuStrip();
        _trayMenuBuilder = new TrayMenuBuilder();
        _menuTreeService = new MenuTreeService();
        _fileSystem = configurationService.FileSystem;
        _batFileService = new BatFileService(_fileSystem);
        _resumeKeyService = new BatResumeKeyService();
        _providerLaunchService = new ProviderLaunchService(new BrowserLaunchService());
        _activityLog = activityLogService ?? configurationService.ActivityLog ?? new ActivityLogService();

        Icon? extractedIcon = null;
        try
        {
            extractedIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        }
        catch (Exception exception) when (exception is IOException or ArgumentException or System.ComponentModel.Win32Exception)
        {
            // O ícone padrão mantém o tray funcional se o executável não expuser um ícone.
        }

        extractedIcon?.Dispose();
        _trayIcon = ApplicationIconProvider.Icon;

        _notifyIcon = new NotifyIcon
        {
            Icon = _trayIcon,
            Text = testMode ? "AuxCodex - Teste" : "AuxCodex",
            Visible = true
        };

        _notifyIcon.MouseClick += OnNotifyIconMouseClick;
        RebuildTrayMenu();
        _activityLog.Info("Bandeja inicializada.");
    }

    public AppConfiguration Configuration { get; private set; }

    public void RebuildTrayMenu()
    {
        foreach (ToolStripItem menuItem in _trayMenu.Items.Cast<ToolStripItem>().ToArray())
        {
            _trayMenu.Items.Remove(menuItem);
            menuItem.Dispose();
        }

        var dynamicItems = _trayMenuBuilder.BuildRootItems(
            Configuration,
            OnMenuItemMouseUp);
        if (dynamicItems.Count == 0)
        {
            _trayMenu.Items.Add(new ToolStripMenuItem("(Nenhum item configurado)")
            {
                Enabled = false
            });
        }
        else
        {
            _trayMenu.Items.AddRange(dynamicItems.ToArray());
        }

        _trayMenu.Items.Add(CreateRootManagementMenuItem());
        _trayMenu.Items.Add(new ToolStripSeparator());
        var exitMenuItem = new ToolStripMenuItem("Sair");
        exitMenuItem.Click += OnExitMenuItemClick;
        _trayMenu.Items.Add(exitMenuItem);
        _activityLog.Info("Menu da bandeja reconstruído.");
    }

    protected override void ExitThreadCore()
    {
        DisposeTrayResources();
        base.ExitThreadCore();
    }

    private void OnExitMenuItemClick(object? sender, EventArgs e)
    {
        ExitApplication();
    }

    internal void ExitApplication()
    {
        _activityLog.Info("Aplicação solicitou encerramento.");
        DisposeTrayResources();
        ExitThread();
        Application.ExitThread();
        Application.Exit();
    }

    internal void InvokeExitMenuForTest()
    {
        var exitItem = _trayMenu.Items.OfType<ToolStripMenuItem>().First(item => string.Equals(item.Text, "Sair", StringComparison.Ordinal));
        exitItem.PerformClick();
    }

    private ToolStripMenuItem CreateRootManagementMenuItem()
    {
        var rootMenuItem = new ToolStripMenuItem("Gerenciar raiz")
        {
            Tag = new TrayMenuEntryTag(Guid.Empty, TrayMenuEntryType.Root)
        };

        var createFolderMenuItem = new ToolStripMenuItem("Criar pasta")
        {
            Tag = rootMenuItem.Tag
        };
        createFolderMenuItem.Click += OnCreateFolderMenuItemClick;

        var createItemMenuItem = new ToolStripMenuItem("Criar item")
        {
            Enabled = true,
            Tag = rootMenuItem.Tag
        };
        createItemMenuItem.Click += OnCreateItemMenuItemClick;

        var providersMenuItem = new ToolStripMenuItem("Provedores...");
        providersMenuItem.Click += OnSecondaryProvidersMenuItemClick;
        rootMenuItem.DropDownItems.Add(providersMenuItem);
        rootMenuItem.DropDownItems.Add(new ToolStripSeparator());
        rootMenuItem.DropDownItems.Add(createFolderMenuItem);
        rootMenuItem.DropDownItems.Add(createItemMenuItem);
        return rootMenuItem;
    }

    private void OnSecondaryProvidersMenuItemClick(object? sender, EventArgs e)
    {
        using var form = new SecondaryProvidersForm(Configuration, _configurationService);
        form.ShowDialog();
        RebuildTrayMenu();
    }
    private void OnNotifyIconMouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && !_trayResourcesDisposed)
        {
            _trayMenu.Show(Cursor.Position);
        }
    }

    private void OnMenuItemMouseUp(object? sender, MouseEventArgs e)
    {
        if (sender is not ToolStripMenuItem menuItem || menuItem.Tag is not TrayMenuEntryTag entryTag)
        {
            return;
        }

        if (e.Button == MouseButtons.Left &&
            (entryTag.EntryType == TrayMenuEntryType.OpenAiAction || entryTag.EntryType == TrayMenuEntryType.SecondaryProviderAction))
        {
            OnConfiguredItemClick(menuItem, EventArgs.Empty);
            return;
        }

        if (e.Button != MouseButtons.Right)
        {
            return;
        }

        if (entryTag.EntryType == TrayMenuEntryType.Folder)
        {
            ShowFolderContextMenu(entryTag.Id);
        }
        else if (entryTag.EntryType == TrayMenuEntryType.Item)
        {
            ShowProjectContextMenu(entryTag.Id);
        }
        else if ((entryTag.EntryType == TrayMenuEntryType.OpenAiAction || entryTag.EntryType == TrayMenuEntryType.SecondaryProviderAction) &&
                 entryTag.ProjectId is Guid projectId)
        {
            ShowProjectContextMenu(projectId);
        }
    }

    private void ShowFolderContextMenu(Guid folderId)
    {
        DisposeFolderContextMenu();

        var contextMenu = new ContextMenuStrip();
        _folderContextMenu = contextMenu;

        var createFolderMenuItem = new ToolStripMenuItem("Criar pasta")
        {
            Tag = new TrayMenuEntryTag(folderId, TrayMenuEntryType.Folder)
        };
        createFolderMenuItem.Click += OnCreateFolderMenuItemClick;

        var createItemMenuItem = new ToolStripMenuItem("Criar item")
        {
            Enabled = true,
            Tag = new TrayMenuEntryTag(folderId, TrayMenuEntryType.Folder)
        };
        createItemMenuItem.Click += OnCreateItemMenuItemClick;

        var renameMenuItem = new ToolStripMenuItem("Renomear")
        {
            Tag = new TrayMenuEntryTag(folderId, TrayMenuEntryType.Folder)
        };
        renameMenuItem.Click += OnRenameFolderMenuItemClick;

        var moveMenuItem = new ToolStripMenuItem("Mover")
        {
            Enabled = true,
            Tag = new TrayMenuEntryTag(folderId, TrayMenuEntryType.Folder)
        };
        moveMenuItem.Click += OnMoveFolderMenuItemClick;

        var deleteMenuItem = new ToolStripMenuItem("Apagar")
        {
            Tag = new TrayMenuEntryTag(folderId, TrayMenuEntryType.Folder)
        };
        deleteMenuItem.Click += OnDeleteFolderMenuItemClick;

        contextMenu.Items.Add(createFolderMenuItem);
        contextMenu.Items.Add(createItemMenuItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(renameMenuItem);
        contextMenu.Items.Add(moveMenuItem);
        contextMenu.Items.Add(deleteMenuItem);
        contextMenu.Closed += OnFolderContextMenuClosed;
        contextMenu.Show(Cursor.Position);
    }

    private void ShowProjectContextMenu(Guid projectId)
    {
        DisposeFolderContextMenu();

        var contextMenu = new ContextMenuStrip();
        _folderContextMenu = contextMenu;

        var editMenuItem = new ToolStripMenuItem("Editar")
        {
            Tag = new TrayMenuEntryTag(projectId, TrayMenuEntryType.Item)
        };
        editMenuItem.Click += OnEditItemMenuItemClick;

        var moveMenuItem = new ToolStripMenuItem("Mover")
        {
            Enabled = true,
            Tag = new TrayMenuEntryTag(projectId, TrayMenuEntryType.Item)
        };
        moveMenuItem.Click += OnMoveItemMenuItemClick;

        var deleteMenuItem = new ToolStripMenuItem("Apagar")
        {
            Tag = new TrayMenuEntryTag(projectId, TrayMenuEntryType.Item)
        };
        deleteMenuItem.Click += OnDeleteItemMenuItemClick;

        contextMenu.Items.Add(editMenuItem);
        contextMenu.Items.Add(deleteMenuItem);
        contextMenu.Items.Add(moveMenuItem);
        contextMenu.Closed += OnFolderContextMenuClosed;
        contextMenu.Show(Cursor.Position);
    }

    private void OnFolderContextMenuClosed(object? sender, ToolStripDropDownClosedEventArgs e)
    {
        if (ReferenceEquals(sender, _folderContextMenu))
        {
            DisposeFolderContextMenu();
        }
    }

    private void OnCreateFolderMenuItemClick(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem menuItem ||
            menuItem.Tag is not TrayMenuEntryTag entryTag)
        {
            return;
        }

        var parentFolder = entryTag.EntryType == TrayMenuEntryType.Root
            ? null
            : _menuTreeService.FindFolder(Configuration, entryTag.Id);

        if (entryTag.EntryType != TrayMenuEntryType.Root && parentFolder is null)
        {
            ShowOperationError("A pasta selecionada não foi encontrada.");
            return;
        }

        using var dialog = new FolderNameForm("Nova pasta");
        dialog.Validation = name => _menuTreeService.HasNameConflict(Configuration, parentFolder, name, null)
            ? "Já existe uma pasta ou item com esse nome neste local."
            : null;

        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        var newFolder = new MenuFolder { Name = dialog.FolderName };
        GetFolderCollection(parentFolder).Add(newFolder);
        TrySaveAndRebuild($"Pasta criada: '{newFolder.Name}'.");
    }

    private void OnRenameFolderMenuItemClick(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem menuItem ||
            menuItem.Tag is not TrayMenuEntryTag { EntryType: TrayMenuEntryType.Folder } entryTag)
        {
            return;
        }

        var folder = _menuTreeService.FindFolder(Configuration, entryTag.Id);
        if (folder is null)
        {
            ShowOperationError("A pasta selecionada não foi encontrada.");
            return;
        }

        var parentFolder = _menuTreeService.FindParentFolder(Configuration, folder.Id);
        using var dialog = new FolderNameForm("Renomear pasta", folder.Name);
        dialog.Validation = name => _menuTreeService.HasNameConflict(Configuration, parentFolder, name, folder.Id)
            ? "Ja existe uma pasta ou item com esse nome neste local."
            : null;

        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        folder.Name = dialog.FolderName;
        TrySaveAndRebuild($"Pasta renomeada: '{folder.Name}'.");
    }

    private void OnMoveFolderMenuItemClick(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem menuItem ||
            menuItem.Tag is not TrayMenuEntryTag { EntryType: TrayMenuEntryType.Folder } entryTag)
        {
            return;
        }

        var folder = _menuTreeService.FindFolder(Configuration, entryTag.Id);
        if (folder is null)
        {
            ShowOperationError("A pasta selecionada não foi encontrada.");
            return;
        }

        var parentFolder = _menuTreeService.FindParentFolder(Configuration, folder.Id);
        var destinations = _menuTreeService.BuildFolderDestinationTree(Configuration, folder.Id);
        using var dialog = new MoveEntryForm(
            "Mover pasta",
            $"Mover pasta \"{folder.Name}\" para:",
            destinations,
            parentFolder?.Id);

        if (dialog.ShowDialog() != DialogResult.OK || !dialog.HasSelection)
        {
            return;
        }

        ReportMoveResult(
            _menuTreeService.MoveFolder(
                Configuration,
                folder,
                dialog.SelectedDestinationFolderId,
                () => _configurationService.Save(Configuration)),
            "pasta");
    }

    private void OnMoveItemMenuItemClick(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem menuItem ||
            menuItem.Tag is not TrayMenuEntryTag { EntryType: TrayMenuEntryType.Item } entryTag)
        {
            return;
        }

        var item = _menuTreeService.FindItem(Configuration, entryTag.Id);
        if (item is null)
        {
            ShowOperationError("O projeto selecionado não foi encontrado.");
            return;
        }

        var parentFolder = _menuTreeService.FindItemParent(Configuration, item.Id);
        var destinations = _menuTreeService.BuildFolderDestinationTree(Configuration);
        using var dialog = new MoveEntryForm(
            "Mover projeto",
            $"Mover \"{item.Name}\" para:",
            destinations,
            parentFolder?.Id);

        if (dialog.ShowDialog() != DialogResult.OK || !dialog.HasSelection)
        {
            return;
        }

        ReportMoveResult(
            _menuTreeService.MoveItem(
                Configuration,
                item,
                dialog.SelectedDestinationFolderId,
                () => _configurationService.Save(Configuration)),
            "projeto");
    }

    private void OnMoveRootItemMenuItemClick(object? sender, EventArgs e)
    {
        var rootItems = Configuration.GetItemsOrdered().ToList();
        if (rootItems.Count == 0)
        {
            ShowOperationError("Não há projetos no nível raiz.");
            return;
        }

        var item = rootItems[0];
        var destinations = _menuTreeService.BuildFolderDestinationTree(Configuration);
        using var dialog = new MoveEntryForm(
            "Mover projeto",
            $"Mover \"{item.Name}\" para:",
            destinations,
            null);

        if (dialog.ShowDialog() != DialogResult.OK || !dialog.HasSelection)
        {
            return;
        }

        ReportMoveResult(
            _menuTreeService.MoveItem(
                Configuration,
                item,
                dialog.SelectedDestinationFolderId,
                () => _configurationService.Save(Configuration)),
            "projeto");
    }

    private void ReportMoveResult(MoveResult result, string entryKind)
    {
        switch (result.Outcome)
        {
            case MoveOutcome.Moved:
                _activityLog.Info($"{entryKind} movido.");
                RebuildTrayMenu();
                break;
            case MoveOutcome.SameLocation:
                ShowOperationError($"O {entryKind} ja esta nesse local.");
                break;
            case MoveOutcome.DuplicateName:
            case MoveOutcome.Cycle:
            case MoveOutcome.Invalid:
            ShowOperationError(result.ErrorMessage ?? "A movimentação não pode ser concluída.");
                break;
        }
    }

    private void OnCreateItemMenuItemClick(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem menuItem ||
            menuItem.Tag is not TrayMenuEntryTag entryTag)
        {
            return;
        }

        var parentFolder = entryTag.EntryType == TrayMenuEntryType.Root
            ? null
            : _menuTreeService.FindFolder(Configuration, entryTag.Id);

        if (entryTag.EntryType != TrayMenuEntryType.Root && parentFolder is null)
        {
            ShowOperationError("A pasta selecionada não foi encontrada.");
            return;
        }

        OpenItemEditor(null, parentFolder);
    }

    private void OnEditItemMenuItemClick(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem menuItem ||
            menuItem.Tag is not TrayMenuEntryTag { EntryType: TrayMenuEntryType.Item } entryTag)
        {
            return;
        }

        var item = _menuTreeService.FindItem(Configuration, entryTag.Id);
        if (item is null)
        {
            ShowOperationError("O projeto selecionado não foi encontrado.");
            return;
        }

        OpenItemEditor(item, _menuTreeService.FindItemParent(Configuration, item.Id));
    }

    private void OpenItemEditor(MenuItem? item, MenuFolder? parentFolder)
    {
        var isNew = item is null;
        var openAiTemplate = string.IsNullOrWhiteSpace(Configuration.OpenAiBatTemplate)
            ? (string.IsNullOrWhiteSpace(Configuration.LastCreatedOpenAiBatContent) ? BatTemplateDefaults.OpenAi : Configuration.LastCreatedOpenAiBatContent)
            : Configuration.OpenAiBatTemplate;
        var edits = isNew ? new List<SessionEditData> { new() { Name = "Principal", OpenAiBatContent = _resumeKeyService.RemoveResumeKey(openAiTemplate) } } :
            item!.GetSessionsOrdered().Select(session => new SessionEditData
            {
                Id=session.Id, Name=session.Name, OpenAiResumeKey=session.OpenAiResumeKey, OpenAi=CloneProvider(session.OpenAi),
                OpenAiBatContent=ReadProviderContent(session.OpenAi,"OpenAI"),
                SecondaryProviders=(session.SecondaryProviders ?? new()).Select(usage =>
                {
                    var definition=Configuration.SecondaryProviders.FirstOrDefault(d=>d.Id==usage.ProviderDefinitionId);
                    return new SessionSecondaryProviderEditData { ProviderDefinitionId=usage.ProviderDefinitionId, ProviderName=definition?.Name ?? "Provedor", ResumeKey=usage.ResumeKey,
                        LaunchConfiguration=CloneProvider(usage.LaunchConfiguration), BatContent=ReadProviderContent(usage.LaunchConfiguration,definition?.Name ?? "Provedor") };
                }).ToList()
            }).ToList();
        var managedBatDirectory = Path.Combine(Path.GetDirectoryName(_configurationService.ConfigurationFilePath) ?? AppContext.BaseDirectory, "Bats");
        using var dialog=new ItemEditForm(isNew?"Novo projeto":"Editar projeto",item?.Name??"",item?.GptUrl??"",edits,Configuration.SecondaryProviders,item?.ProjectDirectory??"",openAiTemplate,managedBatDirectory);
        dialog.NewSessionOpenAiBatContentTemplate=openAiTemplate;
        dialog.ValidateProjectName=name=>_menuTreeService.HasNameConflict(Configuration,parentFolder,name,item?.Id)?"Já existe uma pasta ou projeto com esse nome neste local.":null;
        dialog.SaveHandler=form=>SaveItem(form,item,parentFolder); dialog.ShowDialog();
    }

    private string ReadProviderContent(ProviderLaunchConfiguration provider,string providerName)
    {
        if(string.IsNullOrWhiteSpace(provider.BatFilePath))return "";
        if(!_fileSystem.FileExists(provider.BatFilePath)) { _activityLog.Info($"BAT inexistente; rascunho será composto para {providerName}."); return ""; }
        try
        {
            var content = _batFileService.ReadContent(provider.BatFilePath);
            _activityLog.Info($"BAT carregado: {provider.BatFilePath}.");
            return content;
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException)
        {
            _activityLog.Error($"Falha ao ler BAT do provedor {providerName}.", ex);
            ShowOperationError($"Não foi possível ler o BAT do provedor {providerName}.");
            return "";
        }
    }

    private string? SaveItem(ItemEditForm form,MenuItem? existingItem,MenuFolder? parentFolder)
    {
        var edited=form.EditedSessions; var item=existingItem??new MenuItem(); var previous=CloneSessions(item.Sessions); var oldName=item.Name;var oldUrl=item.GptUrl;var oldProjectDirectory=item.ProjectDirectory;var oldHistory=Configuration.LastCreatedOpenAiBatContent;var added=false;var snapshots=new List<BatFileSnapshot>();
        try
        {
            foreach(var session in edited){CaptureBatFile(snapshots,session.OpenAi.BatFilePath);foreach(var p in session.SecondaryProviders)CaptureBatFile(snapshots,p.LaunchConfiguration.BatFilePath);}
            foreach(var session in edited){WriteBatContent(session.OpenAi.BatFilePath,session.OpenAiBatContent);foreach(var p in session.SecondaryProviders)WriteBatContent(p.LaunchConfiguration.BatFilePath,p.BatContent);}
            item.Name=form.ProjectName;item.GptUrl=form.GptUrl;item.ProjectDirectory=form.ProjectDirectory;item.Sessions=edited.Select(CreateSession).ToList();
            if(existingItem is null){GetItemCollection(parentFolder).Add(item);added=true;if(edited.Count>0)Configuration.LastCreatedOpenAiBatContent=_resumeKeyService.RemoveResumeKey(edited[0].OpenAiBatContent);}
            var validationErrors=ConfigurationValidator.Validate(Configuration); if(validationErrors.Count>0) throw new InvalidOperationException(validationErrors[0]);
            _configurationService.Save(Configuration);
        }
        catch(Exception exception){if(added)GetItemCollection(parentFolder).Remove(item);item.Name=oldName;item.GptUrl=oldUrl;item.ProjectDirectory=oldProjectDirectory;item.Sessions=previous;Configuration.LastCreatedOpenAiBatContent=oldHistory;var rollbackOk=RestoreBatFiles(snapshots,out var rollbackError);var message=exception is InvalidOperationException ? exception.Message : "Não foi possível salvar o projeto ou os arquivos BAT. Verifique os caminhos e tente novamente.";if(!rollbackOk)message+=" A restauração dos arquivos não foi concluída: "+rollbackError;return message;}
        _activityLog.Info($"Projeto {(existingItem is null ? "criado" : "editado")}: '{item.Name}', sessões: {edited.Count}.");
        RebuildTrayMenu();return null;
    }

    private static ProjectSession CreateSession(SessionEditData data)=>new(){Id=data.Id==Guid.Empty?Guid.NewGuid():data.Id,Name=data.Name.Trim(),OpenAiResumeKey=data.OpenAiResumeKey.Trim(),OpenAi=CloneProvider(data.OpenAi),SecondaryProviders=data.SecondaryProviders.Select(p=>new SessionSecondaryProvider{ProviderDefinitionId=p.ProviderDefinitionId,ResumeKey=p.ResumeKey.Trim(),LaunchConfiguration=CloneProvider(p.LaunchConfiguration)}).ToList()};
    private static List<ProjectSession> CloneSessions(IEnumerable<ProjectSession>? sessions)=>(sessions??Enumerable.Empty<ProjectSession>()).Select(session=>new ProjectSession{Id=session.Id,Name=session.Name,OpenAiResumeKey=session.OpenAiResumeKey,OpenAi=CloneProvider(session.OpenAi),SecondaryProviders=(session.SecondaryProviders??new()).Select(p=>new SessionSecondaryProvider{ProviderDefinitionId=p.ProviderDefinitionId,ResumeKey=p.ResumeKey,LaunchConfiguration=CloneProvider(p.LaunchConfiguration)}).ToList()}).ToList();
    private static ProviderLaunchConfiguration CloneProvider(ProviderLaunchConfiguration? provider)=>new(){BatFilePath=provider?.BatFilePath??"",RunAsAdministrator=provider?.RunAsAdministrator??false};
    private void CaptureBatFile(ICollection<BatFileSnapshot> snapshots, string? path)
    {
        if (string.IsNullOrWhiteSpace(path) ||
            snapshots.Any(snapshot => string.Equals(snapshot.Path, path, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var existed = _fileSystem.FileExists(path);
        snapshots.Add(new BatFileSnapshot(path, existed, existed ? _fileSystem.ReadAllBytes(path) : null));
    }

    private void WriteBatContent(string? path, string content)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var parent = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(parent)) _fileSystem.CreateDirectory(parent);
        var existed = _fileSystem.FileExists(path);
        _batFileService.WriteContent(path, content);
        _activityLog.Info($"BAT {(existed ? "salvo" : "criado")}: {path}.");
    }

    private bool RestoreBatFiles(IEnumerable<BatFileSnapshot> snapshots, out string? error)
    {
        error = null;
        var success = true;
        foreach (var snapshot in snapshots)
        {
            try
            {
                if (snapshot.Existed && snapshot.Bytes is not null) _fileSystem.WriteAllBytes(snapshot.Path, snapshot.Bytes);
                else if (!snapshot.Existed && _fileSystem.FileExists(snapshot.Path)) _fileSystem.Delete(snapshot.Path);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
            {
                success = false;
                error ??= $"{snapshot.Path}: {exception.Message}";
            }
        }
        return success;
    }
    private sealed record BatFileSnapshot(string Path, bool Existed, byte[]? Bytes);
    private IList<MenuItem> GetItemCollection(MenuFolder? parentFolder) =>
        parentFolder?.Items ?? Configuration.Items;

    private void OnDeleteItemMenuItemClick(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem menuItem ||
            menuItem.Tag is not TrayMenuEntryTag { EntryType: TrayMenuEntryType.Item } entryTag)
        {
            return;
        }

        var item = _menuTreeService.FindItem(Configuration, entryTag.Id);
        if (item is null)
        {
            ShowOperationError("O projeto selecionado não foi encontrado.");
            return;
        }

        var result = MessageBox.Show(
            $"Deseja realmente apagar o projeto '{item.Name}'? Os arquivos BAT físicos serão mantidos.",
            "Confirmar exclusão",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
        if (result != DialogResult.Yes)
        {
            return;
        }

        var collection = _menuTreeService.GetItemCollection(Configuration, item.Id);
        if (!collection.Remove(item))
        {
            ShowOperationError("Não foi possível apagar o projeto.");
            return;
        }

        TrySaveAndRebuild($"Projeto removido da configuração: '{item.Name}'.");
    }

    private void OnDeleteFolderMenuItemClick(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem menuItem ||
            menuItem.Tag is not TrayMenuEntryTag { EntryType: TrayMenuEntryType.Folder } entryTag)
        {
            return;
        }

        var folder = _menuTreeService.FindFolder(Configuration, entryTag.Id);
        if (folder is null)
        {
            ShowOperationError("A pasta selecionada não foi encontrada.");
            return;
        }

        var hasContent = (folder.Folders?.Count > 0) || (folder.Items?.Count > 0);
        var message = hasContent
            ? $"A pasta '{folder.Name}' contém subpastas ou itens. Deseja realmente apagar a pasta e todo o seu conteúdo?"
            : $"Deseja realmente apagar a pasta '{folder.Name}'?";
        var result = MessageBox.Show(
            message,
            "Confirmar exclusão",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        if (result != DialogResult.Yes)
        {
            return;
        }

        var parentFolder = _menuTreeService.FindParentFolder(Configuration, folder.Id);
        var collection = GetFolderCollection(parentFolder);
        if (!collection.Remove(folder))
        {
            ShowOperationError("Não foi possível apagar a pasta.");
            return;
        }

        TrySaveAndRebuild($"Pasta removida da configuração: '{folder.Name}'.");
    }

    private IList<MenuFolder> GetFolderCollection(MenuFolder? parentFolder) =>
        parentFolder?.Folders ?? Configuration.Folders;

    private void TrySaveAndRebuild(string? activity = null)
    {
        try
        {
            _configurationService.Save(Configuration);
            if (!string.IsNullOrWhiteSpace(activity)) _activityLog.Info(activity);
            RebuildTrayMenu();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            try
            {
                Configuration = _configurationService.Load();
                RebuildTrayMenu();
            }
            catch (Exception reloadException) when (reloadException is IOException or UnauthorizedAccessException or JsonException)
            {
                // Keep the current menu alive if even the recovery read is unavailable.
            }

            _activityLog.Error("Falha ao salvar alteração operacional; configuração anterior foi restaurada.", exception);
            ShowOperationError("Não foi possível salvar a alteração. A configuração anterior foi mantida.");
        }
    }

    private static void ShowOperationError(string message)
    {
        MessageBox.Show(message, "AuxCodex", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void OnConfiguredItemClick(object? sender, EventArgs e)
    {
        if(sender is not ToolStripMenuItem menu || menu.Tag is not TrayMenuEntryTag tag || (tag.EntryType!=TrayMenuEntryType.OpenAiAction && tag.EntryType!=TrayMenuEntryType.SecondaryProviderAction))return;
        var project=tag.ProjectId is Guid projectId?_menuTreeService.FindItem(Configuration,projectId):null;
        var session=project?.FindSession(tag.SessionId??Guid.Empty);
        if(project is null||session is null){ShowOperationError("O projeto ou sessão selecionados não foram encontrados.");return;}
        ProviderLaunchConfiguration? launch;string providerName;
        if(tag.IsOpenAi){launch=session.OpenAi;providerName="OpenAI";}
        else
        {
            var definition=Configuration.SecondaryProviders.FirstOrDefault(d=>d.Id==tag.ProviderDefinitionId);
            var usage=session.SecondaryProviders.FirstOrDefault(p=>p.ProviderDefinitionId==tag.ProviderDefinitionId);
            if(definition is null||usage is null){ShowOperationError("A configuração deste provedor não está disponível.");return;}
            launch=usage.LaunchConfiguration;providerName=definition.Name;
        }
        _activityLog.Info($"Execução solicitada: projeto '{project.Name}', sessão '{session.Name}', provedor '{providerName}'.");
        var result=_providerLaunchService.Launch(project,launch,providerName,session.Name);
        if (result.BatStarted) _activityLog.Info($"Inicialização do provedor concluída: '{providerName}'.");
        else if (result.UacWasCancelled) _activityLog.Warning($"Solicitação de UAC cancelada para o provedor '{providerName}'.");
        else _activityLog.Warning($"Falha ao iniciar provedor: '{providerName}'.");
        if (result.UrlOpened)
        {
            var browserName = result.UsedFirefox ? "Firefox" : "navegador padrão";
            _activityLog.Info($"URL do GPT aberta para o projeto '{project.Name}' usando {browserName}.");
        }
        if(result.ErrorMessage is not null)ShowOperationError(result.ErrorMessage);
    }
    private void DisposeTrayResources()
    {
        if (_trayResourcesDisposed)
        {
            return;
        }

        _trayResourcesDisposed = true;
        _notifyIcon.MouseClick -= OnNotifyIconMouseClick;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        DisposeFolderContextMenu();
        _trayMenu.Dispose();
        _activityLog.Info("Aplicação encerrada.");
    }

    private void DisposeFolderContextMenu()
    {
        if (_folderContextMenu is null)
        {
            return;
        }

        var contextMenu = _folderContextMenu;
        _folderContextMenu = null;
        contextMenu.Closed -= OnFolderContextMenuClosed;
        QueueContextMenuDispose(contextMenu);
    }

    private void QueueContextMenuDispose(ContextMenuStrip contextMenu)
    {
        if (_trayResourcesDisposed)
        {
            contextMenu.Dispose();
            return;
        }

        try
        {
            if (!_trayMenu.IsDisposed && _trayMenu.IsHandleCreated)
            {
                _trayMenu.BeginInvoke((MethodInvoker)contextMenu.Dispose);
                return;
            }
        }
        catch (InvalidOperationException)
        {
            // The tray menu can already be closing during application shutdown.
        }

        contextMenu.Dispose();
    }
}






