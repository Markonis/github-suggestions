namespace WebUIService
{
    using System.Fabric;
    using System.Fabric.Description;

    public class ConfigSettings
    {
        private const string GIT_HUB_SECTION = "GitHub";
        private const string CLIENT_ID = "ClientId";
        private const string CLIENT_SECRET = "ClientSecret";
        private const string REDIRECT_URI = "RedirectUri";

        public string GitHubClientId { get; private set; }
        public string GitHubClientSecret { get; private set; }
        public string GitHubRedirectUri { get; private set; }

        public ConfigSettings(StatelessServiceContext context)
        {
            context.CodePackageActivationContext.ConfigurationPackageModifiedEvent += this.CodePackageActivationContext_ConfigurationPackageModifiedEvent;
            this.UpdateConfigSettings(context.CodePackageActivationContext.GetConfigurationPackageObject("Config").Settings);
        }

        private void CodePackageActivationContext_ConfigurationPackageModifiedEvent(object sender, PackageModifiedEventArgs<ConfigurationPackage> e)
        {
            this.UpdateConfigSettings(e.NewPackage.Settings);
        }

        private void UpdateConfigSettings(ConfigurationSettings settings)
        {
            ConfigurationSection section = settings.Sections[GIT_HUB_SECTION];
            GitHubClientId = section.Parameters[CLIENT_ID].Value;
            GitHubClientSecret = section.Parameters[CLIENT_SECRET].Value;
            GitHubRedirectUri = section.Parameters[REDIRECT_URI].Value;
        }
    }
}
