using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DevExpress.Xpf.Grid;


namespace MLAH_Mornitoring_UDP
{
    public class CommonBase : INotifyPropertyChanged
    {
        #region 인터페이스 고정 구현부
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        }
        #endregion 인터페이스 고정 구현부    


        #region 생성자 & 콜백
        public CommonBase()
        {
        }
        //UIElement PART_TITLEBAR;
        //public override void OnApplyTemplate()
        //{
        //    Grid gridMain = GetTemplateChild("PART_TITLEBAR") as Grid;
        //    if (gridMain != null)
        //    {
        //        PART_TITLEBAR = gridMain;
        //    }
        //    base.OnApplyTemplate();
        //}

        #endregion 생성자 & 콜백


        // CommonBase.cs
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return false;
            }
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        

    }

    public class MessageNodeChildNodesSelector : IChildNodesSelector
    {
        public IEnumerable SelectChildren(object item)
        {
            if (item is MessageNode node)
            {
                return node.Children;
            }
            return null;
        }
    }

    public class InvertBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool booleanValue)
            {
                return !booleanValue;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool booleanValue)
            {
                return !booleanValue;
            }
            return value;
        }
    }

    public class ObservableRangeCollection<T> : ObservableCollection<T>
    {
        public void AddRange(IEnumerable<T> collection)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));

            CheckReentrancy();

            // 성능을 위해 모든 아이템을 내부 리스트에 먼저 추가
            foreach (var i in collection)
            {
                Items.Add(i);
            }

            // UI에 단 한 번의 변경 알림을 보냄
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
        public void RemoveRange(int index, int count)
        {
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (index + count > Items.Count) throw new ArgumentException("index와 count의 합이 컬렉션의 크기를 초과합니다.");

            CheckReentrancy();

            // C# List<T>의 RemoveRange를 직접 호출하여 성능 향상
            var itemsList = Items as List<T>;
            if (itemsList != null)
            {
                itemsList.RemoveRange(index, count);
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    Items.RemoveAt(index);
                }
            }

            // UI에 단 한 번의 변경 알림을 보냄
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

    //public class RelayCommand : ICommand
    //{
    //    readonly Action<object> _execute;
    //    readonly Predicate<object> _canExecute;
    //    //private RelayCommand innerDataGridSelectionChangedCommand;
    //    //private Action<Window> closeAction;

    //    public RelayCommand(Action<object> execute)
    //        : this(execute, null)
    //    {
    //    }

    //    public RelayCommand(Action<object> execute, Predicate<object> canExecute)
    //    {
    //        _execute = execute ?? throw new ArgumentNullException("execute");

    //        _canExecute = canExecute;
    //    }

    //    //public RelayCommand(RelayCommand innerDataGridSelectionChangedCommand)
    //    //{
    //    //    this.innerDataGridSelectionChangedCommand = innerDataGridSelectionChangedCommand;
    //    //}

    //    //public RelayCommand(Action<Window> closeAction)
    //    //{
    //    //    this.closeAction = closeAction;
    //    //}

    //    public bool CanExecute(object parameter)
    //    {
    //        return (_canExecute == null) ? true : _canExecute(parameter);
    //    }

    //    public event EventHandler CanExecuteChanged
    //    {
    //        add
    //        {
    //            CommandManager.RequerySuggested += value;
    //        }
    //        remove
    //        {
    //            CommandManager.RequerySuggested -= value;
    //        }
    //    }


    //    public void Execute(object parameter)
    //    {
    //        _execute(parameter);
    //    }


    //}

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // CommandManager에 의존하는 부분을 제거하고 표준 C# 이벤트로 변경합니다.
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// CanExecute의 조건이 변경되었음을 명시적으로 알리는 메서드입니다.
        /// 이 메서드를 호출하면 UI가 CanExecute를 다시 호출하여 커맨드의 활성화 상태를 갱신합니다.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            // UI 스레드에서 안전하게 이벤트를 발생시킵니다.
            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
            {
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            });
        }

        public bool CanExecute(object parameter)
        {
            // _canExecute 델리게이트가 지정되지 않았다면 항상 true를 반환합니다.
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }
    }

    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<object, Task> _executeAsync;
        private readonly Predicate<object> _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<object, Task> executeAsync, Predicate<object> canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return !_isExecuting && (_canExecute == null || _canExecute(parameter));
        }

        public async void Execute(object parameter)
        {
            if (CanExecute(parameter))
            {
                try
                {
                    _isExecuting = true;
                    await _executeAsync(parameter);
                }
                finally
                {
                    _isExecuting = false;
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }


    }
}
