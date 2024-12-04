using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoUpdateClickOnce.EventArgument
{
    /// <summary>
    /// Event arguments for the update event
    /// </summary>
    public class UpdateEventArgs : EventArgs
    {
        /// <summary>
        /// The application deployment object
        /// </summary>
        public ApplicationDeployment ApplicationDeployment { get; }

        /// <summary>
        /// The current version of the application
        /// </summary>
        public string CurrentVersion { get; }

        /// <summary>
        /// The new version of the application
        /// </summary>
        public string UpdateVersion { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="applicationDeployment">The application deployment</param>
        /// <param name="currentVersion">The current version</param>
        /// <param name="updateVersion">The update version</param>
        public UpdateEventArgs(ApplicationDeployment applicationDeployment, string currentVersion, string updateVersion)
        {
            ApplicationDeployment = applicationDeployment;
            CurrentVersion = currentVersion;
            UpdateVersion = updateVersion;
        }
    }
}
