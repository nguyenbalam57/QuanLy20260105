using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ManagementFile.AdminManagement.ViewModels
{
    /// <summary>
    /// Base ViewModel implementing INotifyPropertyChanged
    /// </summary>
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Sets the property and raises PropertyChanged event if value changed
        /// </summary>
        /// <typeparam name="T">Type of the property</typeparam>
        /// <param name="field">Reference to the backing field</param>
        /// <param name="value">New value</param>
        /// <param name="propertyName">Property name (auto-filled by compiler)</param>
        /// <returns>True if value changed</returns>
        protected virtual bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Raises PropertyChanged event
        /// </summary>
        /// <param name="propertyName">Property name (auto-filled by compiler)</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Raises PropertyChanged for multiple properties
        /// </summary>
        /// <param name="propertyNames">Array of property names</param>
        protected virtual void OnPropertiesChanged(params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
            {
                OnPropertyChanged(propertyName);
            }
        }
    }
}