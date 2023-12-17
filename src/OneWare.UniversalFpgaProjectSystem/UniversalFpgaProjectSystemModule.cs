﻿using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using DynamicData.Binding;
using OneWare.SDK.Converters;
using OneWare.SDK.Models;
using OneWare.SDK.Services;
using OneWare.SDK.ViewModels;
using OneWare.UniversalFpgaProjectSystem.Models;
using OneWare.UniversalFpgaProjectSystem.Services;
using OneWare.UniversalFpgaProjectSystem.ViewModels;
using OneWare.UniversalFpgaProjectSystem.Views;
using Prism.Ioc;
using Prism.Modularity;

namespace OneWare.UniversalFpgaProjectSystem;

public class UniversalFpgaProjectSystemModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterSingleton<UniversalFpgaProjectManager>();
        containerRegistry.RegisterSingleton<FpgaService>();
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        var manager = containerProvider.Resolve<UniversalFpgaProjectManager>();
        var windowService = containerProvider.Resolve<IWindowService>();
        containerProvider
            .Resolve<IProjectManagerService>()
            .RegisterProjectManager(UniversalFpgaProjectRoot.ProjectType, manager);
        
        containerProvider.Resolve<ILanguageManager>().RegisterLanguageExtensionLink(UniversalFpgaProjectRoot.ProjectFileExtension, ".json");

        windowService.RegisterMenuItem("MainWindow_MainMenu/File/New",
            new MenuItemViewModel("FpgaProject")
            {
                Header = "Project",
                Command = new AsyncRelayCommand(() => _ = manager.NewProjectDialogAsync()),
                Icon = SharedConverters.PathToBitmapConverter.Convert(ContainerLocator.Container.Resolve<IPaths>().AppIconPath, typeof(Bitmap), null, null) as Bitmap
            });
        
        windowService.RegisterMenuItem("MainWindow_MainMenu/File/Open",
            new MenuItemViewModel("FpgaProject")
            {
                Header = "Project",
                Command = new AsyncRelayCommand(() => containerProvider.Resolve<IProjectExplorerService>().LoadProjectFileDialogAsync(manager, 
                    new FilePickerFileType($"Universal FPGA Project (*.{UniversalFpgaProjectRoot.ProjectFileExtension})")
                {
                    Patterns = new[] { $"*{UniversalFpgaProjectRoot.ProjectFileExtension}" }
                })),
                Icon = SharedConverters.PathToBitmapConverter.Convert(ContainerLocator.Container.Resolve<IPaths>().AppIconPath, typeof(Bitmap), null, null) as Bitmap
            });

        var toolBarViewModel = containerProvider.Resolve<UniversalFpgaProjectToolBarViewModel>();
            
        var toolBarExtension = new UniversalFpgaProjectToolBarView()
        {
            DataContext = toolBarViewModel
        };
        
        windowService.RegisterMenuItem("MainWindow_MainMenu",
            new MenuItemViewModel("FPGA")
            {
                Header = "FPGA",
                Priority = 200,
            });
        
        windowService.RegisterMenuItem("MainWindow_MainMenu/FPGA",
            [
                new MenuItemViewModel("Download")
                {
                    Header = "Download",
                    Command = new AsyncRelayCommand(() => toolBarViewModel.DownloadAsync()),
                    IconObservable = Application.Current!.GetResourceObservable("VsImageLib.Download16X")
                },
                new MenuItemViewModel("Compile")
                {
                    Header = "Compile",
                    Command = new AsyncRelayCommand(() => toolBarViewModel.CompileAsync()),
                    IconObservable = Application.Current!.GetResourceObservable("CreateIcon")
                },
            ]);

        toolBarExtension.Bind(Visual.IsVisibleProperty, containerProvider
            .Resolve<IProjectExplorerService>()
            .WhenValueChanged(x => x.ActiveProject)
            .Select(x => x is UniversalFpgaProjectRoot));
        
        windowService.RegisterUiExtension("MainWindow_RoundToolBarExtension", toolBarExtension);
    }
}