using Microsoft.VisualStudio.Text.Editor;
using MvvmTools.Views;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MvvmTools.Adornments
{
    /// <summary>
    /// Adornment class that draws a square box in the top right hand corner of the viewport
    /// </summary>
    internal sealed class NavigateViewportAdornment
    {
        /// <summary>
        /// The width of the square box.
        /// </summary>
        private const double AdornmentWidth = 100;

        /// <summary>
        /// The height of the square box.
        /// </summary>
        private const double AdornmentHeight = 40;

        /// <summary>
        /// Distance from the viewport top to the top of the square box.
        /// </summary>
        private const double TopMargin = 20;

        /// <summary>
        /// Distance from the viewport right to the right end of the square box.
        /// </summary>
        private const double RightMargin = 20;

        /// <summary>
        /// Text view to add the adornment on.
        /// </summary>
        private readonly IWpfTextView view;

        /// <summary>
        /// Adornment image
        /// </summary>
        private readonly FrameworkElement control;

        /// <summary>
        /// The layer for the adornment.
        /// </summary>
        private readonly IAdornmentLayer adornmentLayer;

        /// <summary>
        /// Initializes a new instance of the <see cref="NavigateViewportAdornment"/> class.
        /// Creates a square image and attaches an event handler to the layout changed event that
        /// adds the the square in the upper right-hand corner of the TextView via the adornment layer
        /// </summary>
        /// <param name="view">The <see cref="IWpfTextView"/> upon which the adornment will be drawn</param>
        public NavigateViewportAdornment(IWpfTextView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException("view");
            }

            this.view = view;

            this.control = new SwitchViewModelUserControl();

            this.adornmentLayer = view.GetAdornmentLayer("NavigateViewportAdornment");

            this.view.LayoutChanged += this.OnSizeChanged;
        }

        /// <summary>
        /// Event handler for viewport layout changed event. Adds adornment at the top right corner of the viewport.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnSizeChanged(object sender, EventArgs e)
        {
            // Clear the adornment layer of previous adornments
            this.adornmentLayer.RemoveAllAdornments();

            // Place the image in the top right hand corner of the Viewport
            Canvas.SetLeft(this.control, this.view.ViewportRight - RightMargin - control.ActualWidth);
            Canvas.SetTop(this.control, this.view.ViewportTop + TopMargin);

            // Add the image to the adornment layer and make it relative to the viewport
            this.adornmentLayer.AddAdornment(AdornmentPositioningBehavior.ViewportRelative, null, null, this.control, null);
        }
    }
}
