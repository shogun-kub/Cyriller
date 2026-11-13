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
    public partial class MainWindow : Window
    {
        protected int FocusTimeout { get; set; } = 1;

        public MainWindow(MainWindowViewModel dataContext)
        {
            InitializeComponent();
            this.DataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));

            dataContext.NounFormOpened += DataContext_NounFormOpened;
            dataContext.AdjectiveFormOpened += DataContext_AdjectiveFormOpened;
            dataContext.NameFormOpened += DataContext_NameFormOpened;
            dataContext.NumberFormOpened += DataContext_NumberFormOpened;
            dataContext.PhraseFormOpened += DataContext_PhraseFormOpened;
        }

        private async void DataContext_PhraseFormOpened(object sender, EventArgs e)
        {
            // This delay is needed to focus element after UI is updated.
            await Task.Delay(this.FocusTimeout);
            this.FindControl<PhraseView>("ucPhrase").FocusInput();
        }

        private async void DataContext_NumberFormOpened(object sender, EventArgs e)
        {
            // This delay is needed to focus element after UI is updated.
            await Task.Delay(this.FocusTimeout);
            this.FindControl<NumberView>("ucNumber").FocusInput();
        }

        private async void DataContext_NameFormOpened(object sender, EventArgs e)
        {
            // This delay is needed to focus element after UI is updated.
            await Task.Delay(this.FocusTimeout);
            this.FindControl<NameView>("ucName").FocusInput();
        }

        private async void DataContext_AdjectiveFormOpened(object sender, EventArgs e)
        {
            // This delay is needed to focus element after UI is updated.
            await Task.Delay(this.FocusTimeout);
            this.FindControl<AdjectiveView>("ucAdjective").FocusInput();
        }

        private async void DataContext_NounFormOpened(object sender, EventArgs e)
        {
            // This delay is needed to focus element after UI is updated.
            await Task.Delay(this.FocusTimeout);
            this.FindControl<NounView>("ucNoun").FocusInput();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}