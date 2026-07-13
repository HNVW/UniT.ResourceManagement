#nullable enable
namespace UniT.ResourceManagement.DI
{
    using Zenject;

    public static class ResourcesManagersZenject
    {
        public static void BindResourcesAssetManager(this DiContainer container, string? scope = null)
        {
            container.BindInterfacesTo<ResourcesAssetManager>().AsSingle().WithArguments(scope);
        }

        public static void BindResourcesSceneManager(this DiContainer container)
        {
            container.BindInterfacesTo<ResourcesSceneManager>().AsSingle();
        }
    }
}