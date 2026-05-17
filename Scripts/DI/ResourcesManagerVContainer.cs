#if UNIT_VCONTAINER
#nullable enable
namespace UniT.ResourceManagement.DI
{
    using UniT.Logging.DI;
    using VContainer;

    public static class ResourcesManagerVContainer
    {
        public static void RegisterAssetsManager(this IContainerBuilder builder, string? scope = null)
        {
            if (builder.Exists(typeof(IAssetsManager), true)) return;
            builder.RegisterLoggerManager();
            #if UNIT_ADDRESSABLES
            builder.Register<AddressableAssetsManager>(Lifetime.Singleton).WithParameter(scope).AsImplementedInterfaces();
            #else
            builder.Register<ResourceAssetsManager>(Lifetime.Singleton).WithParameter(scope).AsImplementedInterfaces();
            #endif
        }

        public static void RegisterScenesManager(this IContainerBuilder builder)
        {
            if (builder.Exists(typeof(IScenesManager), true)) return;
            builder.RegisterLoggerManager();
            #if UNIT_ADDRESSABLES
            builder.Register<AddressableScenesManager>(Lifetime.Singleton).AsImplementedInterfaces();
            #else
            builder.Register<ResourceScenesManager>(Lifetime.Singleton).AsImplementedInterfaces();
            #endif
        }

        public static void RegisterExternalAssetsManager(this IContainerBuilder builder)
        {
            if (builder.Exists(typeof(IExternalAssetsManager), true)) return;
            builder.RegisterLoggerManager();
            builder.Register<ExternalAssetsManager>(Lifetime.Singleton).AsImplementedInterfaces();
        }
    }
}
#endif