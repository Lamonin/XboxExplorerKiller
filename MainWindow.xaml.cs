using System;
using System.Windows;
using System.Diagnostics;
using System.Windows.Controls;
using System.Timers;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;
using System.Windows.Documents;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Threading;

using Timer = System.Timers.Timer;
using System.Collections.ObjectModel;

namespace XboxExplorerKiller
{
    public class ProcessData
    {
        public ObservableCollection<ProcessInfo> Processes { get; set; } = new ObservableCollection<ProcessInfo>();
    }

    public class ProcessInfo
    {
        public string Name { get; set; }
        public int Delay { get; set; }
    }

    public partial class MainWindow : Window
    {
        const string saveFileName = "XboxExplorerKillerData.json";

        private Run? killerStatusRun;
        private Run? explorerStatusRun;
        private CancellationTokenSource cancelationTokenSource;

        private bool isProcessKilled;
        private bool isProcessSelected;
        private bool isExplorerKilled;
        private readonly Timer timeTimer;
        private readonly Timer killerTimer;
        private HashSet<string> processNamesForKill;

        private ProcessData processData;

        public MainWindow()
        {
            InitializeComponent();

            processData = new ProcessData();
            processNamesForKill = new HashSet<string>();

            cancelationTokenSource = new CancellationTokenSource();

            timeTimer = new Timer(1000);
            timeTimer.Elapsed += DateTimeTimer_Elapsed;
            timeTimer.Start();

            killerTimer = new Timer(1000);
            killerTimer.Elapsed += KillerTimer_Elapsed;

            Closing += MainWindow_Closing;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();

            ProcessListBox.ItemsSource = processData.Processes;
            ProcessListBox.DisplayMemberPath = "Name";

            EditProcessButton.IsEnabled = false;
            RemoveProcessButton.IsEnabled = false;
            DelayLabel.IsEnabled = false;
            ProcessDelayTextBox.IsEnabled = false;

            DateTimeLabel.Content = DateTime.Now.ToString("g");

            killerStatusRun = new Run("Stopped") { Foreground = Brushes.Red, FontWeight = FontWeights.Bold };
            var tb = new TextBlock();
            tb.Inlines.Add("Killer status: ");
            tb.Inlines.Add(killerStatusRun);
            KillerStatusLabel.Content = tb;

            explorerStatusRun = IsExplorerRunning()
                ? new Run("Running") { Foreground = Brushes.Green, FontWeight = FontWeights.Bold }
                : new Run("Killed") { Foreground = Brushes.Red, FontWeight = FontWeights.Bold };
            tb = new TextBlock();
            tb.Inlines.Add("Explorer status: ");
            tb.Inlines.Add(explorerStatusRun);
            ExplorerStatusLabel.Content = tb;
        }


        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            killerTimer.Stop();

            SaveData();

            if (isExplorerKilled)
            {
                const string message_title = "Restart explorer.exe process?";
                const string message_body = "If you do not restart the Explorer process now, " +
                    "then you will have to do it through the Task Manager, or in some other way. " +
                    "Do you want to restart the Explorer process?";

                if (ConfirmDialog.Open(this, message_title, message_body, "Yes", "No") == true)
                {
                    RestartExplorer();
                }

                cancelationTokenSource.Cancel(true);
            }
        }

