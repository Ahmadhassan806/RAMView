using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using RAMView.Core.Algorithms;
using RAMView.Core.Models;
using RAMView.Core.ViewModels;

namespace RAMView.App.Controls;

/// <summary>
/// High-performance animated treemap canvas implementing GPU-backed interpolated motion (§9).
/// Reuses visual elements across updates to minimize GC pressure and memory allocations (§17).
/// </summary>
public class AnimatedTreemapCanvas : Canvas
{
    private static readonly Duration AnimationDuration = new(TimeSpan.FromMilliseconds(280));
    private static readonly IEasingFunction Ease = new QuadraticEase { EasingMode = EasingMode.EaseOut };

    private readonly Dictionary<string, TreemapBoxElement> _activeElements = new(StringComparer.OrdinalIgnoreCase);

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(IReadOnlyList<TreemapRectangle>),
            typeof(AnimatedTreemapCanvas),
            new PropertyMetadata(null, OnItemsSourceChanged));

    public IReadOnlyList<TreemapRectangle>? ItemsSource
    {
        get => (IReadOnlyList<TreemapRectangle>?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AnimatedTreemapCanvas canvas)
        {
            var newItems = e.NewValue as IReadOnlyList<TreemapRectangle>;
            if (canvas.Dispatcher.CheckAccess())
            {
                canvas.UpdateLayoutAnimated(newItems);
            }
            else
            {
                canvas.Dispatcher.BeginInvoke(new Action(() => canvas.UpdateLayoutAnimated(newItems)));
            }
        }
    }

    private void UpdateLayoutAnimated(IReadOnlyList<TreemapRectangle>? newItems)
    {
        if (newItems == null || newItems.Count == 0)
        {
            foreach (var kvp in _activeElements)
            {
                FadeOutAndRemove(kvp.Value);
            }
            _activeElements.Clear();
            return;
        }

        bool animate = SystemParameters.ClientAreaAnimation;
        var incomingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rect in newItems)
        {
            string key = rect.Process.ProcessName + "_" + (rect.Process.InstanceCount > 1 ? "group" : rect.Process.ProcessId.ToString());
            incomingKeys.Add(key);

            if (_activeElements.TryGetValue(key, out var existingBox))
            {
                // Update content & animate to new geometry
                existingBox.UpdateContent(rect);
                AnimateBox(existingBox, rect, animate);
            }
            else
            {
                // New process box appears: fade in (§9)
                var newBox = new TreemapBoxElement(rect, OnBoxClicked);
                _activeElements[key] = newBox;
                Children.Add(newBox);

                SetLeft(newBox, rect.X);
                SetTop(newBox, rect.Y);
                newBox.Width = Math.Max(0, rect.Width);
                newBox.Height = Math.Max(0, rect.Height);

                if (animate)
                {
                    var fadeIn = new DoubleAnimation(0.0, rect.IsHighlighted ? 1.0 : 0.22, AnimationDuration)
                    {
                        EasingFunction = Ease
                    };
                    newBox.BeginAnimation(OpacityProperty, fadeIn);
                }
                else
                {
                    newBox.Opacity = rect.IsHighlighted ? 1.0 : 0.22;
                }
            }
        }

        // Detect disappeared/terminated processes: fade & shrink out (§9)
        var toRemove = new List<string>();
        foreach (var kvp in _activeElements)
        {
            if (!incomingKeys.Contains(kvp.Key))
            {
                FadeOutAndRemove(kvp.Value);
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var key in toRemove)
        {
            _activeElements.Remove(key);
        }
    }

    private void AnimateBox(TreemapBoxElement box, TreemapRectangle target, bool animate)
    {
        double targetOpacity = target.IsHighlighted ? 1.0 : 0.22;

        if (!animate)
        {
            SetLeft(box, target.X);
            SetTop(box, target.Y);
            box.Width = Math.Max(0, target.Width);
            box.Height = Math.Max(0, target.Height);
            box.Opacity = targetOpacity;
            return;
        }

        var animX = new DoubleAnimation(target.X, AnimationDuration) { EasingFunction = Ease };
        var animY = new DoubleAnimation(target.Y, AnimationDuration) { EasingFunction = Ease };
        var animW = new DoubleAnimation(Math.Max(0, target.Width), AnimationDuration) { EasingFunction = Ease };
        var animH = new DoubleAnimation(Math.Max(0, target.Height), AnimationDuration) { EasingFunction = Ease };
        var animO = new DoubleAnimation(targetOpacity, AnimationDuration) { EasingFunction = Ease };

        box.BeginAnimation(LeftProperty, animX);
        box.BeginAnimation(TopProperty, animY);
        box.BeginAnimation(WidthProperty, animW);
        box.BeginAnimation(HeightProperty, animH);
        box.BeginAnimation(OpacityProperty, animO);
    }

    private void FadeOutAndRemove(TreemapBoxElement box)
    {
        var fadeOut = new DoubleAnimation(0.0, AnimationDuration)
        {
            EasingFunction = Ease
        };
        fadeOut.Completed += (s, e) => Children.Remove(box);
        box.BeginAnimation(OpacityProperty, fadeOut);
    }

    private void OnBoxClicked(TreemapRectangle rect)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.SelectedProcess = rect.Process;
        }
    }
}

/// <summary>
/// Individual styled translucent glass box element representing a process.
/// </summary>
internal class TreemapBoxElement : Border
{
    private readonly Action<TreemapRectangle> _onClick;
    private TreemapRectangle _currentRect;

    private readonly Image _iconImage;
    private readonly TextBlock _nameText;
    private readonly Border _badgeBorder;
    private readonly TextBlock _badgeText;
    private readonly TextBlock _ramText;
    private readonly TextBlock _pctText;
    private readonly StackPanel _metricStack;

