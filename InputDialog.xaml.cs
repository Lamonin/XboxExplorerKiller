using System.Windows;

namespace XboxExplorerKiller
{
    /// <summary>
    /// Логика взаимодействия для InputDialog.xaml
    /// </summary>
    public partial class InputDialog : Window
    {
        public InputDialog()
        {
            InitializeComponent();
        }

        public static string UserInput { get; private set; } = "";

        public static bool? Open(Window owner, string title, string message, string defaultInput)
        {
            InputDialog dialog = new InputDialog
            {
                Owner = owner,
                Title = title
            };
            dialog.InputDialogMessage.Text = message;
            dialog.InputDialogUserInput.Text = defaultInput;
            dialog.InputDialogUserInput.Focus();
            return dialog.ShowDialog();
        }

        private void Yes_Button_Click(object sender, RoutedEventArgs e)
        {
            UserInput = InputDialogUserInput.Text;
            DialogResult = true;
        }
    }
}
