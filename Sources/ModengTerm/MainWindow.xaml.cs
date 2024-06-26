﻿using ModengTerm;
using ModengTerm.Base;
using ModengTerm.Base.DataModels;
using ModengTerm.Controls;
using ModengTerm.Terminal;
using ModengTerm.Terminal.ViewModels;
using ModengTerm.ViewModels;
using ModengTerm.ViewModels.Terminals;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using WPFToolkit.Utility;
using WPFToolkit.Utils;
using XTerminal.Base;
using XTerminal.Base.DataModels;
using XTerminal.Base.Enumerations;
using XTerminal.UserControls;
using XTerminal.Windows;

namespace XTerminal
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        #region 实例变量

        private OpenedSessionDataTemplateSelector templateSelector;
        private OpenedSessionItemContainerStyleSelector itemContainerStyleSelector;
        private UserInput userInput;
        private OpenedSessionsVM sessionListVM;

        #endregion

        #region 构造方法

        public MainWindow()
        {
            InitializeComponent();

            this.InitializeWindow();
        }

        #endregion

        #region 实例方法

        private void InitializeWindow()
        {
            this.userInput = new UserInput();
            this.templateSelector = new OpenedSessionDataTemplateSelector();
            this.templateSelector.DataTemplateOpenedSession = this.FindResource("DataTemplateOpenedSession") as DataTemplate;
            this.templateSelector.DataTemplateOpenSession = this.FindResource("DataTemplateOpenSession") as DataTemplate;
            ListBoxOpenedSession.ItemTemplateSelector = this.templateSelector;

            this.itemContainerStyleSelector = new OpenedSessionItemContainerStyleSelector();
            this.itemContainerStyleSelector.StyleOpenedSession = this.FindResource("StyleListBoxItemOpenedSession") as Style;
            this.itemContainerStyleSelector.StyleOpenSession = this.FindResource("StyleListBoxItemOpenSession") as Style;
            ListBoxOpenedSession.ItemContainerStyleSelector = this.itemContainerStyleSelector;

            this.sessionListVM = new OpenedSessionsVM();
            ListBoxOpenedSession.DataContext = this.sessionListVM;
            ListBoxOpenedSession.AddHandler(ListBox.MouseWheelEvent, new MouseWheelEventHandler(this.ListBoxOpenedSession_MouseWheel), true);
        }

        private void CreateSession()
        {
            SessionListWindow sessionListWindow = new SessionListWindow();
            sessionListWindow.Owner = this;
            sessionListWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if ((bool)sessionListWindow.ShowDialog())
            {
                XTermSession session = sessionListWindow.SelectedSession;

                ISessionContent content = this.sessionListVM.OpenSession(session);
                ContentControlSession.Content = content;
                ScrollViewerOpenedSession.ScrollToRightEnd();
            }
        }

        /// <summary>
        /// 向SSH服务器发送输入
        /// </summary>
        /// <param name="userInput"></param>
        private void SendUserInput(InputSessionVM sendTo, UserInput userInput)
        {
            if (sendTo.SendAll)
            {
                foreach (InputSessionVM inputSession in this.sessionListVM.SessionList.OfType<InputSessionVM>())
                {
                    inputSession.SendInput(userInput);
                }
            }
            else
            {
                sendTo.SendInput(userInput);
            }
        }

        #endregion

        #region 公开接口

        public void SendToAllTerminal(string text)
        {
            foreach (ShellSessionVM shellSession in this.sessionListVM.ShellSessions)
            {
                shellSession.SendInput(text);
            }
        }

        #endregion

        #region 事件处理器

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.CreateSession();
        }

        private void ListBoxOpenedSession_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SessionItemVM selectedTabItem = ListBoxOpenedSession.SelectedItem as SessionItemVM;
            if (selectedTabItem == null)
            {
                return;
            }

            if (selectedTabItem is OpenSessionVM)
            {
                if (e.RemovedItems.Count > 0)
                {
                    // 点击的是打开Session按钮，返回到上一个选中的SessionTabItem
                    ListBoxOpenedSession.SelectedItem = e.RemovedItems[0];
                }
                else
                {
                    // 如果当前没有任何一个打开的Session，那么重置选中状态，以便于下次可以继续触发SelectionChanged事件
                    ListBoxOpenedSession.SelectedItem = null;
                }
            }
            else
            {
                OpenedSessionVM openedSessionVM = selectedTabItem as OpenedSessionVM;
                ContentControlSession.Content = openedSessionVM.Content;
            }
        }

        private void ListBoxOpenedSession_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double offset = ScrollViewerOpenedSession.HorizontalOffset;

            if (e.Delta > 0)
            {
                ScrollViewerOpenedSession.ScrollToHorizontalOffset(offset - 50);
            }
            else
            {
                ScrollViewerOpenedSession.ScrollToHorizontalOffset(offset + 50);
            }
        }

        private void ButtonOpenSession_Click(object sender, RoutedEventArgs e)
        {
            this.CreateSession();
        }

        private void ButtonCloseSession_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement frameworkElement = sender as FrameworkElement;
            OpenedSessionVM openedSessionVM = frameworkElement.DataContext as OpenedSessionVM;

            this.sessionListVM.CloseSession(openedSessionVM);

            this.sessionListVM.SelectedSession = this.sessionListVM.SessionList.OfType<OpenedSessionVM>().FirstOrDefault();

            if (this.sessionListVM.SelectedSession == null)
            {
                ContentControlSession.Content = null;
                ListBoxOpenedSession.SelectedItem = null;
            }
        }




        private void MenuItemCreateSession_Click(object sender, RoutedEventArgs e)
        {
            CreateSessionOptionTreeWindow window = new CreateSessionOptionTreeWindow();
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            if (!(bool)window.ShowDialog())
            {
                return;
            }

            XTermSession session = window.Session;

            // 在数据库里新建会话
            int code = MTermApp.Context.ServiceAgent.AddSession(session);
            if (code != ResponseCode.SUCCESS)
            {
                MessageBoxUtils.Error("新建会话失败, 错误码 = {0}", code);
                return;
            }

            // 打开会话
            this.CreateSession();
        }

        private void MenuItemAbout_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.Owner = this;
            aboutWindow.ShowDialog();
        }

        private void CheckBoxEnableDebugMode_CheckedChanged(object sender, RoutedEventArgs e)
        {
            //VTDebug.Enabled = CheckBoxEnableDebugMode.IsChecked.Value;
        }

        private void MenuItemDebugWindow_Click(object sender, RoutedEventArgs e)
        {
            //ISessionContent sessionContent = ContentControlSession.Content as ISessionContent;
            //VideoTerminal terminalSession = sessionContent.DataContext as VideoTerminal;
            //if (terminalSession == null)
            //{
            //    return;
            //}

            //DebugWindow debugWindow = new DebugWindow();
            //debugWindow.VideoTerminal = terminalSession;
            //debugWindow.Owner = this;
            //debugWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            //debugWindow.ShowDialog();
        }




        /// <summary>
        /// 输入中文的时候会触发该事件
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            base.OnPreviewTextInput(e);

            InputSessionVM selectedSession = ListBoxOpenedSession.SelectedItem as InputSessionVM;
            if (selectedSession == null)
            {
                return;
            }

            this.userInput.CapsLock = Console.CapsLock;
            this.userInput.Key = VTKeys.GenericText;
            this.userInput.Text = e.Text;
            this.userInput.Modifiers = VTModifierKeys.None;

            this.SendUserInput(selectedSession, this.userInput);

            e.Handled = true;
        }

        /// <summary>
        /// 从键盘上按下按键的时候会触发
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            InputSessionVM selectedSession = ListBoxOpenedSession.SelectedItem as InputSessionVM;
            if (selectedSession == null)
            {
                return;
            }

            if (e.Key == Key.ImeProcessed)
            {
                // 这些字符交给输入法处理了
            }
            else
            {
                switch (e.Key)
                {
                    case Key.Tab:
                    case Key.Up:
                    case Key.Down:
                    case Key.Left:
                    case Key.Right:
                    case Key.Space:
                        {
                            // 防止焦点移动到其他控件上了
                            e.Handled = true;
                            break;
                        }
                }

                if (e.Key != Key.ImeProcessed)
                {
                    e.Handled = true;
                }

                VTKeys vtKey = TermUtils.ConvertToVTKey(e.Key);
                this.userInput.CapsLock = Console.CapsLock;
                this.userInput.Key = vtKey;
                this.userInput.Text = null;
                this.userInput.Modifiers = (VTModifierKeys)e.KeyboardDevice.Modifiers;
                this.SendUserInput(selectedSession, this.userInput);
            }

            e.Handled = true;
        }

        #endregion
    }

    public class OpenedSessionDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate DataTemplateOpenedSession { get; set; }
        public DataTemplate DataTemplateOpenSession { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is OpenSessionVM)
            {
                return this.DataTemplateOpenSession;
            }
            else
            {
                return this.DataTemplateOpenedSession;
            }
        }
    }

    public class OpenedSessionItemContainerStyleSelector : StyleSelector
    {
        public Style StyleOpenedSession { get; set; }
        public Style StyleOpenSession { get; set; }

        public override Style SelectStyle(object item, DependencyObject container)
        {
            if (item is OpenSessionVM)
            {
                return this.StyleOpenSession;
            }
            else
            {
                return this.StyleOpenedSession;
            }
        }
    }
}
