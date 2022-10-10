using System.Collections.Generic;


namespace Elevator2
{
    internal class Settings
    {
        // The serials that will either be set or read.
        public List<string> Serials { get; set; } = new List<string>();
        // The program name that will be terminated.
        public string Program { get; set; } = "";
        // Delay time in seconds
        public float Delay { get; set; }
        // Whether the delay should be enabled or not.
        public bool DelayEnabled { get; set; }
        // The application to start after removing the drive.
        public string StartPath { get; set; }
        // Whether to hide on startup or not.
        public bool HideOnStart { get; set; }
        // Whether to restart explorer.exe or not.
        public bool RestartExplorer { get; set; }
    }
}
