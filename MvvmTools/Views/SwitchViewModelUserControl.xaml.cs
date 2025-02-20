﻿using MvvmTools.Commands;
using MvvmTools.Extensions;
using MvvmTools.Models;
using MvvmTools.Services;
using MvvmTools.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Community.VisualStudio.Toolkit;
using Unity;
using MessageBox = System.Windows.MessageBox;

namespace MvvmTools.Views;

/// <summary>
/// SwitchViewModelUserControl.xaml 的交互逻辑
/// </summary>
public partial class SwitchViewModelUserControl : UserControl
{
    public SwitchViewModelUserControl()
    {
        InitializeComponent();
        SolutionService = MvvmToolsPackage.Container.Resolve<ISolutionService>();
        Package = MvvmToolsPackage.Container.Resolve<IMvvmToolsPackage>();
        SettingsService = MvvmToolsPackage.Container.Resolve<ISettingsService>();
        Package.Ide.Events.WindowEvents.WindowActivated += OnWindowActivated;      
        this.DataContext = this;
    }

    private void OnWindowActivated(EnvDTE.Window GotFocus, EnvDTE.Window LostFocus)
    {
        Update(); 
    }
    public ISettingsService SettingsService { get; set; }

    public ISolutionService SolutionService { get; }
    public IMvvmToolsPackage Package { get; }

    private void SwitchButton_Click(object sender, RoutedEventArgs e)
    {
        var command = new GoToViewOrViewModelCommand(MvvmToolsPackage.Container);
        command.Invoke();
    }



    public FileType FileType
    {
        get => (FileType)GetValue(FileTypeProperty);
        set => SetValue(FileTypeProperty, value);
    }

    public static readonly DependencyProperty FileTypeProperty =
        DependencyProperty.Register(nameof(FileType), typeof(FileType), typeof(SwitchViewModelUserControl), new PropertyMetadata(default));

