using Dragablz.Dockablz;
using System.Windows;
using System.Windows.Controls;

namespace Dragablz
{
    /// <summary>
    /// this class has been made to get the ability of preset tab location
    /// </summary>
    public class DragablzTabItem : TabItem
    {
        /// <summary>
        /// preset location for branching 
        /// </summary>
        public DropZoneLocation Location
        {
            get { return (DropZoneLocation)GetValue(LocationProperty); }
            set { SetValue(LocationProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Location.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LocationProperty =
            DependencyProperty.Register("Location", typeof(DropZoneLocation), typeof(DragablzTabItem), new PropertyMetadata(DropZoneLocation.Unset));

        /// <summary>
        /// Layout Identifier for which layout this tab belongs to.
        /// </summary>
        public string LayoutName
        {
            get
            {
                if ((Parent as TabablzControl)?.Parent is Layout layout && string.IsNullOrEmpty((string)GetValue(LayoutNameProperty)))
                {
                    if (string.IsNullOrEmpty(layout.Name))
                        layout.SetValue(NameProperty, $"L{layout.GetHashCode()}");
                    SetValue(LayoutNameProperty, layout.Name);
                    return layout.Name;
                }
                return (string)GetValue(LayoutNameProperty);
            }
            set { SetValue(LayoutNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LayoutName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LayoutNameProperty =
            DependencyProperty.Register("LayoutName", typeof(string), typeof(DragablzTabItem), new PropertyMetadata(""));
        /// <summary>
        /// TabControl Identifier for which TabControl this tab belongs to.
        /// </summary>
        public string TabControlName
        {
            get
            {
                if (Parent is TabablzControl tabablzControl && string.IsNullOrEmpty((string)GetValue(TabControlNameProperty)))
                {
                    if (string.IsNullOrEmpty(tabablzControl.Name))
                        tabablzControl.SetValue(NameProperty, $"T{tabablzControl.GetHashCode()}");

                    SetValue(TabControlNameProperty, tabablzControl.Name);
                    return tabablzControl.Name;
                }
                return (string)GetValue(TabControlNameProperty);
            }
            set
            {
                SetValue(TabControlNameProperty, value);
            }
        }

        // Using a DependencyProperty as the backing store for TabControlName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TabControlNameProperty =
            DependencyProperty.Register("TabControlName", typeof(string), typeof(DragablzTabItem), new PropertyMetadata(""));

        /// <summary>
        /// Indicates if this tab is in the application main window.
        /// </summary>
        public bool IsMainWindow
        {
            get { return (bool)GetValue(IsMainWindowProperty); }
            set { SetValue(IsMainWindowProperty, value); }
        }
        // Using a DependencyProperty as the backing store for IsMainWindow.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsMainWindowProperty =
            DependencyProperty.Register("IsMainWindow", typeof(bool), typeof(DragablzTabItem), new PropertyMetadata(true));

        /// <summary>
        /// Order Identifier to help to restore last session tabs
        /// </summary>
        public int Order
        {
            get { return (int)GetValue(OrderProperty); }
            set { SetValue(OrderProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Order.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OrderProperty =
            DependencyProperty.Register("Order", typeof(int), typeof(DragablzTabItem), new PropertyMetadata(0));


        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }

        public int BranchNumber { get; set; }
        public int WindowID { get; set; }
        public double WindowLocationLeft { get; set; }
        public double WindowLocationTop { get; set; }
        public double WindowWidth { get; set; }
        public double WindowHeight { get; set; }
        /// <summary>
        /// has all the required properties to Save/Restore last session tabs
        /// </summary>
        public State CurrentState
        {
            get => new DragablzTabItem.State
            {

                LayoutName = LayoutName,
                Location = Location,
                IsMainWindow = IsMainWindow,
                TabControlName = TabControlName,
                HeaderName = Header,
                Order = Order,
                ID = ++Layout.branchNumber,
                BranchNumber = BranchNumber,
                WindowID = WindowID,
                WindowWidth = WindowWidth,
                WindowHeight = WindowHeight,
                WindowLocationTop = WindowLocationTop,
                WindowLocationLeft = WindowLocationLeft,

            };
            set
            {
                LayoutName = value.LayoutName;
                Location = value.Location;
                TabControlName = value.TabControlName;
                Header = value.HeaderName;
                Order = value.Order;
                BranchNumber = value.BranchNumber;
                IsMainWindow = value.IsMainWindow;
                WindowID = value.WindowID;
                WindowWidth = value.WindowWidth;
                WindowHeight = value.WindowHeight;
                WindowLocationTop = value.WindowLocationTop;
                WindowLocationLeft = value.WindowLocationLeft;
            }
        }
        public class State
        {
            public int ID { get; set; }
            public string LayoutName { get; set; }
            public DropZoneLocation Location { get; set; }
            public bool IsMainWindow { get; set; }
            public string TabControlName { get; set; }
            public object HeaderName { get; set; }
            public int Order { get; set; }
            public int BranchNumber { get; set; }
            public int WindowID { get; set; }
            public double WindowLocationLeft { get; set; }
            public double WindowLocationTop { get; set; }
            public double WindowWidth { get; set; }
            public double WindowHeight { get; set; }
        }
    }
}
