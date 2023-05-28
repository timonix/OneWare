using OneWare.Core;
using OneWare.Core.Services;
using OneWare.Settings;
using OneWare.Shared.Services;
using Prism.Ioc;
using Prism.Modularity;

namespace OneWare.Demo;

public class DemoApp : App
{
    public static readonly ISettingsService SettingsService = new SettingsService();
    
    public static readonly IPaths Paths = new Paths("OneWare Studio", "avares://OneWare.Demo/Assets/icon.ico",
        "avares://OneWare.Demo/Assets/Startup.jpg");

    private static readonly ILogger Logger = new Logger(Paths);

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterInstance(SettingsService);
        containerRegistry.RegisterInstance(Paths);
        containerRegistry.RegisterInstance(Logger);
        
        base.RegisterTypes(containerRegistry);
    }

    public override void Initialize()
    {
        new ThemeManager(SettingsService, "avares://OneWare.Demo/Theme.axaml").Initialize(this);
        base.Initialize();
    }

    protected override IModuleCatalog CreateModuleCatalog()
    {
        return new DirectoryModuleCatalog()
        {
            ModulePath = Paths.ModulesPath
        };
    }
}