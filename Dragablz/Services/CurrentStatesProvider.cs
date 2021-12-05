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
        static CurrentStatesProvider()
        {
            Init();
        }

        public static void Init()
        {
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
        private static List<Action> openedWindows = null;
        /// <summary>
        /// Restore tabs states from last session in the given layout
        /// </summary>
        /// <param name="layout"></param>
        static bool layoutHasContent = false;
        public static void RestoreItemHelper(Layout layout)
        {
            var layoutAccessor = layout.Query();
            if (layoutAccessor.TabablzControl is null)
                return;

            var tabItems = layoutAccessor.TabablzControl.GetOrderedHeaders();
            if (tabItems is null)
                return;
            layoutAccessor.Layout.InterLayoutClient = new DefaultInterLayoutClient();

            var windows = tabItems.Where(x => x.Content is DragablzTabItem).OrderBy(t => (t.Content as DragablzTabItem).CurrentState.BranchNumber).GroupBy(x => (x.Content as DragablzTabItem).WindowID);
            //make a list of DragablzTabs according to TabablzControl names.
            //make a list of DragablzTabs according to Layout names.
            //current TabablzControl

            if (openedWindows is null)
                openedWindows = new List<Action>();

            var tabControl = layoutAccessor.TabablzControl;
            foreach (var windowItem in windows)
            {
                var layoutItems = windowItem.Where(x => x.DataContext is DragablzTabItem).GroupBy(x => (x.DataContext as DragablzTabItem).LayoutName);
                foreach (var layoutItem in layoutItems)
                {
                    var tabControlItems = layoutItem.Where(x => x.DataContext is DragablzTabItem).OrderBy(t => (t.DataContext as DragablzTabItem).CurrentState.BranchNumber).GroupBy(x => (x.DataContext as DragablzTabItem).TabControlName);
                    foreach (var items in tabControlItems)
                    {
                        if (items.Where(i => (i.DataContext as DragablzTabItem).CurrentState.IsMainWindow).Any())
                        {
                            RestoreTabs(layoutAccessor, tabControl, items);
                        }
                        else
                        {
                            openedWindows.Add(() => RestoreTabs(layoutAccessor, tabControl, items));
                        }
                    }
                }
            }
        }

        private static void RestoreTabs(LayoutAccessor layoutAccessor, TabablzControl tabControl, IGrouping<string, DragablzItem> newItems)
        {
            bool hasHost = false;
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
                newTabHost = GetNewTabHostOfItem(layoutAccessor, tabControl, itemGroup, hasHost);

                //if the layout has no content yet do not do the branch, replace the content instead
                object newContent = null;
                if (newTabHost.Container is Layout layout)
                {
                    layoutAccessor = layout.Query();
                    newContent = layout.Content;
                    hasHost = true;
                    layoutAccessor.Layout.SetCurrentValue(Layout.ContentProperty, newContent);
                    continue;
                }
                else
                    newContent = newTabHost.Container;

                if (!newTabHost.TabablzControl.HasItems)
                    continue;

                if (!layoutHasContent)
                {
                    layoutAccessor.Layout.SetCurrentValue(Layout.ContentProperty, newContent);
                    layoutHasContent = true;
                    continue;
                }

                if (location == DropZoneLocation.Right || location == DropZoneLocation.Bottom)
                {
                    branchItem.FirstItem = layoutAccessor.Layout.Content;
                    branchItem.SecondItem = newContent;
                }
                else
                {
                    branchItem.FirstItem = newContent;
                    branchItem.SecondItem = layoutAccessor.Layout.Content;
                }
                layoutAccessor.Layout.SetCurrentValue(Layout.ContentProperty, branchItem);
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
        private static INewTabHost<UIElement> GetNewTabHostOfItem(LayoutAccessor layoutAccessor, TabablzControl tabControl, IGrouping<DropZoneLocation, DragablzItem> relatedItems, bool hasHost)
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
                if (!sourceDragablzTabItem.IsMainWindow && !hasHost)
                {
                    //openedWindows.Add(() => tabControl.CreateWindow(layoutAccessor.Layout, item));
                    var tabHost = tabControl.CreateWindow(layoutAccessor.Layout, item);
                    newTabHost = tabHost;
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
                            if (sourceDragablzTabItem.IsMainWindow && !dragablzTabItem.CurrentState.IsMainWindow && !hasHost)
                            {
                                var tabHost = tabControl.CreateWindow(layoutAccessor.Layout, relatedItem);
                                newTabHost = tabHost;
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

            Settings.Default.Layout = JsonConvert.SerializeObject((dragablzTabItems.Select(d => d.CurrentState).OrderBy(c => c.HeaderName), tabablzControls.Select(t => t.CurrentState).OrderBy(t => t.ID)), Formatting.Indented);
            Settings.Default.Save();
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
            if (openedWindows is not null)
            {
                foreach (var window in openedWindows)
                {
                    window.Invoke();
                }
            }
        }
        private static void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {

            //  Settings.Default.Layout = null;
            var i = Settings.Default.Layout;
            if (string.IsNullOrWhiteSpace(Settings.Default.Layout))
            {
                hasMainWindowRendered = true;
                return;
            }
            Debug.Write(i);
            states = JsonConvert.DeserializeObject<(DragablzTabItem.State[] dragablzTabItemState, TabablzControl.State[] tabablzControlState)>(i);

            if (states.dragablzTabItemState is null || states.tabablzControlState is null)
            {
                hasMainWindowRendered = true;
                return;
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
                    Layout.branchNumber = dragablzTabItems.Last().BranchNumber;

                    var state = dragablzTabItems.Where(d => d.IsMainWindow).FirstOrDefault()?.CurrentState;
                    Application.Current.MainWindow.Width = state.WindowWidth;
                    Application.Current.MainWindow.Height = state.WindowHeight;
                    Application.Current.MainWindow.Top = state.WindowLocationTop;
                    Application.Current.MainWindow.Left = state.WindowLocationLeft;
                    if (state is null)
                        return;
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
