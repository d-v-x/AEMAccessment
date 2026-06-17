namespace AEMAccessment.IService
{
    public interface IApiSyncService
    {
        Task<string?> LoginAsync();

        Task SyncPlatformWellActualAsync();

        Task SyncPlatformWellDummyAsync();
    }
}
