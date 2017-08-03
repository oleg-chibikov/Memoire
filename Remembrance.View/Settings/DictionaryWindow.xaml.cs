﻿using System;
using JetBrains.Annotations;
using Remembrance.Contracts.View.Settings;
using Remembrance.ViewModel.Settings;

namespace Remembrance.View.Settings
{
    [UsedImplicitly]
    internal sealed partial class DictionaryWindow : IDictionaryWindow
    {
        public DictionaryWindow([NotNull] DictionaryViewModel viewModel)
        {
            DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            InitializeComponent();
            //TODO: XAML only
            AddTranslationControl.WordTextBox.Focus();
        }
    }
}