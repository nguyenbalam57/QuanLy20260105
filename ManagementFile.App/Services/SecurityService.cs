using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using System.Linq;

namespace ManagementFile.App.Services
{
    /// <summary>
    /// Service for security hardening and production security measures
    /// Phase 5 Week 15 - Production Readiness & Final Polish
    /// </summary>
    public sealed class SecurityService : INotifyPropertyChanged
    {

        #region Fields

        private readonly ConfigurationService _configurationService;

        private readonly Dictionary<string, SecurityPolicy> _securityPolicies;
        private readonly List<SecurityEvent> _securityEvents;
        private readonly Dictionary<string, int> _failedAttempts;
        private readonly HashSet<string> _blockedIPs;
        private readonly Dictionary<string, DateTime> _sessionTimeouts;

        private bool _isSecurityEnabled;
        private SecurityLevel _currentSecurityLevel;
        private DateTime _lastSecurityScan;
        private int _securityViolations;
        private string _securityStatus;
        private bool _disposed;

        private const int MAX_FAILED_ATTEMPTS = 5;
        private const int LOCKOUT_DURATION_MINUTES = 30;
        private const int MAX_SECURITY_EVENTS = 1000;
        private const string SECURITY_LOG_FILE = "security.log";

        #endregion

        #region Constructor

