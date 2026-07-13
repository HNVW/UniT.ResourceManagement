#nullable enable
namespace UniT.ResourceManagement.DI
{
    using Zenject;

    public static class UnityExternalAssetManagerZenject
    {
        public static void BindUnityExternalAssetManager(this DiContainer container)
        {
            container.BindInterfacesTo<UnityExternalAssetManager>().AsSingle();
        }
    }
}