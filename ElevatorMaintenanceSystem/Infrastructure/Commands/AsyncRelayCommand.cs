using System.Windows.Input;

namespace ElevatorMaintenanceSystem.Infrastructure.Commands;

/// <summary>
/// Async implementation of ICommand for MVVM patterns
/// </summary>
public class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;

    public event EventHandler? CanExecuteChanged;

    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter)
    {
        return _canExecute == null || _canExecute();
    }

    public async void Execute(object? parameter)
    {
        await ExecuteAsync();
    }

    public async Task ExecuteAsync()
    {
        if (CanExecute(null))
        {
            await _execute();
        }
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// Async implementation of ICommand with parameter support
/// </summary>
public class AsyncRelayCommand<T> : ICommand
{
    private readonly Func<T?, Task> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public event EventHandler? CanExecuteChanged;

    public AsyncRelayCommand(Func<T?, Task> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter)
    {
        return _canExecute == null || _canExecute((T?)parameter);
    }

    public async void Execute(object? parameter)
    {
        await ExecuteAsync((T?)parameter);
    }

    public async Task ExecuteAsync(T? parameter)
    {
        if (CanExecute(parameter))
        {
            await _execute(parameter);
        }
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
