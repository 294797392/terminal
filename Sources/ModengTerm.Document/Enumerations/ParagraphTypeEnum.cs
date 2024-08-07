﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModengTerm.Document.Enumerations
{
    public enum ParagraphTypeEnum
    {
        /// <summary>
        /// 保存当前屏幕的内容
        /// </summary>
        Viewport,

        /// <summary>
        /// 保存选中的内容
        /// </summary>
        Selected,

        /// <summary>
        /// 保存全部内容
        /// </summary>
        AllDocument
    }
}