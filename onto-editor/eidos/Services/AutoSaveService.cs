using Eidos.Models;
using Microsoft.Extensions.Logging;

namespace Eidos.Services
{
    /// <summary>
    /// Service for auto-saving notes with debouncing to prevent excessive save operations.
    /// Uses a timer-based approach to queue saves and execute them after a delay.
    /// </summary>
    public class AutoSaveService : IDisposable
    {
        private readonly NoteService _noteService;
        private readonly ILogger<AutoSaveService> _logger;
        private readonly Dictionary<int, Timer> _timers = new();
        private readonly Dictionary<int, AutoSaveStatus> _statuses = new();
        private readonly object _lock = new();
        private const int DEBOUNCE_MS = 2000; // 2 seconds

        public event EventHandler<AutoSaveStatusChangedEventArgs>? StatusChanged;

        public AutoSaveService(
            NoteService noteService,
            ILogger<AutoSaveService> logger)
        {
            _noteService = noteService;
            _logger = logger;
        }

        /// <summary>
        /// Queue a note for auto-save. If a save is already queued for this note,
        /// it will be cancelled and a new timer will be started.
        /// </summary>
        public void QueueSave(int noteId, string title, string content, string userId)
        {
            lock (_lock)
            {
                // Cancel existing timer if present
                if (_timers.TryGetValue(noteId, out var existingTimer))
                {
                    existingTimer.Dispose();
                    _timers.Remove(noteId);
                }

                // Update status to idle (waiting for debounce)
                UpdateStatus(noteId, AutoSaveStatus.Idle);

                // Create new timer
                var timer = new Timer(async _ =>
                {
                    await SaveNoteAsync(noteId, title, content, userId);
                }, null, DEBOUNCE_MS, Timeout.Infinite);

                _timers[noteId] = timer;
            }
        }

        /// <summary>
        /// Force immediate save for a note, bypassing debounce.
        /// Useful when navigating away or closing the application.
        /// </summary>
        public async Task ForceSaveAsync(int noteId, string title, string content, string userId)
        {
            lock (_lock)
            {
                // Cancel pending timer if exists
                if (_timers.TryGetValue(noteId, out var timer))
                {
                    timer.Dispose();
                    _timers.Remove(noteId);
                }
            }

            await SaveNoteAsync(noteId, title, content, userId);
        }

        /// <summary>
        /// Get the current auto-save status for a note
        /// </summary>
        public AutoSaveStatus GetStatus(int noteId)
        {
            lock (_lock)
            {
                return _statuses.TryGetValue(noteId, out var status) ? status : AutoSaveStatus.Idle;
            }
        }

        /// <summary>
        /// Clear auto-save state for a note (when note is closed/deleted)
        /// </summary>
        public void ClearNote(int noteId)
        {
            lock (_lock)
            {
                if (_timers.TryGetValue(noteId, out var timer))
                {
                    timer.Dispose();
                    _timers.Remove(noteId);
                }

                _statuses.Remove(noteId);
            }
        }

        private async Task SaveNoteAsync(int noteId, string title, string content, string userId)
        {
            try
            {
                UpdateStatus(noteId, AutoSaveStatus.Saving);

                // Update note content directly
                await _noteService.UpdateNoteContentAsync(noteId, userId, content);

                _logger.LogDebug("Auto-saved note {NoteId}", noteId);
                UpdateStatus(noteId, AutoSaveStatus.Saved);

                // Clear the timer from dictionary
                lock (_lock)
                {
                    if (_timers.TryGetValue(noteId, out var timer))
                    {
                        timer.Dispose();
                        _timers.Remove(noteId);
                    }
                }

                // After 3 seconds, reset status to Idle
                await Task.Delay(3000);
                UpdateStatus(noteId, AutoSaveStatus.Idle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Auto-save failed for note {NoteId}", noteId);
                UpdateStatus(noteId, AutoSaveStatus.Error);

                // Reset to Idle after 5 seconds on error
                await Task.Delay(5000);
                UpdateStatus(noteId, AutoSaveStatus.Idle);
            }
        }

        private void UpdateStatus(int noteId, AutoSaveStatus status)
        {
            lock (_lock)
            {
                _statuses[noteId] = status;
            }

            StatusChanged?.Invoke(this, new AutoSaveStatusChangedEventArgs(noteId, status));
        }

        public void Dispose()
        {
            lock (_lock)
            {
                foreach (var timer in _timers.Values)
                {
                    timer.Dispose();
                }
                _timers.Clear();
                _statuses.Clear();
            }
        }
    }

    /// <summary>
    /// Event args for auto-save status changes
    /// </summary>
    public class AutoSaveStatusChangedEventArgs : EventArgs
    {
        public int NoteId { get; }
        public AutoSaveStatus Status { get; }

        public AutoSaveStatusChangedEventArgs(int noteId, AutoSaveStatus status)
        {
            NoteId = noteId;
            Status = status;
        }
    }
}
