using System;
using System.Windows;

namespace TrackerOfflineSearch.Settings;

public class Placement<T> : IPlacement<T> where T : Window
{
    #region Constructor

    public Placement(IPlacementFactory factory)
    {
        if (factory is null)
            throw new ArgumentNullException(nameof(factory));

        this.placement = factory.GetPlacement(typeof(T).FullName);
    }

    #endregion

    #region IPlacement<T> implementation

    public void Attach(T window)
    {
        if (window is null)
            throw new ArgumentNullException(nameof(window));

        window.SourceInitialized += this.OnSourceInitialized;
        window.Closing += this.OnClosing;
    }

    #endregion

    #region Private fields & methods

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        if (sender is T window)
        {
            if (this.placement.Location != Rect.Empty)
            {
                window.Left = this.placement.Location.Left;
                window.Top = this.placement.Location.Top;
                window.Width = this.placement.Location.Width;
                window.Height = this.placement.Location.Height;

                window.WindowState = this.placement.State;
            }
        }
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (sender is T window)
        {
            if (window.WindowState == WindowState.Maximized)
            {
                // Use the RestoreBounds as the current values will be 0, 0 and the size of the screen
                this.placement.Location = window.RestoreBounds;
                this.placement.State = WindowState.Maximized;
            }
            else
            {
                this.placement.Location = new Rect(window.Left, window.Top, window.Width, window.Height);
                this.placement.State = WindowState.Normal;
            }

            this.placement.Save();
        }
    }

    private readonly IPlacement placement;

    #endregion
}
