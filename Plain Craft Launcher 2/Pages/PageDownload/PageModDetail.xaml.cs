using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using FluentValidation;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.UI.Hints;
using PCL.Core.Logging;
using PCL.Core.Minecraft.ResourceProject;
using PCL.Core.UI;
using PCL.Core.UI.Dialogs;
using PCL.Core.Utils;
using PCL.Core.Utils.Validate;
using PCL.Network;
using PCL.Network.Loaders;
using Pkl.Ckan;
using Control = System.Windows.Forms.Control;

namespace PCL;

public partial class PageModDetail : IPageView
{
    internal static string GetVersionGroupTitleForLog(string groupKey)
    {
        return (groupKey ?? "").Replace("\0", "/", StringComparison.Ordinal);
    }

    private readonly PageModDetailViewModel _vm = new();

    // 资源下载；整合包另存为
    public static Dictionary<Comp.CompType, string> cachedFolder = new(); // 仅在本次缓存的下载文件夹
    private MyCompItem _compItem;
    private bool _isFirstInit = true;
    private bool _refreshed;

    public void OnNavigatedTo(object? parameter) => RefreshOnNavigate();

    public void OnNavigatedFrom() { }

    private void RefreshOnNavigate()
    {
        if (_refreshed) return;
        _refreshed = true;
        Init();
    }

    private void Init()
    {
        Animations.AniBeginControl();
        var detailAdditional = Launcher.frmMain?.pageCurrent.additional;
        if (detailAdditional is null)
            return;
        _project = detailAdditional.Value.CompProject;
        PanBack.ScrollToHome();
        // 重启加载器
        if (_isFirstInit)
            // 在 Me.Initialized 已经初始化了加载器，不再重复初始化
            _isFirstInit = false;
        else
            PageLoaderRestart(isForceRestart: true);
        // 放置当前工程
        if (_compItem is not null)
            PanIntro.Children.Remove(_compItem);
        _compItem = _project.ToCompItem(false, false);
        _compItem.CanInteraction = false;
        _compItem.ShowFavoriteBtn = false;
        _compItem.Margin = new Thickness(-7, -7, 0d, 8d);
        PanIntro.Children.Insert(0, _compItem);

        // 决定按钮显示
        _vm.ApplyProjectState(_project);
        RefreshFavoriteButton();

        Animations.AniEndControl();
    }

    // 整合包安装：KSP 场景由 CKAN 安装计划统一处理，MC 整合包链路已剥离。
    public void Install_Click(MyListItem sender, EventArgs e)
    {
        Common.Log("[CKAN] 整合包安装在 KSP 场景下不可用");
    }

    // 世界存档下载：KSP 场景无对应资源类型，MC 链路已剥离。
    public void InstallWorld_Click(MyListItem sender, EventArgs e)
    {
        Common.Log("[CKAN] 世界存档下载在 KSP 场景下不可用");
    }

    public void Save_Click(object sender, EventArgs e)
    {
        var file = sender switch
        {
            FrameworkElement Element when Element.Tag is Comp.CompFile CompFile => CompFile,
            FrameworkElement Element when Element.Parent is FrameworkElement Parent && Parent.Tag is Comp.CompFile CompFile => CompFile,
            FrameworkElement Element when Element.Parent is FrameworkElement Parent && Parent.Parent is FrameworkElement GrandParent && GrandParent.Tag is Comp.CompFile CompFile => CompFile,
            _ => null
        };
        if (_project is null)
            return;

        Common.RunInNewThread(() =>
        {
            try
            {
                Common.Log($"[CKAN] 安装模组：{_project.TranslatedName} {file?.Version}");
                var plan = Launcher.Ckan.GetInstallPlanAsync(_project.Id).GetAwaiter().GetResult();
                if (plan is null)
                {
                    Common.Log($"[CKAN] 未找到模组 {_project.Id} 的安装计划", Common.LogLevel.Feedback,
                        userSummary: Lang.Text("Download.Comp.Error.OperationFailed"));
                    return;
                }
                if (!string.IsNullOrEmpty(plan.FailureReason))
                {
                    Common.Log($"[CKAN] {_project.Id} 安装计划不可用：{plan.FailureReason}", Common.LogLevel.Feedback,
                        userSummary: plan.FailureReason);
                    return;
                }

                var progress = new Progress<string>(msg => Common.Log($"[CKAN] {msg}"));
                Launcher.Ckan.ExecutePlanAsync(plan, progress).GetAwaiter().GetResult();

                Common.Log($"[CKAN] 已安装 {_project.TranslatedName}");
                Launcher.frmMain?.BtnExtraDownload.ShowRefresh();
                Launcher.frmMain?.BtnExtraDownload.Ribble();
            }
            catch (InstanceReadOnlyException ex)
            {
                Common.Log(
                    ex,
                    "[CKAN] 只读模式下无法安装",
                    Common.LogLevel.Feedback,
                    userSummary: ex.Message);
            }
            catch (Exception ex)
            {
                Common.Log(
                    ex,
                    "[CKAN] 模组安装失败",
                    Common.LogLevel.Feedback,
                    userSummary: Lang.Text("Download.Comp.Error.OperationFailed"));
            }
        }, "CKAN Install Mod");
    }
    private void BtnIntroWeb_Click(object sender, Common.RouteEventArgs e)
    {
        Common.OpenWebsite(_project.Website);
    }

