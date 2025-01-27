﻿using AlotAddOnGUI.classes;
using AlotAddOnGUI.ui;
using ByteSizeLib;
using MahApps.Metro;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Taskbar;
using Octokit;
using Serilog;
using SlavaGu.ConsoleAppLauncher;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Xml.Linq;
using Flurl.Http;
using System.Windows.Media;
using System.IO.Compression;
using System.Globalization;
using System.Management;
using System.Collections.ObjectModel;
using Microsoft.AppCenter.Crashes;
using Microsoft.AppCenter.Analytics;

namespace AlotAddOnGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow, INotifyPropertyChanged
    {
        private System.Windows.Controls.CheckBox[] buildOptionCheckboxes;
        public ConsoleApp BACKGROUND_MEM_PROCESS = null;
        public bool BACKGROUND_MEM_RUNNING = false;
        ProgressDialogController updateprogresscontroller;
        public const string UPDATE_ADDONUI_CURRENTTASK = "UPDATE_OPERATION_LABEL";
        public const string HIDE_TIPS = "HIDE_TIPS";
        public const string UPDATE_PROGRESSBAR_INDETERMINATE = "SET_PROGRESSBAR_DETERMINACY";
        public const string INCREMENT_COMPLETION_EXTRACTION = "INCREMENT_COMPLETION_EXTRACTION";
        public const string SHOW_DIALOG = "SHOW_DIALOG";
        public const string ERROR_OCCURED = "ERROR_OCCURED";
        public static string EXE_DIRECTORY = System.AppDomain.CurrentDomain.BaseDirectory;
        public static string BINARY_DIRECTORY = EXE_DIRECTORY + "Data\\bin\\";
        private bool errorOccured = false;
        public static bool MEUITM_INSTALLER_MODE = true;
        private bool UsingBundledManifest = false;
        private List<string> BlockingMods;
        private AddonFile meuitmFile;
        private DispatcherTimer backgroundticker;
        private DispatcherTimer tipticker;
        private int completed = 0;
        //private int addonstoinstall = 0;
        private int CURRENT_GAME_BUILD = 0; //set when extraction is run/finished
        private static readonly string ALOT_MEMFILE_NUMBER = "001";
        private int ADDONSTOBUILD_COUNT = 0;
        private bool _preventFileRefresh = true;
        private int HIGHEST_APPROVED_STABLE_MEMNOGUIVERSION = 999; //will be set by manifest
        private int SOAK_APPROVED_STABLE_MEMNOGUIVERSION = -1; //will be set by manifest
        private DateTime SOAK_START_DATE;
        private int[] SoakThresholds = { 50, 150, 400, 1000, 3000, 100000000 };
        public string CustomMEMInstallSource;
        public bool PreventFileRefresh
        {
            get => _preventFileRefresh;
            set
            {
                if (_preventFileRefresh != value)
                {
                    _preventFileRefresh = value;
                    OnPropertyChanged();
                }
            }
        }
        public const string REGISTRY_KEY = @"SOFTWARE\ALOTAddon";
        public const string ME3_BACKUP_REGISTRY_KEY = @"SOFTWARE\Mass Effect 3 Mod Manager";

        public event PropertyChangedEventHandler PropertyChanged;
        List<string> PendingUserFiles = new List<string>();
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public const string MEM_EXE_NAME = "MassEffectModderNoGui.exe";

        NotifyIcon nIcon = new NotifyIcon();
        private const string MEM_OUTPUT_DIR = "Data\\MEM_Packages";
        private const string MEM_OUTPUT_DISPLAY_DIR = "Data\\MEM_Packages";

        private const string ADDON_STAGING_DIR = "ADDON_STAGING";
        private const string USER_STAGING_DIR = "USER_STAGING";
        private const string UPDATE_STAGING_MEMNOGUI_DIR = "Data\\UpdateMemNoGui";
        private const string UPDATE_STAGING_MEM_DIR = "Data\\UpdateMem";
        private List<string> PagefileLocations = new List<string>();
        private string ADDON_FULL_STAGING_DIRECTORY = System.AppDomain.CurrentDomain.BaseDirectory + "Data\\" + ADDON_STAGING_DIR + "\\";
        private string USER_FULL_STAGING_DIRECTORY = System.AppDomain.CurrentDomain.BaseDirectory + "Data\\" + USER_STAGING_DIR + "\\";

        private bool me1Installed;
        private bool me2Installed;
        private bool me3Installed;
        private bool RefreshListOnUserImportClose = false;
        private List<string> musicpackmirrors;

        internal List<ManifestTutorial> AllTutorials { get; private set; } = new List<ManifestTutorial>();
        public ObservableCollectionExtended<AddonFile> DisplayedAddonFiles { get; set; } = new ObservableCollectionExtended<AddonFile>();
        private ObservableCollectionExtended<AddonFile> AllAddonFiles { get; set; } = new ObservableCollectionExtended<AddonFile>();
        private readonly string PRIMARY_HEADER = "Download the listed files for your game as listed below. You can filter per-game in the settings.\nDo not extract or rename any files you download. Drop them onto this interface to import them.";
        private readonly string MEUITM_PRIMARY_HEADER = "Press Install for ME1 below to install MEUITM, or click on Settings and select Switch to ALOT mode\nto switch to ALOT Installer mode, which allows additional textures to be installed.";
        public static readonly string SETTINGSTR_DEBUGLOGGING = "DebugLogging";
        private readonly string DOT_NET_DOWNLOAD_LINK_WEB = "https://download.microsoft.com/download/3/3/2/332D9665-37D5-467A-84E1-D07101375B8C/NDP472-KB4054531-Web.exe";
        private readonly string DOT_NET_REQUIRED_VERSION_MANUAL_LINK = "https://docs.microsoft.com/en-us/dotnet/framework/whats-new/index#downloading-and-installing-the-net-framework-472";
        private const int DOT_NET_REQUIRED_VERSION = 461808;
        private const string DOT_NET_REQUIRED_VERSION_HR = "4.7.2";
        private const string DISCORD_INVITE_LINK = "https://discord.gg/tTePzaa";
        private const string SETTINGSTR_DONT_FORCE_UPGRADES = "DontForceUpgrades";
        private const string SETTINGSTR_LIBRARYDIR = "LibraryDir";
        private const string SETTINGSTR_REPACK = "RepackGameFiles";
        private const string SETTINGSTR_REPACK_ME3 = "RepackGameFilesME3";
        private const string SETTINGSTR_IMPORTASMOVE = "ImportAsMove";
        public const string SETTINGSTR_BETAMODE = "BetaMode";
        public const string SETTINGSTR_LAST_BETA_ADVERT_TIME = "LastBetaAdvertisement";
        public const string SETTINGSTR_DOWNLOADSFOLDER = "DownloadsFolder";

        public const string SETTINGSTR_MANUALINSTALLPATH_ME1 = "LastManualInstallPathME1";
        public const string SETTINGSTR_MANUALINSTALLPATH_ME2 = "LastManualInstallPathME2";
        public const string SETTINGSTR_MANUALINSTALLPATH_ME3 = "LastManualInstallPathME3";

        private List<string> BACKGROUND_MEM_PROCESS_ERRORS;
        private List<string> BACKGROUND_MEM_PROCESS_PARSED_ERRORS;
        private const string SHOW_DIALOG_YES_NO = "SHOW_DIALOG_YES_NO";
        private bool CONTINUE_BACKUP_EVEN_IF_VERIFY_FAILS = false;
        private bool ERROR_SHOWING = false;
        private int PREBUILT_MEM_INDEX; //will increment to 10 when run
        private bool SHOULD_HAVE_OUTPUT_FILE;
        public static bool USING_BETA { get; private set; }
        public bool SOUND_SETTING { get; private set; }
        public StringBuilder BACKGROUND_MEM_STDOUT { get; private set; }
        public int BACKUP_THREAD_GAME { get; private set; }
        private bool _showME1Files = true;
        private bool _showME2Files = true;
        private bool _showME3Files = true;
        private bool Loading = true;
        //private int LODLIMIT = 0;
        private FrameworkElement[] fadeInItems;
        private List<FrameworkElement> currentFadeInItems = new List<FrameworkElement>();
        private bool ShowReadyFilesOnly = false;
        internal AddonDownloadAssistant DOWNLOAD_ASSISTANT_WINDOW;
        private DateTimeOffset LAST_BETA_ADVERT_TIME;
        private bool DONT_FORCE_UPGRADES = false;
        public static string DOWNLOADS_FOLDER;
        private int RefreshesUntilRealRefresh;
        private bool ShowBuildingOnly;
        private WebClient downloadClient;
        public bool ME2_REPACK_MANIFEST_ENABLED = true;
        public bool ME3_REPACK_MANIFEST_ENABLED = true;
        private string MANIFEST_LOC = EXE_DIRECTORY + @"Data\manifest.xml";
        private string MANIFEST_BUNDLED_LOC = EXE_DIRECTORY + @"Data\manifest-bundled.xml";
        private List<string> COPY_QUEUE = new List<string>();
        private List<string> MOVE_QUEUE = new List<string>();
        public DateTime bootTime;
        private DoubleAnimation userfileGameSelectoroFlashingTextAnimation;
        public static bool DEBUG_LOGGING;

        public bool ShowME1Files
        {
            get { return _showME1Files; }
            set
            {
                _showME1Files = value;
                OnPropertyChanged();
                if (!Loading)
                {
                    ApplyFiltering();
                }
            }
        }
        public bool ShowME2Files
        {
            get { return _showME2Files; }
            set
            {
                _showME2Files = value;
                OnPropertyChanged();
                if (!Loading)
                {
                    ApplyFiltering();
                }
            }
        }
        public bool ShowME3Files
        {
            get { return _showME3Files; }
            set
            {
                _showME3Files = value;
                OnPropertyChanged();
                if (!Loading)
                {
                    ApplyFiltering();
                }
            }
        }

        private int _progress_max;
        public int Progressbar_Max
        {
            get { return _progress_max; }
            set
            {
                if (value != _progress_max)
                {
                    _progress_max = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _progressBarValue;
        public static bool TELEMETRY_ALL_ADDON_FILES = false;
        private List<string> ME2DLCRequiringTextureExportFixes;
        private List<string> ME3DLCRequiringTextureExportFixes;

        public double ProgressBarValue
        {
            get { return _progressBarValue; }
            set
            {
                if (_progressBarValue != value)
                {
                    _progressBarValue = value;
                    OnPropertyChanged("ProgressBarValue");
                }
            }
        }

        public MainWindow()
        {
            Log.Information("MainWindow() is starting");
            DOWNLOADED_MODS_DIRECTORY = EXE_DIRECTORY + "Downloaded_Mods"; //This will be changed when settings load;
            Progressbar_Max = 100;
            InitializeComponent();
            App.mainWindow = this;
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(ListView_Files.ItemsSource);
            PropertyGroupDescription groupDescription = new PropertyGroupDescription("Author");
            view.GroupDescriptions.Add(groupDescription);

            LoadSettings();
            MEUITM_INSTALLER_MODE = false;

            //if (!USING_BETA)
            //{
            //    Button_LibraryDir.Visibility = Visibility.Collapsed; //Not for main use right now.
            //    Button_ManualInstallFolder.Visibility = Visibility.Collapsed;
            //}
            //else
            //{
            Button_LibraryDir.ToolTip =
                "Click to change directory imported texture mods will be stored at.\n" +
                "This is where data is stored before installation for ALOT Installer.\n" +
                "If path is not found at app startup, the default subdirectory of Downloaded_Mods will be used.\n\n" +
                "Library location currently is:\n" + DOWNLOADED_MODS_DIRECTORY;
            //}

            SetUIMode();

            Title = "ALOT Installer " + System.Reflection.Assembly.GetEntryAssembly().GetName().Version;
            HeaderLabel.Text = "Preparing application...";
            AddonFilesLabel.Text = "Please wait";
            bootTime = DateTime.Now;

            userfileGameSelectoroFlashingTextAnimation = new DoubleAnimation();
            userfileGameSelectoroFlashingTextAnimation.From = 0.3;
            userfileGameSelectoroFlashingTextAnimation.To = 1;
            userfileGameSelectoroFlashingTextAnimation.Duration = new Duration(TimeSpan.FromSeconds(.7));
            userfileGameSelectoroFlashingTextAnimation.RepeatBehavior = RepeatBehavior.Forever;
            userfileGameSelectoroFlashingTextAnimation.AutoReverse = true;

        }

        /// <summary>
        /// Configures the UI to be in ALOT or MEUITM mode
        /// </summary>
        private void SetUIMode()
        {
            if (MEUITM_INSTALLER_MODE)
            {

                StackPanel_SwitchToALOTMode.Visibility = Visibility.Visible;
                Button_InstallME1.SetValue(Grid.ColumnSpanProperty, 3);

                Button_InstallME2.Visibility = Button_InstallME3.Visibility = Panel_ALOTFiltering.Visibility =
                Label_ALOTStatus_ME2.Visibility = Label_ALOTStatus_ME3.Visibility = Button_ME2Backup.Visibility =
                Button_ME3Backup.Visibility = Checkbox_RepackME2GameFiles.Visibility = Checkbox_RepackME3GameFiles.Visibility =
                Button_DownloadAssistant.Visibility = Button_LibraryDir.Visibility =
                Button_VerifyGameME2.Visibility = Button_VerifyGameME3.Visibility = Button_AutoTOCME3.Visibility =
                    Visibility.Collapsed;
            }
            else
            {
                Button_InstallME1.SetValue(Grid.ColumnSpanProperty, 1);
                StackPanel_SwitchToALOTMode.Visibility = Visibility.Collapsed;

                Button_InstallME2.Visibility = Button_InstallME3.Visibility = Panel_ALOTFiltering.Visibility =
                Label_ALOTStatus_ME2.Visibility = Label_ALOTStatus_ME3.Visibility = Button_ME2Backup.Visibility =
                Button_ME3Backup.Visibility = Checkbox_RepackME2GameFiles.Visibility = Checkbox_RepackME3GameFiles.Visibility =
                Button_DownloadAssistant.Visibility = Button_LibraryDir.Visibility =
                Button_VerifyGameME2.Visibility = Button_VerifyGameME3.Visibility = Button_AutoTOCME3.Visibility =
                    Visibility.Visible;
                Button_InstallME2.Visibility = Visibility.Visible;
                Button_InstallME3.Visibility = Visibility.Visible;
            }
            HeaderLabel.Text = MEUITM_INSTALLER_MODE ? MEUITM_PRIMARY_HEADER : PRIMARY_HEADER;
            UpdateTutorialPanel();
            ApplyFiltering();
        }

        /// <summary>
        /// Executes a task in the future
        /// </summary>
        /// <param name="action">Action to run</param>
        /// <param name="timeoutInMilliseconds">Delay in ms</param>
        /// <returns></returns>
        /// 
        public async Task Execute(Action action, int timeoutInMilliseconds)
        {
            await Task.Delay(timeoutInMilliseconds);
            action();
        }

        private async void PerformPreUpdateCheck()
        {
            //Check local files are OK first.
            Log.Information("Checking for local supporting files");
            string sevenZ = BINARY_DIRECTORY + "7z.exe";
            string sevenZdll = BINARY_DIRECTORY + "7z.dll";
            string lzma = BINARY_DIRECTORY + "lzma.exe";
            string permissionsgranter = BINARY_DIRECTORY + "PermissionsGranter.exe";
            string[] requiredSupportingFiles = { sevenZ, sevenZdll, lzma, permissionsgranter };

            bool requiredToolDownloadRequired = false;
            foreach (string file in requiredSupportingFiles)
            {
                if (!File.Exists(file))
                {
                    requiredToolDownloadRequired = true;
                    Log.Warning("Required tool missing: " + file);
                    break;
                }
            }

            if (requiredToolDownloadRequired)
            {
                try
                {
                    Log.Warning("A required tool is missing. Downloading requirements package now.");
                    AddonFilesLabel.Text = "Downloading required application files";
                    string requiredFilesEndpoint = "https://me3tweaks.com/alot/miscbin.zip".DownloadFileAsync(EXE_DIRECTORY + "Data", "miscbin.zip").Result;
                    System.IO.Compression.ZipFile.ExtractToDirectory(EXE_DIRECTORY + "Data\\miscbin.zip", BINARY_DIRECTORY);
                    File.Delete(EXE_DIRECTORY + "Data\\miscbin.zip");
                }
                catch (Exception e)
                {
                    ThemeManager.ChangeAppStyle(System.Windows.Application.Current,
                                                    ThemeManager.GetAccent("Red"),
                                                    ThemeManager.GetAppTheme("BaseDark")); // or appStyle.Item1
                    AddonFilesLabel.Text = "Failed to download missing required files";
                    Log.Fatal("REQUIRED FILES ARE MISSING AND WERE UNABLE TO BE ACQUIRED. THE PROGRAM WILL NOT BE ABLE TO FUNCTION PROPERLY.");
                    Log.Fatal(App.FlattenException(e));
                    await this.ShowMessageAsync("Required files are unavailable", "Some required files are unavailable and could not be downloaded. These files are critical to usage of the application. Please download a new copy of ALOT Installer as this copy is not functional.");
                    Environment.Exit(1);
                }
            }
            Log.Information("Checking for .NET version " + DOT_NET_REQUIRED_VERSION_HR + "...");
            int netVersion = Utilities.Get45PlusFromRegistry();
            if (netVersion < DOT_NET_REQUIRED_VERSION)
            {
                Log.Warning(".NET " + DOT_NET_REQUIRED_VERSION_HR + " or greater is not installed.");
                //.net " + DOT_NET_REQUIRED_VERSION_HR + "
                MetroDialogSettings mds = new MetroDialogSettings();
                mds.AffirmativeButtonText = "Install";
                mds.NegativeButtonText = "Manual";
                mds.FirstAuxiliaryButtonText = "Later";
                mds.DefaultButtonFocus = MessageDialogResult.Affirmative;
                var upgradenet = await this.ShowMessageAsync(".NET upgrade required", "To continue receiving updates you'll need to install Microsoft .NET " + DOT_NET_REQUIRED_VERSION_HR + " or higher. ALOT Installer can do this for you, select Install below to download and run the installer. Alternatively you can manually install .NET " + DOT_NET_REQUIRED_VERSION_HR + " by clicking the Manual button.", MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary, mds);
                if (upgradenet == MessageDialogResult.Affirmative)
                {
                    await UpgradeDotNet();
                }
                else if (upgradenet == MessageDialogResult.Negative)
                {
                    string link = DOT_NET_REQUIRED_VERSION_MANUAL_LINK;
                    openWebPage(link);
                }
                else
                {
                    Log.Warning("User has declined .NET " + DOT_NET_REQUIRED_VERSION_HR + " install.");
                    PerformUpdateCheck(false);
                }
            }
            else
            {
                Log.Information(".NET " + DOT_NET_REQUIRED_VERSION_HR + " or greater is installed.");
                PerformUpdateCheck(true);
            }
        }

        private async void PerformUpdateCheck(bool dotNetSatisfiedForUpdate)
        {
            Log.Information("Checking for application updates from gitub");
            AddonFilesLabel.Text = "Checking for application updates";
            var versInfo = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;
            var client = new GitHubClient(new ProductHeaderValue("ALOTInstaller"));
            try
            {
                int myReleaseAge = 0;
                var releases = await client.Repository.Release.GetAll("ME3Tweaks", "ALOTInstaller");
                if (releases.Count > 0)
                {
                    Log.Information("Fetched application releases from github");

                    //The release we want to check is always the latest, so [0]
                    Release latest = null;
                    Version latestVer = new Version("0.0.0.0");
                    bool newHiddenBetaBuildAvailable = false;
                    foreach (Release r in releases)
                    {
                        Version releaseVersion = new Version(r.TagName);
                        if (!USING_BETA && r.Prerelease && versInfo.Build < releaseVersion.Build)
                        {
                            newHiddenBetaBuildAvailable = true;
                            continue;
                        }
                        if (versInfo.Major == releaseVersion.Major && versInfo.Build < releaseVersion.Build)
                        {
                            myReleaseAge++;
                        }
                        if (releaseVersion > latestVer)
                        {
                            latest = r;
                            latestVer = releaseVersion;
                        }
                    }

                    if (latest != null)
                    {
                        Log.Information("Latest available: " + latest.TagName);
                        Version releaseName = new Version(latest.TagName);
                        if (versInfo < releaseName && latest.Assets.Count > 0)
                        {
                            if (!dotNetSatisfiedForUpdate)
                            {
                                await this.ShowMessageAsync(".NET upgrade required", "An update is available for ALOT Installer, but the new version requires .NET " + DOT_NET_REQUIRED_VERSION_HR + " to be installed before the update. Restart ALOT Installer and choose the Install option at the .NET prompt.");
                                Log.Error(".NET update declined but we require an update - exiting.");
                                if ((myReleaseAge > 5 || USING_BETA) && !DONT_FORCE_UPGRADES)
                                {
                                    Environment.Exit(1);
                                }
                                else
                                {
                                    FetchManifest();
                                    return;
                                }
                            }


                            bool upgrade = false;
                            bool canCancel = true;
                            Log.Information("Latest release is applicable to us.");
                            if ((myReleaseAge > 5) && !DONT_FORCE_UPGRADES)
                            {
                                Log.Warning("This is an old release. We are force upgrading this client.");
                                upgrade = true;
                                canCancel = false;
                            }
                            else
                            {
                                string versionInfo = "";
                                if (latest.Prerelease)
                                {
                                    versionInfo += " This is a beta build. You are receiving this update because you have opted into Beta Mode in settings.";
                                }
                                int daysAgo = (DateTime.Now - latest.PublishedAt.Value).Days;
                                string ageStr = "";
                                if (daysAgo == 1)
                                {
                                    ageStr = "1 day ago";
                                }
                                else if (daysAgo == 0)
                                {
                                    ageStr = "today";
                                }
                                else
                                {
                                    ageStr = daysAgo + " days ago";
                                }

                                versionInfo += "\nReleased " + ageStr;
                                MetroDialogSettings mds = new MetroDialogSettings();
                                mds.AffirmativeButtonText = "Update";
                                mds.NegativeButtonText = "Later";
                                mds.DefaultButtonFocus = MessageDialogResult.Affirmative;

                                //MessageDialogResult result = await this.ShowMessageAsync("Update Available", "ALOT Installer " + releaseName + " is available. You are currently using version " + versInfo.ToString() + ".\n========================\n" + versionInfo + "\n" + latest.Body + "\n========================\nInstall the update?", MessageDialogStyle.AffirmativeAndNegative, mds);
                                //upgrade = result == MessageDialogResult.Affirmative;
                                string message = "ALOT Installer " + releaseName + " is available. You are currently using version " + versInfo.ToString() + "." + versionInfo;
                                UpdateAvailableDialog uad = new UpdateAvailableDialog(message, latest.Body, this);
                                await this.ShowMetroDialogAsync(uad, mds);
                                await uad.WaitUntilUnloadedAsync();
                                upgrade = uad.wasUpdateAccepted();
                            }
                            if (upgrade)
                            {
                                Log.Information("Downloading update for application");

                                //there's an update
                                string message = "Downloading update...";
                                if (!canCancel)
                                {
                                    if (!USING_BETA)
                                    {
                                        message = "This copy of ALOT Installer is outdated and must be updated.";
                                    }
                                }
                                updateprogresscontroller = await this.ShowProgressAsync("Downloading Update", message, canCancel);
                                updateprogresscontroller.SetIndeterminate();
                                WebClient downloadClient = new WebClient();

                                downloadClient.Headers["Accept"] = "application/vnd.github.v3+json";
                                downloadClient.Headers["user-agent"] = "ALOTInstaller";
                                string temppath = Path.GetTempPath();
                                int downloadProgress = 0;
                                downloadClient.DownloadProgressChanged += (s, e) =>
                                {
                                    if (downloadProgress != e.ProgressPercentage)
                                    {
                                        Log.Information("Program update download percent: " + e.ProgressPercentage);
                                    }
                                    string downloadedStr = ByteSize.FromBytes(e.BytesReceived).ToString() + " of " + ByteSize.FromBytes(e.TotalBytesToReceive).ToString();
                                    updateprogresscontroller.SetMessage(message + "\n\n" + downloadedStr);

                                    downloadProgress = e.ProgressPercentage;
                                    updateprogresscontroller.SetProgress((double)e.ProgressPercentage / 100);
                                };
                                updateprogresscontroller.Canceled += async (s, e) =>
                                {
                                    if (downloadClient != null)
                                    {
                                        Log.Information("Application update was in progress but was canceled.");
                                        downloadClient.CancelAsync();
                                        await updateprogresscontroller.CloseAsync();
                                        FetchManifest();
                                    }
                                };
                                downloadClient.DownloadFileCompleted += UnzipSelfUpdate;
                                string downloadPath = temppath + "ALOTInstaller_Update" + Path.GetExtension(latest.Assets[0].BrowserDownloadUrl);
                                //DEBUG ONLY
                                Uri downloadUri = new Uri(latest.Assets[0].BrowserDownloadUrl);
                                downloadClient.DownloadFileAsync(downloadUri, downloadPath, new KeyValuePair<ProgressDialogController, string>(updateprogresscontroller, downloadPath));
                            }
                            else
                            {
                                AddonFilesLabel.Text = "Application update declined";
                                Log.Warning("Application update was declined");
                                await this.ShowMessageAsync("Old versions are not supported", "Outdated versions of ALOT Installer are not supported and may stop working as the installer manifest and MEMNoGui are updated.");
                                FetchManifest();
                            }
                        }
                        else
                        {
                            //up to date
                            AddonFilesLabel.Text = "Application up to date";
                            Log.Information("Application is up to date.");
                            FetchManifest();
                            if (newHiddenBetaBuildAvailable && LAST_BETA_ADVERT_TIME < (DateTimeOffset.UtcNow.AddDays(-3)))
                            {
                                ShowStatus("ALOT Installer beta build is available! You can opt into betas in the settings menu.", 4000);
                                Utilities.WriteRegistryKey(Registry.CurrentUser, REGISTRY_KEY, SETTINGSTR_LAST_BETA_ADVERT_TIME, DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString());

                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error checking for update: " + e);
                FetchManifest();
            }
        }

        private async Task UpgradeDotNet()
        {
            Log.Information("Downloading .NET 4.7.2 web-installer");
            string message = "Installation will begin once the download has completed. You will need to restart your computer after installation is complete.";
            updateprogresscontroller = await this.ShowProgressAsync("Downloading .NET Web Installer", message, false);
            updateprogresscontroller.SetIndeterminate();
            string temppath = Path.GetTempPath();
            string downloadPath = Path.Combine(temppath, "NDP472-KB4054531-Web.exe");
            WebClient downloadClient = new WebClient();

            downloadClient.Headers["user-agent"] = "ALOTInstaller";
            downloadClient.DownloadProgressChanged += (s, e) =>
            {
                string downloadedStr = ByteSize.FromBytes(e.BytesReceived).ToString() + " of " + ByteSize.FromBytes(e.TotalBytesToReceive).ToString();
                updateprogresscontroller.SetMessage(message + "\n\n" + downloadedStr);
                updateprogresscontroller.SetProgress((double)e.ProgressPercentage / 100);
            };

            downloadClient.DownloadFileCompleted += async (s, e) =>
            {
                await updateprogresscontroller.CloseAsync();
                if (e.Error != null)
                {
                    Log.Error("Error downloading .NET update.");
                    Log.Error(App.FlattenException(e.Error));
                    await this.ShowMessageAsync("Error downloading installer", "Error downloading .NET update: " + e.Error.Message + ". ALOT Installer will now close.");
                    Environment.Exit(1);
                }
                try
                {
                    string argx = "/passive /promptrestart /showfinalerror";
                    int run = Utilities.runProcessAsAdmin(downloadPath, argx, true, true);
                    if (run == 0)
                    {
                        Log.Information(".NET " + DOT_NET_REQUIRED_VERSION_HR + " web installer has begun. We will now close ALOT Installer while it runs.");
                        await this.ShowMessageAsync("Wait for installation to finish", "Once installation has finished, you may need to restart your system. If prompted to do so, restart your system to continue using ALOT Installer.");
                    }
                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    Log.Error("Error running .NET update.");
                    Log.Error(App.FlattenException(ex));
                    await this.ShowMessageAsync("Error running installer", "Error running .NET installer: " + e.Error.Message + ". ALOT Installer will now close.");
                    Environment.Exit(1);
                }
            };
            string net471webinstallerlink = DOT_NET_DOWNLOAD_LINK_WEB;
            downloadClient.DownloadFileAsync(new Uri(net471webinstallerlink), downloadPath, new KeyValuePair<ProgressDialogController, string>(updateprogresscontroller, downloadPath));
        }

        private async void RunMEMUpdaterGUI()
        {
            Debug.WriteLine("Updating MEM GUI...");
            int fileVersion = 0;
            if (File.Exists(BINARY_DIRECTORY + "MassEffectModder.exe"))
            {
                var versInfo = FileVersionInfo.GetVersionInfo(BINARY_DIRECTORY + "MassEffectModder.exe");
                fileVersion = versInfo.FileMajorPart;
                Button_MEM_GUI.Text = "MEM v" + fileVersion;
            }

            if (Directory.Exists(UPDATE_STAGING_MEM_DIR))
            {
                try
                {
                    Utilities.DeleteFilesAndFoldersRecursively(UPDATE_STAGING_MEM_DIR);
                }
                catch (Exception ex)
                {
                    Log.Error("Could not delete " + UPDATE_STAGING_MEM_DIR + ". We will try again later. Exception message: " + ex.Message);
                    return;
                }
            }

            try
            {
                var client = new GitHubClient(new ProductHeaderValue("ALOTInstaller"));
                var user = await client.Repository.Release.GetAll("MassEffectModder", "MassEffectModder");
                if (user.Count > 0)
                {
                    //The release we want to check is always the latest, so [0]
                    Release latest = user[0];
                    int releaseNameInt = Convert.ToInt32(latest.TagName);
                    if (fileVersion < releaseNameInt && latest.Assets.Count > 0)
                    {
                        ReleaseAsset asset = null;
                        foreach (ReleaseAsset a in latest.Assets)
                        {
                            if (a.Name.StartsWith("MassEffectModder-v"))
                            {
                                asset = a;
                            }
                        }
                        if (asset != null)
                        {
                            //there's an update
                            //updateprogresscontroller = await this.ShowProgressAsync("Installing Update", "Mass Effect Modder is updating. Please wait...", true);
                            //updateprogresscontroller.SetIndeterminate();
                            WebClient downloadClient = new WebClient();

                            downloadClient.Headers["Accept"] = "application/vnd.github.v3+json";
                            downloadClient.Headers["user-agent"] = "ALOTInstaller";
                            string temppath = Path.GetTempPath();
                            /*downloadClient.DownloadProgressChanged += (s, e) =>
                            {
                                updateprogresscontroller.SetProgress((double)e.ProgressPercentage / 100);
                            };*/
                            downloadClient.DownloadFileCompleted += UnzipMEMGUIUpdate;
                            string downloadPath = temppath + "MEMGUI_Update" + Path.GetExtension(asset.BrowserDownloadUrl);
                            downloadClient.DownloadFileAsync(new Uri(asset.BrowserDownloadUrl), downloadPath, downloadPath);
                        }
                    }
                    else
                    {
                        //up to date
                        RunMusicDownloadCheck();
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error checking for MEM GUI update: " + e.Message);
                ShowStatus("Error checking for MEM update");
            }
        }

        private void RunMusicDownloadCheck()
        {
            if (musicpackmirrors.Count() == 0)
            {
                return;
            }
            string me1ogg = GetMusicDirectory() + "me1.ogg";
            string me2ogg = GetMusicDirectory() + "me2.ogg";
            string me3ogg = GetMusicDirectory() + "me3.ogg";

            if (!File.Exists(me1ogg) || !File.Exists(me2ogg) || !File.Exists(me3ogg))
            {
                WebClient downloadClient = new WebClient();

                downloadClient.Headers["user-agent"] = "ALOTInstaller";
                string temppath = Path.GetTempPath();
                downloadClient.DownloadFileCompleted += UnzipMusicUpdate;
                string downloadPath = temppath + "ALOTInstallerMusicPack.7z";
                string mirror = musicpackmirrors[0];
                Log.Information("Downloading music pack from " + mirror);
                try
                {
                    downloadClient.DownloadFileAsync(new Uri(mirror), downloadPath, downloadPath);
                }
                catch (Exception e)
                {
                    Log.Error("Exception downloading music file: " + e.ToString());
                }
            }
        }

        private void UnzipMusicUpdate(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                //Extract 7z
                string path = BINARY_DIRECTORY + "7z.exe";

                string args = "x \"" + (string)e.UserState + "\" -aoa -r -o\"" + GetMusicDirectory() + "\"";
                Log.Information("Extracting Music Pack...");
                Utilities.runProcess(path, args);
                Log.Information("Extraction complete.");

                File.Delete((string)e.UserState);
                ShowStatus("Downloaded music pack", 2000);
            }
            else
            {
                Log.Error("Error occured extracting music pack download: " + e.Error.ToString());
            }
        }

        private async void RunMEMUpdater2()
        {
            int fileVersion = 0;
            if (File.Exists(BINARY_DIRECTORY + "MassEffectModderNoGui.exe"))
            {
                var versInfo = FileVersionInfo.GetVersionInfo(BINARY_DIRECTORY + "MassEffectModderNoGui.exe");
                fileVersion = versInfo.FileMajorPart;
            }

            Label_MEMVersion.Content = "MEM Cmd Version: " + fileVersion;
            try
            {
                Log.Information("Checking for updates to MEMNOGUI. The local version is " + fileVersion);
                if (USING_BETA)
                {
                    Log.Information("We will include prerelease builds as we are in beta mode.");
                }
                var client = new GitHubClient(new ProductHeaderValue("ALOTInstaller"));
                var releases = await client.Repository.Release.GetAll("MassEffectModder", "MassEffectModderNoGui");
                Log.Information("Fetched MEMNOGui releases from github...");
                Release latest = null;
                if (releases.Count > 0)
                {
                    //The release we want to check is always the latest, so [0]
                    Release latestReleaseWithAsset = null;
                    foreach (Release r in releases)
                    {
                        if (!USING_BETA && r.Prerelease)
                        {
                            continue;
                        }
                        if (r.Assets.Count == 0)
                        {
                            continue; //latest release has no assets
                        }
                        if (latestReleaseWithAsset != null)
                        {
                            latestReleaseWithAsset = r;
                        }
                        int releaseNameInt = Convert.ToInt32(r.TagName);
                        if (releaseNameInt > fileVersion)
                        {
                            if (USING_BETA)
                            {
                                latest = r;
                                break;
                            }
                            if (releaseNameInt == SOAK_APPROVED_STABLE_MEMNOGUIVERSION)
                            {
                                var comparisonAge = SOAK_START_DATE == null ? DateTime.Now - r.PublishedAt.Value : DateTime.Now - SOAK_START_DATE;
                                var age = DateTime.Now - SOAK_START_DATE;
                                int soakTestReleaseAge = (comparisonAge).Days;
                                if (soakTestReleaseAge > SoakThresholds.Length - 1)
                                {
                                    Log.Information("New MEMNOGUI update is past soak period, accepting this release as an update");
                                    latest = r;
                                    break;
                                }
                                int threshhold = SoakThresholds[soakTestReleaseAge];
                                ReleaseAsset asset = null;
                                foreach (ReleaseAsset a in r.Assets)
                                {
                                    if (a.Name.StartsWith("MassEffectModderNoGui-v"))
                                    {
                                        asset = a;
                                    }
                                }

                                if (asset == null)
                                {
                                    Log.Information("New MEMNOGUI update doesn't have any Windows builds.");
                                    continue;
                                }

                                if (asset.DownloadCount > threshhold)
                                {
                                    Log.Information("New MEMNOGUI update is soak testing and has reached the daily soak threshhold of " + threshhold + ". This update is not applicable to us today, threshhold will expand tomorrow.");
                                    continue;
                                }
                                else
                                {
                                    Log.Information("New MEMNOGUI update is available and soaking, this client will participate in this soak test.");
                                    latest = r;
                                    break;
                                }
                            }
                            if (!USING_BETA && releaseNameInt > HIGHEST_APPROVED_STABLE_MEMNOGUIVERSION)
                            {
                                Log.Information("New MEMNOGUI update is available but is not yet approved for stable channel: " + releaseNameInt);
                                continue;
                            }
                            latest = r;
                            break;
                        }
                        else
                        {
                            Log.Information("Latest release that is available and has been approved for ALOT Installer is v" + releaseNameInt + " - no update available for us");
                            break;
                        }
                    }

                    //No local version, no latest, but we have asset available somehwere
                    if (fileVersion == 0 && latest == null && latestReleaseWithAsset != null)
                    {
                        Log.Information("MEM No Gui does not exist locally, and no applicable version can be found, pulling latest from github");
                        latest = latestReleaseWithAsset;
                    }
                    else if (fileVersion == 0 && latestReleaseWithAsset == null)
                    {
                        //No local version, and we have no server version
                        Log.Error("Cannot pull a copy of MassEffectModderNoGui from server, could not find one with assets. ALOT Installer will have severely limited functionality.");
                    }
                    else if (fileVersion == 0)
                    {
                        Log.Information("MEM No Gui does not exist locally. Pulling a copy from Github.");
                    }

                    if (latest != null)
                    {
                        ReleaseAsset asset = null;
                        foreach (ReleaseAsset a in latest.Assets)
                        {
                            if (a.Name.StartsWith("MassEffectModderNoGui-v"))
                            {
                                asset = a;
                            }
                        }
                        if (asset != null)
                        {
                            Log.Information("MEMNOGUI update available: " + latest.TagName);
                            //there's an update
                            updateprogresscontroller = await this.ShowProgressAsync("Installing Update", "Mass Effect Modder (Cmd Version) is updating (to v" + latest.TagName + "). Please wait...", true);
                            updateprogresscontroller.SetIndeterminate();
                            updateprogresscontroller.Canceled += MEMNoGuiUpdateCanceled;
                            downloadClient = new WebClient();
                            downloadClient.Headers["Accept"] = "application/vnd.github.v3+json";
                            downloadClient.Headers["user-agent"] = "ALOTInstaller";
                            string temppath = Path.GetTempPath();
                            downloadClient.DownloadProgressChanged += (s, e) =>
                            {
                                updateprogresscontroller.SetProgress((double)e.ProgressPercentage / 100);
                            };
                            downloadClient.DownloadFileCompleted += UnzipProgramUpdate;
                            string downloadPath = temppath + "MEM_Update" + Path.GetExtension(asset.BrowserDownloadUrl);
                            downloadClient.DownloadFileAsync(new Uri(asset.BrowserDownloadUrl), downloadPath, new KeyValuePair<ProgressDialogController, string>(updateprogresscontroller, downloadPath));
                        }
                        else
                        {
                            Log.Information("New MEMNOGUI update doesn't have any Windows builds.");
                        }
                    }
                    else
                    {
                        //up to date
                        Log.Information("No updates for MEM NO Gui are available");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error checking for MEMNOGUI update: " + e.Message);
                ShowStatus("Error checking for MEM (NOGUI) update");
            }
        }

        private async void PerformPostStartup()
        {
            EnsureOneGameIsInstalled();
            PerformRAMCheck();
            await PerformWriteCheck(false);
            PerformUACCheck();
            UpdateALOTStatus();
            RunMEMUpdaterGUI();
            //string appCrashFile = EXE_DIRECTORY + @"Data\APP_CRASH";
            //string appCrashHandledFile = EXE_DIRECTORY + @"Data\APP_CRASH_HANDLED";
            bool didAppCrash = await Crashes.HasCrashedInLastSessionAsync();
            ErrorReport crashReport = await Crashes.GetLastSessionCrashReportAsync();
            if (didAppCrash)
            {
                //DateTime crashTime = File.GetCreationTime(appCrashFile);
                //bool hasBeenHandled = false;
                //try
                //{
                //    File.Delete(appCrashFile);
                //    Log.Warning("Removed APP_CRASH");
                //    if (File.Exists(appCrashHandledFile))
                //    {
                //        hasBeenHandled = true;
                //        Log.Warning("Removed APP_CRASH_HANDLED - the previous crash has already been handled");
                //        File.Delete(appCrashHandledFile);
                //    }
                //}
                //catch (Exception e)
                //{
                //    Log.Error("Cannot remove APP_CRASH:" + e.Message);
                //    if (!File.Exists(appCrashHandledFile))
                //    {
                //        File.Create(appCrashHandledFile);
                //    }
                //}
                if (crashReport.AppErrorTime.LocalDateTime.Date == DateTime.Today)
                {
                    var date = crashReport.AppErrorTime.LocalDateTime.Date;

                    Log.Information("Crash date: " + crashReport.AppErrorTime.Date.Date + ", today is " + DateTime.Today + ", crash not handled. Prompting to upload");
                    MetroDialogSettings mds = new MetroDialogSettings();
                    mds.AffirmativeButtonText = "Upload";
                    mds.NegativeButtonText = "No";
                    mds.DefaultButtonFocus = MessageDialogResult.Affirmative;
                    var upload = await this.ShowMessageAsync("Previous installer session crashed", "The previous installer session crashed. Would you like to upload the log to help the developers fix it?", MessageDialogStyle.AffirmativeAndNegative, mds);
                    if (upload == MessageDialogResult.Affirmative)
                    {
                        await uploadLatestLog(true, null);
                        ShowStatus("Crash log uploaded");
                        mds = new MetroDialogSettings();
                        mds.AffirmativeButtonText = "Join Discord";
                        mds.NegativeButtonText = "Decline";
                        mds.DefaultButtonFocus = MessageDialogResult.Affirmative;
                        var result = await this.ShowMessageAsync("Want to help fix this issue?", "We would appreciate if you joined the ALOT Discord server so you can help us reproduce this issue so we can get it fixed.", MessageDialogStyle.AffirmativeAndNegative, mds);
                        if (result == MessageDialogResult.Affirmative)
                        {
                            openWebPage(DISCORD_INVITE_LINK);
                        }
                    }

                }
            }
            /*  if (MEUITM_INSTALLER_MODE)
              {
                  MEUITM_Flyout_BootPanel.Visibility = Visibility.Collapsed;
                  MEUITM_Flyout_InstallOptionsPanel.Visibility = Visibility.Visible;
              } */
            Log.Information("PerformPostStartup() has completed. We are now switching over to user control.");
            if (App.PreloadedME3Path != null || App.PreloadedME2Path != null || App.PreloadedME1Path != null)
            {
                ShowStatus("Using game paths from Mod Manager");
            }
        }

        private void MEMNoGuiUpdateCanceled(object sender, EventArgs e)
        {
            Log.Warning("MEM NO GUI Update has been canceled.");
            if (downloadClient != null && downloadClient.IsBusy)
            {
                downloadClient.CancelAsync();
            }
            if (updateprogresscontroller != null && updateprogresscontroller.IsOpen)
            {
                updateprogresscontroller.CloseAsync();
            }
        }

        private async void UnzipSelfUpdate(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                Log.Error("Error during download of ALOTInstaller update:");
                Log.Error(App.FlattenException(e.Error));
                Log.Warning("We will proceed with update extraction attempt anyways, it will likely fail.");
            }

            KeyValuePair<ProgressDialogController, string> kp = (KeyValuePair<ProgressDialogController, string>)e.UserState;
            if (e.Cancelled)
            {
                Log.Warning("SelfUpdate was canceled, deleting partial file...");

                // delete the partially-downloaded file
                if (File.Exists(kp.Value))
                {
                    File.Delete(kp.Value);
                }
                return;
            }
            Log.Information("Applying update to program UnzipSelfUpdate()");
            if (File.Exists(kp.Value))
            {
                kp.Key.SetIndeterminate();
                kp.Key.SetTitle("Extracting ALOT Installer update");
                string path = BINARY_DIRECTORY + "7z.exe";
                string args = "x \"" + kp.Value + "\" -aoa -r -o\"" + EXE_DIRECTORY + "Update\"";
                Log.Information("Extracting update...");
                int result = Utilities.runProcess(path, args);
                if (result == 0)
                {
                    File.Delete((string)kp.Value);
                    await kp.Key.CloseAsync();

                    Log.Information("Update Extracted - rebooting to update mode");
                    string exe = EXE_DIRECTORY + "Update\\" + System.AppDomain.CurrentDomain.FriendlyName;
                    string currentDirNoSlash = EXE_DIRECTORY.Substring(0, EXE_DIRECTORY.Length - 1);
                    args = "--update-dest \"" + currentDirNoSlash + "\"";
                    Utilities.runProcess(exe, args, true);
                    Environment.Exit(0);
                }
                else
                {
                    Log.Error("Failed to extract update, 7zip return code not 0: " + result);
                    await this.ShowMessageAsync("Update failed to extract", "The update failed to extract. There may have been an issue downloading it. ALOT Installer will attempt the update again when the application is restarted.");
                }
            }
            else
            {
                await kp.Key.CloseAsync();
            }
        }

        private async void UnzipProgramUpdate(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                return; //handled by cancel
            }
            if (e.Error != null)
            {
                Log.Error("Error during download of MEMNOGUI update:");
                Log.Error(App.FlattenException(e.Error));
                Log.Warning("We will proceed with update extraction attempt anyways, it will likely fail.");
            }

            KeyValuePair<ProgressDialogController, string> kp = (KeyValuePair<ProgressDialogController, string>)e.UserState;
            kp.Key.SetIndeterminate();
            kp.Key.SetTitle("Extracting MassEffectModderNoGUI Update");
            //Extract 7z
            string path = BINARY_DIRECTORY + "7z.exe";
            string args = "x \"" + kp.Value + "\" -aoa -r -o\"" + EXE_DIRECTORY + "\\" + UPDATE_STAGING_MEMNOGUI_DIR + "\"";

            Log.Information("Extracting MassEffectModderNoGUI update to staging...");
            int extractcode = Utilities.runProcess(path, args);
            if (extractcode == 0)
            {
                //We're OK
                Log.Information("Extraction complete with code " + extractcode);
                Log.Information("Applying staged update for MEMNOGUI");
                try
                {
                    CopyDir.CopyAll(new DirectoryInfo(UPDATE_STAGING_MEMNOGUI_DIR), new DirectoryInfo(BINARY_DIRECTORY));
                    Log.Information("Update completed");
                }
                catch (Exception exception)
                {
                    Log.Error("Error extracting MEMNOGUI update:");
                    Log.Error(App.FlattenException(exception));
                    await this.ShowMessageAsync("MassEffectModderNoGui update failed", "MassEffectModderNoGui update failed to apply. This program is used to install textures and other operations. The update will be attempted again when the program is restarted.\nThe error was: " + exception.Message);
                }
            }
            else
            {
                //RIP
                Log.Error("MEMNoGui update extraction failed with code " + extractcode);
                await this.ShowMessageAsync("MassEffectModderNoGui update failed", "MassEffectModderNoGui update failed. This program is used to install textures and other operations. The update will be attempted when the program is restarted.");
            }
            if (Directory.Exists(EXE_DIRECTORY + "\\" + UPDATE_STAGING_MEMNOGUI_DIR))
            {
                Utilities.DeleteFilesAndFoldersRecursively(EXE_DIRECTORY + "\\" + UPDATE_STAGING_MEMNOGUI_DIR);
            }
            File.Delete((string)kp.Value);
            await kp.Key.CloseAsync();
            if (File.Exists(BINARY_DIRECTORY + MEM_EXE_NAME))
            {
                var versInfo = FileVersionInfo.GetVersionInfo(BINARY_DIRECTORY + MEM_EXE_NAME);
                int fileVersion = versInfo.FileMajorPart;
                Label_MEMVersion.Content = "MEM Cmd Version: " + fileVersion;
            }
            //PerformPostStartup();
        }

        private void UnzipMEMGUIUpdate(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                Log.Error("Error during download of MEMGUI update:");
                Log.Error(App.FlattenException(e.Error));
                Log.Warning("We will proceed with update extraction attempt anyways, it will likely fail.");
            }
            //Extract 7z
            string path = BINARY_DIRECTORY + "7z.exe";
            //  string pathWithoutTrailingSlash = BINARY_DIRECTORY.Substring(0, BINARY_DIRECTORY.Length - 1);
            string args = "x \"" + e.UserState + "\" -aoa -r -o\"" + UPDATE_STAGING_MEM_DIR + "\"";
            Log.Information("Extracting MEMGUI update...");
            int extractcode = Utilities.runProcess(path, args);
            if (extractcode == 0)
            {
                //We're OK
                Log.Information("Extraction complete with code " + extractcode);
                Log.Information("Applying staged update for MEM GUI");
                try
                {
                    CopyDir.CopyAll(new DirectoryInfo(UPDATE_STAGING_MEM_DIR), new DirectoryInfo(BINARY_DIRECTORY));
                    Log.Information("Update completed");
                }
                catch (Exception ex)
                {
                    Log.Error("MEMGUI update could not be applied: " + ex.Message);
                    //await this.ShowMessageAsync("MassEffectModderNoGui update failed", "MassEffectModderNoGui update failed. This program is used to install textures and other operations. The update will be attempted when the program is restarted.");
                    ShowStatus("Failed to apply MEM GUI update - we will try again next application boot");
                }
            }
            else
            {
                //RIP
                Log.Error("MEM GUI update extraction failed with code " + extractcode);
                ShowStatus("Error updating MEM", 4000);
                // await this.ShowMessageAsync("MassEffectModderNoGui update failed", "MassEffectModderNoGui update failed. This program is used to install textures and other operations. The update will be attempted when the program is restarted.");
            }

            if (Directory.Exists(UPDATE_STAGING_MEM_DIR))
            {
                try
                {
                    Utilities.DeleteFilesAndFoldersRecursively(UPDATE_STAGING_MEM_DIR);
                    File.Delete((string)e.UserState);
                }
                catch (Exception ex)
                {
                    Log.Error("Could not delete " + UPDATE_STAGING_MEM_DIR + " or " + (string)e.UserState + ". We will try again later. Exception message: " + ex.Message);
                }
            }

            if (File.Exists(BINARY_DIRECTORY + "MassEffectModder.exe"))
            {
                var versInfo = FileVersionInfo.GetVersionInfo(BINARY_DIRECTORY + "MassEffectModder.exe");
                int fileVersion = versInfo.FileMajorPart;
                Button_MEM_GUI.Text = "MEM v" + fileVersion;
                ShowStatus("Updated Mass Effect Modder (GUI version) to v" + fileVersion, 3000);
            }
            RunMusicDownloadCheck();
        }

        private async void BuildCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ShowReadyFilesOnly = false;
            TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress, this);
            int result = (int)e.Result;
            Log.Information("BuildCompleted() with result " + result);
            PreventFileRefresh = false;
            SetInstallButtonsAvailability();
            Button_Settings.IsEnabled = true;
            Button_DownloadAssistant.IsEnabled = true;
            Build_ProgressBar.IsIndeterminate = false;

            switch (result)
            {
                case -2:
                    Log.Warning("BuildCompleted result: Blocked due to incompatible mods installed");
                    HeaderLabel.Text = "Installation blocked due to incompatible mods detected in Mass Effect" + GetGameNumberSuffix(CURRENT_GAME_BUILD) + ".\nRestore your game to a compatible state and do not install these mods.";
                    AddonFilesLabel.Text = "Installation aborted";
                    string badModsStr = "";
                    foreach (string str in BlockingMods)
                    {
                        badModsStr += "\n - " + str;
                    }
                    string prefix = "The following mods appear to be installed and are";
                    if (BlockingMods.Count == 1)
                    {
                        prefix = "The following mod appears to be installed and is";
                    }
                    await this.ShowMessageAsync("Incompatible mods detected", prefix + " known to be incompatible with ALOT for Mass Effect" + GetGameNumberSuffix(CURRENT_GAME_BUILD) + ". Restore your game to an unmodified state, and then install compatible versions of these mods if one is available. If one is not available, you cannot install the mod and use ALOT with it." + badModsStr);
                    PreventFileRefresh = false;
                    break;
                case -1:
                default:
                    Log.Error("BuildCompleted() got -1 (or default catch all) result.");
                    HeaderLabel.Text = "An error occured while building and staging textures for installation.\nView the log (Settings -> Diagnostics -> View Installer Log) for more information.";
                    AddonFilesLabel.Text = "Staging aborted";
                    await this.ShowMessageAsync("Error occured while building installation package", "An error occured while building the installation package. You can view the installer log file for more information in the settings menu. You should report this issue to the developers on Discord in the #bugs channel (Settings -> Report an issue).");
                    RefreshesUntilRealRefresh = 4;
                    break;
                case 1:
                case 2:
                case 3:
                    if (errorOccured)
                    {
                        Log.Warning("Error while building and staging, see previous entries in log.");
                        HeaderLabel.Text = "Addon built with errors.\nThe Addon was built but some files did not process correctly and were skipped.\nThe MEM packages for the addon have been placed into the " + MEM_OUTPUT_DISPLAY_DIR + " directory.";
                        AddonFilesLabel.Text = "MEM Packages placed in the " + MEM_OUTPUT_DISPLAY_DIR + " folder";
                        await this.ShowMessageAsync("Textures staged for Mass Effect" + GetGameNumberSuffix(result) + " with errors", "Some files had errors occur during the build and staging process. These files were skipped. Your game may look strange in some parts if you were to install these textures. You should report this to the developers on Discord (Settings -> Report an issue).");
                    }
                    else
                    {

                        //flash
                        var helper = new FlashWindowHelper(System.Windows.Application.Current);
                        // Flashes the window and taskbar 5 times and stays solid 
                        // colored until user focuses the main window
                        helper.FlashApplicationWindow();

                        //Is Alot Installed?
                        ALOTVersionInfo currentAlotInfo = GetCurrentALOTInfo(CURRENT_GAME_BUILD);
                        bool readyToInstallALOT = false;
                        foreach (AddonFile af in ADDONFILES_TO_BUILD)
                        {
                            if (af.ALOTVersion > 0 || af.MEUITM)
                            {
                                readyToInstallALOT = true;
                            }
                        }

                        //Disk requirements
                        long fullsize = Utilities.DirSize(new DirectoryInfo(getOutputDir(CURRENT_GAME_BUILD)));

                        //Memory requirements
                        long memorySpaceRequired = 14 * ByteSize.BytesInGigaByte;
                        long systemMemoryAmount = Utilities.GetInstalledRamAmount();

                        memorySpaceRequired -= systemMemoryAmount;
                        long fulldiskspaceonsamedrive = memorySpaceRequired + fullsize;

                        bool hasEnoughFreeOnPageDisk = memorySpaceRequired <= 0;
                        if (!hasEnoughFreeOnPageDisk)
                        {
                            foreach (string pageFile in PagefileLocations)
                            {
                                bool sameDrive = Path.GetPathRoot(pageFile) == Path.GetPathRoot(Utilities.GetGamePath(CURRENT_GAME_BUILD));
                                ulong xfreeBytes, xdiskSize, xtotalFreeBytes;
                                Utilities.GetDiskFreeSpaceEx(pageFile, out xfreeBytes, out xdiskSize, out xtotalFreeBytes);
                                if (xfreeBytes > (sameDrive ? (ulong)fulldiskspaceonsamedrive : (ulong)fullsize))
                                {
                                    fullsize = fulldiskspaceonsamedrive;
                                    hasEnoughFreeOnPageDisk = true;
                                    Log.Information("We will need around " + ByteSize.FromBytes(fullsize) + " to install this texture set. The free space is " + ByteSize.FromBytes(xfreeBytes));
                                    break;
                                }

                            }
                        }
                        else
                        {
                            Log.Information("System should have enough RAM + Pagefile space for memory requirements");
                        }

                        Utilities.GetDiskFreeSpaceEx(Utilities.GetGamePath(CURRENT_GAME_BUILD), out ulong freeBytes, out ulong diskSize, out ulong totalFreeBytes);

                        if (freeBytes < (ulong)fullsize)
                        {
                            //not enough disk space for build
                            Log.Error("There is not enough disk space on " + Path.GetPathRoot(Utilities.GetGamePath(CURRENT_GAME_BUILD)) + " to install. You will need " + ByteSize.FromBytes(fullsize) + " of free space on " + Path.GetPathRoot(Utilities.GetGamePath(CURRENT_GAME_BUILD)) + ":\\ to install.");
                            HeaderLabel.Text = "Not enough free space to install textures for Mass Effect" + GetGameNumberSuffix(CURRENT_GAME_BUILD) + ".";
                            AddonFilesLabel.Text = "MEM Packages placed in the " + MEM_OUTPUT_DISPLAY_DIR + " folder";
                            await this.ShowMessageAsync("Not enough free space for install", "There is not enough disk space on " + Path.GetPathRoot(Utilities.GetGamePath(CURRENT_GAME_BUILD)) + " to install. You will need " + ByteSize.FromBytes(fullsize) + " of free space on " + Path.GetPathRoot(Utilities.GetGamePath(CURRENT_GAME_BUILD)) + ":\\ to install.");
                            errorOccured = false;
                            break;
                        }

                        if (ADDONFILES_TO_BUILD.Count == 0)
                        {
                            //bug found
                            HeaderLabel.Text = "No files selected to install for Mass Effect" + GetGameNumberSuffix(CURRENT_GAME_BUILD) + ".";
                            AddonFilesLabel.Text = "This is a bug. Please report this to the developers on Discord.";
                            await this.ShowMessageAsync("No files selected for installation", "No files were selected for installation. This should not be possible - you have found a bug. Please report this to the developers on Discord.");
                            errorOccured = false;
                            break;
                        }
                        //debug
                        //readyToInstallALOT = true;
                        if (readyToInstallALOT || MEUITM_INSTALLER_MODE || currentAlotInfo != null) //not installed
                        {
                            var ready = false;
                            for (int i = 0; i < 4; i++)
                            {
                                if (!ready)
                                {
                                    ready = await PerformWriteCheck(true);
                                }
                            }

                            if (!ready)
                            {
                                Log.Warning("Cannot determine if game directory is writable, or user is declining PermissionsGranter.exe. Aborting installation.");
                                await this.ShowMessageAsync("Unable to gain write access to game directories", "ALOT Installer was unable to gain write access to all installed game's directories. ALOT Installer will not attempt installation if the privledge checks don't pass. Please allow ALOT Installer permissions through PermissionsGranter.exe's UAC prompt.");
                                errorOccured = false;
                                break;
                            }

                            if (ready)
                            {
                                HeaderLabel.Text = "Ready to install";
                                AddonFilesLabel.Text = "MEM Packages placed in the " + MEM_OUTPUT_DISPLAY_DIR + " folder";
                                MetroDialogSettings mds = new MetroDialogSettings();
                                mds.AffirmativeButtonText = "Install Now";

                                mds.NegativeButtonText = "Cancel Install";
                                mds.DefaultButtonFocus = MessageDialogResult.Affirmative;
                                var buildResult = await this.ShowMessageAsync("Ready to install textures", "Textures have been prepared and are ready to install.\n\nOnce you press install, you won't be able to install any mods or DLC or you will create broken textures in your game.\n\nEnsure you have installed all of your non-texture mods and DLC at this point, as there is no going back once this process has started.\n\nTurn off antivirus real time scanning during installation to avoid issues caused by antivirus programs.", MessageDialogStyle.AffirmativeAndNegative, mds);
                                if (buildResult == MessageDialogResult.Affirmative)
                                {
                                    bool run = true;
                                    while (Utilities.IsGameRunning(CURRENT_GAME_BUILD))
                                    {
                                        run = false;
                                        await this.ShowMessageAsync("Mass Effect" + GetGameNumberSuffix(CURRENT_GAME_BUILD) + " is running", "Please close Mass Effect" + GetGameNumberSuffix(CURRENT_GAME_BUILD) + " to continue.");
                                        if (!Utilities.IsGameRunning(CURRENT_GAME_BUILD))
                                        {
                                            run = true;
                                            break;
                                        }
                                    }
                                    if (run)
                                    {
                                        Log.Information("User has chosen to install textures after build - we are now starting InstallALOT()");
                                        InstallALOT(result, ADDONFILES_TO_BUILD);
                                    }
                                    else
                                    {
                                        Log.Warning("User has declined to install textures after build, or the game is running.");
                                    }
                                }
                                else
                                {
                                    HeaderLabel.Text = MEUITM_INSTALLER_MODE ? MEUITM_PRIMARY_HEADER : PRIMARY_HEADER;
                                }
                            }
                        }
                        else
                        {
                            //we should never hit this condition anymore.
                            await this.ShowMessageAsync("Addon(s) have been built", "Your textures have been built into MEM files, ready for installation. Due to ALOT not being installed, you will have to install these manually. The files have been placed into the MEM_Packages subdirectory.");
                        }
                    }
                    errorOccured = false;
                    break;
            }
            if (ADDONFILES_TO_BUILD != null)
            {
                foreach (AddonFile af in ADDONFILES_TO_BUILD)
                {
                    if (!af.IsInErrorState())
                    {
                        af.SetIdle();
                        af.ReadyStatusText = null;
                    }
                    af.Building = false;
                }
            }
            ShowBuildingOnly = false;
            BUILD_ALOT = false;
            BUILD_ADDON_FILES = false;
            BUILD_USER_FILES = false;
            BUILD_ALOT_UPDATE = false;
            ApplyFiltering();
            CURRENT_GAME_BUILD = 0; //reset
        }

        private ALOTVersionInfo GetCurrentALOTInfo(int game)
        {
            switch (game)
            {
                case 1:
                    return CURRENTLY_INSTALLED_ME1_ALOT_INFO;
                case 2:
                    return CURRENTLY_INSTALLED_ME2_ALOT_INFO;
                case 3:
                    return CURRENTLY_INSTALLED_ME3_ALOT_INFO;
                default:
                    return null; // could be bad.
            }
        }
        private void SetInstallFlyoutState(bool open)
        {
            InstallingOverlayFlyout.IsOpen = open;
            if (open)
            {
                BorderThickness = new Thickness(0, 0, 0, 0);
            }
            else
            {
                BorderThickness = new Thickness(1, 1, 1, 1);

            }
        }

        private async void CheckImportLibrary_Tick(object sender, EventArgs e)
        {
            if (PreventFileRefresh)
            {
                return;
            }
            if (RefreshesUntilRealRefresh > 0)
            {
                RefreshesUntilRealRefresh--;
                return;
            }
            // code to execute periodically
            //Console.WriteLine("Checking for files existence...");
            string basepath = DOWNLOADED_MODS_DIRECTORY + "\\";
            int numdone = 0;

            int numME1Files = 0;
            int numME2Files = 0;
            int numME3Files = 0;
            int numME1FilesReady = 0;
            int numME2FilesReady = 0;
            int numME3FilesReady = 0;
            List<AddonFile> newUnreadyUserFiles = new List<AddonFile>();
            foreach (AddonFile af in DisplayedAddonFiles)
            {
                if (af.Game_ME1) numME1Files++;
                if (af.Game_ME2) numME2Files++;
                if (af.Game_ME3) numME3Files++;
                bool ready = File.Exists(basepath + af.Filename);
                if (ready != af.Ready)
                {
                    Utilities.WriteDebugLog(af.FriendlyName + " ready state is about to change due to detection/missing file: " + basepath + af.Filename);
                }
                if (af.UserFile)
                {
                    ready = File.Exists(af.UserFilePath);
                    af.Staged = false;
                }
                else if (!ready && af.UnpackedSingleFilename != null)
                {
                    //Check for single file
                    ready = File.Exists(basepath + af.UnpackedSingleFilename);
                    af.Staged = false;
                }

                if (!ready && af.UnpackedSingleFilename != null && af.ALOTVersion > 0)
                {
                    int game = 0;
                    if (af.Game_ME1)
                    {
                        game = 1;
                    }
                    else if (af.Game_ME2)
                    {
                        game = 2;
                    }
                    else if (af.Game_ME3)
                    {
                        game = 3;
                    }
                    //Check for staged file
                    string stagedpath = getOutputDir(game) + ALOT_MEMFILE_NUMBER + "_" + af.UnpackedSingleFilename;
                    ready = File.Exists(stagedpath);
                    if (ready)
                    {
                        af.Staged = true;
                    }
                    if (ready != af.Ready)
                    {
                        Utilities.WriteDebugLog(af.FriendlyName + " ready state is about to change due to detection of staged file: " + stagedpath);
                    }
                }

                //Check for torrent filename
                if (!ready && af.TorrentFilename != null/* && af.ALOTVersion > 0*/)
                {
                    var testForTorrentVer = File.Exists(basepath + af.TorrentFilename);
                    if (testForTorrentVer && new FileInfo(basepath + af.TorrentFilename).Length == af.FileSize)
                    {
                        try
                        {
                            //Will retry in 5s
                            Log.Information("Attempting to rename torrent-filename for " + af.FriendlyName + " to Nexus-based filename");
                            File.Move(basepath + af.TorrentFilename, basepath + af.Filename);
                            Log.Information("Renamed torrent-filename of " + af.FriendlyName + " to Nexus-based filename");
                        }
                        catch (Exception ex)
                        {
                            Log.Warning("Error: Unable to move torrent file into correct name in downloaded mods lib: " + ex.ToString());
                        }
                    }
                }

                if (af.Ready != ready) //status is changing
                {
                    Log.Information(af.FriendlyName + " changing ready states. Is now ready: " + ready);
                    af.Ready = ready;
                    af.ReadyStatusText = null;
                    af.ReadyIconPath = null;
                    if (!af.Ready && af.UserFile)
                    {
                        newUnreadyUserFiles.Add(af);
                    }
                }
                if (af.Ready)
                {
                    if (af.Game_ME1) numME1FilesReady++;
                    if (af.Game_ME2) numME2FilesReady++;
                    if (af.Game_ME3) numME3FilesReady++;
                }
                else
                {
                    af.Staged = false;
                }
                numdone += ready && !af.Optional ? 1 : 0;

            }
            int numcurrentfiles = DisplayedAddonFiles.Where(p => !p.Optional).Count();
            if (numcurrentfiles != 0)
            {
                ProgressBarValue = (((double)numdone / numcurrentfiles) * 100);
            }
            else
            {
                ProgressBarValue = Convert.ToDouble(0);
            }
            string tickerText = "";
            tickerText += ShowME1Files ? "ME1: " + numME1FilesReady + "/" + numME1Files + " imported" : "ME1: N/A";
            tickerText += " - ";
            tickerText += ShowME2Files ? "ME2: " + numME2FilesReady + "/" + numME2Files + " imported" : "ME2: N/A";
            tickerText += " - ";
            tickerText += ShowME3Files ? "ME3: " + numME3FilesReady + "/" + numME3Files + " imported" : "ME3: N/A";
            AddonFilesLabel.Text = tickerText;

            if (newUnreadyUserFiles.Count > 0)
            {
                AllAddonFiles.ReplaceAll(AllAddonFiles.Except(newUnreadyUserFiles));
                ApplyFiltering();
                string message = "The following user files are no longer available on disk (they may have been moved or deleted) and have been removed from the list of files available in ALOT Installer. If you wish to use these files you will need to drag and drop them onto the interface again.";
                foreach (AddonFile removeFile in newUnreadyUserFiles)
                {
                    message += "\n - " + removeFile.UserFilePath;
                }
                await this.ShowMessageAsync("Some files no longer available", message);
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            fadeInItems = new FrameworkElement[] { FirstRun_MainContent, FirstRunText_TitleBeta, FirstRunText_BetaSummary };
            buildOptionCheckboxes = new System.Windows.Controls.CheckBox[] { Checkbox_BuildOptionALOT, Checkbox_BuildOptionALOTUpdate, Checkbox_BuildOptionMEUITM, Checkbox_BuildOptionUser, Checkbox_BuildOptionAddon };
            if (EXE_DIRECTORY.Length > 105)
            {
                Log.Fatal("ALOT Installer is nested too deep for Addon to build properly (" + EXE_DIRECTORY.Length + " chars) due to Windows API limitations.");
                await this.ShowMessageAsync("ALOT Installer is too deep in the filesystem", "ALOT Installer can have issues extracting and building textures for installation if the program is nested too deeply in the filesystem. This is an issue with Windows file path limitations. Move the ALOT Installer directory up a few folders on your filesystem. A good place to put ALOT Installer is in Documents.");
                Environment.Exit(1);
            }

            bool hasWriteAccess = await testWriteAccess();
            if (hasWriteAccess) PerformPreUpdateCheck();
        }

        private async void EnsureOneGameIsInstalled()
        {
            string me1Path = Utilities.GetGamePath(1);
            string me2Path = Utilities.GetGamePath(2);
            string me3Path = Utilities.GetGamePath(3);

            //int installedGames = 5;
            me1Installed = (me1Path != null);
            me2Installed = (me2Path != null);
            me3Installed = (me3Path != null);

            Log.Information("ME1 Installed: " + me1Installed + " " + me1Path);
            Log.Information("ME2 Installed: " + me2Installed + " " + me2Path);
            Log.Information("ME3 Installed: " + me3Installed + " " + me3Path);

            if (!me1Installed && !me2Installed && !me3Installed)
            {
                Log.Error("No trilogy games are installed (could not find any using lookups). App won't be able to do anything");
                await this.ShowMessageAsync("None of the Mass Effect Trilogy games are installed", "ALOT Installer requires at least one of the trilogy games to be installed before you can use it.\n\nIf you're using the Steam version of Mass Effect or Mass Effect 2, you must run the game at least once so the game can be detected.");
                Log.Error("Exiting due to no games installed");
                Environment.Exit(1);
            }
            Log.Information("At least one game is installed");
        }

        private void SetupButtons()
        {
            string me1Path = Utilities.GetGamePath(1);
            string me2Path = Utilities.GetGamePath(2);
            string me3Path = Utilities.GetGamePath(3);

            //int installedGames = 5;
            me1Installed = (me1Path != null);
            me2Installed = (me2Path != null);
            me3Installed = (me3Path != null);

            Switch_ME1Filter.IsEnabled = ShowME1Files = me1Installed;// me1Installed;
            Switch_ME2Filter.IsEnabled = ShowME2Files = me2Installed;
            Switch_ME3Filter.IsEnabled = ShowME3Files = me3Installed;

            ValidateGameBackup(1);
            ValidateGameBackup(2);
            ValidateGameBackup(3);

            if (me1Installed || me2Installed || me3Installed)
            {
                if (backgroundticker == null)
                {
                    backgroundticker = new DispatcherTimer();
                    backgroundticker.Tick += new EventHandler(CheckImportLibrary_Tick);
                    backgroundticker.Interval = new TimeSpan(0, 0, 5); // execute every 5s
                    backgroundticker.Start();
                    BuildWorker = new BackgroundWorker();
                    BuildWorker.DoWork += BuildAddon;
                    BuildWorker.ProgressChanged += BuildWorker_ProgressChanged;
                    BuildWorker.RunWorkerCompleted += BuildCompleted;
                    BuildWorker.WorkerReportsProgress = true;
                }
                Button_DownloadAssistant.IsEnabled = true;
                SetInstallButtonsAvailability();
            }
        }

        private void SetInstallButtonsAvailability()
        {
            string me1Path = Utilities.GetGamePath(1);
            string me2Path = Utilities.GetGamePath(2);
            string me3Path = Utilities.GetGamePath(3);

            //int installedGames = 5;
            me1Installed = (me1Path != null);
            me2Installed = (me2Path != null);
            me3Installed = (me3Path != null);
            Switch_ME1Filter.IsEnabled = me1Installed;
            Switch_ME2Filter.IsEnabled = me2Installed;
            Switch_ME3Filter.IsEnabled = me3Installed;

            if (!me1Installed)
            {
                Log.Information("ME1 not installed - disabling ME1 install");
                Button_InstallME1.IsEnabled = false;
                Button_InstallME1.ToolTip = "Mass Effect is not installed. To install textures for ME1 the game must already be installed\nIf you are using steam version of the game, run it once before running ALOT Installer";
                Button_InstallME1.Content = "ME1 Not Installed";
                Button_VerifyGameME1.IsEnabled = false;
                Textblock_VerifyME1.Text = "ME1 NOT INSTALLED";
            }
            else
            {
                Button_InstallME1.IsEnabled = true;
                Button_InstallME1.ToolTip = "Click to build and install textures for Mass Effect";
                Button_InstallME1.Content = "Install for ME1";
                Button_VerifyGameME1.IsEnabled = true;
                Textblock_VerifyME1.Text = "VERIFY VANILLA";
            }

            if (!me2Installed)
            {
                Log.Information("ME2 not installed - disabling ME2 install");
                Button_InstallME2.IsEnabled = false;
                Button_InstallME2.ToolTip = "Mass Effect 2 is not installed. To install textures for ME2 the game must already be installed\nIf you are using steam version of the game, run it once before running ALOT Installer";
                Button_InstallME2.Content = "ME2 Not Installed";
                Button_VerifyGameME2.IsEnabled = false;
                Textblock_VerifyME2.Text = "ME2 NOT INSTALLED";

            }
            else
            {
                Button_InstallME2.IsEnabled = true;
                Button_InstallME2.ToolTip = "Click to build and install textures for Mass Effect 2";
                Button_InstallME2.Content = "Install for ME2";
                Button_VerifyGameME2.IsEnabled = true;
                Textblock_VerifyME2.Text = "VERIFY VANILLA";
            }

            if (!me3Installed)
            {
                Log.Information("ME3 not installed - disabling ME3 install");
                Button_InstallME3.IsEnabled = false;
                Button_InstallME3.ToolTip = "Mass Effect 3 is not installed. To install textures for ME3 the game must already be installed";
                Button_InstallME3.Content = "ME3 Not Installed";
                Textblock_VerifyME3.Text = Textblock_AutoTOCME3.Text = "ME3 NOT INSTALLED";
                Button_VerifyGameME3.IsEnabled = Button_AutoTOCME3.IsEnabled = false;

            }
            else
            {
                Button_InstallME3.IsEnabled = true;
                Button_InstallME3.ToolTip = "Click to build and install textures for Mass Effect 3";
                Button_InstallME3.Content = "Install for ME3";
                Button_VerifyGameME3.IsEnabled = Button_AutoTOCME3.IsEnabled = true;
                Textblock_AutoTOCME3.Text = "AUTOTOC";
                Textblock_VerifyME3.Text = "VERIFY VANILLA";
            }
        }

        private bool ValidateGameBackup(int game)
        {
            switch (game)
            {
                case 1:
                    {
                        string me1path = Utilities.GetGamePath(1, true);
                        string path = Utilities.GetGameBackupPath(1);
                        CheckIfRunningInGameSubDir(me1path);
                        if (path != null && me1path != null)
                        {
                            Button_ME1Backup.Content = "Restore ME1";
                            Button_ME1Backup.ToolTip = "Click to restore game from " + Environment.NewLine + path;
                        }
                        else
                        {
                            if (Directory.Exists(me1path))
                            {
                                Button_ME1Backup.Content = "Backup ME1";
                                Button_ME1Backup.ToolTip = "Click to backup game";
                            }
                            else
                            {
                                Button_ME1Backup.Content = "ME1 NOT INSTALLED";
                                Button_ME1Backup.IsEnabled = false;
                            }
                        }
                        Button_ME1Backup.ToolTip += Environment.NewLine + "Game is installed at " + Environment.NewLine + Utilities.GetGamePath(1, true);
                        return path != null;
                    }
                case 2:
                    {
                        string path = Utilities.GetGameBackupPath(2);
                        string me2path = Utilities.GetGamePath(2, true);
                        CheckIfRunningInGameSubDir(me2path);

                        if (path != null && me2path != null)
                        {
                            Button_ME2Backup.Content = "Restore ME2";
                            Button_ME2Backup.ToolTip = "Click to restore game from " + Environment.NewLine + path;
                        }
                        else
                        {
                            if (Directory.Exists(me2path))
                            {
                                Button_ME2Backup.Content = "Backup ME2";
                                Button_ME2Backup.ToolTip = "Click to backup game";
                            }
                            else
                            {
                                Button_ME2Backup.Content = "ME2 NOT INSTALLED";
                                Button_ME2Backup.IsEnabled = false;
                            }

                        }
                        Button_ME2Backup.ToolTip += Environment.NewLine + "Game is installed at " + Environment.NewLine + Utilities.GetGamePath(2, true);
                        return path != null;
                    }
                case 3:
                    {
                        string me3path = Utilities.GetGamePath(3, true);
                        CheckIfRunningInGameSubDir(me3path);
                        string path = Utilities.GetGameBackupPath(3);
                        if (path != null && me3path != null)
                        {
                            Button_ME3Backup.Content = "Restore ME3";
                            Button_ME3Backup.ToolTip = "Click to restore game from " + Environment.NewLine + path;
                        }
                        else
                        {
                            if (Directory.Exists(me3path))
                            {
                                Button_ME3Backup.Content = "Backup ME3";
                                Button_ME3Backup.ToolTip = "Click to backup game";
                            }
                            else
                            {
                                Button_ME3Backup.Content = "ME3 NOT INSTALLED";
                                Button_ME3Backup.IsEnabled = false;
                            }
                        }
                        Button_ME3Backup.ToolTip += Environment.NewLine + "Game is installed at " + Environment.NewLine + Utilities.GetGamePath(3, true);

                        return path != null;
                    }
                default:
                    return false;
            }
        }

        private async void CheckIfRunningInGameSubDir(string path)
        {
            if (path != null && Utilities.IsSubfolder(path, EXE_DIRECTORY))
            {
                Log.Error("FATAL: Running from subdirectory of a game: " + path + " This is not allowed. App will now exit.");
                await this.ShowMessageAsync("ALOT Installer is in a game directory", "ALOT Installer cannot run from inside a game directory. Move ALOT Installer out of of the game directory and into a folder like Desktop or Documents.");
                Environment.Exit(1);
            }
        }

        private void FetchManifest()
        {
            using (WebClient webClient = new WebClient())
            {
                webClient.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                Log.Information("Fetching latest manifest from github");
                Build_ProgressBar.IsIndeterminate = true;
                AddonFilesLabel.Text = "Downloading latest installer manifest";
                if (!File.Exists("DEV_MODE"))
                {
                    try
                    {
                        //File.Copy(@"C:\Users\mgame\Downloads\Manifest.xml", MANIFEST_LOC);
                        string url = "https://raw.githubusercontent.com/ME3Tweaks/ALOTInstaller/master/manifest.xml";
                        if (USING_BETA)
                        {
                            Log.Information("In BETA mode.");
                            url = "https://raw.githubusercontent.com/ME3Tweaks/ALOTInstaller/master/manifest-beta.xml";
                            Title += " BETA MODE";
                        }
                        webClient.DownloadStringCompleted += async (sender, e) =>
                        {
                            if (e.Error == null)
                            {
                                string pageSourceCode = e.Result;
                                if (Utilities.TestXMLIsValid(pageSourceCode))
                                {
                                    Log.Information("Manifest fetched.");
                                    try
                                    {
                                        File.WriteAllText(MANIFEST_LOC, pageSourceCode);
                                        //Legacy stuff
                                        if (File.Exists(EXE_DIRECTORY + @"manifest-new.xml"))
                                        {
                                            File.Delete(MANIFEST_LOC);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Error("Unable to write and remove old manifest! We're probably headed towards a crash.");
                                        Log.Error(App.FlattenException(ex));
                                        UsingBundledManifest = true;
                                    }
                                    ManifestDownloaded();
                                }
                                else
                                {
                                    Log.Error("Response from server was not valid XML! " + pageSourceCode);
                                    Crashes.TrackError(new Exception("Invalid XML from server manifest!"));
                                    if (File.Exists(MANIFEST_LOC))
                                    {
                                        Log.Information("Reading cached manifest instead.");
                                        ManifestDownloaded();
                                    }
                                    else if (!File.Exists(MANIFEST_LOC) && File.Exists(MANIFEST_BUNDLED_LOC))
                                    {
                                        Log.Information("Reading bundled manifest instead.");
                                        File.Delete(MANIFEST_LOC);
                                        File.Copy(MANIFEST_BUNDLED_LOC, MANIFEST_LOC);
                                        UsingBundledManifest = true;
                                        ManifestDownloaded();
                                    }
                                    else
                                    {
                                        Log.Error("Local manifest also doesn't exist! No manifest is available.");
                                        await this.ShowMessageAsync("No Manifest Available", "An error occured downloading or reading the manifest for ALOT Installer. There is no local bundled version available. Information that is required to build and install ALOT is not available. Check the program logs.");
                                        Environment.Exit(1);
                                    }
                                }
                            }
                            else
                            {
                                Log.Error("Exception occured getting manifest from server: " + e.Error.ToString());
                                if (File.Exists(MANIFEST_LOC))
                                {
                                    Log.Information("Reading cached manifest instead.");
                                    ManifestDownloaded();
                                }
                                else if (!File.Exists(MANIFEST_LOC) && File.Exists(MANIFEST_BUNDLED_LOC))
                                {
                                    Log.Information("Reading bundled manifest instead.");
                                    File.Delete(MANIFEST_LOC);
                                    File.Copy(MANIFEST_BUNDLED_LOC, MANIFEST_LOC);
                                    UsingBundledManifest = true;
                                    ManifestDownloaded();
                                }
                                else
                                {
                                    Log.Fatal("No local manifest exists to use, exiting...");
                                    await this.ShowMessageAsync("No Manifest Available", "An error occured downloading the manifest for ALOT Installer. There is no local bundled version available. Information that is required to build and install ALOT is not available. Check the program logs.");
                                    Environment.Exit(1);
                                }
                            }
                            //do something with results 
                        };
                        webClient.DownloadStringAsync(new Uri(url));
                    }
                    catch (WebException e)
                    {
                        Log.Error("WebException occured getting manifest from server: " + e.ToString());
                        if (!File.Exists(MANIFEST_LOC) && File.Exists(MANIFEST_BUNDLED_LOC))
                        {
                            Log.Information("Reading bundled manifest instead.");
                            File.Delete(MANIFEST_LOC);
                            File.Copy(MANIFEST_BUNDLED_LOC, MANIFEST_LOC);
                            UsingBundledManifest = true;
                            ManifestDownloaded();
                        }
                    }
                    //}
                    //catch (Exception e)
                    //{
                    //    Debug.WriteLine(DateTime.Now);
                    //    Log.Error("Other Exception occured getting manifest from server/reading manifest: " + e.ToString());
                    //    if (!File.Exists(MANIFEST_LOC) && File.Exists(MANIFEST_BUNDLED_LOC))
                    //    {
                    //        Log.Information("Reading bundled manifest instead.");
                    //        File.Delete(MANIFEST_LOC);
                    //        File.Copy(MANIFEST_BUNDLED_LOC, MANIFEST_LOC);
                    //        UsingBundledManifest = true;
                    //    }
                    //}
                }
                else
                {
                    Log.Information("DEV_MODE file found. Not using online manifest.");
                    UsingBundledManifest = true;
                    Title += " DEV MODE";
                    ManifestDownloaded();
                }

                //if (!File.Exists(MANIFEST_LOC))
                //{
                //    Log.Fatal("No local manifest exists to use, exiting...");
                //    await this.ShowMessageAsync("No Manifest Available", "An error occured downloading the manifest for addon. Information that is required to build the addon is not available. Check the program logs.");
                //    Environment.Exit(1);
                //}

            }
        }

        private void ManifestDownloaded()
        {
            Button_Settings.IsEnabled = true;
            readManifest();

            Log.Information("readManifest() has completed.");
            CheckOutputDirectoriesForUnpackedSingleFiles();

            Loading = false;
            Build_ProgressBar.IsIndeterminate = false;
            HeaderLabel.Text = PRIMARY_HEADER;
            AddonFilesLabel.Text = "Scanning...";
            PreventFileRefresh = false;
            CheckImportLibrary_Tick(null, null);

            //Check if this is MEUITM installer mode
            var readyfiles = AllAddonFiles.Where(x => x.Ready && !x.UserFile).ToList();
            if (readyfiles.Count == 1 && readyfiles[0].MEUITM)
            {
                //Only MEUITM file was found
                MEUITM_INSTALLER_MODE = true;
            }
            SetUIMode();

            bool? hasShownFirstRun = Utilities.GetRegistrySettingBool("HasRunFirstRun");
            if (hasShownFirstRun == null || !(bool)hasShownFirstRun)
            {
                Log.Information("Showing first run flyout");
                playFirstTimeAnimation();
            }
            else
            {
                PerformPostStartup();
            }
        }

        private void CheckOutputDirectoriesForUnpackedSingleFiles(int game = 0)
        {
            if (Path.GetPathRoot(DOWNLOADED_MODS_DIRECTORY) == Path.GetPathRoot(getOutputDir(1)))
            {
                bool ReImportedFiles = false;
                foreach (AddonFile af in AllAddonFiles)
                {
                    if (af.Ready && !af.Staged)
                    {
                        continue;
                    }

                    //File is not ready. Might be missing single file...
                    if (af.UnpackedSingleFilename != null)
                    {
                        int i = 0;
                        if (af.Game_ME1) i = 1;
                        if (af.Game_ME2) i = 2;
                        if (af.Game_ME3) i = 3;
                        string outputPath = getOutputDir(i);
                        string importedFilePath = DOWNLOADED_MODS_DIRECTORY + "\\" + af.UnpackedSingleFilename;
                        string outputFilename = outputPath + ALOT_MEMFILE_NUMBER + "_" + af.UnpackedSingleFilename; //This only will work for ALOT right now. May expand if it becomes more useful.
                        if (File.Exists(outputFilename) && (game == 0 || game == i))
                        {

                            Log.Information("Re-importing extracted single file: " + outputFilename);
                            try
                            {
                                File.Delete(importedFilePath);
                                File.Move(outputFilename, importedFilePath);
                                ReImportedFiles = true;
                                af.Staged = false;
                                af.ReadyStatusText = null;
                            }
                            catch (Exception e)
                            {
                                Log.Error("Failed to reimport file! " + e.Message);
                            }
                        }
                    }
                }
                Utilities.WriteRegistryKey(Registry.CurrentUser, REGISTRY_KEY, "CheckOutputDirectoriesOnManifestLoad", false);

                if (ReImportedFiles)
                {
                    ShowStatus("Re-imported files due to shutdown during build or install", 3000);
                }
            }
        }

        private async void PerformRAMCheck()
        {
            long ramAmountKb = Utilities.GetInstalledRamAmount();
            long installedRamGB = ramAmountKb / 1048576L;
            if (installedRamGB < 7.98)
            {
                await this.ShowMessageAsync("System memory is less than 8 GB", "Building and installing textures uses considerable amounts of memory. Installation will be significantly slower on systems with less than 8 GB of memory. Systems with more than 8GB of memory will see significant speed improvements during installation.");
            }
            //Check pagefile
            try
            {
                //Current
                using (var query = new ManagementObjectSearcher("SELECT Caption,AllocatedBaseSize FROM Win32_PageFileUsage"))
                {
                    foreach (ManagementBaseObject obj in query.Get())
                    {
                        string pagefileName = (string)obj.GetPropertyValue("Caption");
                        Log.Information("Detected pagefile: " + pagefileName);
                        PagefileLocations.Add(pagefileName.ToLower());
                    }
                }

                //Max
                using (var query = new ManagementObjectSearcher("SELECT Name,MaximumSize FROM Win32_PageFileSetting"))
                {
                    foreach (ManagementBaseObject obj in query.Get())
                    {
                        string pagefileName = (string)obj.GetPropertyValue("Name");
                        uint max = (uint)obj.GetPropertyValue("MaximumSize");
                        if (max > 0)
                        {
                            // Not system managed
                            PagefileLocations.RemoveAll(x => Path.GetFullPath(x).Equals(Path.GetFullPath(pagefileName)));
                            Log.Warning("Pagefile has been modified by the end user. The maximum page file size on " + pagefileName + " is: " + max + "MB");
                        }
                    }
                }

                if (PagefileLocations.Count() > 0)
                {
                    Log.Information("We have a usable page file - OK");
                }
                else
                {
                    Log.Error("We have no uncapped or available pagefiles to use! Very high chance application will run out of memory");
                    await this.ShowMessageAsync("Pagefile is off or size has been capped", "The system pagefile (virtual memory) settings are not currently managed by Windows, or the pagefile is off. ALOT Installer uses large amounts of memory and will very often run out of memory if virtual memory is capped or turned off.");
                }
            }
            catch (Exception e)
            {
                Log.Error("Unable to check pagefile settings:");
                Log.Error(App.FlattenException(e));
            }

            //Debug.WriteLine("Ram Amount, KB: " + ramAmountKb);
        }

        private async Task<bool> PerformWriteCheck(bool required)
        {
            Log.Information("Performing Write Check...");
            string me1Path = Utilities.GetGamePath(1);
            string me2Path = Utilities.GetGamePath(2);
            string me3Path = Utilities.GetGamePath(3);
            bool isAdmin = Utilities.IsAdministrator();
            //int installedGames = 5;
            me1Installed = (me1Path != null && Directory.Exists(me1Path));
            me2Installed = (me2Path != null && Directory.Exists(me2Path));
            me3Installed = (me3Path != null && Directory.Exists(me3Path));
            Utilities.RemoveRunAsAdminXPSP3FromME1();

            bool me1AGEIAKeyNotWritable = false;
            string args = "";
            List<string> directories = new List<string>();
            try
            {
                if (me1Installed)
                {
                    string me1SubPath = Path.Combine(me1Path, @"BioGame\CookedPC\Packages");
                    bool me1Writable = Utilities.IsDirectoryWritable(me1Path) && Utilities.IsDirectoryWritable(me1SubPath);
                    if (!me1Writable)
                    {
                        Log.Information("ME1 not writable: " + me1Path);
                        directories.Add(me1Path);
                    }
                    try
                    {
                        var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\AGEIA Technologies", true);
                        if (key != null)
                        {
                            key.Close();
                        }
                        else
                        {
                            Log.Information("ME1 AGEIA Technologies key is not present or is not writable.");
                            me1AGEIAKeyNotWritable = true;
                        }
                    }
                    catch (SecurityException)
                    {
                        Log.Information("ME1 AGEIA Technologies key is not writable.");
                        me1AGEIAKeyNotWritable = true;
                    }
                }

                if (me2Installed)
                {
                    string me2SubPath = Path.Combine(me2Path, @"Binaries");
                    bool me2Writable = Utilities.IsDirectoryWritable(me2Path) && Utilities.IsDirectoryWritable(me2SubPath);
                    if (!me2Writable)
                    {

                        Log.Information("ME2 not writable: " + me2Path);
                        directories.Add(me2Path);

                    }
                }

                if (me3Installed)
                {
                    string me3SubPath = Path.Combine(me3Path, @"Binaries");
                    bool me3Writable = Utilities.IsDirectoryWritable(me3Path) && Utilities.IsDirectoryWritable(me3SubPath);
                    if (!me3Writable)
                    {

                        Log.Information("ME3 not writable: " + me3Path);
                        directories.Add(me3Path);
                    }
                }

                if (directories.Count() > 0 || me1AGEIAKeyNotWritable)
                {
                    foreach (String str in directories)
                    {
                        if (args != "")
                        {
                            args += " ";
                        }
                        args += "\"" + str + "\"";
                    }

                    if (me1AGEIAKeyNotWritable)
                    {
                        args += " -create-hklm-reg-key \"SOFTWARE\\WOW6432Node\\AGEIA Technologies\"";
                    }
                    args = "\"" + System.Security.Principal.WindowsIdentity.GetCurrent().Name + "\" " + args;
                    //need to run write permissions program
                    if (isAdmin)
                    {
                        string exe = BINARY_DIRECTORY + "PermissionsGranter.exe";
                        int result = Utilities.runProcess(exe, args);
                        if (result == 0)
                        {
                            Log.Information("Elevated process returned code 0, directories are hopefully writable now.");
                            return true;
                        }
                        else
                        {
                            Log.Error("Elevated process returned code " + result + ", directories probably aren't writable.");
                            return false;
                        }
                    }
                    else
                    {
                        string message = "Some game folders/registry keys are not writeable by your user account. ALOT Installer will attempt to grant access to these folders/registry with the PermissionsGranter.exe program:\n";
                        if (required)
                        {
                            message = "Some game paths and registry keys are not writeable by your user account. These need to be writable or ALOT Installer will be unable to install ALOT. Please grant administrative privledges to PermissionsGranter.exe to give your account the necessary privileges to the following:\n";
                        }
                        foreach (String str in directories)
                        {
                            message += "\n" + str;
                        }
                        if (me1AGEIAKeyNotWritable)
                        {
                            message += "\nRegistry: HKLM\\SOFTWARE\\WOW6432Node\\AGEIA Technologies (Fixes an ME1 launch issue)";
                        }
                        await this.ShowMessageAsync("Granting permissions to Mass Effect directories", message);
                        string exe = BINARY_DIRECTORY + "PermissionsGranter.exe";
                        int result = Utilities.runProcessAsAdmin(exe, args);
                        if (result == 0)
                        {
                            Log.Information("Elevated process returned code 0, directories are hopefully writable now.");
                            return true;
                        }
                        else
                        {
                            Log.Error("Elevated process returned code " + result + ", directories probably aren't writable.");
                            return false;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error checking for write privledges. This may be a significant sign that an installed game is not in a good state.");
                Log.Error(App.FlattenException(e));
                await this.ShowMessageAsync("Error checking write privileges", "An error occured while checking write privileges to game folders. This may be a sign that the game is in a bad state.\n\nThe error was:\n" + e.Message);
                return false;
            }
            return true;
        }

        private async void PerformUACCheck()
        {
            bool isAdmin = Utilities.IsAdministrator();

            //Check if UAC is off
            bool uacIsOn = true;
            string softwareKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System";

            int? value = (int?)Registry.GetValue(softwareKey, "EnableLUA", null);
            if (value != null)
            {
                uacIsOn = value > 0;
                Log.Information("UAC is on: " + uacIsOn);
            }
            if (isAdmin && uacIsOn)
            {
                Log.Warning("This session is running as administrator.");
                await this.ShowMessageAsync("ALOT Installer should be run as standard user", "Running ALOT Installer as an administrator will disable drag and drop functionality and may cause issues due to the program running in a different user context. You should restart the application without running it as an administrator.");
            }
        }

        private void UpdateALOTStatus()
        {
            CURRENTLY_INSTALLED_ME1_ALOT_INFO = Utilities.GetInstalledALOTInfo(1);
            CURRENTLY_INSTALLED_ME2_ALOT_INFO = Utilities.GetInstalledALOTInfo(2);
            CURRENTLY_INSTALLED_ME3_ALOT_INFO = Utilities.GetInstalledALOTInfo(3);


            string me1ver = "";
            string me2ver = "";
            string me3ver = "";


            if (CURRENTLY_INSTALLED_ME1_ALOT_INFO != null)
            {
                if (CURRENTLY_INSTALLED_ME1_ALOT_INFO.ALOTVER > 0)
                {
                    bool meuitminstalled = CURRENTLY_INSTALLED_ME1_ALOT_INFO.MEUITMVER > 0;
                    me1ver = CURRENTLY_INSTALLED_ME1_ALOT_INFO.ALOTVER + "." + CURRENTLY_INSTALLED_ME1_ALOT_INFO.ALOTUPDATEVER + (meuitminstalled ? ", MEUITM v" + CURRENTLY_INSTALLED_ME1_ALOT_INFO.MEUITMVER : "");
                }
                else
                {
                    if (CURRENTLY_INSTALLED_ME1_ALOT_INFO.MEUITMVER > 0)
                    {
                        me1ver = "ALOT: N/A, MEUITM: v" + CURRENTLY_INSTALLED_ME1_ALOT_INFO.MEUITMVER;

                    }
                    else
                    {
                        me1ver = "Installed, unable to detect version";
                    }
                }
            }
            else
            {
                me1ver = "ALOT/MEUITM not installed";
            }

            if (CURRENTLY_INSTALLED_ME2_ALOT_INFO != null)
            {
                if (CURRENTLY_INSTALLED_ME2_ALOT_INFO.ALOTVER > 0)
                {
                    me2ver = CURRENTLY_INSTALLED_ME2_ALOT_INFO.ALOTVER + "." + CURRENTLY_INSTALLED_ME2_ALOT_INFO.ALOTUPDATEVER;
                }
                else
                {
                    me2ver = "Installed, unable to detect version";
                }
            }
            else
            {
                me2ver = "ALOT not installed";
            }

            if (CURRENTLY_INSTALLED_ME3_ALOT_INFO != null)
            {
                if (CURRENTLY_INSTALLED_ME3_ALOT_INFO.ALOTVER > 0)
                {
                    me3ver = CURRENTLY_INSTALLED_ME3_ALOT_INFO.ALOTVER + "." + CURRENTLY_INSTALLED_ME3_ALOT_INFO.ALOTUPDATEVER;
                }
                else
                {
                    me3ver = "Installed, unable to detect version";
                }
            }
            else
            {
                me3ver = "ALOT not installed";
            }

            string me1ToolTip = CURRENTLY_INSTALLED_ME1_ALOT_INFO != null ? "ALOT detected as installed" : "ALOT not detected as installed. Detection requires installation through ALOT or MEUITM Installer.";
            string me2ToolTip = CURRENTLY_INSTALLED_ME2_ALOT_INFO != null ? "ALOT detected as installed" : "ALOT not detected as installed. Detection requires installation through ALOT Installer.";
            string me3ToolTip = CURRENTLY_INSTALLED_ME3_ALOT_INFO != null ? "ALOT detected as installed" : "ALOT not detected as installed. Detection requires installation through ALOT Installer.";

            string message1 = "ME1: " + me1ver;
            string message2 = "ME2: " + me2ver;
            string message3 = "ME3: " + me3ver;

            Label_ALOTStatus_ME1.Content = message1;
            Label_ALOTStatus_ME2.Content = message2;
            Label_ALOTStatus_ME3.Content = message3;

            Label_ALOTStatus_ME1.ToolTip = me1ToolTip;
            Label_ALOTStatus_ME2.ToolTip = me2ToolTip;
            Label_ALOTStatus_ME3.ToolTip = me3ToolTip;

            //Button_ME1_ShowLODOptions.Visibility = (CURRENTLY_INSTALLED_ME1_ALOT_INFO != null && CURRENTLY_INSTALLED_ME1_ALOT_INFO.ALOTVER > 0) ? Visibility.Visible : Visibility.Collapsed;

            foreach (AddonFile af in AllAddonFiles)
            {
                af.ReadyStatusText = null; //update description
            }
        }

        private void playFirstTimeAnimation()
        {
            foreach (FrameworkElement tb in fadeInItems)
            {
                tb.Opacity = 0;
            }
            Button_FirstRun_Dismiss.Opacity = 0;
            FirstRunFlyout.IsOpen = true;
            currentFadeInItems = fadeInItems.ToList();
            #region Fade in
            // Create a storyboard to contain the animations.
            Storyboard storyboard = new Storyboard();
            TimeSpan duration = new TimeSpan(0, 0, 2);

            // Create a DoubleAnimation to fade the not selected option control
            DoubleAnimation animation = new DoubleAnimation();

            animation.From = 0.0;
            animation.To = 1.0;
            animation.BeginTime = new TimeSpan(0, 0, 2);
            animation.Duration = new Duration(duration);
            animation.Completed += new EventHandler(ItemFadeInComplete_Chain);

            FrameworkElement item = currentFadeInItems[0];
            currentFadeInItems.RemoveAt(0);
            // Configure the animation to target de property Opacity
            Storyboard.SetTargetName(animation, item.Name);
            Storyboard.SetTargetProperty(animation, new PropertyPath(OpacityProperty));
            // Add the animation to the storyboard
            storyboard.Children.Add(animation);

            // Begin the storyboard
            storyboard.Begin(this);

            #endregion
        }

        private void ItemFadeInComplete_Chain(object sender, EventArgs e)
        {
            Storyboard storyboard = new Storyboard();
            TimeSpan duration = new TimeSpan(0, 0, 0, 0, 700);

            // Create a DoubleAnimation to fade the not selected option control
            DoubleAnimation animation = new DoubleAnimation();

            animation.From = 0.0;
            animation.To = 1.0;
            animation.Duration = new Duration(duration);

            System.Windows.FrameworkElement item;
            if (currentFadeInItems.Count > 0)
            {
                item = currentFadeInItems[0];
                animation.Completed += new EventHandler(ItemFadeInComplete_Chain);
                currentFadeInItems.RemoveAt(0);
            }
            else
            {
                item = Button_FirstRun_Dismiss;
            }

            // Configure the animation to target de property Opacity
            Storyboard.SetTargetName(animation, item.Name);
            Storyboard.SetTargetProperty(animation, new PropertyPath(OpacityProperty));
            // Add the animation to the storyboard
            storyboard.Children.Add(animation);

            // Begin the storyboard
            storyboard.Begin(this);
        }

        private async void readManifest()
        {
            //if (!File.Exists(@"manifest.xml"))
            //{
            //    await FetchManifest();
            //    return;
            //}
            Log.Information("Reading manifest...");
            List<AddonFile> linqlist = null;
            musicpackmirrors = new List<string>();

            try
            {
                XElement rootElement = XElement.Load(MANIFEST_LOC);
                string version = (string)rootElement.Attribute("version") ?? "";
                Debug.WriteLine("Manifest version: " + version);
                musicpackmirrors = rootElement.Elements("musicpackmirror").Select(xe => xe.Value).ToList();
                AllTutorials.AddRange((from e in rootElement.Elements("tutorial")
                                       select new ManifestTutorial
                                       {
                                           Link = (string)e.Attribute("link"),
                                           Text = (string)e.Attribute("text"),
                                           ToolTip = (string)e.Attribute("tooltip"),
                                           MEUITMOnly = e.Attribute("meuitm") != null ? (bool)e.Attribute("meuitm") : false
                                       }).ToList());

                HIGHEST_APPROVED_STABLE_MEMNOGUIVERSION = rootElement.Element("highestapprovedmemversion") == null ? HIGHEST_APPROVED_STABLE_MEMNOGUIVERSION : (int)rootElement.Element("highestapprovedmemversion");
                if (rootElement.Element("soaktestingmemversion") != null)
                {
                    XElement soakElem = rootElement.Element("soaktestingmemversion");
                    SOAK_APPROVED_STABLE_MEMNOGUIVERSION = (int)soakElem;
                    if (soakElem.Attribute("soakstartdate") != null)
                    {
                        string soakStartDateStr = soakElem.Attribute("soakstartdate").Value;
                        SOAK_START_DATE = DateTime.ParseExact(soakStartDateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    }
                }

                Utilities.SUPPORTED_HASHES_ME1.Clear();
                Utilities.SUPPORTED_HASHES_ME2.Clear();
                Utilities.SUPPORTED_HASHES_ME3.Clear();

                if (rootElement.Element("supportedhashes") != null)
                {
                    var supportedHashesList = rootElement.Element("supportedhashes").Descendants("supportedhash");
                    foreach (var item in supportedHashesList)
                    {
                        KeyValuePair<string, string> kvp = new KeyValuePair<string, string>(item.Value, (string)item.Attribute("name"));
                        switch ((string)item.Attribute("game"))
                        {
                            case "me1":
                                Utilities.SUPPORTED_HASHES_ME1.Add(kvp);
                                break;
                            case "me2":
                                Utilities.SUPPORTED_HASHES_ME2.Add(kvp);
                                break;
                            case "me3":
                                Utilities.SUPPORTED_HASHES_ME3.Add(kvp);
                                break;
                        }
                    }
                }

                if (rootElement.Element("stages") != null)
                {
                    ProgressWeightPercentages.Stages =
                        (from stage in rootElement.Element("stages").Descendants("stage")
                         select new Stage
                         {
                             StageName = stage.Attribute("name").Value,
                             TaskName = stage.Attribute("tasktext").Value,
                             Weight = Convert.ToDouble(stage.Attribute("weight").Value, CultureInfo.InvariantCulture),
                             ME1Scaling = stage.Attribute("me1weightscaling") != null ? Convert.ToDouble(stage.Attribute("me1weightscaling").Value, CultureInfo.InvariantCulture) : 1,
                             ME2Scaling = stage.Attribute("me2weightscaling") != null ? Convert.ToDouble(stage.Attribute("me2weightscaling").Value, CultureInfo.InvariantCulture) : 1,
                             ME3Scaling = stage.Attribute("me3weightscaling") != null ? Convert.ToDouble(stage.Attribute("me3weightscaling").Value, CultureInfo.InvariantCulture) : 1,
                             FailureInfos = stage.Elements("failureinfo").Select(z => new StageFailure
                             {
                                 FailureIPCTrigger = z.Attribute("ipcerror") != null ? z.Attribute("ipcerror").Value : null,
                                 FailureBottomText = z.Attribute("failedbottommessage").Value,
                                 FailureTopText = z.Attribute("failedtopmessage").Value,
                                 FailureHeaderText = z.Attribute("failedheadermessage").Value,
                                 FailureResultCode = Convert.ToInt32(z.Attribute("resultcode").Value),
                                 Warning = z.Attribute("warning") != null ? bool.Parse(z.Attribute("warning").Value) : false
                             }).ToList()
                         }).ToList();
                }
                else
                {
                    ProgressWeightPercentages.SetDefaultWeights();
                }
                var repackoptions = rootElement.Element("repackoptions");
                if (repackoptions != null)
                {
                    ME2_REPACK_MANIFEST_ENABLED = repackoptions.Attribute("me2repackenabled") != null ? (bool)repackoptions.Attribute("me2repackenabled") : true;
                    Log.Information("Manifest says ME2 repack option can be used: " + ME2_REPACK_MANIFEST_ENABLED);
                    ME3_REPACK_MANIFEST_ENABLED = repackoptions.Attribute("me3repackenabled") != null ? (bool)repackoptions.Attribute("me3repackenabled") : false;
                    Log.Information("Manifest says ME3 repack option can be used: " + ME3_REPACK_MANIFEST_ENABLED);
                    Checkbox_RepackME2GameFiles.IsEnabled = ME2_REPACK_MANIFEST_ENABLED;
                    Checkbox_RepackME3GameFiles.IsEnabled = ME3_REPACK_MANIFEST_ENABLED;
                    if (!ME2_REPACK_MANIFEST_ENABLED)
                    {
                        Checkbox_RepackME2GameFiles.IsChecked = false;
                        Checkbox_RepackME2GameFiles.ToolTip = "Disabled by server manifest";
                    }
                    if (!ME3_REPACK_MANIFEST_ENABLED)
                    {
                        Checkbox_RepackME3GameFiles.IsChecked = false;
                        Checkbox_RepackME3GameFiles.ToolTip = "Disabled by server manifest";
                    }
                }
                else
                {
                    Log.Information("Manifest does not have repackoptions - using defaults");
                }

                if (rootElement.Element("me3dlctexturefixes") != null)
                {
                    ME3DLCRequiringTextureExportFixes = rootElement.Elements("me3dlctexturefixes").Descendants("dlc").Select(x => x.Attribute("name").Value.ToUpperInvariant()).ToList();
                }
                else
                {
                    ME3DLCRequiringTextureExportFixes = new List<string>();
                }

                if (rootElement.Element("me2dlctexturefixes") != null)
                {
                    ME2DLCRequiringTextureExportFixes = rootElement.Elements("me2dlctexturefixes").Descendants("dlc").Select(x => x.Attribute("name").Value.ToUpperInvariant()).ToList();
                }
                else
                {
                    ME2DLCRequiringTextureExportFixes = new List<string>();
                }

                linqlist = (from e in rootElement.Elements("addonfile")
                            select new AddonFile
                            {
                                AlreadyInstalled = false,
                                Showing = false,
                                Enabled = true,
                                ComparisonsLink = (string)e.Attribute("comparisonslink"),
                                InstallME1DLCASI = e.Attribute("installme1dlcasi") != null ? (bool)e.Attribute("installme1dlcasi") : false,
                                FileSize = e.Element("file").Attribute("size") != null ? Convert.ToInt64((string)e.Element("file").Attribute("size")) : 0L,
                                CopyDirectly = e.Element("file").Attribute("copydirectly") != null ? (bool)e.Element("file").Attribute("copydirectly") : false,
                                MEUITM = e.Attribute("meuitm") != null ? (bool)e.Attribute("meuitm") : false,
                                MEUITMVer = e.Attribute("meuitmver") != null ? Convert.ToInt32((string)e.Attribute("meuitmver")) : 0,
                                ProcessAsModFile = e.Attribute("processasmodfile") != null ? (bool)e.Attribute("processasmodfile") : false,
                                Author = (string)e.Attribute("author"),
                                FriendlyName = (string)e.Attribute("friendlyname"),
                                Optional = e.Attribute("optional") != null ? (bool)e.Attribute("optional") : false,
                                Game_ME1 = e.Element("games") != null ? (bool)e.Element("games").Attribute("me1") : false,
                                Game_ME2 = e.Element("games") != null ? (bool)e.Element("games").Attribute("me2") : false,
                                Game_ME3 = e.Element("games") != null ? (bool)e.Element("games").Attribute("me3") : false,
                                Filename = (string)e.Element("file").Attribute("filename"),
                                Tooltipname = e.Element("file").Attribute("tooltipname") != null ? (string)e.Element("file").Attribute("tooltipname") : (string)e.Attribute("friendlyname"),
                                DownloadLink = (string)e.Element("file").Attribute("downloadlink"),
                                ALOTVersion = e.Attribute("alotversion") != null ? Convert.ToInt16((string)e.Attribute("alotversion")) : (short)0,
                                ALOTUpdateVersion = e.Attribute("alotupdateversion") != null ? Convert.ToByte((string)e.Attribute("alotupdateversion")) : (byte)0,
                                UnpackedSingleFilename = e.Element("file").Attribute("unpackedsinglefilename") != null ? (string)e.Element("file").Attribute("unpackedsinglefilename") : null,
                                ALOTMainVersionRequired = e.Attribute("appliestomainversion") != null ? Convert.ToInt16((string)e.Attribute("appliestomainversion")) : (short)0,
                                FileMD5 = (string)e.Element("file").Attribute("md5"),
                                UnpackedFileMD5 = (string)e.Element("file").Attribute("unpackedmd5"),
                                UnpackedFileSize = e.Element("file").Attribute("unpackedsize") != null ? Convert.ToInt64((string)e.Element("file").Attribute("unpackedsize")) : 0L,
                                TorrentFilename = (string)e.Element("file").Attribute("torrentfilename"),
                                Ready = false,
                                IsModManagerMod = e.Element("file").Attribute("modmanagermod") != null ? (bool)e.Element("file").Attribute("modmanagermod") : false,
                                ExtractionRedirects = e.Elements("extractionredirect")
                                    .Select(d => new ExtractionRedirect
                                    {
                                        ArchiveRootPath = (string)d.Attribute("archiverootpath"),
                                        RelativeDestinationDirectory = (string)d.Attribute("relativedestinationdirectory"),
                                        OptionalRequiredDLC = (string)d.Attribute("optionalrequireddlc"),
                                        DLCFriendlyName = (string)d.Attribute("dlcname"),
                                        IsDLC = d.Attribute("isdlc") != null ? (bool)d.Attribute("isdlc") : false,
                                        ModVersion = (string)d.Attribute("version")
                                    }).ToList(),
                                PackageFiles = e.Elements("packagefile")
                                    .Select(r => new PackageFile
                                    {
                                        ChoiceTitle = "", //unused in this block
                                        SourceName = (string)r.Attribute("sourcename"),
                                        DestinationName = (string)r.Attribute("destinationname"),
                                        TPFSource = (string)r.Attribute("tpfsource"),
                                        MoveDirectly = r.Attribute("movedirectly") != null ? true : false,
                                        CopyDirectly = r.Attribute("copydirectly") != null ? true : false,
                                        Delete = r.Attribute("delete") != null ? true : false,
                                        ME1 = r.Attribute("me1") != null ? true : false,
                                        ME2 = r.Attribute("me2") != null ? true : false,
                                        ME3 = r.Attribute("me3") != null ? true : false,
                                        Processed = false
                                    }).ToList(),
                                ChoiceFiles = e.Elements("choicefile")
                                    .Select(q => new ChoiceFile
                                    {
                                        ChoiceTitle = (string)q.Attribute("choicetitle"),
                                        Choices = q.Elements("packagefile").Select(c => new PackageFile
                                        {
                                            ChoiceTitle = (string)c.Attribute("choicetitle"),
                                            SourceName = (string)c.Attribute("sourcename"),
                                            DestinationName = (string)c.Attribute("destinationname"),
                                            TPFSource = (string)c.Attribute("tpfsource"),
                                            MoveDirectly = c.Attribute("movedirectly") != null ? true : false,
                                            CopyDirectly = c.Attribute("copydirectly") != null ? true : false,
                                            Delete = c.Attribute("delete") != null ? true : false,
                                            ME1 = c.Attribute("me1") != null ? true : false,
                                            ME2 = c.Attribute("me2") != null ? true : false,
                                            ME3 = c.Attribute("me3") != null ? true : false,
                                            Processed = false
                                        }).ToList()
                                    }).ToList(),
                                ZipFiles = e.Elements("zipfile")
                                    .Select(q => new classes.ZipFile
                                    {
                                        ChoiceTitle = (string)q.Attribute("choicetitle"),
                                        Optional = q.Attribute("optional") != null ? (bool)q.Attribute("optional") : false,
                                        DefaultOption = q.Attribute("default") != null ? (bool)q.Attribute("default") : true,
                                        InArchivePath = q.Attribute("inarchivepath").Value,
                                        GameDestinationPath = q.Attribute("gamedestinationpath").Value,
                                        DeleteShaders = q.Attribute("deleteshaders") != null ? (bool)q.Attribute("deleteshaders") : false, //me1 only
                                        MEUITMSoftShadows = q.Attribute("meuitmsoftshadows") != null ? (bool)q.Attribute("meuitmsoftshadows") : false, //me1,meuitm only
                                    }).ToList(),
                                CopyFiles = e.Elements("copyfile")
                                    .Select(q => new CopyFile
                                    {
                                        ChoiceTitle = (string)q.Attribute("choicetitle"),
                                        Optional = q.Attribute("optional") != null ? (bool)q.Attribute("optional") : false,
                                        DefaultOption = q.Attribute("default") != null ? (bool)q.Attribute("default") : true,
                                        InArchivePath = q.Attribute("inarchivepath").Value,
                                        GameDestinationPath = q.Attribute("gamedestinationpath").Value,
                                    }).ToList(),
                            }).ToList();
                if (!version.Equals(""))
                {
                    Log.Information("Manifest version: " + version);
                    Title += " - Manifest version " + version;
                    if (UsingBundledManifest)
                    {
                        Title += " (Bundled)";
                        Log.Information("Using bundled manifest. Something might be wrong...");
                    }

                }
                //throw new Exception("Test error.");
            }
            catch (Exception e)
            {
                Log.Error("Error has occured parsing the XML!");
                Log.Error(App.FlattenException(e));
                MessageDialogResult result = await this.ShowMessageAsync("Error reading file manifest", "An error occured while reading the manifest file for installation. This may indicate a network failure or a packaging failure by Mgamerz - Please submit an issue to github (http://github.com/ME3Tweaks/ALOTInstaller/issues) and include the most recent log file from the logs directory.\n\n" + e.Message, MessageDialogStyle.Affirmative);
                AddonFilesLabel.Text = "Error parsing manifest XML! Check the logs.";
                return;
            }
            linqlist = linqlist.OrderBy(o => o.Author).ThenBy(x => x.FriendlyName).ToList();


            AllAddonFiles.ReplaceAll(linqlist);
            DisplayedAddonFiles.ReplaceAll(AllAddonFiles);
            //get list of installed games
            SetupButtons();
            int meuitmindex = -1;
            foreach (AddonFile af in DisplayedAddonFiles)
            {
                //Set Game
                foreach (PackageFile pf in af.PackageFiles)
                {
                    //Damn I did not think this one through very well
                    af.Game_ME1 |= pf.ME1;
                    af.Game_ME2 |= pf.ME2;
                    af.Game_ME3 |= pf.ME3;
                    if (!af.Game_ME1 && !af.Game_ME2 && !af.Game_ME3)
                    {
                        af.Game_ME1 = af.Game_ME2 = af.Game_ME3 = true; //if none is set, then its set to all
                    }
                    if (af.MEUITM)
                    {
                        meuitmindex = AllAddonFiles.IndexOf(af);
                        meuitmFile = af;
                    }
                }
            }
            UpdateALOTStatus();
            string me1status = "ME1 MEMI Marker: ";
            if (CURRENTLY_INSTALLED_ME1_ALOT_INFO != null)
            {
                me1status += "ALOT " + CURRENTLY_INSTALLED_ME1_ALOT_INFO.ALOTVER + "." + CURRENTLY_INSTALLED_ME1_ALOT_INFO.ALOTUPDATEVER + "." + CURRENTLY_INSTALLED_ME1_ALOT_INFO.ALOTHOTFIXVER + ", MEUITM v" + CURRENTLY_INSTALLED_ME1_ALOT_INFO.MEUITMVER;
            }
            else
            {
                me1status += "Not installed";
            }
            Log.Information(me1status);

            string me2status = "ME2 MEMI Marker: ";
            if (CURRENTLY_INSTALLED_ME2_ALOT_INFO != null)
            {
                me2status += "ALOT " + CURRENTLY_INSTALLED_ME2_ALOT_INFO.ALOTVER + "." + CURRENTLY_INSTALLED_ME2_ALOT_INFO.ALOTUPDATEVER + "." + CURRENTLY_INSTALLED_ME2_ALOT_INFO.ALOTHOTFIXVER;
            }
            else
            {
                me2status += "Not installed";
            }
            Log.Information(me2status);

            string me3status = "ME3 MEMI Marker: ";
            if (CURRENTLY_INSTALLED_ME3_ALOT_INFO != null)
            {
                me3status += "ALOT " + CURRENTLY_INSTALLED_ME3_ALOT_INFO.ALOTVER + "." + CURRENTLY_INSTALLED_ME3_ALOT_INFO.ALOTUPDATEVER + "." + CURRENTLY_INSTALLED_ME3_ALOT_INFO.ALOTHOTFIXVER;
            }
            else
            {
                me3status += "Not installed";
            }
            Log.Information(me3status);
            ApplyFiltering(); //sets data source and separators
            RunMEMUpdater2();
        }

        private void UpdateTutorialPanel()
        {
            StackPanel_ManifestTutorials.Children.Clear();
            var tutorials = AllTutorials.Where(x => (!MEUITM_INSTALLER_MODE && !x.MEUITMOnly || MEUITM_INSTALLER_MODE && x.MEUITMOnly)).ToList();
            if (tutorials.Count > 0)
            {
                Label_NoTutorials.Visibility = Visibility.Collapsed;
                foreach (ManifestTutorial tut in tutorials)
                {
                    System.Windows.Controls.Button buttonOK = new System.Windows.Controls.Button();
                    buttonOK.Content = tut.Text;
                    buttonOK.ToolTip = tut.ToolTip;
                    buttonOK.Margin = new Thickness(20, 0, 20, 3);
                    buttonOK.Padding = new Thickness(0, 3, 0, 3);
                    buttonOK.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center;

                    buttonOK.Style = (Style)FindResource("AccentedSquareButtonStyle");
                    ControlsHelper.SetContentCharacterCasing(buttonOK, System.Windows.Controls.CharacterCasing.Upper);
                    //                    buttonOK.FontSize = 12;
                    //                    buttonOK.Contr
                    //Style = "{StaticResource AccentedSquareButtonStyle}" Controls: ControlsHelper.ContentCharacterCasing = "Upper"
                    buttonOK.Click += async (s, e) =>
                    {
                        try
                        {
                            Log.Information("Opening URL: " + tut.Link);
                            System.Diagnostics.Process.Start(tut.Link);
                        }
                        catch (Exception other)
                        {
                            Log.Error("Exception opening browser - handled. The error was " + other.Message);
                            System.Windows.Clipboard.SetText(tut.Link);
                            await this.ShowMessageAsync("Unable to open web browser", "Unable to open your default web browser. Open your browser and paste the link (already copied to clipboard) into your URL bar.");
                        }
                    };
                    StackPanel_ManifestTutorials.Children.Add(buttonOK);
                }
            }
        }

        private void ApplyFiltering(bool scrollToBottom = false)
        {
            if (MEUITM_INSTALLER_MODE)
            {
                var preVal = Loading;
                Loading = true;
                ShowME1Files = true;
                ShowME2Files = false;
                ShowME3Files = false;
                Loading = preVal;
            }
            List<AddonFile> newList = new List<AddonFile>();
            if (meuitmFile != null)
            {

                if (CURRENTLY_INSTALLED_ME1_ALOT_INFO != null && CURRENTLY_INSTALLED_ME1_ALOT_INFO.MEUITMVER > 0)
                {
                    //Disable MEUITM
                    meuitmFile.AlreadyInstalled = true;
                }
                else
                {
                    meuitmFile.AlreadyInstalled = false;
                }
            }
            foreach (AddonFile af in AllAddonFiles)
            {
                if (ShowBuildingOnly)
                {
                    if (af.Building)
                    {
                        newList.Add(af);
                    }
                }
                else
                {
                    if ((!af.Ready || !af.Enabled) && ShowReadyFilesOnly)
                    { continue; }
                    bool shouldDisplay = ((af.Game_ME1 && ShowME1Files) || (af.Game_ME2 && ShowME2Files) || (af.Game_ME3 && ShowME3Files));
                    if (shouldDisplay)
                    {
                        if (MEUITM_INSTALLER_MODE && !af.MEUITM)
                        {
                            continue;
                        }
                        else
                        {
                            newList.Add(af);
                        }
                    }
                }
            }
            var set = new HashSet<AddonFile>(newList);

            if (ListView_Files.Items.Count == 0 || !set.SetEquals(DisplayedAddonFiles))
            {
                //refresh ui
                DisplayedAddonFiles.ReplaceAll(newList);

            }
            CheckImportLibrary_Tick(null, null);

            if (DOWNLOAD_ASSISTANT_WINDOW != null)
            {
                List<AddonFile> notReadyAddonFiles = new List<AddonFile>();
                foreach (AddonFile af in DisplayedAddonFiles)
                {
                    if (!af.Ready && !af.UserFile)
                    {
                        notReadyAddonFiles.Add(af);
                    }
                }
                DOWNLOAD_ASSISTANT_WINDOW.setNewMissingAddonfiles(notReadyAddonFiles);
            }

            if (scrollToBottom && VisualTreeHelper.GetChildrenCount(ListView_Files) > 0)
            {
                Border border = (Border)VisualTreeHelper.GetChild(ListView_Files, 0);
                ScrollViewer scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
                scrollViewer.ScrollToBottom();
            }
        }

        /// <summary>
        /// Class for passing data between threads
        /// </summary>
        public class ThreadCommand
        {
            /// <summary>
            /// Creates a new thread command object with the specified command and data object. This constructori s used for passing data to another thread. The receiver will need to read the command then cast the data.
            /// </summary>
            /// <param name="command">command for this thread communication.</param>
            /// <param name="data">data to pass to another thread</param>
            public ThreadCommand(string command, object data)
            {
                this.Command = command;
                this.Data = data;
            }

            /// <summary>
            /// Creates a new thread command object with the specified command. This constructor is used for notifying other threads something has happened.
            /// </summary>
            /// <param name="command">command for this thread communication.</param>
            /// <param name="data">data to pass to another thread</param>
            public ThreadCommand(string command)
            {
                this.Command = command;
            }

            public string Command;
            public object Data;
        }

        private async void Button_InstallME2_Click(object sender, RoutedEventArgs e)
        {
            if (await InstallPrecheck(2))
            {
                ShowBuildOptions(2);
            }
        }

        private void ShowBuildOptions(int game)
        {
            CURRENT_GAME_BUILD = game;
            Loading = true; //preven 1;
            ShowME1Files = game == 1;
            ShowME2Files = game == 2;
            ShowME3Files = game == 3;
            Loading = false;
            ShowReadyFilesOnly = true;
            PreventFileRefresh = true;
            ApplyFiltering();
            ALOTVersionInfo installedInfo = Utilities.GetInstalledALOTInfo(game);
            bool alotInstalled = installedInfo != null && installedInfo.ALOTVER > 0; //default value
            bool alotavailalbleforinstall = false;
            bool alotupdateavailalbeforinstall = false;
            bool meuitmavailableforinstall = false;
            int installedALOTUpdateVersion = (installedInfo == null) ? 0 : installedInfo.ALOTUPDATEVER;
            if (installedInfo == null || installedInfo.ALOTVER == 0 && installedInfo.MEUITMVER > 0) //not installed or mem installed
            {
                Checkbox_BuildOptionAddon.IsChecked = true;
                Checkbox_BuildOptionAddon.IsEnabled = false;
            }
            else
            {
                Checkbox_BuildOptionAddon.IsChecked = false;
                Checkbox_BuildOptionAddon.IsEnabled = true;
            }

            //installedInfo = null -> MEUITM, ALOT not installed
            //installedInfo = X ver = 0, meuitmver = 0 -> ALOT Installed via MEM Installer
            //installedInfo = X ver > 0, mueitmver = 0 -> ALOT installed with ALOT installer
            //installedInfo = X, ver = 0, meuitmver > 0 -> MEUITM installed via MEM Installer, no alot (or maybe old one.)
            //installedInfo = X, ver > 0, meuitmver > 0-> MEUITM installed, alot  installed via alot installer
            //

            bool hasApplicableUserFile = false;
            bool checkAlotBox = false;
            bool checkAlotUpdateBox = false;
            bool checkMEUITMBox = false;

            int installingALOTver = 0;

            bool blockALOTInstallDueToMainVersionDiff = false;
            bool hasAddonFile = false;
            foreach (AddonFile af in DisplayedAddonFiles)
            {
                if (!af.Enabled)
                {
                    continue;
                }
                if ((af.Game_ME1 && game == 1) || (af.Game_ME2 && game == 2) || (af.Game_ME3 && game == 3))
                {
                    if (af.UserFile && af.Ready)
                    {
                        hasApplicableUserFile = true;
                        continue;
                    }
                    if (installedInfo != null && (af.ALOTVersion > 0 || af.ALOTUpdateVersion > 0))
                    {
                        if (installedInfo.ALOTVER == 0 && installedInfo.MEUITMVER == 0)
                        {
                            //alot 5.0 or unable to find version
                            Log.Warning("ALOT main version " + af.ALOTVersion + " blocked from installing because we are unable to detect version information for ALOT. This is typically from old 5.0 or lower installations which is not supported.");
                            blockALOTInstallDueToMainVersionDiff = true;
                            if (af.ALOTVersion > 0)
                            {
                                installingALOTver = af.ALOTVersion;
                            }
                            continue;
                        }
                        if (installedInfo.ALOTVER != 0 && installedInfo.ALOTVER != af.ALOTVersion && af.ALOTVersion != 0 && installedInfo.MEUITMVER > 0)
                        {
                            //alot installed same version
                            Log.Warning("ALOT main version " + af.ALOTVersion + " blocked from installing because it is different main version than the currently installed one.");
                            blockALOTInstallDueToMainVersionDiff = true;
                            if (af.ALOTVersion > 0)
                            {
                                installingALOTver = af.ALOTVersion;
                            }
                            continue;
                        }
                    }
                    if (af.ALOTVersion > 0)
                    {
                        alotavailalbleforinstall = true;
                        if (!alotInstalled)
                        {
                            checkAlotBox = true;
                        }
                        continue;
                    }
                    if (af.ALOTUpdateVersion > 0)
                    {
                        alotupdateavailalbeforinstall = true;
                        //Perform update check...
                        if (installedInfo != null)
                        {
                            if (installedInfo.ALOTUPDATEVER >= af.ALOTUpdateVersion)
                            {
                                checkAlotUpdateBox = false; //same or higher update is already installed
                            }
                            else
                            {
                                checkAlotUpdateBox = true;
                            }
                        }
                        else
                        {
                            checkAlotUpdateBox = true; //same or higher update is already installed
                        }
                        continue;
                    }

                    if (af.MEUITM)
                    {
                        meuitmavailableforinstall = true;
                        if (installedInfo == null || installedInfo.MEUITMVER < af.MEUITMVer)
                        {
                            checkMEUITMBox = true;
                        }
                        continue;
                    }

                    hasAddonFile = true;
                }
            }

            Checkbox_BuildOptionALOT.IsChecked = checkAlotBox;
            Checkbox_BuildOptionALOT.IsEnabled = !checkAlotBox && alotavailalbleforinstall;

            Checkbox_BuildOptionALOTUpdate.IsChecked = checkAlotUpdateBox;
            Checkbox_BuildOptionALOTUpdate.IsEnabled = !checkAlotUpdateBox && alotupdateavailalbeforinstall;
            Checkbox_BuildOptionALOTUpdate.Visibility = alotupdateavailalbeforinstall ? Visibility.Visible : Visibility.Collapsed;

            Checkbox_BuildOptionMEUITM.IsChecked = checkMEUITMBox;
            Checkbox_BuildOptionMEUITM.IsEnabled = !checkMEUITMBox && meuitmavailableforinstall;
            Checkbox_BuildOptionMEUITM.Visibility = meuitmavailableforinstall ? Visibility.Visible : Visibility.Collapsed;



            Checkbox_BuildOptionAddon.IsEnabled = hasAddonFile;

            Checkbox_BuildOptionUser.IsChecked = hasApplicableUserFile;
            Checkbox_BuildOptionUser.IsEnabled = hasApplicableUserFile;



            bool hasOneOption = false;
            foreach (System.Windows.Controls.CheckBox cb in buildOptionCheckboxes)
            {
                if (cb.IsEnabled)
                {
                    hasOneOption = true;
                    break;
                }
            }

            if (hasOneOption || blockALOTInstallDueToMainVersionDiff)
            {
                Label_WhatToBuildAndInstall.Text = "Choose what to install for Mass Effect" + GetGameNumberSuffix(CURRENT_GAME_BUILD) + ".";
                if (blockALOTInstallDueToMainVersionDiff)
                {
                    string currentString = "(Unknown version)";
                    if (installedInfo != null && installedInfo.ALOTVER > 0)
                    {
                        currentString = "(" + installedInfo.ALOTVER + "." + installedInfo.ALOTUPDATEVER + ")";
                    }

                    Label_WhatToBuildAndInstall.Text = "Imported ALOT file (" + installingALOTver + ".x) cannot be installed over the current installation " + currentString + "." + System.Environment.NewLine + Label_WhatToBuildAndInstall.Text;
                }
                else if (alotInstalled && installedInfo.ALOTVER > 0)
                {
                    Label_WhatToBuildAndInstall.Text = "ALOT is already installed. " + Label_WhatToBuildAndInstall.Text;
                }
                ShowReadyFilesOnly = false;
                WhatToBuildFlyout.IsOpen = true;
                Button_BuildAndInstall.IsEnabled = true;
            }
            else
            {
                if (blockALOTInstallDueToMainVersionDiff)
                {
                    Label_WhatToBuildAndInstall.Text = "Imported ALOT file (" + installingALOTver + ".x) cannot be installed over the current installation (" + installedInfo.ALOTVER + "." + installedInfo.ALOTUPDATEVER + ")." + System.Environment.NewLine + Label_WhatToBuildAndInstall.Text;
                }
                //Run button 
                Button_BuildAndInstall_Click(null, null);
            }
        }

        private async void Button_ME1Backup_Click(object sender, RoutedEventArgs e)
        {
            if (BACKUP_THREAD_GAME > 0)
            {
                return;
            }
            if (ValidateGameBackup(1))
            {
                if (Utilities.IsGameRunning(1))
                {
                    await this.ShowMessageAsync("Mass Effect" + GetGameNumberSuffix(1) + " is running", "Please close Mass Effect" + GetGameNumberSuffix(1) + " before attempting restore.");
                    return;
                }

                //Game is backed up
                MetroDialogSettings settings = new MetroDialogSettings();
                settings.NegativeButtonText = "Cancel";
                settings.AffirmativeButtonText = "Restore";
                MessageDialogResult result = await this.ShowMessageAsync("Restoring Mass Effect from backup", "Restoring Mass Effect will wipe out the current installation and put your game back to the state when you backed it up. state. Are you sure you want to do this?", MessageDialogStyle.AffirmativeAndNegative, settings);
                if (result == MessageDialogResult.Affirmative)
                {
                    //RESTORE
                    RestoreGame(1);
                }
            }
            else
            {
                //MEM - VERIFY VANILLA FOR BACKUP
                BackupGame(1);
            }
        }
        private async void Button_ME2Backup_Click(object sender, RoutedEventArgs e)
        {
            if (BACKUP_THREAD_GAME > 0)
            {
                return;
            }
            if (ValidateGameBackup(2))
            {
                if (Utilities.IsGameRunning(2))
                {
                    await this.ShowMessageAsync("Mass Effect" + GetGameNumberSuffix(2) + " is running", "Please close Mass Effect" + GetGameNumberSuffix(2) + " before attempting restore.");
                    return;
                }

                //Game is backed up
                MetroDialogSettings settings = new MetroDialogSettings();
                settings.NegativeButtonText = "Cancel";
                settings.AffirmativeButtonText = "Restore";
                MessageDialogResult result = await this.ShowMessageAsync("Restoring Mass Effect 2 from backup", "Restoring Mass Effect 2 will wipe out the current installation and put your game back to the state when you backed it up. state. Are you sure you want to do this?", MessageDialogStyle.AffirmativeAndNegative, settings);
                if (result == MessageDialogResult.Affirmative)
                {
                    //RESTORE
                    RestoreGame(2);
                }
            }
            else
            {
                //MEM - VERIFY VANILLA FOR BACKUP
                BackupGame(2);
            }
        }

        private async void Button_ME3Backup_Click(object sender, RoutedEventArgs e)
        {
            if (BACKUP_THREAD_GAME > 0)
            {
                return;
            }
            if (ValidateGameBackup(3))
            {

                if (Utilities.IsGameRunning(3))
                {
                    await this.ShowMessageAsync("Mass Effect" + GetGameNumberSuffix(3) + " is running", "Please close Mass Effect" + GetGameNumberSuffix(3) + " before attempting restore.");
                    return;
                }
                //Game is backed up
                MetroDialogSettings settings = new MetroDialogSettings();
                settings.NegativeButtonText = "Cancel";
                settings.AffirmativeButtonText = "Restore";
                MessageDialogResult result = await this.ShowMessageAsync("Restoring Mass Effect 3 from backup", "Restoring Mass Effect 3 will wipe out the current installation and put your game back to the state when you backed it up. state. Are you sure you want to do this?", MessageDialogStyle.AffirmativeAndNegative, settings);
                if (result == MessageDialogResult.Affirmative)
                {
                    //RESTORE
                    RestoreGame(3);
                }
            }
            else
            {
                //Backup-Precheck
                List<string> folders = ME3Constants.getStandardDLCFolders();
                string me3DLCPath = ME3Constants.GetDLCPath();
                List<string> dlcFolders = new List<string>();
                if (Directory.Exists(me3DLCPath))
                {
                    foreach (string s in Directory.GetDirectories(me3DLCPath))
                    {
                        dlcFolders.Add(s.Remove(0, me3DLCPath.Length + 1)); //+1 for the final \\
                    }
                    var hasCustomDLC = dlcFolders.Except(folders);
                    if (hasCustomDLC.Count() > 0)
                    {
                        //Game is modified
                        string message = "Additional folders in the DLC directory were detected:";
                        foreach (string str in hasCustomDLC)
                        {
                            message += "\n - " + str;
                        }

                        message += "\n\nThis installation cannot be used for backup as it has been modified.";
                        await this.ShowMessageAsync("Mass Effect 3 is modified", message);
                        return;
                    }
                    //MEM - VERIFY VANILLA FOR BACKUP

                    BackupGame(3);
                }
                else
                {
                    Log.Error("Mass Effect 3 DLC directory is missing! Game path may be wrong, or game is probably FUBAR'd: " + me3DLCPath);
                    await this.ShowMessageAsync("Mass Effect 3 DLC directory is missing", "The DLC directory doesn't exist. There should be a DLC directory at " + me3DLCPath);
                    return;
                }
            }
        }

        private async void BackupGame(int game)
        {
            Log.Information("Start of UI thread BackupGame() for Mass Effect " + game);
            ALOTVersionInfo info = Utilities.GetInstalledALOTInfo(game);
            if (info != null)
            {
                //Game is modified via ALOT flag
                if (info.ALOTVER > 0)
                {
                    Log.Warning("ALOT is installed. Backup of ALOT installed game is not allowed.");
                    await this.ShowMessageAsync("ALOT is installed", "You cannot backup an installation that has ALOT already installed. If you have a backup, you can restore it by clicking the game backup button in the Settings menu. Otherwise, delete your game folder and redownload it.");
                }
                else if (info.MEUITMVER > 0)
                {
                    Log.Warning("MEUITM is installed. Backup of MEUITM installed game is not allowed.");
                    await this.ShowMessageAsync("MEUITM is installed", "You cannot backup an installation that has ALOT already installed. If you have a backup, you can restore it by clicking the game backup button in the Settings menu. Otherwise, delete your game folder and redownload it.");
                }
                else
                {
                    Log.Warning("ALOT or MEUITM is installed. Backup of ALOT or MEUITM installed game is not allowed.");
                    await this.ShowMessageAsync("ALOT is installed", "You cannot backup an installation that has ALOT already installed. If you have a backup, you can restore it by clicking the game backup button in the Settings menu. Otherwise, delete your game folder and redownload it.");
                }
                return;
            }

            string gamedir = Utilities.GetGamePath(game);
            if (gamedir == null)
            {
                //exe is missing? not sure how this could be null at this point.
                Log.Error("Game directory is null - has the filesystem changed since the app was booted?");
                await this.ShowMessageAsync("Cannot determine game path", "The game path cannot be determined - this is most likely a bug. Please report this issue to the developers on Discord (Settings -> Report an issue).");
                return;
            }

            if (game == 2 || game == 3)
            {
                //Check for Texture2D.tfc
                var path = Utilities.GetGamePath(game);
                if (game == 2) { path = Path.Combine(path, "BioGame", "CookedPC", "Texture2D.tfc"); }
                if (game == 3) { path = Path.Combine(path, "BIOGame", "CookedPCConsole", "Texture2D.tfc"); }
                if (File.Exists(path))
                {
                    Log.Error("Previous installation file found: " + path);
                    Log.Error("Game was not removed before reinstallation or was \"fixed\" using a game repair");
                    string howToFixStr = "You must delete your current game installation (do not uninstall or repair) to fully remove leftover files. You can use the ALOT Installer backup feature to backup a vanilla game once this is done.";
                    await this.ShowMessageAsync("Leftover files detected", "Files from a previous ALOT installation were detected and will cause installation to fail. " + howToFixStr);
                    return;
                }

                //Check for new MEM TextureMEM00.tfc file
                if (game == 2) { path = Path.Combine(path, "BioGame", "CookedPC", "TextureMEM00.tfc"); }
                if (game == 3) { path = Path.Combine(path, "BIOGame", "CookedPCConsole", "TextureMEM00.tfc"); }
                if (File.Exists(path))
                {
                    Log.Error("Previous installation file found: " + path);
                    Log.Error("Game was not removed before reinstallation or was \"fixed\" using a game repair");
                    string howToFixStr = "You must delete your current game installation (do not uninstall or repair) to fully remove leftover files. You can use the ALOT Installer backup feature to backup a vanilla game once this is done.";
                    await this.ShowMessageAsync("Leftover files detected", "Files from a previous ALOT installation were detected and will cause installation to fail. " + howToFixStr);
                    return;
                }
            }

            var openFolder = new CommonOpenFileDialog();
            openFolder.IsFolderPicker = true;
            openFolder.Title = "Select backup destination";
            openFolder.AllowNonFileSystemItems = false;
            openFolder.EnsurePathExists = true;
            if (openFolder.ShowDialog() != CommonFileDialogResult.Ok)
            {
                return;
            }
            Log.Information("User has chosen directory for backup destination: " + openFolder.FileName);

            var dir = openFolder.FileName;
            if (!Directory.Exists(dir))
            {
                Log.Error("User attempting to backup to directory that doesn't exist. Explorer can cause this issue sometimes by allow selection of previous directory.");
                await this.ShowMessageAsync("Directory does not exist", "The backup destination directory does not exist: " + dir);
                return;
            }
            if (!Utilities.IsDirectoryEmpty(dir))
            {
                Log.Warning("User attempting to backup to directory that is not empty");
                await this.ShowMessageAsync("Directory is not empty", "The backup destination directory must be empty.");
                return;
            }

            if (Utilities.IsSubfolder(gamedir, dir))
            {
                Log.Warning("User attempting to backup to subdirectory of backup source - not allowed because this will cause infinite recursion and will be deleted when restores are attempted");
                await this.ShowMessageAsync("Directory is subdirectory of game", "Backup directories cannot be subfolders of the game directory. Choose a different directory.");
                return;
            }
            BackupWorker = new BackgroundWorker();
            BackupWorker.DoWork += VerifyAndBackupGame;
            BackupWorker.WorkerReportsProgress = true;
            BackupWorker.ProgressChanged += BackupWorker_ProgressChanged;
            BackupWorker.RunWorkerCompleted += BackupCompleted;
            BACKUP_THREAD_GAME = game;
            SettingsFlyout.IsOpen = false;
            PreventFileRefresh = true;
            HeaderLabel.Text = "Backing up Mass Effect" + (game == 1 ? "" : " " + game) + "...\nDo not close the application until this process completes.";
            BackupWorker.RunWorkerAsync(dir);
            Button_InstallME1.IsEnabled = Button_InstallME2.IsEnabled = Button_InstallME3.IsEnabled = Button_Settings.IsEnabled = Button_DownloadAssistant.IsEnabled = false;
            ShowStatus("Verifying game data before backup", 4000);
            // get all the directories in selected dirctory
        }

        private void BackupCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Build_ProgressBar.IsIndeterminate = false;
            TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress, this);
            string destPath = (string)e.Result;
            if (destPath != null)
            {
                //Write registry key
                switch (BACKUP_THREAD_GAME)
                {
                    case 1:
                    case 2:
                        Utilities.WriteRegistryKey(Registry.CurrentUser, REGISTRY_KEY, "ME" + BACKUP_THREAD_GAME + "VanillaBackupLocation", destPath);
                        break;
                    case 3:
                        Utilities.WriteRegistryKey(Registry.CurrentUser, ME3_BACKUP_REGISTRY_KEY, "VanillaCopyLocation", destPath);
                        break;
                }
                ValidateGameBackup(BACKUP_THREAD_GAME);
                AddonFilesLabel.Text = "Backup completed.";
            }
            else
            {
                AddonFilesLabel.Text = "Backup failed! Check the logs.";
            }
            Button_Settings.IsEnabled = true;
            Button_DownloadAssistant.IsEnabled = true;
            SetInstallButtonsAvailability();
            PreventFileRefresh = false;
            ValidateGameBackup(BACKUP_THREAD_GAME);
            BACKUP_THREAD_GAME = -1;
            HeaderLabel.Text = PRIMARY_HEADER;
        }

        public static string GetGameNumberSuffix(int gameNumber)
        {
            return gameNumber == 1 ? "" : " " + gameNumber;
        }

        private async void Button_InstallME3_Click(object sender, RoutedEventArgs e)
        {
            if (await InstallPrecheck(3))
            {
                ShowBuildOptions(3);
            }
        }




        /// <summary>
        /// Returns the mem output dir with a \ on the end.
        /// </summary>
        /// <param name="game">Game number to get path for</param>
        /// <returns></returns>
        private string getOutputDir(int game, bool trailingSlash = true)
        {
            string ret = EXE_DIRECTORY + MEM_OUTPUT_DIR + "\\ME" + game;
            if (trailingSlash)
            {
                ret += "\\";
            }
            return ret;
        }

        private async void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            string fname = null;
            if (e.Source is Hyperlink)
            {
                fname = (string)((Hyperlink)e.Source).Tag;
            }
            try
            {
                Log.Information("Opening URL: " + e.Uri.ToString());
                System.Diagnostics.Process.Start(e.Uri.ToString());
                if (fname != null)
                {
                    this.nIcon.Visible = true;
                    //this.WindowState = System.Windows.WindowState.Minimized;
                    this.nIcon.Icon = Properties.Resources.tooltiptrayicon;
                    this.nIcon.ShowBalloonTip(14000, "Directions", "Download the file titled: \"" + fname + "\"", ToolTipIcon.Info);
                }
            }
            catch (Exception other)
            {
                Log.Error("Exception opening browser - handled. The error was " + other.Message);
                System.Windows.Clipboard.SetText(e.Uri.ToString());
                await this.ShowMessageAsync("Unable to open web browser", "Unable to open your default web browser. Open your browser and paste the link (already copied to clipboard) into your URL bar." + fname != null ? " Download the file named " + fname + ", then drag and drop it onto this program's interface." : "");
            }
        }

        private async Task<bool> InitBuild(int game)
        {
            if (game == 1 && MEUITM_INSTALLER_MODE)
            {
                AddonFile meuitm = AllAddonFiles.FirstOrDefault(x => x.MEUITM);
                if (meuitm != null)
                {
                    ModConfigurationDialog mcd = new ModConfigurationDialog(meuitm, this, true);
                    await this.ShowMetroDialogAsync(mcd);
                    await mcd.WaitUntilUnloadedAsync();
                    if (mcd.Canceled)
                    {
                        return false;
                    }
                }
            }

            Log.Information("InitBuild() started for Mass Effect " + game);

            AddonFilesLabel.Text = "Preparing to build texture packages...";
            CheckOutputDirectoriesForUnpackedSingleFiles(game);
            Build_ProgressBar.IsIndeterminate = true;
            Log.Information("Deleting any pre-existing extraction and staging directories.");
            string destinationpath = EXTRACTED_MODS_DIRECTORY;
            try
            {
                if (Directory.Exists(destinationpath))
                {
                    Utilities.DeleteFilesAndFoldersRecursively(destinationpath);
                }

                if (Directory.Exists(ADDON_FULL_STAGING_DIRECTORY))
                {
                    Utilities.DeleteFilesAndFoldersRecursively(ADDON_FULL_STAGING_DIRECTORY);
                }
                if (Directory.Exists(USER_FULL_STAGING_DIRECTORY))
                {
                    Utilities.DeleteFilesAndFoldersRecursively(USER_FULL_STAGING_DIRECTORY);
                }
            }
            catch (System.IO.IOException e)
            {
                Log.Error("Unable to delete staging and target directories.\n" + e.ToString());
                await this.ShowMessageAsync("Error occured while preparing directories", "ALOT Installer was unable to cleanup some directories. Make sure all file explorer windows are closed that may be open in the working directories.");
                return false;
            }

            PreventFileRefresh = true;
            Button_InstallME1.IsEnabled = false;
            Button_InstallME2.IsEnabled = false;
            Button_InstallME3.IsEnabled = false;
            Button_DownloadAssistant.IsEnabled = false;
            Button_Settings.IsEnabled = false;

            Directory.CreateDirectory(ADDON_FULL_STAGING_DIRECTORY);
            Directory.CreateDirectory(USER_FULL_STAGING_DIRECTORY);

            HeaderLabel.Text = "Preparing to build ALOT Addon for Mass Effect " + game + ".\nDon't close this window until the process completes.";
            // Install_ProgressBar.IsIndeterminate = true;
            Utilities.WriteRegistryKey(Registry.CurrentUser, REGISTRY_KEY, "CheckOutputDirectoriesOnManifestLoad", true);
            return true;
        }

        private void File_Drop_BackgroundThread(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                if (PreventFileRefresh)
                {
                    ShowStatus("Dropping files onto interface not available during operation", 5000);
                    return;
                }

                //if (MEUITM_INSTALLER_MODE)
                //{
                //    ShowStatus("Dropping files on window not supported in MEUITM mode, switch to ALOT mode for this feature");
                //    return;
                //}
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);

                PerformImportOperation(files);
            }
        }

        private void PerformImportOperation(string[] files, bool acceptUserFiles = true)
        {
            if (files.Count() > 0)
            {
                //don't know how you can drop less than 1 files but whatever
                //This code is for failsafe in case somehow library file exists but is not detected properly, like user moved file but something is running
                string file = files[0];
                string basepath = DOWNLOADED_MODS_DIRECTORY + "\\";

                if (file.ToLower().StartsWith(basepath.ToLower()))
                {
                    Log.Information("Cannot import files from the texture library folder.");
                    ShowStatus("Can't import files from texture library directory, files already at destination", 5000);
                    return;
                }
            }
            Log.Information("Files queued for import checks:");
            foreach (string file in files)
            {
                Log.Information(" - " + file);
            }
            List<Tuple<AddonFile, string, string>> filesToImport = new List<Tuple<AddonFile, string, string>>();
            List<string> noMatchFiles = new List<string>();
            long totalBytes = 0;
            List<string> acceptableUserFiles = new List<string>();
            List<string> alreadyImportedFiles = new List<string>();
            List<string> badSizeFiles = new List<string>();

            foreach (string file in files)
            {
                string fname = Path.GetFileName(file);
                //remove (1) and such
                string fnameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                if (fnameWithoutExtension.EndsWith(")"))
                {
                    if (fnameWithoutExtension.LastIndexOf("(") >= fnameWithoutExtension.Length - 3)
                    {
                        //it's probably a copy
                        fname = fnameWithoutExtension.Remove(fnameWithoutExtension.LastIndexOf("("), fnameWithoutExtension.LastIndexOf(")") - fnameWithoutExtension.LastIndexOf("(") + 1).Trim() + Path.GetExtension(file);
                        Log.Information("File Drag/Drop corrected to " + fname);
                    }
                }

                bool hasMatch = false;
                foreach (AddonFile af in AllAddonFiles)
                {
                    if (af.Ready == true) continue; //don't process ready files
                    if (af.UserFile)
                    {
                        if (af.UserFilePath == file && af.Ready)
                        {
                            alreadyImportedFiles.Add(file);
                            hasMatch = true;
                            break;
                        }
                        continue; //don't check these
                    }
                    bool isUnpackedSingleFile = af.UnpackedSingleFilename != null && af.UnpackedSingleFilename.Equals(fname, StringComparison.InvariantCultureIgnoreCase) && File.Exists(file); //make sure not folder with same name.
                    bool mainFileExists = af.Filename.Equals(fname, StringComparison.InvariantCultureIgnoreCase) && File.Exists(file);
                    bool torrentFileExists = af.TorrentFilename != null && af.TorrentFilename.Equals(fname, StringComparison.InvariantCultureIgnoreCase) && File.Exists(file);
                    //This code can be removed when microsoft or nexusmods fixes their %20 bug
                    bool msEdgeBugFileExists = af.Filename.Equals(fname.Replace("%20", " "), StringComparison.InvariantCultureIgnoreCase) && File.Exists(file);
                    if (isUnpackedSingleFile || mainFileExists || msEdgeBugFileExists || torrentFileExists) //make sure folder not with same name
                    {
                        hasMatch = true;
                        //Check size as validation
                        if (!isUnpackedSingleFile && af.FileSize > 0)
                        {
                            FileInfo fi = new FileInfo(file);
                            if (fi.Length != af.FileSize)
                            {
                                Log.Error("File to import has the wrong size: " + file + ", it should have size " + af.FileSize + ", but file to import is size " + fi.Length);
                                badSizeFiles.Add(file);
                                hasMatch = true;
                                continue;
                            }
                        }

                        if (isUnpackedSingleFile && af.UnpackedFileSize > 0)
                        {
                            FileInfo fi = new FileInfo(file);
                            if (fi.Length != af.UnpackedFileSize)
                            {
                                Log.Error("Unpacked file to import has the wrong size: " + file + ", it should have size " + af.FileSize + ", but file to import is size " + fi.Length);
                                badSizeFiles.Add(file);
                                hasMatch = true;
                                continue;
                            }
                        }

                        //Copy file to directory
                        string basepath = DOWNLOADED_MODS_DIRECTORY + "\\";
                        string destination = basepath + ((isUnpackedSingleFile) ? af.UnpackedSingleFilename : af.Filename);
                        //Log.Information("Copying dragged file to downloaded mods directory: " + file);
                        //File.Copy(file, destination, true);
                        filesToImport.Add(Tuple.Create(af, file, destination));
                        totalBytes += new System.IO.FileInfo(file).Length;
                        //filesimported.Add(af);
                        //timer_Tick(null, null);
                        break;
                    }
                }
                if (!hasMatch)
                {
                    string datadir = EXE_DIRECTORY + @"Data";
                    string path = Path.GetDirectoryName(file);
                    path = path.TrimEnd('\\', '/');
                    if (Utilities.IsSubfolder(datadir, path) || datadir == path)
                    {
                        Log.Warning("User file from data subdirectory (or deeper) is not allowed: " + file);
                        ShowStatus("Files not allowed to be added from Data folder or subdirectories", 5000);
                        continue;
                    }
                    string temppath = Path.GetTempPath();
                    temppath = temppath.TrimEnd('\\', '/');

                    if (Utilities.IsSubfolder(temppath, path) || temppath == path)
                    {
                        Log.Warning("User file from temp subdirectory is not allowed. Please extract files before attempting to add as a user file. File: " + file);
                        ShowStatus("Files not allowed to be added from Temp directory", 5000);
                        continue;
                    }
                    string extension = Path.GetExtension(file).ToLower();
                    switch (extension)
                    {
                        case ".7z":
                        case ".rar":
                        case ".zip":
                        case ".tpf":
                        case ".mem":
                        case ".mod":
                            if (acceptUserFiles)
                            {
                                acceptableUserFiles.Add(file);
                            }
                            break;
                        case ".dds":
                        case ".png":
                        case ".jpg":
                        case ".jpeg":
                        case ".tga":
                        case ".bmp":
                            string filename = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                            if (!filename.Contains("0x"))
                            {
                                Log.Error("Texture filename not valid: " + Path.GetFileName(file) + " Texture filename must include texture CRC (0xhhhhhhhh)");
                                continue;
                            }
                            int idx = filename.IndexOf("0x");
                            if (filename.Length - idx < 10)
                            {
                                Log.Error("Texture filename not valid: " + Path.GetFileName(file) + " Texture filename must include texture CRC (0xhhhhhhhh)");
                                continue;
                            }
                            uint crc;
                            string crcStr = filename.Substring(idx + 2, 8);
                            try
                            {
                                crc = uint.Parse(crcStr, System.Globalization.NumberStyles.HexNumber);
                            }
                            catch
                            {
                                Log.Error("Texture filename not valid: " + Path.GetFileName(file) + " Texture filename must include texture CRC (0xhhhhhhhh)");
                                continue;
                            }
                            //File has hash
                            acceptableUserFiles.Add(file);
                            break;
                        default:
                            Log.Information("Dragged file does not match any addon manifest file and is not acceptable extension: " + file);
                            noMatchFiles.Add(file);
                            break;
                    }
                }
            } //END LOOP
            if (filesToImport.Count == 0 && acceptableUserFiles.Count > 0)
            {
                PendingUserFiles = acceptableUserFiles;
                LoadUserFileSelection(PendingUserFiles[0]);
                WhatToBuildFlyout.IsOpen = false;
                UserTextures_Flyout.IsOpen = true;
            }

            string statusMessage = "";
            statusMessage += "Already imported: " + alreadyImportedFiles.Count;
            if (noMatchFiles.Count > 0)
            {
                statusMessage += " | ";
                statusMessage += "Not supported: " + noMatchFiles.Count;
            }

            if (badSizeFiles.Count > 0)
            {
                statusMessage += " | ";
                statusMessage += "Corrupt/Bad files: " + badSizeFiles.Count;
            }
            if (noMatchFiles.Count > 0 || alreadyImportedFiles.Count > 0 || badSizeFiles.Count > 0)
            {
                ShowStatus(statusMessage);
            }

            //if (noMatchFiles.Count == 0 && filesToImport.Count == 0 && acceptableUserFiles.Count == 0)
            //{
            //    ShowStatus("All dropped files are already imported");
            //}

            if (filesToImport.Count > 0)
            {
                ImportFiles(filesToImport, new List<string>(), null, 0, totalBytes);
            }
        }

        /// <summary>
        /// Imports files into the ALOT Installer library
        /// </summary>
        /// <param name="filesToImport">List of addon files that are being improted</param>
        /// <param name="importedFiles">Files that are currently imported</param>
        /// <param name="progressController">UI controller for the progress bar</param>
        /// <param name="processedBytes">How many bytes have been processed</param>
        /// <param name="totalBytes">How many bytes total will be processed</param>
        private async void ImportFiles(List<Tuple<AddonFile, string, string>> filesToImport, List<string> importedFiles, ProgressDialogController progressController, long processedBytes, long totalBytes)
        {
            PreventFileRefresh = true;
            string importingfrom = Path.GetPathRoot(filesToImport[0].Item2);
            string importingto = Path.GetPathRoot(DOWNLOADED_MODS_DIRECTORY);
            if (DOWNLOAD_ASSISTANT_WINDOW != null)
            {
                DOWNLOAD_ASSISTANT_WINDOW.ShowStatus("Importing...");
                DOWNLOAD_ASSISTANT_WINDOW.SetImportButtonEnabled(false);
            }

            if ((bool)Checkbox_MoveFilesAsImport.IsChecked && importingfrom == importingto)
            {
                ImportWorker = new BackgroundWorker();
                ImportWorker.DoWork += ImportFilesAsMove;
                ImportWorker.RunWorkerCompleted += ImportCompleted;
                ImportWorker.RunWorkerAsync(filesToImport);
            }
            else
            {
                Tuple<AddonFile, string, string> fileToImport = filesToImport[0];
                filesToImport.RemoveAt(0);
                //COPY
                if (progressController == null)
                {
                    MetroDialogSettings settings = new MetroDialogSettings();
                    progressController = await this.ShowProgressAsync("Importing files", "ALOT Installer is importing files, please wait...\nImporting " + fileToImport.Item1.FriendlyName, false, settings);
                    progressController.SetIndeterminate();
                    progressController.SetCancelable(true);
                }
                else
                {
                    progressController.SetMessage("ALOT Installer is importing files, please wait...\nImporting " + fileToImport.Item1.FriendlyName);
                    if (DOWNLOAD_ASSISTANT_WINDOW != null)
                    {
                        DOWNLOAD_ASSISTANT_WINDOW.ShowStatus("Importing " + importedFiles.Count + " file" + (importedFiles.Count != 1 ? "s" : ""));
                    }
                }
                WebClient downloadClient = new WebClient();
                long preDownloadStartBytes = processedBytes;
                downloadClient.DownloadProgressChanged += (s, e) =>
                {
                    long currentBytes = preDownloadStartBytes;
                    currentBytes += e.BytesReceived;
                    double progress = (((double)currentBytes / totalBytes));
                    int taskbarprogress = (int)((currentBytes * 100 / totalBytes));

                    TaskbarManager.Instance.SetProgressValue(taskbarprogress, 100);
                    progressController.SetProgress(progress);
                };
                downloadClient.DownloadFileCompleted += async (s, e) =>
                {
                    string destfile = fileToImport.Item3;
                    if (File.Exists(destfile))
                    {
                        TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress, this);
                        TaskbarManager.Instance.SetProgressValue(0, 0);
                        processedBytes += new System.IO.FileInfo(destfile).Length;
                        importedFiles.Add(fileToImport.Item1.FriendlyName);
                        if (filesToImport.Count > 0)
                        {
                            ImportFiles(filesToImport, importedFiles, progressController, processedBytes, totalBytes);
                        }
                        else
                        {
                            ShowCopyImportsFinishedMessage(progressController, importedFiles);
                        }
                    }
                    else
                    {
                        Log.Error("Destination file doesn't exist after file copy. This may need some more analysis to determine the exact cause.");
                        Log.Error("Destination file: " + destfile);
                        await this.ShowMessageAsync("File failed to import", "'" + fileToImport.Item1 + "' failed to import. The destination file does not exist:\n" + fileToImport.Item3 + ".\n\nThis may indicate a lack of disk space on the drive ALOT Installer is running from, or possibly other issues.");
                        if (importedFiles.Count > 0)
                        {
                            ShowCopyImportsFinishedMessage(progressController, importedFiles);
                        }
                        else
                        {
                            await progressController.CloseAsync();
                        }
                    }

                };
                TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal, this);
                TaskbarManager.Instance.SetProgressValue(0, 100);
                downloadClient.DownloadFileAsync(new Uri(fileToImport.Item2), fileToImport.Item3);
            }
        }

        private async void ShowCopyImportsFinishedMessage(ProgressDialogController progressController, List<string> importedFiles)
        {
            //imports finished
            await progressController.CloseAsync();
            if (WindowState == WindowState.Minimized)
            {
                //queue it
                foreach (string af in importedFiles)
                {
                    COPY_QUEUE.Add(af);
                }
            }
            else
            {
                string detailsMessage = "The following files were just imported to ALOT Installer:";
                foreach (string af in importedFiles)
                {
                    detailsMessage += "\n - " + af;
                }

                string originalTitle = importedFiles.Count + " file" + (importedFiles.Count != 1 ? "s" : "") + " imported";
                string originalMessage = importedFiles.Count + " file" + (importedFiles.Count != 1 ? "s have" : " has") + " been copied into the texture library.";

                ShowImportFinishedMessage(originalTitle, originalMessage, detailsMessage);
            }
            PreventFileRefresh = false; //allow refresh
            if (DOWNLOAD_ASSISTANT_WINDOW != null)
            {
                DOWNLOAD_ASSISTANT_WINDOW.ShowStatus(importedFiles.Count + " file" + (importedFiles.Count != 1 ? "s were" : " was") + " imported");
                DOWNLOAD_ASSISTANT_WINDOW.SetImportButtonEnabled(true);
            }
        }

        private void ImportFilesAsMove(object sender, DoWorkEventArgs e)
        {
            List<Tuple<AddonFile, string, string>> filesToImport = (List<Tuple<AddonFile, string, string>>)e.Argument;
            List<string> completedItems = new List<string>();
            List<string> failedItems = new List<string>();
            while (filesToImport.Count > 0)
            {
                Tuple<AddonFile, string, string> fileToImport = filesToImport[0];
                filesToImport.RemoveAt(0);
                Log.Information("Importing via move: " + fileToImport.Item2);
                if (File.Exists(fileToImport.Item3))
                {
                    File.Delete(fileToImport.Item3);
                }
                try
                {
                    File.Move(fileToImport.Item2, fileToImport.Item3);
                    Log.Information("Imported via move: " + fileToImport.Item2);
                    completedItems.Add(fileToImport.Item1.FriendlyName);
                }
                catch (IOException ex)
                {
                    Log.Error("Unable to move file " + fileToImport.Item2 + " due to IOException: " + ex.Message);
                    failedItems.Add(fileToImport.Item1.FriendlyName + ": " + ex.Message);
                }
            }
            e.Result = new Tuple<List<string>, List<string>>(completedItems, failedItems);
        }

        private async void ImportCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e != null)
            {
                if (e.Error != null)
                {
                    //An error has occured
                    Log.Error("Error moving files: " + e.Error.Message);
                }
                else
                if (e.Result != null)
                {
                    var result = (Tuple<List<string>, List<string>>)e.Result;
                    List<string> importedFiles = result.Item1;
                    List<string> failedFiles = result.Item2;

                    if (WindowState == WindowState.Minimized)
                    {
                        foreach (string af in importedFiles)
                        {
                            MOVE_QUEUE.Add(af);
                        }
                    }
                    else
                    {
                        //imports finished
                        if (failedFiles.Count > 0)
                        {
                            string detailsMessage = "The following files were unable to be imported into ALOT Installer.";
                            foreach (string af in failedFiles)
                            {
                                detailsMessage += "\n - " + af;
                            }
                            string originalTitle = failedFiles.Count + " file" + (failedFiles.Count != 1 ? "s" : "") + " failed to import";
                            await this.ShowMessageAsync(originalTitle, detailsMessage);
                        }
                        if (importedFiles.Count > 0)
                        {
                            string detailsMessage = "The following files were just imported to ALOT Installer. The files have been moved to the texture library.";
                            foreach (string af in importedFiles)
                            {
                                detailsMessage += "\n - " + af;
                            }
                            string originalTitle = importedFiles.Count + " file" + (importedFiles.Count != 1 ? "s" : "") + " imported";
                            string originalMessage = importedFiles.Count + " file" + (importedFiles.Count != 1 ? "s have" : " has") + " been moved into the texture library.";
                            ShowImportFinishedMessage(originalTitle, originalMessage, detailsMessage);
                        }
                    }
                    CheckImportLibrary_Tick(null, null);
                    PreventFileRefresh = false; //allow refresh

                    if (DOWNLOAD_ASSISTANT_WINDOW != null)
                    {
                        DOWNLOAD_ASSISTANT_WINDOW.ShowStatus(importedFiles.Count + " file" + (importedFiles.Count != 1 ? "s were" : " was") + " imported");
                    }
                }
                PreventFileRefresh = false;
            }
            if (DOWNLOAD_ASSISTANT_WINDOW != null)
            {
                DOWNLOAD_ASSISTANT_WINDOW.SetImportButtonEnabled(true);
            }
        }

        private async void ShowImportFinishedMessage(string originalTitle, string originalMessage, string detailsMessage)
        {
            MetroDialogSettings settings = new MetroDialogSettings();
            settings.NegativeButtonText = "OK";
            settings.AffirmativeButtonText = "Details";
            MessageDialogResult result = await this.ShowMessageAsync(originalTitle, originalMessage, MessageDialogStyle.AffirmativeAndNegative, settings);
            if (result == MessageDialogResult.Affirmative)
            {
                await this.ShowMessageAsync(originalTitle, detailsMessage);
            }
        }

        internal void ShowStatus(string message, int msOpen = 6000)
        {
            StatusFlyout.AutoCloseInterval = msOpen;
            StatusLabel.Text = message;
            StatusFlyout.IsOpen = true;
        }

        private void Button_Settings_Click(object sender, RoutedEventArgs e)
        {
            SettingsFlyout.IsOpen = true;
        }

        private async Task<bool> testWriteAccess()
        {
            try
            {
                using (var file = File.Create(EXE_DIRECTORY + "\\write_permissions_test")) { };
                File.Delete(EXE_DIRECTORY + "\\write_permissions_test");
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                Log.Error("The program cannot run in a directory that is write-protected.");
                await this.ShowMessageAsync("Running from write-protected directory", "Your user account doesn't have write permissions to the current directory. Move ALOT Installer to somewhere where yours does, like the Documents folder.");
                Environment.Exit(1);
                return false;
            }
            catch (Exception e)
            {
                //do nothing with other ones, I guess.
                Log.Error("Exception testing write permissions: " + e.Message);
                Log.Warning("We are continuing as if we have write permissions. It is possible we don't have any. Application will probably crash in these conditions.");
            }
            return true;
        }

        private async void Button_InstallME1_Click(object sender, RoutedEventArgs e)
        {
            if (await InstallPrecheck(1))
            {
                ShowBuildOptions(1);
            }
        }

        private void Button_ViewLog_Click(object sender, RoutedEventArgs e)
        {
            var directory = new DirectoryInfo("logs");
            FileInfo latestlogfile = directory.GetFiles("alotinstaller*.txt").OrderByDescending(f => f.LastWriteTime).First();
            if (latestlogfile != null)
            {
                ProcessStartInfo psi = new ProcessStartInfo(EXE_DIRECTORY + "logs\\" + latestlogfile.ToString());
                psi.UseShellExecute = true;
                Process.Start(psi);
            }
        }

        private void LoadSettings()
        {
            bool importasmove = Utilities.GetRegistrySettingBool(SETTINGSTR_IMPORTASMOVE) ?? true;
            Checkbox_MoveFilesAsImport.IsChecked = importasmove;

            DEBUG_LOGGING = Utilities.GetRegistrySettingBool(SETTINGSTR_DEBUGLOGGING) ?? false;
            Checkbox_DebugLogging.IsChecked = DEBUG_LOGGING;

            USING_BETA = Utilities.GetRegistrySettingBool(SETTINGSTR_BETAMODE) ?? false;
            Checkbox_BetaMode.IsChecked = USING_BETA;
            Analytics.TrackEvent("Session Type", new Dictionary<string, string>()
            {
                ["Type"] = USING_BETA ? "Beta" : "Stable"
            });

            LAST_BETA_ADVERT_TIME = DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(Utilities.GetRegistrySettingString(SETTINGSTR_LAST_BETA_ADVERT_TIME) ?? "0"));

            DONT_FORCE_UPGRADES = Utilities.GetRegistrySettingBool(SETTINGSTR_DONT_FORCE_UPGRADES) ?? false;

            DOWNLOADS_FOLDER = Utilities.GetRegistrySettingString(SETTINGSTR_DOWNLOADSFOLDER);
            if (DOWNLOADS_FOLDER == null)
            {
                DOWNLOADS_FOLDER = KnownFolders.GetPath(KnownFolder.Downloads);
            }

            //if (USING_BETA)
            //{
            string librarydir = Utilities.GetRegistrySettingString(SETTINGSTR_LIBRARYDIR);
            if (librarydir != null && Directory.Exists(librarydir))
            {
                DOWNLOADED_MODS_DIRECTORY = librarydir;
            }
            //}

            bool repack = Utilities.GetRegistrySettingBool(SETTINGSTR_REPACK) ?? false;
            Checkbox_RepackME2GameFiles.IsChecked = repack;

            bool repackme3 = Utilities.GetRegistrySettingBool(SETTINGSTR_REPACK_ME3) ?? false;
            Checkbox_RepackME3GameFiles.IsChecked = repackme3;
            if (USING_BETA)
            {
                ThemeManager.ChangeAppStyle(System.Windows.Application.Current,
                                                    ThemeManager.GetAccent("Crimson"),
                                                    ThemeManager.GetAppTheme("BaseDark")); // or appStyle.Item1
            }
        }

        private void Button_ReportIssue_Click(object sender, RoutedEventArgs e)
        {
            openWebPage(DISCORD_INVITE_LINK);
        }

        public static void openWebPage(string link)
        {
            try
            {
                Log.Information("Opening URL: " + link);
                System.Diagnostics.Process.Start(link);
            }
            catch (Exception other)
            {
                Log.Error("Exception opening browser - handled. The error was " + other.Message);
                System.Windows.Clipboard.SetText(link);
                //await this.ShowMessageAsync("Unable to open web browser", "Unable to open your default web browser. Open your browser and paste the link (already copied to clipboard) into your URL bar.");
            }
        }

        private void RestoreGame(int game)
        {
            BackupWorker = new BackgroundWorker();
            BackupWorker.DoWork += RestoreGame;
            BackupWorker.WorkerReportsProgress = true;
            BackupWorker.ProgressChanged += BackupWorker_ProgressChanged;
            BackupWorker.RunWorkerCompleted += RestoreCompleted;
            BACKUP_THREAD_GAME = game;
            SettingsFlyout.IsOpen = false;
            Button_Settings.IsEnabled = false;
            Button_DownloadAssistant.IsEnabled = false;
            PreventFileRefresh = true;
            HeaderLabel.Text = "Restoring Mass Effect" + (game == 1 ? "" : " " + game) + "...\nDo not close the application until this process completes.";
            Button_InstallME1.IsEnabled = Button_InstallME2.IsEnabled = Button_InstallME3.IsEnabled = Button_Settings.IsEnabled = false;
            TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal, this);
            TaskbarManager.Instance.SetProgressValue(0, 0);
            BackupWorker.RunWorkerAsync();
        }

        private async void RestoreCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress, this);
            Build_ProgressBar.IsIndeterminate = false;
            if (e.Result != null)
            {
                bool result = (bool)e.Result;
                if (result)
                {
                    AddonFilesLabel.Text = "Restore completed.";
                    await this.ShowMessageAsync("Restore completed", "Mass Effect" + GetGameNumberSuffix(BACKUP_THREAD_GAME) + " has been restored from backup.");
                }
                else
                {
                    AddonFilesLabel.Text = "Restore failed! Check the logs. Your game may be in an inconsistent or missing state.";
                }
                SetInstallButtonsAvailability();
                UpdateALOTStatus();

                foreach (AddonFile af in AllAddonFiles)
                {
                    if (af.ALOTVersion > 0 || af.ALOTUpdateVersion > 0)
                    {
                        af.ReadyStatusText = null; //fire property reset
                        af.SetIdle();
                    }
                }
            }
            else
            {
                AddonFilesLabel.Text = "Restore failed! Check the logs.";
                SetInstallButtonsAvailability();
                UpdateALOTStatus();
            }

            Button_Settings.IsEnabled = true;
            Button_DownloadAssistant.IsEnabled = true;
            PreventFileRefresh = false;

            BACKUP_THREAD_GAME = -1;
            HeaderLabel.Text = PRIMARY_HEADER;

            if (CURRENTLY_INSTALLED_ME1_ALOT_INFO != null && CURRENTLY_INSTALLED_ME1_ALOT_INFO.MEUITMVER == 0)
            {
                if (meuitmFile != null)
                {
                    int index = AllAddonFiles.IndexOf(meuitmFile);
                    if (index < 0)
                    {
                        //add back in
                        AllAddonFiles.Add(meuitmFile);
                        AllAddonFiles.ReplaceAll(AllAddonFiles.OrderBy(o => o.Author).ThenBy(x => x.FriendlyName).ToList());
                    }
                }
            }

            ApplyFiltering();
        }

        private async void Checkbox_BetaMode_Click(object sender, RoutedEventArgs e)
        {
            bool isEnabling = (bool)Checkbox_BetaMode.IsChecked;
            bool restart = true;
            if (isEnabling)
            {
                MessageDialogResult result = await this.ShowMessageAsync("Enabling BETA mode", "Enabling BETA mode will enable the beta manifest as well as beta features and beta updates. These builds are for testing, and may not be stable (and will sometimes outright crash). If you use this mode, we would appreciate if you gave feedback on the Discord.\nEnable BETA mode?", MessageDialogStyle.AffirmativeAndNegative);
                if (result == MessageDialogResult.Negative)
                {
                    Checkbox_BetaMode.IsChecked = false;
                    restart = false;
                }
            }
            Utilities.WriteRegistryKey(Registry.CurrentUser, REGISTRY_KEY, SETTINGSTR_BETAMODE, ((bool)Checkbox_BetaMode.IsChecked ? 1 : 0));
            USING_BETA = (bool)Checkbox_BetaMode.IsChecked;
            if (restart)
            {
                System.Windows.Forms.Application.Restart();
                Environment.Exit(0);
            }
        }

        private void Button_MEMVersion_Click(object sender, RoutedEventArgs e)
        {
            ShowStatus("Starting MassEffectModder.exe", 3000);
            SettingsFlyout.IsOpen = false;
            Utilities_Flyout.IsOpen = false;
            string ini = BINARY_DIRECTORY + "Installer.ini";
            if (File.Exists(ini))
            {
                File.Delete(ini);
            }

            string exe = BINARY_DIRECTORY + "MassEffectModder.exe";
            Utilities.runProcess(exe, "", true);
            Analytics.TrackEvent("Ran Mass Effect Modder GUI");

        }

        private void InstallingOverlayFlyout_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //Allow installing UI overlay to be window drag
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Button_InstallDone_Click(object sender, RoutedEventArgs e)
        {
            SetInstallFlyoutState(false);
        }

        private void Checkbox_MoveFilesAsImport_Click(object sender, RoutedEventArgs e)
        {
            bool settingVal = (bool)Checkbox_MoveFilesAsImport.IsChecked;
            Utilities.WriteRegistryKey(Registry.CurrentUser, REGISTRY_KEY, SETTINGSTR_IMPORTASMOVE, ((bool)Checkbox_MoveFilesAsImport.IsChecked ? 1 : 0));
        }

        private async void Window_Closing(object sender, CancelEventArgs e)
        {
            bool isClosing = true;
            if (WARN_USER_OF_EXIT)
            {
                Log.Information("User is attempting to close program while installer is running.");
                e.Cancel = true;

                MetroDialogSettings mds = new MetroDialogSettings();
                mds.AffirmativeButtonText = "Yes";
                mds.NegativeButtonText = "No";
                mds.DefaultButtonFocus = MessageDialogResult.Negative;

                MessageDialogResult result = await this.ShowMessageAsync("Closing ALOT Installer may leave game in a broken state", "MEM is currently installing textures. Closing the program will likely leave your game in an unplayable, broken state. Are you sure you want to exit?", MessageDialogStyle.AffirmativeAndNegative, mds);
                if (result == MessageDialogResult.Affirmative)
                {
                    Log.Error("User has chosen to kill MEM and close program. Game will likely be broken.");
                    WARN_USER_OF_EXIT = false;

                    Close();
                }
                else
                {
                    isClosing = false;
                }
            }

            if (isClosing)
            {
                if (DOWNLOAD_ASSISTANT_WINDOW != null)
                {
                    DOWNLOAD_ASSISTANT_WINDOW.SHUTTING_DOWN = true;
                    DOWNLOAD_ASSISTANT_WINDOW.Close();
                }

                if (BuildWorker.IsBusy || InstallWorker.IsBusy)
                {
                    //We should add indicator that we closed while busy
                    Utilities.WriteRegistryKey(Registry.CurrentUser, REGISTRY_KEY, "CheckOutputDirectoriesOnManifestLoad", true);
                }
            }

        }

        private void InstallingOverlayoutFlyout_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (this.WindowState == System.Windows.WindowState.Normal)
            {
                this.WindowState = System.Windows.WindowState.Maximized;
            }
            else
            {
                this.WindowState = System.Windows.WindowState.Normal;
            }
        }

        private void ShowFirstTime(object sender, RoutedEventArgs e)
        {
            FirstRunFlyout.IsOpen = true;
        }

        private async void Button_FirstTimeRunDismiss_Click(object sender, RoutedEventArgs e)
        {
            bool? hasShownFirstRun = Utilities.GetRegistrySettingBool("HasRunFirstRun");

            Utilities.WriteRegistryKey(Registry.CurrentUser, REGISTRY_KEY, "HasRunFirstRun", true);
            FirstRunFlyout.IsOpen = false;
            SettingsFlyout.IsOpen = true;
            PerformPostStartup();
            if (hasShownFirstRun == null || !(bool)hasShownFirstRun)
            {
                bool me1backedup = Utilities.GetGameBackupPath(1) != null;
                bool me2backedup = Utilities.GetGameBackupPath(1) != null;
                bool me3backedup = Utilities.GetGameBackupPath(1) != null;

                if (!(me1backedup || me2backedup || me3backedup))
                {
                    //no games backed up
                    await this.ShowMessageAsync("Backup games before installing ALOT", "It is strongly recommended you make a game backup before installing ALOT. By doing so, you have an easy way to restore the game in the event something goes wrong, and ALOT Installer will handle the extra work required to fully uninstall ALOT. The ALOT team recommends a backup of an unmodified game; you can backup and restore from the settings menu.");
                }
            }
        }

        private void Button_ManualFileME1_Click(object sender, RoutedEventArgs e)
        {
            AddUserFileAndQueue(1);
        }

        private void AddUserFileAndQueue(int game)
        {
            string file = PendingUserFiles[0];
            AddonFile af = new AddonFile();
            switch (game)
            {
                case 1:
                    af.Game_ME1 = true;
                    break;
                case 2:
                    af.Game_ME2 = true;
                    break;
                case 3:
                    af.Game_ME3 = true;
                    break;
            }
            af.ChoiceFiles = new List<ChoiceFile>();
            af.Enabled = true;
            af.UserFile = true;
            af.DownloadLink = "http://example.com";
            af.Author = "User Supplied Files (ME" + game + ")";
            af.FriendlyName = Path.GetFileNameWithoutExtension(file);
            af.Filename = Path.GetFileName(file);

            af.UserFilePath = file;
            AllAddonFiles.Add(af);
            QueueNextUserFile();
        }

        private void QueueNextUserFile()
        {
            PendingUserFiles.RemoveAt(0);
            RefreshListOnUserImportClose = true;
            if (PendingUserFiles.Count <= 0)
            {
                UserTextures_Flyout.IsOpen = false;
                //prevent double click
                Button_ManualFileME1.IsEnabled = Button_ManualFileME2.IsEnabled = Button_ManualFileME3.IsEnabled = false;
            }
            else
            {
                LoadUserFileSelection(PendingUserFiles[0]);
            }
        }

        private async void LoadUserFileSelection(string v)
        {
            string extension = Path.GetExtension(v.ToLower());
            if (extension == ".zip" || extension == ".rar" || extension == ".7z")
            {
                List<string> files = await Utilities.GetArchiveFileListing(v);
                if (ArchiveHasValidFiles(files))
                {
                    UserTextures_Title.Text = "Select which game " + Path.GetFileName(v) + " applies to";
                    Panel_UserTexturesSelectGame.Visibility = Visibility.Visible;
                    Panel_UserTexturesBadArchive.Visibility = Visibility.Collapsed;

                }
                else
                {
                    //archive is not acceptable
                    Log.Information("This file is not usable, it contains no acceptable files in it.");
                    UserFileNotAcceptable(v);
                    return;
                }
            }
            UserTextures_Title.Text = "Select which game " + Path.GetFileName(v) + " applies to";
            Panel_UserTexturesSelectGame.Visibility = Visibility.Visible;
            Panel_UserTexturesBadArchive.Visibility = Visibility.Collapsed;
        }

        private void UserFileNotAcceptable(string v)
        {
            UserTextures_Title.Text = Path.GetFileName(v) + " is not a usable file";
            Panel_UserTexturesSelectGame.Visibility = Visibility.Collapsed;
            Panel_UserTexturesBadArchive.Visibility = Visibility.Visible;
        }

        private bool ArchiveHasValidFiles(List<string> files)
        {
            bool hasAtLeastOnceAcceptableFile = false;
            foreach (string file in files)
            {
                string extension = Path.GetExtension(file).ToLower();
                switch (extension)
                {
                    case ".tpf":
                    case ".mem":
                    case ".mod":
                        //acceptable
                        hasAtLeastOnceAcceptableFile = true;
                        break;
                    case ".dds":
                    case ".png":
                    case ".jpg":
                    case ".jpeg":
                    case ".tga":
                    case ".bmp":
                        string filename = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                        if (!filename.Contains("0x"))
                        {
                            Log.Error("Texture filename in archive not valid: " + Path.GetFileName(file) + " Texture filename must include texture CRC (0xhhhhhhhh)");
                            return false;
                        }
                        int idx = filename.IndexOf("0x");
                        if (filename.Length - idx < 10)
                        {
                            Log.Error("Texture filename in archive not valid: " + Path.GetFileName(file) + " Texture filename must include texture CRC (0xhhhhhhhh)");
                            return false;
                        }
                        uint crc;
                        string crcStr = filename.Substring(idx + 2, 8);
                        try
                        {
                            crc = uint.Parse(crcStr, System.Globalization.NumberStyles.HexNumber);
                        }
                        catch
                        {
                            Log.Error("Texture filename in archive not valid: " + Path.GetFileName(file) + " Texture filename must include texture CRC (0xhhhhhhhh)");
                            return false;
                        }
                        hasAtLeastOnceAcceptableFile = true;
                        break;
                    default:
                        //we don't care
                        break;
                }
            }
            return hasAtLeastOnceAcceptableFile;
        }

        private void UserTextures_Flyout_IsOpenChanged(object sender, RoutedEventArgs e)
        {
            if (UserTextures_Flyout.IsOpen == false)
            {
                if (RefreshListOnUserImportClose)
                {
                    ApplyFiltering(true);
                    RefreshListOnUserImportClose = false;
                    PendingUserFiles.Clear();
                }
            }
            else
            {
                Button_ManualFileME1.IsEnabled = Switch_ME1Filter.IsEnabled; //Hack, but we can use this to determine if ME1 is able to be filtered to
                Button_ManualFileME2.IsEnabled = Switch_ME2Filter.IsEnabled; //Hack, but we can use this to determine if ME2 is able to be filtered to 
                Button_ManualFileME3.IsEnabled = Switch_ME3Filter.IsEnabled; //Hack, but we can use this to determine if ME3 is able to be filtered to
                // Fading animation for the textblock to show that the userfiles text
                UserTextures_ManifestFileFlashing.BeginAnimation(TextBlock.OpacityProperty, userfileGameSelectoroFlashingTextAnimation);
            }
        }

        private void Button_ManualFileME3_Click(object sender, RoutedEventArgs e)
        {
            AddUserFileAndQueue(3);

        }

        private void Button_ManualFileME2_Click(object sender, RoutedEventArgs e)
        {
            AddUserFileAndQueue(2);
        }

        public void ImportFromDownloadsFolder()
        {
            if (Directory.Exists(DOWNLOADS_FOLDER))
            {
                Log.Information("Looking for files to import from: " + DOWNLOADS_FOLDER);
                List<string> filelist = new List<string>();
                List<AddonFile> addonFilesNotReady = new List<AddonFile>();
                foreach (AddonFile af in AllAddonFiles)
                {
                    if (!af.Ready)
                    {
                        addonFilesNotReady.Add(af);
                    }
                }
                Log.Information("Number of files not ready: " + addonFilesNotReady.Count);
                string[] files = Directory.GetFiles(DOWNLOADS_FOLDER);
                var filesImporting = new List<AddonFile>(); //used to prevent duplicate imports when dealing with (1) and %20
                foreach (string file in files)
                {
                    string fname = Path.GetFileName(file); //we do not check duplicates with (1) etc
                                                           //remove (1) and such
                    string fnameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                    if (fnameWithoutExtension.EndsWith(")"))
                    {
                        if (fnameWithoutExtension.LastIndexOf("(") >= fnameWithoutExtension.Length - 3)
                        {
                            //it's probably a copy
                            fname = fnameWithoutExtension.Remove(fnameWithoutExtension.LastIndexOf("("), fnameWithoutExtension.LastIndexOf(")") - fnameWithoutExtension.LastIndexOf("(") + 1).Trim() + Path.GetExtension(file);
                            Log.Information("Import from downloads folder filename corrected to " + fname);
                        }
                    }

                    foreach (AddonFile af in addonFilesNotReady)
                    {
                        if (!filesImporting.Contains(af) && ((af.TorrentFilename != null && fname == af.TorrentFilename) || fname == af.Filename || fname.Replace("%20", " ") == af.Filename)) //MSEdge %20 bug
                        {
                            filelist.Add(file);
                            filesImporting.Add(af);
                            break;
                        }
                    }
                }
                SettingsFlyout.IsOpen = false;
                if (filelist.Count > 0)
                {
                    Log.Information("Found this many files to import from downloads folder:" + filelist.Count);
                    Analytics.TrackEvent("Imported files with Download Assistant");

                    PerformImportOperation(filelist.ToArray(), false);
                }
                else
                {
                    if (DOWNLOAD_ASSISTANT_WINDOW != null)
                    {
                        DOWNLOAD_ASSISTANT_WINDOW.ShowStatus("No files found for importing");
                    }
                    Log.Information("Did not find any files for importing in: " + DOWNLOADS_FOLDER);
                    ShowStatus("No files found for importing in " + DOWNLOADS_FOLDER);
                }
            }
            else
            {
                Log.Information("Downloads folder does not exist: " + DOWNLOADS_FOLDER);
            }
        }

        private async void Button_BuildAndInstall_Click(object sender, RoutedEventArgs e)
        {
            bool oneOptionChecked = false;
            foreach (System.Windows.Controls.CheckBox cb in buildOptionCheckboxes)
            {
                if ((bool)cb.IsChecked)
                {
                    oneOptionChecked = true;
                    break;
                }
            }
            Button_BuildAndInstall.IsEnabled = false;
            WhatToBuildFlyout.IsOpen = false;
            if (oneOptionChecked)
            {
                if (CURRENT_GAME_BUILD > 0 && CURRENT_GAME_BUILD < 4)
                {
                    if (await InitBuild(CURRENT_GAME_BUILD))
                    {

                        Loading = true; //prevent refresh when filtering
                        ShowME1Files = CURRENT_GAME_BUILD == 1;
                        ShowME2Files = CURRENT_GAME_BUILD == 2;
                        ShowME3Files = CURRENT_GAME_BUILD == 3;
                        Loading = false;

                        switch (CURRENT_GAME_BUILD)
                        {
                            case 1:
                                Button_InstallME1.Content = "Building...";
                                break;
                            case 2:
                                Button_InstallME2.Content = "Building...";
                                break;
                            case 3:
                                Button_InstallME3.Content = "Building...";
                                break;
                        }
                        BUILD_ALOT = Checkbox_BuildOptionALOT.IsChecked.Value;
                        BUILD_ADDON_FILES = Checkbox_BuildOptionAddon.IsChecked.Value;
                        BUILD_USER_FILES = Checkbox_BuildOptionUser.IsChecked.Value;
                        BUILD_ALOT_UPDATE = Checkbox_BuildOptionALOTUpdate.IsChecked.Value;
                        BUILD_MEUITM = Checkbox_BuildOptionMEUITM.IsChecked.Value;
                        TELEMETRY_ALL_ADDON_FILES &= BUILD_ADDON_FILES;

                        TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal, this);
                        BuildWorker.RunWorkerAsync(CURRENT_GAME_BUILD);
                    }
                    else
                    {
                        ShowReadyFilesOnly = false;
                        ApplyFiltering();
                        CURRENT_GAME_BUILD = 0;
                        Log.Warning("Install was aborted due to initinstall returning false");
                    }
                }
                else
                {
                    ShowReadyFilesOnly = false;
                    ApplyFiltering();
                    CURRENT_GAME_BUILD = 0;
                }
            }
        }

        private void Button_BuildAndInstallCancel_Click(object sender, RoutedEventArgs e)
        {

            ShowReadyFilesOnly = false;
            PreventFileRefresh = false;
            ApplyFiltering();
            WhatToBuildFlyout.IsOpen = false;
            CURRENT_GAME_BUILD = 0;
        }

        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            ((Expander)sender).BringIntoView();
        }

        private async void Button_UploadLog_Click(object sender, RoutedEventArgs e)
        {
            LogSelectorWindow lsw = new LogSelectorWindow(this);
            lsw.Owner = this;
            lsw.ShowDialog();
            string log = lsw.GetSelectedLogText();
            if (log != null)
            {
                await uploadLatestLog(false, log);
            }
        }

        public async Task<string> uploadLatestLog(bool isPreviousCrashLog, string log, bool openPageWhenFinished = true)
        {
            Log.Information("Preparing to upload installer log");

            string alotInstallerVer = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();

            if (log == null)
            {
                //latest

                var directory = new DirectoryInfo("logs");
                var logfiles = directory.GetFiles("alotinstaller*.txt").OrderByDescending(f => f.LastWriteTime).ToList();
                if (logfiles.Count() > 0)
                {
                    var currentTime = DateTime.Now;
                    log = "";
                    if (currentTime.Date != bootTime.Date && logfiles.Count() > 1)
                    {
                        //we need to do last 2 files
                        Log.Information("Log file has rolled over since app was booted - including previous days' log.");
                        File.Copy(logfiles.ElementAt(1).FullName, logfiles.ElementAt(1).FullName + ".tmp");
                        log = File.ReadAllText(logfiles.ElementAt(1).FullName + ".tmp");
                        File.Delete(logfiles.ElementAt(1).FullName + ".tmp");
                        log += "\n";
                    }
                    Log.Information("Staging log file for upload. This is the final log item that should appear in an uploaded log.");
                    File.Copy(logfiles.ElementAt(0).FullName, logfiles.ElementAt(0).FullName + ".tmp");
                    log += File.ReadAllText(logfiles.ElementAt(0).FullName + ".tmp");
                    File.Delete(logfiles.ElementAt(0).FullName + ".tmp");
                }
                else
                {
                    Log.Information("No logs available, somehow. Canceling upload");
                }
            }
            string zipStaged = EXE_DIRECTORY + "logs\\logfile_forUpload";
            File.WriteAllText(zipStaged, log);

            //Compress with LZMA for VPS Upload
            string outfile = "logfile_forUpload.lzma";
            string args = "e \"" + zipStaged + "\" \"" + outfile + "\" -mt2";
            Utilities.runProcess(BINARY_DIRECTORY + "lzma.exe", args);
            File.Delete(zipStaged);
            var lzmalog = File.ReadAllBytes(outfile);
            Analytics.TrackEvent("Uploaded log");
            ProgressDialogController progresscontroller = await this.ShowProgressAsync("Uploading log", "Log is currently uploading, please wait...", true);
            progresscontroller.SetIndeterminate();
            try
            {
                var responseString = await "https://me3tweaks.com/alot/logupload.php".PostUrlEncodedAsync(new { LogData = Convert.ToBase64String(lzmalog), ALOTInstallerVersion = alotInstallerVer, Type = "log", CrashLog = isPreviousCrashLog }).ReceiveString();
                File.Delete(outfile);
                Uri uriResult;
                bool result = Uri.TryCreate(responseString, UriKind.Absolute, out uriResult)
                    && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
                if (result)
                {
                    //should be valid URL.
                    //diagnosticsWorker.ReportProgress(0, new ThreadCommand(SET_DIAGTASK_ICON_GREEN, Image_Upload));
                    //e.Result = responseString;
                    await progresscontroller.CloseAsync();
                    Log.Information("Result from server for log upload: " + responseString);
                    if (openPageWhenFinished)
                    {
                        openWebPage(responseString);
                    }
                    SettingsFlyout.IsOpen = false;
                    return responseString;
                }
                else
                {
                    File.Delete(outfile);

                    //diagnosticsWorker.ReportProgress(0, new ThreadCommand(SET_DIAG_TEXT, "Error from oversized log uploader: " + responseString));
                    //diagnosticsWorker.ReportProgress(0, new ThreadCommand(SET_DIAGTASK_ICON_RED, Image_Upload));
                    await progresscontroller.CloseAsync();
                    Log.Error("Error uploading log. The server responded with: " + responseString);
                    //e.Result = "Diagnostic complete.";
                    await this.ShowMessageAsync("Log upload error", "The server rejected the upload. The response was: " + responseString);
                    //Utilities.OpenAndSelectFileInExplorer(diagfilename);
                }
            }
            catch (FlurlHttpTimeoutException)
            {
                // FlurlHttpTimeoutException derives from FlurlHttpException; catch here only
                // if you want to handle timeouts as a special case
                await progresscontroller.CloseAsync();
                Log.Error("Request timed out while uploading log.");
                await this.ShowMessageAsync("Log upload timed out", "The log took too long to upload. You will need to upload your log manually.");

            }
            catch (Exception ex)
            {
                // ex.Message contains rich details, inclulding the URL, verb, response status,
                // and request and response bodies (if available)
                await progresscontroller.CloseAsync();
                Log.Error("Handled error uploading log: " + App.FlattenException(ex));
                string exmessage = ex.Message;
                var index = exmessage.IndexOf("Request body:");
                if (index > 0)
                {
                    exmessage = exmessage.Substring(0, index);
                }
                await this.ShowMessageAsync("Log upload failed", "The log was unable to upload. The error message is: " + exmessage + "You will need to upload your log manually.");
            }
            SettingsFlyout.IsOpen = false;
            File.Delete(outfile);
            return "";
        }


        private void Button_OpenAddonAssistantWindow_Click(object sender, RoutedEventArgs e)
        {
            if (!Utilities.IsWindowOpen<AddonDownloadAssistant>())
            {
                List<AddonFile> notReadyAddonFiles = new List<AddonFile>();
                foreach (AddonFile af in DisplayedAddonFiles)
                {
                    if (!af.Ready && !af.UserFile)
                    {
                        notReadyAddonFiles.Add(af);
                    }
                }
                if (notReadyAddonFiles.Count > 0)
                {

                    DOWNLOAD_ASSISTANT_WINDOW = new AddonDownloadAssistant(this, notReadyAddonFiles);
                    DOWNLOAD_ASSISTANT_WINDOW.Show();
                }
                else
                {
                    if (ShowME1Files && ShowME2Files && ShowME3Files)
                    {
                        ShowStatus("All files are already imported", 3000);
                    }
                    else
                    {
                        ShowStatus("All files with this filter are already imported", 3000);
                    }
                }
            }
        }

        private void DownloadAssisant_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!Button_DownloadAssistant.IsEnabled)
            {
                if (DOWNLOAD_ASSISTANT_WINDOW != null)
                {
                    DOWNLOAD_ASSISTANT_WINDOW.SHUTTING_DOWN = false;
                    DOWNLOAD_ASSISTANT_WINDOW.Close();
                }
            }
        }

        private void Button_GenerateDiagnostics_Click(object sender, RoutedEventArgs e)
        {
            SettingsFlyout.IsOpen = false;
            DiagnosticsWindow dw = new DiagnosticsWindow();
            dw.Owner = this;
            dw.ShowDialog();
        }

        private void Checkbox_RepackME2Files_Click(object sender, RoutedEventArgs e)
        {
            Utilities.WriteRegistryKey(Registry.CurrentUser, REGISTRY_KEY, SETTINGSTR_REPACK, ((bool)Checkbox_RepackME2GameFiles.IsChecked ? 1 : 0));
        }

        private void Checkbox_RepackME3Files_Click(object sender, RoutedEventArgs e)
        {
            Utilities.WriteRegistryKey(Registry.CurrentUser, REGISTRY_KEY, SETTINGSTR_REPACK_ME3, ((bool)Checkbox_RepackME3GameFiles.IsChecked ? 1 : 0));
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            switch (this.WindowState)
            {
                case WindowState.Maximized:
                case WindowState.Normal:
                    if (COPY_QUEUE.Count > 0)
                    {
                        string detailsMessage = "The following files were just imported to ALOT Installer:";
                        foreach (string af in COPY_QUEUE)
                        {
                            detailsMessage += "\n - " + af;
                        }

                        string originalTitle = COPY_QUEUE.Count + " file" + (COPY_QUEUE.Count != 1 ? "s" : "") + " imported";
                        string originalMessage = COPY_QUEUE.Count + " file" + (COPY_QUEUE.Count != 1 ? "s have" : " has") + " been copied into the texture library.";

                        ShowImportFinishedMessage(originalTitle, originalMessage, detailsMessage);
                        COPY_QUEUE.Clear();
                    }
                    if (MOVE_QUEUE.Count > 0)
                    {
                        string detailsMessage = "The following files were just imported to ALOT Installer. The files have been moved to the texture library.";
                        foreach (string af in MOVE_QUEUE)
                        {
                            detailsMessage += "\n - " + af;
                        }
                        string originalTitle = MOVE_QUEUE.Count + " file" + (MOVE_QUEUE.Count != 1 ? "s" : "") + " imported";
                        string originalMessage = MOVE_QUEUE.Count + " file" + (MOVE_QUEUE.Count != 1 ? "s have" : " has") + " been moved into the texture library.";
                        ShowImportFinishedMessage(originalTitle, originalMessage, detailsMessage);
                        MOVE_QUEUE.Clear();
                    }
                    break;
            }
        }

        private void OriginWarning_Button_Click(object sender, RoutedEventArgs e)
        {
            OriginWarningFlyout.IsOpen = false;
        }

        private void ListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var rowIndex = ListView_Files.SelectedIndex;
            var row = (System.Windows.Controls.ListViewItem)ListView_Files.ItemContainerGenerator.ContainerFromIndex(rowIndex);
            System.Windows.Controls.ContextMenu cm = row.ContextMenu;
            AddonFile af = (AddonFile)row.DataContext;

            //Reset
            foreach (System.Windows.Controls.MenuItem mi in cm.Items)
            {
                mi.Visibility = Visibility.Visible;
            }

            int i = 0;
            while (i < cm.Items.Count)
            {
                System.Windows.Controls.MenuItem mi = (System.Windows.Controls.MenuItem)cm.Items[i];
                switch (i)
                {
                    case 0: //Visit download
                        if (af.UserFile)
                        {
                            mi.Visibility = Visibility.Collapsed;
                        }
                        break;
                    case 1:
                        if (!af.Ready || PreventFileRefresh)
                        {
                            mi.Visibility = Visibility.Collapsed;
                        }
                        break;
                    case 2: //Toggle on/off
                        if (af.ALOTVersion > 0 || af.ALOTUpdateVersion > 0 || !af.Ready || PreventFileRefresh || (af.MEUITM && MEUITM_INSTALLER_MODE))
                        {
                            mi.Visibility = Visibility.Collapsed;
                            break;
                        }
                        if (af.Enabled)
                        {
                            mi.Header = "Disable file";
                            mi.ToolTip = "Click to disable file for this session.\nThis file will not be processed when staging files for installation.";
                            mi.ToolTip = "Prevents this file from being used for installation";
                        }
                        else
                        {
                            mi.Header = "Enable file";
                            mi.ToolTip = "Allows this file to be used for installation";
                        }
                        break;
                    case 3: //Remove user file
                        mi.Visibility = af.UserFile ? Visibility.Visible : Visibility.Collapsed;
                        break;
                }
                i++;
            }
        }

        private void ContextMenu_OpenDownloadPage(object sender, RoutedEventArgs e)
        {
            var rowIndex = ListView_Files.SelectedIndex;
            var row = (System.Windows.Controls.ListViewItem)ListView_Files.ItemContainerGenerator.ContainerFromIndex(rowIndex);
            AddonFile af = (AddonFile)row.DataContext;
            openWebPage(af.DownloadLink);
        }

        private void ContextMenu_ToggleFile(object sender, RoutedEventArgs e)
        {
            var rowIndex = ListView_Files.SelectedIndex;
            var row = (System.Windows.Controls.ListViewItem)ListView_Files.ItemContainerGenerator.ContainerFromIndex(rowIndex);
            AddonFile af = (AddonFile)row.DataContext;

            if (af.Ready)
            {
                af.Enabled = !af.Enabled;
                if (!af.Enabled)
                {
                    af.ReadyStatusText = "Disabled";
                }
                else
                {
                    af.ReadyStatusText = null;
                }
            }
        }

        private void ContextMenu_ViewFile(object sender, RoutedEventArgs e)
        {
            var rowIndex = ListView_Files.SelectedIndex;
            var row = (System.Windows.Controls.ListViewItem)ListView_Files.ItemContainerGenerator.ContainerFromIndex(rowIndex);
            AddonFile af = (AddonFile)row.DataContext;
            if (af.Ready)
            {
                Utilities.OpenAndSelectFileInExplorer(af.GetFile());
            }
        }

        private void ContextMenu_RemoveFile(object sender, RoutedEventArgs e)
        {
            var rowIndex = ListView_Files.SelectedIndex;
            var row = (System.Windows.Controls.ListViewItem)ListView_Files.ItemContainerGenerator.ContainerFromIndex(rowIndex);
            AddonFile af = (AddonFile)row.DataContext;
            if (af.UserFile)
            {
                AllAddonFiles.Remove(af);
                ApplyFiltering();
            }
        }

        private async void CleanupDownloadedMods(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(DOWNLOADED_MODS_DIRECTORY))
            {
                Log.Information("Determining files that are no longer relevant...");
                var files = new DirectoryInfo(DOWNLOADED_MODS_DIRECTORY).GetFiles().Select(o => o.Name).ToList();
                string list = "";

                SettingsFlyout.IsOpen = false;
                foreach (AddonFile af in AllAddonFiles)
                {
                    if (!af.UserFile && File.Exists(af.GetFile()))
                    {
                        string name = af.GetFile();
                        if (name != null)
                        { //crash may occur in some extreme cases
                            name = Path.GetFileName(name);
                        }
                        Utilities.WriteDebugLog("File is still relevant: " + name + ", part of " + af.FriendlyName);
                        files.Remove(name); //remove manifest file from list of files to remove.
                    }
                }

                foreach (string file in files)
                {
                    list += "\n - " + file;
                    Log.Information(" - File no longer relevant: " + file);
                }

                if (files.Count > 0)
                {
                    MetroDialogSettings mds = new MetroDialogSettings();
                    mds.AffirmativeButtonText = "Delete";
                    mds.NegativeButtonText = "Keep";
                    mds.DefaultButtonFocus = MessageDialogResult.Affirmative;
                    MessageDialogResult mdr = await this.ShowMessageAsync("Found outdated files", "The following files in the texture library are no longer listed in the manifest and can be safely deleted: " + list, MessageDialogStyle.AffirmativeAndNegative, mds);
                    if (mdr == MessageDialogResult.Affirmative)
                    {
                        Log.Information("User elected to delete outdated files.");
                        int numDeleted = 0;
                        foreach (string file in files)
                        {
                            Log.Information("Deleting " + file);
                            try
                            {
                                File.Delete(DOWNLOADED_MODS_DIRECTORY + "\\" + file);
                                numDeleted++;
                            }
                            catch (Exception ex)
                            {
                                Log.Error("Error deleting file: " + file);
                                Log.Error(App.FlattenException(ex));
                            }
                        }
                        string message = "Deleted " + numDeleted + " file" + (numDeleted != 1 ? "s" : "");
                        if (numDeleted != files.Count)
                        {
                            message += ". Some files could not be deleted, see installer log.";
                        }
                        ShowStatus(message);
                    }
                }
                else
                {
                    Log.Information("No outdated files found.");
                    ShowStatus("No outdated files were found", 4000);
                }
            }
        }

        private async void Button_ConfigureMod_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.ListViewItem lvi = FindAncestor<System.Windows.Controls.ListViewItem>(((FrameworkElement)sender));
            ListView_Files.SelectedIndex = ListView_Files.Items.IndexOf(lvi.DataContext);
            AddonFile af = (AddonFile)lvi.DataContext;

            ModConfigurationDialog mcd = new ModConfigurationDialog(af, this, false);
            await this.ShowMetroDialogAsync(mcd);
        }

        public static T FindAncestor<T>(DependencyObject dependencyObject) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(dependencyObject);

            if (parent == null) return null;

            var parentT = parent as T;
            return parentT ?? FindAncestor<T>(parent);
        }

        private async void CloseCustomDialog(object sender, RoutedEventArgs e)
        {
            var dialog = (BaseMetroDialog)this.Resources["Dialog_ModConfiguration"];

            await this.HideMetroDialogAsync(dialog);
        }

        private void Button_UserTexturesBadFile_Click(object sender, RoutedEventArgs e)
        {
            QueueNextUserFile();
        }

        private void Checkbox_DebugLogging_Click(object sender, RoutedEventArgs e)
        {
            Utilities.WriteRegistryKey(Registry.CurrentUser, REGISTRY_KEY, SETTINGSTR_DEBUGLOGGING, (Checkbox_DebugLogging.IsChecked.Value ? 1 : 0));
            DEBUG_LOGGING = Checkbox_DebugLogging.IsChecked.Value;
            Log.Information("Debug logging is being turned " + (DEBUG_LOGGING ? "on" : "off"));
        }

        private void ListView_Files_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListView_Files.SelectedItem != null)
            {
                ListView_Files.ScrollIntoView(ListView_Files.SelectedItem);
            }
        }

        private void ListView_Files_Loaded(object sender, RoutedEventArgs e)
        {
            if (ListView_Files.SelectedItem != null)
            {
                ListView_Files.ScrollIntoView(ListView_Files.SelectedItem);
            }
        }

        private void SwitchToALOTMode_Button_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Exiting MEUITM mode.");
            MEUITM_INSTALLER_MODE = false;
            SetUIMode();
        }

        private void Button_Utilities_Click(object sender, RoutedEventArgs e)
        {
            Utilities_Flyout.IsOpen = true;
            SettingsFlyout.IsOpen = false;
        }

        private void Button_ME3AutoTOC_Click(object sender, RoutedEventArgs e)
        {
            ShowStatus("Performing AutoTOC on Mass Effect 3...", 3000);
            Analytics.TrackEvent("Ran autotoc for ME3");

            SettingsFlyout.IsOpen = false;
            Utilities_Flyout.IsOpen = false;
            string exe = BINARY_DIRECTORY + "FullAutoTOC.exe";
            ConsoleApp ca = new ConsoleApp(exe, "\"" + Utilities.GetGamePath(3) + "\"");
            ca.ConsoleOutput += (o, args2) =>
            {
                if (args2.Line != null && args2.Line != "")
                {
                    Log.Information("FullAutoTOC output: " + args2.Line);
                }
            };
            ca.Exited += (o, args2) =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    ShowStatus("AutoTOC complete", 2000);
                });
            };
            ca.Run();
        }

        private void Button_VerifyME1_Click(object sender, RoutedEventArgs e)
        {
            VerifyGame(1);
        }

        private void Button_VerifyME2_Click(object sender, RoutedEventArgs e)
        {
            VerifyGame(2);
        }

        private void Button_VerifyME3_Click(object sender, RoutedEventArgs e)
        {
            VerifyGame(3);
        }

        private void VerifyGame(int v)
        {
            if (PreventFileRefresh)
            {
                return; //double click.
            }
            Button_InstallME1.IsEnabled = Button_InstallME2.IsEnabled = Button_InstallME3.IsEnabled = Button_Settings.IsEnabled = Button_DownloadAssistant.IsEnabled = false;
            BackupWorker = new BackgroundWorker();
            BackupWorker.DoWork += verifyGame;
            BackupWorker.WorkerReportsProgress = true;
            BackupWorker.ProgressChanged += BackupWorker_ProgressChanged;
            BackupWorker.RunWorkerCompleted += VerifyCompleted;
            BACKUP_THREAD_GAME = v;
            SettingsFlyout.IsOpen = false;
            Utilities_Flyout.IsOpen = false;
            PreventFileRefresh = true;
            BackupWorker.RunWorkerAsync();
        }

        public void verifyGame(object sender, DoWorkEventArgs e)
        {

            Log.Information("Verifying game: Mass Effect " + BACKUP_THREAD_GAME);
            string exe = BINARY_DIRECTORY + MEM_EXE_NAME;
            string args = "--check-game-data-vanilla --gameid " + BACKUP_THREAD_GAME + " --ipc";
            List<string> acceptedIPC = new List<string>();
            acceptedIPC.Add("TASK_PROGRESS");
            acceptedIPC.Add("ERROR");
            BackupWorker.ReportProgress(completed, new ThreadCommand(UPDATE_ADDONUI_CURRENTTASK, "Verifying game data..."));

            runMEM_BackupAndBuild(exe, args, BackupWorker, acceptedIPC);
            while (BACKGROUND_MEM_PROCESS.State == AppState.Running)
            {
                Thread.Sleep(250);
            }
            int backupVerifyResult = BACKGROUND_MEM_PROCESS.ExitCode ?? 1;
            if (backupVerifyResult != 0)
            {
                string modified = "";
                string gameDir = Utilities.GetGamePath(BACKUP_THREAD_GAME);
                foreach (String error in BACKGROUND_MEM_PROCESS_ERRORS)
                {
                    modified += "\n - " + error;
                    //.Remove(0, gameDir.Length + 1);
                }
                Log.Warning("Game verification failed.");
                string message = "Mass Effect" + GetGameNumberSuffix(BACKUP_THREAD_GAME) + " has files that do not match what is in the MEM database.\nThe files are listed below." + modified;
                e.Result = new KeyValuePair<string, string>("Game is modified", message);
                //Thread resumes
            }
            else
            {
                Log.Information("Game verification passed - no issues.");
                string message = "Mass Effect" + GetGameNumberSuffix(BACKUP_THREAD_GAME) + " has passed the vanilla game check.";
                e.Result = new KeyValuePair<string, string>("Game appears unmodified", message);
            }
        }

        private async void VerifyCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress, this);
            var helper = new FlashWindowHelper(System.Windows.Application.Current);
            AddonFilesLabel.Text = "Verification has completed";
            // Flashes the window and taskbar 5 times and stays solid 
            // colored until user focuses the main window
            helper.FlashApplicationWindow();
            if (e.Result != null)
            {
                KeyValuePair<string, string> result = (KeyValuePair<string, string>)e.Result;
                await this.ShowMessageAsync(result.Key, result.Value);
            }
            PreventFileRefresh = false;
            SetInstallButtonsAvailability();
            Button_Settings.IsEnabled = Button_DownloadAssistant.IsEnabled = true;
            RefreshesUntilRealRefresh = 3;
        }

        private void Button_CloseUtilities(object sender, RoutedEventArgs e)
        {
            Utilities_Flyout.IsOpen = false;
        }

        private void ShowSetLibraryDir(object sender, RoutedEventArgs e)
        {
            SettingsFlyout.IsOpen = false;
            var openFolder = new CommonOpenFileDialog();
            openFolder.IsFolderPicker = true;
            openFolder.Title = "Select library location";
            openFolder.AllowNonFileSystemItems = false;
            openFolder.EnsurePathExists = true;
            if (openFolder.ShowDialog() != CommonFileDialogResult.Ok)
            {
                return;
            }
            DOWNLOADED_MODS_DIRECTORY = openFolder.FileName;
            Utilities.WriteRegistryKey(Registry.CurrentUser, REGISTRY_KEY, SETTINGSTR_LIBRARYDIR, openFolder.FileName);
            foreach (AddonFile af in AllAddonFiles)
            {
                af.Ready = false;
            }
            Button_LibraryDir.ToolTip = "Click to change texture library directory.\nIf path is not found at app startup, the default subdirectory of Downloaded_Mods will be used.\n\nLibrary location currently is:\n" + DOWNLOADED_MODS_DIRECTORY;
            ShowStatus("Updated library directory - please wait while files refresh...", 4000);
        }
    }
}