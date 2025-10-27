using Eidos.Constants;

namespace Eidos.Services
{
    public enum ToastType
    {
        Success,
        Error,
        Warning,
        Info
    }

    public class ToastService
    {
        public event Action<string, ToastType, int>? OnShow;

        public void ShowSuccess(string message, int duration = AppConstants.Toast.SuccessDuration)
        {
            OnShow?.Invoke(message, ToastType.Success, duration);
        }

        public void ShowError(string message, int duration = AppConstants.Toast.ErrorDuration)
        {
            OnShow?.Invoke(message, ToastType.Error, duration);
        }

        public void ShowWarning(string message, int duration = AppConstants.Toast.WarningDuration)
        {
            OnShow?.Invoke(message, ToastType.Warning, duration);
        }

        public void ShowInfo(string message, int duration = AppConstants.Toast.InfoDuration)
        {
            OnShow?.Invoke(message, ToastType.Info, duration);
        }
    }
}