    private void BtnIntroWiki_Click(object sender, Common.RouteEventArgs e)
    {
        // KSP / CKAN 模组无 MC 百科条目：CKAN 数据源下 Wiki 按钮已隐藏，点击兜底不跳转任何站点。
        Common.Log("[CKAN] 当前模组无可用 Wiki 条目");
    }

    private void BtnIntroCopy_Click(object sender, Common.RouteEventArgs e)
    {
        Common.ClipboardSet(_compItem.LabTitle.Text + _compItem.LabTitleRaw.Text);
    }

    private void BtnFavorites_Click(object sender, Common.RouteEventArgs e)
    {
        CompFavoritesUi.ShowMenu(_project, (UIElement)sender, RefreshFavoriteButton);
    }

    private void BtnIntroLinkCopy_Click(object sender, Common.RouteEventArgs e)
    {
        Comp.CompClipboard.currentText = _project.Website;
        Common.ClipboardSet(_project.Website);
    }

    // 翻译简介
    private async void BtnTranslate_Click(object sender, Common.RouteEventArgs e)
    {
        var control = sender as FrameworkElement;
        if (control is not null)
            control.IsEnabled = false;
        try
        {
            HintService.Show(Hints.Info(Lang.Text("Download.Comp.Detail.DescriptionTranslating", _project.TranslatedName)));
            var chineseDescription = await _project.ChineseDescription;
            if (chineseDescription is null)
                return;
            await DialogService.Instance.ShowAsync(new TextDialogRequest
            {
                Title = Lang.Text("Common.Dialog.Title"),
                Content = Lang.Text("Download.Comp.Detail.DescriptionTranslationResult",
                    _project.Description, chineseDescription),
                Buttons = DialogButtons.Ok,
                ForceWait = false
            });
        }
        catch (Exception ex)
        {
            Common.Log(
                ex,
                "翻译资源简介失败",
                Common.LogLevel.Feedback,
                userSummary: Lang.Text("Download.Comp.Error.OperationFailed"));
        }
        finally
        {
            if (control is not null)
                control.IsEnabled = true;
        }
    }

    /// <summary>
    ///     刷新收藏按钮的显示状态
    /// </summary>
    public void RefreshFavoriteButton()
    {
        try
        {
            if (_project is null) return;

            _vm.RefreshFavorite(_project.Id);
            BtnFavorites.SvgIcon = _vm.IsFavorite ? "lucide/heart-filled" : "lucide/heart";

            // 刷新顶部的项目卡片收藏状态
            if (_compItem is not null)
                _compItem.RefreshFavoriteStatus();
        }
        catch (Exception ex)
        {
            Common.Log(ex, "刷新收藏按钮状态时出错");
        }
    }

    #region 加载器

    private readonly Loading.LoaderTask<int, List<Comp.CompFile>> _compFileLoader;

