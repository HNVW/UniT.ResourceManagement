#if UNIT_DI
#nullable enable
namespace UniT.ResourceManagement.DI
{
    using UniT.DI;
    using UniT.Logging.DI;

    public static class ResourcesManagerDI
    {
        public static void AddAssetsManager(this DependencyContainer container, string? scope = null)
        {
            if (container.Contains<IAssetsManager>()) return;
            container.AddLoggerManager();
            #if UNIT_ADDRESSABLES
            container.AddInterfaces<AddressableAssetsManager>(scope);
            #else
            container.AddInterfaces<ResourceAssetsManager>(scope);
            #endif
        }

        public static void AddScenesManager(this DependencyContainer container)
        {
            if (container.Contains<IScenesManager>()) return;
            container.AddLoggerManager();
            #if UNIT_ADDRESSABLES
            container.AddInterfaces<AddressableScenesManager>();
            #else
            container.AddInterfaces<ResourceScenesManager>();
            #endif
        }

        public static void AddExternalAssetsManager(this DependencyContainer container)
        {
            if (container.Contains<IExternalAssetsManager>()) return;
            container.AddLoggerManager();
            container.AddInterfaces<ExternalAssetsManager>();
        }
    }
}
#endif