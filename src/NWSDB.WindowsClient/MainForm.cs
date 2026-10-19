using System.Globalization;
using NWSDB.Shared.Dtos;

namespace NWSDB.WindowsClient;

/// <summary>
/// Windows Forms client with two tabs:
/// 1. Customer counter - log in, view the current bill and history, pay (REST API through the gateway).
/// 2. Billing staff - create, read, update and delete bills (SOAP web service).
/// </summary>
public class MainForm : Form
{
    private readonly RestApiClient _rest = new();
    private readonly SoapBillingClient _soap = new();

    // Customer tab
    private readonly TextBox _accountBox = new() { Left = 130, Top = 20, Width = 160, Text = "1001" };
    private readonly TextBox _passwordBox = new() { Left = 130, Top = 55, Width = 160, UseSystemPasswordChar = true };
    private readonly Button _loginButton = new() { Left = 310, Top = 53, Width = 100, Text = "Log in" };
    private readonly Label _billLabel = new() { Left = 20, Top = 100, Width = 700, Height = 24, Text = "Log in to see your current bill." };
    private readonly ComboBox _channelBox = new() { Left = 130, Top = 135, Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Button _payButton = new() { Left = 310, Top = 133, Width = 140, Text = "Pay current bill", Enabled = false };
    private readonly DataGridView _historyGrid = NewGrid(20, 180);
    private readonly Label _customerStatus = NewStatus();

    // Staff tab
    private readonly TextBox _staffAccountBox = new() { Left = 130, Top = 20, Width = 120, Text = "1001" };
    private readonly Button _loadButton = new() { Left = 270, Top = 18, Width = 120, Text = "Load bills" };
    private readonly DataGridView _billsGrid = NewGrid(20, 60);
    private readonly TextBox _billIdBox = new() { Left = 110, Top = 290, Width = 80, ReadOnly = true };
    private readonly TextBox _monthBox = new() { Left = 310, Top = 290, Width = 90, PlaceholderText = "2026-10" };
    private readonly NumericUpDown _unitsBox = new() { Left = 520, Top = 290, Width = 80, Maximum = 10000 };
    private readonly NumericUpDown _amountBox = new() { Left = 110, Top = 325, Width = 110, Maximum = 1000000, DecimalPlaces = 2 };
    private readonly DateTimePicker _dueBox = new() { Left = 310, Top = 325, Width = 150, Format = DateTimePickerFormat.Short };
    private readonly ComboBox _statusBox = new() { Left = 520, Top = 325, Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label _staffStatus = NewStatus();

    public MainForm()
    {
        Text = "NWSDB Desktop Client";
        Width = 780;
        Height = 520;
        StartPosition = FormStartPosition.CenterScreen;

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(BuildCustomerTab());
        tabs.TabPages.Add(BuildStaffTab());
        Controls.Add(tabs);
    }

    // ---------------- Customer tab (REST) ----------------
    private TabPage BuildCustomerTab()
    {
        var page = new TabPage("Customer counter (REST API)");
        _channelBox.Items.AddRange(new object[] { "NWSDB website", "Partner bank app", "Mobile wallet", "Post office" });
        _channelBox.SelectedIndex = 1;
        _loginButton.Click += async (_, _) => await LoginAsync();
        _payButton.Click += async (_, _) => await PayAsync();
        page.Controls.AddRange(new Control[]
        {
            NewLabel("Account number", 20, 23), _accountBox,
            NewLabel("Password", 20, 58), _passwordBox, _loginButton,
            _billLabel,
            NewLabel("Payment channel", 20, 138), _channelBox, _payButton,
            _historyGrid, _customerStatus
        });
        return page;
    }

    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(_accountBox.Text) || string.IsNullOrEmpty(_passwordBox.Text))
        {
            ShowError(_customerStatus, "Enter both your account number and password.");
            return;
        }
        try
        {
            var user = await _rest.LoginAsync(_accountBox.Text.Trim(), _passwordBox.Text);
            _passwordBox.Clear();
            _payButton.Enabled = true;
            ShowOk(_customerStatus, $"Signed in as {user.FullName}.");
            await LoadCustomerBillsAsync();
        }
        catch (Exception ex) { ShowError(_customerStatus, ex.Message); }
    }

    private async Task LoadCustomerBillsAsync()
    {
        var accountNo = _rest.CurrentUser!.AccountNo;
        var bill = await _rest.GetCurrentBillAsync(accountNo);
        _billLabel.Text = $"Current bill ({bill.Month}): LKR {bill.Amount.ToString("N2", CultureInfo.InvariantCulture)} " +
                          $"due on {bill.DueDate:yyyy-MM-dd} - {bill.Status}";
        _historyGrid.DataSource = await _rest.GetBillHistoryAsync(accountNo);
    }

    private async Task PayAsync()
    {
        try
        {
            var receipt = await _rest.PayAsync(_rest.CurrentUser!.AccountNo, (string)_channelBox.SelectedItem!);
            ShowOk(_customerStatus, $"Paid LKR {receipt.Amount.ToString("N2", CultureInfo.InvariantCulture)} via {receipt.Channel}. Receipt: {receipt.ReceiptNo}");
            await LoadCustomerBillsAsync();
        }
        catch (Exception ex) { ShowError(_customerStatus, ex.Message); }
    }

