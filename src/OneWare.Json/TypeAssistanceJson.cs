﻿using OneWare.Json.Folding;
using OneWare.Json.Formatting;
using OneWare.Shared;
using OneWare.Shared.EditorExtensions;
using OneWare.Shared.LanguageService;

namespace OneWare.Json;

public class TypeAssistanceJson : TypeAssistance
{
    public TypeAssistanceJson(IEditor editor) : base(editor)
    {
        FoldingStrategy = new RegexFoldingStrategy(FoldingRegexJson.FoldingStart, FoldingRegexJson.FoldingEnd);
        FormattingStrategy = new JsonFormatter();
    }
}