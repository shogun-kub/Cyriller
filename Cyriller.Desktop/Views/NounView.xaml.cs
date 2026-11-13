using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Cyriller.Desktop.ViewModels;

namespace Cyriller.Desktop.Views
{
    public partial class NounView : UserControl
    {
        public NounView()
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
