#nullable enable
namespace UniT.ResourceManagement.Addressables.DI
{
    using InternalDI;

    public static class AddressablesManagersInternalDI
    {
        public static void AddAddressablesAssetManager(this DependencyContainer container, string? scope = null)
        {
            container.AddInterfaces<AddressablesAssetManager>(scope);
        }

        public static void AddAddressablesSceneManager(this DependencyContainer container)
        {
            container.AddInterfaces<AddressablesSceneManager>();
        }
    }
}