using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Cyriller.Desktop.Views
{
    public partial class AdjectiveView : UserControl
    {
        public AdjectiveView()
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
