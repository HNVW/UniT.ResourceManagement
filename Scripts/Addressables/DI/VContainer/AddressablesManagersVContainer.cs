#nullable enable
namespace UniT.ResourceManagement.DI
{
    using VContainer;

    public static class AddressablesManagersVContainer
    {
        public static void RegisterAddressablesAssetManager(this IContainerBuilder builder, string? scope = null)
        {
            builder.Register<AddressablesAssetManager>(Lifetime.Singleton).WithParameter(scope).AsImplementedInterfaces();
        }

        public static void RegisterAddressablesSceneManager(this IContainerBuilder builder)
        {
            builder.Register<AddressablesSceneManager>(Lifetime.Singleton).AsImplementedInterfaces();
        }
    }
}