﻿using ModengTerm.Terminal.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModengTerm.Terminal.Document
{
    /// <summary>
    /// 存储文档里的一个段落信息
    /// </summary>
    public class VTParagraph
    {
        /// <summary>
        /// 表示一个空的段落
        /// </summary>
        public static readonly VTParagraph Empty = new VTParagraph();

        /// <summary>
        /// 判断该段落是否为空
        /// </summary>
        public bool IsEmpty { get { return string.IsNullOrEmpty(this.Content); } }

        /// <summary>
        /// 该段落的创建时间
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// 该段落的纯文件数据，包含换行符
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 该段落的字符列表
        /// </summary>
        public List<List<VTCharacter>> CharacterList { get; set; }

        /// <summary>
        /// 该段落的起始行
        /// </summary>
        public int FirstPhysicsRow { get; set; }

        /// <summary>
        /// 该段落的结束行
        /// </summary>
        public int LastPhysicsRow { get; set; }

        /// <summary>
        /// 段落的起始行的第一个字符索引
        /// </summary>
        public int StartCharacterIndex { get; set; }

        /// <summary>
        /// 段落的结束行的最后一个字符索引
        /// </summary>
        public int EndCharacterIndex { get; set; }

        /// <summary>
        /// 是否是备用缓冲区里的内容
        /// </summary>
        public bool IsAlternate { get; set; }

        /// <summary>
        /// 用来渲染该段落的画布
        /// </summary>
        public IDrawingDocument Canvas { get; set; }

        /// <summary>
        /// 滚动条信息
        /// </summary>
        public VTScrollInfo ScrollInfo { get; set; }

        /// <summary>
        /// 可视区域内的第一行数据
        /// </summary>
        public VTextLine FirstLine { get; set; }

        /// <summary>
        /// 可视区域内的最后一行数据
        /// </summary>
        public VTextLine LastLine { get; set; }

        /// <summary>
        /// 文本选中信息
        /// </summary>
        public VTextSelection TextSelection { get; set; }

        public VTParagraph()
        {
        }

        /// <summary>
        /// 请求重绘该段落
        /// </summary>
        public void RequestInvalidate()
        {
        }
    }
}