        private void DateTimeTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                DateTimeLabel.Content = DateTime.Now.ToString("g");
            });
        }

        private void KillerTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            if (!isProcessKilled)
            {
                if (IsAnyProcessRunning(out var process))
                {
                    isProcessKilled = true;
                    var findedProcess = processData.Processes.First(p => p.Name == process.ProcessName);
                    KillExplorerAndWaitUntilProcessExit(process, findedProcess.Delay);
                }
            }
        }

        private async void KillExplorerAndWaitUntilProcessExit(Process process, int delay)
        {
            cancelationTokenSource = new CancellationTokenSource();
            try
            {
                await Task.Delay(delay * 1000, cancelationTokenSource.Token);
                KillExplorer();
                await process.WaitForExitAsync(cancelationTokenSource.Token);
                RestartExplorer();
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                isProcessKilled = false;
            }
        }

        private void SaveData()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, saveFileName);
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(path, JsonSerializer.Serialize(processData, options));
        }

        private void LoadData()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, saveFileName);
            if (!File.Exists(path))
            {
                Console.WriteLine("Saved file not be found!");
                return;
            }

            var savedData = JsonSerializer.Deserialize<ProcessData>(File.ReadAllText(saveFileName));
            if (savedData == null || savedData.Processes == null) return;

            processData = savedData;
        }

        private static void CallCmdCommand(string command)
        {
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    FileName = "cmd.exe",
                    Arguments = "/C " + command
                }
            };
            process.Start();
            process.WaitForExit();
        }

        private bool IsAnyProcessRunning(out Process runnedProcess)
        {
            Process[] processList = Process.GetProcesses();

            foreach (var process in processList)
            {
                if (processNamesForKill.Contains(process.ProcessName))
                {
                    runnedProcess = process;
                    return true;
                }
            }
            runnedProcess = null;
            return false;
        }

        private bool IsExplorerRunning()
        {
            Process[] processlist = Process.GetProcesses();
            foreach (var process in processlist)
            {
                if (process.ProcessName == "explorer")
                {
                    return true;
                }
            }
            return false;
        }

        private void KillExplorer()
        {
            CallCmdCommand("taskkill /f /im explorer.exe");
            Dispatcher.Invoke(() =>
            {
                explorerStatusRun.Text = "Killed";
                explorerStatusRun.Foreground = Brushes.Red;
            });
            isExplorerKilled = true;
        }

        private void RestartExplorer()
        {
            CallCmdCommand("start %windir%\\explorer.exe");
            Dispatcher.Invoke(() =>
            {
                explorerStatusRun.Text = "Running";
                explorerStatusRun.Foreground = Brushes.Green;
            });
            isExplorerKilled = false;
        }

        private void ProcessListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            isProcessSelected = ProcessListBox.SelectedIndex != -1;
            EditProcessButton.IsEnabled = isProcessSelected;
            RemoveProcessButton.IsEnabled = isProcessSelected;
            DelayLabel.IsEnabled = isProcessSelected;
            ProcessDelayTextBox.IsEnabled = isProcessSelected;
        }

        private void Add_Button_Click(object sender, RoutedEventArgs e)
        {
            if (InputDialog.Open(this, "Add process", "Enter process name:", "Example Process Name") == true)
            {
                processData.Processes.Add(new ProcessInfo { Name = InputDialog.UserInput, Delay = 0 });
            }
        }

        private void Edit_Button_Click(object sender, RoutedEventArgs e)
        {
            if (InputDialog.Open(this, "Edit process name", "Enter new process name:", ((ProcessInfo) ProcessListBox.SelectedValue).Name) == true)
            {
                var p = processData.Processes[ProcessListBox.SelectedIndex];
                processData.Processes[ProcessListBox.SelectedIndex] = new ProcessInfo { Name = InputDialog.UserInput, Delay = p.Delay };
            }
        }

        private void Remove_Button_Click(object sender, RoutedEventArgs e)
        {
            if (ConfirmDialog.Open(this, "Remove process from list", "Are you sure?") == true)
            {
                processData.Processes.RemoveAt(ProcessListBox.SelectedIndex);
            }
        }

        private void Stop_Button_Click(object sender, RoutedEventArgs e)
        {
            ProcessListBox.IsEnabled = true;
            AddProcessButton.IsEnabled = true;
            EditProcessButton.IsEnabled = isProcessSelected;
            RemoveProcessButton.IsEnabled = isProcessSelected;
            DelayLabel.IsEnabled = isProcessSelected;
            ProcessDelayTextBox.IsEnabled = isProcessSelected;

            killerStatusRun.Text = "Stopped";
            killerStatusRun.Foreground = Brushes.Red;
            killerTimer.Stop();

            cancelationTokenSource.Cancel(true);
        }

        private void Start_Button_Click(object sender, RoutedEventArgs e)
        {
            ProcessListBox.IsEnabled = false;
            AddProcessButton.IsEnabled = false;
            EditProcessButton.IsEnabled = false;
            RemoveProcessButton.IsEnabled = false;
            DelayLabel.IsEnabled = false;
            ProcessDelayTextBox.IsEnabled = false;

            processNamesForKill = processData.Processes.Select(p => p.Name).ToHashSet();
            killerStatusRun.Text = "Working";
            killerStatusRun.Foreground = Brushes.Green;
            killerTimer.Start();
        }

        private void Kill_Button_Click(object sender, RoutedEventArgs e)
        {
            KillExplorer();
            Focus();
        }

        private void Restart_Button_Click(object sender, RoutedEventArgs e)
        {
            cancelationTokenSource.Cancel(true);
            RestartExplorer();
            Focus();
        }
    }
}
