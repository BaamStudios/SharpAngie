using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using BaamStudios.SharpAngie.Annotations;

namespace BaamStudios.SharpAngie
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        private readonly Dictionary<string, NotifyCollectionChangedEventHandler> _changeHandlers = new Dictionary<string, NotifyCollectionChangedEventHandler>();

        [IgnoreDataMember]
        public virtual ViewModelBase Parent { get; set; }

        [IgnoreDataMember]
        public virtual string ParentPropertyName { get; set; }

        [IgnoreDataMember]
        public virtual object ParentPropertyIndex { get; set; }

        public delegate void DeepPropertyChangedEventHandler(ViewModelBase sender, string propertyPath, object value);

        public event DeepPropertyChangedEventHandler DeepPropertyChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));

            var propertyInfo = GetType().GetProperty(propertyName);
            var value = propertyInfo != null ? propertyInfo.GetValue(this) : null;
            OnDeepPropertyChanged(this, propertyName, null, null, value);
        }

        private void OnDeepPropertyChanged(ViewModelBase sender, string propertyName, object propertyIndex, string childPath, object value)
        {
            var propertyPath = GetPropertyPath(propertyName, propertyIndex, childPath);

            if (DeepPropertyChanged != null)
                DeepPropertyChanged(sender, propertyPath, value);

            if (Parent != null)
                Parent.OnDeepPropertyChanged(sender, ParentPropertyName, ParentPropertyIndex, propertyPath, value);
        }

        private static string GetPropertyPath(string propertyName, object propertyIndex, string childPropertyPath)
        {
            var path = new StringBuilder();

            if (!string.IsNullOrEmpty(propertyName))
                path.Append(propertyName);

            if (propertyIndex != null)
                path.Append("[" + propertyIndex + "]");

            if (!string.IsNullOrEmpty(childPropertyPath))
            {
                if (path.Length > 0)
                    path.Append(".");
                path.Append(childPropertyPath);
            }

            return path.ToString();
        }

        #region property setter
        
        protected bool SetProperty<T>(ref T backingField, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(value, backingField))
            {
                return false;
            }
            backingField = value;
            var vm = backingField as ViewModelBase;
            if (vm != null)
            {
                vm.Parent = this;
                vm.ParentPropertyName = propertyName;
            }
            OnPropertyChanged(propertyName);
            return true;
        }

        protected bool SetProperty<T>(ref ObservableCollection<T> backingField, ObservableCollection<T> value,
            [CallerMemberName] string propertyName = null)
        {
            if (Equals(value, backingField))
            {
                return false;
            }
            if (backingField != null)
            {
                UnwireCollection(backingField, propertyName);
            }
            backingField = value;
            if (backingField != null)
            {
                WireCollection(backingField, propertyName);
            }
            OnPropertyChanged(propertyName);
            return true;
        }

        private void UnwireCollection<T>(ObservableCollection<T> collection, [CallerMemberName] string propertyName = null)
        {
            if (_changeHandlers.ContainsKey(propertyName))
            {
                collection.CollectionChanged -= _changeHandlers[propertyName];
            }
        }

        private void WireCollection<T>(ObservableCollection<T> collection, [CallerMemberName] string propertyName = null)
        {
            if (!_changeHandlers.ContainsKey(propertyName))
            {
                _changeHandlers.Add(propertyName, (sender, args) =>
                {
                    WireCollectionItems((IEnumerable)sender, propertyName);
                    OnPropertyChanged(propertyName);
                });
            }
            collection.CollectionChanged += _changeHandlers[propertyName];

            WireCollectionItems(collection, propertyName);
        }

        private void WireCollectionItems(IEnumerable enumerable, string propertyName)
        {
            var index = 0;
            foreach (var item in enumerable)
            {
                var vm = item as ViewModelBase;
                if (vm != null)
                {
                    vm.Parent = this;
                    vm.ParentPropertyName = propertyName;
                    vm.ParentPropertyIndex = index;
                }
                index++;
            }
        }

        #endregion
    }
}