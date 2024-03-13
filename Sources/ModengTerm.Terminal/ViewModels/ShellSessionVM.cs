﻿using DotNEToolkit;
using Microsoft.Win32;
using ModengTerm.Base;
using ModengTerm.Base.DataModels;
using ModengTerm.Base.Enumerations;
using ModengTerm.Document;
using ModengTerm.Document.Drawing;
using ModengTerm.Document.Enumerations;
using ModengTerm.ServiceAgents;
using ModengTerm.ServiceAgents.DataModels;
using ModengTerm.Terminal.Callbacks;
using ModengTerm.Terminal.DataModels;
using ModengTerm.Terminal.Enumerations;
using ModengTerm.Terminal.Loggering;
using ModengTerm.Terminal.Session;
using ModengTerm.Terminal.Windows;
using ModengTerm.ViewModels;
using ModengTerm.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WPFToolkit.MVVM;
using WPFToolkit.Utility;

namespace ModengTerm.Terminal.ViewModels
{
    public class ShellFunctionMenu : ViewModelBase
    {
        private bool canChecked;
        private bool isChecked;

        public BindableCollection<ShellFunctionMenu> Children { get; set; }

        public bool CanChecked
        {
            get { return this.canChecked; }
            set
            {
                if (this.canChecked != value)
                {
                    this.canChecked = value;
                    this.NotifyPropertyChanged("CanChecked");
                }
            }
        }

        public bool IsChecked
        {
            get { return this.isChecked; }
            set
            {
                if (this.isChecked != value)
                {
                    this.isChecked = value;
                    this.NotifyPropertyChanged("IsChecked");
                }
            }
        }

        public ExecuteShellFunctionCallback Execute { get; private set; }

        public ShellFunctionMenu(string name)
        {
            this.ID = Guid.NewGuid().ToString();
            this.Name = name;
        }

        public ShellFunctionMenu(string name, ExecuteShellFunctionCallback execute)
        {
            this.ID = Guid.NewGuid().ToString();
            this.Name = name;
            this.Execute = execute;
        }

        public ShellFunctionMenu(string name, ExecuteShellFunctionCallback execute, bool canChecked) :
            this(name, execute)
        {
            this.canChecked = canChecked;
        }
    }

    public class ShellSessionVM : OpenedSessionVM
    {
        #region 类变量

        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger("ShellSessionVM");

        #endregion

        #region 实例变量

        private int viewportRow;
        private int viewportColumn;

        /// <summary>
        /// 与终端进行通信的信道
        /// </summary>
        private SessionTransport sessionTransport;

        /// <summary>
        /// 终端引擎
        /// </summary>
        private VideoTerminal videoTerminal;

        private Encoding writeEncoding;

        private bool sendAll;

        /// <summary>
        /// 书签管理器
        /// </summary>
        private VTBookmark bookmarkMgr;

        private RecordStatusEnum recordState;

        /// <summary>
        /// 是否正在运行
        /// </summary>
        private bool isRunning;

        /// <summary>
        /// 提供剪贴板功能
        /// </summary>
        private VTClipboard clipboard;

        private PlaybackStatusEnum playbackStatus;
        private PlaybackStream playbackStream;

        #endregion

        #region 属性

        /// <summary>
        /// 可视区域的行数
        /// </summary>
        public int ViewportRow
        {
            get { return viewportRow; }
            set
            {
                if (this.viewportRow != value)
                {
                    this.viewportRow = value;
                    this.NotifyPropertyChanged("ViewportRow");
                }
            }
        }

        /// <summary>
        /// 可视区域的列数
        /// </summary>
        public int ViewportColumn
        {
            get { return this.viewportColumn; }
            set
            {
                if (this.viewportColumn != value)
                {
                    this.viewportColumn = value;
                    this.NotifyPropertyChanged("ViewportColumn");
                }
            }
        }

        /// <summary>
        /// 向外部公开终端模拟器的控制接口
        /// </summary>
        public IVideoTerminal VideoTerminal { get { return this.videoTerminal; } }

        /// <summary>
        /// 是否向所有终端发送数据
        /// </summary>
        public bool SendAll
        {
            get { return this.sendAll; }
            set
            {
                if (this.sendAll != value)
                {
                    this.sendAll = value;
                    this.NotifyPropertyChanged("SendAll");
                }
            }
        }

        /// <summary>
        /// 该终端的菜单状态
        /// </summary>
        public BindableCollection<ShellFunctionMenu> FunctionMenus { get; private set; }

