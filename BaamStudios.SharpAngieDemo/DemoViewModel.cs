using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Markup;
using BaamStudios.SharpAngie;
using DrWPF.Windows.Data;

namespace BaamStudios.SharpAngieDemo
{
    public class DemoViewModel : ViewModelBase
    {
        private string _field1;

        private string _field2;

        private DemoViewModel _child;

        private ObservableCollection<DemoViewModel> _children;

        private ObservableDictionary<string, DemoViewModel> _indexedChildren;

        public string Field1
        {
            get
            {
                return _field1;
            }
            set
            {
                SetProperty(ref _field1, value);
            }
        }

        public string Field2
        {
            get
            {
                return _field2;
            }
            set
            {
                SetProperty(ref _field2, value);
            }
        }

        public DemoViewModel Child
        {
            get
            {
                return _child;
            }
            set
            {
                SetProperty(ref _child, value);
            }
        }

        public ObservableCollection<DemoViewModel> Children
        {
            get
            {
                return _children;
            }
            set
            {
                SetProperty(ref _children, value);
            }
        }

        public ObservableDictionary<string, DemoViewModel> IndexedChildren
        {
            get
            {
                return _indexedChildren;
            }
            set
            {
                SetProperty(ref _indexedChildren, value);
            }
        }

        public void DoSomething(string param1, string param2)
        {
            Field1 = param1;
            Field2 = param2;
            if (Children == null)
            {
                Children =
                    new ObservableCollection<DemoViewModel>(new List<DemoViewModel> { new DemoViewModel { Field1 = "v3" } });
            }
            else
            {
                Children.Add(new DemoViewModel { Field1 = Children.Count.ToString() });
            }
        }
    }
}