    public PageModDetail()
    {
        _compFileLoader = new Loading.LoaderTask<int, List<Comp.CompFile>>("Comp File", task =>
        {
            LoadTargetFromAdditional();
            if (_project is null)
            {
                task.output = new List<Comp.CompFile>();
                return;
            }
            var versions = Launcher.Ckan.GetVersionsAsync(_project.Id).GetAwaiter().GetResult();
            var result = CkanCompAdapter.ToFiles(versions);
            if (task.IsAborted)
                return;
            task.output = result;
        });
        Initialized += PageModDetail_Inited;
        Loaded += (_, _) => LoadTargetFromAdditional();
        Unloaded += (_, _) => _refreshed = false;
        PageEnter += RefreshOnNavigate;
        InitializeComponent();
        DataContext = _vm;
        _vm.FilterSelectionChanged = UpdateFilterResult;
    }

    public PageModDetailViewModel ViewModel => _vm;

    // 初始化加载器信息
    private void PageModDetail_Inited(object sender, EventArgs e)
    {
        LoadTargetFromAdditional();
        PageLoaderInit(Load, PanLoad, PanMain, CardIntro, _compFileLoader, _ => Load_OnFinish());
    }

    public void LoadTargetFromAdditional()
    {
        var additional = Launcher.frmMain?.pageCurrent.additional;
        if (additional is null)
            return;
        var detailData = additional.Value;
        _project = detailData.CompProject;
        _targetInstance = detailData.TargetVersion;
        _targetLoader = detailData.TargetLoader;
        _pageType = detailData.ResourceType;
    }

    private Comp.CompProject _project;
    private string _targetInstance;
    private Comp.CompLoaderType _targetLoader;

    /// <summary>
    ///     当前页面应展示的内容类别。可能为 Any。
    /// </summary>
    private Comp.CompType _pageType;

    // 自动重试
    private void Load_State(object sender, LoadingState state, LoadingState oldState)
    {
        switch (_compFileLoader.State)
        {
            case LoadState.Failed:
            {
                var errorMessage = "";
                if (_compFileLoader.Error is not null)
                    errorMessage = _compFileLoader.Error.Message;
                if (errorMessage.Contains(Lang.Text("Common.Error.InvalidJson")))
                {
                    Common.Log("[Comp] 下载的文件 Json 列表损坏，已自动重试", Common.LogLevel.Debug);
                    PageLoaderRestart();
                }

                break;
            }
        }
    }

    private bool groupedDrop; // 是否按 Drop 筛选（1.21 / 1.20 / 1.19 / ...）而非小版本号（1.21.1 / 1.21 / 1.20.4 / ...）

    private bool groupedOld; // 是否折叠远古版本为一个选项

