namespace Prefix.Poop.Interfaces;

internal interface IBaseInterface
{
    bool Init();

    void OnPostInit()
    {
    }

    void Shutdown()
    {
    }

    void OnAllSharpModulesLoaded()
    {
    }
}
