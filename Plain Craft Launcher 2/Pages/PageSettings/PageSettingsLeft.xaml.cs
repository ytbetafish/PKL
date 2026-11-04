using System;
using System.Windows;

namespace PCL;

public partial class PageSettingsLeft
{
    public event Action<string>? NavChanged;

    public PageSettingsLeft()
    {
        InitializeComponent();
        NavKsp.Checked = true;
        NavKsp.Check += (s, e) => NavChanged?.Invoke("ksp");
        NavCkan.Check += (s, e) => NavChanged?.Invoke("ckan");
        NavTranslate.Check += (s, e) => NavChanged?.Invoke("translate");
        NavAbout.Check += (s, e) => NavChanged?.Invoke("about");
    }

    public void SelectNav(string key)
    {
        switch (key)
        {
            case "ksp": NavKsp.Checked = true; break;
            case "ckan": NavCkan.Checked = true; break;
            case "translate": NavTranslate.Checked = true; break;
            case "about": NavAbout.Checked = true; break;
        }
    }
}
