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

        public void ShowSuccess(string message, int duration = 3000)
        {
            OnShow?.Invoke(message, ToastType.Success, duration);
        }

        public void ShowError(string message, int duration = 5000)
        {
            OnShow?.Invoke(message, ToastType.Error, duration);
        }

        public void ShowWarning(string message, int duration = 4000)
        {
            OnShow?.Invoke(message, ToastType.Warning, duration);
        }

        public void ShowInfo(string message, int duration = 3000)
        {
            OnShow?.Invoke(message, ToastType.Info, duration);
        }
    }
}
