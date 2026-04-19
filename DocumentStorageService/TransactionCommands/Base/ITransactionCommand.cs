using CommonSDK;

namespace DocumentStorageService.Commands
{
	internal interface ITransactionCommand
	{
		IJsonSerializableRequest? Request { get; }
		IJsonSerializableResponse? Result { get; }

		Task CommitAsync();

		Task ExecuteAsync();

		Task RollbackAsync();
	}
}