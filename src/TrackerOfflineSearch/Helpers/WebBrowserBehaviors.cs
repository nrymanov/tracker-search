using System;
using System.Windows;
using System.Windows.Controls;

namespace TrackerOfflineSearch.Helpers;

public static class WebBrowserBehaviors
{
    public static readonly DependencyProperty BindableSourceProperty = DependencyProperty.RegisterAttached(
        "BindableSource", 
        typeof(object), 
        typeof(WebBrowserBehaviors), 
        new UIPropertyMetadata(null, BindableSourcePropertyChanged)
    );

    public static object GetBindableSource(DependencyObject obj) => (string)obj.GetValue(BindableSourceProperty);

    public static void SetBindableSource(DependencyObject obj, object value) => obj.SetValue(BindableSourceProperty, value);

    public static void BindableSourcePropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
        if (o is not WebBrowser browser) 
            return;

        if (e.NewValue is string content)
        {
            browser.NavigateToString(content);
        }
        else if (e.NewValue is Uri)
        {
            browser.Source = e.NewValue as Uri;
        }
    }
}
