namespace DocumentStorageService.Commands
{
	internal interface ITransactionCommandExecutor
	{
		Task RunAsync(ITransactionCommand command);
	}
}