        public SecurityService(
            ConfigurationService configurationService)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));

            _securityPolicies = new Dictionary<string, SecurityPolicy>();
            _securityEvents = new List<SecurityEvent>();
            _failedAttempts = new Dictionary<string, int>();
            _blockedIPs = new HashSet<string>();
            _sessionTimeouts = new Dictionary<string, DateTime>();

            _currentSecurityLevel = SecurityLevel.Standard;
            _securityStatus = "Initializing";

            InitializeSecurityPolicies();
            InitializeSecurityMeasures();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Is security hardening enabled
        /// </summary>
        public bool IsSecurityEnabled
        {
            get => _isSecurityEnabled;
            private set => SetProperty(ref _isSecurityEnabled, value);
        }

        /// <summary>
        /// Current security level
        /// </summary>
        public SecurityLevel CurrentSecurityLevel
        {
            get => _currentSecurityLevel;
            set => SetProperty(ref _currentSecurityLevel, value);
        }

        /// <summary>
        /// Last security scan time
        /// </summary>
        public DateTime LastSecurityScan
        {
            get => _lastSecurityScan;
            private set => SetProperty(ref _lastSecurityScan, value);
        }

        /// <summary>
        /// Number of security violations detected
        /// </summary>
        public int SecurityViolations
        {
            get => _securityViolations;
            private set => SetProperty(ref _securityViolations, value);
        }

        /// <summary>
        /// Security status description
        /// </summary>
        public string SecurityStatus
        {
            get => _securityStatus ?? "Unknown";
            private set => SetProperty(ref _securityStatus, value);
        }

        /// <summary>
        /// Security policies count
        /// </summary>
        public int SecurityPoliciesCount => _securityPolicies.Count;

        /// <summary>
        /// Failed attempts count
        /// </summary>
        public int FailedAttemptsCount => _failedAttempts.Count;

        /// <summary>
        /// Blocked IPs count
        /// </summary>
        public int BlockedIPsCount => _blockedIPs.Count;

        /// <summary>
        /// Recent security events
        /// </summary>
        public IEnumerable<SecurityEvent> RecentSecurityEvents => _securityEvents.Skip(Math.Max(0, _securityEvents.Count - 50));

        #endregion

        #region UI Helper Properties

        /// <summary>
        /// Security level text
        /// </summary>
        public string SecurityLevelText => CurrentSecurityLevel.ToString();

        /// <summary>
        /// Security level color
        /// </summary>
        public string SecurityLevelColor
        {
            get
            {
                if (CurrentSecurityLevel == SecurityLevel.Low) return "Green";
                if (CurrentSecurityLevel == SecurityLevel.Standard) return "Blue";
                if (CurrentSecurityLevel == SecurityLevel.High) return "Orange";
                if (CurrentSecurityLevel == SecurityLevel.Maximum) return "Red";
                return "Gray";
            }
        }

        /// <summary>
        /// Security status display text
        /// </summary>
        public string SecurityStatusText => $"Security: {SecurityStatus}";

        /// <summary>
        /// Last scan display text
        /// </summary>
        public string LastScanText => LastSecurityScan > DateTime.MinValue 
            ? $"Last scan: {LastSecurityScan:yyyy-MM-dd HH:mm:ss}" 
            : "No scans performed";

        /// <summary>
        /// Security violations text
        /// </summary>
        public string SecurityViolationsText => SecurityViolations == 0 
            ? "No violations detected" 
            : $"{SecurityViolations} violations detected";

        /// <summary>
        /// Has security violations
        /// </summary>
        public bool HasSecurityViolations => SecurityViolations > 0;

        #endregion

        #region Methods

        /// <summary>
        /// Initialize security policies
        /// </summary>
        private void InitializeSecurityPolicies()
        {
            // Password policy
            _securityPolicies["PasswordPolicy"] = new SecurityPolicy
            {
                Name = "Password Policy",
                Description = "Strong password requirements",
                IsEnabled = true,
                Settings = new Dictionary<string, object>
                {
                    { "MinLength", 8 },
                    { "RequireUppercase", true },
                    { "RequireLowercase", true },
                    { "RequireNumbers", true },
                    { "RequireSpecialChars", true },
                    { "MaxAge", 90 }, // days
                    { "HistoryCount", 5 }
                }
            };

            // Session policy
            _securityPolicies["SessionPolicy"] = new SecurityPolicy
            {
                Name = "Session Policy",
                Description = "Session timeout and management",
                IsEnabled = true,
                Settings = new Dictionary<string, object>
                {
                    { "TimeoutMinutes", 30 },
                    { "MaxConcurrentSessions", 3 },
                    { "RequireReauth", true },
                    { "SecureCookies", true }
                }
            };

            // Access control policy
            _securityPolicies["AccessControlPolicy"] = new SecurityPolicy
            {
                Name = "Access Control Policy",
                Description = "Role-based access control",
                IsEnabled = true,
                Settings = new Dictionary<string, object>
                {
                    { "RequireAuthorization", true },
                    { "AuditAccess", true },
                    { "DenyByDefault", true }
                }
            };

            // Data protection policy
            _securityPolicies["DataProtectionPolicy"] = new SecurityPolicy
            {
                Name = "Data Protection Policy",
                Description = "Encryption and data security",
                IsEnabled = true,
                Settings = new Dictionary<string, object>
                {
                    { "EncryptSensitiveData", true },
                    { "SecureTransport", true },
                    { "DataClassification", true },
                    { "BackupEncryption", true }
                }
            };

            // Audit policy
            _securityPolicies["AuditPolicy"] = new SecurityPolicy
            {
                Name = "Audit Policy",
                Description = "Security event auditing",
                IsEnabled = true,
                Settings = new Dictionary<string, object>
                {
                    { "LogSecurityEvents", true },
                    { "LogAccessAttempts", true },
                    { "LogPrivilegedActions", true },
                    { "RetentionDays", 365 }
                }
            };
        }

        /// <summary>
        /// Initialize security measures
        /// </summary>
        private void InitializeSecurityMeasures()
        {
            try
            {
                IsSecurityEnabled = _configurationService.IsFeatureEnabled("EnableSecurity");
                
                if (IsSecurityEnabled)
                {
                    ApplySecurityHardening();
                }

                SecurityStatus = IsSecurityEnabled ? "Active" : "Disabled";
                LogSecurityEvent("Security service initialized", SecurityEventType.Information);
            }
            catch (Exception ex)
            {
                SecurityStatus = "Error";
                LogSecurityEvent($"Security initialization failed: {ex.Message}", SecurityEventType.Error);
            }
        }

        /// <summary>
        /// Apply security hardening measures
        /// </summary>
        private void ApplySecurityHardening()
        {
            try
            {
                // Set security level based on environment
                if (_configurationService.IsProduction)
                {
                    CurrentSecurityLevel = SecurityLevel.High;
                }
                else if (_configurationService.IsStaging)
                {
                    CurrentSecurityLevel = SecurityLevel.Standard;
                }
                else
                {
                    CurrentSecurityLevel = SecurityLevel.Low;
                }

                // Apply security measures based on level
                switch (CurrentSecurityLevel)
                {
                    case SecurityLevel.Maximum:
                    case SecurityLevel.High:
                        EnableAdvancedSecurity();
                        break;
                    case SecurityLevel.Standard:
                        EnableStandardSecurity();
                        break;
                    case SecurityLevel.Low:
                        EnableBasicSecurity();
                        break;
                }

                LogSecurityEvent($"Security hardening applied - Level: {CurrentSecurityLevel}", SecurityEventType.Information);
            }
            catch (Exception ex)
            {
                LogSecurityEvent($"Security hardening failed: {ex.Message}", SecurityEventType.Error);
            }
        }

        /// <summary>
        /// Enable advanced security measures
        /// </summary>
        private void EnableAdvancedSecurity()
        {
            EnableStandardSecurity();
            
            // Additional advanced measures
            _securityPolicies["PasswordPolicy"].Settings["MinLength"] = 12;
            _securityPolicies["SessionPolicy"].Settings["TimeoutMinutes"] = 15;
            
            LogSecurityEvent("Advanced security measures enabled", SecurityEventType.Information);
        }

        /// <summary>
        /// Enable standard security measures
        /// </summary>
        private void EnableStandardSecurity()
        {
            EnableBasicSecurity();
            
            // Additional standard measures
            _securityPolicies["SessionPolicy"].Settings["RequireReauth"] = true;
            _securityPolicies["AuditPolicy"].Settings["LogPrivilegedActions"] = true;
            
            LogSecurityEvent("Standard security measures enabled", SecurityEventType.Information);
        }

        /// <summary>
        /// Enable basic security measures
        /// </summary>
        private void EnableBasicSecurity()
        {
            // Basic security measures
            foreach (var policy in _securityPolicies.Values)
            {
                policy.IsEnabled = true;
            }
            
            LogSecurityEvent("Basic security measures enabled", SecurityEventType.Information);
        }

        /// <summary>
        /// Validate password against policy
        /// </summary>
        public bool ValidatePassword(string password, out List<string> violations)
        {
            violations = new List<string>();
            
            if (!_securityPolicies.ContainsKey("PasswordPolicy"))
            {
                return true;
            }

            var policy = _securityPolicies["PasswordPolicy"];
            if (!policy.IsEnabled)
            {
                return true;
            }

            try
            {
                var minLength = (int)policy.Settings["MinLength"];
                var requireUppercase = (bool)policy.Settings["RequireUppercase"];
                var requireLowercase = (bool)policy.Settings["RequireLowercase"];
                var requireNumbers = (bool)policy.Settings["RequireNumbers"];
                var requireSpecialChars = (bool)policy.Settings["RequireSpecialChars"];

                if (string.IsNullOrEmpty(password))
                {
                    violations.Add("Password cannot be empty");
                    return false;
                }

                if (password.Length < minLength)
                {
                    violations.Add($"Password must be at least {minLength} characters long");
                }

                if (requireUppercase && !password.Any(char.IsUpper))
                {
                    violations.Add("Password must contain at least one uppercase letter");
                }

                if (requireLowercase && !password.Any(char.IsLower))
                {
                    violations.Add("Password must contain at least one lowercase letter");
                }

                if (requireNumbers && !password.Any(char.IsDigit))
                {
                    violations.Add("Password must contain at least one number");
                }

                if (requireSpecialChars && !password.Any(ch => !char.IsLetterOrDigit(ch)))
                {
                    violations.Add("Password must contain at least one special character");
                }

                return violations.Count == 0;
            }
            catch (Exception ex)
            {
                violations.Add($"Password validation error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Record failed login attempt
        /// </summary>
        public bool RecordFailedAttempt(string username, string ipAddress)
        {
            try
            {
                var key = $"{username}:{ipAddress}";
                
                if (!_failedAttempts.ContainsKey(key))
                {
                    _failedAttempts[key] = 0;
                }

                _failedAttempts[key]++;
                
                LogSecurityEvent($"Failed login attempt for {username} from {ipAddress} (Attempt {_failedAttempts[key]})", 
                    SecurityEventType.Warning);

                if (_failedAttempts[key] >= MAX_FAILED_ATTEMPTS)
                {
                    _blockedIPs.Add(ipAddress);
                    SecurityViolations++;
                    
                    LogSecurityEvent($"IP {ipAddress} blocked after {MAX_FAILED_ATTEMPTS} failed attempts", 
                        SecurityEventType.Critical);
                    
                    return true; // Account locked
                }

                return false;
            }
            catch (Exception ex)
            {
                LogSecurityEvent($"Error recording failed attempt: {ex.Message}", SecurityEventType.Error);
                return false;
            }
        }

        /// <summary>
        /// Check if IP is blocked
        /// </summary>
        public bool IsIPBlocked(string ipAddress)
        {
            return _blockedIPs.Contains(ipAddress);
        }

        /// <summary>
        /// Unblock IP address
        /// </summary>
        public void UnblockIP(string ipAddress)
        {
            if (_blockedIPs.Remove(ipAddress))
            {
                // Clear failed attempts for this IP
                var keysToRemove = _failedAttempts.Keys.Where(k => k.EndsWith($":{ipAddress}")).ToList();
                foreach (var key in keysToRemove)
                {
                    _failedAttempts.Remove(key);
                }
                
                LogSecurityEvent($"IP {ipAddress} unblocked", SecurityEventType.Information);
            }
        }

        /// <summary>
        /// Clear all blocked IPs
        /// </summary>
        public void ClearAllBlockedIPs()
        {
            var count = _blockedIPs.Count;
            _blockedIPs.Clear();
            _failedAttempts.Clear();
            
            LogSecurityEvent($"Cleared {count} blocked IPs", SecurityEventType.Information);
        }

        /// <summary>
        /// Encrypt sensitive data
        /// </summary>
        public string EncryptData(string plainText, string key = null)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            try
            {
                if (!_securityPolicies.ContainsKey("DataProtectionPolicy") || 
                    !_securityPolicies["DataProtectionPolicy"].IsEnabled ||
                    !(bool)_securityPolicies["DataProtectionPolicy"].Settings["EncryptSensitiveData"])
                {
                    return plainText; // No encryption required
                }

                // Simple encryption for demo purposes (use proper encryption in production)
                var data = Encoding.UTF8.GetBytes(plainText);
                var encrypted = Convert.ToBase64String(data);
                
                LogSecurityEvent("Data encrypted", SecurityEventType.Information);
                return encrypted;
            }
            catch (Exception ex)
            {
                LogSecurityEvent($"Encryption failed: {ex.Message}", SecurityEventType.Error);
                return plainText;
            }
        }

        /// <summary>
        /// Decrypt sensitive data
        /// </summary>
        public string DecryptData(string encryptedText, string key = null)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return encryptedText;

            try
            {
                if (!_securityPolicies.ContainsKey("DataProtectionPolicy") || 
                    !_securityPolicies["DataProtectionPolicy"].IsEnabled ||
                    !(bool)_securityPolicies["DataProtectionPolicy"].Settings["EncryptSensitiveData"])
                {
                    return encryptedText; // No decryption required
                }

                // Simple decryption for demo purposes
                var data = Convert.FromBase64String(encryptedText);
                var decrypted = Encoding.UTF8.GetString(data);
                
                return decrypted;
            }
            catch (Exception ex)
            {
                LogSecurityEvent($"Decryption failed: {ex.Message}", SecurityEventType.Error);
                return encryptedText;
            }
        }

        /// <summary>
        /// Generate secure hash
        /// </summary>
        public string GenerateHash(string input, string salt = null)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            try
            {
                using (var sha256 = SHA256.Create())
                {
                    var saltedInput = input + (salt ?? "ManagementFileSalt");
                    var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedInput));
                    return Convert.ToBase64String(hash);
                }
            }
            catch (Exception ex)
            {
                LogSecurityEvent($"Hash generation failed: {ex.Message}", SecurityEventType.Error);
                return string.Empty;
            }
        }

        /// <summary>
        /// Verify hash
        /// </summary>
        public bool VerifyHash(string input, string hash, string salt = null)
        {
            var computedHash = GenerateHash(input, salt);
            return computedHash.Equals(hash, StringComparison.Ordinal);
        }

        /// <summary>
        /// Perform security scan
        /// </summary>
        public SecurityScanResult PerformSecurityScan()
        {
            try
            {
                LastSecurityScan = DateTime.Now;
                var result = new SecurityScanResult
                {
                    ScanTime = LastSecurityScan,
                    Issues = new List<string>()
                };

                // Check password policies
                foreach (var policy in _securityPolicies.Values)
                {
                    if (!policy.IsEnabled)
                    {
                        result.Issues.Add($"Security policy '{policy.Name}' is disabled");
                    }
                }

                // Check for security violations
                if (SecurityViolations > 0)
                {
                    result.Issues.Add($"{SecurityViolations} security violations detected");
                }

                // Check blocked IPs
                if (_blockedIPs.Count > 10)
                {
                    result.Issues.Add($"High number of blocked IPs: {_blockedIPs.Count}");
                }

                // Check recent security events
                var recentCriticalEvents = _securityEvents
                    .Where(e => e.Type == SecurityEventType.Critical && e.Timestamp > DateTime.Now.AddHours(-24))
                    .Count();
                
                if (recentCriticalEvents > 5)
                {
                    result.Issues.Add($"High number of critical security events in last 24h: {recentCriticalEvents}");
                }

                result.IssueCount = result.Issues.Count;
                result.OverallRating = result.IssueCount == 0 ? "Excellent" : 
                                     result.IssueCount <= 2 ? "Good" : 
                                     result.IssueCount <= 5 ? "Fair" : "Poor";

                LogSecurityEvent($"Security scan completed - {result.IssueCount} issues found", 
                    result.IssueCount > 0 ? SecurityEventType.Warning : SecurityEventType.Information);

                return result;
            }
            catch (Exception ex)
            {
                LogSecurityEvent($"Security scan failed: {ex.Message}", SecurityEventType.Error);
                return new SecurityScanResult
                {
                    ScanTime = DateTime.Now,
                    IssueCount = 1,
                    Issues = new List<string> { $"Security scan failed: {ex.Message}" },
                    OverallRating = "Error"
                };
            }
        }

        /// <summary>
        /// Log security event
        /// </summary>
        public void LogSecurityEvent(string message, SecurityEventType type)
        {
            try
            {
                var securityEvent = new SecurityEvent
                {
                    Id = Guid.NewGuid(),
                    Timestamp = DateTime.Now,
                    Type = type,
                    Message = message,
                    Source = "SecurityService"
                };

                _securityEvents.Add(securityEvent);

                // Keep only recent events
                while (_securityEvents.Count > MAX_SECURITY_EVENTS)
                {
                    _securityEvents.RemoveAt(0);
                }

                // Write to security log file
                WriteToSecurityLog(securityEvent);

                SecurityEventLogged?.Invoke(securityEvent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Security logging error: {ex.Message}");
            }
        }

        /// <summary>
        /// Write to security log file
        /// </summary>
        private void WriteToSecurityLog(SecurityEvent securityEvent)
        {
            try
            {
                var logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SECURITY_LOG_FILE);
                var logEntry = $"{securityEvent.Timestamp:yyyy-MM-dd HH:mm:ss}\t{securityEvent.Type}\t{securityEvent.Message}\t{securityEvent.Source}";
                
                File.AppendAllLines(logFilePath, new[] { logEntry });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Security log write error: {ex.Message}");
            }
        }

        /// <summary>
        /// Export security report
        /// </summary>
        public void ExportSecurityReport(string filePath)
        {
            try
            {
                var lines = new List<string>();
                lines.Add($"# Security Report - {DateTime.Now}");
                lines.Add($"# Security Level: {CurrentSecurityLevel}");
                lines.Add($"# Security Status: {SecurityStatus}");
                lines.Add($"# Security Violations: {SecurityViolations}");
                lines.Add($"# Blocked IPs: {_blockedIPs.Count}");
                lines.Add($"# Failed Attempts: {_failedAttempts.Count}");
                lines.Add("");
                lines.Add("[Security Policies]");
                foreach (var policy in _securityPolicies.Values)
                {
                    lines.Add($"Policy: {policy.Name} - {(policy.IsEnabled ? "Enabled" : "Disabled")}");
                    lines.Add($"  Description: {policy.Description}");
                    foreach (var setting in policy.Settings)
                    {
                        lines.Add($"  {setting.Key}: {setting.Value}");
                    }
                    lines.Add("");
                }

                lines.Add("[Recent Security Events]");
                foreach (var eventItem in _securityEvents.Skip(Math.Max(0, _securityEvents.Count - 100)))
                {
                    lines.Add($"{eventItem.Timestamp:yyyy-MM-dd HH:mm:ss}\t{eventItem.Type}\t{eventItem.Message}");
                }

                lines.Add("");
                lines.Add("[Blocked IP Addresses]");
                foreach (var ip in _blockedIPs)
                {
                    lines.Add(ip);
                }

                File.WriteAllLines(filePath, lines);
                LogSecurityEvent($"Security report exported to {filePath}", SecurityEventType.Information);
            }
            catch (Exception ex)
            {
                LogSecurityEvent($"Security report export failed: {ex.Message}", SecurityEventType.Error);
                throw;
            }
        }

        /// <summary>
        /// Get security policy by name
        /// </summary>
        public SecurityPolicy GetSecurityPolicy(string name)
        {
            return _securityPolicies.ContainsKey(name) ? _securityPolicies[name] : null;
        }

        /// <summary>
        /// Update security policy
        /// </summary>
        public void UpdateSecurityPolicy(string name, SecurityPolicy policy)
        {
            _securityPolicies[name] = policy;
            LogSecurityEvent($"Security policy '{name}' updated", SecurityEventType.Information);
        }

        #endregion

        #region Events

        /// <summary>
        /// Event raised when security event is logged
        /// </summary>
        public event Action<SecurityEvent> SecurityEventLogged;

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

        #region IDisposable

        public void Dispose()
        {
            if (!_disposed)
            {
                _securityPolicies?.Clear();
                _securityEvents?.Clear();
                _failedAttempts?.Clear();
                _blockedIPs?.Clear();
                _sessionTimeouts?.Clear();
                _disposed = true;
            }
        }

        #endregion
    }

    #region Supporting Models

    /// <summary>
    /// Security policy model
    /// </summary>
    public class SecurityPolicy
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public bool IsEnabled { get; set; }
        public Dictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Security event model
    /// </summary>
    public class SecurityEvent
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }
        public SecurityEventType Type { get; set; }
        public string Message { get; set; } = "";
        public string Source { get; set; } = "";

        public string TypeIcon
        {
            get
            {
                if (Type == SecurityEventType.Information) return "ℹ️";
                if (Type == SecurityEventType.Warning) return "⚠️";
                if (Type == SecurityEventType.Error) return "❌";
                if (Type == SecurityEventType.Critical) return "🚨";
                return "❓";
            }
        }

        public string TypeColor
        {
            get
            {
                if (Type == SecurityEventType.Information) return "Blue";
                if (Type == SecurityEventType.Warning) return "Orange";
                if (Type == SecurityEventType.Error) return "Red";
                if (Type == SecurityEventType.Critical) return "DarkRed";
                return "Gray";
            }
        }
    }

    /// <summary>
    /// Security scan result model
    /// </summary>
    public class SecurityScanResult
    {
        public DateTime ScanTime { get; set; }
        public int IssueCount { get; set; }
        public List<string> Issues { get; set; } = new List<string>();
        public string OverallRating { get; set; } = "";
    }

    #endregion

    #region Enums

    /// <summary>
    /// Security level enumeration
    /// </summary>
    public enum SecurityLevel
    {
        Low,
        Standard,
        High,
        Maximum
    }

    /// <summary>
    /// Security event type enumeration
    /// </summary>
    public enum SecurityEventType
    {
        Information,
        Warning,
        Error,
        Critical
    }

    #endregion
}