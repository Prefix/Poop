using System;
using System.IO;
using System.Linq;
using Prefix.Poop.Extensions;
using Prefix.Poop.Interfaces;
using Prefix.Poop.Managers;
using Prefix.Poop.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sharp.Shared;

namespace Prefix.Poop;

public sealed class Poop : IModSharpModule
{
    public string DisplayName => "Poop";
    public string DisplayAuthor => "Prefix";

    private readonly ILogger<Poop> _logger;
    private readonly InterfaceBridge _bridge;
    private readonly ServiceProvider _serviceProvider;

    public Poop(ISharedSystem sharedSystem,
        string? dllPath,
        string? sharpPath,
        Version? version,
        IConfiguration? coreConfiguration,
        bool hotReload)
    {
        ArgumentNullException.ThrowIfNull(dllPath);
        ArgumentNullException.ThrowIfNull(sharpPath);
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(coreConfiguration);

        var bridge = new InterfaceBridge(dllPath, sharpPath, version, this, sharedSystem);

        // Try to load appsettings.json first (server config), fall back to appsettings.example.json
        var configFileName = "appsettings.json";
        var configPath = Path.Combine(dllPath, configFileName);

        if (!File.Exists(configPath))
        {
            configFileName = "appsettings.example.json";
            configPath = Path.Combine(dllPath, configFileName);
        }

        var configuration = new ConfigurationBuilder()
                            .AddJsonFile(configPath, false, false)
                            .Build();

        //sharedSystem.GetModSharp().GetGameData().Register("Poop.games");

        var services = new ServiceCollection();

        services.AddSingleton(bridge);
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(sharedSystem.GetLoggerFactory());

        ConfigureServices(services);

        _bridge = bridge;
        _logger = sharedSystem.GetLoggerFactory().CreateLogger<Poop>();

        _serviceProvider = services.BuildServiceProvider();
    }

    public bool Init()
    {
        var init = 0;

        var managers = CallInit<IManager>();

        if (managers > 0)
        {
            init += managers;
        }

        var modules = CallInit<IModule>();

        if (modules > 0)
        {
            init += modules;
        }

        return init == 0 ? throw new ApplicationException("No Modules") : true;
    }

    public void PostInit()
    {
        CallPostInit<IManager>();
        CallPostInit<IModule>();
    }

    public void Shutdown()
    {
        CallShutdown<IModule>();
        CallShutdown<IManager>();

        // You must unregister your game data when your module is unloaded.
        //_bridge.GameData.Unregister("Poop.games");
    }

    public void OnAllModulesLoaded()
    {
        CallOnAllSharpModulesLoaded<IManager>();
        CallOnAllSharpModulesLoaded<IModule>();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services
            .AddManagers() // Managers
            .AddModules(); // Modules
    }

    private int CallInit<T>() where T : IBaseInterface
    {
        var init = 0;
        var services = _serviceProvider.GetServices<T>().ToList();

        foreach (var service in services)
        {
            try
            {
                service.Init();
                init++;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while calling Init in {m}", service.GetType().Name);
            }

        }
        return init;
    }

    private void CallPostInit<T>() where T : IBaseInterface
    {
        foreach (var service in _serviceProvider.GetServices<T>())
        {
            try
            {
                service.OnPostInit();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while calling PostInit in {m}", service.GetType().Name);
            }
        }
    }

    private void CallShutdown<T>() where T : IBaseInterface
    {
        foreach (var service in _serviceProvider.GetServices<T>())
        {
            try
            {
                service.Shutdown();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while calling Shutdown in {m}", service.GetType().Name);
            }
        }
    }

    private void CallOnAllSharpModulesLoaded<T>() where T : IBaseInterface
    {
        foreach (var service in _serviceProvider.GetServices<T>())
        {
            try
            {
                service.OnAllSharpModulesLoaded();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while calling OnAllSharpModulesLoaded in {m}", service.GetType().Name);
            }
        }
    }
}
