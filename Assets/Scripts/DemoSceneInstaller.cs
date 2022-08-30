using UnityEngine;
using Zenject;

public class DemoSceneInstaller : MonoInstaller {
    public override void InstallBindings() {
        Container.Bind<IsoSpriteSortingManager>()
            .FromNewComponentOnNewGameObject()
            .AsSingle();
    }
}