    public TreemapBoxElement(TreemapRectangle rect, Action<TreemapRectangle> onClick)
    {
        _currentRect = rect;
        _onClick = onClick;

        CornerRadius = new CornerRadius(6);
        BorderThickness = new Thickness(1);
        BorderBrush = new SolidColorBrush(Color.FromArgb(0x35, 0xFF, 0xFF, 0xFF));
        Background = new SolidColorBrush(Color.FromArgb(0x1F, 0xFF, 0xFF, 0xFF));
        ClipToBounds = true;
        Cursor = Cursors.Hand;

        var grid = new Grid
        {
            Margin = new Thickness(4, 3, 4, 3),
            ClipToBounds = true
        };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        // Header Row: Icon + Name + Badge
        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _iconImage = new Image
        {
            Width = 14,
            Height = 14,
            Margin = new Thickness(0, 0, 4, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        RenderOptions.SetBitmapScalingMode(_iconImage, BitmapScalingMode.HighQuality);
        Grid.SetColumn(_iconImage, 0);
        headerGrid.Children.Add(_iconImage);

        _nameText = new TextBlock
        {
            Foreground = new SolidColorBrush(Color.FromRgb(0xF9, 0xFA, 0xFB)),
            FontWeight = FontWeights.SemiBold,
            FontSize = 11,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(_nameText, 1);
        headerGrid.Children.Add(_nameText);

        _badgeBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(0x35, 0xFF, 0xFF, 0xFF)),
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(3, 1, 3, 1),
            Margin = new Thickness(2, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        _badgeText = new TextBlock
        {
            FontSize = 9,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(0xE5, 0xE7, 0xEB))
        };
        _badgeBorder.Child = _badgeText;
        Grid.SetColumn(_badgeBorder, 2);
        headerGrid.Children.Add(_badgeBorder);

        Grid.SetRow(headerGrid, 0);
        grid.Children.Add(headerGrid);

        // Metric Row: RAM + Percentage
        _metricStack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 2, 0, 0)
        };

        _ramText = new TextBlock
        {
            Foreground = new SolidColorBrush(Color.FromRgb(0xF9, 0xFA, 0xFB)),
            FontWeight = FontWeights.Bold,
            FontSize = 13,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        _metricStack.Children.Add(_ramText);

        _pctText = new TextBlock
        {
            Foreground = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)),
            FontSize = 10,
            FontWeight = FontWeights.Medium,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        _metricStack.Children.Add(_pctText);

        Grid.SetRow(_metricStack, 1);
        grid.Children.Add(_metricStack);

        Child = grid;

        MouseEnter += (s, e) =>
        {
            Background = new SolidColorBrush(Color.FromArgb(0x38, 0xFF, 0xFF, 0xFF));
            BorderBrush = new SolidColorBrush(Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF));
        };

        MouseLeave += (s, e) =>
        {
            Background = new SolidColorBrush(Color.FromArgb(0x1F, 0xFF, 0xFF, 0xFF));
            BorderBrush = new SolidColorBrush(Color.FromArgb(0x35, 0xFF, 0xFF, 0xFF));
        };

        MouseLeftButtonUp += (s, e) =>
        {
            _onClick(_currentRect);
            e.Handled = true;
        };

        UpdateContent(rect);
    }

    public void UpdateContent(TreemapRectangle rect)
    {
        _currentRect = rect;
        var p = rect.Process;

        _nameText.Text = p.ProcessName;
        _ramText.Text = p.FormattedMemory;
        _pctText.Text = p.FormattedPercentage;

        if (p.InstanceCount > 1)
        {
            _badgeBorder.Visibility = Visibility.Visible;
            _badgeText.Text = $"×{p.InstanceCount}";
        }
        else
        {
            _badgeBorder.Visibility = Visibility.Collapsed;
        }

        if (p.IconPngBytes != null && p.IconPngBytes.Length > 0)
        {
            try
            {
                var bmp = new BitmapImage();
                using var ms = new System.IO.MemoryStream(p.IconPngBytes);
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = ms;
                bmp.EndInit();
                bmp.Freeze();
                _iconImage.Source = bmp;
                _iconImage.Visibility = Visibility.Visible;
            }
            catch
            {
                _iconImage.Visibility = Visibility.Collapsed;
            }
        }
        else
        {
            _iconImage.Visibility = Visibility.Collapsed;
        }

        // Adaptive typography based on box size (§7)
        if (rect.Height < 36)
        {
            _metricStack.Visibility = Visibility.Collapsed;
            _nameText.FontSize = 10;
        }
        else
        {
            _metricStack.Visibility = Visibility.Visible;
            _nameText.FontSize = 11;
        }

        // Rich Tooltip
        ToolTip = new ToolTip
        {
            Background = new SolidColorBrush(Color.FromArgb(0xF5, 0x12, 0x16, 0x22)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(0x50, 0xFF, 0xFF, 0xFF)),
            BorderThickness = new Thickness(1),
            Foreground = Brushes.White,
            Padding = new Thickness(8, 6, 8, 6),
            Content = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = p.ProcessName, FontWeight = FontWeights.Bold, FontSize = 13, Foreground = Brushes.White },
                    new TextBlock { Text = $"RAM: {p.FormattedMemory}", FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(0x93, 0xC5, 0xFD)), Margin = new Thickness(0, 2, 0, 0) },
                    new TextBlock { Text = $"Tracked Share: {p.FormattedPercentage}", FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(0x9C, 0xA3, 0xAF)) },
                    new TextBlock { Text = $"PID: {p.ProcessId}", FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(0x9C, 0xA3, 0xAF)) },
                    new TextBlock { Text = $"Instances: {p.InstanceCount}", FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(0x9C, 0xA3, 0xAF)) }
                }
            }
        };
    }
}
