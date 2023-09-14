﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModengTerm.Terminal.Enumerations
{
    public enum SaveModeEnum
    {
        /// <summary>
        /// 保存当前屏幕的内容
        /// </summary>
        SaveScreen,

        /// <summary>
        /// 保存选中的内容
        /// </summary>
        SaveSelected,

        /// <summary>
        /// 保存全部内容
        /// </summary>
        SaveAll
    }
}