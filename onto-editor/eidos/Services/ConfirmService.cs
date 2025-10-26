namespace Eidos.Services
{
    public enum ConfirmType
    {
        Danger,
        Warning,
        Info
    }

    public class ConfirmService
    {
        public event Action<string, string, string, ConfirmType, TaskCompletionSource<bool>>? OnShow;

        public Task<bool> ShowAsync(string title, string message, string confirmText = "Confirm", ConfirmType type = ConfirmType.Danger)
        {
            var tcs = new TaskCompletionSource<bool>();
            OnShow?.Invoke(title, message, confirmText, type, tcs);
            return tcs.Task;
        }
    }
}
