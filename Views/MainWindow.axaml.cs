using Avalonia.Controls;
using Avalonia.Input;

namespace MSRDownloader.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;

        if (sender is TextBlock {Parent: CheckBox {Parent: TreeViewItem treeViewItem}})
        {
            treeViewItem.IsExpanded = true;
        }
    }

    private void BlockDoubleClickExpand(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
    }
}