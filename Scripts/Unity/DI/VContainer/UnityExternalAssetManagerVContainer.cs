#nullable enable
namespace UniT.ResourceManagement.DI
{
    using VContainer;

    public static class UnityExternalAssetManagerVContainer
    {
        public static void RegisterUnityExternalAssetManager(this IContainerBuilder builder)
        {
            builder.Register<UnityExternalAssetManager>(Lifetime.Singleton).AsImplementedInterfaces();
        }
    }
}