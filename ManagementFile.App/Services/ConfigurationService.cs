using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ManagementFile.App.Services
{
    /// <summary>
    /// Service for configuration management and environment-specific settings
    /// Phase 5 Week 15 - Production Readiness & Final Polish
    /// </summary>
    public sealed class ConfigurationService : INotifyPropertyChanged
    {
        #region Fields

        private readonly Dictionary<string, string> _configurationValues;
        private readonly Dictionary<string, bool> _featureFlags;
        private string _environment;
        private string _applicationName;
        private string _applicationVersion;
        private string _configurationFile;
        private DateTime _lastConfigUpdate;
        private bool _disposed;

        private const string DEFAULT_ENVIRONMENT = "Development";
        private const string DEFAULT_APP_NAME = "ManagementFile";
        private const string CONFIG_FILE_NAME = "app.config";

        #endregion

        #region Constructor

        public ConfigurationService()
        {
            _configurationValues = new Dictionary<string, string>();
            _featureFlags = new Dictionary<string, bool>();
            
            InitializeConfiguration();
            LoadConfiguration();
            InitializeFeatureFlags();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Current environment (Development, Staging, Production)
        /// </summary>
        public string Environment
        {
            get => _environment ?? DEFAULT_ENVIRONMENT;
            private set => SetProperty(ref _environment, value);
        }

        /// <summary>
        /// Application name
        /// </summary>
        public string ApplicationName
        {
            get => _applicationName ?? DEFAULT_APP_NAME;
            private set => SetProperty(ref _applicationName, value);
        }

        /// <summary>
        /// Application version
        /// </summary>
        public string ApplicationVersion
        {
            get => _applicationVersion;
            private set => SetProperty(ref _applicationVersion, value);
        }

        /// <summary>
        /// Configuration file path
        /// </summary>
        public string ConfigurationFile
        {
            get => _configurationFile;
            private set => SetProperty(ref _configurationFile, value);
        }

        /// <summary>
        /// Last configuration update time
        /// </summary>
        public DateTime LastConfigUpdate
        {
            get => _lastConfigUpdate;
            private set => SetProperty(ref _lastConfigUpdate, value);
        }

        /// <summary>
        /// Is development environment
        /// </summary>
        public bool IsDevelopment => Environment.Equals("Development", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Is staging environment
        /// </summary>
        public bool IsStaging => Environment.Equals("Staging", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Is production environment
        /// </summary>
        public bool IsProduction => Environment.Equals("Production", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// All configuration values
        /// </summary>
        public IReadOnlyDictionary<string, string> ConfigurationValues => _configurationValues;

        /// <summary>
        /// All feature flags
        /// </summary>
        public IReadOnlyDictionary<string, bool> FeatureFlags => _featureFlags;

        #endregion

        #region UI Helper Properties

        /// <summary>
        /// Environment display text
        /// </summary>
        public string EnvironmentDisplayText => $"Environment: {Environment}";

        /// <summary>
        /// Application info text
        /// </summary>
        public string ApplicationInfoText => $"{ApplicationName} v{ApplicationVersion}";

        /// <summary>
        /// Configuration status text
        /// </summary>
        public string ConfigurationStatusText => $"Config loaded: {LastConfigUpdate:yyyy-MM-dd HH:mm:ss}";

        /// <summary>
        /// Environment color for UI
        /// </summary>
        public string EnvironmentColor
        {
            get
            {
                if (IsProduction) return "Red";
                if (IsStaging) return "Orange";
                return "Green";
            }
        }

        /// <summary>
        /// Feature flags count text
        /// </summary>
        public string FeatureFlagsText => $"{_featureFlags.Count} feature flags configured";

        #endregion

        #region Configuration Methods

        /// <summary>
        /// Initialize configuration
        /// </summary>
        private void InitializeConfiguration()
        {
            try
            {
                // Set application version
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                ApplicationVersion = assembly.GetName().Version?.ToString() ?? "1.0.0.0";
                
                // Set configuration file path
                ConfigurationFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_FILE_NAME);
                
                // Determine environment
                Environment = GetEnvironmentFromConfig();
                
                LastConfigUpdate = DateTime.Now;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing configuration: {ex.Message}");
                Environment = DEFAULT_ENVIRONMENT;
                ApplicationVersion = "1.0.0.0";
            }
        }

        /// <summary>
        /// Get environment from configuration
        /// </summary>
        private string GetEnvironmentFromConfig()
        {
            try
            {
                // Try to get from app.config
                var envSetting = ConfigurationManager.AppSettings["Environment"];
                if (!string.IsNullOrEmpty(envSetting))
                    return envSetting;

                // Try to get from environment variables
                var envVariable = System.Environment.GetEnvironmentVariable("MANAGEMENTFILE_ENVIRONMENT");
                if (!string.IsNullOrEmpty(envVariable))
                    return envVariable;

                // Check for debug configuration
                #if DEBUG
                return "Development";
                #else
                return "Production";
                #endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting environment: {ex.Message}");
                return DEFAULT_ENVIRONMENT;
            }
        }

        /// <summary>
        /// Load configuration from various sources
        /// </summary>
        private void LoadConfiguration()
        {
            try
            {
                // Clear existing configuration
                _configurationValues.Clear();

                // Load from app.config
                LoadFromAppConfig();

                // Load environment-specific configuration
                LoadEnvironmentSpecificConfig();

                // Load from environment variables
                LoadFromEnvironmentVariables();

                LastConfigUpdate = DateTime.Now;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading configuration: {ex.Message}");
            }
        }

        /// <summary>
        /// Load from app.config file
        /// </summary>
        private void LoadFromAppConfig()
        {
            try
            {
                foreach (string key in ConfigurationManager.AppSettings.AllKeys)
                {
                    _configurationValues[key] = ConfigurationManager.AppSettings[key];
                }

                // Load connection strings
                foreach (ConnectionStringSettings connString in ConfigurationManager.ConnectionStrings)
                {
                    _configurationValues[$"ConnectionString_{connString.Name}"] = connString.ConnectionString;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading app.config: {ex.Message}");
            }
        }

        /// <summary>
        /// Load environment-specific configuration
        /// </summary>
        private void LoadEnvironmentSpecificConfig()
        {
            var environmentConfigFile = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, 
                $"app.{Environment.ToLower()}.config");

            if (File.Exists(environmentConfigFile))
            {
                try
                {
                    // This would typically load from environment-specific config files
                    // For now, we'll set some default values based on environment
                    SetEnvironmentDefaults();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading environment config: {ex.Message}");
                }
            }
            else
            {
                SetEnvironmentDefaults();
            }
        }

        /// <summary>
        /// Set environment-specific default values
        /// </summary>
        private void SetEnvironmentDefaults()
        {
            if (IsProduction)
            {
                _configurationValues["ApiTimeout"] = "30000";
                _configurationValues["LogLevel"] = "Warning";
                _configurationValues["EnableDebugMode"] = "false";
                _configurationValues["CacheExpiration"] = "3600";
                _configurationValues["MaxRetryAttempts"] = "3";
            }
            else if (IsStaging)
            {
                _configurationValues["ApiTimeout"] = "45000";
                _configurationValues["LogLevel"] = "Information";
                _configurationValues["EnableDebugMode"] = "true";
                _configurationValues["CacheExpiration"] = "1800";
                _configurationValues["MaxRetryAttempts"] = "5";
            }
            else // Development
            {
                _configurationValues["ApiTimeout"] = "60000";
                _configurationValues["LogLevel"] = "Debug";
                _configurationValues["EnableDebugMode"] = "true";
                _configurationValues["CacheExpiration"] = "300";
                _configurationValues["MaxRetryAttempts"] = "10";
            }

            // Common settings
            _configurationValues["ApplicationName"] = ApplicationName;
            _configurationValues["ApplicationVersion"] = ApplicationVersion;
            _configurationValues["Environment"] = Environment;
        }

        /// <summary>
        /// Load from environment variables
        /// </summary>
        private void LoadFromEnvironmentVariables()
        {
            try
            {
                // Load ManagementFile-specific environment variables
                var envVars = System.Environment.GetEnvironmentVariables();
                foreach (var key in envVars.Keys)
                {
                    var keyStr = key.ToString();
                    if (keyStr.StartsWith("MANAGEMENTFILE_", StringComparison.OrdinalIgnoreCase))
                    {
                        var configKey = keyStr.Substring(15); // Remove "MANAGEMENTFILE_" prefix
                        _configurationValues[configKey] = envVars[key].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading environment variables: {ex.Message}");
            }
        }

        /// <summary>
        /// Initialize feature flags
        /// </summary>
        private void InitializeFeatureFlags()
        {
            try
            {
                // Default feature flags
                _featureFlags["EnableAdvancedSearch"] = !IsProduction;
                _featureFlags["EnableBulkOperations"] = true;
                _featureFlags["EnableKeyboardShortcuts"] = true;
                _featureFlags["EnablePerformanceMonitoring"] = true;
                _featureFlags["EnableReporting"] = true;
                _featureFlags["EnableNotifications"] = true;
                _featureFlags["EnableFileManagement"] = true;
                _featureFlags["EnableTimeTracking"] = true;
                _featureFlags["EnableUserManagement"] = true;
                _featureFlags["EnableProjectManagement"] = true;

                // Development-only features
                _featureFlags["EnableDebugMode"] = IsDevelopment;
                _featureFlags["EnableMockData"] = IsDevelopment;
                _featureFlags["EnableVerboseLogging"] = IsDevelopment;

                // Production features
                _featureFlags["EnableSecurity"] = IsProduction;
                _featureFlags["EnableAuditLogging"] = IsProduction;
                _featureFlags["EnablePerformanceOptimization"] = IsProduction;

                // Load feature flags from configuration
                foreach (var config in _configurationValues)
                {
                    if (config.Key.StartsWith("Feature_", StringComparison.OrdinalIgnoreCase))
                    {
                        var featureName = config.Key.Substring(8); // Remove "Feature_" prefix
                        if (bool.TryParse(config.Value, out bool isEnabled))
                        {
                            _featureFlags[featureName] = isEnabled;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing feature flags: {ex.Message}");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Get configuration value by key
        /// </summary>
        public string GetConfigValue(string key, string defaultValue = "")
        {
            return _configurationValues.ContainsKey(key) ? _configurationValues[key] : defaultValue;
        }

        /// <summary>
        /// Get configuration value as integer
        /// </summary>
        public int GetConfigValueAsInt(string key, int defaultValue = 0)
        {
            var value = GetConfigValue(key);
            return int.TryParse(value, out int result) ? result : defaultValue;
        }

        /// <summary>
        /// Get configuration value as boolean
        /// </summary>
        public bool GetConfigValueAsBool(string key, bool defaultValue = false)
        {
            var value = GetConfigValue(key);
            return bool.TryParse(value, out bool result) ? result : defaultValue;
        }

        /// <summary>
        /// Get configuration value as double
        /// </summary>
        public double GetConfigValueAsDouble(string key, double defaultValue = 0.0)
        {
            var value = GetConfigValue(key);
            return double.TryParse(value, out double result) ? result : defaultValue;
        }

        /// <summary>
        /// Check if feature flag is enabled
        /// </summary>
        public bool IsFeatureEnabled(string featureName)
        {
            return _featureFlags.ContainsKey(featureName) && _featureFlags[featureName];
        }

        /// <summary>
        /// Set configuration value
        /// </summary>
        public void SetConfigValue(string key, string value)
        {
            _configurationValues[key] = value;
            LastConfigUpdate = DateTime.Now;
            OnPropertyChanged(nameof(ConfigurationValues));
            OnPropertyChanged(nameof(ConfigurationStatusText));
        }

        /// <summary>
        /// Set feature flag
        /// </summary>
        public void SetFeatureFlag(string featureName, bool isEnabled)
        {
            _featureFlags[featureName] = isEnabled;
            OnPropertyChanged(nameof(FeatureFlags));
            OnPropertyChanged(nameof(FeatureFlagsText));
        }

        /// <summary>
        /// Reload configuration from all sources
        /// </summary>
        public void ReloadConfiguration()
        {
            LoadConfiguration();
            InitializeFeatureFlags();
            ConfigurationReloaded?.Invoke();
        }

        /// <summary>
        /// Get API base URL based on environment
        /// </summary>
        public string GetApiBaseUrl()
        {
            var configUrl = GetConfigValue("ApiBaseUrl");
            if (!string.IsNullOrEmpty(configUrl))
                return configUrl;

            if (IsProduction)
                return "https://api.managementfile.com";
            if (IsStaging)
                return "https://staging-api.managementfile.com";
            
            return "http://localhost:5000"; // Development
        }

        /// <summary>
        /// Get database connection string
        /// </summary>
        public string GetConnectionString(string name = "DefaultConnection")
        {
            var connStringKey = $"ConnectionString_{name}";
            var connString = GetConfigValue(connStringKey);
            
            if (!string.IsNullOrEmpty(connString))
                return connString;

            // Fallback connection strings
            if (IsProduction)
                return "Server=prod-sql;Database=ManagementFile;Trusted_Connection=true;";
            if (IsStaging)
                return "Server=staging-sql;Database=ManagementFile_Staging;Trusted_Connection=true;";
            
            return "Server=(localdb)\\mssqllocaldb;Database=ManagementFile_Dev;Trusted_Connection=true;";
        }

        /// <summary>
        /// Get log level for the current environment
        /// </summary>
        public string GetLogLevel()
        {
            return GetConfigValue("LogLevel", IsDevelopment ? "Debug" : "Warning");
        }

        /// <summary>
        /// Get API timeout in milliseconds
        /// </summary>
        public int GetApiTimeout()
        {
            return GetConfigValueAsInt("ApiTimeout", IsDevelopment ? 60000 : 30000);
        }

        /// <summary>
        /// Get cache expiration in seconds
        /// </summary>
        public int GetCacheExpiration()
        {
            return GetConfigValueAsInt("CacheExpiration", IsDevelopment ? 300 : 3600);
        }

        /// <summary>
        /// Get maximum retry attempts
        /// </summary>
        public int GetMaxRetryAttempts()
        {
            return GetConfigValueAsInt("MaxRetryAttempts", IsDevelopment ? 10 : 3);
        }

        /// <summary>
        /// Export configuration to file
        /// </summary>
        public void ExportConfiguration(string filePath)
        {
            try
            {
                var lines = new List<string>();
                lines.Add($"# Configuration Export - {DateTime.Now}");
                lines.Add($"# Environment: {Environment}");
                lines.Add($"# Application: {ApplicationName} v{ApplicationVersion}");
                lines.Add("");

                lines.Add("[Configuration Values]");
                foreach (var config in _configurationValues)
                {
                    lines.Add($"{config.Key}={config.Value}");
                }

                lines.Add("");
                lines.Add("[Feature Flags]");
                foreach (var flag in _featureFlags)
                {
                    lines.Add($"Feature_{flag.Key}={flag.Value}");
                }

                File.WriteAllLines(filePath, lines);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to export configuration: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Import configuration from file
        /// </summary>
        public void ImportConfiguration(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException("Configuration file not found");

                var lines = File.ReadAllLines(filePath);
                var currentSection = "";

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                        continue;

                    if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                    {
                        currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                        continue;
                    }

                    var parts = trimmedLine.Split(new[] { '=' }, 2);
                    if (parts.Length != 2) continue;

                    var key = parts[0].Trim();
                    var value = parts[1].Trim();

                    if (currentSection == "Configuration Values")
                    {
                        _configurationValues[key] = value;
                    }
                    else if (currentSection == "Feature Flags" && key.StartsWith("Feature_"))
                    {
                        var featureName = key.Substring(8); // Remove "Feature_" prefix
                        if (bool.TryParse(value, out bool isEnabled))
                        {
                            _featureFlags[featureName] = isEnabled;
                        }
                    }
                }

                LastConfigUpdate = DateTime.Now;
                ReloadConfiguration();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to import configuration: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validate current configuration
        /// </summary>
        public List<string> ValidateConfiguration()
        {
            var issues = new List<string>();

            // Check required configuration values
            var requiredKeys = new[] { "ApplicationName", "ApplicationVersion", "Environment" };
            foreach (var key in requiredKeys)
            {
                if (!_configurationValues.ContainsKey(key) || string.IsNullOrEmpty(_configurationValues[key]))
                {
                    issues.Add($"Missing required configuration: {key}");
                }
            }

            // Validate API timeout
            var apiTimeout = GetApiTimeout();
            if (apiTimeout < 1000 || apiTimeout > 300000) // 1s to 5 minutes
            {
                issues.Add($"API timeout out of range: {apiTimeout}ms (should be 1000-300000)");
            }

            // Validate cache expiration
            var cacheExpiration = GetCacheExpiration();
            if (cacheExpiration < 60 || cacheExpiration > 86400) // 1 minute to 24 hours
            {
                issues.Add($"Cache expiration out of range: {cacheExpiration}s (should be 60-86400)");
            }

            // Validate environment
            var validEnvironments = new[] { "Development", "Staging", "Production" };
            if (!Array.Exists(validEnvironments, env => env.Equals(Environment, StringComparison.OrdinalIgnoreCase)))
            {
                issues.Add($"Invalid environment: {Environment}");
            }

            return issues;
        }

        #endregion

        #region Events

        /// <summary>
        /// Event raised when configuration is reloaded
        /// </summary>
        public event Action ConfigurationReloaded;

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void SetProperty<T>(ref T backingField, T value, [CallerMemberName] string propertyName = null)
        {
            if (!Equals(backingField, value))
            {
                backingField = value;
                OnPropertyChanged(propertyName);
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}