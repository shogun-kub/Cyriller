using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Cyriller.Desktop.Views
{
    public partial class NumberView : UserControl
    {
        public NumberView()
        {
            this.InitializeComponent();
        }

        public void FocusInput()
        {
            base.Focus();
            this.FindControl<NumericUpDown>("txtInputValue").Focus();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
