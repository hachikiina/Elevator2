using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Drawing;
using MessageBox = System.Windows.MessageBox;

namespace Elevator2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Settings Info { get; set; }
        public MainWindow()
        {
            InitializeComponent();

            #region json check
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "settings.json"))
            {
                Info = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "settings.json"));
                appName.Text = Info.Program;
                appPath.Text = Info.StartPath;
            }
            else
                Info = new Settings();
            #endregion

            // On usb drive insert and removal
            #region usbDetect
            //insert
            WqlEventQuery q_creation = new WqlEventQuery();
            q_creation.EventClassName = "__InstanceCreationEvent";
            q_creation.WithinInterval = new TimeSpan(0, 0, 2);    //How often do you want to check it? 2 seconds.
            q_creation.Condition = @"TargetInstance ISA 'Win32_DiskDriveToDiskPartition'";
            var mwe_creation = new ManagementEventWatcher(q_creation);
            mwe_creation.EventArrived += new EventArrivedEventHandler(USBEventArrived_Creation);
            mwe_creation.Start(); // Start listen for events 

            //Remove
            WqlEventQuery q_deletion = new WqlEventQuery();
            q_deletion.EventClassName = "__InstanceDeletionEvent";
            q_deletion.WithinInterval = new TimeSpan(0, 0, 2);    //How often do you want to check it? 2Sec.
            q_deletion.Condition = @"TargetInstance ISA 'Win32_DiskDriveToDiskPartition'  ";
            var mwe_deletion = new ManagementEventWatcher(q_deletion);
            mwe_deletion.EventArrived += new EventArrivedEventHandler(USBEventArrived_Deletion);
            mwe_deletion.Start(); // Start listen for events
            #endregion

            #region System tray stuff
            NotifyIcon ni = new NotifyIcon();
            ni.Icon = new Icon("Main.ico");
            ni.Visible = true;
            ni.Text = "Elevator v2";
            ni.ContextMenuStrip = new ContextMenuStrip();
            ni.ContextMenuStrip.Items.Add("Maximize", null, this.OnMaximize);
            ni.ContextMenuStrip.Items.Add("Close", null, this.Close);
            ni.DoubleClick +=
                delegate (object sender, EventArgs args)
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                };
            #endregion

            if (Info.HideOnStart)
            {
                startHide.IsChecked = true;
                this.Hide();
            }
            if (Info.RestartExplorer)
            {
                restExp.IsChecked = true;
            }
        }

        private void OnMaximize(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
        }

        private void Minimizer_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void Close(object sender, EventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void UsbFetch_Click(object sender, RoutedEventArgs e)
        {
            ManagementObjectSearcher theSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive WHERE InterfaceType='USB'");
            foreach (ManagementObject currentObject in theSearcher.Get())
            {
                ManagementObject theSerialNumberObjectQuery = new ManagementObject("Win32_PhysicalMedia.Tag='" + currentObject["DeviceID"] + "'");
                //new Thread(() => MessageBox.Show("Serial number: " + theSerialNumberObjectQuery["SerialNumber"].ToString()));
                MessageBox.Show("Serial number: " + theSerialNumberObjectQuery["SerialNumber"].ToString());
            }
            //MessageBox.Show(info.Serials.ToArray()[0]);
        }

        private void USBEventArrived_Creation(object sender, EventArrivedEventArgs e)
        {
            try
            {
                ManagementObjectSearcher theSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive WHERE InterfaceType='USB'");
                foreach (ManagementObject currentObject in theSearcher.Get())
                {
                    ManagementObject objQuery = new ManagementObject("Win32_PhysicalMedia.Tag='" + currentObject["DeviceID"] + "'");
                    foreach (var item in Info.Serials)
                    {
                        if (item == objQuery["SerialNumber"].ToString())
                        {
                            //todo: kill the application stated in the json.
                            Process.GetProcessesByName(Info.Program)[0].Kill();
                            if (Info.RestartExplorer)
                                Process.GetProcessesByName("explorer")[0].Kill(); // enable before releasing
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is IndexOutOfRangeException)
                {
                    new Thread(() => MessageBox.Show("Couldn't find the specified program running.", "Error", MessageBoxButton.OK, MessageBoxImage.Error)).Start();
                }
                else
                {
                    new Thread(() => MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)).Start();
                }
            }
        }

        private void USBEventArrived_Deletion(object sender, EventArrivedEventArgs e)
        {
            #region not working detection code
            //ManagementObjectSearcher theSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive WHERE InterfaceType='USB'");
            //foreach (ManagementObject currentObject in theSearcher.Get())
            //{
            //    if (currentObject is null)
            //    {
            //        Process.Start(@"C:\Windows\System32\notepad.exe");
            //    }
            //    else
            //    {
            //        ManagementObject objQuery = new ManagementObject("Win32_PhysicalMedia.Tag='" + currentObject["DeviceID"] + "'");
            //        foreach (var item in info.Serials)
            //        {
            //            if (item == objQuery["SerialNumber"].ToString())
            //            {
            //                return;
            //            }
            //            else
            //            {
            //                Process.Start(@"C:\Windows\System32\notepad.exe");
            //            }
            //        }
            //    }
            //} 
            #endregion
            try
            {
                if (Process.GetProcessesByName(Info.Program).Length == 0)
                {
                    Process.Start(Info.StartPath);
                }
            }
            catch (Exception ex)
            {
                new Thread(() => MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)).Start();
            }
        }

        private void UsbReg_Click(object sender, RoutedEventArgs e)
        {
            //yeah this is bad practice i know but whatever gets the job done.
            while (true)
            {
                var dialog = new PasswordDialog();
                if (dialog.ShowDialog() ?? false)
                {
                    // todo: retrieve this from an env file instead of hardcoding it
                    // don't really need it to be secure since it is only used for adding keys.
                    if (dialog.ResponseText == "password")
                    {
                        break;
                    }
                    else
                    {
                        MessageBox.Show("The password was incorrect.", "Wrong Password", MessageBoxButton.OK, MessageBoxImage.Error);
                        continue;
                    }
                }
                else
                {
                    return;
                }
            }

            try
            {
                if (string.IsNullOrEmpty(usbId.Text) || string.IsNullOrWhiteSpace(usbId.Text))
                {
                    ManagementObjectSearcher theSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive WHERE InterfaceType='USB'");
                    foreach (ManagementObject currentObject in theSearcher.Get())
                    {
                        ManagementObject objQuery = new ManagementObject("Win32_PhysicalMedia.Tag='" + currentObject["DeviceID"] + "'");
                        foreach (string item in Info.Serials)
                        {
                            if (item == objQuery["SerialNumber"].ToString())
                            {
                                //new Thread(() => MessageBox.Show("That ID already exists in the database.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning));
                                MessageBox.Show("That ID already exists in the database.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                        }
                        Info.Serials.Add(objQuery["SerialNumber"].ToString());
                        usbId.Text = objQuery["SerialNumber"].ToString();
                        ToJson();
                    }
                }
                else
                {
                    Info.Serials.Add(usbId.Text);
                    ToJson();
                }
            }
            catch (Exception ex)
            {
                new Thread(() => MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)).Start();
            }
        }

        public void ToJson()
        {
            JsonSerializer jsonSerializer = new JsonSerializer();

            using (StreamWriter sw = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "settings.json"))
            {
                sw.WriteLine(JsonConvert.SerializeObject(Info, Formatting.Indented));
            }
        }

        private void AppSet_Click(object sender, RoutedEventArgs e)
        {
            Info.Program = appName.Text.Replace(".exe", "");
            appName.Text = Info.Program;
            ToJson();
        }

        private void AppPathSet_Click(object sender, RoutedEventArgs e)
        {
            Info.StartPath = appPath.Text;
            ToJson();
        }

        private void StartHide_Click(object sender, RoutedEventArgs e)
        {
            Info.HideOnStart = startHide.IsChecked ?? false;
            ToJson();
        }

        private void RestExp_Click(object sender, RoutedEventArgs e)
        {
            Info.RestartExplorer = restExp.IsChecked ?? false;
            ToJson();
        }
    }
}
