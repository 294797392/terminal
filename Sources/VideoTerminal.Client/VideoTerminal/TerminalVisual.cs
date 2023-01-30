﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace XTerminal.Controls
{
    /// <summary>
    /// 表示终端上的一个可视化对象（光标，文本块...）
    /// </summary>
    public abstract class TerminalVisual : DrawingVisual
    {
        public TerminalVisual()
        {
        }
    }
}