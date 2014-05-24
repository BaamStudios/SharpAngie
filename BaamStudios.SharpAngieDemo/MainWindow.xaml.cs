using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BaamStudios.SharpAngieDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            WebControl.Source = new Uri(System.IO.Path.Combine(Directory.GetCurrentDirectory(), "DemoView.html"), UriKind.Absolute);
            var viewModel = new DemoViewModel
                            {
                                Field1 = "foo",
                                Child = new DemoViewModel
                                        {
                                            Field2 = "bar"
                                        },
                                Children = new ObservableCollection<DemoViewModel>(new List<DemoViewModel> { new DemoViewModel { Field1 = "baz" } })
                            };
            DataContext = viewModel;
            new WebViewBridge(WebControl, viewModel);
        }
    }
}
