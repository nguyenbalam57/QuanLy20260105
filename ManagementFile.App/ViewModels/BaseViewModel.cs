using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace ManagementFile.App.ViewModels
{
    /// <summary>
    /// Base ViewModel với INotifyPropertyChanged implementation
    /// </summary>
    public abstract class BaseViewModel : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (object.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #region IDisposable

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    /// <summary>
    /// RelayCommand implementation - Single unified version
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            // Fix for C# 7.3 compatibility - explicitly create delegates
            Action<object> executeWrapper = null;
            if (execute != null)
            {
                executeWrapper = _ => execute();
            }

            Func<object, bool> canExecuteWrapper = null;
            if (canExecute != null)
            {
                canExecuteWrapper = _ => canExecute();
            }

            _execute = executeWrapper ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecuteWrapper;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute?.Invoke(parameter) ?? true;
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    /// <summary>
    /// AsyncRelayCommand for async operations - Single unified version
    /// </summary>
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<object, System.Threading.Tasks.Task> _execute;
        private readonly Func<object, bool> _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<object, System.Threading.Tasks.Task> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public AsyncRelayCommand(Func<System.Threading.Tasks.Task> execute, Func<bool> canExecute = null)
        {
            // Fix for C# 7.3 compatibility - explicitly create delegates
            Func<object, System.Threading.Tasks.Task> executeWrapper = null;
            if (execute != null)
            {
                executeWrapper = _ => execute();
            }

            Func<object, bool> canExecuteWrapper = null;
            if (canExecute != null)
            {
                canExecuteWrapper = _ => canExecute();
            }

            _execute = executeWrapper ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecuteWrapper;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);
        }

        public async void Execute(object parameter)
        {
            if (_isExecuting)
                return;

            _isExecuting = true;
            RaiseCanExecuteChanged();

            try
            {
                await _execute(parameter);
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    /// <summary>
    /// Generic AsyncRelayCommand with typed parameter - NEW ADDITION
    /// </summary>
    /// <typeparam name="T">Type of command parameter</typeparam>
    public class AsyncRelayCommand<T> : ICommand
    {
        private readonly Func<T, System.Threading.Tasks.Task> _execute;
        private readonly Func<T, bool> _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<T, System.Threading.Tasks.Task> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            try
            {
                if (_isExecuting)
                    return false;

                // Handle null parameter
                if (parameter == null)
                {
                    if (typeof(T).IsValueType && !IsNullable(typeof(T)))
                        return false;

                    return _canExecute?.Invoke(default(T)) ?? true;
                }

                // Check for MS.Internal.NamedObject which is a WPF internal binding placeholder
                if (parameter.GetType().FullName == "MS.Internal.NamedObject")
                {
                    System.Diagnostics.Debug.WriteLine($"[2025-10-09 06:32:37] AsyncRelayCommand: Received MS.Internal.NamedObject, treating as null");
                    return _canExecute?.Invoke(default(T)) ?? true;
                }

                // Safe type checking
                if (!CanSafelyCastToType(parameter, typeof(T)))
                {
                    System.Diagnostics.Debug.WriteLine($"[2025-10-09 06:32:37] AsyncRelayCommand: Cannot cast {parameter.GetType().Name} to {typeof(T).Name}");
                    return false;
                }

                return _canExecute?.Invoke((T)parameter) ?? true;
            }
            catch (InvalidCastException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[2025-10-09 06:32:37] AsyncRelayCommand InvalidCastException in CanExecute: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Parameter type: {parameter?.GetType().FullName ?? "null"}");
                System.Diagnostics.Debug.WriteLine($"Expected type: {typeof(T).FullName}");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[2025-10-09 06:32:37] AsyncRelayCommand unexpected error in CanExecute: {ex.Message}");
                return false;
            }
        }

        public async void Execute(object parameter)
        {
            if (_isExecuting || !CanExecute(parameter))
                return;

            try
            {
                _isExecuting = true;
                RaiseCanExecuteChanged();

                T typedParameter = default(T);

                if (parameter != null && parameter.GetType().FullName != "MS.Internal.NamedObject")
                {
                    if (CanSafelyCastToType(parameter, typeof(T)))
                    {
                        typedParameter = (T)parameter;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[2025-10-09 06:32:37] AsyncRelayCommand: Cannot execute - parameter type mismatch");
                        return;
                    }
                }

                await _execute(typedParameter);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[2025-10-09 06:32:37] AsyncRelayCommand error in Execute: {ex.Message}");

                // Show error to user
                if (Application.Current.MainWindow != null)
                {
                    MessageBox.Show($"Lỗi thực thi command: {ex.Message}", "Lỗi",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        private static bool IsNullable(Type type)
        {
            return Nullable.GetUnderlyingType(type) != null;
        }

        private static bool CanSafelyCastToType(object obj, Type targetType)
        {
            if (obj == null)
                return !targetType.IsValueType || IsNullable(targetType);

            // Special handling for WPF internal objects
            if (obj.GetType().FullName == "MS.Internal.NamedObject")
                return false;

            return targetType.IsAssignableFrom(obj.GetType());
        }
    }

    /// <summary>
    /// Generic RelayCommand with typed parameter - Single unified version
    /// </summary>
    /// <typeparam name="T">Type of command parameter</typeparam>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            try
            {
                if (parameter == null && typeof(T).IsValueType && !IsNullable(typeof(T)))
                    return false;

                // Safe casting with type checking
                if (parameter == null && !typeof(T).IsValueType)
                    return _canExecute?.Invoke(default(T)) ?? true;

                // Check if parameter can be cast to T
                if (parameter != null && !CanCastToType(parameter, typeof(T)))
                {
                    System.Diagnostics.Debug.WriteLine($"Cannot cast {parameter.GetType().Name} to {typeof(T).Name}");
                    return false;
                }

                return _canExecute?.Invoke((T)parameter) ?? true;
            }
            catch (InvalidCastException ex)
            {
                System.Diagnostics.Debug.WriteLine($"InvalidCastException in CanExecute: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Parameter type: {parameter?.GetType().Name ?? "null"}");
                System.Diagnostics.Debug.WriteLine($"Expected type: {typeof(T).Name}");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Unexpected error in CanExecute: {ex.Message}");
                return false;
            }
        }

        public void Execute(object parameter)
        {
            try
            {
                if (parameter == null && !typeof(T).IsValueType)
                {
                    _execute(default(T));
                    return;
                }

                // Safe casting for execution
                if (parameter != null && CanCastToType(parameter, typeof(T)))
                {
                    _execute((T)parameter);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Cannot execute command: parameter type mismatch");
                }
            }
            catch (InvalidCastException ex)
            {
                System.Diagnostics.Debug.WriteLine($"InvalidCastException in Execute: {ex.Message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error executing command: {ex.Message}");
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        private static bool IsNullable(Type type)
        {
            return Nullable.GetUnderlyingType(type) != null;
        }

        private static bool CanCastToType(object obj, Type targetType)
        {
            if (obj == null)
                return !targetType.IsValueType || IsNullable(targetType);

            return targetType.IsAssignableFrom(obj.GetType());
        }
    }
}