        /// <summary>
        /// 发送到所有窗口的委托，由外部赋值
        /// </summary>
        public SendToAllTerminalCallback SendToAllCallback { get; set; }

        /// <summary>
        /// 日志记录器
        /// </summary>
        public LoggerManager LoggerManager { get; set; }

        public IDrawingDocument MainDocument { get; set; }
        public IDrawingDocument AlternateDocument { get; set; }

        #endregion

        #region 构造方法

        public ShellSessionVM(XTermSession session) :
            base(session)
        {
        }

        #endregion

        #region OpenedSessionVM Member

        protected override int OnOpen()
        {
            this.recordState = RecordStatusEnum.Stop;
            this.writeEncoding = Encoding.GetEncoding(this.Session.GetOption<string>(OptionKeyEnum.WRITE_ENCODING));
            this.bookmarkMgr = new VTBookmark(this.Session);
            this.clipboard = new VTClipboard()
            {
                MaximumHistory = this.Session.GetOption<int>(OptionKeyEnum.TERM_MAX_CLIPBOARD_HISTORY)
            };

            #region 初始化功能菜单

            this.FunctionMenus = new BindableCollection<ShellFunctionMenu>()
            {
                new ShellFunctionMenu("查找...",  this.Find),
                new ShellFunctionMenu("日志")
                {
                    Children = new BindableCollection<ShellFunctionMenu>()
                    {
                        new ShellFunctionMenu("启动",  this.StartLogger),
                        new ShellFunctionMenu("停止",  this.StopLogger),
                        new ShellFunctionMenu("暂停",  this.PauseLogger),
                        new ShellFunctionMenu("继续",  this.ResumeLogger)
                    }
                },
                new ShellFunctionMenu("复制", this.Copy),
                new ShellFunctionMenu("粘贴", this.Paste),
                new ShellFunctionMenu("全选", this.SelectAll),
                new ShellFunctionMenu("查看剪贴板历史", this.ClipboardHistory),
                //new ShellFunctionMenu("收藏夹")
                //{
                //    Children = new BindableCollection<ShellFunctionMenu>()
                //    {
                //        new ShellFunctionMenu("加入收藏夹", ShellFunctionEnum.AddFavorites, this.AddFavorites),
                //        new ShellFunctionMenu("查看收藏夹", ShellFunctionEnum.FaviritesList, this.FaviritesList),
                //    }
                //},
                //new ShellFunctionMenu("书签")
                //{
                //    Children = new BindableCollection<ShellFunctionMenu>()
                //    {
                //        new ShellFunctionMenu("新建书签", ShellFunctionEnum.AddBookmark, this.AddBookmark),
                //        new ShellFunctionMenu("删除书签", ShellFunctionEnum.RemoveBookmark, this.RemoveBookmark),
                //        new ShellFunctionMenu("查看书签列表", ShellFunctionEnum.BookmarkList, this.BookmarkList),
                //        new ShellFunctionMenu("显示书签栏", ShellFunctionEnum.DisplayBookmark, this.DisplayBookmark),
                //        new ShellFunctionMenu("隐藏书签栏", ShellFunctionEnum.HidenBookmark, this.HidenBookmark),
                //    }
                //},
                new ShellFunctionMenu("录屏")
                {
                    Children = new BindableCollection<ShellFunctionMenu>()
                    {
                        new ShellFunctionMenu("启动录制", this.StartRecord),
                        new ShellFunctionMenu("停止录制", this.StopRecord),
                        new ShellFunctionMenu("暂停录制", this.PauseRecord),
                        new ShellFunctionMenu("恢复录制", this.ResumeRecord),
                        new ShellFunctionMenu("打开回放", this.OpenRecord)
                    }
                },
                new ShellFunctionMenu("保存")
                {
                    Children = new BindableCollection<ShellFunctionMenu>()
                    {
                        new ShellFunctionMenu("当前屏幕内容", this.SaveDocument),
                        new ShellFunctionMenu("选中内容", this.SaveSelection),
                        new ShellFunctionMenu("所有内容", this.SaveAllDocument),
                    }
                },
                new ShellFunctionMenu("发送到所有会话", this.SendToAll, true),
                //new ShellFunctionMenu("交互测试用例")
                //{
                //    Children = new BindableCollection<ShellFunctionMenu>()
                //    {
                //        new ShellFunctionMenu("记录", ShellFunctionEnum.SaveInteractve, this.SaveInteractive),
                //    }
                //},
                //new ShellFunctionMenu("记录调试日志")
                //{
                //    Children = new BindableCollection<ShellFunctionMenu>()
                //    {
                //        new ShellFunctionMenu("Interactive"),
                //        new ShellFunctionMenu("vttestCode"),
                //        new ShellFunctionMenu("RawRead")
                //    }
                //}
                new ShellFunctionMenu("关于", this.About)
            };

            #endregion

            #region 初始化终端

            VTOptions options = new VTOptions()
            {
                Session = this.Session,
                SessionTransport = new SessionTransport(),
                AlternateDocument = this.AlternateDocument,
                MainDocument = this.MainDocument
            };
            this.videoTerminal = new VideoTerminal();
            this.videoTerminal.ViewportChanged += this.VideoTerminal_ViewportChanged;
            this.videoTerminal.Initialize(options);

            #endregion

            #region 连接终端通道

            // 连接SSH服务器
            SessionTransport transport = options.SessionTransport;
            transport.StatusChanged += this.SessionTransport_StatusChanged;
            transport.DataReceived += this.SessionTransport_DataReceived;
            transport.Initialize(this.Session);
            transport.OpenAsync();

            this.sessionTransport = transport;

            #endregion

            this.isRunning = true;

            return ResponseCode.SUCCESS;
        }

