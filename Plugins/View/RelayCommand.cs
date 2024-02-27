using System.Windows.Input;
using System;

namespace Plugins
{
    /// <summary> Команда выполнения </summary>
    internal class RelayCommand : ICommand
    {
        /// <summary> Выполняемое действие команды </summary>
        private readonly Action<object> execute;
        /// <summary> Проверка команды на возможность выполнения </summary>
        private readonly Func<object, bool> canExecute;
        /// <summary> Изменение параметра команды "Возможность выполнения" </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
        /// <summary> Основной конструктор класса </summary>
        /// <param name="execute"> Выполняемое командой действие </param>
        /// <param name="canExecute"> Возможность выполнения команды </param>
        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }
        /// <summary> Проверка на возможность выполнения команды </summary>
        /// <param name="parameter">Параметр метода "Возможность выполнить"</param>
        /// <returns>true, если команду можно выполнить, false в противном случае</returns>
        public bool CanExecute(object parameter) => this.canExecute == null || this.canExecute(parameter);
        /// <summary>Вызов выполнения команды</summary>
        /// <param name="parameter">Параметр команды</param>
        public void Execute(object parameter) => this.execute(parameter);
    }
}
