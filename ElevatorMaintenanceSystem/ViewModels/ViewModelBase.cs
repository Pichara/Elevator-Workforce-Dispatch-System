using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows;

namespace ElevatorMaintenanceSystem.ViewModels;

/// <summary>
/// Base ViewModel with IDisposable and memory leak prevention
/// </summary>
public abstract partial class ViewModelBase : ObservableObject, IDisposable
{
    private readonly List<IDisposable> _disposables = new();
    private bool _disposed;

    /// <summary>
    /// Add a disposable to the collection for automatic cleanup
    /// </summary>
    protected void AddDisposable(IDisposable disposable)
    {
        if (disposable == null) throw new ArgumentNullException(nameof(disposable));
        _disposables.Add(disposable);
    }

    /// <summary>
    /// Add an event handler with automatic cleanup
    /// </summary>
    protected void AddEventHandler<TEventArgs>(object source, EventHandler<TEventArgs> handler)
        where TEventArgs : EventArgs
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        source.GetType().GetEvent(handler.Method.Name.Substring(handler.Method.Name.IndexOf('_') + 1))?
            .AddEventHandler(source, handler);
    }

    /// <summary>
    /// Clean up all disposable resources
    /// </summary>
    public virtual void Dispose()
    {
        if (_disposed) return;

        foreach (var disposable in _disposables)
        {
            disposable?.Dispose();
        }
        _disposables.Clear();

        _disposed = true;
    }

    /// <summary>
    /// Bulk add items to ObservableCollection with UI thread marshalling
    /// </summary>
    protected void AddRange<T>(ObservableCollection<T> collection, IEnumerable<T> items)
    {
        if (collection == null) throw new ArgumentNullException(nameof(collection));
        if (items == null) throw new ArgumentNullException(nameof(items));

        foreach (var item in items)
        {
            collection.Add(item);
        }
    }
}
