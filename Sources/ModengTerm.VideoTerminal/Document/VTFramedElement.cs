﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XTerminal.Document;

namespace ModengTerm.Terminal.Document
{
    /// <summary>
    /// 表示一个需要一帧一帧渲染的，实时渲染的元素
    /// </summary>
    public abstract class VTFramedElement : VTDocumentElement
    {
        /// <summary>
        /// 显示下一帧的间隔时间
        /// 有可能每一帧之间的间隔都是不一样的
        /// </summary>
        public double FrameInterval { get; set; }

        /// <summary>
        /// 还剩余多少时间就渲染
        /// </summary>
        public double LeftTime { get; set; }
    }
}