        protected override void OnClose()
        {
            if (!this.isRunning)
            {
                return;
            }

            // 停止对终端的日志记录
            this.StopLogger();

            // 停止录制
            this.StopRecord();

            this.sessionTransport.StatusChanged -= this.SessionTransport_StatusChanged;
            this.sessionTransport.DataReceived -= this.SessionTransport_DataReceived;
            this.sessionTransport.Close();
            this.sessionTransport.Release();

            this.videoTerminal.ViewportChanged -= this.VideoTerminal_ViewportChanged;
            this.videoTerminal.Release();

            // 释放剪贴板
            this.clipboard.Release();

            this.isRunning = false;
        }

        #endregion

        #region 实例方法

        private LogFileTypeEnum FilterIndex2FileType(int filterIndex)
        {
            switch (filterIndex)
            {
                case 1: return LogFileTypeEnum.PlainText;
                case 2: return LogFileTypeEnum.HTML;

                default:
                    throw new NotImplementedException();
            }
        }

        private void SaveToFile(ParagraphTypeEnum paragraphType)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "文本文件(*.txt)|*.txt|html文件(*.html)|*.html";
            saveFileDialog.FileName = string.Format("{0}_{1}", this.Session.Name, DateTime.Now.ToString(DateTimeFormat.yyyyMMddhhmmss));
            if ((bool)saveFileDialog.ShowDialog())
            {
                LogFileTypeEnum fileType = this.FilterIndex2FileType(saveFileDialog.FilterIndex);

                try
                {
                    VTParagraph paragraph = this.videoTerminal.CreateParagraph(paragraphType, fileType);
                    File.WriteAllText(saveFileDialog.FileName, paragraph.Content);
                }
                catch (Exception ex)
                {
                    logger.Error("保存日志异常", ex);
                    MessageBoxUtils.Error("保存失败");
                }
            }
        }

        #endregion

        #region 公开接口

        /// <summary>
        /// 向SSH主机发送用户输入
        /// 用户每输入一个字符，就调用一次这个函数
        /// </summary>
        /// <param name="input">用户输入信息</param>
        public void SendInput(UserInput input)
        {
            if (this.sessionTransport.Status != SessionStatusEnum.Connected)
            {
                return;
            }

            VTKeyboard keyboard = this.videoTerminal.Keyboard;

            byte[] bytes = keyboard.TranslateInput(input);
            if (bytes == null)
            {
                return;
            }

            VTDebug.Context.WriteInteractive(VTSendTypeEnum.UserInput, bytes);

            // 这里输入的都是键盘按键
            int code = this.sessionTransport.Write(bytes);
            if (code != ResponseCode.SUCCESS)
            {
                logger.ErrorFormat("处理输入异常, {0}", ResponseCode.GetMessage(code));
            }
        }

        public void SendInput(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            byte[] bytes = this.writeEncoding.GetBytes(text);

            int code = this.sessionTransport.Write(bytes);
            if (code != ResponseCode.SUCCESS)
            {
                logger.ErrorFormat("发送数据失败, {0}", code);
            }
        }

        #endregion

        #region 事件处理器

        private void VideoTerminal_ViewportChanged(IVideoTerminal vt, int newRow, int newColumn)
        {
            this.ViewportRow = newRow;
            this.ViewportColumn = newColumn;
        }

