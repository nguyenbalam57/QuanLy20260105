using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ManagementFile.App.Behaviors
{
    /// <summary>
    /// Behavior để chỉ cho phép nhập số và dấu chấm
    /// Hỗ trợ nhập nhanh giờ: 2.5, 0.5, 8
    /// </summary>
    public static class NumericTextBoxBehavior
    {
        #region AllowDecimal Attached Property

        public static readonly DependencyProperty AllowDecimalProperty =
            DependencyProperty.RegisterAttached(
                "AllowDecimal",
                typeof(bool),
                typeof(NumericTextBoxBehavior),
                new PropertyMetadata(true, OnAllowDecimalChanged));

        public static bool GetAllowDecimal(DependencyObject obj)
        {
            return (bool)obj.GetValue(AllowDecimalProperty);
        }

        public static void SetAllowDecimal(DependencyObject obj, bool value)
        {
            obj.SetValue(AllowDecimalProperty, value);
        }

        private static void OnAllowDecimalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                if ((bool)e.NewValue)
                {
                    textBox.PreviewTextInput += TextBox_PreviewTextInput;
                    textBox.PreviewKeyDown += TextBox_PreviewKeyDown;
                    DataObject.AddPastingHandler(textBox, TextBox_Pasting);
                }
                else
                {
                    textBox.PreviewTextInput -= TextBox_PreviewTextInput;
                    textBox.PreviewKeyDown -= TextBox_PreviewKeyDown;
                    DataObject.RemovePastingHandler(textBox, TextBox_Pasting);
                }
            }
        }

        #endregion

        #region SelectAllOnFocus Attached Property

        public static readonly DependencyProperty SelectAllOnFocusProperty =
            DependencyProperty.RegisterAttached(
                "SelectAllOnFocus",
                typeof(bool),
                typeof(NumericTextBoxBehavior),
                new PropertyMetadata(false, OnSelectAllOnFocusChanged));

        public static bool GetSelectAllOnFocus(DependencyObject obj)
        {
            return (bool)obj.GetValue(SelectAllOnFocusProperty);
        }

        public static void SetSelectAllOnFocus(DependencyObject obj, bool value)
        {
            obj.SetValue(SelectAllOnFocusProperty, value);
        }

        private static void OnSelectAllOnFocusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                if ((bool)e.NewValue)
                {
                    textBox.GotFocus += TextBox_GotFocus;
                    textBox.PreviewMouseLeftButtonDown += TextBox_PreviewMouseLeftButtonDown;
                }
                else
                {
                    textBox.GotFocus -= TextBox_GotFocus;
                    textBox.PreviewMouseLeftButtonDown -= TextBox_PreviewMouseLeftButtonDown;
                }
            }
        }

        #endregion

        #region Event Handlers

        private static void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            var text = textBox.Text;
            var newText = text.Insert(textBox.SelectionStart, e.Text);

            // Allow only numbers and one decimal point
            var regex = new Regex(@"^\d*\.?\d*$");
            e.Handled = !regex.IsMatch(newText);
        }

        private static void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var textBox = sender as TextBox;

            // Allow: Backspace, Delete, Tab, Escape, Enter
            if (e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Tab ||
                e.Key == Key.Escape || e.Key == Key.Enter)
            {
                return;
            }

            // Allow: Home, End, Left, Right
            if (e.Key == Key.Home || e.Key == Key.End ||
                e.Key == Key.Left || e.Key == Key.Right)
            {
                return;
            }

            // Allow: Ctrl+A, Ctrl+C, Ctrl+V, Ctrl+X, Ctrl+Z
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (e.Key == Key.A || e.Key == Key.C || e.Key == Key.V ||
                    e.Key == Key.X || e.Key == Key.Z)
                {
                    return;
                }
            }

            // Disallow all other keys
            if (!IsNumericKey(e.Key))
            {
                e.Handled = true;
            }
        }

        private static void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var text = (string)e.DataObject.GetData(typeof(string));
                var regex = new Regex(@"^\d*\.?\d*$");

                if (!regex.IsMatch(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private static void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            textBox?.SelectAll();
        }

        private static void TextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null && !textBox.IsKeyboardFocusWithin)
            {
                textBox.Focus();
                e.Handled = true;
            }
        }

        private static bool IsNumericKey(Key key)
        {
            return (key >= Key.D0 && key <= Key.D9) ||
                   (key >= Key.NumPad0 && key <= Key.NumPad9) ||
                   key == Key.Decimal || key == Key.OemPeriod;
        }

        #endregion
    }

    /// <summary>
    /// Behavior để tự động move focus khi nhấn Tab/Enter
    /// </summary>
    public static class AutoMoveFocusBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(AutoMoveFocusBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                if ((bool)e.NewValue)
                {
                    textBox.PreviewKeyDown += TextBox_PreviewKeyDown;
                }
                else
                {
                    textBox.PreviewKeyDown -= TextBox_PreviewKeyDown;
                }
            }
        }

        private static void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Tab)
            {
                var textBox = sender as TextBox;
                var direction = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift
                    ? FocusNavigationDirection.Previous
                    : FocusNavigationDirection.Next;

                var request = new TraversalRequest(direction);
                textBox?.MoveFocus(request);

                e.Handled = true;
            }
        }
    }
}
