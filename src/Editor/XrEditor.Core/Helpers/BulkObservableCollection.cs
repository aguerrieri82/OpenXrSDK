using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text;

namespace XrEditor
{
    public class BulkObservableCollection<T> : ObservableCollection<T>
    {
        int _updateCount = 0;
        bool _isChanged;


        public void InsertRange(int startIndex, IList<T> items)
        {
            int curI = startIndex;
            foreach (var item in items)
            {
                Insert(curI, item);
                curI++;
            }
            //OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, (IList)items, startIndex));
        }

        public void RemoveRange(int startIndex, int count)
        {
            //var removed = new List<T>(count);

            for (var i = startIndex + count - 1; i >= startIndex; i--)
            {
                //removed.Insert(0, Items[i]);
                RemoveAt(i);
            }

            //OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, (IList)removed, startIndex));
        }

        public void BeginUpdate()
        {
            _updateCount++;
        }

        public void EndUpdate()
        {
            _updateCount--;
            if (_updateCount ==0 && _isChanged)
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                _isChanged = false;
            }
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (_updateCount > 0)
            {
                _isChanged = true;
                return;
            }   
            base.OnCollectionChanged(e);
        }
    }
}
