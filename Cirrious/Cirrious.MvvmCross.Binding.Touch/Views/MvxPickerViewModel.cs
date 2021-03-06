// MvxPickerViewModel.cs
// (c) Copyright Cirrious Ltd. http://www.cirrious.com
// MvvmCross is licensed using Microsoft Public License (Ms-PL)
// Contributions and inspirations noted in readme.md and license.txt
// 
// Project Lead - Stuart Lodge, @slodge, me@slodge.com

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Windows.Input;
using Cirrious.CrossCore;
using Cirrious.CrossCore.WeakSubscription;
using Cirrious.MvvmCross.Binding.Attributes;
using Cirrious.MvvmCross.Binding.ExtensionMethods;
using UIKit;

namespace Cirrious.MvvmCross.Binding.Touch.Views
{
    public class MvxPickerViewModel
        : UIPickerViewModel
    {
        private readonly UIPickerView _pickerView;
        private IEnumerable _itemsSource;
        private IDisposable _subscription;
        private object _selectedItem;

        public bool ReloadOnAllItemsSourceSets { get; set; }

        public MvxPickerViewModel(UIPickerView pickerView)
        {
            _pickerView = pickerView;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_subscription != null)
                {
                    _subscription.Dispose();
                    _subscription = null;
                }
            }

            base.Dispose(disposing);
        }

        [MvxSetToNullAfterBinding]
        public virtual IEnumerable ItemsSource
        {
            get { return _itemsSource; }
            set
            {
                if (Object.ReferenceEquals(_itemsSource, value)
                    && !ReloadOnAllItemsSourceSets)
                    return;

                if (_subscription != null)
                {
                    _subscription.Dispose();
                    _subscription = null;
                }

                _itemsSource = value;

                var collectionChanged = _itemsSource as INotifyCollectionChanged;
                if (collectionChanged != null)
                {
                    _subscription = collectionChanged.WeakSubscribe(CollectionChangedOnCollectionChanged);
                }

                Reload();
                ShowSelectedItem();
            }
        }

        protected virtual void CollectionChangedOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Mvx.Trace(
                "CollectionChanged called inside MvxPickerViewModel - beware that this isn't fully tested - picker might not fully support changes while the picker is visible");
            Reload();
        }

        protected virtual void Reload()
        {
            _pickerView.ReloadComponent(0);
        }

        public override int GetComponentCount(UIPickerView picker)
        {
            return 1;
        }

        public override int GetRowsInComponent(UIPickerView picker, int component)
        {
            return _itemsSource == null ? 0 : _itemsSource.Count();
        }

        public override string GetTitle(UIPickerView picker, int row, int component)
        {
            return _itemsSource == null ? "-" : RowTitle(row, _itemsSource.ElementAt(row));
        }

        protected virtual string RowTitle(int row, object item)
        {
            return item.ToString();
        }

        public override void Selected(UIPickerView picker, int row, int component)
        {
            _selectedItem = _itemsSource.ElementAt(row);

            var handler = SelectedItemChanged;
            if (handler != null)
                handler(this, EventArgs.Empty);

            var command = SelectedChangedCommand;
            if (command != null)
                if (command.CanExecute(_selectedItem))
                    command.Execute(_selectedItem);
        }

        public object SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                _selectedItem = value;
                ShowSelectedItem();
            }
        }

        public event EventHandler SelectedItemChanged;

        public ICommand SelectedChangedCommand { get; set; }

        protected virtual void ShowSelectedItem()
        {
            if (_itemsSource == null)
                return;

            var position = _itemsSource.GetPosition(_selectedItem);
            if (position < 0)
                return;

            var animated = !_pickerView.Hidden;
            _pickerView.Select(position, 0, animated);
        }
    }
}