    private void Load_OnFinish()
    {
        var results = PageModDetailViewModel.FilterResults(_compFileLoader.output, _pageType);

        // 初始化筛选器
        List<string> instanceFilters = null;
        List<string> modLoaderFilters = null;

        void updateFilters()
        {
            instanceFilters = results.SelectMany(v => v.GameVersions)
                .Select(v => GetGroupedVersionName(v, groupedDrop, groupedOld)).Distinct()
                .OrderByDescending(s => s, new McVersionComparer.VersionComparer()).ToList();
            modLoaderFilters = results.SelectMany(v => v.ModLoaders).Select(l => l.ToString()).Distinct()
                .OrderByDescending(s => s).ToList();
        }

        ;

        // 确定分组方式
        groupedDrop = false;
        groupedOld = false;
        updateFilters();
        if (instanceFilters.Count >= 9)
        {
            groupedDrop = true;
            groupedOld = false;
            updateFilters();
            if (instanceFilters.Count >= 9)
            {
                groupedDrop = false;
                groupedOld = true;
                updateFilters();
                if (instanceFilters.Count >= 9)
                {
                    groupedDrop = true;
                    groupedOld = true;
                    updateFilters();
                }
            }
        }


        // UI 化筛选器
        _vm.InstanceFilterOptions.Clear();
        _vm.ModLoaderFilterOptions.Clear();
        _vm.InstanceFilterLabelChips.Clear();
        _vm.ModLoaderFilterLabelChips.Clear();
        if (_pageType == Comp.CompType.Mod)
        {
            PanInstanceFilterHost.Margin = new Thickness(10d, 10d, 0d, 5d);
            PanModLoaderFilter.Margin = new Thickness(10d, 5d, 0d, 10d);
        }
        else
        {
            PanInstanceFilterHost.Margin = new Thickness(10d, 10d, 0d, 10d);
            PanModLoaderFilter.Margin = new Thickness(0d);
        }

        _vm.SetFilterVisibility(instanceFilters.Count >= 2);
        if (instanceFilters.Count < 2)
        {
            _vm.SetInstanceFilter(null);
        }
        else
        {
            // 插入标签
            if (_pageType == Comp.CompType.Mod)
            {
                _vm.InstanceFilterLabelChips.Add(
                    new CompFilterChip(Lang.Text("Download.Comp.Detail.InstanceFilter")));
                _vm.ModLoaderFilterLabelChips.Add(
                    new CompFilterChip(Lang.Text("Download.Comp.Detail.LoaderFilter")));
            }

            instanceFilters.Insert(0, Lang.Text("Common.Option.All"));
            modLoaderFilters.Insert(0, Lang.Text("Common.Option.All"));
            // 转化为按钮
            foreach (var version in instanceFilters)
                _vm.InstanceFilterOptions.Add(new CompFilterOption(version));

            if (_pageType == Comp.CompType.Mod)
                foreach (var loader in modLoaderFilters)
                    _vm.ModLoaderFilterOptions.Add(new CompFilterOption(loader));

            // 自动选择
            CompFilterOption? instanceToCheck = null;
            CompFilterOption? modLoaderToCheck = null;
            if (!string.IsNullOrEmpty(_targetInstance))
            {
                var targetFile = results.FirstOrDefault(v => v.GameVersions.Contains(_targetInstance));
                if (targetFile is not null)
                {
                    var targetGroup = GetGroupedVersionName(_targetInstance, groupedDrop, groupedOld);
                    instanceToCheck = _vm.InstanceFilterOptions.FirstOrDefault(option =>
                        (option.Text ?? "") == (targetGroup ?? ""));
                }
            }

            if (_pageType == Comp.CompType.Mod)
                if (_targetLoader != Comp.CompLoaderType.Any)
                {
                    var targetFile = results.FirstOrDefault(v => v.ModLoaders.Contains(_targetLoader));
                    if (targetFile is not null)
                        modLoaderToCheck = _vm.InstanceFilterOptions.Skip(1).FirstOrDefault(option =>
                            (option.Text ?? "") == (_targetLoader.ToString() ?? ""));
                }

            // 注意：在 Mod 下加载器筛选项与实例筛选项分别存放
            instanceToCheck ??= _vm.InstanceFilterOptions.FirstOrDefault();
            if (_pageType == Comp.CompType.Mod)
                modLoaderToCheck ??= _vm.ModLoaderFilterOptions.FirstOrDefault();
            _vm.SetInstanceFilter(instanceToCheck);
            if (_pageType == Comp.CompType.Mod)
                _vm.SetModLoaderFilter(modLoaderToCheck);
        }

        // 更新筛选结果（文件列表 UI 化）
        UpdateFilterResult();
    }

