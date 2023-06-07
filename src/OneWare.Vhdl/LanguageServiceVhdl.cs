﻿using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using Nerdbank.Streams;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OneWare.Shared;
using OneWare.Shared.LanguageService;
using OneWare.Shared.Models;
using OneWare.Shared.Services;
using Prism.Ioc;
using IFile = OneWare.Shared.IFile;

namespace OneWare.Vhdl
{
    public class LanguageServiceVhdl : LanguageService
    {
        public LanguageServiceVhdl(string workspace) : base ("VHDL LS", "/Users/hendrikmennen/VHDPlus/Packages/rusthdl/release/bin/vhdl_ls", null, workspace)
        {
            
        }
        
        public LanguageServiceVhdl(string workspace, string executablePath) : base ("VHDL LS", executablePath, null, workspace)
        {
            
        }
        
        public override ITypeAssistance GetTypeAssistance(IEditor editor)
        {
            return new TypeAssistanceVhdl(editor, this);
        }

        public override IEnumerable<ErrorListItemModel> ConvertErrors(PublishDiagnosticsParams pdp, IFile file)
        {
            if (file is IProjectFile pf && pf.TopFolder?.Search(Path.GetFileNameWithoutExtension(file.FullPath) + ".qip", false) != null)
                return new List<ErrorListItemModel>();

            return base.ConvertErrors(pdp, file);
        }
    }
}