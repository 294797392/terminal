﻿using System;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using XTerminal.Base;
using XTerminal.Base.DataModels;

namespace XTerminal.UserControls
{
    /// <summary>
    /// SFTPContentUserControl.xaml 的交互逻辑
    /// </summary>
    public partial class SFTPContentUserControl : SessionContent
    {
        public SFTPContentUserControl()
        {
            InitializeComponent();
        }

        #region SessionContent

        public override int Open(XTermSession session)
        {
            return ResponseCode.SUCCESS;
        }

        public override void Close()
        {

        }

        #endregion
    }
}