    private void UpdateFilterResult()
    {
        var results = PageModDetailViewModel.FilterResults(_compFileLoader.output, _pageType);
        if (results is null)
            return;

        var instanceFilter = _vm.InstanceFilterValue;
        var modLoaderFilter = _vm.ModLoaderFilterValue;

        // 1. 预处理基础变量
        var targetVersionText = _targetLoader != Comp.CompLoaderType.Any ? _targetLoader + " " : "";
        var targetCardName = !string.IsNullOrEmpty(_targetInstance) || _targetLoader != Comp.CompLoaderType.Any
            ? Lang.Text("Download.Comp.Detail.SelectedVersion", targetVersionText, _targetInstance)
            : "";

        // 使用 HashSet 提高查询性能 O(1)
        var supportedLoaders =
            new HashSet<Comp.CompLoaderType>(Enum.GetValues(typeof(Comp.CompLoaderType))
                .Cast<Comp.CompLoaderType>());
        var ignoreQuilt = Config.Download.Comp.IgnoreQuilt;
        var hasMultipleLoaders = _project.ModLoaders.Count > 1;

        // 2. 核心数据归类 (使用 Dictionary 配合 HashSet 去重)
        var dict = new SortedDictionary<string, List<Comp.CompFile>>(
            new PageModDetailViewModel.CardSorter(targetCardName));
        dict.Add(Lang.Text("Download.Comp.Detail.VersionGroup.Other"), new List<Comp.CompFile>());

        // 用于记录每个卡片内已存在的 version，防止 Contains(version) 的 O(n) 消耗
        var versionDuplicateChecker = new Dictionary<string, HashSet<Comp.CompFile>>();

        foreach (var version in results)
        {
            // 处理普通卡片归类
            foreach (var gameVersion in version.GameVersions)
            {
                // 筛选器预检查
                var currentGroupedName = GetGroupedVersionName(gameVersion, groupedDrop, groupedOld);
                if (instanceFilter is not null && (currentGroupedName ?? "") != (instanceFilter ?? ""))
                    continue;
                var verName = GetGroupedVersionName(gameVersion, false, false);
                var loaders = new List<string>();

                // 判定 Loader 逻辑
                if (hasMultipleLoaders && version.Type == Comp.CompType.Mod &&
                    McInstanceInfo.IsFormatFit(verName))
                {
                    foreach (var loader in version.ModLoaders)
                    {
                        if (loader == Comp.CompLoaderType.Quilt && ignoreQuilt)
                            continue;
                        if (!supportedLoaders.Contains(loader))
                            continue;

                        // 模组加载器筛选器
                        if (modLoaderFilter is not null && (loader.ToString() ?? "") != (modLoaderFilter ?? ""))
                            continue;

                        loaders.Add(loader + " ");
                    }

                    if (loaders.Count == 0 && modLoaderFilter is not null) continue;
                }

                if (loaders.Count == 0)
                    loaders.Add("");

                // 填充数据
                foreach (var loaderPrefix in loaders)
                {
                    var targetKey = loaderPrefix + verName;
                    AddVersionToDict(dict, versionDuplicateChecker, targetKey, version);
                }
            }

            // 处理“所选版本”卡片 (逻辑合并，减少二次循环)
            if (!string.IsNullOrEmpty(targetCardName))
            {
                var isMatchFilter = instanceFilter is null ||
                                    GetGroupedVersionName(_targetInstance, groupedDrop, groupedOld)
                                        .StartsWithF(instanceFilter);

                if (isMatchFilter && version.GameVersions.Contains(_targetInstance))
                    if (_targetLoader == Comp.CompLoaderType.Any || version.ModLoaders.Contains(_targetLoader))
                        // 再次检查 version 是否符合筛选器（针对该文件的所有游戏版本）
                        if (instanceFilter is null || version.GameVersions.Any(v =>
                                (GetGroupedVersionName(v, groupedDrop, groupedOld) ?? "") == (instanceFilter ?? "")))
                            AddVersionToDict(dict, versionDuplicateChecker, targetCardName, version);
            }
        }

        // 3. 渲染 UI
        try
        {
            PanResults.Children.Clear();
            var additional = Launcher.frmMain.pageCurrent.additional;
            var additionalTitles = additional is not null
                ? additional.Value.ExpandedTitles
                : new List<string>();

            foreach (var pair in dict)
            {
                if (pair.Value.Count == 0)
                    continue;

                // 创建卡片组件
                var newCard = new MyCard
                {
                    Title = pair.Key,
                    Margin = new Thickness(0d, 0d, 0d, 15d)
                };

                // 闭包引用：避免在 Sub 内做高耗时操作
                var files = pair.Value;
                var currentKey = pair.Key;

                var newStack = new StackPanel
                {
                    Margin = new Thickness(20d, MyCard.SwapedHeight, 18d, 0d),
                    VerticalAlignment = VerticalAlignment.Top,
                    Tag = files
                };

                newCard.Children.Add(newStack);
                newCard.SwapControl = newStack;

                // 延迟加载安装项的逻辑
                newCard.InstallMethod = stack =>
                {
                    var list = (List<Comp.CompFile>)stack.Tag;
                    // 排序和去重检查
                    list.Sort((a, b) => b.ReleaseDate.CompareTo(a.ReleaseDate));
                    var distinctCount = list.Select(f => f.DisplayName).Distinct().Count();
                    var badDisplayName = distinctCount != list.Count;

                    // 批量添加子项
                    switch (_project.Type)
                    {
                        case Comp.CompType.ModPack:
                        {
                            foreach (var item in list)
                                stack.Children.Add(item.ToListItem(
                                    (sender, e) => Launcher.frmDownloadCompDetail.Install_Click((MyListItem)sender, e),
                                    Launcher.frmDownloadCompDetail.Save_Click, badDisplayName));
                            break;
                        }
                        case Comp.CompType.World:
                        {
                            foreach (var item in list)
                                stack.Children.Add(item.ToListItem(
                                    (sender, e) =>
                                        Launcher.frmDownloadCompDetail.InstallWorld_Click((MyListItem)sender, e),
                                    Launcher.frmDownloadCompDetail.Save_Click, badDisplayName));
                            break;
                        }

                        default:
                        {
                            CompCardUi.CompFilesCardPreload(stack, list);
                            foreach (var item in list)
                                stack.Children.Add(item.ToListItem(Launcher.frmDownloadCompDetail.Save_Click,
                                    badDisplayName: badDisplayName));
                            break;
                        }
                    }
                };

                PanResults.Children.Add(newCard);

                // 展开逻辑
                if ((currentKey ?? "") == (targetCardName ?? "") || additionalTitles.Contains(newCard.Title))
                    newCard.StackInstall();
                else
                    newCard.IsSwapped = true;

                // 特殊提示
                if (currentKey == Lang.Text("Download.Comp.Detail.VersionGroup.Other"))
                    newStack.Children.Add(new MyHint
                    {
                        Text = Lang.Text("Download.Comp.Detail.VersionRecognitionDelayHint"),
                        Theme = MyHint.Themes.Yellow,
                        Margin = new Thickness(5d, 0d, 0d, 8d)
                    });
            }

            // 单卡片自动展开
            if (PanResults.Children.Count == 1) ((MyCard)PanResults.Children[0]).IsSwapped = false;
        }

        catch (Exception ex)
        {
            Common.Log(
                ex,
                "可视化工程下载列表出错",
                Common.LogLevel.Feedback,
                userSummary: Lang.Text("Download.Comp.Error.OperationFailed"));
        }
    }

