﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management;
using System.Windows.Forms;
using Microsoft.Win32;

using HardwareHelperLib;
using System.Diagnostics;
using System.Threading;
using System.IO;

namespace GCNUSBFeeder
{
    public class SystemHelper
    {
        public static event EventHandler<Driver.LogEventArgs> Log;
        #region Registry Functions
        public static RegistryKey registryRun = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
        public static void addToStartup()
        {
            try
            {
                registryRun.SetValue("GCNAdapter", Application.ExecutablePath.ToString());
            }
            catch
            {
                Log(null, new Driver.LogEventArgs("Unable to set a startup object. (Are you running as administrator?"));
            }
        }

        public static void removeFromStartup()
        {
            try
            {
                registryRun.DeleteValue("GCNAdapter", false);
            }
            catch
            {

            }
        }

        public static bool isOnStartUp()
        {
            if (registryRun.GetValue("GCNAdapter") != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        #region Windows Version
        public static double GetOSVersion()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32S: return 3.1;
                case PlatformID.Win32Windows:
                    switch (Environment.OSVersion.Version.Minor)
                    {
                        case 0:  return 3.2;
                        case 10: return 3.3;
                        case 90: return 3.4;
                    }
                    break;
                case PlatformID.Win32NT:
                    switch (Environment.OSVersion.Version.Major)
                    {
                        case 3: return 3.51;
                        case 4: return 4.0;
                        case 5:
                            switch (Environment.OSVersion.Version.Minor)
                            {
                                case 0: return 5.0;
                                case 1: return 5.1;
                                case 2: return 5.2;
                            }
                            break;
                        case 6:
                            switch (Environment.OSVersion.Version.Minor)
                            {
                                case 0: return 6.0; // Vista
                                case 1: return 6.1; // 7.0
                                case 2: return 6.2; // 8.0
                                case 3: return 6.3; // 8.1
                                case 4: return 6.4; // 10 TP
                            }
                            break;
                        case 10: return 10.0; // 10?
                    }
                    break;
                case PlatformID.WinCE: return 2.0;
            }
            return 0;
        }
        #endregion

        #region Driver Functions
        public static void checkForMissingDrivers()
        {
            bool vJoy   = false;
            bool libUsb = false;
            try
            {
                SelectQuery query = new SelectQuery("Win32_SystemDriver");
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
                var drivers = searcher.Get();
                foreach (var d in drivers)
                {
                    if (d["Name"].ToString() == "vjoy") vJoy = true;
                    if (d["Name"].ToString().Contains("libusb")) libUsb = true;
                    if (vJoy && libUsb) break;
                }
                //if (!libUsb)
                //{
                //    Log(null, new Driver.LogEventArgs("LibUSB was not detected, please rerun the installer."));
                //}
                if (!vJoy)
                {
                    Log(null, new Driver.LogEventArgs("vJoy was not detected, please rerun the installer."));
                }
            }
            catch
            {
                Log(null, new Driver.LogEventArgs("Driver check failed, (Are you running as Administrator?)"));
            }
        }

        public static void RunLibUsbInstall()
        {
            //install.bat needs to be in the .\LibUSB\ directory for this to work
            Log(null, new Driver.LogEventArgs("Attempting LibUSB WUP-028.inf filter fix..."));
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.WorkingDirectory = @".\LibUSB";
                psi.FileName = "install.bat";
                var p = new Process();
                p.StartInfo = psi;
                p.Start();

                while (!p.HasExited)
                {
                    Thread.Sleep(1000);
                }
                Log(null, new Driver.LogEventArgs("LibUSB filter fix has completed."));
            }
            catch
            {
                Log(null, new Driver.LogEventArgs(@"Error: \LibUSB\install.bat was not found or unable to start."));
            }
        }

        #endregion

        #region External vJoy Functions
        static string vJoyDirectory = Path.GetPathRoot(Environment.SystemDirectory)+@"Program Files\vJoy\";

