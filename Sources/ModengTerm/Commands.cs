﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ModengTerm
{
    public static class MCommands
    {
        /// <summary>
        /// 快捷输入ShellCommand命令
        /// </summary>
        public static RoutedUICommand SendCommand { get; private set; }


        public static RoutedUICommand PanelVisiblityCommand { get; private set; }

        static MCommands()
        {
            SendCommand = new RoutedUICommand();
            PanelVisiblityCommand = new RoutedUICommand();
        }
    }
}
