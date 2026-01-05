using ManagementFile.App.Models.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ManagementFile.App.Views.Dialogs.Comments
{
    /// <summary>
    /// CommentLine - Flexible input dialog with customizable fields
    /// Usage:
    ///   // Simple single field
    ///   var result = CommentLine.ShowSingle("Nhập tên", "Họ tên");
    ///   
    ///   // Multiple custom fields
    ///   var fields = new Dictionary<string, CommentLineFieldConfig>
    ///   {
    ///       { "name", new CommentLineFieldConfig { Label = "Tên", Required = true } },
    ///       { "email", new CommentLineFieldConfig { Label = "Email", Placeholder = "email@example.com" } },
    ///       { "note", new CommentLineFieldConfig { Label = "Ghi chú", Multiline = true, MaxLength = 500 } }
    ///   };
    ///   var result = CommentLine.Show("Nhập thông tin", "Điền form", fields);
    ///   
    ///   // Access results
    ///   if (result != null)
    ///   {
    ///       var name = result["name"];
    ///       var email = result["email"];
    ///       var note = result["note"];
    ///   }
    /// </summary>
    public partial class CommentLine : Window
    {
        #region Fields

        private Dictionary<string, CommentLineFieldConfig> _fieldConfigs;
        private Dictionary<string, TextBox> _fieldControls;
        private Dictionary<string, TextBlock> _fieldLabels;
        private Dictionary<string, TextBlock> _charCounters;

        #endregion

        #region Properties

        /// <summary>
        /// Result dictionary with field values
        /// </summary>
        public Dictionary<string, string> Result { get; private set; }

        /// <summary>
        /// Whether user confirmed the dialog
        /// </summary>
        public bool IsConfirmed { get; private set; }

        #endregion

        #region Constructor

        private CommentLine()
        {
            InitializeComponent();

            _fieldControls = new Dictionary<string, TextBox>();
            _fieldLabels = new Dictionary<string, TextBlock>();
            _charCounters = new Dictionary<string, TextBlock>();

            Loaded += CommentLine_Loaded;

            System.Diagnostics.Debug.WriteLine("[CommentLine] Constructor initialized");
        }

        #endregion

        #region Static Show Methods - Advanced

        /// <summary>
        /// Show dialog with custom fields configuration
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="message">Optional message (can be null or empty)</param>
        /// <param name="fields">Dictionary of field configurations (key = field name, value = config)</param>
        /// <param name="defaultValues">Optional default values for fields</param>
        /// <param name="owner">Owner window</param>
        /// <returns>Dictionary of field values (key = field name, value = user input) or null if cancelled</returns>
        public static Dictionary<string, string> Show(
            string title = "Nhập Thông Tin",
            string message = null,
            Dictionary<string, CommentLineFieldConfig> fields = null,
            Dictionary<string, string> defaultValues = null,
            Window owner = null)
        {
            // Validate and create default fields if needed
            if (fields == null || fields.Count == 0)
            {
                fields = new Dictionary<string, CommentLineFieldConfig>
                {
                    { "value", new CommentLineFieldConfig { Label = "Nội dung", Required = true } }
                };
            }

            var dialog = new CommentLine
            {
                _fieldConfigs = fields,
               
            };

            // Safely set owner
            try
            {
                if (owner != null && owner.IsLoaded)
                {
                    dialog.Owner = owner;
                }
                else if (Application.Current?.MainWindow != null && Application.Current.MainWindow.IsLoaded)
                {
                    dialog.Owner = Application.Current.MainWindow;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CommentLine] Could not set owner: {ex.Message}");
                // Continue without owner
            }

            dialog.TitleTextBlock.Text = title;
            dialog.Title = title;

            if (!string.IsNullOrWhiteSpace(message))
            {
                dialog.MessageTextBlock.Text = message;
                dialog.MessageTextBlock.Visibility = Visibility.Visible;
            }

            // Build UI dynamically
            dialog.BuildFields(defaultValues);

            System.Diagnostics.Debug.WriteLine($"[CommentLine] Show dialog '{title}' with {fields.Count} fields");

            var result = dialog.ShowDialog();

            if (result == true && dialog.IsConfirmed)
            {
                System.Diagnostics.Debug.WriteLine($"[CommentLine] Confirmed with {dialog.Result.Count} values");
                foreach (var kvp in dialog.Result)
                {
                    System.Diagnostics.Debug.WriteLine($"  - {kvp.Key}: {kvp.Value?.Substring(0, Math.Min(50, (kvp.Value != null && kvp.Value.Length > 0) ? kvp.Value.Length : 0))}");
                }
                return dialog.Result;
            }

            System.Diagnostics.Debug.WriteLine("[CommentLine] Cancelled");
            return null;
        }

        #endregion

        #region Static Show Methods - Simple Shortcuts

        /// <summary>
        /// Show simple single-line input dialog
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="label">Field label</param>
        /// <param name="placeholder">Placeholder text</param>
        /// <param name="defaultValue">Default value</param>
        /// <param name="required">Whether field is required</param>
        /// <param name="maxLength">Maximum character length (0 = no limit)</param>
        /// <param name="owner">Owner window</param>
        /// <returns>User input string or null if cancelled</returns>
        public static string ShowSingle(
            string title = "Nhập Thông Tin",
            string label = "Nội dung",
            string placeholder = "",
            string defaultValue = "",
            bool required = true,
            int maxLength = 0,
            Window owner = null)
        {
            var fields = new Dictionary<string, CommentLineFieldConfig>
            {
                {
                    "value",
                    new CommentLineFieldConfig
                    {
                        Label = label,
                        Placeholder = placeholder,
                        Required = required,
                        MaxLength = maxLength
                    }
                }
            };

            var defaults = string.IsNullOrEmpty(defaultValue)
                ? null
                : new Dictionary<string, string> { { "value", defaultValue } };

            var result = Show(title, null, fields, defaults, owner);

            return result?["value"];
        }

        /// <summary>
        /// Show multiline text input dialog (textarea)
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="label">Field label</param>
        /// <param name="placeholder">Placeholder text</param>
        /// <param name="defaultValue">Default value</param>
        /// <param name="required">Whether field is required</param>
        /// <param name="maxLength">Maximum character length (0 = no limit)</param>
        /// <param name="owner">Owner window</param>
        /// <returns>User input string or null if cancelled</returns>
        public static string ShowMultiline(
            string title = "Nhập Nội Dung",
            string label = "Nội dung",
            string placeholder = "",
            string defaultValue = "",
            bool required = true,
            int maxLength = 0,
            Window owner = null)
        {
            var fields = new Dictionary<string, CommentLineFieldConfig>
            {
                {
                    "value",
                    new CommentLineFieldConfig
                    {
                        Label = label,
                        Placeholder = placeholder,
                        Required = required,
                        Multiline = true,
                        MaxLength = maxLength
                    }
                }
            };

            var defaults = string.IsNullOrEmpty(defaultValue)
                ? null
                : new Dictionary<string, string> { { "value", defaultValue } };

            var result = Show(title, null, fields, defaults, owner);

            return result?["value"];
        }

        /// <summary>
        /// Show two-field input dialog
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="field1Label">First field label</param>
        /// <param name="field2Label">Second field label</param>
        /// <param name="field1Required">Whether first field is required</param>
        /// <param name="field2Required">Whether second field is required</param>
        /// <param name="field1Default">First field default value</param>
        /// <param name="field2Default">Second field default value</param>
        /// <param name="field2Multiline">Whether second field is multiline</param>
        /// <param name="owner">Owner window</param>
        /// <returns>Dictionary with "field1" and "field2" keys or null if cancelled</returns>
        public static Dictionary<string, string> ShowDouble(
            string title = "Nhập Thông Tin",
            string field1Label = "Trường 1",
            string field2Label = "Trường 2",
            bool field1Required = true,
            bool field2Required = false,
            string field1Default = "",
            string field2Default = "",
            bool field2Multiline = false,
            Window owner = null)
        {
            var fields = new Dictionary<string, CommentLineFieldConfig>
            {
                { "field1", new CommentLineFieldConfig { Label = field1Label, Required = field1Required } },
                { "field2", new CommentLineFieldConfig { Label = field2Label, Required = field2Required, Multiline = field2Multiline } }
            };

            var defaults = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(field1Default)) defaults["field1"] = field1Default;
            if (!string.IsNullOrEmpty(field2Default)) defaults["field2"] = field2Default;

            return Show(title, null, fields, defaults.Count > 0 ? defaults : null, owner);
        }

        /// <summary>
        /// Show three-field input dialog
        /// </summary>
        public static Dictionary<string, string> ShowTriple(
            string title = "Nhập Thông Tin",
            string field1Label = "Trường 1",
            string field2Label = "Trường 2",
            string field3Label = "Trường 3",
            bool field1Required = true,
            bool field2Required = false,
            bool field3Required = false,
            Window owner = null)
        {
            var fields = new Dictionary<string, CommentLineFieldConfig>
            {
                { "field1", new CommentLineFieldConfig { Label = field1Label, Required = field1Required } },
                { "field2", new CommentLineFieldConfig { Label = field2Label, Required = field2Required } },
                { "field3", new CommentLineFieldConfig { Label = field3Label, Required = field3Required } }
            };

            return Show(title, null, fields, null, owner);
        }

        #endregion

        #region Static Async Methods

        /// <summary>
        /// Async version of Show
        /// </summary>
        public static Task<Dictionary<string, string>> ShowAsync(
            string title = "Nhập Thông Tin",
            string message = null,
            Dictionary<string, CommentLineFieldConfig> fields = null,
            Dictionary<string, string> defaultValues = null,
            Window owner = null)
        {
            return Task.FromResult(Show(title, message, fields, defaultValues, owner));
        }

        /// <summary>
        /// Async version of ShowSingle
        /// </summary>
        public static Task<string> ShowSingleAsync(
            string title = "Nhập Thông Tin",
            string label = "Nội dung",
            string placeholder = "",
            string defaultValue = "",
            bool required = true,
            int maxLength = 0,
            Window owner = null)
        {
            return Task.FromResult(ShowSingle(title, label, placeholder, defaultValue, required, maxLength, owner));
        }

        /// <summary>
        /// Async version of ShowMultiline
        /// </summary>
        public static Task<string> ShowMultilineAsync(
            string title = "Nhập Nội Dung",
            string label = "Nội dung",
            string placeholder = "",
            string defaultValue = "",
            bool required = true,
            int maxLength = 0,
            Window owner = null)
        {
            return Task.FromResult(ShowMultiline(title, label, placeholder, defaultValue, required, maxLength, owner));
        }

        #endregion

        #region Private Methods - UI Building

        private void BuildFields(Dictionary<string, string> defaultValues)
        {
            FieldsContainer.Children.Clear();
            _fieldControls.Clear();
            _fieldLabels.Clear();
            _charCounters.Clear();

            System.Diagnostics.Debug.WriteLine($"[CommentLine] Building {_fieldConfigs.Count} fields");

            foreach (var kvp in _fieldConfigs)
            {
                var fieldKey = kvp.Key;
                var config = kvp.Value;

                System.Diagnostics.Debug.WriteLine($"[CommentLine] Building field '{fieldKey}': {config.Label}");

                // Create field container
                var fieldPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };

                // Create label with required indicator
                var labelPanel = new StackPanel { Orientation = Orientation.Horizontal };

                var label = new TextBlock
                {
                    Text = config.Label,
                    Style = (Style)FindResource("FieldLabel")
                };
                labelPanel.Children.Add(label);
                _fieldLabels[fieldKey] = label;

                if (config.Required)
                {
                    var requiredMark = new TextBlock
                    {
                        Style = (Style)FindResource("RequiredIndicator")
                    };
                    labelPanel.Children.Add(requiredMark);
                }

                fieldPanel.Children.Add(labelPanel);

                // Create TextBox
                var textBox = new TextBox
                {
                    Name = $"Field_{fieldKey}"
                };

                if (config.Multiline)
                {
                    textBox.Style = (Style)FindResource("MultilineTextBox");
                }
                else
                {
                    textBox.Style = (Style)FindResource("ModernTextBox");
                    textBox.Height = 42;
                }

                // Set MaxLength
                if (config.MaxLength > 0)
                {
                    textBox.MaxLength = config.MaxLength;
                }

                // Set Placeholder
                if (!string.IsNullOrEmpty(config.Placeholder))
                {
                    textBox.Tag = config.Placeholder;

                    // Check if we have default value
                    bool hasDefault = defaultValues != null &&
                                     defaultValues.ContainsKey(fieldKey) &&
                                     !string.IsNullOrWhiteSpace(defaultValues[fieldKey]);

                    if (!hasDefault)
                    {
                        UpdatePlaceholder(textBox, config.Placeholder);
                    }

                    textBox.GotFocus += TextBox_GotFocus;
                    textBox.LostFocus += TextBox_LostFocus;
                }

                // Set default value
                if (defaultValues != null && defaultValues.ContainsKey(fieldKey))
                {
                    var defaultValue = defaultValues[fieldKey];
                    if (!string.IsNullOrEmpty(defaultValue))
                    {
                        textBox.Text = defaultValue;
                        textBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2C3E50"));
                    }
                }

                fieldPanel.Children.Add(textBox);

                // Add character counter for fields with MaxLength
                if (config.MaxLength > 0 && config.Multiline)
                {
                    var countText = new TextBlock
                    {
                        FontSize = 11,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#95A5A6")),
                        Margin = new Thickness(0, 4, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Text = $"0 / {config.MaxLength}"
                    };

                    // Update counter on text change
                    var currentFieldKey = fieldKey; // Capture for closure
                    textBox.TextChanged += (s, e) =>
                    {
                        var tb = s as TextBox;
                        var currentText = tb.Text;

                        // Don't count placeholder
                        if (tb.Tag is string ph && currentText == ph)
                        {
                            countText.Text = $"0 / {config.MaxLength}";
                        }
                        else
                        {
                            var length = currentText?.Length ?? 0;
                            countText.Text = $"{length} / {config.MaxLength}";

                            // Change color when approaching limit
                            if (length > config.MaxLength * 0.9)
                            {
                                countText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E67E22"));
                            }
                            else if (length == config.MaxLength)
                            {
                                countText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
                            }
                            else
                            {
                                countText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#95A5A6"));
                            }
                        }
                    };

                    fieldPanel.Children.Add(countText);
                    _charCounters[fieldKey] = countText;

                    // Initialize counter with default value
                    if (defaultValues != null && defaultValues.ContainsKey(fieldKey))
                    {
                        var length = defaultValues[fieldKey]?.Length ?? 0;
                        countText.Text = $"{length} / {config.MaxLength}";
                    }
                }

                _fieldControls[fieldKey] = textBox;
                FieldsContainer.Children.Add(fieldPanel);
            }

            System.Diagnostics.Debug.WriteLine($"[CommentLine] Built {_fieldControls.Count} field controls");
        }

        private void UpdatePlaceholder(TextBox textBox, string placeholder)
        {
            if (string.IsNullOrEmpty(textBox.Text) || textBox.Text == placeholder)
            {
                textBox.Text = placeholder;
                textBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#95A5A6"));
            }
        }

        #endregion

        #region Private Methods - Validation & Data Collection

        private bool ValidateFields()
        {
            ValidationBorder.Visibility = Visibility.Collapsed;

            System.Diagnostics.Debug.WriteLine("[CommentLine] Validating fields...");

            foreach (var kvp in _fieldConfigs)
            {
                var fieldKey = kvp.Key;
                var config = kvp.Value;

                if (config.Required)
                {
                    var textBox = _fieldControls[fieldKey];
                    var value = textBox.Text;

                    // Check if it's placeholder text
                    if (textBox.Tag is string placeholder && value == placeholder)
                    {
                        value = "";
                    }

                    if (string.IsNullOrWhiteSpace(value))
                    {
                        ShowValidationError($"Vui lòng nhập {config.Label}");
                        textBox.Focus();
                        System.Diagnostics.Debug.WriteLine($"[CommentLine] Validation failed: {config.Label} is required");
                        return false;
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine("[CommentLine] Validation passed");
            return true;
        }

        private void ShowValidationError(string message)
        {
            ValidationMessage.Text = message;
            ValidationBorder.Visibility = Visibility.Visible;
        }

        private Dictionary<string, string> CollectValues()
        {
            var result = new Dictionary<string, string>();

            foreach (var kvp in _fieldControls)
            {
                var fieldKey = kvp.Key;
                var textBox = kvp.Value;
                var value = textBox.Text;

                // Check if it's placeholder text
                if (textBox.Tag is string placeholder && value == placeholder)
                {
                    value = "";
                }

                result[fieldKey] = value?.Trim() ?? "";
            }

            return result;
        }

        #endregion

        #region Event Handlers

        private void CommentLine_Loaded(object sender, RoutedEventArgs e)
        {
            // Focus first field
            if (_fieldControls.Count > 0)
            {
                var firstField = _fieldControls.First().Value;
                Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        firstField.Focus();
                        firstField.SelectAll();
                    }),
                    System.Windows.Threading.DispatcherPriority.Loaded
                );
            }

            System.Diagnostics.Debug.WriteLine("[CommentLine] Dialog loaded and focused");
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null && textBox.Tag is string placeholder)
            {
                if (textBox.Text == placeholder)
                {
                    textBox.Text = "";
                    textBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2C3E50"));
                }
                else
                {
                    textBox.SelectAll();
                }
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null && textBox.Tag is string placeholder)
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    UpdatePlaceholder(textBox, placeholder);
                }
            }
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateFields())
            {
                System.Diagnostics.Debug.WriteLine("[CommentLine] Validation failed, cannot confirm");
                return;
            }

            Result = CollectValues();
            IsConfirmed = true;
            DialogResult = true;

            System.Diagnostics.Debug.WriteLine($"[CommentLine] Confirmed - Collected {Result.Count} values:");
            foreach (var kvp in Result)
            {
                var preview = kvp.Value.Length > 50 ? kvp.Value.Substring(0, 50) + "..." : kvp.Value;
                System.Diagnostics.Debug.WriteLine($"  [{kvp.Key}] = '{preview}'");
            }

            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = false;
            DialogResult = false;

            System.Diagnostics.Debug.WriteLine("[CommentLine] Cancelled by user");

            Close();
        }

        #endregion
    }

}