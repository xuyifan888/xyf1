﻿using PoeHUD.Controllers;
using PoeHUD.Hud.Interfaces;
using PoeHUD.Hud.Menu;
using PoeHUD.Models;
using PoeHUD.Plugins;
using System;
using System.IO;
using System.Reflection;
using Graphics = PoeHUD.Hud.UI.Graphics;
using System.Collections.Generic;
using Trinet.Core.IO.Ntfs;

namespace PoeHUD.Hud.PluginExtension
{
    public class PluginExtensionPlugin : IPlugin
    {
        public readonly GameController GameController;
        public readonly Graphics Graphics;
        public PluginExtensionPlugin(GameController gameController, Graphics graphics)
        {
            GameController = gameController;
            Graphics = graphics;
            SearchPlugins();
            LoadSettings();  
            InitMenuForPlugins(); 
            InitPlugins();
            gameController.EntityListWrapper.EntityAdded += OnEntityAdded;
            gameController.EntityListWrapper.EntityRemoved += OnEntityRemoved;
        }
        
        private void InitMenuForPlugins()
        {
            RootButton mainMenu = MenuPlugin.MenuRootButton;
            var pluginsMenu = MenuPlugin.AddChild(mainMenu, "Plugins", true);
            eInitMenu(pluginsMenu);
        }

        public event Action eInitialise = delegate { };
        public event Action eRender = delegate { };
        public event Action<EntityWrapper> eEntityAdded = delegate { };
        public event Action<EntityWrapper> eEntityRemoved = delegate { };
        public event Action<MenuItem> eInitMenu = delegate { };
        public event Action eLoadSettings = delegate { };
        public event Action eClose = delegate { };
        public static List<BasePlugin> Plugins = new List<BasePlugin>();
        private List<string> PluginUpdateLog;
        public const string UpdateTempDir = "%PluginUpdate%";//Do not change this value. Otherwice this value in PoeHUD_PluginsUpdater plugin should be also changed.
        public const string UpdateBackupDir = "%Backup%";


        private void SearchPlugins()
        {
            DirectoryInfo PluginsDir = new DirectoryInfo("plugins");
            if (!PluginsDir.Exists) return;

            foreach (var pluginDirectoryInfo in PluginsDir.GetDirectories())
            {
                var pluginTempUpdateDir = Path.Combine(pluginDirectoryInfo.FullName, UpdateTempDir);

                if(Directory.Exists(pluginTempUpdateDir))
                {
                    PluginUpdateLog = new List<string>();

                    var backupDir = Path.Combine(pluginDirectoryInfo.FullName, UpdateBackupDir);

                    if (Directory.Exists(backupDir))
                        FileOperationAPIWrapper.MoveToRecycleBin(backupDir);

                    var logFilePAth = Path.Combine(pluginDirectoryInfo.FullName, "%PluginUpdateLog.txt");
                    if (File.Exists(logFilePAth))
                        File.Delete(logFilePAth);

                    if (MoveDirectoryFiles(pluginDirectoryInfo.FullName, pluginTempUpdateDir, pluginDirectoryInfo.FullName))
                    {
                        PluginUpdateLog.Add("Deleting temp dir:\t" + pluginTempUpdateDir);
                        Directory.Delete(pluginTempUpdateDir, true);
                    }
                    else
                    {
                        LogMessage("PoeHUD PluginUpdater: some files wasn't moved or replaced while update (check %PluginUpdateLog.txt). You can move them manually: " + pluginTempUpdateDir, 20);
                        File.WriteAllLines(logFilePAth, PluginUpdateLog.ToArray());
                    }
                }

                var directoryDlls = pluginDirectoryInfo.GetFiles("*.dll", SearchOption.TopDirectoryOnly);

                foreach (var dll in directoryDlls)
                    TryLoadDll(dll.FullName, pluginDirectoryInfo.FullName);
            }
        }

