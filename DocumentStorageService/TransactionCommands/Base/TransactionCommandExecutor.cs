namespace DocumentStorageService.Commands
{
	internal sealed class TransactionCommandExecutor : ITransactionCommandExecutor
	{
		/// <summary>
		/// Executes the full transaction command lifecycle: Execute, Commit and Rollback on failures.
		/// </summary>
		public async Task RunAsync(ITransactionCommand command)
		{
			if (command == null) throw new ArgumentNullException(nameof(command));

			// Execute phase: perform local persistence
			await command.ExecuteAsync().ConfigureAwait(false);

			// Commit phase: perform external side-effects (indexing). If commit fails, attempt rollback.
			try
			{
				await command.CommitAsync().ConfigureAwait(false);
			}
			catch
			{
				try
				{
					await command.RollbackAsync().ConfigureAwait(false);
				}
				catch (Exception rollbackEx)
				{
					// Log rollback failure and continue.
					System.Diagnostics.Trace.WriteLine($"Rollback failed: {rollbackEx}");
				}

				// rethrow original commit exception to let caller handle it
				throw;
			}
		}
	}
}