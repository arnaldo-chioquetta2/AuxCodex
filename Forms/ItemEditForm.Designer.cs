namespace AuxCodex.Forms;

public partial class ItemEditForm
{
    // Controles estruturais da tela. As páginas das abas continuam dinâmicas.
    private readonly TextBox _projectName = new();
    private readonly TextBox _url = new();
    private readonly NumericUpDown _completion = new();
    private readonly Button _historyButton = new();
    private readonly TextBox _sessionName = new();
    private readonly ListBox _sessionList = new();
    private readonly TabControl _tabs = new();
    private readonly Label _error = new();

    private readonly Button _newSessionButton = new();
    private readonly Button _renameSessionButton = new();
    private readonly Button _removeSessionButton = new();
    private readonly Button _addSecondaryButton = new();
    private readonly Button _cancelButton = new();
    private readonly Button _saveButton = new();

    private readonly TableLayoutPanel _rootLayout = new();
    private readonly TableLayoutPanel _commonLayout = new();
    private readonly TableLayoutPanel _leftLayout = new();
    private readonly TableLayoutPanel _rightLayout = new();
    private readonly SplitContainer _sessionSplit = new();
    private readonly FlowLayoutPanel _sessionActions = new();
    private readonly FlowLayoutPanel _sessionHeader = new();
    private readonly FlowLayoutPanel _providerActions = new();
    private readonly FlowLayoutPanel _bottomActions = new();
}