        private void SessionTransport_DataReceived(SessionTransport client, byte[] bytes, int size)
        {
            this.videoTerminal.ProcessData(bytes, size);

            switch (this.recordState)
            {
                case RecordStatusEnum.Pause:
                    {
                        break;
                    }

                case RecordStatusEnum.Stop:
                    {
                        break;
                    }

                case RecordStatusEnum.Recording:
                    {
                        // 拷贝回放数据
                        byte[] frameData = new byte[size];
                        Buffer.BlockCopy(bytes, 0, frameData, 0, frameData.Length);

                        // 写入回放帧
                        PlaybackFrame frame = new PlaybackFrame()
                        {
                            Timestamp = DateTime.Now.ToFileTime(),
                            Data = frameData
                        };
                        this.playbackStream.WriteFrame(frame);

                        break;
                    }

                default:
                    throw new NotImplementedException();
            }
        }

        private void SessionTransport_StatusChanged(object client, SessionStatusEnum status)
        {
            logger.InfoFormat("会话状态发生改变, {0}", status);

            try
            {
                switch (status)
                {
                    case SessionStatusEnum.Connected:
                        {
                            break;
                        }

                    case SessionStatusEnum.Connecting:
                        {
                            break;
                        }

                    case SessionStatusEnum.ConnectionError:
                        {
                            break;
                        }

                    case SessionStatusEnum.Disconnected:
                        {
                            break;
                        }

                    default:
                        throw new NotImplementedException();
                }
            }
            catch (Exception ex)
            {
                logger.Error("SessionTransport_StatusChanged异常", ex);
            }

            base.NotifyStatusChanged(status);
        }

        private void StartLogger()
        {
            LoggerOptionsWindow window = new LoggerOptionsWindow(this.videoTerminal);
            window.Owner = Window.GetWindow(this.Content);
            if ((bool)window.ShowDialog())
            {
                this.LoggerManager.Start(this.videoTerminal, window.Options);
            }
        }

        private void StopLogger()
        {
            this.LoggerManager.Stop(this.videoTerminal);
        }

        private void PauseLogger()
        {
            this.LoggerManager.Pause(this.videoTerminal);
        }

        private void ResumeLogger()
        {
            this.LoggerManager.Resume(this.videoTerminal);
        }

        private void Copy()
        {
            VTParagraph paragraph = this.videoTerminal.GetSelectedParagraph();
            if (paragraph.IsEmpty)
            {
                return;
            }

            this.clipboard.SetData(paragraph);

            // 把数据设置到Windows剪贴板里
            System.Windows.Clipboard.SetText(paragraph.Content);
        }

        private void Paste()
        {
            string text = System.Windows.Clipboard.GetText();
            this.SendInput(text);
        }

        private void SelectAll()
        {
            this.videoTerminal.SelectAll();
        }

        /// <summary>
        /// 显示剪贴板历史记录
        /// </summary>
        private void ClipboardHistory()
        {
            ClipboardParagraphSource clipboardParagraphSource = new ClipboardParagraphSource(this.clipboard);
            clipboardParagraphSource.Session = this.Session;

            ClipboardVM clipboardVM = new ClipboardVM(clipboardParagraphSource, this);
            clipboardVM.SendToAllTerminalDlg = this.SendToAllCallback;

            ParagraphsWindow paragraphsWindow = new ParagraphsWindow(clipboardVM);
            paragraphsWindow.Title = "剪贴板历史";
            paragraphsWindow.Owner = Window.GetWindow(this.Content);
            paragraphsWindow.Show();
        }

        /// <summary>
        /// 选中的内容添加到收藏夹
        /// </summary>
        private void AddFavorites()
        {
            VTParagraph paragraph = this.videoTerminal.GetSelectedParagraph();
            if (paragraph.IsEmpty)
            {
                return;
            }

            Favorites favorites = new Favorites()
            {
                ID = Guid.NewGuid().ToString(),
                Typeface = this.videoTerminal.ActiveDocument.Typeface,
                SessionID = this.Session.ID,
                StartCharacterIndex = paragraph.StartCharacterIndex,
                EndCharacterIndex = paragraph.EndCharacterIndex,
                CharacterList = paragraph.CharacterList,
                CreationTime = paragraph.CreationTime,
            };

            throw new NotImplementedException();

            //int code = this.TerminalAgent.AddFavorites(favorites);
            //if (code != ResponseCode.SUCCESS)
            //{
            //    MTMessageBox.Info("保存失败");
            //}
        }

