using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PCL;

public partial class MyCompItem
{
    private readonly MyCompItemViewModel _vm = new();

    public MyCompItemViewModel ViewModel => _vm;

    private string _stateLast;

    public bool CanInteraction { get; set; } = true;

    public void RefreshColor(object sender, EventArgs e)
    {
        if (!CanInteraction)
            return;
        string stateNew;
        int time;
        if (IsMouseOver)
        {
            if (_isMouseDown)
            {
                stateNew = "MouseDown";
                time = 120;
            }
            else
            {
                stateNew = "MouseOver";
                time = 120;
            }
        }
        else
        {
            stateNew = "Idle";
            time = 180;
        }

        if ((_stateLast ?? "") == (stateNew ?? ""))
            return;
        _stateLast = stateNew;

        if (IsLoaded && !Animations.AniExitsAnyControl())
        {
            var ani = new List<Animations.AniData>();
            if (IsMouseOver)
            {
                if (PanButtons is not null && _HasActionButtons)
                    ani.Add(Animations.AaOpacity(PanButtons, 1d - PanButtons.Opacity,
                        (int)Math.Round(time * 0.35d),
                        (int)Math.Round(time * 0.15d)));
                ani.AddRange(new[]
                {
                    Animations.AaColor(RectBack, Border.BackgroundProperty,
                        _isMouseDown ? "ColorBrush6" : "ColorBrushBg1", time),
                    Animations.AaOpacity(RectBack, 1d - RectBack.Opacity, time,
                        ease: new Animations.AniEaseOutFluent())
                });
                if (_isMouseDown)
                    ani.Add(Animations.AaScaleTransform(RectBack,
                        0.996d - ((ScaleTransform)RectBack.RenderTransform).ScaleX, (int)Math.Round(time * 1.2d),
                        ease: new Animations.AniEaseOutFluent()));
                else
                    ani.Add(Animations.AaScaleTransform(RectBack,
                        1d - ((ScaleTransform)RectBack.RenderTransform).ScaleX, (int)Math.Round(time * 1.2d),
                        ease: new Animations.AniEaseOutFluent()));
            }
            else
            {
                if (PanButtons is not null && _HasActionButtons)
                    ani.Add(Animations.AaOpacity(PanButtons, -PanButtons.Opacity,
                        (int)Math.Round(time * 0.4d)));
                ani.AddRange(new[]
                {
                    Animations.AaOpacity(RectBack, -RectBack.Opacity, time),
                    Animations.AaColor(RectBack, Border.BackgroundProperty,
                        _isMouseDown ? "ColorBrush6" : "ColorBrush7", time),
                    Animations.AaScaleTransform(RectBack,
                        0.996d - ((ScaleTransform)RectBack.RenderTransform).ScaleX,
                        time, ease: new Animations.AniEaseOutFluent()),
                    Animations.AaScaleTransform(RectBack, -0.196d, 1, after: true)
                });
            }

            Animations.AniStart(ani, "CompItem Color " + Uuid);
        }
        else
        {
            Animations.AniStop("CompItem Color " + Uuid);
            if (_rectBack is not null)
                RectBack.Opacity = 0d;
            if (PanButtons is not null)
                PanButtons.Opacity = 0d;
        }
    }

    #region 基础属性

    public int Uuid = Common.GetUuid();

    public string Logo
    {
        get => PathLogo.Source;
        set => PathLogo.Source = value;
    }

    public string Title
    {
        get => _vm.TitleText;
        set
        {
            if ((_vm.TitleText ?? "") == (value ?? ""))
                return;
            _vm.TitleText = value;
        }
    }

