﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace StaggeredGridView.Controls
{
    public class StaggeredGridView : ContentControl
    {
        #region EVENTS
        public event EventHandler PullToRefreshRequested;
        #endregion

        #region ITEMS SOURCE
        public IEnumerable<IStaggeredGridViewItem> ItemsSource
        {
            get { return (IEnumerable<IStaggeredGridViewItem>)GetValue(StaggeredGridView.ItemsSourceProperty); }
            set { SetValue(StaggeredGridView.ItemsSourceProperty, value); }
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.RegisterAttached("ItemsSource", typeof(object), typeof(StaggeredGridView), new PropertyMetadata(null, OnItemsSourceChanged));

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var list = d as StaggeredGridView;

            //clean old
            if (e.OldValue as INotifyCollectionChanged != null)
                (e.OldValue as INotifyCollectionChanged).CollectionChanged -= list.StaggeredGrid_CollectionChanged;

            if (e.NewValue is INotifyCollectionChanged)
                (e.NewValue as INotifyCollectionChanged).CollectionChanged += list.StaggeredGrid_CollectionChanged;

            list.incrementalCollection = e.NewValue as ISupportIncrementalLoading;

            list.Redraw();
        }
        #endregion

        #region NUMBER OF COLUMNS
        public int ColumnsNumber
        {
            get { return (int)GetValue(ColumnsNumberProperty); }
            set { SetValue(ColumnsNumberProperty, value); }
        }

        public static readonly DependencyProperty ColumnsNumberProperty =
            DependencyProperty.RegisterAttached("ColumnsNumber", typeof(int), typeof(StaggeredGridView), new PropertyMetadata(2, OnColumnsNumberChanged));

        private static void OnColumnsNumberChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var list = d as StaggeredGridView;

            if ((int)e.NewValue != list.columns.Count)
            {
                list.columns.Clear();

                for (int i = 0; i < (int)e.NewValue; i++)
                    list.columns.Add(new ColumnSummary());

                list.Redraw();
            }
        }
        #endregion

        #region COLUMNS SPACING
        public int ColumnsSpacing
        {
            get { return (int)GetValue(ColumnsSpacingProperty); }
            set { SetValue(ColumnsSpacingProperty, value); }
        }

        public static readonly DependencyProperty ColumnsSpacingProperty =
            DependencyProperty.RegisterAttached("ColumnsSpacing", typeof(double), typeof(StaggeredGridView), new PropertyMetadata(5, OnColumnsSpacingChanged));

        private static void OnColumnsSpacingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var list = d as StaggeredGridView;
            list.Redraw();
        }
        #endregion

        #region ROWS SPACING
        public int RowsSpacing
        {
            get { return (int)GetValue(RowsSpacingProperty); }
            set { SetValue(RowsSpacingProperty, value); }
        }

        public static readonly DependencyProperty RowsSpacingProperty =
            DependencyProperty.RegisterAttached("RowsSpacing", typeof(double), typeof(StaggeredGridView), new PropertyMetadata(5, OnRowsSpacingChanged));

        private static void OnRowsSpacingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var list = d as StaggeredGridView;
            list.Redraw();
        }
        #endregion

        public DataTemplate ItemTemplate { get; set; }

        #region HEADER AND FOOTER
        public static readonly DependencyProperty HeaderProperty = DependencyProperty.RegisterAttached("Header", typeof(object), typeof(StaggeredGridView), new PropertyMetadata(null, OnHeaderChanged));
        public static readonly DependencyProperty FooterProperty = DependencyProperty.Register("Footer", typeof(object), typeof(StaggeredGridView), new PropertyMetadata(null, OnFooterChanged));

        public object Header
        {
            get { return GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        public object Footer
        {
            get { return GetValue(FooterProperty); }
            set { SetValue(FooterProperty, value); }
        }

        private static void OnHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var list = d as StaggeredGridView;
            list.header = e.NewValue;
            list.Redraw();
        }

        private static void OnFooterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                var list = d as StaggeredGridView;

                list.footer = e.NewValue;

                if (list.footerPresenter != null)
                    list.footerPresenter.SizeChanged -= list.FooterPresenter_SizeChanged;

                list.Redraw();

                //TODO for the footer is superfluous to the Redraw being at the bottom, optimizing the SizeChanged
            }
            catch { }
        }
        #endregion

        #region PULL TO REFRESH
        public static readonly DependencyProperty IsPullToRefreshEnabledProperty = DependencyProperty.RegisterAttached("IsPullToRefreshEnabled", typeof(bool), typeof(StaggeredGridView), new PropertyMetadata(false, OnPullToRefreshEnabledChanged));
        public static readonly DependencyProperty RefreshHeaderHeightProperty = DependencyProperty.Register("RefreshHeaderHeight", typeof(double), typeof(StaggeredGridView), new PropertyMetadata(100D, OnRefreshHeaderHeightChanged));
        public static readonly DependencyProperty PullTextProperty = DependencyProperty.Register("PullText", typeof(string), typeof(StaggeredGridView), new PropertyMetadata("Pull to refresh"));
        public static readonly DependencyProperty RefreshTextProperty = DependencyProperty.Register("RefreshText", typeof(string), typeof(StaggeredGridView), new PropertyMetadata("Release to refresh"));
        public static readonly DependencyProperty ArrowColorProperty = DependencyProperty.Register("ArrowColor", typeof(Brush), typeof(StaggeredGridView), new PropertyMetadata(new SolidColorBrush(Colors.Black)));

        public bool IsPullToRefreshEnabled
        {
            get { return (bool)GetValue(StaggeredGridView.IsPullToRefreshEnabledProperty); }
            set { SetValue(StaggeredGridView.IsPullToRefreshEnabledProperty, value); }
        }

        public double RefreshHeaderHeight
        {
            get { return (double)GetValue(RefreshHeaderHeightProperty); }
            set { SetValue(RefreshHeaderHeightProperty, value); }
        }

        public string RefreshText
        {
            get { return (string)GetValue(RefreshTextProperty); }
            set { SetValue(RefreshTextProperty, value); }
        }

        public string PullText
        {
            get { return (string)GetValue(PullTextProperty); }
            set { SetValue(PullTextProperty, value); }
        }

        public Brush ArrowColor
        {
            get { return (Brush)GetValue(ArrowColorProperty); }
            set { SetValue(ArrowColorProperty, value); }
        }

        private static void OnPullToRefreshEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var list = d as StaggeredGridView;

            if (list.CheckIsTouchPresent())
            {
                list.isPullToRefreshEnabled = (bool)e.NewValue;

                if (list.isPullToRefreshEnabled)
                    VisualStateManager.GoToState(list, VisualStateNormal, true);
                else
                    VisualStateManager.GoToState(list, VisualStateRefreshDisabled, true);
            }
            else if ((bool)e.NewValue)
            {
                list.IsPullToRefreshEnabled = false;    //automatically disable
            }
        }

        private static void OnRefreshHeaderHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var list = d as StaggeredGridView;

            list.refreshHeaderHeight = (double)e.NewValue;

            if (list.scroll != null)
                list.TranslateScrollViewer();
        }

        #endregion

        #region PRIVATE
        //PULL TO REFRESH
        private DispatcherTimer compressionTimer;
        private DispatcherTimer timer;
        private Grid containerGrid;
        private Border pullToRefreshIndicator;
        private double refreshHeaderHeight;
        private bool isPullToRefreshEnabled;
        private bool isCompressionTimerRunning;
        private bool isReadyToRefresh;
        private bool isCompressedEnough;
        private double offsetTreshhold = 70;
        private bool isTouchPresent;

        //COLUMNS        
        private List<ColumnSummary> columns = new List<ColumnSummary>() { new ColumnSummary(), new ColumnSummary() };

        private double maxVisibleTop;
        private double minVisibleBottom;

        private double visibleHeight;

        private FrameworkElement _animatingRemoveUi;

        //UI PANELS
        private ScrollViewer scroll;
        private Canvas canvas;

        //INCREMENTAL
        private ISupportIncrementalLoading incrementalCollection;
        private bool loadingIncremental;

        //HEADER
        private object header;
        private double headerHeight;

        //FOOTER
        private object footer;
        private ContentPresenter footerPresenter;
        private double footerHeight;
        #endregion

        #region CONSTANTS
        private const float VISIBLE_HEIGHT_FACTOR = 2.5f;       //higher -> more elements in first UI pool
        private const float SCROLL_MARGIN_FACTOR = 1f;          //higher -> easier that new elements will be added in UI pool when scrolling
        private const float INCREMENTAL_SCROLL_FACTOR = 0.6f;   //must be less than 1; closer to 1 -> pagination start soon

        private const string VisualStateNormal = "Normal";
        private const string VisualStateReadyToRefresh = "ReadyToRefresh";
        private const string VisualStateRefreshDisabled = "RefreshDisabled";
        #endregion

        public StaggeredGridView()
        {
            this.DefaultStyleKey = typeof(StaggeredGridView);
            Loaded += StaggeredGridView_Loaded;
            Unloaded += StaggeredGridView_Unloaded;

            if (!CheckIsTouchPresent())
                IsPullToRefreshEnabled = false;
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            //get ui elements from template
            scroll = (ScrollViewer)GetTemplateChild("ScrollViewer");
            containerGrid = (Grid)GetTemplateChild("ContainerGrid");
            canvas = (Canvas)GetTemplateChild("Canvas");
            pullToRefreshIndicator = (Border)GetTemplateChild("PullToRefreshIndicator");

            //translate scrollviewer
            refreshHeaderHeight = RefreshHeaderHeight;
            TranslateScrollViewer();

            //subscribe events
            scroll.ViewChanging += ScrollViewer_ViewChanging;
            scroll.ViewChanged += ScrollViewer_ViewChanged;

            SizeChanged += StaggeredGrid_SizeChanged;
        }

        private void TranslateScrollViewer()
        {
            if (isPullToRefreshEnabled)
            {
                var transform = new CompositeTransform();
                transform.TranslateY = -refreshHeaderHeight;
                scroll.Margin = new Thickness(0, 0, 0, -refreshHeaderHeight);
                scroll.RenderTransform = transform;

                pullToRefreshIndicator.Visibility = Visibility.Visible;
            }
            else
            {
                pullToRefreshIndicator.Visibility = Visibility.Collapsed;

                scroll.Margin = new Thickness(0);
                scroll.RenderTransform = new CompositeTransform();
            }
        }

        private void CleanTimers()
        {
            if (timer != null)
            {
                timer.Stop();
                timer.Tick -= Timer_Tick;
                timer = null;
            }

            if (compressionTimer != null)
            {
                compressionTimer.Stop();
                compressionTimer.Tick -= CompressionTimer_Tick;
                compressionTimer = null;
            }
        }

        private void StaggeredGridView_Loaded(object sender, RoutedEventArgs e)
        {
            CleanTimers();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += Timer_Tick;

            compressionTimer = new DispatcherTimer();
            compressionTimer.Interval = TimeSpan.FromMilliseconds(300);
            compressionTimer.Tick += CompressionTimer_Tick;

            timer.Start();
        }

        private void StaggeredGridView_Unloaded(object sender, RoutedEventArgs e)
        {
            CleanTimers();
        }

        private void StaggeredGrid_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    var dataTemplate = CreateItemTemplate();
                    if (dataTemplate != null)
                    {
                        var newItems = e.NewItems.Cast<IStaggeredGridViewItem>();
                        ArrangeItemsAndFooter(newItems, dataTemplate);
                        if (scroll != null)
                        {
                            ScrollItems(scroll.VerticalOffset
#if DEBUG
                            , "CollectionChanged (Add)"
#endif
                            );
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (scroll != null)
                    {
                        if (e.OldItems.Count == 1)
                        {
                            bool animating = _animatingRemoveUi != null;     //look for another animation in progress

                            if (!animating)
                            {
                                var sourceItem = e.OldItems[0];
                                foreach (var col in columns)
                                {
                                    var assignedItem = col.assignedItems.FirstOrDefault(a => a.DataSource == sourceItem);
                                    if (assignedItem != null && assignedItem.UI != null)
                                    {
                                        try
                                        {
                                            var animation = scroll.Resources["RemoveAnimation"] as Storyboard;
                                            if (animation != null)
                                            {
                                                assignedItem.UI.RenderTransform = new CompositeTransform();
                                                assignedItem.UI.RenderTransformOrigin = new Point(0.5, 0.5);

                                                foreach (var timeline in animation.Children)
                                                    Storyboard.SetTarget(timeline, assignedItem.UI);

                                                _animatingRemoveUi = assignedItem.UI;

                                                animation.Completed += Animation_Completed;
                                                animating = true;
                                                animation.Begin();
                                            }
                                        }
                                        catch
                                        {
                                            animating = false;
                                        }
                                        break;
                                    }
                                }
                            }

                            if (!animating)
                                CollectionChangedRedraw();
                        }
                        else if (e.OldItems.Count > 1)
                        {
                            CollectionChangedRedraw();
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Reset:
                    CollectionChangedRedraw();
                    break;
            }
        }

        private void Animation_Completed(object sender, object e)
        {
            var animation = sender as Storyboard;

            animation.Completed -= Animation_Completed;
            animation.Stop();

            canvas?.Children.Remove(_animatingRemoveUi);
            _animatingRemoveUi = null;

            CollectionChangedRedraw();
        }

        private void CollectionChangedRedraw()
        {
            if (scroll != null)
            {
                _usedItemsSource = null;
                var oldScroll = scroll.VerticalOffset;
                Redraw();
                ScrollItems(scroll.VerticalOffset
#if DEBUG
                            , "CollectionChanged (Other)"
#endif
                            );
            }
        }

        private void StaggeredGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Clip = new RectangleGeometry()
            {
                Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height)
            };

            Redraw();
        }

        private void ClearAll()
        {
            foreach (var col in columns)
                col.Reset();

            if (canvas != null)
                canvas.Children.Clear();

            headerHeight = 0;

            footerHeight = 0;
            footerPresenter = null;
        }

        private object _usedItemsSource;
        private object _usedItemTemplate;
        private double _usedWidth;
        private double _usedHeight;

        private void Redraw()
        {
            Debug.WriteLine("[REDRAW REQUESTED]");

            if (ItemsSource != _usedItemsSource || ItemTemplate != _usedItemTemplate || _usedWidth != ActualWidth || _usedHeight != ActualHeight)
                ClearAll();
            else
                return;

            //checks 1
            if (ItemsSource == null || ItemTemplate == null || ActualWidth == 0 || ActualHeight == 0)
                return;

            var dataTemplate = CreateItemTemplate();

            //checks 2
            if (dataTemplate == null)
                return;

            _animatingRemoveUi = null;
            _usedItemsSource = ItemsSource;
            _usedItemTemplate = ItemTemplate;
            _usedWidth = ActualWidth;
            _usedHeight = ActualHeight;

            Debug.WriteLine("[REDRAW STARTED]");

            //calculate sizes
            var internalWidth = GetInternalWidth();
            scroll.Width = GetExternaWidth(); ;
            canvas.Width = internalWidth;

            var columnWidth = GetColumnWidth();
            for (int i = 1; i < columns.Count; i++)
                columns[i].leftMargin = (columnWidth + ColumnsSpacing) * i;

            ArrangeHeader();
            ArrangeItemsAndFooter(ItemsSource, dataTemplate);

            Debug.WriteLine("[REDRAW COMPLETED]");
        }

        private bool CheckIsTouchPresent()
        {
            isTouchPresent = new TouchCapabilities().TouchPresent > 0;
            return isTouchPresent;
        }

        private double GetExternaWidth()
        {
            return ActualWidth;
        }

        private double GetInternalWidth()
        {
            return ActualWidth - Padding.Left - Padding.Right;
        }

        private double GetColumnWidth()
        {
            return (GetInternalWidth() - ColumnsSpacing * (columns.Count - 1)) / columns.Count;
        }

        private double GetAvailableHeight()
        {
            return Window.Current.Bounds.Height;        //TODO use the real available height
        }

        private double GetVerticalMargins(FrameworkElement dataTemplate)
        {
            return dataTemplate.Margin.Top + dataTemplate.Margin.Bottom + RowsSpacing;
        }

        private FrameworkElement CreateItemTemplate()
        {
            return ItemTemplate.LoadContent() as FrameworkElement;
        }

        private void ArrangeHeader()
        {
            //header
            if (Header != null)
            {
                var internalWidth = GetInternalWidth();

                var headerPresenter = new ContentPresenter();
                headerPresenter.Width = internalWidth;
                headerPresenter.Content = Header;
                canvas.Children.Add(headerPresenter);
                headerPresenter.Measure(new Size(internalWidth, double.PositiveInfinity));
                headerHeight = headerPresenter.DesiredSize.Height;

                //TODO handle headerPresenter.SizeChanged (move all redraw all mantaining scroll)

                foreach (var col in columns)
                {
                    col.height += headerHeight;
                    col.visibleBottom += headerHeight;
                }
            }
        }

        private void ArrangeItemsAndFooter(IEnumerable<IStaggeredGridViewItem> items, FrameworkElement dataTemplate)
        {
            var internalWidth = GetInternalWidth();

            if (internalWidth <= 0)
                return;

            if (footerPresenter != null)
            {
                foreach (var col in columns)
                    col.height -= footerHeight;
            }

            visibleHeight = GetAvailableHeight();

            var columnWidth = GetColumnWidth();

            //arrange items
            bool visible = true;

            foreach (var item in items)
            {
                var column = ChooseBelowPositionUsingHeight();

                double scaledWidth;
                double scaledHeight;

                //if you don't want to stretch smaller content
                //if (item.Width + dataTemplate.Margin.Left + dataTemplate.Margin.Right <= columnWidth)
                //{
                //    scaledWidth = item.Width;
                //    scaledHeight = item.Height;
                //}
                //else
                //{
                scaledWidth = columnWidth - dataTemplate.Margin.Left - dataTemplate.Margin.Right;
                scaledHeight = item.Height * scaledWidth / item.Width;
                //}

                if (scaledWidth < 0)
                    Debugger.Break();

                var verticalMargins = GetVerticalMargins(dataTemplate);

                var virtualization = new VirtualizationMapping(item, scaledWidth, scaledHeight);
                column.assignedItems.Add(virtualization);
                column.height += (virtualization.Height + verticalMargins);

                if (visible &&
                    columns.All(c => c.visibleBottom >= visibleHeight * VISIBLE_HEIGHT_FACTOR))
                {
                    visible = false;
                }

                if (visible)
                {
                    var contentItem = CreateItemTemplate();
                    contentItem.DataContext = item;
                    contentItem.Width = virtualization.Width;
                    contentItem.Height = virtualization.Height;

                    virtualization.UI = contentItem;

                    PositionItem(contentItem, Direction.Below, column, column.assignedItems.Count - 1);

                    canvas.Children.Add(contentItem);
#if DEBUG
                    Debug.WriteLine("[CREATED_UI_ITEMS] " + canvas.Children.Count);
#endif

                    column.lastVisibleIndex++;
                    column.visibleBottom += (virtualization.Height + verticalMargins);
                }
            }

            var maxColHeight = columns.Max(c => c.height);

            //footer
            if (Footer != null)
            {
                var newFooter = false;

                if (footerPresenter == null)
                {
                    newFooter = true;
                    footerPresenter = new ContentPresenter();
                    footerPresenter.Width = internalWidth;
                    footerPresenter.Content = Footer;
                }

                Canvas.SetTop(footerPresenter, maxColHeight);
                if (newFooter)
                    canvas.Children.Add(footerPresenter);
                footerPresenter.Measure(new Size(internalWidth, double.PositiveInfinity));

                if (newFooter)
                    footerPresenter.SizeChanged += FooterPresenter_SizeChanged;

                footerHeight = footerPresenter.DesiredSize.Height;

                foreach (var col in columns)
                    col.height += footerHeight;

                maxColHeight += footerHeight;
            }

            UpdateVisibleMargins();
            canvas.Height = maxColHeight;
        }

        private void FooterPresenter_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Height != footerHeight)
            {
                var heightDiff = e.NewSize.Height - footerHeight;

                foreach (var col in columns)
                    col.height += heightDiff;

                canvas.Height += heightDiff;

                footerHeight = e.NewSize.Height;
            }
        }

        private void ScrollItems(double scrollVerticalOffset,
#if DEBUG
            string from
#endif
            )
        {
            if (scrollVerticalOffset + 2 * visibleHeight > minVisibleBottom)
            {
                //SCROLL DOWN
                Debug.WriteLine("");

                int stops = 0;

                do
                {
                    foreach (var col in columns)
                    {
                        if (col.lastVisibleIndex + 1 < col.assignedItems.Count)
                        {
#if DEBUG
                            Debug.WriteLine($"[MOVE DOWN] {from} | {scrollVerticalOffset} + {2 * visibleHeight} > {minVisibleBottom}");
#endif

                            var oldItem = col.assignedItems[col.firstVisibleIndex];

                            //check if can recycle (movable top UI)
                            bool canRecycle = col.visibleTop + oldItem.Height < scrollVerticalOffset - (visibleHeight * SCROLL_MARGIN_FACTOR);

                            var nextItem = col.assignedItems[col.lastVisibleIndex + 1];

                            FrameworkElement uiToSwap;

                            if (canRecycle)
                            {
                                uiToSwap = oldItem.UI;
                                oldItem.UI = null;
                            }
                            else
                            {
                                uiToSwap = CreateItemTemplate();
                            }

                            //update VirtualizationMapping
                            nextItem.UI = uiToSwap;
                            uiToSwap.DataContext = nextItem.DataSource;
                            uiToSwap.Width = nextItem.Width;
                            uiToSwap.Height = nextItem.Height;

                            PositionItem(uiToSwap, Direction.Below, col, col.lastVisibleIndex);

                            var verticalMargins = GetVerticalMargins(uiToSwap);

                            if (canRecycle)
                            {
                                col.firstVisibleIndex++;
                                col.visibleTop += (oldItem.Height + verticalMargins);
                            }
                            else
                            {
                                canvas.Children.Add(uiToSwap);

#if DEBUG
                                Debug.WriteLine("[CREATED_UI_ITEMS] " + canvas.Children.Count);
#endif
                            }

                            col.lastVisibleIndex++;
                            col.visibleBottom += (nextItem.Height + verticalMargins);
                        }
                        else
                        {
                            stops++;
                            //reached footer
                        }
                    }

                    UpdateVisibleMargins();
                }
                while (stops < 2 && scrollVerticalOffset > minVisibleBottom - visibleHeight);
            }
            else
            {
                var scrollToTopDistance = scrollVerticalOffset - maxVisibleTop;


                if (scrollToTopDistance < visibleHeight)      //la distanza tra lo scroll e il margine superiore realizzato è meno di una schermata
                {
                    //SCROLL UP
                    Debug.WriteLine("");

                    int stops = 0;

                    do
                    {
                        foreach (var col in columns)
                        {
                            if (col.firstVisibleIndex > 0)
                            {
#if DEBUG
                                Debug.WriteLine($"[MOVE UP] {from} | {scrollVerticalOffset} - {maxVisibleTop} = {scrollToTopDistance} < {visibleHeight}");
#endif

                                var oldItem = col.assignedItems[col.lastVisibleIndex];

                                col.firstVisibleIndex--;
                                col.lastVisibleIndex--;

                                var previousItem = col.assignedItems[col.firstVisibleIndex];

                                var uiToSwap = oldItem.UI;
                                oldItem.UI = null;

                                //update VirtualizationMapping
                                previousItem.UI = uiToSwap;
                                uiToSwap.DataContext = previousItem.DataSource;
                                uiToSwap.Width = previousItem.Width;
                                uiToSwap.Height = previousItem.Height;

                                var verticalMargins = GetVerticalMargins(uiToSwap);

                                col.visibleTop -= (previousItem.Height + verticalMargins);
                                col.visibleBottom -= (oldItem.Height + verticalMargins);

                                PositionItem(uiToSwap, Direction.Above, col, col.firstVisibleIndex);
                            }
                            else
                            {
                                stops++;
                                //reached header
                            }
                        }

                        UpdateVisibleMargins();
                    }
                    while (stops < 2 && scrollVerticalOffset < maxVisibleTop + visibleHeight);
                }
            }
        }

        private void ScrollViewer_ViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            //pull to refresh
            if (isPullToRefreshEnabled)
            {
                if (e.NextView.VerticalOffset == 0)
                {
                    timer?.Start();
                }
                else
                {
                    CompletePullToRefresh();

                    if (canvas.ChildrenTransitions != null && canvas.ChildrenTransitions.Count > 0)
                        canvas.ChildrenTransitions = null;
                }
            }

            //recycle
            ScrollItems(e.NextView.VerticalOffset
#if DEBUG
                , "ViewChanging"
#endif
                );
        }

        private async void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            //incremental loading
            if (incrementalCollection != null &&
                scroll.VerticalOffset / scroll.ScrollableHeight >= INCREMENTAL_SCROLL_FACTOR &&
                !loadingIncremental &&
                incrementalCollection.HasMoreItems)
            {
                loadingIncremental = true;
                await incrementalCollection.LoadMoreItemsAsync(20);     //TODO choose your pagination increment for ISupportIncrementalLoading
                loadingIncremental = false;
            }
        }

        private void UpdateVisibleMargins()
        {
            maxVisibleTop = columns.Max(c => c.visibleTop);
            minVisibleBottom = columns.Min(c => c.visibleBottom);
        }

        private ColumnSummary ChooseBelowPositionUsingHeight()
        {
            ColumnSummary higher = columns[0];

            for (int i = 1; i < columns.Count; i++)
                if (columns[i].height < higher.height)
                    higher = columns[i];

            return higher;
        }

        private void PositionItem(FrameworkElement item, Direction direction, ColumnSummary column, int columnIndex)
        {
            Canvas.SetLeft(item, column.leftMargin);

            if (direction == Direction.Below)
                Canvas.SetTop(item, column.visibleBottom);
            else
                Canvas.SetTop(item, column.visibleTop + headerHeight);
        }

        /// <summary>
        /// Detect if I've scrolled far enough and been there for enough time to refresh
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CompressionTimer_Tick(object sender, object e)
        {
            if (isCompressedEnough)
            {
                VisualStateManager.GoToState(this, VisualStateReadyToRefresh, true);
                isReadyToRefresh = true;
            }
            else
            {
                isCompressedEnough = false;
                compressionTimer.Stop();
            }
        }

        /// <summary>
        /// Invoke timer if we've scrolled far enough up into negative space. If we get back to offset 0 the refresh command and event is invoked. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Tick(object sender, object e)
        {
            if (pullToRefreshIndicator == null)
            {
                timer.Stop();
                return; //fix for Visibility Collapsed
            }

            Rect elementBounds = pullToRefreshIndicator.TransformToVisual(containerGrid).TransformBounds(new Rect(0.0, 0.0, refreshHeaderHeight, refreshHeaderHeight));
            var compressionOffset = elementBounds.Bottom;

            if (compressionOffset != 0)
                Debug.WriteLine("[COMPRESSION] " + compressionOffset);

            if (compressionOffset > offsetTreshhold)
            {
                if (isCompressionTimerRunning == false)
                {
                    isCompressionTimerRunning = true;
                    compressionTimer.Start();
                }

                isCompressedEnough = true;
            }
            else if (isReadyToRefresh == true && compressionOffset == 0)
            {
                InvokeRefresh();
            }
            else
            {
                isCompressedEnough = false;
                isCompressionTimerRunning = false;

                if (compressionOffset < 0)
                    CompletePullToRefresh();
            }
        }

        /// <summary>
        /// Set correct visual state and invoke refresh event and command
        /// </summary>
        private void InvokeRefresh()
        {
            isReadyToRefresh = false;
            VisualStateManager.GoToState(this, VisualStateNormal, true);

            PullToRefreshRequested?.Invoke(this, EventArgs.Empty);
        }

        public static DependencyObject FindChildControl<T>(DependencyObject control, string ctrlName)
        {
            int childNumber = VisualTreeHelper.GetChildrenCount(control);
            for (int i = 0; i < childNumber; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(control, i);
                FrameworkElement fe = child as FrameworkElement;
                // Not a framework element or is null
                if (fe == null) return null;
                if (string.IsNullOrEmpty(ctrlName) && child is T)
                    return child;
                else if (child is T && fe.Name == ctrlName)
                {
                    // Found the control so return
                    return child;
                }
                else
                {
                    // Not found it - search children
                    DependencyObject nextLevel = FindChildControl<T>(child, ctrlName);
                    if (nextLevel != null)
                        return nextLevel;
                }
            }
            return null;
        }

        public void CompletePullToRefresh()
        {
            timer?.Stop();
            compressionTimer?.Stop();

            isCompressionTimerRunning = false;
            isCompressedEnough = false;
            isReadyToRefresh = false;

            VisualStateManager.GoToState(this, VisualStateNormal, true);
        }

        public void ScrollIntoView(IStaggeredGridViewItem item)
        {
            foreach (var col in columns)
            {
                double verticalOffset = headerHeight;

                foreach (var vm in col.assignedItems)
                {
                    if (vm == item)
                    {
                        //found
                        scroll.ChangeView(null, verticalOffset, null, true);
                        break;
                    }

                    verticalOffset += vm.Height;
                }
            }
        }
    }

    /// <summary>
    /// Represents the data source interface for an item in a StaggeredGridView control.
    /// </summary>
    public interface IStaggeredGridViewItem
    {
        double Width { get; set; }
        double Height { get; set; }
    }

    internal class VirtualizationMapping
    {
        internal IStaggeredGridViewItem DataSource;
        internal double Width;
        internal double Height;
        internal FrameworkElement UI;

        public VirtualizationMapping(IStaggeredGridViewItem datasource, double width, double height)
        {
            DataSource = datasource;
            Width = width;
            Height = height;
        }
    }

    internal enum Direction
    {
        Above,
        Below,
    }

    internal class ColumnSummary
    {
        internal double leftMargin = 0;

        internal double height = 0;

        internal List<VirtualizationMapping> assignedItems = new List<VirtualizationMapping>();

        internal int firstVisibleIndex = 0;
        internal int lastVisibleIndex = -1;

        internal double visibleTop = 0;
        internal double visibleBottom = 0;

        internal void Reset()
        {
            visibleBottom = visibleTop = 0;
            firstVisibleIndex = 0;
            lastVisibleIndex = -1;
            assignedItems.Clear();
            height = 0;
            leftMargin = 0;
        }
    }
}