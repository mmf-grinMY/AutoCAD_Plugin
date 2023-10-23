using System;
using System.Windows;
using System.Windows.Controls;

namespace Plugins
{
    /// <summary> Description of FlipPanel. </summary>
    public class FlipPanel : Control
    {
        #region DependencyProperty

        /// <summary> Заголовок главной панели </summary>
        public static readonly DependencyProperty MainHeaderProperty =
            DependencyProperty.Register("MainHeader", typeof(string), typeof(FlipPanel));
        /// <summary> Заголовок дополнительной панели </summary>
        public static readonly DependencyProperty OtherHeaderProperty =
            DependencyProperty.Register("OtherHeader", typeof(string), typeof(FlipPanel));
        /// <summary> Содержимое главной панели </summary>
        public static readonly DependencyProperty MainContentProperty =
            DependencyProperty.Register("MainContent", typeof(DependencyObject), typeof(FlipPanel));
        /// <summary> Содержимое дополнительной панели </summary>
        public static readonly DependencyProperty OtherContentProperty =
            DependencyProperty.Register("OtherContent", typeof(DependencyObject), typeof(FlipPanel));

        #endregion

        public FlipPanel() { }

        #region Property

        /// <summary> Заголовок главной панели </summary>
        public string MainHeader
        {
            get { return GetValue(MainHeaderProperty).ToString(); }
            set { SetValue(MainHeaderProperty, value); }
        }
        /// <summary> Заголовок дополнительной панели </summary>
        public string OtherHeader
        {
            get { return GetValue(OtherHeaderProperty).ToString(); }
            set { SetValue(OtherHeaderProperty, value); }
        }
        /// <summary> Содержимое главной панели </summary>
        public DependencyObject MainContent
        {
            get { return (DependencyObject)GetValue(MainContentProperty); }
            set { SetValue(MainContentProperty, value); }
        }
        /// <summary> Содержимое дополнительной панели </summary>
        public DependencyObject OtherContent
        {
            get { return (DependencyObject)GetValue(OtherContentProperty); }
            set { SetValue(OtherContentProperty, value); }
        }

        #endregion

        #region EventHandlers

        /// <summary> Логика открытия дополнительной панели </summary>
        /// <param name="sender"> Вызывающий событие элемент управления </param>
        /// <param name="e"> Аргументы вызова </param>
        private void OpenExtentedInput_Click(object sender, RoutedEventArgs e)
        {
            (this.Template.FindName("mainInput", this) as StackPanel).Visibility = Visibility.Collapsed;
            (this.Template.FindName("otherInput", this) as StackPanel).Visibility = Visibility.Visible;
        }
        /// <summary> Логика открытия основной панели </summary>
        /// <param name="sender"> Вызывающий событие элемент управления </param>
        /// <param name="e"> Аргументы вызова </param>
        private void OpenSimpleInput_Click(object sender, RoutedEventArgs e)
        {
            (this.Template.FindName("otherInput", this) as StackPanel).Visibility = Visibility.Collapsed;
            (this.Template.FindName("mainInput", this) as StackPanel).Visibility = Visibility.Visible;
        }
        public StackPanel OtherInput => this.Template.FindName("otherInput", this) as StackPanel;

        #endregion

        #region Override Methods

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            var mainBtn = this.Template.FindName("mainButton", this) as Button;
            mainBtn.Click += OpenExtentedInput_Click;
            var otherBtn = this.Template.FindName("otherButton", this) as Button;
            otherBtn.Click += OpenSimpleInput_Click;
        }

        #endregion
    }
}