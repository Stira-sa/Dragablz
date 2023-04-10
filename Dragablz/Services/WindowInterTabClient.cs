using Dragablz.Core;
using Dragablz.Dockablz;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Dragablz
{
    public class WindowInterTabClient : DefaultInterTabClient
    {
        public override INewTabHost<Window> GetNewHost(IInterTabClient interTabClient, object partition, TabablzControl source)
        {
            if (source == null) throw new ArgumentNullException("source");
            var sourceWindow = Window.GetWindow(source);
            if (sourceWindow == null) throw new ApplicationException("Unable to ascertain source window.");
            var newWindow = (Window)Activator.CreateInstance(sourceWindow.GetType());

            newWindow.Dispatcher.Invoke(new Action(() => { }), DispatcherPriority.DataBind);

            var newTabablzControl = newWindow.LogicalTreeDepthFirstTraversal().OfType<TabablzControl>().FirstOrDefault();
            newTabablzControl.Name = $"T{newTabablzControl.GetHashCode()}";
            if (newTabablzControl == null) throw new ApplicationException("Unable to ascertain tab control.");

            newWindow.Content = new UserControl() { Content = new Layout() { Partition = partition.ToString(), Content = newTabablzControl } };

            if (newTabablzControl.ItemsSource == null)
                newTabablzControl.Items.Clear();

            return new NewTabHost<Window>(newWindow, newTabablzControl);

        }
        //public INewTabHost<Window> GetNewHost(IInterTabClient interTabClient, object partition, TabablzControl source)
        //{
        //    if (source == null) throw new ArgumentNullException("source");
        //    var sourceWindow = Window.GetWindow(source);
        //    if (sourceWindow == null) throw new ApplicationException("Unable to ascertain source window.");
        //    var newWindow = (Window)Activator.CreateInstance(sourceWindow.GetType());

        //    newWindow.Dispatcher.Invoke(new Action(() => { }), DispatcherPriority.DataBind);

        //    var newTabablzControl = newWindow.LogicalTreeDepthFirstTraversal().OfType<TabablzControl>().FirstOrDefault();
        //    newTabablzControl.Name = $"T{newTabablzControl.GetHashCode()}";
        //    if (newTabablzControl == null) throw new ApplicationException("Unable to ascertain tab control.");

        //    if (newTabablzControl.ItemsSource == null)
        //        newTabablzControl.Items.Clear();

        //    return new NewTabHost<Window>(newWindow, newTabablzControl);
        //}

        //public virtual TabEmptiedResponse TabEmptiedHandler(TabablzControl tabControl, Window window)
        //{
        //    return TabEmptiedResponse.CloseWindowOrLayoutBranch;
        //}
    }
}