        /// <summary>
        /// 显示收藏夹列表
        /// </summary>
        private void FaviritesList()
        {
            throw new NotImplementedException();

            //FavoritesParagraphSource favoritesParagraphSource = new FavoritesParagraphSource(this.TerminalAgent);
            //favoritesParagraphSource.Session = this.Session;

            //FavoritesVM favoritesVM = new FavoritesVM(favoritesParagraphSource, this);
            //favoritesVM.SendToAllTerminalDlg = this.SendToAllCallback;

            //ParagraphsWindow paragraphsWindow = new ParagraphsWindow(favoritesVM);
            //paragraphsWindow.Title = "收藏夹列表";
            //paragraphsWindow.Owner = Window.GetWindow(this.Content);
            //paragraphsWindow.Show();
        }

        /// <summary>
        /// 开始录像
        /// </summary>
        private void StartRecord()
        {
            if (this.recordState == RecordStatusEnum.Recording)
            {
                return;
            }

            RecordOptionsVM recordOptionsVM = new RecordOptionsVM();

            RecordOptionsWindow recordOptionsWindow = new RecordOptionsWindow();
            recordOptionsWindow.Owner = Window.GetWindow(this.Content);
            recordOptionsWindow.DataContext = recordOptionsVM;
            if ((bool)recordOptionsWindow.ShowDialog())
            {
                PlaybackFile playbackFile = new PlaybackFile()
                {
                    ID = Guid.NewGuid().ToString(),
                    Name = recordOptionsVM.FileName,
                    Session = this.Session,
                };

                // 先打开录像文件
                this.playbackStream = new PlaybackStream();
                int code = this.playbackStream.OpenWrite(playbackFile);
                if (code != ResponseCode.SUCCESS)
                {
                    MTMessageBox.Error("打开录像文件失败, {0}", ResponseCode.GetMessage(code));
                    return;
                }

                // 然后保存录像记录
                code = this.ServiceAgent.AddPlaybackFile(playbackFile);
                if (code != ResponseCode.SUCCESS)
                {
                    MTMessageBox.Error("录制失败, 保存录制记录失败, {0}", ResponseCode.GetMessage(code));
                    this.playbackStream.Close();
                    return;
                }

                this.recordState = RecordStatusEnum.Recording;
            }
        }

        /// <summary>
        /// 停止录像
        /// </summary>
        private void StopRecord()
        {
            if (this.recordState == RecordStatusEnum.Stop)
            {
                return;
            }

            // TODO：此时文件可能正在被写入，PlaybackFile里做了异常处理，所以直接这么写
            // 需要优化
            this.playbackStream.Close();

            this.recordState = RecordStatusEnum.Stop;
        }

        /// <summary>
        /// 暂停录像
        /// </summary>
        private void PauseRecord()
        {
            if (this.recordState == RecordStatusEnum.Pause)
            {
                return;
            }

            this.recordState = RecordStatusEnum.Pause;
        }

        /// <summary>
        /// 继续录像
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void ResumeRecord()
        {
            if (this.recordState == RecordStatusEnum.Recording)
            {
                return;
            }

            switch (this.recordState)
            {
                case RecordStatusEnum.Stop:
                    {
                        break;
                    }

                case RecordStatusEnum.Recording:
                    {
                        break;
                    }

                case RecordStatusEnum.Pause:
                    {
                        this.recordState = RecordStatusEnum.Recording;
                        break;
                    }

                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// 打开录像
        /// </summary>
        private void OpenRecord()
        {
            OpenRecordWindow openRecordWindow = new OpenRecordWindow();
            openRecordWindow.ServiceAgent = this.ServiceAgent;
            openRecordWindow.Session = this.Session;
            openRecordWindow.DisplayAllPlaybackList = false;
            openRecordWindow.Owner = Window.GetWindow(this.Content);
            openRecordWindow.Show();

            openRecordWindow.InitializeWindow();
        }

        /// <summary>
        /// 查找
        /// </summary>
        private void Find()
        {
            FindVM vtFind = new FindVM(this.videoTerminal);
            FindWindow findWindow = new FindWindow(vtFind);
            findWindow.Owner = Window.GetWindow(this.Content);
            findWindow.Show();
        }

        private void SaveDocument()
        {
            this.SaveToFile(ParagraphTypeEnum.Viewport);
        }

        private void SaveSelection()
        {
            this.SaveToFile(ParagraphTypeEnum.Selected);
        }

        private void SaveAllDocument()
        {
            this.SaveToFile(ParagraphTypeEnum.AllDocument);
        }

        private void SendToAll()
        {
        }

        private void SaveInteractive()
        {
        }

        private void About() 
        {
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.Owner = Application.Current.MainWindow;
            aboutWindow.ShowDialog();
        }

        #endregion
    }
}
