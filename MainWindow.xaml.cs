using System;
using System.Windows;
using System.Diagnostics;
using System.Windows.Controls;
using System.Timers;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;

namespace XboxExplorerKiller
{
    public class SaveData
    {
        public string[] Processes { get; set; }
    }

    public partial class MainWindow : Window
    {
        const string saveFileName = "XboxExplorerKiller.json";

        private bool isProcessSelected;
        private bool isExplorerKilled;
        private string killedProcessName;
        private Timer killerTimer;
        HashSet<string> processNamesForKill;

        public MainWindow()
        {
            InitializeComponent();
            killerTimer = new Timer(1000);
            killerTimer.Elapsed += KillerTimer_Elapsed;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            EditProcessButton.IsEnabled = false;
            RemoveProcessButton.IsEnabled = false;
            LoadData();
        }

        private void KillerTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (isExplorerKilled)
            {
                if (Process.GetProcessesByName(killedProcessName).Length == 0)
                {
                    RestartExplorer();
                    isExplorerKilled = false;
                }
            }
            else
            {
                Process[] processlist = Process.GetProcesses();

                foreach (var process in processlist)
                {
                    if (processNamesForKill.Contains(process.ProcessName))
                    {
                        KillExplorer();
                        isExplorerKilled = true;
                        killedProcessName = process.ProcessName;
                        break;
                    }
                }
            }
        }

        private void SaveData()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, saveFileName);
            var save = new SaveData() { Processes = ProcessListBox.Items.Cast<string>().ToArray() };
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(path, JsonSerializer.Serialize(save, options));
        }

        private void LoadData()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, saveFileName);
            if (!File.Exists(path))
            {
                Console.WriteLine("Saved file not be found!");
                return;
            }

            var jsonString = File.ReadAllText(saveFileName);
            SaveData savedData = JsonSerializer.Deserialize<SaveData>(jsonString);
            foreach (var process in savedData.Processes)
            {
                ProcessListBox.Items.Add(process);
            }
        }

        private void CallCmdCommand(string command)
        {
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    Arguments = "/C " + command
                }
            };
            process.Start();
            process.WaitForExit();
        }

        private void KillExplorer()
        {
            Dispatcher.Invoke(() => { ExplorerStatusLabel.Content = "Explorer status: Killed"; });
            CallCmdCommand("taskkill /f /im explorer.exe");
        }

        private void RestartExplorer()
        {
            Dispatcher.Invoke(() => { ExplorerStatusLabel.Content = "Explorer status: Running"; });
            CallCmdCommand("start %windir%\\explorer.exe");
        }

        private string GetListBoxSelectedValue(ListBox listBox)
        {
            int idx = listBox.SelectedIndex;
            return idx != -1 ? listBox.Items[listBox.SelectedIndex].ToString() : "";
        }

        private void ReplaceListBoxSelectedValue(ListBox listBox, string newValue)
        {
            int idx = listBox.SelectedIndex;
            listBox.Items.RemoveAt(idx);
            listBox.Items.Insert(idx, newValue);
        }


        private void ProcessListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            isProcessSelected = ProcessListBox.SelectedIndex != -1;
            EditProcessButton.IsEnabled = isProcessSelected;
            RemoveProcessButton.IsEnabled = isProcessSelected;
        }

        private void Add_Button_Click(object sender, RoutedEventArgs e)
        {
            if (InputDialog.Open(this, "Add process", "Enter process name:", "Example Process Name") == true)
            {
                ProcessListBox.Items.Add(InputDialog.UserInput);
                SaveData();
            }
        }

        private void Edit_Button_Click(object sender, RoutedEventArgs e)
        {
            if (InputDialog.Open(this, "Edit process name", "Enter new process name:", GetListBoxSelectedValue(ProcessListBox)) == true)
            {
                ReplaceListBoxSelectedValue(ProcessListBox, InputDialog.UserInput);
                SaveData();
            }
        }

        private void Remove_Button_Click(object sender, RoutedEventArgs e)
        {
            if (ConfirmDialog.Open(this, "Remove process from list", "Are you sure?") == true)
            {
                ProcessListBox.Items.RemoveAt(ProcessListBox.SelectedIndex);
                SaveData();
            }
        }

        private void Stop_Button_Click(object sender, RoutedEventArgs e)
        {
            ProcessListBox.IsEnabled = true;
            AddProcessButton.IsEnabled = true;
            EditProcessButton.IsEnabled = isProcessSelected;
            RemoveProcessButton.IsEnabled = isProcessSelected;

            KillerStatusLabel.Content = "Killer status: Stopped";
            killerTimer.Stop();
        }

        private void Start_Button_Click(object sender, RoutedEventArgs e)
        {
            ProcessListBox.IsEnabled = false;
            AddProcessButton.IsEnabled = false;
            EditProcessButton.IsEnabled = false;
            RemoveProcessButton.IsEnabled = false;

            processNamesForKill = new HashSet<string>(ProcessListBox.Items.Cast<string>().ToList());
            KillerStatusLabel.Content = "Killer status: Working";
            killerTimer.Start();
        }

        private void Kill_Button_Click(object sender, RoutedEventArgs e)
        {
            KillExplorer();
        }

        private void Restart_Button_Click(object sender, RoutedEventArgs e)
        {
            RestartExplorer();
        }
    }
}
