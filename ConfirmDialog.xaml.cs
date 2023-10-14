using System.Windows;

namespace XboxExplorerKiller
{
    /// <summary>
    /// Логика взаимодействия для ConfirmDialog.xaml
    /// </summary>
    public partial class ConfirmDialog : Window
    {
        public ConfirmDialog()
        {
            InitializeComponent();
        }

        public static bool? Open(Window owner, string title, string message)
        {
            ConfirmDialog dialog = new ConfirmDialog
            {
                Title = title,
                Owner = owner
            };
            dialog.ConfirmDialogMessage.Text = message;
            return dialog.ShowDialog();
        }

        private void Yes_Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
