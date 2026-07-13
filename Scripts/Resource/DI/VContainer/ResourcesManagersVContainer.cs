#nullable enable
namespace UniT.ResourceManagement.DI
{
    using VContainer;

    public static class ResourcesManagersVContainer
    {
        public static void RegisterResourcesAssetManager(this IContainerBuilder builder, string? scope = null)
        {
            builder.Register<ResourcesAssetManager>(Lifetime.Singleton).WithParameter(scope).AsImplementedInterfaces();
        }

        public static void RegisterResourcesSceneManager(this IContainerBuilder builder)
        {
            builder.Register<ResourcesSceneManager>(Lifetime.Singleton).AsImplementedInterfaces();
        }
    }
}