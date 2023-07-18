﻿using System.Runtime.Serialization;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using OneWare.Shared.Services;

namespace OneWare.Shared.Views;

public abstract class ExtendedDocument : Document, IExtendedDocument
{
    private readonly IProjectExplorerService _projectExplorerService;
    public IRelayCommand? Undo { get; protected set; }
    public IRelayCommand? Redo { get; protected set; }

    public IAsyncRelayCommand? TryClose { get; protected set; }

    private string _fullPath;

    [DataMember]
    public string FullPath
    {
        get => _fullPath;
        set
        {
            SetProperty(ref _fullPath, value);
            Id = $"Editor: {value}";
        }
    }

    private IFile? _currentFile;

    public IFile? CurrentFile
    {
        get => _currentFile;
        private set => SetProperty(ref _currentFile, value);
    }

    private bool _isLoading = true;

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private bool _loadingFailed;

    public bool LoadingFailed
    {
        get => _loadingFailed;
        set => SetProperty(ref _loadingFailed, value);
    }

    private bool _isReadOnly;

    public bool IsReadOnly
    {
        get => _isReadOnly;
        set => SetProperty(ref _isReadOnly, value);
    }

    private bool _isDirty;

    public bool IsDirty
    {
        get => _isDirty;
        set => SetProperty(ref _isDirty, value);
    }

    protected ExtendedDocument(string fullPath, IProjectExplorerService projectExplorerService)
    {
        _fullPath = fullPath;
        _projectExplorerService = projectExplorerService;
    }

    public virtual Task<bool> TryCloseAsync()
    {
        return Task.FromResult(true);
    }

    public virtual Task<bool> SaveAsync()
    {
        return Task.FromResult(true);
    }

    public virtual void InitializeContent()
    {
        var oldCurrentFile = CurrentFile;
        CurrentFile = _projectExplorerService.Search(FullPath) as IFile ?? new ExternalFile(FullPath);
        Title = CurrentFile is ExternalFile ? $"[{CurrentFile.Header}]" : CurrentFile.Header;
        ChangeCurrentFile(oldCurrentFile);
    }

    protected abstract void ChangeCurrentFile(IFile? oldFile);
}