    // ---------------- Staff tab (SOAP CRUD) ----------------
    private TabPage BuildStaffTab()
    {
        var page = new TabPage("Billing staff (SOAP CRUD)");
        _statusBox.Items.AddRange(new object[] { "Unpaid", "Paid" });
        _statusBox.SelectedIndex = 0;
        _billsGrid.Height = 210;
        _billsGrid.SelectionChanged += (_, _) => FillFormFromGrid();
        _loadButton.Click += async (_, _) => await LoadStaffBillsAsync();

        var create = new Button { Left = 110, Top = 365, Width = 100, Text = "Create" };
        var update = new Button { Left = 220, Top = 365, Width = 100, Text = "Update" };
        var delete = new Button { Left = 330, Top = 365, Width = 100, Text = "Delete" };
        var clear = new Button { Left = 440, Top = 365, Width = 100, Text = "Clear" };
        create.Click += async (_, _) => await RunAsync(async () =>
        {
            var id = await _soap.CreateBillAsync(ReadForm());
            ShowOk(_staffStatus, $"Bill {id} created. The amount was calculated from the tariff.");
        });
        update.Click += async (_, _) => await RunAsync(async () =>
        {
            var bill = ReadForm();
            if (bill.BillId == 0) throw new InvalidOperationException("Select a bill in the list first.");
            ShowOk(_staffStatus, await _soap.UpdateBillAsync(bill) ? $"Bill {bill.BillId} updated." : "Bill not found.");
        });
        delete.Click += async (_, _) => await RunAsync(async () =>
        {
            var id = ReadForm().BillId;
            if (id == 0) throw new InvalidOperationException("Select a bill in the list first.");
            if (MessageBox.Show($"Delete bill {id}?", "Confirm", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            ShowOk(_staffStatus, await _soap.DeleteBillAsync(id) ? $"Bill {id} deleted." : "Bill not found.");
        });
        clear.Click += (_, _) => { _billIdBox.Clear(); _monthBox.Clear(); _unitsBox.Value = 0; _amountBox.Value = 0; };

        page.Controls.AddRange(new Control[]
        {
            NewLabel("Account number", 20, 23), _staffAccountBox, _loadButton, _billsGrid,
            NewLabel("Bill ID", 20, 293), _billIdBox, NewLabel("Month", 230, 293), _monthBox,
            NewLabel("Units used", 420, 293), _unitsBox,
            NewLabel("Amount (LKR)", 20, 328), _amountBox, NewLabel("Due date", 230, 328), _dueBox,
            NewLabel("Status", 420, 328), _statusBox,
            create, update, delete, clear, _staffStatus
        });
        _staffStatus.Top = 405;
        return page;
    }

    private async Task LoadStaffBillsAsync() => await RunAsync(async () =>
    {
        var bills = await _soap.GetBillsAsync(_staffAccountBox.Text.Trim());
        _billsGrid.DataSource = bills;
        ShowOk(_staffStatus, $"{bills.Count} bills loaded with the SOAP operation GetBills.");
    }, reload: false);

    private void FillFormFromGrid()
    {
        if (_billsGrid.CurrentRow?.DataBoundItem is not SoapBill b) return;
        _billIdBox.Text = b.BillId.ToString();
        _monthBox.Text = b.Month;
        _unitsBox.Value = b.UnitsUsed;
        _amountBox.Value = b.Amount;
        _dueBox.Value = b.DueDate;
        _statusBox.SelectedItem = b.Status;
    }

    private SoapBill ReadForm() => new()
    {
        BillId = int.TryParse(_billIdBox.Text, out var id) ? id : 0,
        AccountNo = _staffAccountBox.Text.Trim(),
        Month = _monthBox.Text.Trim(),
        UnitsUsed = (int)_unitsBox.Value,
        Amount = _amountBox.Value,
        DueDate = _dueBox.Value.Date,
        Status = (string)_statusBox.SelectedItem!
    };

    /// <summary>Runs a SOAP call, shows any fault message, then reloads the list.</summary>
    private async Task RunAsync(Func<Task> action, bool reload = true)
    {
        try
        {
            await action();
            if (reload)
            {
                _billsGrid.DataSource = await _soap.GetBillsAsync(_staffAccountBox.Text.Trim());
            }
        }
        catch (Exception ex) { ShowError(_staffStatus, ex.Message); }
    }

    // ---------------- helpers ----------------
    private static Label NewLabel(string text, int left, int top) =>
        new() { Text = text, Left = left, Top = top, AutoSize = true };

    private static Label NewStatus() =>
        new() { Left = 20, Top = 420, Width = 720, Height = 24, ForeColor = Color.DarkGreen };

    private static DataGridView NewGrid(int left, int top) => new()
    {
        Left = left, Top = top, Width = 720, Height = 230, ReadOnly = true,
        AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
    };

    private static void ShowOk(Label label, string text) { label.ForeColor = Color.DarkGreen; label.Text = text; }
    private static void ShowError(Label label, string text) { label.ForeColor = Color.Firebrick; label.Text = text; }
}
