#nullable enable
namespace UniT.ResourceManagement.DI
{
    using Zenject;

    public static class AddressablesManagersZenject
    {
        public static void BindAddressablesAssetManager(this DiContainer container, string? scope = null)
        {
            container.BindInterfacesTo<AddressablesAssetManager>().AsSingle().WithArguments(scope);
        }

        public static void BindAddressablesSceneManager(this DiContainer container)
        {
            container.BindInterfacesTo<AddressablesSceneManager>().AsSingle();
        }
    }
}