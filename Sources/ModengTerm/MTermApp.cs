﻿using DotNEToolkit;
using ModengTerm.Base;
using ModengTerm.Base.Definitions;
using ModengTerm.Base.ServiceAgents;
using ModengTerm.Terminal;
using ModengTerm.Terminal.Loggering;
using ModengTerm.UserControls.OptionsUserControl;
using ModengTerm.UserControls.OptionsUserControl.RawTcp;
using ModengTerm.UserControls.OptionsUserControl.SSH;
using ModengTerm.UserControls.OptionsUserControl.Terminal;
using ModengTerm.ViewModels;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Threading;
using XTerminal.UserControls.OptionsUserControl;

namespace ModengTerm
{
    public class MTermApp : ModularApp<MTermApp, MTermManifest>, INotifyPropertyChanged
    {
        /// <summary>
        /// C#的代码混淆工具不能进行反射，因为会修改类名，所以把要动态创建的控件实例类型写死
        /// </summary>
        public static readonly List<OptionDefinition> TerminalOptionList = new List<OptionDefinition>()
        {
            new OptionDefinition("会话")
            {
                Children = new List<OptionDefinition>()
                {
                    new OptionDefinition(OptionDefinition.CommandLineID, "命令行", typeof(CommandLineOptionsUserControl)),
                    new OptionDefinition(OptionDefinition.SshID, "SSH", typeof(SSHOptionsUserControl))
                    {
                        Children = new List<OptionDefinition>()
                        {
                            new OptionDefinition("端口转发", typeof(PortForwardOptionsUserControl))
                        }
                    },
                    new OptionDefinition(OptionDefinition.SerialPortID, "串口", typeof(SerialPortOptionsUserControl)),
                    new OptionDefinition(OptionDefinition.RawTcpID, "Tcp", typeof(RawTcpOptionsUserControl)),
                }
            },

            new OptionDefinition("终端", typeof(TerminalOptionsUserControl))
            {
                Children = new List<OptionDefinition>()
                {
                    new OptionDefinition("外观", typeof(ThemeOptionsUserControl)),
                    new OptionDefinition("行为", typeof(BehaviorOptionsUserControl)),
                    new OptionDefinition("高级", typeof(AdvanceOptionsUserControl))
                }
            },
        };

        #region 实例变量

        private OpenedSessionVM selectedOpenedSession;

        private DispatcherTimer drawFrameTimer;

        #endregion

        #region 属性

        /// <summary>
        /// 访问服务的代理
        /// </summary>
        public ServiceAgent ServiceAgent { get; private set; }

        /// <summary>
        /// 日志记录器
        /// </summary>
        public LoggerManager LoggerManager { get; private set; }

        /// <summary>
        /// 主窗口ViewModel
        /// </summary>
        public MainWindowVM MainWindowVM { get; private set; }

        #endregion

        #region ModularApp

        protected override int OnInitialize()
        {
            this.ServiceAgent = new LocalServiceAgent();
            this.ServiceAgent.Initialize();

            this.LoggerManager = new LoggerManager();
            this.LoggerManager.Initialize();

            // 在最后初始化ViewModel，因为ViewModel里可能会用到ServiceAgent
            this.MainWindowVM = new MainWindowVM();

            VTApp.Context.ServiceAgent = this.ServiceAgent;
            VTApp.Context.Initialize();

            return ResponseCode.SUCCESS;
        }

        protected override void OnRelease()
        {
        }

        #endregion

        #region 实例方法

        #endregion

        #region 公开接口

        #endregion

        #region 实例方法

        //private void ProcessFrame(int elapsed, IFramedElement element)
        //{
        //    element.Elapsed -= elapsed;

        //    if (element.Elapsed <= 0)
        //    {
        //        // 渲染
        //        try
        //        {
        //            element.RequestInvalidate();
        //        }
        //        catch (Exception ex)
        //        {
        //            logger.Error("RequestInvalidate运行异常", ex);
        //        }

        //        element.Elapsed = element.Delay;
        //    }
        //}

        #endregion

        #region 事件处理器

        /// <summary>
        /// 光标闪烁线程
        /// 所有的光标都在这一个线程运行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //private void DrawFrameTimer_Tick(object sender, EventArgs e)
        //{
        //    IEnumerable<ShellSessionVM> vtlist = this.OpenedTerminals;

        //    foreach (ShellSessionVM vt in vtlist)
        //    {
        //        // 如果当前界面上没有显示终端，那么不处理帧
        //        FrameworkElement frameworkElement = vt.Content as FrameworkElement;
        //        if (!frameworkElement.IsLoaded)
        //        {
        //            continue;
        //        }

        //        VTDocument activeDocument = vt.VideoTerminal.ActiveDocument;

        //        int elapsed = this.drawFrameTimer.Interval.Milliseconds;

        //        //this.ProcessFrame(elapsed, activeDocument.Cursor);

        //        //switch (vt.Background.PaperType)
        //        //{
        //        //    case WallpaperTypeEnum.Live:
        //        //        {
        //        //            this.ProcessFrame(elapsed, vt.Background);
        //        //            break;
        //        //        }

        //        //    case WallpaperTypeEnum.Image:
        //        //    case WallpaperTypeEnum.Color:
        //        //        {
        //        //            if (vt.Background.Effect != EffectTypeEnum.None)
        //        //            {
        //        //                this.ProcessFrame(elapsed, vt.Background);
        //        //            }
        //        //            break;
        //        //        }

        //        //    default:
        //        //        {
        //        //            break;
        //        //        }
        //        //}
        //    }
        //}

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}
