using System.ComponentModel;

namespace Asv.Modeling;

/// <summary>
/// Defines a base contract for all view models in the application.
/// This interface provides a unique identifier, supports property change notifications,
/// and ensures proper disposal of resources.
/// </summary>
public interface IViewModel
    : IDisposable,
        INotifyPropertyChanging,
        INotifyPropertyChanged,
        ISupportParentChange<IViewModel>,
        ISupportRoutedEvents<IViewModel>,
        ISupportNavigation<IViewModel> { }
