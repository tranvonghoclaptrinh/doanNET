using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QL_TrungTamNgoaiNgu.ViewModels
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        private bool _isBusy;
        private string _errorMessage;
        private string _toastMessage;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsBusy
        {
            get => _isBusy;
            protected set => SetProperty(ref _isBusy, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            protected set => SetProperty(ref _errorMessage, value);
        }

        public string ToastMessage
        {
            get => _toastMessage;
            protected set => SetProperty(ref _toastMessage, value);
        }

        protected bool SetProperty<T>(
            ref T backingStore,
            T value,
            [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
            {
                return false;
            }

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void SetSuccessToast(string message)
        {
            ErrorMessage = null;
            ToastMessage = message;
        }

        protected void SetErrorToast(Exception exception)
        {
            ErrorMessage = exception.Message;
            ToastMessage = exception.Message;
        }
    }
}
