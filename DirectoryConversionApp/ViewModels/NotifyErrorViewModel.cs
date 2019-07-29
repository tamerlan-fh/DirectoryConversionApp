using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace DirectoryConversionApp.ViewModels
{
    public abstract class NotifyErrorViewModel : ViewModelBase, INotifyDataErrorInfo
    {
        #region events

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        protected void OnErrorsChanged(string property)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(property));
        }

        #endregion events

        protected readonly Dictionary<string, ICollection<string>> errors
            = new Dictionary<string, ICollection<string>>();

        public bool HasErrors
        {
            get { return errors.Any(); }
        }

        protected void AddError(string property, string error)
        {
            if (errors.TryGetValue(property, out ICollection<string> propertyErrors))
                propertyErrors.Add(error);
            else
                errors[property] = new List<string> { error };

            OnErrorsChanged(property);
        }

        protected void RemoveError(string property)
        {
            errors.Remove(property);
            OnErrorsChanged(property);
        }

        public IEnumerable GetErrors(string property)
        {
            if (!string.IsNullOrEmpty(property) && errors.TryGetValue(property, out ICollection<string> propertyErrors))
                return propertyErrors;

            return null;
        }
    }
}
