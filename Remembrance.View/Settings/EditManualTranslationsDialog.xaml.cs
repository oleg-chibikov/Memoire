using System.Windows;
using System.Windows.Input;
using JetBrains.Annotations;
using Xceed.Wpf.DataGrid;

namespace Remembrance.View.Settings
{
    /// <summary>
    /// The edit manual translations dialog.
    /// </summary>
    internal sealed partial class EditManualTranslationsDialog
    {
        public EditManualTranslationsDialog()
        {
            InitializeComponent();
        }

        private void DataCell_OnLostFocus(object sender, RoutedEventArgs e)
        {
            // TODO: Move to library
            var cell = (DataCell)sender;
            cell.ParentRow.EndEdit();
        }

        private void EditManualTranslationsDialog_OnPreviewLostKeyboardFocus(object sender, [NotNull] KeyboardFocusChangedEventArgs e)
        {
            // TODO: View library
            if (e.NewFocus != null && (!e.NewFocus.Focusable || !e.NewFocus.IsEnabled))
            {
                e.Handled = true;
            }
        }
    }
}