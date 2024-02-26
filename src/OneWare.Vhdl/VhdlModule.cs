﻿using OneWare.Essentials.Helpers;
using OneWare.Essentials.Models;
using OneWare.Essentials.Services;
using OneWare.UniversalFpgaProjectSystem.Services;
using OneWare.Vhdl.Parsing;
using Prism.Ioc;
using Prism.Modularity;

namespace OneWare.Vhdl;

public class VhdlModule : IModule
{
    public const string LspName = "RustHDL";
    public const string LspPathSetting = "VhdlModule_RustHdlPath";
    
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        var nativeToolService = containerProvider.Resolve<INativeToolService>();
        
        var nativeTool = nativeToolService.Register(LspName);
            
        nativeTool.AddPlatform(PlatformId.WinX64, "https://github.com/VHDL-LS/rust_hdl/releases/download/v0.77.0/vhdl_ls-x86_64-pc-windows-msvc.zip")
            .WithShortcut("LSP", Path.Combine("vhdl_ls-x86_64-pc-windows-msvc", "bin" , "vhdl_ls.exe"), LspPathSetting);
        
        nativeTool.AddPlatform(PlatformId.LinuxX64, "https://github.com/VHDL-LS/rust_hdl/releases/download/v0.77.0/vhdl_ls-x86_64-unknown-linux-gnu.zip")
            .WithShortcut("LSP", Path.Combine("vhdl_ls-x86_64-unknown-linux-gnu", "bin" , "vhdl_ls"), LspPathSetting);
        
        containerProvider.Resolve<ISettingsService>().RegisterTitledPath("Languages", "VHDL", LspPathSetting, "RustHDL Path", "Path for RustHDL executable", 
            nativeToolService.Get(LspName)!.GetShortcutPathOrEmpty("LSP"),
            null, containerProvider.Resolve<IPaths>().PackagesDirectory, File.Exists);
        
        containerProvider.Resolve<IErrorService>().RegisterErrorSource(LspName);
        containerProvider.Resolve<ILanguageManager>().RegisterTextMateLanguage("vhdl", "avares://OneWare.Vhdl/Assets/vhdl.tmLanguage.json", ".vhd", ".vhdl");
        containerProvider.Resolve<ILanguageManager>().RegisterService(typeof(LanguageServiceVhdl),true, ".vhd", ".vhdl");
        
        containerProvider.Resolve<FpgaService>().RegisterNodeProvider<VhdlNodeProvider>(".vhd", ".vhdl");
    }
}