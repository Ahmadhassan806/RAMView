using System.Windows;
using RAMView.Core.ViewModels;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace RAMView.App.Controls;

public partial class TreemapView : WpfUserControl
{
    public TreemapView()
    {
        InitializeComponent();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            if (e.NewSize.Width > 20 && e.NewSize.Height > 20)
            {
                vm.CanvasWidth = e.NewSize.Width;
                vm.CanvasHeight = e.NewSize.Height;
            }
        }
    }
}