        public static void CreateJoystick(int portNum)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                WorkingDirectory = vJoyDirectory,
                FileName = "vJoyConfig.exe",
                Arguments = portNum + " -f -a x y z rx ry rz -b 12", //create
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };

            var p = new Process();
            p.StartInfo = psi;
            try
            {
                Log(null, new Driver.LogEventArgs("Enabling port " + portNum + "..."));
                p.Start();
                while (!p.HasExited)
                {
                    Thread.Sleep(500);
                }
                Log(null, new Driver.LogEventArgs("Port " + portNum + " is detected (OK)."));
            }
            catch
            {
                Log(null, new Driver.LogEventArgs("Error: Unable to complete configuration for port " + portNum + ". (Check vJoy install?)"));
            }
        }

        public static void DestroyJoystick(int portNum)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                WorkingDirectory = vJoyDirectory,
                FileName = "vJoyConfig.exe",
                Arguments = "-d "+ portNum, //delete
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };

            var p = new Process();
            p.StartInfo = psi;
            try
            {
                p.Start();
                while (!p.HasExited)
                {
                    Thread.Sleep(500);
                }
                Log(null, new Driver.LogEventArgs("Port " + portNum + " disabled."));
            }
            catch
            {
                Log(null, new Driver.LogEventArgs("Error: Unable to complete configuration for port " + portNum + ". (Check vJoy install?)"));
            }
        }

        public static void CreateAllJoysticks()
        {
            try
            {
                var p = Process.Start(Application.StartupPath + @"\ConfigJoysticks.bat");
                while (!p.HasExited)
                {
                    Thread.Sleep(500);
                }
                Log(null, new Driver.LogEventArgs("All ports opened."));
            }
            catch
            {
                Log(null, new Driver.LogEventArgs("Error: ConfigJoysticks.bat was not found or unable to start."));
            }
        }

        public static void DestroyAllJoysticks()
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                WorkingDirectory = vJoyDirectory,
                FileName = "vJoyConfig.exe",
                Arguments = "-d 1 2 3 4", //delete
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };

            var p = new Process();
            p.StartInfo = psi;
            try
            {
                p.Start();
                while (!p.HasExited)
                {
                    Thread.Sleep(500);
                }
                Log(null, new Driver.LogEventArgs("All ports disabled."));
            }
            catch
            {
                Log(null, new Driver.LogEventArgs("Error: Unable to complete configuration for ports. (Check vJoy install?)"));
            }
        }

        public static void EnablevJoyDLL()
        {
            try
            {
                HardwareHelperLib.HH_Lib lb = new HH_Lib();
                lb.SetDeviceState("vJoy Device", true);
            }
            catch
            {
                Log(null, new Driver.LogEventArgs("Error: Unable to complete vJoy enable."));
            }
        }

        public static void DisablevJoyDLL()
        {
            try
            {
                HardwareHelperLib.HH_Lib lb = new HH_Lib();
                lb.SetDeviceState("vJoy Device", false);
            }
            catch
            {
                Log(null, new Driver.LogEventArgs("Error: Unable to disable vJoy."));
            }
        }

        public static void CheckDevice()
        {
            try
            {
                HardwareHelperLib.HH_Lib lb = new HH_Lib();

                lb.CheckDevice("vJoy Device");
            }
            catch
            {

            }
        }


        public static void EnablevJoy()
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                WorkingDirectory = Application.StartupPath + @"\HardwareHelperLib\",
                FileName = "HardwareHelperLib.exe",
                Arguments = "-e",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                Verb = "runas"
            };

            var p = new Process();
            p.StartInfo = psi;
            try
            {
                p.Start();
                while (!p.HasExited)
                {
                    Thread.Sleep(500);
                }
                //7Log(null, new Driver.LogEventArgs("Enabling vJoy."));
            }
            catch
            {
                Log(null, new Driver.LogEventArgs("Error: Unable to complete vJoy enable."));
            }
        }

        public static void DisablevJoy()
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                WorkingDirectory = Application.StartupPath + @"\HardwareHelperLib\",
                FileName = "HardwareHelperLib.exe",
                Arguments = "-d",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                Verb = "runas"
            };

            var p = new Process();
            p.StartInfo = psi;
            try
            {
                p.Start();
                while (!p.HasExited)
                {
                    Thread.Sleep(500);
                }
                Log(null, new Driver.LogEventArgs("Disabling vJoy."));
            }
            catch
            {
                Log(null, new Driver.LogEventArgs("Error: Unable to disable vJoy."));
            }
        }

        #endregion
    }
}
