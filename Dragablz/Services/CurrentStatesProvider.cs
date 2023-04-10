using Dragablz.Dockablz;
using Dragablz.Properties;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Dragablz.Services
{
    public class CurrentStatesProvider
    {
        #region DragablzTabItem  
        /// <summary>
        /// Indicates if the main window has rendered.
        /// </summary>
        public static bool hasMainWindowRendered;
        #endregion

        #region CurrentStatesProvider 
        /// <summary>
        /// Holds the Number of Branching Order
        /// </summary>
        public static int branchNumber;
        #endregion
        static CurrentStatesProvider()
        {
            Init();
        }

        public static void Init()
        {
            Layouts = new List<Layout>();
            if (Application.Current?.MainWindow != null && !isInitiated)
            {
                Application.Current.MainWindow.ContentRendered += MainWindow_ContentRendered;
                Application.Current.MainWindow.Closing += MainWindow_Closing;
                Application.Current.MainWindow.Loaded += MainWindow_Loaded;
                isInitiated = true;
            }
        }

        #region DragablzTabItem
        /// <summary>
        /// opened windows list from last session
        /// </summary>
        private static List<(Action<bool> openWindow, bool hasWindow)> openedWindows = null;
        /// <summary>
        /// Restore tabs states from last session in the given layout
        /// </summary>
        /// <param name="layout"></param>
        static bool layoutHasContent = false;
        public static List<Layout> Layouts;
        static LayoutAccessor mainLayoutAccessor;
        public static void RestoreItemHelper(Layout layout)
        {
            var layoutAccessor = layout.Query();
            if (layoutAccessor.TabablzControl is null)
                return;

            var tabItems = layoutAccessor.TabablzControl.GetOrderedHeaders();
            if (tabItems is null)
                return;
            layoutAccessor.Layout.InterLayoutClient = new DefaultInterLayoutClient();
            var layoutGroups = tabItems.Where(x => x.DataContext is DragablzTabItem).GroupBy(x => (x.DataContext as DragablzTabItem).LayoutName);

            if (openedWindows is null)
                openedWindows = new List<(Action<bool>, bool)>();

            var windowsGroups = tabItems.Where(x => x.Content is DragablzTabItem).OrderBy(t => (t.Content as DragablzTabItem).CurrentState.BranchNumber).GroupBy(x => (x.Content as DragablzTabItem).WindowID);
            foreach (var windowsGroup in windowsGroups)
            {
                mainLayoutAccessor = layoutAccessor;
                var tabablzControl = mainLayoutAccessor.TabablzControl;
                var hasWindow = false;
                var tabControlItems = windowsGroup.Where(x => x.DataContext is DragablzTabItem).OrderBy(t => (t.DataContext as DragablzTabItem).CurrentState.BranchNumber).GroupBy(x => (x.DataContext as DragablzTabItem).TabControlName);
                foreach (var items in tabControlItems)
                {
                    if (items.Where(i => (i.DataContext as DragablzTabItem).CurrentState.IsMainWindow).Any())
                    {
                        RestoreTabs(items, tabablzControl, hasWindow);
                    }
                    else
                    {
                        openedWindows.Add(((hasWindow) => RestoreTabs(items, tabablzControl, hasWindow), hasWindow));
                    }
                    hasWindow = true;
                }
            }
        }

        private static void RestoreTabs(IEnumerable<DragablzItem> newItems, TabablzControl tabablzControl, bool hasWindow)
        {
            foreach (var itemGroup in newItems.OrderBy(t => (t.Content as DragablzTabItem).Order).GroupBy(x => (x.DataContext as DragablzTabItem).Location))
            {

                if (itemGroup.First().DataContext is not DragablzTabItem dragablzTabItem)
                    continue;

                var location = dragablzTabItem.Location;
                var branchItem = new Branch
                {
                    Orientation = (location == DropZoneLocation.Right || location == DropZoneLocation.Left || location == DropZoneLocation.Unset) ? Orientation.Horizontal : Orientation.Vertical
                };
                INewTabHost<UIElement> newTabHost = null;
                newTabHost = GetNewTabHostOfItem(mainLayoutAccessor, tabablzControl, itemGroup, hasWindow);

                //if the layout has no content yet do not do the branch, replace the content instead
                object newContent = null;
                if (newTabHost.Container is Layout layout && !hasWindow)
                {
                    mainLayoutAccessor = layout.Query();
                    newContent = layout.Content;
                    mainLayoutAccessor.Layout.SetCurrentValue(Layout.ContentProperty, newContent);
                    continue;
                }
                else
                    newContent = newTabHost.Container;

                if (!newTabHost.TabablzControl.HasItems)
                    continue;

                if (!layoutHasContent)
                {
                    mainLayoutAccessor.Layout.SetCurrentValue(Layout.ContentProperty, newContent);
                    layoutHasContent = true;
                    continue;
                }

                if (location == DropZoneLocation.Right || location == DropZoneLocation.Bottom)
                {
                    branchItem.FirstItem = mainLayoutAccessor.Layout.Content;
                    branchItem.SecondItem = newContent;
                }
                else
                {
                    branchItem.FirstItem = newContent;
                    branchItem.SecondItem = mainLayoutAccessor.Layout.Content;
                }
                mainLayoutAccessor.Layout.SetCurrentValue(Layout.ContentProperty, branchItem);
            }
        }

        /// <summary>
        /// Helper method to Create a TabablzControl of given DragablzItem.
        /// </summary>
        /// <param name="layoutAccessor"></param>
        /// <param name="tabControl"></param>
        /// <param name="item"></param>
        /// <param name="newTabHost"></param>
        /// <param name="sourceOfDragItemsControl"></param>
        /// <param name="sourceItem"></param>
        private static INewTabHost<UIElement> GetNewTabHostOfItem(LayoutAccessor layoutAccessor, TabablzControl tabControl, IGrouping<DropZoneLocation, DragablzItem> relatedItems, bool hasWindow)
        {
            INewTabHost<UIElement> newTabHost = layoutAccessor.Layout.InterLayoutClient.GetNewHost(layoutAccessor.Layout.Partition, tabControl);
            if (newTabHost == null)
                throw new ApplicationException("InterLayoutClient did not provide a new tab host.");

            var item = relatedItems.First();

            DragablzItemsControl sourceOfDragItemsControl = ItemsControl.ItemsControlFromItemContainer(item) as DragablzItemsControl;
            if (sourceOfDragItemsControl == null) throw new ApplicationException("Unable to determin source items control.");

            object sourceItem = sourceOfDragItemsControl.ItemContainerGenerator.ItemFromContainer(item);

            //check if the tab was open from last session
            if (sourceItem is DragablzTabItem sourceDragablzTabItem)
            {
                if (!sourceDragablzTabItem.IsMainWindow && !hasWindow)
                {
                    newTabHost = tabControl.CreateWindow(layoutAccessor.Layout, item, sourceDragablzTabItem);
                }
                else
                {
                    //newTabHost.TabablzControl.Name = (item.Content as DragablzTabItem).TabControlName;
                    tabControl.RemoveItem(item);
                    newTabHost.TabablzControl.SelectedItem = sourceItem;
                    newTabHost.TabablzControl.AddToSource(sourceItem);
                }
                //check for the adjacent tabs in the same control
                if (relatedItems is not null)
                {
                    foreach (var relatedItem in relatedItems.Skip(1))
                    {
                        if (relatedItem.DataContext is DragablzTabItem dragablzTabItem)
                        {
                            if (sourceDragablzTabItem.IsMainWindow && !dragablzTabItem.CurrentState.IsMainWindow && !hasWindow)
                            {
                                newTabHost = tabControl.CreateWindow(layoutAccessor.Layout, relatedItem, dragablzTabItem);
                            }
                            else
                            {
                                var relatedSourceItem = sourceOfDragItemsControl.ItemContainerGenerator.ItemFromContainer(relatedItem);
                                tabControl.RemoveItem(relatedItem);
                                newTabHost.TabablzControl.AddToSource(relatedSourceItem);
                            }
                        }

                    }
                }
            }
            return newTabHost;
        }

        //ToDo: Move this region somewhere else.
        #region Save/Restore States
        static List<DragablzTabItem> dragablzTabItems = new List<DragablzTabItem>();
        static List<TabablzControl> tabablzControls = new List<TabablzControl>();
        private static int windowID = 0;
        private static void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            TabablzControl.isMainWindowClosing = true;
            dragablzTabItems.Clear();
            tabablzControls.Clear();
            int counter = 0;
            windowID = 0;
            foreach (Window window in Application.Current.Windows.Cast<Window>().Where(w => w.Content != null))
            {
                if (window == Application.Current.MainWindow)
                    continue;
                windowID++;
                foreach (var tabablzControl in FindVisualChildren<TabablzControl>(window))
                {
                    tabablzControl.CurrentState.Order = counter++;
                    tabablzControls.Add(tabablzControl);
                    SignOrder(tabablzControl, false);
                }
            }
            foreach (Window item in Application.Current.Windows)
            {
                if (item != Application.Current.MainWindow)
                    item.Close();
            }

            windowID = 0;
            foreach (var tabablzControl in FindVisualChildren<TabablzControl>(Application.Current.MainWindow))
            {
                tabablzControl.CurrentState.Order = counter++;
                tabablzControls.Add(tabablzControl);
                SignOrder(tabablzControl, true);
            }
            try
            {
                Settings.Default.Layout = JsonConvert.SerializeObject((dragablzTabItems.Select(d => d.CurrentState).OrderBy(c => c.HeaderName), tabablzControls.Select(t => t.CurrentState).OrderBy(t => t.Order)), Formatting.Indented);
                Settings.Default.Save();
            }
            catch
            {

            }
        }
        private static void SignOrder(TabablzControl tabablzControl, bool isMainWindow)
        {
            int counter = 0;
            foreach (var item in tabablzControl.GetOrderedHeaders())
            {
                if (item.DataContext is DragablzTabItem dragablzTabItem)
                {
                    if (dragablzTabItems.Contains(dragablzTabItem))
                        continue;

                    var newState = dragablzTabItem.CurrentState;
                    var window = Application.Current.MainWindow;
                    if (!isMainWindow)
                        window = Window.GetWindow(dragablzTabItem);

                    newState.WindowWidth = window.ActualWidth;
                    newState.WindowHeight = window.ActualHeight;
                    newState.WindowLocationLeft = window.Left;
                    newState.WindowLocationTop = window.Top;

                    newState.Order = counter++;
                    newState.IsMainWindow = isMainWindow;
                    newState.WindowID = windowID;
                    dragablzTabItem.CurrentState = newState;
                    dragablzTabItems.Add(dragablzTabItem);
                }
            }
        }
        private static void MainWindow_ContentRendered(object sender, EventArgs e)
        {
            hasMainWindowRendered = true;
            foreach (var item in Layouts)
            {
                RestoreItemHelper(item);
            }
            if (openedWindows is not null)
            {
                foreach (var window in openedWindows)
                {
                    window.openWindow.Invoke(window.hasWindow);
                }
            }
        }
        private static void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {

           Settings.Default.Layout = null;

            var l = Settings.Default.Layout;
            if (l is not null)
            {
                Clipboard.SetText(l);

            }
            if (string.IsNullOrWhiteSpace(Settings.Default.Layout))
            {
                hasMainWindowRendered = true;
                return;
            }
            Debug.Write(l);
            states = JsonConvert.DeserializeObject<(DragablzTabItem.State[] dragablzTabItemState, TabablzControl.State[] tabablzControlState)>(l);

            if (states.dragablzTabItemState is null || states.tabablzControlState is null)
            {
                hasMainWindowRendered = true;
                return;
            }

            foreach (var item in states.dragablzTabItemState.OrderBy(s => s.BranchNumber))
            {
                item.BranchNumber = ++branchNumber;
            }

            List<TabablzControl> tabs = new List<TabablzControl>();
            foreach (var tabablzControl in FindVisualChildren<TabablzControl>(sender as Window).OrderBy(t => t.CurrentState.ID))
            {
                tabablzControls.Add(tabablzControl);
                foreach (var item in tabablzControl.GetHeadersOrderedByName())
                {
                    if (item.DataContext is DragablzTabItem dragablzTabItem)
                    {
                        dragablzTabItems.Add(dragablzTabItem);
                    }
                }

                if (dragablzTabItems.Count == states.dragablzTabItemState.Length && dragablzTabItems.Count != 0)
                {
                    RestoreState();
                    dragablzTabItems = dragablzTabItems.OrderBy(i => i.CurrentState.BranchNumber).ToList();

                    var state = dragablzTabItems.Where(d => d.IsMainWindow).FirstOrDefault()?.CurrentState;
                    if (state is null)
                        return;

                    Application.Current.MainWindow.Width = state.WindowWidth;
                    Application.Current.MainWindow.Height = state.WindowHeight;
                    Application.Current.MainWindow.Top = state.WindowLocationTop;
                    Application.Current.MainWindow.Left = state.WindowLocationLeft;
                }
            }
        }

        public static int restoreCounter = 0;
        private static (DragablzTabItem.State[] dragablzTabItemState, TabablzControl.State[] tabablzControlState) states;
        private static bool isInitiated;
        private static void RestoreState()
        {
            foreach (var dragablzTabItem in dragablzTabItems.OrderBy(d => d.CurrentState.HeaderName))
            {
                dragablzTabItem.CurrentState = states.dragablzTabItemState[restoreCounter++];
            }
            restoreCounter = 0;
            foreach (var tabablzControl in tabablzControls.OrderBy(d => d.CurrentState.ID))
            {
                tabablzControl.CurrentState = states.tabablzControlState[restoreCounter++];
            }
        }
        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    var child = VisualTreeHelper.GetChild(depObj, i);

                    if (child != null && child is T)
                        yield return (T)child;

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                        yield return childOfChild;
                }
            }
        }
        #endregion
        #endregion

    }
}
