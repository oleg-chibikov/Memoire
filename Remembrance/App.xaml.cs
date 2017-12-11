using System.Windows;
using Autofac;
using Common.WPF.Controls.AutoCompleteTextBox.Provider;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.View.Settings;
using Remembrance.Core.CardManagement;
using Remembrance.DAL;
using Remembrance.Resources;
using Remembrance.View.Card;
using Remembrance.View.Various;
using Remembrance.ViewModel.Card;
using Remembrance.ViewModel.Settings;
using Remembrance.WebApi;
using Scar.Common.IO;
using Scar.Common.Messages;
using Scar.Common.WPF.View;

namespace Remembrance
{
    public sealed partial class App
    {
        protected override string AppGuid { get; } = "c0a76b5a-12ab-45c5-b9d9-d693faa6e7b9";
        protected override string AlreadyRunningMessage { get; } = Errors.AlreadyRunning;

        protected override void OnStartup()
        {
            Container.Resolve<ITrayWindow>().ShowDialog();
            Container.Resolve<IAssessmentCardManager>();
            // Need to create first instance of this class in the UI thread (for proper SyncContext)
            Container.Resolve<ITranslationDetailsCardManager>();
            Container.Resolve<ApiHoster>();
            var myResourceDictionary = new ResourceDictionary
            {
                {"SuggestionProvider", Container.Resolve<IAutoCompleteDataProvider>()}
            };

            Current.Resources.MergedDictionaries.Add(myResourceDictionary);
        }

        protected override void RegisterDependencies(ContainerBuilder builder)
        {
            builder.RegisterGeneric(typeof(WindowFactory<>)).SingleInstance();
            builder.RegisterAssemblyTypes(typeof(AssessmentCardManager).Assembly).AsImplementedInterfaces().SingleInstance();
            builder.RegisterAssemblyTypes(typeof(TranslationEntryRepository).Assembly).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ApiHoster>().AsSelf().SingleInstance();
            builder.RegisterType<OpenFileService>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<SaveFileService>().AsImplementedInterfaces().SingleInstance();
            //TODO: move autocomplete to library and nuget
            builder.RegisterType<SuggestionProvider>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterAssemblyTypes(typeof(AssessmentTextInputCardViewModel).Assembly).AsSelf().InstancePerDependency();
            builder.RegisterAssemblyTypes(typeof(AssessmentTextInputCardWindow).Assembly).AsImplementedInterfaces().InstancePerDependency();
        }

        protected override void ShowMessage(Message message)
        {
            var viewModel = Container.Resolve<MessageViewModel>(new TypedParameter(typeof(Message), message));
            SynchronizationContext.Post(x => Container.Resolve<IMessageWindow>(new TypedParameter(typeof(MessageViewModel), viewModel)).Restore(), null);
        }
    }
}