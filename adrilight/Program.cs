﻿using Microsoft.Win32;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace adrilight {

    static class Program {

        private static readonly ILogger _log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static void Main()
        {
#if DEBUG
            var config = new LoggingConfiguration();
            var debuggerTarget = new DebuggerTarget() { Layout = "${processtime} ${message:exceptionSeparator=\n\t:withException=true}" };
            config.AddTarget("debugger", debuggerTarget);
            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, debuggerTarget));

            LogManager.Configuration = config;
#endif

            _log.Debug($"adrilight {VersionNumber}: Main() started.");

            AppDomain.CurrentDomain.UnhandledException +=
                (sender, args) => ApplicationOnThreadException(sender, args.ExceptionObject as Exception);

            Settings.Load();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += (sender, args) => ApplicationOnThreadException(sender, args.Exception);

            Application.ApplicationExit += (s, e) => _log.Debug("Application exit!");
            SystemEvents.PowerModeChanged += (s, e) => _log.Debug("Changing Powermode to {0}", e.Mode);

            SetupNotifyIcon();

            if (!Settings.StartMinimized)
            {
                OpenSettingsWindow();
            }
            Application.Run();
        }
        
        private static NotifyIcon SetupNotifyIcon()
        {
            var icon = new System.Drawing.Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream("adrilight.adrilight_icon.ico"));
            var contextMenu = new ContextMenu();
            contextMenu.MenuItems.Add(new MenuItem("Settings...", (s, e) => OpenSettingsWindow()));
            contextMenu.MenuItems.Add(new MenuItem("Exit", (s, e) => Application.Exit()));

            var notifyIcon = new NotifyIcon()
            {
                Text = $"adrilight {GetVersionNumber()}",
                Icon = icon,
                Visible = true,
                ContextMenu =contextMenu
            };
            notifyIcon.DoubleClick += (s, e) => { OpenSettingsWindow(); };

            Application.ApplicationExit += (s, e) => notifyIcon.Dispose();

            return notifyIcon;
        }

        static MainForm _mainForm;
        private static void OpenSettingsWindow()
        {
            if(_mainForm == null)
            {
                _mainForm = new MainForm();
                _mainForm.FormClosed += MainForm_FormClosed;
                _mainForm.Show();
            }
            else
            {
                _mainForm.BringToFront();
            }
        }

        private static void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (_mainForm == null) return;

            //deregister to avoid memory leak
            _mainForm.FormClosed -= MainForm_FormClosed;
            _mainForm = null;
        }

        private static void ApplicationOnThreadException(object sender, Exception ex)
        {
            _log.Fatal(ex, $"ApplicationOnThreadException from sender={sender}, adrilight version={VersionNumber}");

            var sb = new StringBuilder();
            sb.AppendLine($"Sender: {sender}");
            if (sender != null)
            {
                sb.AppendLine($"Sender Type: {sender.GetType().FullName}");
            }
            sb.AppendLine("-------");
            do
            {
                sb.AppendLine($"exception type: {ex.GetType().FullName}");
                sb.AppendLine($"exception message: {ex.Message}");
                sb.AppendLine($"exception stacktrace: {ex.StackTrace}");
                sb.AppendLine("-------");
                ex = ex.InnerException;
            } while (ex != null);

            MessageBox.Show(sb.ToString(), "unhandled exception :-(");
            Application.Exit();
        }

        public static string VersionNumber { get; } = GetVersionNumber();

        private static string GetVersionNumber()
        {
            var assembly = Assembly.GetExecutingAssembly();
            string currentVersion;
            using (var versionStream = assembly.GetManifestResourceStream("adrilight.version.txt"))
            {
                currentVersion = new StreamReader(versionStream).ReadToEnd().Trim();
            }
            return currentVersion;
        }
    }
}
