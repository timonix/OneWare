﻿using Avalonia.Threading;
using OneWare.Essentials.Helpers;
using OneWare.Essentials.Models;
using OneWare.Essentials.Services;
using Prism.Ioc;

namespace OneWare.ProjectExplorer.Services;

public class ProjectWatchInstance : IDisposable
{
    private readonly IProjectExplorerService _projectExplorerService;
    private readonly IDockService _dockService;
    private readonly IWindowService _windowService;
    private readonly IProjectRoot _root;
    private readonly FileSystemWatcher _fileSystemWatcher;
    private readonly object _lock = new();
    private DispatcherTimer? _timer;
    private readonly Dictionary<string, List<FileSystemEventArgs>> _changes = new();

    public ProjectWatchInstance(IProjectRoot root, IProjectExplorerService projectExplorerService,
        IDockService dockService, ISettingsService settingsService, IWindowService windowService, ILogger logger)
    {
        _root = root;
        _projectExplorerService = projectExplorerService;
        _dockService = dockService;
        _windowService = windowService;

        _fileSystemWatcher = new FileSystemWatcher(root.FullPath)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
            IncludeSubdirectories = true
        };

        _fileSystemWatcher.Changed += File_Changed;
        _fileSystemWatcher.Deleted += File_Changed;
        _fileSystemWatcher.Renamed += File_Changed;
        _fileSystemWatcher.Created += File_Changed;

        try
        {
            settingsService.GetSettingObservable<bool>("Editor_DetectExternalChanges").Subscribe(x =>
            {
                _fileSystemWatcher.EnableRaisingEvents = x;

                _timer?.Stop();
                if (!x) return;
                _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(300), DispatcherPriority.Background, (_, _) =>
                {
                    lock (_lock)
                    {
                        ProcessChanges();
                    }
                });
                _timer.Start();
            });
        }
        catch (Exception e)
        {
            logger.Error(e.Message, e);
        }
    }

    private void File_Changed(object source, FileSystemEventArgs e)
    {
        if (e.Name == null) return;
        lock (_lock)
        {
            _changes.TryAdd(e.FullPath, new List<FileSystemEventArgs>());
            _changes[e.FullPath].Add(e);
        }
    }

    private void ProcessChanges()
    {
        foreach (var change in _changes)
        {
            _ = ProcessAsync(change.Key, change.Value);
        }

        //Task.WhenAll(_changes.Select(x => ProcessAsync(x.Key, x.Value)));
        _changes.Clear();
    }

    private async Task ProcessAsync(string path, IReadOnlyCollection<FileSystemEventArgs> changes)
    {
        try
        {
            FileAttributes attributes = FileAttributes.None;
            
            if (File.Exists(path) || Directory.Exists(path))
            {
                attributes = File.GetAttributes(path);
            }
            
            var entry = _root.Search(path);

            var lastArg = changes.Last();

            if (entry is not null)
            {
                switch (lastArg.ChangeType)
                {
                    case WatcherChangeTypes.Created:
                    case WatcherChangeTypes.Renamed:
                    case WatcherChangeTypes.Changed:
                        if (entry is ISavable savable)
                        {
                            var lastWriteTime = File.GetLastWriteTime(savable.FullPath);
                            if (lastWriteTime > savable.LastSaveTime)
                                await _projectExplorerService.ReloadAsync(entry);

                            if (savable is IProjectFile { Root: IProjectRootWithFile rootWithFile } &&
                                rootWithFile.ProjectFilePath == savable.FullPath && lastWriteTime > rootWithFile.LastSaveTime)
                            {
                                await _projectExplorerService.ReloadAsync(rootWithFile);
                            }
                        }
                        else await _projectExplorerService.ReloadAsync(entry);

                        return;
                    case WatcherChangeTypes.Deleted:
                        await _projectExplorerService.RemoveAsync(entry);
                        return;
                }
            }

            if (entry is null)
            {
                //Ignore change if parent folder is not included
                if (attributes.HasFlag(FileAttributes.Directory))
                {
                    var parentPath = Path.GetDirectoryName(path);
                    if (parentPath != null && !_root.IsPathIncluded(parentPath)) return;
                }
                
                if (_projectExplorerService.Items.FirstOrDefault(x =>
                        x is IProjectRootWithFile rootWithFile && rootWithFile.ProjectFilePath == path) is IProjectRoot root and ISavable savable)
                {
                    if (File.GetLastWriteTime(savable.FullPath) > savable.LastSaveTime)
                        await _projectExplorerService.ReloadAsync(root);
                }

                switch (lastArg.ChangeType)
                {
                    case WatcherChangeTypes.Renamed:
                        if (lastArg is RenamedEventArgs { Name: not null, OldFullPath: not null } renamedEventArgs &&
                            _root.Search(renamedEventArgs.OldFullPath) is { } oldEntry)
                        {
                            if (oldEntry is IProjectFile file)
                            {
                                _dockService.OpenFiles.TryGetValue(file, out var tab);
                                
                                await _projectExplorerService.RemoveAsync(oldEntry);
                                AddNew(path, attributes);
                                
                                //TODO dont remove tab and Initialize Current Tab
                            }
                            else
                            {
                                await _projectExplorerService.RemoveAsync(oldEntry);
                                AddNew(path, attributes);
                            }
                        }

                        return;
                    case WatcherChangeTypes.Created:
                    case WatcherChangeTypes.Changed when changes.Any(x => x.ChangeType is WatcherChangeTypes.Created):
                        AddNew(path, attributes);
                        return;
                    case WatcherChangeTypes.Deleted:
                        if(_root.Search(path) is {} deletedEntry)
                            await _projectExplorerService.RemoveAsync(deletedEntry);
                        return;
                }
            }
        }
        catch (Exception e)
        {
            ContainerLocator.Container.Resolve<ILogger>().Error(e.Message, e, false);
        }
    }

    private void AddNew(string path, FileAttributes attributes)
    {
        var relativePath = Path.GetRelativePath(_root.FullPath, path);
        
        if (attributes.HasFlag(FileAttributes.Directory))
        {
            var folder = _root.AddFolder(relativePath);
            ProjectHelper.ImportEntries(path, folder);
            if(folder.Items.Count == 0) folder.TopFolder!.Remove(folder);
            return;
        }
        
        if (!_root.IsPathIncluded(relativePath)) return;
        
        _root.AddFile(relativePath);
    }

    public void Dispose()
    {
        _timer?.Stop();
        _fileSystemWatcher.Dispose();
    }
}