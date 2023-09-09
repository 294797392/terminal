﻿using DotNEToolkit;
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
using XTerminal.Base.DataModels;
using XTerminal.Base.Enumerations;
using XTerminal.ViewModels;

namespace XTerminal.Windows
{
    /// <summary>
    /// CreateSessionWindow2.xaml 的交互逻辑
    /// </summary>
    public partial class CreateSessionOptionTreeWindow : Window
    {
        private static log4net.ILog logger = log4net.LogManager.GetLogger("CreateSessionOptionTreeWindow");

        #region 实例变量

        private Dictionary<string, Control> contentMap;

        #endregion

        #region 属性

        /// <summary>
        /// 获取当前编辑完成了的会话
        /// </summary>
        public XTermSession Session { get; private set; }

        #endregion

        #region 构造方法

        public CreateSessionOptionTreeWindow()
        {
            InitializeComponent();

            this.InitializeWindow();
        }

        #endregion

        #region 实例方法

        private void InitializeWindow()
        {
            this.contentMap = new Dictionary<string, Control>();

            CreateSessionVM createSessionVM = new CreateSessionVM(XTermApp.Context.ServiceAgent);

            base.DataContext = new CreateSessionVM(XTermApp.Context.ServiceAgent);
        }

        #endregion

        #region 事件处理器

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            CreateSessionVM createSessionVM = this.DataContext as CreateSessionVM;

            XTermSession session = createSessionVM.GetSession();
            if (session == null)
            {
                return;
            }

            this.Session = session;

            base.DialogResult = true;
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            base.DialogResult = false;
        }

        private void TreeViewOptions_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            OptionTreeNodeVM selectedOption = TreeViewOptions.SelectedItem as OptionTreeNodeVM;
            if (selectedOption == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(selectedOption.EntryClass))
            {
                return;
            }

            Control control;
            if (!this.contentMap.TryGetValue(selectedOption.EntryClass, out control))
            {
                try
                {
                    control = ConfigFactory<Control>.CreateInstance(selectedOption.EntryClass);

                    this.contentMap[selectedOption.EntryClass] = control;
                }
                catch (Exception ex)
                {
                    logger.Error("创建页面实例异常", ex);
                    return;
                }
            }

            ContentControlContent.Content = control;
        }

        #endregion
    }
}
