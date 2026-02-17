using System;
using System.Windows.Input;

namespace RegulationAssistantChatClient.ViewModels
{
	/// <summary>
	/// A simple ICommand implementation that delegates execution and can-execute evaluation to provided delegates.
	/// Commonly used in MVVM for wiring UI commands to view model actions without creating many small command classes.
	/// </summary>
	public class RelayCommand : ICommand
	{
		private readonly Func<bool> canExecute;
		private readonly Action execute;

		/// <summary>
		/// Initializes a new instance of the <see cref="RelayCommand"/> class.
		/// </summary>
		/// <param name="execute">Action to invoke when the command is executed. Must not be null.</param>
		/// <param name="canExecute">Optional predicate used to determine whether the command can execute. When null the command is always executable.</param>
		public RelayCommand(Action execute, Func<bool> canExecute = null)
		{
			this.execute = execute;
			this.canExecute = canExecute;
		}

		/// <summary>
		/// Occurs when changes occur that affect whether or not the command should execute.
		/// UI frameworks such as WPF listen to this event to enable/disable bound controls.
		/// </summary>
		public event EventHandler CanExecuteChanged;

		/// <summary>
		/// Determines whether the command can execute in its current state by delegating to the <paramref name="_canExecute"/> predicate if provided.
		/// </summary>
		/// <param name="parameter">Unused. Parameter included to satisfy the <see cref="ICommand"/> signature.</param>
		/// <returns><c>true</c> when the command can execute; otherwise <c>false</c>.</returns>
		public bool CanExecute(object parameter)
		{
			return canExecute == null || canExecute();
		}

		/// <summary>
		/// Executes the command action.
		/// </summary>
		/// <param name="parameter">Unused. Parameter included to satisfy the <see cref="ICommand"/> signature.</param>
		public void Execute(object parameter)
		{
			execute();
		}

		/// <summary>
		/// Raises the <see cref="CanExecuteChanged"/> event to notify listeners that the executability of the command may have changed.
		/// Call this method after state changes that affect <see cref="CanExecute(object)"/>.
		/// </summary>
		public void RaiseCanExecuteChanged()
		{
			CanExecuteChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}