    private async Task Update()
    {
        var projectItem = Package.ActiveDocument.ProjectItem;
        var settings = await SettingsService.LoadSettings();
        var suffixes = settings.ViewSuffixes.Where(p => p != "");
        if(projectItem is null)
        {
            FileType = FileType.Other;
            return;
        }
        
        var isCodeBehind = projectItem.Name.EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase) ||
                           projectItem.Name.EndsWith(".xaml.vb", StringComparison.OrdinalIgnoreCase) ||
                           projectItem.Name.EndsWith(".axaml.cs", StringComparison.OrdinalIgnoreCase) ||
                           projectItem.Name.EndsWith(".axaml.vb", StringComparison.OrdinalIgnoreCase)||
                           (projectItem.Name.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)&suffixes.Any(p => projectItem.Name.Replace(".cs","").EndsWith(p)))
                           ;
        var isView = projectItem.Name.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) ||
                           projectItem.Name.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) ||
                           projectItem.Name.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase) ||
                           projectItem.Name.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase);

        var name1 = projectItem.Name.Split('.').First();

        if (name1.EndsWith("ViewModel"))
        {
            FileType = FileType.ViewModel;
        }
        else if (isView)
        {
            FileType = FileType.View;
        }
        else if(isCodeBehind)
        {
            FileType = FileType.CodeBehind;
        }
        else
        {
            FileType = FileType.Other;
        }
    }

    private async void ViewButton_Click(object sender, RoutedEventArgs e)
    {
        await Navigate(FileType.View);
    }

    private async void CodeBehindButton_Click(object sender, RoutedEventArgs e)
    {
        await Navigate(FileType.CodeBehind);
    }

    private async void ViewModelButton_Click(object sender, RoutedEventArgs e)
    {
        await Navigate(FileType.ViewModel);
    }
    private void PresentViewViewModelOptions(List<ProjectItemAndType> docs)
    {
        var window = new SelectFileDialog();
        var vm = new SelectFileDialogViewModel(docs, MvvmToolsPackage.Container);
        window.DataContext = vm;

        var result = window.ShowDialog();

        if (result.GetValueOrDefault())
        {
            // Go to the selected project item.
            VS.Documents.OpenInPreviewTabAsync(vm.SelectedDocument.ProjectItem.FileNames[1]);

        }
    }

    private async Task Navigate(FileType fileType)
    {
        try
        {
            var pi = Package.ActiveDocument?.ProjectItem;

            if (pi != null)
            {
                var classesInFile = SolutionService.GetClassesInProjectItem(pi);

                //if (classesInFile.Count == 0)
                //{
                //    MessageBox.Show("No classes found in file.", "MVVM Tools");
                //    return;
                //}

                var settings = await SettingsService.LoadSettings();

                // Solution not fully loaded so settings not loaded either.
                if (settings?.SolutionOptions == null)
                    return;

                List<ProjectItemAndType> docs;

                //如果当前项目为字典，classesInFile可能没有内容
                //这时候直接搜索pi.Name

                if (!settings.GoToViewOrViewModelSearchSolution)
                {
                    // ProjectModel from which to derive initial settings.
                    var settingsPm =
                        settings.ProjectOptions.FirstOrDefault(
                            p => p.ProjectModel.ProjectIdentifier == pi.ContainingProject.UniqueName);

                    // This shouldn't be null unless the user adds a new project and then
                    // quickly invokes this command, but better to check it.
                    if (settingsPm == null)
                        settingsPm = settings.SolutionOptions;

                    var viewModelLocationOptions = new LocationDescriptor
                    {
                        Namespace = settingsPm.ViewModelLocation.Namespace,
                        PathOffProject = settingsPm.ViewModelLocation.PathOffProject,
                        ProjectIdentifier = settingsPm.ViewModelLocation.ProjectIdentifier ?? pi.ContainingProject.UniqueName
                    };

                    var viewLocationOptions = new LocationDescriptor()
                    {
                        Namespace = settingsPm.ViewLocation.Namespace,
                        PathOffProject = settingsPm.ViewLocation.PathOffProject,
                        ProjectIdentifier = settingsPm.ViewLocation.ProjectIdentifier ?? pi.ContainingProject.UniqueName
                    };

                    docs = SolutionService.GetRelatedDocuments(
                        viewModelLocationOptions,
                        viewLocationOptions,
                        pi,
                        classesInFile.Select(c => c.Class),
                        new[] { "uc" },
                        settings.ViewSuffixes,
                        settingsPm.ViewModelSuffix);
                }
                else
                {
                    // Passing the first two parameters as null tells GetRelatedDocuments() to
                    // search the entire solution.
                    docs = SolutionService.GetRelatedDocuments(
                        null,
                        null,
                        pi,
                        classesInFile.Select(c => c.Class),
                        new[] { "uc" },
                        settings.ViewSuffixes,
                        settings.SolutionOptions.ViewModelSuffix,
                        true);
                }


                
                //无内容
                if (docs.Count == 0)
                {
                    var name1 = pi.Name.Split('.').First();
                    //再用当前项目名称搜一遍
                    docs = SolutionService.GetRelatedDocuments(
                        null,
                        null,
                        pi,
                        [name1],
                        new[] { "uc" },
                        settings.ViewSuffixes,
                        settings.SolutionOptions.ViewModelSuffix,
                        true);
                    if(docs.Count == 0)
                    {
                        string classes = "\n        ";
                        foreach (var c in classesInFile)
                            classes += c.Class + "\n        ";

                        MessageBox.Show(
                            $"Couldn't find any matching views or view models.\n\nClasses in this file:\n\n{classes}", "MVVM Tools");

                        return;
                    }
                }
                docs = docs.DistinctBy(p => p.ProjectItem.Name).ToList();
                if(fileType is FileType.View)
                {
                    var xamlDocs = docs.FindAll(d =>
                    d.ProjectItem.Name.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase)||
                    d.ProjectItem.Name.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase));
                    if (xamlDocs.Count > 1)
                    {
                        PresentViewViewModelOptions(xamlDocs);
                        return;
                    }
                    if (xamlDocs.Count == 0) return;
                    VS.Documents.OpenInPreviewTabAsync(xamlDocs[0].ProjectItem.FileNames[1]);

                }
                if (fileType is FileType.CodeBehind)
                {
                    var codeBehindDocs = docs.FindAll(d =>
                        (d.ProjectItem.Name.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)& classesInFile.Count == 0) ||
                        d.ProjectItem.Name.EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase)||
                        d.ProjectItem.Name.EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase) ||
                        d.ProjectItem.Name.EndsWith(".xaml.vb", StringComparison.OrdinalIgnoreCase) ||
                        d.ProjectItem.Name.EndsWith(".axaml.cs", StringComparison.OrdinalIgnoreCase) ||
                        d.ProjectItem.Name.EndsWith(".axaml.vb", StringComparison.OrdinalIgnoreCase));
                    //var list = docs.Select(p => p.ProjectItem.Name).ToList();
                    if (codeBehindDocs.Count > 1)
                    {
                        PresentViewViewModelOptions(codeBehindDocs);
                        return;
                    }
                    if (codeBehindDocs.Count == 0) return;

                    VS.Documents.OpenInPreviewTabAsync(codeBehindDocs[0].ProjectItem.FileNames[1]);

                }
                if (fileType is FileType.ViewModel)
                {
                    var viewModel = docs.FindAll(d =>
                        d.ProjectItem.Name.Contains("ViewModel"));
                    if (viewModel.Count > 1)
                    {
                        PresentViewViewModelOptions(viewModel);
                        return;
                    }
                    if (viewModel.Count == 0) return;

                    VS.Documents.OpenInPreviewTabAsync(viewModel[0].ProjectItem.FileNames[1]);
                }
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            //throw;
        }

    }

}
public static class EnumerableExtensions 
{
    public static IEnumerable<TSource> DistinctBy<TSource, TKey>
        (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
    {
        HashSet<TKey> seenKeys = new HashSet<TKey>();
        foreach (TSource element in source)
        {
            if (seenKeys.Add(keySelector(element)))
            {
                yield return element;
            }
        }
    }
}
public enum FileType
{
    View,
    CodeBehind,
    ViewModel,
    Other
}