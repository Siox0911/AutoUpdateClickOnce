using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Web;
using System.Windows;
using System.Xml.Linq;

namespace AutoUpdateClickOnce
{
    /// <summary>
    /// <para>
    /// Works only with .Net 8.0 and higher (do not confuse with C# 8). 
    /// The downside is that it happens during the running process and an important task
    /// might not be executed anymore. So you have to decide when to call the methods 
    /// <see cref="CheckForUpdateAsync(CancellationToken)"/> respectively the synchronous 
    /// <see cref="CheckForUpdate(CancellationToken)"/> method and <see cref="Update()"/>.
    /// </para>
    /// <para>
    /// Example of usage. Here, the update is performed without any prompt or user interaction:
    /// <code>
    /// if(ApplicationDeployment.IsNetworkDeployed)
    /// {
    ///     var appDeployment = ApplicationDeployment.CurrentDeployment;
    ///     await appDeployment.CheckForUpdateAsync();
    ///     if(appDeployment.IsUpdateAvailable)
    ///     {
    ///         //Save and export settings
    ///         SaveSettings(); //SaveSettings is not a part of the ApplicationDeployment or ExportHelper class
    ///         var settingsExportHelper = new SettingsExportHelper(Properties.Settings.Default, Properties.Settings.Default.SettingsFile);
    ///         //true means that is human readable (well formated)
    ///         settingsExportHelper.Export(true);
    ///         
    ///         //Update the application
    ///         appDeployment.Update();
    ///     }
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// <term>Interesting Links</term>
    /// <description>
    /// The following links are very helpful, but I have changed the ApplicationDeployment.cs class.
    /// So that an update is actually performed.
    /// </description>
    /// <list type="bullet">
    /// <item>
    /// <term>Access ClickOnce deployment properties for .NET on Windows (en)</term>
    /// <description><see href="https://learn.microsoft.com/en-us/visualstudio/deployment/access-clickonce-deployment-properties-dotnet?view=vs-2022"/></description>
    /// </item>
    /// <item><term>Access ClickOnce activation data for .NET on Windows (en)</term>
    /// <description><see href="https://learn.microsoft.com/en-us/visualstudio/deployment/access-clickonce-activation-data-dotnet?view=vs-2022"/></description>
    /// </item>
    /// <item><term>ApplicationDeployment.cs Source</term>
    /// <description><see href="https://github.com/dotnet/deployment-tools/blob/main/docs/dotnet-mage/ApplicationDeployment.cs"/></description>
    /// </item> 
    /// <item><term>ClickOnce for .NET on Windows</term>
    /// <description><see href="https://learn.microsoft.com/en-us/visualstudio/deployment/clickonce-deployment-dotnet?view=vs-2022"/></description>
    /// </item>
    /// </list>
    /// </para>
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "We want some properties to be accessible only when an instance exists. This makes it more tangible, even if they don't actually require an instance.")]
    public class ApplicationDeployment
    {
        #region private fields

        private Version? availableUpdateVersion; //Available version to update to

        private static ApplicationDeployment? currentDeployment = null; //Current deployment instance
        private static bool currentDeploymentInitialized = false; //Has the current deployment instance been initialized

        private static bool isNetworkDeployed = false; //Is deployed on the network
        private static bool isNetworkDeployedInitialized = false; //Has isNetworkDeployed been initialized
        private bool isUpdateAvailable; //Is an update available

        private IEnumerable<string>? activationData; //Activation data
        private bool activationDataInitialized = false; //Has the activation data been initialized

        //Timersupprt for event OnUpdateAvailable
        internal System.Timers.Timer? updateCheckTimer; //Timer for the update check

        #endregion private fields

        #region Events

        /// <summary>
        /// Event that is triggered when an update is available. This event will be triggered only once. To restart the update check, call <see cref="RestartUpdateCheck"/>.
        /// </summary>
        public event EventHandler<EventArgument.UpdateEventArgs>? UpdateAvailable;

        #endregion Events

        #region public static Properties

        /// <summary>
        /// Is deployed on the network
        /// </summary>
        public static bool IsNetworkDeployed
        {
            get
            {
                if (!isNetworkDeployedInitialized)
                {
                    _ = bool.TryParse(Environment.GetEnvironmentVariable("ClickOnce_IsNetworkDeployed"), out isNetworkDeployed);
                    isNetworkDeployedInitialized = true;
                }

                return isNetworkDeployed;
            }
        }

        /// <summary>
        /// Returns the current deployment instance
        /// </summary>
        public static ApplicationDeployment? CurrentDeployment
        {
            get
            {
                if (!currentDeploymentInitialized)
                {
                    currentDeployment = IsNetworkDeployed ? new ApplicationDeployment() : null;
                    currentDeploymentInitialized = true;

                    currentDeployment?.GenerateAndStartUpdateTimer();
                }

                return currentDeployment;
            }
        }

        #endregion public static Properties

        #region public Properties

        /// <summary>
        /// Returns the activation URI
        /// </summary>
        public Uri? ActivationUri
        {
            get
            {
                _ = Uri.TryCreate(Environment.GetEnvironmentVariable("ClickOnce_ActivationUri"), UriKind.Absolute, out Uri? val);
                return val;
            }
        }

        /// <summary>
        /// Returns the current application version
        /// </summary>
        public Version? CurrentVersion
        {
            get
            {
                _ = Version.TryParse(Environment.GetEnvironmentVariable("ClickOnce_CurrentVersion"), out Version? val);
                return val;
            }
        }

        /// <summary>
        /// Returns the data directory. This is not the directory with the settings alias user.config.
        /// </summary>
        public string? DataDirectory
        {
            get { return Environment.GetEnvironmentVariable("ClickOnce_DataDirectory"); }
        }

        /// <summary>
        /// Indicates if the app was started for the first time after an update
        /// </summary>
        public bool IsFirstRun
        {
            get
            {
                _ = bool.TryParse(Environment.GetEnvironmentVariable("ClickOnce_IsFirstRun"), out bool val);
                return val;
            }
        }

        /// <summary>
        /// Returns the time of the last update check
        /// </summary>
        public DateTime TimeOfLastUpdateCheck
        {
            get
            {
                _ = DateTime.TryParse(Environment.GetEnvironmentVariable("ClickOnce_TimeOfLastUpdateCheck"), out DateTime value);
                return value;
            }
        }

        /// <summary>
        /// Returns the name of the updated application
        /// </summary>
        public string? UpdatedApplicationFullName
        {
            get
            {
                return Environment.GetEnvironmentVariable("ClickOnce_UpdatedApplicationFullName");
            }
        }

        /// <summary>
        /// The version to which it was updated
        /// </summary>
        public Version? UpdatedVersion
        {
            get
            {
                _ = Version.TryParse(Environment.GetEnvironmentVariable("ClickOnce_UpdatedVersion"), out Version? val);
                return val;
            }
        }

        /// <summary>
        /// The available version to update to
        /// </summary>
        public Version? AvailableUpdateVersion
        {
            get
            {
                return availableUpdateVersion;
            }
        }

        /// <summary>
        /// This is the update URL specified in the application configuration. So the URL with the file alias application.application.
        /// </summary>
        public Uri? UpdateLocation
        {
            get
            {
                _ = Uri.TryCreate(Environment.GetEnvironmentVariable("ClickOnce_UpdateLocation"), UriKind.Absolute, out Uri? val);
                return val;
            }
        }

        /// <summary>
        /// Indicates if the update URL is a UNC address
        /// </summary>
        public bool IsUNCUpdateLocation
        {
            get
            {
                if (!string.IsNullOrEmpty(UpdateLocation?.ToString()))
                {
                    //TODO: try this out, check framework version
                    //AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName?.StartsWith("netcoreapp");

                    return UpdateLocation.ToString().ToLower().StartsWith("file://");
                }

                return false;
            }
        }

        /// <summary>
        /// Indicates if the update URL is a web address
        /// </summary>
        public bool IsWebUpdateLocation
        {
            get
            {
                if (!string.IsNullOrEmpty(UpdateLocation?.ToString()))
                {
                    return UpdateLocation.ToString().ToLower().StartsWith("http://") || UpdateLocation.ToString().ToLower().StartsWith("https://");
                }

                return false;
            }
        }

        /// <summary>
        /// Is <see langword="true"/> if an update is available
        /// </summary>
        public bool IsUpdateAvailable
        {
            get
            {
                return isUpdateAvailable;
            }
        }

        /// <summary>
        /// Returns the launcher version. Depends on the .NET Framework version. And I think also on the Visual Studio version.
        /// </summary>
        public Version? LauncherVersion
        {
            get
            {
                _ = Version.TryParse(Environment.GetEnvironmentVariable("ClickOnce_LauncherVersion"), out Version? val);
                return val;
            }
        }

        /// <summary>
        /// Returns the activation data. This is the data that is passed to the application when it is started.
        /// </summary>
        public IEnumerable<string>? ActivationData
        {
            get
            {
                if (!activationDataInitialized)
                {
                    activationData = GetActivationData() ?? [];
                    activationDataInitialized = true;
                }
                return activationData;
            }
        }

        #endregion public Properties

        #region private Constructors

        /// <summary>
        /// Constructor; called only internally
        /// </summary>
        private ApplicationDeployment()
        {
            // As an alternative solution, we could initialize all properties here
        }

        #endregion private Constructors

        #region public Methods

        /// <summary>
        /// Starts the update by opening the <see cref="UpdateLocation"/>. The running application is immediately terminated without saving settings.
        /// A web address is not yet supported and will throw a <see cref="NotImplementedException"/>.
        /// </summary>
        /// <exception cref="NotImplementedException">If the update is to be performed via a web address</exception>
        /// <exception cref="Exception"></exception>
        public void Update()
        {
            try
            {
                //If it is a UNC address, we start the file and terminate the running application
                if (IsUNCUpdateLocation)
                {
                    if (!string.IsNullOrEmpty(UpdateLocation?.ToString()) && IsUpdateAvailable)
                    {
                        var process = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                UseShellExecute = true,
                                CreateNoWindow = true,
                                WindowStyle = ProcessWindowStyle.Hidden,
                                //RedirectStandardInput = true,
                                RedirectStandardOutput = false,
                                FileName = UpdateLocation.LocalPath,
                            }
                        };

                        process.Start();

                        if (process != null)
                        {
                            //Terminate own process
                            Process.GetCurrentProcess().Kill();
                        }
                    }
                }

                //Not yet implemented
                if (IsWebUpdateLocation)
                {
                    throw new NotImplementedException();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Checks if an update is available
        /// </summary>
        /// <returns></returns>
        /// <exception cref="OperationCanceledException">If the operation was canceled</exception>
        /// <exception cref="Exception"></exception>
        public async Task<bool> CheckForUpdateAsync(CancellationToken token = default)
        {
            try
            {
                //UpdateLocation must be set
                if (!string.IsNullOrEmpty(UpdateLocation?.ToString()))
                {
                    //If it is a UNC address, we open the file and read the version
                    if (IsUNCUpdateLocation)
                    {
                        using (var stream = File.OpenRead(UpdateLocation.LocalPath))
                        {
                            return await CheckVersionAsync(stream, token).ConfigureAwait(false);
                        }
                    }

                    //If it is a web address, we open the file and read the version
                    if (IsWebUpdateLocation)
                    {
                        using (var client = new HttpClient())
                        {
                            using (var stream = await client.GetStreamAsync(ActivationUri, token))
                            {
                                return await CheckVersionAsync(stream, token).ConfigureAwait(false);
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }

            return false;
        }

        /// <summary>
        /// Checks if an update is available
        /// </summary>
        /// <returns></returns>
        /// <exception cref="OperationCanceledException">If the operation was canceled</exception>
        /// <exception cref="Exception"></exception>
        public bool CheckForUpdate(CancellationToken token = default)
        {
            try
            {
                //Redirect the synchronous variant to the asynchronous variant
                return CheckForUpdateAsync(token).Result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Restarts the update check timer, after the event <see cref="UpdateAvailable"/> has been triggered.
        /// </summary>
        public void RestartUpdateCheck()
        {
            updateCheckTimer?.Start();
        }

        #endregion public Methods

        #region private Methods

        /// <summary>
        /// Reads the version from the Manifest 
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task<bool> CheckVersionAsync(Stream stream, CancellationToken token = default)
        {
            //Read the version
            var version = await ReadServerManifestAsync(stream, token);
            //If the version is greater, then an update is available
            if (version != null && version > CurrentVersion)
            {
                isUpdateAvailable = true;
                return true;
            }
            isUpdateAvailable = false;
            return false;
        }

        /// <summary>
        /// Loads the manifest file and reads the version. The manifest file is the file alias application.application.
        /// Derived from: <see href="https://github.com/derskythe/WpfSettings/blob/master/PureManApplicationDevelopment/PureManClickOnce.cs"/>
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="OperationCanceledException">If the operation was canceled</exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="OverflowException"></exception>
        /// <exception cref="FormatException"></exception>
        /// <exception cref="Exception"></exception>
        private async Task<Version?> ReadServerManifestAsync(Stream stream, CancellationToken token = default)
        {
            //We have the stream, so it should be readable
            var xmlDoc = await XDocument.LoadAsync(stream, LoadOptions.None, token);

            //Notify that an exception should be thrown if canceled
            token.ThrowIfCancellationRequested();

            //Get the namespace for the assemblyIdentity
            XNamespace nsSys = "urn:schemas-microsoft-com:asm.v1";

            //Search all descendants for the assemblyIdentity and take the first one or null
            var xmlElement = xmlDoc.Descendants(nsSys + "assemblyIdentity").FirstOrDefault();

            //Check here and throw an exception if necessary
            if (xmlElement == null)
            {
                throw new Exception($"Invalid manifest document for {UpdateLocation}");
            }

            //Read the version
            var version = xmlElement?.Attribute("version")?.Value;

            //Check here and throw an exception if necessary
            if (string.IsNullOrEmpty(version))
            {
                throw new Exception($"Version info is empty! {UpdateLocation}");
            }
            //Otherwise, parse the version and return it
            else
            {
                try
                {
                    availableUpdateVersion = new Version(version);
                    return availableUpdateVersion;
                }
                catch (ArgumentOutOfRangeException)
                {
                    throw;
                }
                catch (ArgumentNullException)
                {
                    throw;
                }
                catch (ArgumentException)
                {
                    throw;
                }
                catch (OverflowException)
                {
                    throw;
                }
                catch (FormatException)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Gets the activation data. This is the data that is passed to the application when it is started.
        /// </summary>
        /// <returns>A list with the activation data</returns>
        private List<string> GetActivationData()
        {
            var counter = Environment.GetEnvironmentVariable("ClickOnce_ActivationData_Count");
            var result = new List<string>();
            if (!string.IsNullOrEmpty(counter))
            {
                if (int.TryParse(counter, out int intCounter))
                {
                    for (int i = 0; i < intCounter; i++)
                    {
                        var data = Environment.GetEnvironmentVariable($"ClickOnce_ActivationData_{i}");
                        if (!string.IsNullOrEmpty(data))
                        {
                            result.Add(data);
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Generates and starts the update timer. The timer is set to 60 seconds.
        /// </summary>
        private void GenerateAndStartUpdateTimer()
        {
            updateCheckTimer = new System.Timers.Timer(1000 * 60);
            updateCheckTimer.Elapsed += async (sender, e) => await UpdateCheckTimerElapsedAsync();
            updateCheckTimer.AutoReset = true;
            updateCheckTimer.Enabled = true;
            updateCheckTimer.Start();
        }

        /// <summary>
        /// Checks for an update. This will trigger the event <see cref="UpdateAvailable"/> if an update is available.
        /// </summary>
        /// <returns></returns>
        private async Task UpdateCheckTimerElapsedAsync()
        {
            if (CurrentDeployment != null)
            {
                _ = await CurrentDeployment.CheckForUpdateAsync();
                if (CurrentDeployment.IsUpdateAvailable)
                {
                    updateCheckTimer?.Stop();
                    var eventArgs = new EventArgument.UpdateEventArgs(
                                        CurrentDeployment,
                                        CurrentDeployment.CurrentVersion?.ToString() ?? string.Empty,
                                        CurrentDeployment.AvailableUpdateVersion?.ToString() ?? string.Empty
                                        );
                    CurrentDeployment.UpdateAvailable?.Invoke(CurrentDeployment, eventArgs);
                }
            }
        }

        /// <summary>
        /// Opens the URL and returns the process instance. Currently not used
        /// Thanks to: <see href="https://github.com/derskythe/WpfSettings/blob/master/PureManApplicationDevelopment/PureManClickOnce.cs"/>
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static Process? OpenUrl(string url)
        {
            try
            {
                //TODO: Could this work with the web? ActivationUri?
                var info = new ProcessStartInfo(url)
                {
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = false,
                    UseShellExecute = false
                };
                var proc = Process.Start(info);
                return proc;
            }
            catch
            {
                //From .Net 7.0 we end up here if we want to open a UNC address
                url = HttpUtility.UrlEncode(url);
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                url = url.Replace("&", "^&");
                //Should replace any e.g. spaces and other characters in the URL
                return Process.Start(
                    new ProcessStartInfo("cmd", $"/c start {url}")
                    {
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = false,
                        UseShellExecute = false,
                    }
                );
            }
        }

        #endregion private Methods
    }
}
