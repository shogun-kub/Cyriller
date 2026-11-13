using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Cyriller.Desktop.Views
{
    public partial class PhraseView : UserControl
    {
        public PhraseView()
        {
            this.InitializeComponent();
        }

        public void FocusInput()
        {
            base.Focus();
            this.FindControl<TextBox>("txtInputText").Focus();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
