using AuxCodex.Models;
using AuxCodex.Services;

namespace AuxCodex.Forms;

public sealed class SecondaryProvidersForm : Form
{
    private static readonly Guid OpenAiViewId = Guid.Empty;
    private readonly AppConfiguration _configuration;
    private readonly ConfigurationService _configurationService;
    private readonly ListBox _list = new();
    private readonly TextBox _name = new();
    private readonly TextBox _template = new();
    private readonly Button _deleteButton = new() { Text = "Excluir", AutoSize = true };
    private readonly BatResumeKeyService _resume = new();

    public SecondaryProvidersForm(AppConfiguration configuration, ConfigurationService configurationService)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        Icon = Utils.ApplicationIconProvider.Icon;
        Text = "Provedores";
        Width = 850;
        Height = 540;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(700, 450);

        var main = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(10), ColumnCount = 2, RowCount = 1 };
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210));
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(main);
        var left = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        _list.Dock = DockStyle.Fill;
        left.Controls.Add(_list, 0, 0);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill };
        ButtonAdd(buttons, "Adicionar", Add);
        _deleteButton.Click += Delete;
        buttons.Controls.Add(_deleteButton);
        left.Controls.Add(buttons, 0, 1);
        main.Controls.Add(left, 0, 0);

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = 4 };
        right.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        right.Controls.Add(new Label { Text = "Nome:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        _name.Dock = DockStyle.Fill;
        right.Controls.Add(_name, 1, 0);
        right.Controls.Add(new Label { Text = "Template BAT:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
        right.Controls.Add(new Label { Text = "Modelos/resume são mantidos; a chave após resume é removida ao salvar.", AutoSize = true }, 1, 1);
        _template.Dock = DockStyle.Fill;
        _template.Multiline = true;
        _template.AcceptsReturn = true;
        _template.AcceptsTab = true;
        _template.ScrollBars = ScrollBars.Both;
        _template.WordWrap = false;
        _template.Font = new Font("Consolas", 10);
        right.Controls.Add(_template, 0, 2);
        right.SetColumnSpan(_template, 2);
        var bottom = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        ButtonAdd(bottom, "Salvar", SaveSelected);
        ButtonAdd(bottom, "Fechar", (_, _) => Close());
        right.Controls.Add(bottom, 0, 3);
        right.SetColumnSpan(bottom, 2);
        main.Controls.Add(right, 1, 0);
        _list.SelectedIndexChanged += (_, _) => LoadSelected();
        RefreshList(OpenAiViewId);
    }

    private static void ButtonAdd(Control parent, string text, EventHandler action)
    {
        var button = new Button { Text = text, AutoSize = true, Padding = new Padding(8, 4, 8, 4) };
        button.Click += action;
        parent.Controls.Add(button);
    }

    private void RefreshList(Guid? select = null)
    {
        _list.Items.Clear();
        _list.Items.Add(new Entry(OpenAiViewId, "OpenAI", true, BatTemplateDefaults.ResolveOpenAi(_configuration, out _)));
        foreach (var definition in _configuration.SecondaryProviders.OrderBy(value => value.Name, StringComparer.CurrentCultureIgnoreCase))
            _list.Items.Add(new Entry(definition.Id, definition.Name, false, definition.BatTemplate, definition));
        if (select.HasValue)
            for (var index = 0; index < _list.Items.Count; index++)
                if (((Entry)_list.Items[index]!).Id == select.Value) { _list.SelectedIndex = index; break; }
    }

    private void LoadSelected()
    {
        if (_list.SelectedItem is not Entry entry)
        {
            _name.Clear();
            _template.Clear();
            _name.ReadOnly = false;
            _deleteButton.Enabled = false;
            return;
        }
        _name.Text = entry.Name;
        _template.Text = entry.Template;
        _name.ReadOnly = entry.IsOpenAi;
        _deleteButton.Enabled = !entry.IsOpenAi;
    }

    private void Add(object? sender, EventArgs e)
    {
        var definition = new SecondaryProviderDefinition { Id = Guid.NewGuid(), Name = "Novo provedor", BatTemplate = string.Empty };
        _configuration.SecondaryProviders.Add(definition);
        RefreshList(definition.Id);
        _name.Focus();
        _name.SelectAll();
    }

    private void SaveSelected(object? sender, EventArgs e)
    {
        if (_list.SelectedItem is not Entry entry) return;
        if (entry.IsOpenAi)
        {
            var oldTemplate = _configuration.OpenAiBatTemplate;
            _configuration.OpenAiBatTemplate = _resume.RemoveResumeKey(_template.Text);
            try { _configurationService.Save(_configuration); }
            catch (Exception)
            {
                _configuration.OpenAiBatTemplate = oldTemplate;
                MessageBox.Show(this, "Não foi possível salvar o provedor OpenAI.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            _configurationService.ActivityLog?.Info("Template global do OpenAI alterado.");
            RefreshList(OpenAiViewId);
            return;
        }
        if (!TrySaveDefinition(entry.Id, _name.Text, _template.Text, out var error))
        {
            MessageBox.Show(this, error, Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        RefreshList(entry.Id);
    }

    internal bool TrySaveDefinition(Guid id, string name, string template, out string error)
    {
        error = string.Empty;
        var definition = _configuration.SecondaryProviders.FirstOrDefault(value => value.Id == id);
        if (definition is null) { error = "O provedor selecionado não foi encontrado."; return false; }
        name = name.Trim();
        if (name.Length == 0) { error = "Informe o nome do provedor."; return false; }
        if (string.Equals(name, "OpenAI", StringComparison.OrdinalIgnoreCase)) { error = "OpenAI já é o provedor principal e não pode ser duplicado."; return false; }
        if (_configuration.SecondaryProviders.Any(value => value.Id != id && string.Equals(value.Name.Trim(), name, StringComparison.OrdinalIgnoreCase)))
        { error = "Já existe um provedor com esse nome."; return false; }
        var oldName = definition.Name;
        var oldTemplate = definition.BatTemplate;
        definition.Name = name;
        definition.BatTemplate = _resume.RemoveResumeKey(template);
        try
        {
            _configurationService.Save(_configuration);
            _configurationService.ActivityLog?.Info($"Provedor editado: '{definition.Name}'.");
            return true;
        }
        catch (Exception)
        {
            definition.Name = oldName;
            definition.BatTemplate = oldTemplate;
            error = "Não foi possível salvar o catálogo.";
            return false;
        }
    }

    private void Delete(object? sender, EventArgs e)
    {
        if (_list.SelectedItem is not Entry entry || entry.IsOpenAi) return;
        var count = CountUsage(_configuration.Folders, _configuration.Items, entry.Id);
        if (count > 0) { MessageBox.Show(this, $"Este provedor está em uso por {count} sessão(ões) e não pode ser excluído.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        if (MessageBox.Show(this, $"Excluir {entry.Name}?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        if (!TryRemoveDefinition(entry.Id, out var error)) MessageBox.Show(this, error, Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        RefreshList();
    }

    internal bool TryRemoveDefinition(Guid id, out string error)
    {
        error = string.Empty;
        if (id == OpenAiViewId) { error = "OpenAI não pode ser excluído."; return false; }
        var definition = _configuration.SecondaryProviders.FirstOrDefault(value => value.Id == id);
        if (definition is null) { error = "O provedor selecionado não foi encontrado."; return false; }
        if (definition.IsBuiltIn) { error = "Os provedores padrão não podem ser excluídos."; return false; }
        var count = CountUsage(_configuration.Folders, _configuration.Items, id);
        if (count > 0) { error = $"Este provedor está em uso por {count} sessão(ões) e não pode ser excluído."; return false; }
        _configuration.SecondaryProviders.Remove(definition);
        try
        {
            _configurationService.Save(_configuration);
            _configurationService.ActivityLog?.Info($"Provedor excluído da configuração: '{definition.Name}'.");
            return true;
        }
        catch (Exception) { _configuration.SecondaryProviders.Add(definition); error = "Não foi possível salvar o catálogo."; return false; }
    }

    private static int CountUsage(IEnumerable<MenuFolder> folders, IEnumerable<MenuItem> items, Guid id)
    {
        var count = items.Sum(item => (item.Sessions ?? new()).Sum(session => (session.SecondaryProviders ?? new()).Count(provider => provider.ProviderDefinitionId == id)));
        foreach (var folder in folders) count += CountUsage(folder.Folders, folder.Items, id);
        return count;
    }

    private sealed record Entry(Guid Id, string Name, bool IsOpenAi, string Template, SecondaryProviderDefinition? Definition = null)
    {
        public override string ToString() => Name;
    }
}
