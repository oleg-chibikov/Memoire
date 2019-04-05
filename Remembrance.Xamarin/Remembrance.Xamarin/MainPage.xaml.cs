using Remembrance.ViewModel;
using System;
using System.ComponentModel;

namespace Remembrance.Xamarin
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(true)]
    public partial class MainPage
    {
        public MainPage(AddTranslationViewModel viewModel)
        {
            BindingContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            InitializeComponent();
        }
    }
}
