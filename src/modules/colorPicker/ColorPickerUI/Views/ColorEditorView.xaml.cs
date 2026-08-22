// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Specialized;
using System.ComponentModel;

using ColorPicker.Helpers;
using ColorPicker.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace ColorPicker.Views
{
    /// <summary>
    /// Interaction logic for ColorEditorView.xaml
    /// </summary>
    public sealed partial class ColorEditorView : UserControl
    {
        private const double MinimumFormatNameColumnWidth = 48;
        private ColorEditorViewModel _colorEditorViewModel;

        public static readonly DependencyProperty FormatNameColumnWidthProperty =
            DependencyProperty.Register(
                nameof(FormatNameColumnWidth),
                typeof(double),
                typeof(ColorEditorView),
                new PropertyMetadata(MinimumFormatNameColumnWidth));

        public double FormatNameColumnWidth
        {
            get => (double)GetValue(FormatNameColumnWidthProperty);
            private set => SetValue(FormatNameColumnWidthProperty, value);
        }

        public ColorEditorView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void HistoryContextFlyout_Opening(object sender, object e)
        {
            bool hasSelection = HistoryColors.SelectedItems.Count > 0;
            RemoveMenuItem.IsEnabled = hasSelection;
            ExportMenuItem.IsEnabled = hasSelection;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            EnableHistoryColorsScrollIntoView();
            AttachColorEditorViewModel();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            DetachColorEditorViewModel();
        }

        private void AttachColorEditorViewModel()
        {
            if (DataContext is not ColorEditorViewModel colorEditorViewModel)
            {
                FormatNameColumnWidth = MinimumFormatNameColumnWidth;
                return;
            }

            if (ReferenceEquals(_colorEditorViewModel, colorEditorViewModel))
            {
                UpdateFormatNameColumnWidth();
                return;
            }

            DetachColorEditorViewModel();
            _colorEditorViewModel = colorEditorViewModel;
            _colorEditorViewModel.ColorRepresentations.CollectionChanged += ColorRepresentations_CollectionChanged;
            UpdateFormatNameColumnWidth();
        }

        private void DetachColorEditorViewModel()
        {
            if (_colorEditorViewModel == null)
            {
                return;
            }

            _colorEditorViewModel.ColorRepresentations.CollectionChanged -= ColorRepresentations_CollectionChanged;
            _colorEditorViewModel = null;
        }

        private void ColorRepresentations_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateFormatNameColumnWidth();
        }

        private void UpdateFormatNameColumnWidth()
        {
            if (_colorEditorViewModel == null || _colorEditorViewModel.ColorRepresentations.Count == 0)
            {
                FormatNameColumnWidth = MinimumFormatNameColumnWidth;
                return;
            }

            var measuringTextBlock = new TextBlock
            {
                Style = Application.Current.Resources["CaptionTextBlockStyle"] as Style,
            };

            double width = MinimumFormatNameColumnWidth;
            foreach (var representation in _colorEditorViewModel.ColorRepresentations)
            {
                measuringTextBlock.Text = representation.FormatName;
                measuringTextBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                width = Math.Max(width, Math.Ceiling(measuringTextBlock.DesiredSize.Width));
            }

            FormatNameColumnWidth = width;
        }

        /// <summary>
        /// Updating SelectedColorIndex will not refresh the ListView viewport. We listen for the
        /// SelectedColorIndex property change and, when a new color is added (value &lt;= 0), call
        /// ScrollIntoView so the ListView scrolls back to the start.
        /// </summary>
        private void EnableHistoryColorsScrollIntoView()
        {
            if (DataContext is not ColorEditorViewModel colorEditorViewModel)
            {
                return;
            }

            ((INotifyPropertyChanged)colorEditorViewModel).PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(colorEditorViewModel.SelectedColorIndex) && colorEditorViewModel.SelectedColorIndex <= 0)
                {
                    HistoryColors.ScrollIntoView(colorEditorViewModel.SelectedColor);
                }
            };
        }

        /// <summary>
        /// Scrolls the history ListView horizontally on mouse wheel. WinUI has no MouseWheel; use
        /// PointerWheelChanged and the wheel delta from the pointer point.
        /// </summary>
        private void HistoryColors_OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            var scrollViewer = FindVisualChild<ScrollViewer>(HistoryColors);
            if (scrollViewer != null)
            {
                var delta = e.GetCurrentPoint(HistoryColors).Properties.MouseWheelDelta;

                // Positive delta scrolls left (towards the most-recent color), matching the WPF behavior.
                scrollViewer.ChangeView(scrollViewer.HorizontalOffset - delta, null, null);
                e.Handled = true;
            }
        }

        private static T FindVisualChild<T>(DependencyObject obj)
            where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child is T tChild)
                {
                    return tChild;
                }

                T childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }

            return null;
        }
    }
}
