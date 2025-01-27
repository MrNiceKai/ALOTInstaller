﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management;   //This namespace is used to work with WMI classes. For using this namespace add reference of System.Management.dll .
using Microsoft.Win32;     //This namespace is used to work with Registry editor.
using System.IO;
using AlotAddOnGUI.classes;
using System.Runtime.InteropServices;
using System.Threading;
using Serilog;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Xml;
using System.Windows;
using System.Xml.Linq;
using System.Security.Cryptography;
using SlavaGu.ConsoleAppLauncher;
using System.ComponentModel;
using ByteSizeLib;
using System.Globalization;
using ME3Explorer.Packages;
using Microsoft.AppCenter.Analytics;

namespace AlotAddOnGUI
{
    public class Utilities
    {
        public const uint MEMI_TAG = 0x494D454D;

        public const int WIN32_EXCEPTION_ELEVATED_CODE = -98763;
        [DllImport("kernel32.dll")]
        static extern uint GetLastError();
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetPhysicallyInstalledSystemMemory(out long TotalMemoryInKilobytes);
        public static string GetOperatingSystemInfo()
        {
            StringBuilder sb = new StringBuilder();
            OperatingSystem os = Environment.OSVersion;
            Version osBuildVersion = os.Version;

            //Windows 10 only
            string releaseId = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId", "").ToString();
            string productName = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName", "").ToString();
            string verLine = productName;
            if (osBuildVersion.Major == 10)
            {
                verLine += " " + releaseId;
            }
            sb.AppendLine(verLine);
            sb.AppendLine("Version " + osBuildVersion);
            sb.AppendLine(GetCPUString());
            long ramInBytes = Utilities.GetInstalledRamAmount();
            sb.AppendLine("System Memory: " + ByteSize.FromKiloBytes(ramInBytes));
            sb.AppendLine("System Culture: " + Thread.CurrentThread.CurrentCulture.Name);
            return sb.ToString();
        }

        public static string GetCPUString()
        {
            string str = "";
            ManagementObjectSearcher mosProcessor = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            try
            {
                foreach (ManagementObject moProcessor in mosProcessor.Get())
                {
                    if (str != "")
                    {
                        str += "\n";
                    }

                    if (moProcessor["name"] != null)
                    {
                        str += moProcessor["name"].ToString();
                        str += "\n";
                    }
                    if (moProcessor["maxclockspeed"] != null)
                    {
                        str += "Maximum reported clock speed: ";
                        str += moProcessor["maxclockspeed"].ToString();
                        str += " Mhz\n";
                    }
                    if (moProcessor["numberofcores"] != null)
                    {
                        str += "Cores: ";

                        str += moProcessor["numberofcores"].ToString();
                        str += "\n";
                    }
                    if (moProcessor["numberoflogicalprocessors"] != null)
                    {
                        str += "Logical processors: ";
                        str += moProcessor["numberoflogicalprocessors"].ToString();
                        str += "\n";
                    }

                }
                return str
                   .Replace("(TM)", "™")
                   .Replace("(tm)", "™")
                   .Replace("(R)", "®")
                   .Replace("(r)", "®")
                   .Replace("(C)", "©")
                   .Replace("(c)", "©")
                   .Replace("    ", " ")
                   .Replace("  ", " ").Trim();
            }
            catch
            {
                return "Access denied: Not authorized to get CPU information\n";
            }
        }

        public static bool IsWindows10OrNewer()
        {
            var os = Environment.OSVersion;
            return os.Platform == PlatformID.Win32NT &&
                   (os.Version.Major >= 10);
        }

        /// <summary> Checks for write access for the given file.
        /// </summary>
        /// <param name="fileName">The filename.</param>
        /// <returns>true, if write access is allowed, otherwise false</returns>
        public static bool IsDirectoryWritable(string dir)
        {
            var files = Directory.GetFiles(dir);
            try
            {
                System.IO.File.Create(Path.Combine(dir, "temp_alot.txt")).Close();
                System.IO.File.Delete(Path.Combine(dir, "temp_alot.txt"));
                return true;
            }
            catch (System.UnauthorizedAccessException)
            {
                return false;
            }
            catch (Exception e)
            {
                Log.Error("Error checking permissions to folder: " + dir);
                Log.Error("Directory write test had error that was not UnauthorizedAccess: " + e.Message);
            }
            return false;
        }