    /// <summary>
    ///     辅助方法：向字典添加数据并处理去重
    /// </summary>
    private void AddVersionToDict(SortedDictionary<string, List<Comp.CompFile>> dict,
        Dictionary<string, HashSet<Comp.CompFile>> checker, string key, Comp.CompFile version)
    {
        if (!dict.ContainsKey(key))
        {
            dict.Add(key, new List<Comp.CompFile>());
            checker.Add(key, new HashSet<Comp.CompFile>());
        }

        // 使用 HashSet.Add 判断是否重复，比 List.Contains 快得多
        if (checker[key].Add(version)) dict[key].Add(version);
    }

    private string GetGroupedVersionName(string name, bool groupedByDrop, bool foldOld)
    {
        if (name is null)
            return Lang.Text("Download.Comp.Detail.VersionGroup.Other");
        if (name.Contains('w'))
            return Lang.Text("Download.Comp.Detail.VersionGroup.Snapshot");
        if (foldOld && McInstanceInfo.VersionToDrop(name, true) < 120)
            return Lang.Text("Download.Comp.Detail.VersionGroup.Old");
        if (groupedByDrop)
        {
            var drop = McInstanceInfo.VersionToDrop(name, true);
            if (drop >= 0)
                return McInstanceInfo.DropToVersion(drop);
            return name;
        }

        return name;
    }

    #endregion
}

/// <summary>为筛选器模板中的单选框提供内部文本边距的附加属性。</summary>
public static class CompFilterRadioButton
{
    public static readonly DependencyProperty TextMarginProperty = DependencyProperty.RegisterAttached(
        "TextMargin", typeof(Thickness), typeof(CompFilterRadioButton),
        new FrameworkPropertyMetadata(new Thickness(), OnTextMarginChanged));

    private static void OnTextMarginChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MyRadioButton radioButton && radioButton.LabText is not null)
            radioButton.LabText.Margin = (Thickness)e.NewValue;
    }

    public static Thickness GetTextMargin(DependencyObject element)
    {
        return (Thickness)element.GetValue(TextMarginProperty);
    }

    public static void SetTextMargin(DependencyObject element, Thickness value)
    {
        element.SetValue(TextMarginProperty, value);
    }
}

