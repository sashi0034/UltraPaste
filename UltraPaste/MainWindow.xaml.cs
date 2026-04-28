using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clipboard = System.Windows.Clipboard;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;

namespace UltraPaste;

public partial class MainWindow : Window
{
    private IntPtr _previousWindow;
    private readonly List<PasteItem> _allItems = [];
    private List<PasteItem> _filteredItems = [];

    public MainWindow()
    {
        InitializeComponent();
        BuildAllItems();
    }

    private void BuildAllItems()
    {
        _allItems.Clear();

        // Text transforms (require clipboard content)
        _allItems.Add(new PasteItem { Label = "to_snake_case", GetResult = TextTransformService.ToSnakeCase });
        _allItems.Add(new PasteItem { Label = "ToUpperCamelCase", GetResult = TextTransformService.ToPascalCase });
        _allItems.Add(new PasteItem { Label = "toLowerCamelCase", GetResult = TextTransformService.ToCamelCase });
        _allItems.Add(
            new PasteItem { Label = "TO_UPPER_SNAKE_CASE", GetResult = TextTransformService.ToUpperSnakeCase });
        _allItems.Add(new PasteItem { Label = "to-kebab-case", GetResult = TextTransformService.ToKebabCase });
        _allItems.Add(new PasteItem { Label = "Sort Lines A→Z", GetResult = TextTransformService.SortLinesAscending });
        _allItems.Add(new PasteItem { Label = "Sort Lines Z→A", GetResult = TextTransformService.SortLinesDescending });
        _allItems.Add(new PasteItem { Label = "Unique Lines", GetResult = TextTransformService.UniqueLines });
        _allItems.Add(new PasteItem { Label = "Trim Lines", GetResult = TextTransformService.TrimLines });

        // Time inserts (ignore clipboard)
        var timeFmts = new (string label, string fmt)[]
        {
            ("Now: yyyy_MM_dd", "yyyy_MM_dd"),
            ("Now: yyyy-MM-dd", "yyyy-MM-dd"),
            ("Now: yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm:ss"),
            ("Now: yyyyMMdd_HHmmss", "yyyyMMdd_HHmmss"),
            ("Now: yyyyMMddHHmmss", "yyyyMMddHHmmss"),
            ("Now: HH:mm:ss", "HH:mm:ss"),
            ("Now: HH:mm", "HH:mm"),
            ("Now: yyyy/MM/dd", "yyyy/MM/dd"),
            ("Now: dd/MM/yyyy", "dd/MM/yyyy"),
            ("Now: MMM dd, yyyy", "MMM dd, yyyy"),
        };

        foreach (var (label, fmt) in timeFmts)
        {
            var capturedFmt = fmt;
            _allItems.Add(new PasteItem
            {
                Label = label,
                GetResult = _ => DateTime.Now.ToString(capturedFmt)
            });
        }

        // Unix timestamp
        _allItems.Add(new PasteItem
        {
            Label = "Now: Unix timestamp",
            GetResult = _ => DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()
        });
    }

    public void ShowAtCursor()
    {
        // Capture previous window BEFORE we steal focus
        _previousWindow = NativeMethods.GetForegroundWindow();

        // Refresh previews with current clipboard
        string? clipText = Clipboard.ContainsText() ? Clipboard.GetText() : null;
        foreach (var item in _allItems)
            item.RefreshPreview(clipText);

        // Reset filter and populate list
        FilterBox.Text = "";
        ApplyFilter("");

        // Position at center of the previous window, clamped to work area
        var area = SystemParameters.WorkArea;
        double centerX = area.Left + area.Width / 2;
        double centerY = area.Top + area.Height / 2;
        if (NativeMethods.GetWindowRect(_previousWindow, out var rect))
        {
            centerX = rect.Left + rect.Width / 2.0;
            centerY = rect.Top + rect.Height / 2.0;
        }

        Left = (centerX - Width / 2);
        Top = (centerY - ActualHeight / 2);

        Show();
        Activate();
        FilterBox.Focus();

        // Select first item
        if (ItemList.Items.Count > 0)
            ItemList.SelectedIndex = 0;
    }

    public void HidePopup()
    {
        Hide();
        FilterBox.Text = "";
    }

    private void ApplyFilter(string filter)
    {
        _filteredItems = string.IsNullOrWhiteSpace(filter)
            ? _allItems.ToList()
            : _allItems.Where(i => i.Label.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

        ItemList.ItemsSource = _filteredItems;

        if (ItemList.Items.Count > 0)
            ItemList.SelectedIndex = 0;
    }

    private void OnFilterChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter(FilterBox.Text);
    }

    private void OnFilterKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
        case Key.Down:
            MoveSelection(1);
            e.Handled = true;
            break;
        case Key.Up:
            MoveSelection(-1);
            e.Handled = true;
            break;
        case Key.Enter:
            if (ItemList.SelectedItem is PasteItem item)
                ExecuteItem(item);
            e.Handled = true;
            break;
        case Key.Escape:
            HidePopup();
            e.Handled = true;
            break;
        }
    }

    private void OnListKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
        case Key.Enter:
            if (ItemList.SelectedItem is PasteItem item)
                ExecuteItem(item);
            e.Handled = true;
            break;
        case Key.Escape:
            HidePopup();
            e.Handled = true;
            break;
        }
    }

    private void OnListMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ItemList.SelectedItem is PasteItem item)
            ExecuteItem(item);
    }

    private void OnDeactivated(object sender, EventArgs e)
    {
        HidePopup();
    }

    private void MoveSelection(int delta)
    {
        if (ItemList.Items.Count == 0) return;
        var next = (ItemList.SelectedIndex + delta + ItemList.Items.Count) % ItemList.Items.Count;
        ItemList.SelectedIndex = next;
        ItemList.ScrollIntoView(ItemList.SelectedItem);
    }

    private async void ExecuteItem(PasteItem item)
    {
        var previousWindow = _previousWindow;

        // Compute and set result in clipboard
        string? clipText = Clipboard.ContainsText() ? Clipboard.GetText() : null;
        var result = item.GetResult(clipText);
        if (!string.IsNullOrEmpty(result))
            Clipboard.SetText(result);

        HidePopup();

        if (previousWindow == IntPtr.Zero) return;

        // Give the popup time to disappear, then restore focus and send Ctrl+V
        await Task.Delay(80);
        NativeMethods.SetForegroundWindow(previousWindow);
        await Task.Delay(60);
        NativeMethods.SendCtrlV();
    }
}