        public static bool IsDirectoryWritable2(string dirPath)
        {
            try
            {
                using (FileStream fs = File.Create(
                    Path.Combine(
                        dirPath,
                        Path.GetRandomFileName()
                    ),
                    1,
                    FileOptions.DeleteOnClose)
                )
                { }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <summary>
        /// Gets the MEM game path. If the MEM game path is not set, the one from the registry is used.
        /// </summary>
        /// <param name="gameID"></param>
        /// <returns></returns>
        public static String GetGamePath(int gameID, bool allowMissingEXE = false)
        {
            Utilities.WriteDebugLog("Looking up game path for Mass Effect " + gameID + ", allow missing EXE: " + allowMissingEXE);
            //Read config file.
            string path = null;
            string mempath = null;
            string inipath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "MassEffectModder");
            inipath = Path.Combine(inipath, "MassEffectModder.ini");
            Utilities.WriteDebugLog("MEM ini path " + inipath);

            if (File.Exists(inipath))
            {
                Utilities.WriteDebugLog("ini exists - loading mem ini");

                IniFile configIni = new IniFile(inipath);
                string key = "ME" + gameID;
                path = configIni.Read(key, "GameDataPath");
                if (path != null && path != "")
                {
                    path = path.TrimEnd(Path.DirectorySeparatorChar);
                    mempath = path;
                    Utilities.WriteDebugLog("gamepath from mem ini: " + mempath);

                    string GameEXEPath = "";
                    switch (gameID)
                    {
                        case 1:
                            GameEXEPath = Path.Combine(path, @"Binaries\MassEffect.exe");
                            break;
                        case 2:
                            GameEXEPath = Path.Combine(path, @"Binaries\MassEffect2.exe");
                            break;
                        case 3:
                            GameEXEPath = Path.Combine(path, @"Binaries\Win32\MassEffect3.exe");
                            break;
                    }

                    if (!File.Exists(GameEXEPath))
                    {
                        Utilities.WriteDebugLog("mem path has missing exe, not using mem path: " + GameEXEPath);
                        path = null; //mem path is not valid. might still be able to return later.
                    }
                    else
                    {
                        Utilities.WriteDebugLog("Using mem path: " + GameEXEPath);
                        return path;
                    }
                }
                else
                {
                    Utilities.WriteDebugLog("mem ini does not have path for this game.");
                }
            }

            //does not exist in ini (or ini does not exist).
            string softwareKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\";
            string key64 = @"Wow6432Node\";
            string gameKey = @"BioWare\Mass Effect";
            string entry = "Path";

            if (gameID == 2)
                gameKey += @" 2";
            else if (gameID == 3)
            {
                gameKey += @" 3";
                entry = "Install Dir";
            }

            path = (string)Registry.GetValue(softwareKey + gameKey, entry, null);
            if (path == null)
            {
                path = (string)Registry.GetValue(softwareKey + key64 + gameKey, entry, null);
            }
            if (path != null)
            {
                Utilities.WriteDebugLog("Found game path via registry: " + path);
                path = path.TrimEnd(Path.DirectorySeparatorChar);

                string GameEXEPath = "";
                switch (gameID)
                {
                    case 1:
                        GameEXEPath = Path.Combine(path, @"Binaries\MassEffect.exe");
                        break;
                    case 2:
                        GameEXEPath = Path.Combine(path, @"Binaries\MassEffect2.exe");
                        break;
                    case 3:
                        GameEXEPath = Path.Combine(path, @"Binaries\Win32\MassEffect3.exe");
                        break;
                }
                Utilities.WriteDebugLog("GetGamePath Registry EXE Check Path: " + GameEXEPath);

                if (File.Exists(GameEXEPath))
                {
                    Utilities.WriteDebugLog("EXE file exists - returning this path: " + GameEXEPath);
                    return path; //we have path now
                }
            }
            else
            {
                Utilities.WriteDebugLog("Could not find game via registry.");
            }
            if (mempath != null && allowMissingEXE)
            {
                Utilities.WriteDebugLog("mem path not null and we allow missing EXEs. Returning " + mempath);
                return mempath;
            }
            Utilities.WriteDebugLog("No path found. Returning null");
            return null;
        }

        public static void WriteDebugLog(string v)
        {
            if (MainWindow.DEBUG_LOGGING)
            {
                Log.Debug(v);
            }
        }

        public static string GetGameEXEPath(int game)
        {
            string path = GetGamePath(game);
            if (path == null) { return null; }
            switch (game)
            {
                case 1:
                    Utilities.WriteDebugLog("GetEXE ME1 Path: " + Path.Combine(path, @"Binaries\MassEffect.exe"));
                    return Path.Combine(path, @"Binaries\MassEffect.exe");
                case 2:
                    Utilities.WriteDebugLog("GetEXE ME2 Path: " + Path.Combine(path, @"Binaries\MassEffect.exe"));
                    return Path.Combine(path, @"Binaries\MassEffect2.exe");
                case 3:
                    Utilities.WriteDebugLog("GetEXE ME3 Path: " + Path.Combine(path, @"Binaries\MassEffect.exe"));
                    return Path.Combine(path, @"Binaries\Win32\MassEffect3.exe");
            }
            return null;
        }

        public static string ReadLockedTextFile(string file)
        {
            try
            {
                using (FileStream fileStream = new FileStream(
                    file,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite))
                {
                    using (StreamReader streamReader = new StreamReader(fileStream))
                    {
                        return streamReader.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static bool IsDirectoryEmpty(string path)
        {
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }

        internal static void WriteRegistryKey(RegistryKey subkey, string subpath, string value, string data)
        {
            int i = 0;
            string[] subkeys = subpath.Split('\\');
            while (i < subkeys.Length)
            {
                subkey = subkey.CreateSubKey(subkeys[i]);
                i++;
            }
            subkey.SetValue(value, data);
        }

        internal static void WriteRegistryKey(RegistryKey subkey, string subpath, string value, bool data)
        {
            WriteRegistryKey(subkey, subpath, value, data ? 1 : 0);
        }

        internal static void WriteRegistryKey(RegistryKey subkey, string subpath, string value, int data)
        {
            int i = 0;
            string[] subkeys = subpath.Split('\\');
            while (i < subkeys.Length)
            {
                subkey = subkey.CreateSubKey(subkeys[i]);
                i++;
            }
            subkey.SetValue(value, data);
        }

        /// <summary>
        /// Gets an ALOT registry setting string.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetRegistrySettingString(string name)
        {
            string softwareKey = @"HKEY_CURRENT_USER\" + MainWindow.REGISTRY_KEY;
            return (string)Registry.GetValue(softwareKey, name, null);
        }

        /// <summary>
        /// Gets a string value frmo the registry from the specified key and value name.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetRegistrySettingString(string key, string name)
        {
            return (string)Registry.GetValue(key, name, null);
        }

        public static bool? GetRegistrySettingBool(string name)
        {
            string softwareKey = @"HKEY_CURRENT_USER\" + MainWindow.REGISTRY_KEY;

            int? value = (int?)Registry.GetValue(softwareKey, name, null);
            if (value != null)
            {
                return value > 0;
            }
            return null;
        }

        public static string GetGameBackupPath(int game)
        {
            string entry = "";
            string path = null;
            switch (game)
            {
                case 1:
                    entry = "ME1VanillaBackupLocation";
                    path = Utilities.GetRegistrySettingString(entry);
                    break;
                case 2:
                    entry = "ME2VanillaBackupLocation";
                    path = Utilities.GetRegistrySettingString(entry);
                    break;
                case 3:
                    //Check for backup via registry - Use Mod Manager's game backup key to find backup.
                    string softwareKey = @"HKEY_CURRENT_USER\SOFTWARE\Mass Effect 3 Mod Manager";
                    entry = "VanillaCopyLocation";
                    path = Utilities.GetRegistrySettingString(softwareKey, entry);
                    break;
                default:
                    return null;
            }
            if (path == null || !Directory.Exists(path))
            {
                return null;
            }
            if (!Directory.Exists(path + @"\BIOGame") || !Directory.Exists(path + @"\Binaries"))
            {
                return null;
            }
            return path;
        }

        // Pinvoke for API function
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetDiskFreeSpaceEx(string lpDirectoryName,
        out ulong lpFreeBytesAvailable,
        out ulong lpTotalNumberOfBytes,
        out ulong lpTotalNumberOfFreeBytes);

        public static bool DriveFreeBytes(string folderName, out ulong freespace)
        {
            freespace = 0;
            if (string.IsNullOrEmpty(folderName))
            {
                throw new ArgumentNullException("folderName");
            }

            if (!folderName.EndsWith("\\"))
            {
                folderName += '\\';
            }

            ulong free = 0, dummy1 = 0, dummy2 = 0;

            if (GetDiskFreeSpaceEx(folderName, out free, out dummy1, out dummy2))
            {
                freespace = free;
                return true;
            }
            else
            {
                return false;
            }
        }

        public static string GetRelativePath(string filespec, string folder)
        {
            Uri pathUri = new Uri(filespec);
            // Folders must end in a slash
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                folder += Path.DirectorySeparatorChar;
            }
            Uri folderUri = new Uri(folder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        internal static Tuple<bool, string> GetRawGameSourceByHash(int game, string hash)
        {
            List<KeyValuePair<string, string>> list = null;
            switch (game)
            {
                case 1:
                    list = SUPPORTED_HASHES_ME1;
                    break;
                case 2:
                    list = SUPPORTED_HASHES_ME2;
                    break;
                case 3:
                    list = SUPPORTED_HASHES_ME3;
                    break;
            }

            foreach (KeyValuePair<string, string> hashPair in list)
            {
                if (hashPair.Key == hash)
                {
                    return new Tuple<bool, string>(true, "Game source: " + hashPair.Value);
                }
            }
            return new Tuple<bool, string>(false, "Unknown source - this installation is not supported.");
        }

        internal static void LogGameSourceByHash(int game, string hash)
        {
            Tuple<bool, string> supportStatus = GetRawGameSourceByHash(game, hash);
            if (supportStatus.Item1)
            {
                //supported
                Log.Warning("Executable hash: " + hash + ", " + supportStatus.Item2);
                Analytics.TrackEvent("Game source", new Dictionary<string, string>() { { "SourceME" + game, supportStatus.Item2 } });

            }
            else
            {
                Log.Fatal("This installation is not supported. Only official copies of the game are supported.");
                Log.Fatal("Executable hash: " + hash + ", " + supportStatus.Item2);
                Analytics.TrackEvent("Game source", new Dictionary<string, string>() { { "Unknown SourceME" + game, hash } });
            }
        }

        public static List<KeyValuePair<string, string>> SUPPORTED_HASHES_ME1 = new List<KeyValuePair<string, string>>();
        public static List<KeyValuePair<string, string>> SUPPORTED_HASHES_ME2 = new List<KeyValuePair<string, string>>();
        public static List<KeyValuePair<string, string>> SUPPORTED_HASHES_ME3 = new List<KeyValuePair<string, string>>();

        /// <summary>
        /// Checks if a hash string is in the list of supported hashes.
        /// </summary>
        /// <param name="game">Game ID</param>
        /// <param name="hash">Executable hash</param>
        /// <returns>True if found, false otherwise</returns>
        public static bool CheckIfHashIsSupported(int game, string hash)
        {
            List<KeyValuePair<string, string>> list = null;
            switch (game)
            {
                case 1:
                    list = SUPPORTED_HASHES_ME1;
                    break;
                case 2:
                    list = SUPPORTED_HASHES_ME2;
                    break;
                case 3:
                    list = SUPPORTED_HASHES_ME3;
                    break;
            }

            foreach (KeyValuePair<string, string> hashPair in list)
            {
                if (hashPair.Key == hash)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool GetME1LAAEnabled()
        {
            string exePath = Utilities.GetGameEXEPath(1);
            if (File.Exists(exePath))
            {
                using (FileStream fs = new FileStream(exePath, FileMode.Open, FileAccess.Read))
                {
                    fs.JumpTo(0x3C); // jump to offset of COFF header
                    uint offset = fs.ReadUInt32() + 4; // skip PE signature too
                    fs.JumpTo(offset + 0x12); // jump to flags entry
                    ushort flag = fs.ReadUInt16(); // read flags
                    return (flag & 0x20) == 0x20; // check for LAA flag
                }
            }
            return false;
        }

        public static bool DeleteFilesAndFoldersRecursively(string target_dir)
        {
            bool result = true;
            foreach (string file in Directory.GetFiles(target_dir))
            {
                File.SetAttributes(file, FileAttributes.Normal); //remove read only
                try
                {
                    //Debug.WriteLine("Deleting file: " + file);
                    File.Delete(file);
                }
                catch (Exception e)
                {
                    Log.Error("Unable to delete file: " + file + ". It may be open still: " + e.Message);
                    return false;
                }
            }

            foreach (string subDir in Directory.GetDirectories(target_dir))
            {
                result &= DeleteFilesAndFoldersRecursively(subDir);
            }

            Thread.Sleep(4); // This makes the difference between whether it works or not. Sleep(0) is not enough.
            try
            {
                //Debug.WriteLine("Deleting directory: " + target_dir);

                Directory.Delete(target_dir);
            }
            catch (Exception e)
            {
                Log.Error("Unable to delete directory: " + target_dir + ". It may be open still. " + e.Message);
                return false;
            }
            return result;
        }

        public static string CalculateMD5(string filename)
        {
            try
            {
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(filename))
                    {
                        var hash = md5.ComputeHash(stream);
                        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    }
                }
            }
            catch (IOException e)
            {
                Log.Error("I/O ERROR CALCULATING CHECKSUM OF FILE: " + filename);
                Log.Error("This is a critical error - this system may have hardware issues.");
                Log.Error(App.FlattenException(e));
                return "";
            }
        }

        public static void RemoveRunAsAdminXPSP3FromME1()
        {
            string gamePath = GetGamePath(1);
            gamePath += "\\Binaries\\MassEffect.exe";
            var compatKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers", true);
            if (compatKey != null)
            {
                string compatString = (string)compatKey.GetValue(gamePath, null);
                if (compatString != null) //has compat setting
                {
                    string[] compatsettings = compatString.Split(' ');
                    List<string> newSettings = new List<string>();

                    foreach (string str in compatsettings)
                    {
                        switch (str)
                        {
                            case "~":
                            case "RUNASADMIN":
                            case "WINXPSP3":
                                continue;
                            default:
                                newSettings.Add(str);
                                break;
                        }
                    }

                    if (newSettings.Count > 0)
                    {
                        string newcompatString = "~";
                        foreach (string compatitem in newSettings)
                        {
                            newcompatString += " " + compatitem;
                        }
                        if (newcompatString == compatString)
                        {
                            return;
                        }
                        else
                        {
                            compatKey.SetValue(gamePath, newcompatString);
                            Log.Information("New stripped compatibility string: " + newcompatString);
                        }
                    }
                    else
                    {
                        compatKey.DeleteValue(gamePath);
                        Log.Information("Removed compatibility settings for ME1.");
                    }
                }
            }
        }

        public static bool InstallBinkw32Bypass(int game)
        {
            try
            {
                Log.Information("Installing binkw32 for Mass Effect " + game);
                string gamePath = GetGamePath(game);
                switch (game)
                {
                    case 1:
                        gamePath += "\\Binaries\\";
                        System.IO.File.WriteAllBytes(gamePath + "binkw23.dll", AlotAddOnGUI.Properties.Resources.me1_binkw23);
                        System.IO.File.WriteAllBytes(gamePath + "binkw32.dll", AlotAddOnGUI.Properties.Resources.me1_binkw32);
                        break;
                    case 2:
                        gamePath += "\\Binaries\\";
                        System.IO.File.WriteAllBytes(gamePath + "binkw23.dll", AlotAddOnGUI.Properties.Resources.me2_binkw23);
                        System.IO.File.WriteAllBytes(gamePath + "binkw32.dll", AlotAddOnGUI.Properties.Resources.me2_binkw32);
                        break;
                    case 3:
                        gamePath += "\\Binaries\\Win32\\";
                        System.IO.File.WriteAllBytes(gamePath + "binkw23.dll", AlotAddOnGUI.Properties.Resources.me3_binkw23);
                        System.IO.File.WriteAllBytes(gamePath + "binkw32.dll", AlotAddOnGUI.Properties.Resources.me3_binkw32);
                        break;
                }
                Log.Information("Installed binkw32 for Mass Effect " + game);
                return true;
            }
            catch (Exception e)
            {
                Log.Error("Unable to install binkw32: " + e.Message);
                Log.Error(App.FlattenException(e));
                Log.Error("DLC will not authenticate.");
            }
            return false;
        }

        /// <summary>
        /// Creates a marker file using the specified information as well as the current MEM (no GUI) and Installer release version
        /// </summary>
        /// <param name="game"></param>
        /// <param name="alotVersionInfo"></param>
        public static void CreateMarkerFile(int game, ALOTVersionInfo alotVersionInfo)
        {
            using (FileStream fs = new FileStream(GetALOTMarkerFilePath(game), FileMode.Open, FileAccess.Write))
            {
                fs.SeekEnd();
                fs.WriteInt32(alotVersionInfo.MEUITMVER); //MEUITM version.
                fs.WriteInt16(alotVersionInfo.ALOTVER); //-12 //ALOT Primary
                fs.WriteByte(alotVersionInfo.ALOTUPDATEVER); //-10 //ALOT Update
                fs.WriteByte(alotVersionInfo.ALOTHOTFIXVER); //-9 //Hotfix version (not used)

                //Get versions
                var versInfo = FileVersionInfo.GetVersionInfo(MainWindow.BINARY_DIRECTORY + "MassEffectModderNoGui.exe");
                short memVersionUsed = (short)versInfo.FileMajorPart;

                //Get current installer release version
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
                short installerVersionUsed = Convert.ToInt16(fvi.FileBuildPart);

                fs.WriteInt16(memVersionUsed); // -8
                fs.WriteInt16(installerVersionUsed); // -6
                fs.WriteUInt32(MEMI_TAG); // -4
            }
        }


        public static bool GrantAccess(string fullPath)
        {
            try
            {
                DirectoryInfo dInfo = new DirectoryInfo(fullPath);
                DirectorySecurity dSecurity = dInfo.GetAccessControl();
                dSecurity.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.FullControl, InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, PropagationFlags.NoPropagateInherit, AccessControlType.Allow));
                dInfo.SetAccessControl(dSecurity);
            }
            catch (Exception e)
            {
                Log.Error("Error granting write access: " + e.Message);
                return false;
            }
            return true;
        }

        internal static string GetALOTMarkerFilePath(int gameID)
        {
            string gamePath = Utilities.GetGamePath(gameID);
            if (gamePath != null)
            {

                if (gameID == 1)
                {
                    gamePath += @"\BioGame\CookedPC\testVolumeLight_VFX.upk";
                }
                if (gameID == 2)
                {
                    gamePath += @"\BioGame\CookedPC\BIOC_Materials.pcc";
                }
                if (gameID == 3)
                {
                    gamePath += @"\BIOGame\CookedPCConsole\adv_combat_tutorial_xbox_D_Int.afc";
                }
                return gamePath;
            }
            return null;
        }

        public static ALOTVersionInfo GetInstalledALOTInfo(int gameID)
        {
            string gamePath = Utilities.GetALOTMarkerFilePath(gameID);
            if (gamePath != null && File.Exists(gamePath))
            {
                try
                {
                    using (FileStream fs = new FileStream(gamePath, System.IO.FileMode.Open, FileAccess.Read))
                    {
                        fs.SeekEnd();
                        long endPos = fs.Position;
                        fs.Position = endPos - 4;
                        uint memi = fs.ReadUInt32();

                        if (memi == MEMI_TAG)
                        {
                            //ALOT has been installed
                            fs.Position = endPos - 8;
                            int installerVersionUsed = fs.ReadInt32();
                            int perGameFinal4Bytes = -20;
                            switch (gameID)
                            {
                                case 1:
                                    perGameFinal4Bytes = 0;
                                    break;
                                case 2:
                                    perGameFinal4Bytes = 4352;
                                    break;
                                case 3:
                                    perGameFinal4Bytes = 16777472;
                                    break;
                            }

                            if (installerVersionUsed >= 10 && installerVersionUsed != perGameFinal4Bytes) //default bytes before 178 MEMI Format
                            {
                                fs.Position = endPos - 12;
                                short ALOTVER = fs.ReadInt16();
                                byte ALOTUPDATEVER = (byte)fs.ReadByte();
                                byte ALOTHOTFIXVER = (byte)fs.ReadByte();

                                //unused for now
                                fs.Position = endPos - 16;
                                int MEUITMVER = fs.ReadInt32();

                                return new ALOTVersionInfo(ALOTVER, ALOTUPDATEVER, ALOTHOTFIXVER, MEUITMVER);
                            }
                            else
                            {
                                return new ALOTVersionInfo(0, 0, 0, 0); //MEMI tag but no info we know of
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error("Error reading marker file for Mass Effect " + gameID + ". ALOT Info will be returned as null (nothing installed). " + e.Message);
                    return null;
                }
            }
            return null;
        }
        private static FileAttributes RemoveAttribute(FileAttributes attributes, FileAttributes attributesToRemove)
        {
            return attributes & ~attributesToRemove;
        }

        public static bool MakeAllFilesInDirReadWrite(string directory)
        {
            Log.Information("Marking all files in directory to read-write: " + directory);
            //Log.Warning("If the application crashes after this statement, please come to to the ALOT discord - this is an issue we have not yet been able to reproduce and thus can't fix without outside assistance.");
            var di = new DirectoryInfo(directory);
            foreach (var file in di.GetFiles("*", SearchOption.AllDirectories))
            {
                if (!file.Exists)
                {
                    Log.Warning("File is no longer in the game directory: " + file.FullName);
                    Log.Error("This file was found when the read-write filescan took place, but is no longer present. The application is going to crash.");
                    Log.Error("Another session may be running, or there may be a bug. Please come to the ALOT Discord so we can analyze this as we cannot reproduce it.");
                }

                Utilities.WriteDebugLog("Clearing read-only marker (if any) on file: " + file.FullName);
                try
                {
                    file.Attributes &= ~FileAttributes.ReadOnly;
                }
                catch (DirectoryNotFoundException ex)
                {
                    if (file.FullName.Length > 260)
                    {
                        Log.Error("Path of file is too long - Windows API length limitations for files will cause errors. File: " + file.FullName);
                        Log.Error("The game is either nested too deep or a mod has been improperly installed causing a filepath to be too long.");
                        Log.Error(App.FlattenException(ex));
                    }
                    return false;
                }
            }
            return true;
        }


        public static int runProcess(string exe, string args, bool standAlone = false)
        {
            Log.Information("Running process: " + exe + " " + args);
            using (Process p = new Process())
            {
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.FileName = exe;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.Arguments = args;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;


                StringBuilder output = new StringBuilder();
                StringBuilder error = new StringBuilder();

                using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
                using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
                {
                    p.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            outputWaitHandle.Set();
                        }
                        else
                        {
                            output.AppendLine(e.Data);
                        }
                    };
                    p.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            errorWaitHandle.Set();
                        }
                        else
                        {
                            error.AppendLine(e.Data);
                        }
                    };

                    p.Start();
                    if (!standAlone)
                    {
                        int timeout = 600000;
                        p.BeginOutputReadLine();
                        p.BeginErrorReadLine();

                        if (p.WaitForExit(timeout) &&
                            outputWaitHandle.WaitOne(timeout) &&
                            errorWaitHandle.WaitOne(timeout))
                        {
                            // Process completed. Check process.ExitCode here.
                            Log.Information("Process standard output of " + exe + " " + args + ":");
                            if (output.ToString().Length > 0)
                            {
                                Log.Information("Standard:\n" + output.ToString());
                            }
                            if (error.ToString().Length > 0)
                            {
                                Log.Error("Error output:\n" + error.ToString());
                            }
                            return p.ExitCode;
                        }
                        else
                        {
                            // Timed out.
                            Log.Error("Process timed out: " + exe + " " + args);
                            return -1;
                        }
                    }
                    else
                    {
                        return 0; //standalone
                    }
                }
            }
        }

        internal static void CompactFile(string packageFile)
        {
            Log.Information("Loading ME3Explorer library");
            ME3ExplorerMinified.DLL.Startup();

            Log.Information("Opening package: " + packageFile);

            var package = MEPackageHandler.OpenMEPackage(packageFile);
            Log.Information("Saving package: " + packageFile);

            package.save();
            Log.Information("Saved and compacted package: " + packageFile);
        }

        internal static void TagWithALOTMarker(string packageFile)
        {
            try
            {
                using (FileStream fs = new FileStream(packageFile, FileMode.Open, FileAccess.ReadWrite))
                {
                    fs.SeekEnd();
                    fs.Seek(-App.MEMendFileMarker.Length, SeekOrigin.Current);
                    string marker = fs.ReadStringASCII(App.MEMendFileMarker.Length);
                    if (marker != App.MEMendFileMarker)
                    {
                        fs.SeekEnd();
                        fs.WriteStringASCII(App.MEMendFileMarker);
                        Log.Information("Re-tagged file with ALOT Marker");
                    }
                    else
                    {
                        Log.Information("File already tagged with ALOT marker, skipping re-tagging");
                    }
                }
            }
            catch
            {
                Log.Error("Failed to tag file with ALOT marker!");
            }
        }

        public static bool InstallME3ASIs()
        {
            Log.Information("Installing ME3Logger_truncating.asi...");
            try
            {
                string path = Utilities.GetGamePath(3);
                path = Path.Combine(path, "Binaries", "Win32", "asi");
                Directory.CreateDirectory(path);
                path = Path.Combine(path, "ME3Logger_truncating.asi");
                File.WriteAllBytes(path, AlotAddOnGUI.Properties.Resources.ME3Logger_truncating);
                Log.Information("Installed ME3Logger_truncating.asi");
            }
            catch (Exception ex)
            {
                Log.Error("Failed to install me3logger_truncating.asi: " + ex.Message);
                return false;
            }

            try
            {
                if (CheckIfHashIsSupported(3, CalculateMD5(Utilities.GetGameEXEPath(3))))
                {
                    Log.Information("Installing AutoTOC.asi...");
                    string path = Path.Combine(Utilities.GetGamePath(3), "Binaries", "Win32", "asi");
                    Directory.CreateDirectory(path);
                    path = Path.Combine(path, "AutoTOC.asi");
                    File.WriteAllBytes(path, AlotAddOnGUI.Properties.Resources.AutoTOC);
                    Log.Information("Installed AutoTOC.asi");
                }
                else
                {
                    Log.Error("Installation is not supported - not installed autotoc asi");
                }
            }
            catch (Exception ex)
            {
                Log.Error("Failed to install (or doing precheck) for AutoTOC.asi: " + ex.Message);
                return false;
            }

            return true;
        }

        public static Task<List<string>> GetArchiveFileListing(string archive)
        {
            string path = MainWindow.BINARY_DIRECTORY + "7z.exe";
            string args = "l \"" + archive + "\"";

            Log.Information("Running 7z archive inspector process: 7z " + args);
            ConsoleApp ca = new ConsoleApp(path, args);
            int startindex = 0;
            List<string> files = new List<string>();
            ca.ConsoleOutput += (o, args2) =>
            {
                if (startindex < 0)
                {
                    return;
                }
                if (args2.Line.Contains("------------------------"))
                {
                    if (startindex > 0)
                    {
                        //we found final line
                        startindex = -1;
                        return;
                    }
                    startindex = args2.Line.IndexOf("------------------------"); //this is such a hack...
                    return;
                }
                if (startindex > 0)
                {
                    files.Add(args2.Line.Substring(startindex));
                }
            };
            ca.Run();
            ca.WaitForExit();
            return Task.FromResult<List<string>>(files);
        }

        public static int runProcessAsAdmin(string exe, string args, bool standAlone = false, bool createWindow = false)
        {
            Log.Information("Running process as admin: " + exe + " " + args);
            using (Process p = new Process())
            {
                p.StartInfo.CreateNoWindow = createWindow;
                p.StartInfo.FileName = exe;
                p.StartInfo.UseShellExecute = true;
                p.StartInfo.Arguments = args;
                p.StartInfo.Verb = "runas";
                try
                {
                    p.Start();
                    if (!standAlone)
                    {
                        p.WaitForExit(60000);
                        try
                        {
                            return p.ExitCode;
                        }
                        catch (Exception e)
                        {
                            Log.Error("Error getting return code from admin process. It may have timed out.\n" + App.FlattenException(e));
                            return -1;
                        }
                    }
                    else
                    {
                        return 0;
                    }
                }
                catch (System.ComponentModel.Win32Exception e)
                {
                    Log.Error("Error running elevated process: " + e.Message);
                    return WIN32_EXCEPTION_ELEVATED_CODE;
                }
            }
        }

        public static Task DeleteAsync(string path)
        {
            if (!File.Exists(path))
            {
                return Task.FromResult(0);
            }
            return Task.Run(() => { File.Delete(path); });
        }

        public static Task<FileStream> CreateAsync(string path)
        {
            if (path == null || path == "")
            {
                return null;
            }
            return Task.Run(() => File.Create(path));
        }

        public static Task MoveAsync(string sourceFileName, string destFileName)
        {
            if (sourceFileName == null || sourceFileName == "")
            {
                return null;
            }
            if (!File.Exists(sourceFileName))
            {
                return null;
            }
            return Task.Run(() => { File.Move(sourceFileName, destFileName); });
        }

        public static void TurnOffOriginAutoUpdate()
        {
            Log.Information("Attempting to disable auto update support in Origin");
            string appdatapath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Origin");
            if (Directory.Exists(appdatapath))
            {
                var appdatafiles = Directory.GetFiles(appdatapath, "local_*.xml");
                foreach (string configfile in appdatafiles)
                {
                    try
                    {
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.Load(configfile);

                        XmlNode node = xmlDoc.SelectSingleNode("/Settings/Setting[@key='AutoPatch']");
                        if (node == null)
                        {
                            node = xmlDoc.CreateNode("element", "Setting", "");
                            xmlDoc.SelectSingleNode("/Settings").AppendChild(node);
                        }
                        XmlAttribute attr = xmlDoc.CreateAttribute("type");
                        attr.Value = 1.ToString();
                        SetAttrSafe(node, attr);

                        attr = xmlDoc.CreateAttribute("value");
                        attr.Value = "false";
                        SetAttrSafe(node, attr);

                        attr = xmlDoc.CreateAttribute("key");
                        attr.Value = "AutoPatch";
                        SetAttrSafe(node, attr);
                        xmlDoc.Save(configfile);
                        Log.Information("Updated file with autopatch off: " + configfile);
                    }
                    catch (Exception e)
                    {
                        Log.Error("Unable to turn off origin in game for file " + configfile + ": " + e.Message);
                    }
                }
            }
            else
            {
                Log.Warning("Origin folder does not exist: " + appdatapath);
            }
        }
        public static void TurnOffOriginAutoUpdateForGame(int game)
        {
            Log.Information("Attempting to disable auto update support for game: " + game);
            string gamePath = GetGamePath(game);
            if (gamePath != null && Directory.Exists(gamePath))
            {
                gamePath += @"\__Installer\installerdata.xml";
                if (File.Exists(gamePath))
                {
                    //Origin installer file
                    string newValue = string.Empty;
                    XmlDocument xmlDoc = new XmlDocument();

                    xmlDoc.Load(gamePath);

                    XmlNode node = xmlDoc.SelectSingleNode("game/metadata/featureFlags");
                    if (node != null)
                    {
                        //set settings same as me3
                        XmlAttribute attr = xmlDoc.CreateAttribute("autoUpdateEnabled");
                        attr.Value = 0.ToString();
                        SetAttrSafe(node, attr);

                        attr = xmlDoc.CreateAttribute("useGameVersionFromManifestEnabled");
                        attr.Value = 1.ToString();
                        SetAttrSafe(node, attr);

                        attr = xmlDoc.CreateAttribute("treatUpdatesAsMandatory");
                        attr.Value = 0.ToString();
                        SetAttrSafe(node, attr);

                        attr = xmlDoc.CreateAttribute("forceTouchupInstallerAfterUpdate");
                        attr.Value = 0.ToString();
                        SetAttrSafe(node, attr);

                        attr = xmlDoc.CreateAttribute("useGameVersionFromManifestEnabled");
                        attr.Value = 1.ToString();
                        SetAttrSafe(node, attr);

                        attr = xmlDoc.CreateAttribute("enableDifferentialUpdate");
                        attr.Value = 1.ToString();
                        SetAttrSafe(node, attr);

                        xmlDoc.Save(gamePath);
                    }
                }
                else
                {
                    Log.Information("Installer manifest does not exist. This does not appear to be an origin installation. Skipping this step");
                }
            }
        }

        private static void SetAttrSafe(XmlNode node, params XmlAttribute[] attrList)
        {
            foreach (var attr in attrList)
            {
                if (node.Attributes[attr.Name] != null)
                {
                    node.Attributes[attr.Name].Value = attr.Value;
                }
                else
                {
                    node.Attributes.Append(attr);
                }
            }
        }

        public static long GetInstalledRamAmount()
        {
            long memKb;
            GetPhysicallyInstalledSystemMemory(out memKb);
            if (memKb == 0L)
            {
                uint errorcode = GetLastError();
                string errorMessage = new Win32Exception(Marshal.GetLastWin32Error()).Message;
                Log.Warning("Failed to get RAM amount. This may indicate a potential (or soon coming) hardware problem. The error message was: " + errorMessage);
            }
            return memKb;
        }

        public static bool isRunningOnAMD()
        {
            var processorIdentifier = System.Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER");
            return processorIdentifier != null && processorIdentifier.Contains("AuthenticAMD");
        }

        public static bool TestXMLIsValid(string inputXML)
        {
            try
            {
                XDocument.Parse(inputXML);
                return true;
            }
            catch (XmlException)
            {
                return false;
            }
        }

        public static double GetDouble(string value, double defaultValue)
        {
            double result;

            // Try parsing in the current culture
            if (!double.TryParse(value, System.Globalization.NumberStyles.Any, CultureInfo.CurrentCulture, out result) &&
                // Then try in US english
                !double.TryParse(value, System.Globalization.NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out result) &&
                // Then in neutral language
                !double.TryParse(value, System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out result))
            {
                result = defaultValue;
            }
            return result;
        }

        public static string sha256(string randomString)
        {
            System.Security.Cryptography.SHA256Managed crypt = new System.Security.Cryptography.SHA256Managed();
            System.Text.StringBuilder hash = new System.Text.StringBuilder();
            byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(randomString), 0, Encoding.UTF8.GetByteCount(randomString));
            foreach (byte theByte in crypto)
            {
                hash.Append(theByte.ToString("x2"));
            }
            return hash.ToString();
        }

        public static bool OpenAndSelectFileInExplorer(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
            {
                return false;
            }
            //Clean up file path so it can be navigated OK
            filePath = System.IO.Path.GetFullPath(filePath);
            System.Diagnostics.Process.Start("explorer.exe", string.Format("/select,\"{0}\"", filePath));
            return true;

        }

        public static bool IsWindowOpen<T>(string name = "") where T : Window
        {
            return string.IsNullOrEmpty(name)
               ? Application.Current.Windows.OfType<T>().Any()
               : Application.Current.Windows.OfType<T>().Any(w => w.Name.Equals(name));
        }

        public static long DirSize(DirectoryInfo d)
        {
            long size = 0;
            // Add file sizes.
            FileInfo[] fis = d.GetFiles();
            foreach (FileInfo fi in fis)
            {
                size += fi.Length;
            }
            // Add subdirectory sizes.
            DirectoryInfo[] dis = d.GetDirectories();
            foreach (DirectoryInfo di in dis)
            {
                size += DirSize(di);
            }
            return size;
        }

        public static bool IsSubfolder(string parentPath, string childPath)
        {
            var parentUri = new Uri(parentPath);
            var childUri = new DirectoryInfo(childPath).Parent;
            while (childUri != null)
            {
                if (new Uri(childUri.FullName) == parentUri)
                {
                    return true;
                }
                childUri = childUri.Parent;
            }
            return false;
        }

        public static void GetAntivirusInfo()
        {
            ManagementObjectSearcher wmiData = new ManagementObjectSearcher(@"root\SecurityCenter2", "SELECT * FROM AntivirusProduct");
            ManagementObjectCollection data = wmiData.Get();

            foreach (ManagementObject virusChecker in data)
            {
                var virusCheckerName = virusChecker["displayName"];
                var productState = virusChecker["productState"];
                uint productVal = (uint)productState;
                var bytes = BitConverter.GetBytes(productVal);
                Log.Information("Antivirus info: " + virusCheckerName + " with state " + bytes[1].ToString("X2") + " " + bytes[2].ToString("X2") + " " + bytes[3].ToString("X2"));
            }
        }

        public static bool IsGameRunning(int gameID)
        {
            if (gameID == 1)
            {
                Process[] pname = Process.GetProcessesByName("MassEffect");
                return pname.Length > 0;
            }
            if (gameID == 2)
            {
                Process[] pname = Process.GetProcessesByName("MassEffect2");
                Process[] pname2 = Process.GetProcessesByName("ME2Game");
                return pname.Length > 0 || pname2.Length > 0;
            }
            else
            {
                Process[] pname = Process.GetProcessesByName("MassEffect3");
                return pname.Length > 0;
            }
        }

        public static int Get45PlusFromRegistry()
        {
            const string subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";

            using (RegistryKey ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(subkey))
            {
                if (ndpKey != null && ndpKey.GetValue("Release") != null)
                {
                    return (int)ndpKey.GetValue("Release");
                }
                else
                {
                    Log.Warning(".NET Framework Version 4.5 or later is not detected.");
                }
            }
            return 0;
        }
    }
}