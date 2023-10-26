﻿using ModengTerm.Terminal.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModengTerm.Terminal.Document
{
    /// <summary>
    /// 表示一个UI元素
    /// </summary>
    /// <typeparam name="TDrawingObject"></typeparam>
    public abstract class VTElement<TDrawingObject>
        where TDrawingObject : IDrawingObject
    {
        #region 实例变量

        protected bool arrangeDirty;

        private double offsetX;
        private double offsetY;
        protected IDrawingDocument drawingDocument;

        #endregion

        #region 属性

        /// <summary>
        /// 该对象的类型
        /// </summary>
        public abstract VTDocumentElements Type { get; }

        /// <summary>
        /// 该元素左上角的X坐标
        /// </summary>
        public double OffsetX
        {
            get { return this.offsetX; }
            set
            {
                if (this.offsetX != value)
                {
                    this.offsetX = value;
                    this.SetArrangeDirty(true);
                }
            }
        }

        /// <summary>
        /// 该元素左上角的Y坐标
        /// </summary>
        public double OffsetY
        {
            get { return this.offsetY; }
            set
            {
                if (this.offsetY != value)
                {
                    this.offsetY = value;
                    this.SetArrangeDirty(true);
                }
            }
        }

        /// <summary>
        /// 对应的绘图对象
        /// </summary>
        protected TDrawingObject DrawingObject { get; set; }

        #endregion

        #region 构造方法

        public VTElement(IDrawingDocument drawingDocument)
        {
            this.drawingDocument = drawingDocument;
        }

        #endregion

        #region 公开接口

        /// <summary>
        /// 初始化
        /// </summary>
        public void Initialize()
        {
            if (this.Type == VTDocumentElements.Scrollbar)
            {
                // Scrollbar就不用创建了，因为Scrollbar默认就是存在的
                this.DrawingObject = (TDrawingObject)this.drawingDocument.Scrollbar;
            }
            else
            {
                this.DrawingObject = drawingDocument.CreateDrawingObject<TDrawingObject>(this.Type);
            }

            this.OnInitialize();

            this.DrawingObject.Initialize();
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Release()
        {
            this.OnRelease();

            this.DrawingObject.Release();
        }

        /// <summary>
        /// 在这个阶段，DrawingObject已经被创建出来了
        /// 子类需要做的事情就是对DrawingObject里的一些属性和字段进行初始化赋值
        /// 子类不需要调用DrawingObject的Initialize方法，OnInitialize完之后会自动调用DrawingObject.Initialize
        /// </summary>
        protected abstract void OnInitialize();

        protected abstract void OnRelease();

        /// <summary>
        /// 请求重绘
        /// 首先判断该绘图元素是否需要重绘
        /// 需要再重绘
        /// </summary>
        public abstract void RequestInvalidate();

        #endregion

        #region 实例方法

        protected void SetArrangeDirty(bool dirty)
        {
            if (this.arrangeDirty != dirty)
            {
                this.arrangeDirty = dirty;
            }
        }

        #endregion
    }
}
