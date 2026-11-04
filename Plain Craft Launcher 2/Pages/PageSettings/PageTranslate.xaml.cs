using System.Windows;

namespace PCL.Pages;

public partial class PageTranslate
{
    public PageTranslate()
    {
        InitializeComponent();
        TxtBaseUrl.Text = PklSettings.Current.TranslateBaseUrl;
        TxtApiKey.Text = PklSettings.Current.TranslateApiKey;
        TxtModel.Text = PklSettings.Current.TranslateModel;
        TxtProxy.Text = PklSettings.Current.HttpProxy;
        TxtConcurrency.Text = PklSettings.Current.DownloadConcurrency.ToString();
        TxtTimeout.Text = PklSettings.Current.HttpTimeoutSeconds.ToString();
    }

    private void BtnSave_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        PklSettings.Current.TranslateBaseUrl = TxtBaseUrl.Text.Trim();
        PklSettings.Current.TranslateApiKey = TxtApiKey.Text.Trim();
        PklSettings.Current.TranslateModel = TxtModel.Text.Trim();
        PklSettings.Current.HttpProxy = TxtProxy.Text.Trim();
        int.TryParse(TxtConcurrency.Text.Trim(), out var conc);
        if (conc > 0) PklSettings.Current.DownloadConcurrency = conc;
        int.TryParse(TxtTimeout.Text.Trim(), out var to);
        if (to > 0) PklSettings.Current.HttpTimeoutSeconds = to;
        PklSettings.Save();
        LabSaved.Text = "已保存。";
    }
}