        private bool MoveDirectoryFiles(string origDirectory, string sourceDirectory, string targetDirectory)
        {
            bool noErrors = true;
            var sourceDirectoryInfo = new DirectoryInfo(sourceDirectory);

            foreach (var file in sourceDirectoryInfo.GetFiles())
            {
                var destFile = Path.Combine(targetDirectory, file.Name);
                bool fileExist = File.Exists(destFile);
                
                try
                {
                    var fileLocalPath = destFile.Replace(origDirectory, "");

                    if (fileExist)
                    {
                        var backupPath = origDirectory + @"\" + UpdateBackupDir + fileLocalPath;//Do not use Path.Combine due to Path.IsPathRooted checks
                        var backupDirPath = Path.GetDirectoryName(backupPath);

                        if (!Directory.Exists(backupDirPath))
                            Directory.CreateDirectory(backupDirPath);

                        File.Copy(destFile, backupPath, true);
                    }

                    File.Copy(file.FullName, destFile, true);
                    File.Delete(file.FullName);//Delete from temp update dir

                    if (fileExist)
                        PluginUpdateLog.Add("File Replaced:\t\t" + destFile + " vs " + file.FullName);
                    else
                        PluginUpdateLog.Add("File Added:\t\t\t" + destFile);
                }
                catch (Exception ex)
                {
                    noErrors = false;
                    if (fileExist)
                    {
                        LogError("PoeHUD PluginUpdater: can't replace file: " + destFile + ", Error: " + ex.Message, 10);
                        PluginUpdateLog.Add("Error replacing file: \t" + destFile);
                    }
                    else
                    {
                        LogError("PoeHUD PluginUpdater: can't move file: " + destFile + ", Error: " + ex.Message, 10);
                        PluginUpdateLog.Add("Error moving file: \t" + destFile);
                    }
                }
            }

            foreach (var directory in sourceDirectoryInfo.GetDirectories())
            {
                var destDir = Path.Combine(targetDirectory, directory.Name);

                if (Directory.Exists(destDir))
                {
                    PluginUpdateLog.Add("Merging directory: \t" + destDir);
                    var curDirProcessNoErrors = MoveDirectoryFiles(origDirectory, directory.FullName, destDir);

                    if(curDirProcessNoErrors)
                        Directory.Delete(directory.FullName, true);

                    noErrors = curDirProcessNoErrors || noErrors;
                }
                else
                {
                    Directory.Move(directory.FullName, destDir);
                    PluginUpdateLog.Add("Moving directory: \t" + destDir);
                }
            }
            return noErrors;
        }



        private void TryLoadDll(string path, string dir)
        {
            if(ProcessFile_Real(path))
            {
                LogMessage("Can't unblock plugin: " + path, 5);
                return;
            }

            var myAsm = Assembly.LoadFrom(path);
            if (myAsm == null) return;

            var asmTypes = myAsm.GetTypes();
            if (asmTypes.Length == 0) return;

            foreach (var type in asmTypes)
            {
                if (type.IsSubclassOf(typeof(BasePlugin)))
                {
                    var extPlugin = new ExternalPlugin(type, this, dir);
                    Plugins.Add(extPlugin.BPlugin);
                    LogMessage("Loaded plugin: " + type.Name, 1);
                }
            }
        }

        private const string ZoneName = "Zone.Identifier";
        static bool ProcessFile_Real(string path)
        {
            bool result = FileSystem.AlternateDataStreamExists(path, ZoneName);
            if (result)
            {
                // Clear the read-only attribute, if set:
                FileAttributes attributes = File.GetAttributes(path);
                if (FileAttributes.ReadOnly == (FileAttributes.ReadOnly & attributes))
                {
                    attributes &= ~FileAttributes.ReadOnly;
                    File.SetAttributes(path, attributes);
                }

                result = FileSystem.DeleteAlternateDataStream(path, ZoneName);

                result = FileSystem.AlternateDataStreamExists(path, ZoneName);//Check again
            }

            return result;
        }

        #region PluginMethods
        public void InitPlugins()
        {
            eInitialise();
        }
        public void LoadSettings()
        {
            eLoadSettings();
        }
        public void Render()
        {
            eRender();
        }
        private void OnEntityAdded(EntityWrapper entityWrapper)
        {
            eEntityAdded(entityWrapper);
        }
        private void OnEntityRemoved(EntityWrapper entityWrapper)
        {
            eEntityRemoved(entityWrapper);
        }
        public void Dispose()
        {
            eClose();
        }
        #endregion



        public void LogError(object message, float displayTime)
        {
            DebugPlug.DebugPlugin.LogMsg(message, displayTime, SharpDX.Color.Red);
        }
        public void LogMessage(object message, float displayTime)
        {
            DebugPlug.DebugPlugin.LogMsg(message, displayTime);
        }
    }
}
