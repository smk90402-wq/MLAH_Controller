using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;

namespace MLAH_Controller
{


    public class BindableSelectedItemBehavior : Behavior<System.Windows.Controls.TreeView>
    {
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(
                "SelectedItem",
                typeof(object),
                typeof(BindableSelectedItemBehavior),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

        public object SelectedItem
        {
            get { return GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.SelectedItemChanged += OnTreeViewSelectedItemChanged;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (this.AssociatedObject != null)
            {
                this.AssociatedObject.SelectedItemChanged -= OnTreeViewSelectedItemChanged;
            }
        }

        private static void OnSelectedItemChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            // 여기서는 TreeView.SelectedItem에 값을 할당하지 않습니다.
        }

        private void OnTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            this.SelectedItem = this.AssociatedObject.SelectedItem;
        }
    }

    public class BindableSelectedItemBehaviorForDataGrid : Behavior<DataGrid>
    {
        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
            "SelectedItem",
            typeof(object),
            typeof(BindableSelectedItemBehaviorForDataGrid),
            new PropertyMetadata(null, OnSelectedItemChanged));

        public object SelectedItem
        {
            get { return GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.SelectionChanged += AssociatedObject_SelectionChanged;
        }

        protected override void OnDetaching()
        {
            if (this.AssociatedObject != null)
            {
                this.AssociatedObject.SelectionChanged -= AssociatedObject_SelectionChanged;
            }
            base.OnDetaching();
        }

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = d as BindableSelectedItemBehaviorForDataGrid;
            if (behavior?.AssociatedObject != null && behavior.AssociatedObject.SelectedItem != e.NewValue)
            {
                behavior.AssociatedObject.SelectedItem = e.NewValue;
            }
        }

        private void AssociatedObject_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedItem = AssociatedObject.SelectedItem;
        }
    }

    public class BindableSelectedItemBehaviorForListView : Behavior<System.Windows.Controls.ListView>
    {
        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
            "SelectedItem",
            typeof(object),
            typeof(BindableSelectedItemBehaviorForListView),
            new PropertyMetadata(null, OnSelectedItemChanged));

        public object SelectedItem
        {
            get { return GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.SelectionChanged += OnListViewSelectionChanged;
        }

        protected override void OnDetaching()
        {
            if (this.AssociatedObject != null)
            {
                this.AssociatedObject.SelectionChanged -= OnListViewSelectionChanged;
            }
            base.OnDetaching();
        }

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = d as BindableSelectedItemBehaviorForListView;
            if (behavior?.AssociatedObject != null && behavior.AssociatedObject.SelectedItem != e.NewValue)
            {
                behavior.AssociatedObject.SelectedItem = e.NewValue;
            }
        }

        private void OnListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedItem = AssociatedObject.SelectedItem;
        }
    }

}