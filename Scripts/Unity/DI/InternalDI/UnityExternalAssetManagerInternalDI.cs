#nullable enable
namespace UniT.ResourceManagement.DI
{
    using InternalDI;

    public static class UnityExternalAssetManagerInternalDI
    {
        public static void AddUnityExternalAssetManager(this DependencyContainer container)
        {
            container.AddInterfaces<UnityExternalAssetManager>();
        }
    }
}