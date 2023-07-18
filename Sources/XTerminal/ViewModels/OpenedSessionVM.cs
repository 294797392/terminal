﻿using DotNEToolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPFToolkit.MVVM;
using XTerminal.Base;
using XTerminal.Base.DataModels;
using XTerminal.Document.Rendering;
using XTerminal.Session;
using XTerminal.Session.Property;
using XTerminal.Sessions;

namespace XTerminal.ViewModels
{
    /// <summary>
    /// 运行时的会话信息
    /// </summary>
    public class OpenedSessionVM : ViewModelBase
    {
        #region 实例变量

        private SessionStatusEnum status;
        private VideoTerminal videoTerminal;
        private Base.DataModels.XTermSession session;

        #endregion

        #region 公开事件

        /// <summary>
        /// 会话状态改变的时候触发
        /// </summary>
        public event Action<OpenedSessionVM, SessionStatusEnum> StatusChanged;

        #endregion

        #region 属性

        /// <summary>
        /// 当前状态
        /// </summary>
        public SessionStatusEnum Status
        {
            get { return this.status; }
            set
            {
                if (this.status != value)
                {
                    this.status = value;
                    this.NotifyPropertyChanged("Status");
                }
            }
        }

        /// <summary>
        /// 会话类型
        /// </summary>
        public SessionTypeEnum Type { get; set; }

        /// <summary>
        /// 用来渲染终端的画布容器
        /// </summary>
        public ITerminalSurfacePanel CanvasPanel { get; set; }

        #endregion

        #region 构造方法

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="session">要打开的会话对象</param>
        public OpenedSessionVM(XTermSession session)
        {
            this.session = session;
            this.ID = session.ID;
            this.Name = session.Name;
            this.Description = session.Description;
            this.Type = (SessionTypeEnum)session.Type;
        }

        #endregion

        #region 公开接口

        public void Open()
        {
            VTInitialOptions initialOptions = new VTInitialOptions()
            {
                SessionType = (SessionTypeEnum)this.session.Type,
                SessionProperties = new SessionProperties()
                {
                    ServerAddress = this.session.Host,
                    ServerPort = this.session.Port,
                    UserName = this.session.UserName,
                    Password = this.session.Password
                },
                TerminalProperties = new TerminalProperties()
                {
                    Columns = this.session.Column,
                    Rows = this.session.Row,
                    Type = TerminalTypeEnum.XTerm
                },
                ReadBufferSize = 8192,
                CursorOption = new CursorOptions()
                {
                    Style = Base.VTCursorStyles.Line,
                    Interval = XTermConsts.CURSOR_BLINK_INTERVAL
                }
            };
            this.videoTerminal = new VideoTerminal();
            this.videoTerminal.SessionStatusChanged += this.VideoTerminal_SessionStatusChanged;
            this.videoTerminal.SurfacePanel = this.CanvasPanel;
            this.videoTerminal.Initialize(initialOptions);
        }

        public void Close()
        {
            if (this.videoTerminal == null)
            {
                return;
            }
            this.videoTerminal.SessionStatusChanged -= this.VideoTerminal_SessionStatusChanged;
            this.videoTerminal.Release();
        }

        #endregion

        #region 实例方法

        #endregion

        #region 事件处理器

        private void VideoTerminal_SessionStatusChanged(VideoTerminal videoTerminal, SessionStatusEnum status)
        {
            this.Status = status;
        }

        #endregion
    }
}