    public string SubTitle
    {
        get => _vm.SubTitleText;
        set
        {
            if ((_vm.SubTitleText ?? "") == (value ?? ""))
                return;
            _vm.SubTitleText = value;
            _vm.SubTitleVisibility = string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    public string Description
    {
        get => _vm.DescriptionText;
        set
        {
            if ((_vm.DescriptionText ?? "") == (value ?? ""))
                return;
            _vm.DescriptionText = value;
        }
    }

    public string Version
    {
        get => LabVersion.Text;
        set => LabVersion.Text = value;
    }

    public string Downloads
    {
        get => LabDownload.Text;
        set => LabDownload.Text = value;
    }

    public string UpdateTime
    {
        get => LabTime.Text;
        set => LabTime.Text = value;
    }

    public string Source
    {
        get => LabSource.Text;
        set => LabSource.Text = value;
    }

    public MyCompItem()
    {
        InitializeComponent();
        Click += (sender, e) => MyCompItem_Click((MyCompItem)sender, e);
        PreviewMouseLeftButtonUp += Button_MouseUp;
        PreviewMouseLeftButtonDown += Button_MouseDown;
        MouseLeave += Button_MouseLeave;
        PreviewMouseLeftButtonUp += Button_MouseLeave;
        MouseEnter += RefreshColor;
        MouseLeave += RefreshColor;
        MouseLeftButtonDown += RefreshColor;
        MouseLeftButtonUp += RefreshColor;
    }

    private void LabInfo_MouseEnter(object sender, MouseEventArgs e)
    {
        if (IsTextTrimmed(LabInfo))
        {
            ToolTipInfo.Content = LabInfo.Text;
            ToolTipInfo.Width = LabInfo.ActualWidth + 25d;
            LabInfo.ToolTip = ToolTipInfo;
        }
        else
        {
            LabInfo.ToolTip = null;
        }
    }

    private bool IsTextTrimmed(TextBlock textBlock)
    {
        var typeface = new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight,
            textBlock.FontStretch);
        var formattedText = new FormattedText(textBlock.Text, Thread.CurrentThread.CurrentCulture,
            textBlock.FlowDirection, typeface, textBlock.FontSize, textBlock.Foreground,
            Common.Dpi);
        return formattedText.Width > textBlock.ActualWidth;
    }

    public List<string> Tags
    {
        set => TagPanelHelper.FillLocalized(
            PanTags,
            value,
            new SolidColorBrush(Color.FromRgb(134, 134, 134)));
    }

    public bool ShowFavoriteBtn
    {
        get => BtnDelete.Visibility == Visibility.Visible;
        set
        {
            BtnDelete.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            _UpdatePanButtons();
        }
    }

    public bool ShowDownloadBtn
    {
        get => BtnDownload.Visibility == Visibility.Visible;
        set
        {
            BtnDownload.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            _UpdatePanButtons();
        }
    }

    private bool _HasActionButtons => ShowFavoriteBtn || ShowDownloadBtn;

    private void _UpdatePanButtons()
    {
        if (PanButtons is null) return;
        PanButtons.Visibility = _HasActionButtons ? Visibility.Visible : Visibility.Collapsed;
    }

    public void RefreshFavoriteStatus()
    {
    }
    #endregion

    #region 点击

    public event ClickEventHandler? Click;

    public delegate void ClickEventHandler(object sender, MouseButtonEventArgs e);

    private void BtnDelete_Click(object sender, EventArgs e)
    {
    }

    private void _BtnDownload_Click(object sender, EventArgs e)
    {
    }

    private void MyCompItem_Click(MyCompItem sender, EventArgs e)
    {
        if (Tag is ModItem mod)
        {
            PCL.Core.UI.Hints.HintService.Show(PCL.Core.UI.Hints.Hints.Info($"点击了模组: {mod.Name}", log: false));
        }
    }

    private bool _isMouseDown;

    private void Button_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isMouseDown)
            return;
        Click?.Invoke(sender, e);
    }

    private void Button_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!CanInteraction)
            return;
        var clickPosition = e.GetPosition(this);
        var isClickOnButton = false;

        if (PanButtons.Visibility == Visibility.Visible)
            isClickOnButton = _IsClickOnActionButton(BtnDelete, clickPosition) ||
                              _IsClickOnActionButton(BtnDownload, clickPosition);

        if (isClickOnButton) return;

        var isClickOnLabInfo = false;
        if (LabInfo.Visibility == Visibility.Visible)
        {
            var labInfoBounds = new Rect(LabInfo.TranslatePoint(new Point(0d, 0d), this), LabInfo.RenderSize);
            isClickOnLabInfo = labInfoBounds.Contains(clickPosition);
        }

        if (IsMouseDirectlyOver || isClickOnLabInfo) _isMouseDown = true;
    }

    private void Button_MouseLeave(object sender, object e)
    {
        _isMouseDown = false;
    }

    private bool _IsClickOnActionButton(FrameworkElement button, Point clickPosition)
    {
        if (button is null || button.Visibility != Visibility.Visible) return false;
        var bounds = new Rect(button.TranslatePoint(new Point(0d, 0d), this), button.RenderSize);
        return bounds.Contains(clickPosition);
    }

    #endregion

    #region 后加载指向背景

    private Border _rectBack;

    public Border RectBack
    {
        get
        {
            if (_rectBack is null)
            {
                var rect = new Border
                {
                    Name = "RectBack",
                    CornerRadius = new CornerRadius(3d),
                    RenderTransform = new ScaleTransform(0.8d, 0.8d),
                    RenderTransformOrigin = new Point(0.5d, 0.5d),
                    BorderThickness = new Thickness(Common.GetWpfSize(1d)),
                    SnapsToDevicePixels = true,
                    IsHitTestVisible = false,
                    Opacity = 0d
                };
                rect.SetResourceReference(Border.BackgroundProperty, "ColorBrush7");
                rect.SetResourceReference(Border.BorderBrushProperty, "ColorBrush6");
                SetColumnSpan(rect, 999);
                SetRowSpan(rect, 999);
                Children.Insert(0, rect);
                _rectBack = rect;
            }

            return _rectBack;
        }
    }

    #endregion
}
