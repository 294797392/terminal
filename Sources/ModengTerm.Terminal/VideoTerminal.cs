﻿using ModengTerm.Base.DataModels;
using ModengTerm.Base.Enumerations;
using ModengTerm.Document;
using ModengTerm.Document.Drawing;
using ModengTerm.Document.Enumerations;
using ModengTerm.Document.Utility;
using ModengTerm.Terminal.Loggering;
using ModengTerm.Terminal.Session;
using System.Text;
using XTerminal.Base.Definitions;
using XTerminal.Parser;

namespace ModengTerm.Terminal
{
    public class VTOptions
    {
        public IDrawingDocument AlternateDocument { get; set; }

        public IDrawingDocument MainDocument { get; set; }

        /// <summary>
        /// 该终端所对应的Session
        /// </summary>
        public XTermSession Session { get; set; }

        /// <summary>
        /// 发送数据给主机的回调
        /// </summary>
        public SessionTransport SessionTransport { get; set; }
    }

    /// <summary>
    /// 处理虚拟终端的所有逻辑
    /// 主缓冲区：文档物理行数是不固定的，可以大于终端行数
    /// 备用缓冲区：文档的物理行数是固定的，等于终端的行数
    /// </summary>
    public class VideoTerminal : IVideoTerminal
    {
        #region 类变量

        private static log4net.ILog logger = log4net.LogManager.GetLogger("VideoTerminal");

        private static readonly byte[] OS_OperatingStatusData = new byte[4] { 0x1b, (byte)'[', (byte)'0', (byte)'n' };
        private static readonly byte[] DA_DeviceAttributesData = new byte[7] { 0x1b, (byte)'[', (byte)'?', (byte)'1', (byte)':', (byte)'0', (byte)'c' };

        #endregion

        #region 公开事件

        /// <summary>
        /// 当某一行被完整打印之后触发
        /// </summary>
        public event Action<IVideoTerminal, VTHistoryLine> LinePrinted;

        /// <summary>
        /// 当切换显示文档之后触发
        /// IVideoTerminal：事件触发者
        /// VTDocument：oldDocument，切换之前显示的文档
        /// VTDocument：newDocument，切换之后显示的文档
        /// </summary>
        public event Action<IVideoTerminal, VTDocument, VTDocument> DocumentChanged;

        /// <summary>
        /// 当可视区域的行或列改变的时候触发
        /// </summary>
        public event Action<IVideoTerminal, int, int> ViewportChanged;

        #endregion

        #region 实例变量

        private SessionTransport sessionTransport;

        /// <summary>
        /// 终端字符解析器
        /// </summary>
        private VTParser vtParser;

        /// <summary>
        /// 主缓冲区文档模型
        /// </summary>
        private VTDocument mainDocument;

        /// <summary>
        /// 备用缓冲区文档模型
        /// </summary>
        private VTDocument alternateDocument;

        /// <summary>
        /// 当前正在使用的文档模型
        /// </summary>
        private VTDocument activeDocument;

        /// <summary>
        /// UI线程上下文
        /// </summary>
        internal SynchronizationContext uiSyncContext;

        #region Mouse

        /// <summary>
        /// 鼠标滚轮滚动一次，滚动几行
        /// </summary>
        private int scrollDelta;

        #endregion

        /// <summary>
        /// DECAWM是否启用
        /// </summary>
        private bool autoWrapMode;

        private bool xtermBracketedPasteMode;

        private IDrawingDocument documentCanvas;

        /// <summary>
        /// 是否正在运行
        /// </summary>
        private bool isRunning;

        /// <summary>
        /// 输入编码方式
        /// </summary>
        private Encoding writeEncoding;

        /// <summary>
        /// 根据当前电脑键盘的按键状态，转换成标准的ANSI控制序列
        /// </summary>
        private VTKeyboard keyboard;

        /// <summary>
        /// 终端颜色表
        /// ColorName -> RgbKey
        /// </summary>
        private VTColorTable colorTable;
        internal string foregroundColor;
        internal string backgroundColor;

        private VTOptions vtOptions;

        #endregion

        #region 属性

        /// <summary>
        /// 会话名字
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 获取当前光标所在行
        /// </summary>
        private VTextLine ActiveLine { get { return this.activeDocument.ActiveLine; } }

        /// <summary>
        /// 获取当前光标所在行
        /// </summary>
        private int CursorRow { get { return Cursor.Row; } }

        /// <summary>
        /// 获取当前光标所在列
        /// 下一个字符要显示的位置
        /// </summary>
        private int CursorCol { get { return Cursor.Column; } }

        /// <summary>
        /// activeDocument的光标信息
        /// 该坐标是基于ViewableDocument的坐标
        /// Cursor的位置是下一个要打印的字符的位置
        /// </summary>
        public VTCursor Cursor { get { return this.activeDocument.Cursor; } }

        public VTScrollInfo ScrollInfo { get { return this.activeDocument.Scrollbar; } }

        /// <summary>
        /// 当前正在使用的缓冲区
        /// </summary>
        public VTDocument ActiveDocument { get { return this.activeDocument; } }

        /// <summary>
        /// 备用缓冲区
        /// </summary>
        public VTDocument AlternateDocument { get { return this.alternateDocument; } }

        /// <summary>
        /// 主缓冲区
        /// </summary>
        public VTDocument MainDocument { get { return this.mainDocument; } }

        /// <summary>
        /// UI线程上下文对象
        /// </summary>
        public SynchronizationContext UISyncContext { get { return uiSyncContext; } }

        public VTLogger Logger { get; set; }

        /// <summary>
        /// 电脑按键和发送的数据的映射关系
        /// </summary>
        public VTKeyboard Keyboard { get { return keyboard; } }

        /// <summary>
        /// 当前显示的是否是备用缓冲区
        /// </summary>
        public bool IsAlternate { get { return this.activeDocument == this.alternateDocument; } }

        #endregion

        #region 构造方法

        public VideoTerminal()
        {
        }

        #endregion

        #region 公开接口

        /// <summary>
        /// 初始化终端模拟器
        /// </summary>
        /// <param name="sessionInfo"></param>
        public void Initialize(VTOptions options)
        {
            // 开启日志记录
            //VTDebug.Context.Categories.FirstOrDefault(v => v.Category == VTDebugCategoryEnum.Interactive).Enabled = true;

            vtOptions = options;

            uiSyncContext = SynchronizationContext.Current;

            // DECAWM
            autoWrapMode = false;

            // 初始化变量

            isRunning = true;

            sessionTransport = options.SessionTransport;

            XTermSession sessionInfo = options.Session;

            Name = sessionInfo.Name;

            writeEncoding = Encoding.GetEncoding(sessionInfo.GetOption<string>(OptionKeyEnum.WRITE_ENCODING));
            scrollDelta = sessionInfo.GetOption<int>(OptionKeyEnum.MOUSE_SCROLL_DELTA);
            colorTable = sessionInfo.GetOption<VTColorTable>(OptionKeyEnum.TEHEM_COLOR_TABLE);
            foregroundColor = sessionInfo.GetOption<string>(OptionKeyEnum.THEME_FONT_COLOR);
            backgroundColor = sessionInfo.GetOption<string>(OptionKeyEnum.THEME_BACKGROUND_COLOR);

            #region 初始化键盘

            keyboard = new VTKeyboard();
            keyboard.Encoding = writeEncoding;
            keyboard.SetAnsiMode(true);
            keyboard.SetKeypadMode(false);

            #endregion

            #region 初始化终端解析器

            vtParser = new VTParser();
            vtParser.ColorTable = colorTable;
            vtParser.ActionEvent += VtParser_ActionEvent;
            vtParser.Initialize();

            #endregion

            #region 初始化文档模型

            VTDocumentOptions mainOptions = this.CreateDocumentOptions("MainDocument", sessionInfo, options.MainDocument);
            this.mainDocument = new VTDocument(mainOptions);
            this.mainDocument.Initialize();

            VTDocumentOptions alternateOptions = this.CreateDocumentOptions("AlternateDocument", sessionInfo, options.AlternateDocument);
            alternateOptions.ScrollbackMax = 0;
            this.alternateDocument = new VTDocument(alternateOptions);
            this.alternateDocument.Initialize();
            this.alternateDocument.SetVisible(false);

            this.activeDocument = this.mainDocument;

            // 初始化完VTDocument之后，真正要使用的Column和Row已经被计算出来并保存到了VTDocumentOptions里
            // 此时重新设置sessionInfo里的Row和Column，因为SessionTransport要使用
            sessionInfo.SetOption<int>(OptionKeyEnum.SSH_TERM_ROW, mainOptions.ViewportRow);
            sessionInfo.SetOption<int>(OptionKeyEnum.SSH_TERM_COL, mainOptions.ViewportColumn);

            this.ViewportChanged?.Invoke(this, this.mainDocument.ViewportRow, this.mainDocument.ViewportColumn);

            #endregion

            #region 初始化背景

            // 此时Inser(0)在Z顺序的最下面一层
            //this.backgroundCanvas = this.drawingTerminal.CreateCanvas(0);
            //this.backgroundCanvas.Name = "BackgroundDocument";
            //this.backgroundCanvas.ScrollbarVisible = false;

            //this.wallpaper = new VTWallpaper(this.backgroundCanvas)
            //{
            //    PaperType = sessionInfo.GetOption<WallpaperTypeEnum>(OptionKeyEnum.SSH_THEME_BACKGROUND_TYPE),
            //    Uri = sessionInfo.GetOption<string>(OptionKeyEnum.SSH_THEME_BACKGROUND_URI),
            //    BackgroundColor = sessionInfo.GetOption<string>(OptionKeyEnum.SSH_THEME_BACK_COLOR),
            //    Effect = sessionInfo.GetOption<EffectTypeEnum>(OptionKeyEnum.SSH_THEME_BACKGROUND_EFFECT),
            //    Rect = new VTRect(0, 0, this.drawingTerminal.GetSize())
            //};
            //this.wallpaper.Initialize();
            //this.wallpaper.RequestInvalidate();

            #endregion
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Release()
        {
            isRunning = false;

            vtParser.ActionEvent -= VtParser_ActionEvent;
            vtParser.Release();

            this.mainDocument.Release();
            this.alternateDocument.Release();
        }

        /// <summary>
        /// 处理从远程主机收到的数据流
        /// </summary>
        /// <param name="bytes">收到的数据流缓冲区</param>
        /// <param name="size">要处理的数据长度</param>
        public void ProcessData(byte[] bytes, int size)
        {
            VTDebug.Context.WriteRawRead(bytes, size);

            int oldScroll = this.activeDocument.Scrollbar.ScrollValue;

            try
            {
                vtParser.ProcessCharacters(bytes, size);
            }
            catch (Exception ex)
            {
                logger.Error("ProcessCharacters异常", ex);
            }

            uiSyncContext.Send((o) =>
            {
                // 全部数据都处理完了之后，只渲染一次
                this.activeDocument.RequestInvalidate();

                int newScroll = this.activeDocument.Scrollbar.ScrollValue;
                if (newScroll != oldScroll)
                {
                    // 计算ScrollData
                    VTScrollData scrollData = this.GetScrollData(this.activeDocument, oldScroll, newScroll);
                    this.activeDocument.InvokeScrollChanged(scrollData);
                }

            }, null);
        }

        /// <summary>
        /// 选中全部的文本
        /// </summary>
        public void SelectAll()
        {
            if (this.IsAlternate)
            {
                activeDocument.SelectViewport();
            }
            else
            {
                activeDocument.SelectAll();
            }
        }

        /// <summary>
        /// 获取当前使用鼠标选中的段落区域
        /// </summary>
        /// <returns></returns>
        public VTParagraph GetSelectedParagraph()
        {
            VTextSelection selection = this.activeDocument.Selection;
            if (selection.IsEmpty)
            {
                return VTParagraph.Empty;
            }

            return this.CreateParagraph(ParagraphTypeEnum.Selected, ParagraphFormatEnum.PlainText);
        }

        /// <summary>
        /// 创建指定的段落内容
        /// </summary>
        /// <param name="paragraphType">段落类型</param>
        /// <param name="fileType">要创建的内容格式</param>
        /// <returns></returns>
        public VTParagraph CreateParagraph(ParagraphTypeEnum paragraphType, ParagraphFormatEnum fileType)
        {
            // 所有要保存的行存储在这里
            List<VTHistoryLine> historyLines = new List<VTHistoryLine>();
            int startCharacterIndex = 0, endCharacterIndex = 0;

            switch (paragraphType)
            {
                case ParagraphTypeEnum.AllDocument:
                    {
                        if (IsAlternate)
                        {
                            // 备用缓冲区直接保存VTextLine
                            VTextLine current = this.activeDocument.FirstLine;
                            while (current != null)
                            {
                                historyLines.Add(current.History);
                                current = current.NextLine;
                            }
                        }
                        else
                        {
                            historyLines.AddRange(this.activeDocument.History.GetAllHistoryLines());
                        }

                        startCharacterIndex = 0;
                        endCharacterIndex = historyLines.LastOrDefault().Characters.Count - 1;

                        break;
                    }

                case ParagraphTypeEnum.Viewport:
                    {
                        VTextLine current = this.activeDocument.FirstLine;
                        while (current != null)
                        {
                            historyLines.Add(current.History);
                            current = current.NextLine;
                        }

                        startCharacterIndex = 0;
                        endCharacterIndex = historyLines.LastOrDefault().Characters.Count - 1;

                        break;
                    }

                case ParagraphTypeEnum.Selected:
                    {
                        VTextSelection selection = this.activeDocument.Selection;

                        if (selection.IsEmpty)
                        {
                            return VTParagraph.Empty;
                        }

                        int topRow, bottomRow;
                        selection.Normalize(out topRow, out bottomRow, out startCharacterIndex, out endCharacterIndex);

                        if (IsAlternate)
                        {
                            // 备用缓冲区没有滚动内容，只能选中当前显示出来的文档
                            int rows = bottomRow - topRow + 1;
                            VTextLine firstLine = this.activeDocument.FindLine(topRow);
                            while (rows >= 0)
                            {
                                historyLines.Add(firstLine.History);

                                firstLine = firstLine.NextLine;

                                rows--;
                            }
                        }
                        else
                        {
                            IEnumerable<VTHistoryLine> histories;
                            if (!this.activeDocument.History.TryGetHistories(topRow, bottomRow, out histories))
                            {
                                logger.ErrorFormat("SaveSelected失败, 有的历史记录为空");
                                return VTParagraph.Empty;
                            }
                            historyLines.AddRange(histories);
                        }
                        break;
                    }

                default:
                    throw new NotImplementedException();
            }

            CreateContentParameter parameter = new CreateContentParameter()
            {
                SessionName = String.Empty,
                HistoryLines = historyLines,
                StartCharacterIndex = startCharacterIndex,
                EndCharacterIndex = endCharacterIndex,
                ContentType = fileType,
                Typeface = this.activeDocument.Typeface
            };

            string text = VTUtils.CreateContent(parameter);

            return new VTParagraph()
            {
                Content = text,
                CreationTime = DateTime.Now,
                StartCharacterIndex = startCharacterIndex,
                EndCharacterIndex = endCharacterIndex,
                CharacterList = historyLines,
                IsAlternate = IsAlternate
            };
        }

        /// <summary>
        /// 滚动并重新渲染
        /// </summary>
        /// <param name="physicsRow">要滚动到的物理行数</param>
        public void ScrollTo(int physicsRow, ScrollOptions options = ScrollOptions.ScrollToTop)
        {
            if (this.IsAlternate)
            {
                // 备用缓冲区不可以滚动
                return;
            }

            activeDocument.ScrollTo(physicsRow, options);
            activeDocument.RequestInvalidate();
        }

        public void OnSizeChanged(VTSize oldSize, VTSize newSize)
        {
            if (this.sessionTransport.Status != SessionStatusEnum.Connected)
            {
                return;
            }

            VTDocument document = this.activeDocument;

            int oldRow = document.ViewportRow, oldCol = document.ViewportColumn;
            int newRow = 0, newCol = 0;
            VTUtils.CalculateAutoFitSize(newSize, document.Typeface, out newRow, out newCol);

            if (newRow == document.ViewportRow && newCol == document.ViewportColumn)
            {
                // 变化之后的行和列和现在的行和列一样，什么都不做
                return;
            }

            // 对Document执行Resize
            // 目前的实现在ubuntu下没问题，但是在Windows10操作系统上运行Windows命令行里的vim程序会有问题，可能是Windows下的vim程序兼容性导致的，暂时先这样
            // 遇到过一种情况：如果终端名称不正确，比如XTerm，那么当行数增加的时候，光标会移动到该行的最右边，终端名称改成xterm就没问题了
            // 目前的实现思路是：如果是减少行，那么从第一行开始删除；如果是增加行，那么从最后一行开始新建行。不考虑ScrollMargin

            this.MainDocumentResize(this.mainDocument, oldRow, newRow, oldCol, newCol);
            this.AlternateDocumentResize(this.alternateDocument, oldRow, newRow, oldCol, newCol);

            // 重绘当前显示的文档
            this.activeDocument.RequestInvalidate();

            // 给HOST发送修改行列的请求
            this.sessionTransport.Resize(newRow, newCol);

            if (this.ViewportChanged != null)
            {
                this.ViewportChanged(this, newRow, newCol);
            }
        }

        #endregion

        #region 实例方法

        private void PerformDeviceStatusReport(StatusType statusType)
        {
            switch (statusType)
            {
                case StatusType.OS_OperatingStatus:
                    {
                        // Result ("OK") is CSI 0 n
                        VTDebug.Context.WriteInteractive(VTActions.DSR_DeviceStatusReport, "{0}", statusType);
                        VTDebug.Context.WriteInteractive(VTSendTypeEnum.DSR_DeviceStatusReport, statusType, OS_OperatingStatusData);
                        sessionTransport.Write(OS_OperatingStatusData);
                        break;
                    }

                case StatusType.CPR_CursorPositionReport:
                    {
                        // 打开VIM后会收到这个请求

                        // 1,1 is the top - left corner of the viewport in VT - speak, so add 1
                        // Result is CSI ? r ; c R
                        int cursorRow = CursorRow + 1;
                        int cursorCol = CursorCol + 1;
                        VTDebug.Context.WriteInteractive(VTActions.DSR_DeviceStatusReport, "{0},{1},{2}", statusType, CursorRow, CursorCol);
                        string cprData = string.Format("\x1b[{0};{1}R", cursorRow, cursorCol);
                        byte[] cprBytes = Encoding.ASCII.GetBytes(cprData);
                        VTDebug.Context.WriteInteractive(VTSendTypeEnum.DSR_DeviceStatusReport, statusType, cprBytes);
                        sessionTransport.Write(cprBytes);
                        break;
                    }

                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// 自动判断ch是多字节字符还是单字节字符，创建一个VTCharacter
        /// </summary>
        /// <param name="ch">多字节或者单字节的字符</param>
        /// <returns></returns>
        private VTCharacter CreateCharacter(object ch, VTextAttributeState attributeState)
        {
            if (ch is char)
            {
                // 说明是unicode字符
                // 如果无法确定unicode字符的类别，那么占1列，否则占2列
                int column = 2;
                char c = Convert.ToChar(ch);
                if (c >= 0x2500 && c <= 0x259F ||     //  Box Drawing, Block Elements
                    c >= 0x2000 && c <= 0x206F)       // Unicode - General Punctuation
                {
                    // https://symbl.cc/en/unicode/blocks/box-drawing/
                    // https://unicodeplus.com/U+2500#:~:text=The%20unicode%20character%20U%2B2500%20%28%E2%94%80%29%20is%20named%20%22Box,Horizontal%22%20and%20belongs%20to%20the%20Box%20Drawing%20block.
                    // gotop命令会用到Box Drawing字符，BoxDrawing字符不能用宋体，不然渲染的时候界面会乱掉。BoxDrawing字符的范围是0x2500 - 0x257F
                    // 经过测试发现WindowsTerminal如果用宋体渲染top的话，界面也是乱的...

                    column = 1;
                }

                return VTCharacter.Create(c, column, attributeState);
            }
            else
            {
                // 说明是ASCII码可见字符
                return VTCharacter.Create(Convert.ToChar(ch), 1, attributeState);
            }
        }

        private VTDocumentOptions CreateDocumentOptions(string name, XTermSession sessionInfo, IDrawingDocument drawingDocument)
        {
            string fontFamily = sessionInfo.GetOption<string>(OptionKeyEnum.THEME_FONT_FAMILY);
            double fontSize = sessionInfo.GetOption<double>(OptionKeyEnum.THEME_FONT_SIZE);

            VTypeface typeface = drawingDocument.GetTypeface(fontSize, fontFamily);
            typeface.BackgroundColor = sessionInfo.GetOption<string>(OptionKeyEnum.THEME_BACKGROUND_COLOR);
            typeface.ForegroundColor = sessionInfo.GetOption<string>(OptionKeyEnum.THEME_FONT_COLOR);

            VTSize terminalSize = drawingDocument.Size;
            double contentMargin = sessionInfo.GetOption<double>(OptionKeyEnum.SSH_THEME_CONTENT_MARGIN);
            VTSize contentSize = new VTSize(terminalSize.Width - 15, terminalSize.Height).Offset(-contentMargin * 2);
            TerminalSizeModeEnum sizeMode = sessionInfo.GetOption<TerminalSizeModeEnum>(OptionKeyEnum.SSH_TERM_SIZE_MODE);

            int viewportRow = sessionInfo.GetOption<int>(OptionKeyEnum.SSH_TERM_ROW);
            int viewportColumn = sessionInfo.GetOption<int>(OptionKeyEnum.SSH_TERM_COL);
            if (sizeMode == TerminalSizeModeEnum.AutoFit)
            {
                /// 如果SizeMode等于Fixed，那么就使用DefaultViewportRow和DefaultViewportColumn
                /// 如果SizeMode等于AutoFit，那么动态计算行和列
                VTUtils.CalculateAutoFitSize(contentSize, typeface, out viewportRow, out viewportColumn);
            }

            VTDocumentOptions documentOptions = new VTDocumentOptions()
            {
                Name = name,
                ViewportRow = viewportRow,
                ViewportColumn = viewportColumn,
                AutoWrapMode = false,
                CursorStyle = sessionInfo.GetOption<VTCursorStyles>(OptionKeyEnum.THEME_CURSOR_STYLE),
                CursorColor = sessionInfo.GetOption<string>(OptionKeyEnum.THEME_CURSOR_COLOR),
                CursorSpeed = sessionInfo.GetOption<VTCursorSpeeds>(OptionKeyEnum.THEME_CURSOR_SPEED),
                ScrollDelta = sessionInfo.GetOption<int>(OptionKeyEnum.MOUSE_SCROLL_DELTA),
                ScrollbackMax = sessionInfo.GetOption<int>(OptionKeyEnum.TERM_MAX_SCROLLBACK),
                Typeface = typeface,
                DrawingObject = drawingDocument,
                SelectionColor = sessionInfo.GetOption<string>(OptionKeyEnum.THEME_SELECTION_COLOR),
            };

            return documentOptions;
        }

        /// <summary>
        /// 换行
        /// 换行逻辑是把第一行拿到最后一行，要考虑到scrollMargin
        /// </summary>
        /// <param name="document">要执行换行的文档</param>
        private void LineFeed(VTDocument document)
        {
            // 可滚动区域的第一行和最后一行
            VTextLine head = document.FirstLine.FindNext(document.ScrollMarginTop);
            VTextLine last = document.LastLine.FindPrevious(document.ScrollMarginBottom);

            // 光标所在行是可滚动区域的最后一行
            // 也表示即将滚动
            if (last == ActiveLine)
            {
                // 光标在滚动区域的最后一行，那么把滚动区域的第一行拿到滚动区域最后一行的下面
                logger.DebugFormat("LineFeed，光标在可滚动区域最后一行，向下滚动一行");

                document.SwapLine(head, last);

                if (this.IsAlternate)
                {
                    // 备用缓冲区重置被移动的行
                    head.DeleteAll();
                }
                else
                {
                    // 换行之后记录历史行
                    // 注意用户可以输入Backspace键或者上下左右光标键来修改最新行的内容，所以最新一行的内容是实时变化的，目前的解决方案是在渲染整个文档的时候去更新最后一个历史行的数据
                    // MainScrrenBuffer和AlternateScrrenBuffer里的行分别记录
                    // AlternateScreenBuffer是用来给man，vim等程序使用的
                    // 暂时只记录主缓冲区里的数据，备用缓冲区需要考虑下是否需要记录和怎么记录，因为VIM，Man等程序用的是备用缓冲区，用户是可以实时编辑缓冲区里的数据的

                    // 主缓冲区要创建一个新的VTHistoryLine
                    VTHistoryLine historyLine = new VTHistoryLine();
                    head.SetHistory(historyLine);
                    document.History.AddHistory(historyLine);

                    #region 更新滚动条的值

                    // 滚动条滚动到底
                    // 计算滚动条可以滚动的最大值

                    int scrollMax = document.History.Lines - document.ViewportRow;
                    if (scrollMax > 0)
                    {
                        this.ScrollInfo.ScrollMax = Math.Min(scrollMax, document.ScrollMax);
                        this.ScrollInfo.ScrollValue = this.ScrollInfo.ScrollMax;
                    }

                    #endregion

                    // 触发行被完全打印的事件
                    LinePrinted?.Invoke(this, last.History);
                }
            }
            else
            {
                // 光标不在可滚动区域的最后一行，说明可以直接移动光标
                logger.DebugFormat("LineFeed，光标在滚动区域内，直接移动光标到下一行");
                document.SetCursor(Cursor.Row + 1, Cursor.Column);
            }
        }

        /// <summary>
        /// 反向换行
        /// 反向换行不增加新行，也不减少新行，保持总行数不变
        /// </summary>
        /// <param name="document"></param>
        private void ReverseLineFeed(VTDocument document)
        {
            // 可滚动区域的第一行和最后一行
            VTextLine head = document.FirstLine.FindNext(document.ScrollMarginTop);
            VTextLine last = document.LastLine.FindPrevious(document.ScrollMarginBottom);

            if (head == ActiveLine)
            {
                // 此时光标位置在可视区域的第一行
                logger.DebugFormat("RI_ReverseLineFeed，光标在可视区域第一行，向上移动一行并且可视区域往上移动一行");

                // 上移之后，删除整行数据，终端会重新打印该行数据的
                // 如果不删除的话，在man程序下有可能会显示重叠的信息
                // 复现步骤：man cc -> enter10次 -> help -> enter10次 -> q -> 一直按上键
                document.SwapLineReverse(last, head);

                last.DeleteAll();

                if (this.IsAlternate)
                {
                }
                else
                {
                    // 物理行号和scrollMargin无关，保持物理行号不变
                }
            }
            else
            {
                // 这里假设光标在可视区域里面
                // 实际上有可能光标在可视区域上的上面或者下面，但是目前还没找到判断方式

                // 光标位置在可视区域里面
                logger.DebugFormat("RI_ReverseLineFeed，光标在可视区域里，直接移动光标到上一行");
                document.SetCursor(Cursor.Row - 1, Cursor.Column);
            }
        }

        /// <summary>
        /// 重置终端大小
        /// 模仿Xshell的做法：
        /// 1. 扩大行的时候，如果有滚动内容，那么显示滚动内容。如果没有滚动内容，则直接在后面扩大。
        /// 2. 减少行的时候，如果有滚动内容，那么减少滚动内容。
        /// 3. ActiveLine保持不变
        /// 调用这个函数的时候保证此时文档已经滚动到底
        /// 是否需要考虑scrollMargin?目前没考虑
        /// </summary>
        /// <param name="document"></param>
        /// <param name="oldRow"></param>
        /// <param name="newRow"></param>
        /// <param name="oldCol"></param>
        /// <param name="newCol"></param>
        private void MainDocumentResize(VTDocument document, int oldRow, int newRow, int oldCol, int newCol)
        {
            // 调整大小前前先滚动到底，不然会有问题
            this.activeDocument.ScrollToBottom();

            document.Resize(oldRow, newRow, oldCol, newCol);
        }

        private void AlternateDocumentResize(VTDocument document, int oldRow, int newRow, int oldCol, int newCol)
        {
            document.Resize(oldRow, newRow, oldCol, newCol);

            // 备用缓冲区，因为SSH主机会重新打印所有字符，所以清空所有文本
            document.EraseAll();
        }

        /// <summary>
        /// 根据滚动前的值和滚动后的值计算滚动数据
        /// </summary>
        /// <param name="document"></param>
        /// <param name="oldScroll"></param>
        /// <param name="newScroll"></param>
        /// <returns></returns>
        private VTScrollData GetScrollData(VTDocument document, int oldScroll, int newScroll)
        {
            int scrolledRows = Math.Abs(newScroll - oldScroll);

            int scrollValue = newScroll;
            int viewportRow = document.ViewportRow;
            VTHistory history = document.History;
            VTScrollInfo scrollbar = document.Scrollbar;

            List<VTHistoryLine> removedLines = new List<VTHistoryLine>();
            List<VTHistoryLine> addedLines = new List<VTHistoryLine>();

            if (scrolledRows >= viewportRow)
            {
                // 此时说明把所有行都滚动到屏幕外了

                // 遍历显示
                VTextLine current = document.FirstLine;
                for (int i = 0; i < viewportRow; i++)
                {
                    addedLines.Add(current.History);
                }

                // 我打赌不会报异常
                IEnumerable<VTHistoryLine> historyLines;
                history.TryGetHistories(oldScroll, oldScroll + viewportRow, out historyLines);
                removedLines.AddRange(historyLines);
            }
            else
            {
                // 此时说明有部分行被移动出去了
                if (newScroll > oldScroll)
                {
                    // 往下滚动
                    IEnumerable<VTHistoryLine> historyLines;
                    history.TryGetHistories(oldScroll, oldScroll + scrolledRows, out historyLines);
                    removedLines.AddRange(historyLines);

                    history.TryGetHistories(oldScroll + viewportRow, oldScroll + viewportRow + scrolledRows - 1, out historyLines);
                    addedLines.AddRange(historyLines);
                }
                else
                {
                    // 往上滚动,2
                    IEnumerable<VTHistoryLine> historyLines;
                    history.TryGetHistories(oldScroll + viewportRow - scrolledRows, oldScroll + viewportRow - 1, out historyLines);
                    removedLines.AddRange(historyLines);

                    history.TryGetHistories(newScroll, newScroll + scrolledRows, out historyLines);
                    addedLines.AddRange(historyLines);
                }
            }

            return new VTScrollData()
            {
                NewScroll = newScroll,
                OldScroll = oldScroll,
                AddedLines = addedLines,
                RemovedLines = removedLines
            };
        }

        #endregion

        #region 事件处理器

        private void VtParser_ActionEvent(VTParser parser, VTActions action, object parameter)
        {
            switch (action)
            {
                case VTActions.Print:
                    {
                        // 根据测试得出结论：
                        // 在VIM模式下输入中文字符，VIM会自动把光标往后移动2列，所以判断VIM里一个中文字符占用2列的宽度
                        // 在正常模式下，如果遇到中文字符，也使用2列来显示
                        // 也就是说，如果终端列数一共是80，那么可以显示40个中文字符，显示完40个中文字符后就要换行

                        // 如果在shell里删除一个中文字符，那么会执行两次光标向后移动的动作，然后EraseLine - ToEnd
                        // 由此可得出结论，不论是VIM还是shell，一个中文字符都是按照占用两列的空间来计算的

                        // 用户输入的时候，如果滚动条没滚动到底，那么先把滚动条滚动到底
                        // 不然会出现在VTDocument当前的最后一行打印字符的问题
                        activeDocument.ScrollToBottom();

                        // 创建并打印新的字符
                        char ch = Convert.ToChar(parameter);
                        VTDebug.Context.WriteInteractive(action, "{0},{1},{2}", CursorRow, CursorCol, ch);
                        VTCharacter character = CreateCharacter(parameter, activeDocument.AttributeState);
                        activeDocument.PrintCharacter(character);
                        activeDocument.SetCursor(CursorRow, CursorCol + character.ColumnSize);

                        break;
                    }

                case VTActions.CarriageReturn:
                    {
                        // CR
                        // 把光标移动到行开头
                        VTDebug.Context.WriteInteractive(action, "{0},{1}", CursorRow, CursorCol);
                        activeDocument.SetCursor(CursorRow, 0);
                        break;
                    }

                case VTActions.FF:
                case VTActions.VT:
                case VTActions.LF:
                    {
                        // LF
                        // 滚动边距会影响到LF（DECSTBM_SetScrollingRegion），在实现的时候要考虑到滚动边距

                        VTDebug.Context.WriteInteractive(action, "{0},{1}", CursorRow, CursorCol);

                        // 如果滚动条不在最底部，那么先把滚动条滚动到底
                        activeDocument.ScrollToBottom();

                        // 想像一下有一个打印机往一张纸上打字，当打印机想移动到下一行打字的时候，它会发出一个LineFeed指令，让纸往上移动一行
                        // LineFeed，字面意思就是把纸上的下一行喂给打印机使用
                        this.LineFeed(this.activeDocument);
                        break;
                    }

                case VTActions.RI_ReverseLineFeed:
                    {
                        VTDebug.Context.WriteInteractive(action, string.Empty);

                        // 和LineFeed相反，也就是把光标往上移一个位置
                        // 在用man命令的时候往上滚动会触发这个指令
                        // 反向换行 – 执行\n的反向操作，将光标向上移动一行，维护水平位置，如有必要，滚动缓冲区 *
                        this.ReverseLineFeed(this.ActiveDocument);
                        break;
                    }

                case VTActions.PlayBell:
                    {
                        // 响铃
                        break;
                    }

                case VTActions.ForwardTab:
                    {
                        // 执行TAB键的动作（在当前光标位置处打印4个空格）
                        // 微软的terminal项目里说，如果光标在该行的最右边，那么再次执行TAB的时候光标会自动移动到下一行，目前先不这么做

                        VTDebug.Context.WriteInteractive(action, string.Empty);

                        int tabSize = 4;
                        activeDocument.SetCursor(CursorRow, CursorCol + tabSize);

                        break;
                    }

                #region Erase

                case VTActions.EL_EraseLine:
                    {
                        List<int> parameters = parameter as List<int>;
                        XTerminal.Parser.EraseType eraseType = (XTerminal.Parser.EraseType)VTParameter.GetParameter(parameters, 0, 0);
                        VTDebug.Context.WriteInteractive(action, "{0},{1},{2}", CursorRow, CursorCol, eraseType);
                        activeDocument.EraseLine(CursorCol, (Document.Enumerations.EraseType)eraseType);
                        break;
                    }

                case VTActions.ED_EraseDisplay:
                    {
                        List<int> parameters = parameter as List<int>;
                        XTerminal.Parser.EraseType eraseType = (XTerminal.Parser.EraseType)VTParameter.GetParameter(parameters, 0, 0);
                        VTDebug.Context.WriteInteractive(action, "{0},{1},{2}", CursorRow, CursorCol, eraseType);

                        switch (eraseType)
                        {
                            // In most terminals, this is done by moving the viewport into the scrollback, clearing out the current screen.
                            // top和clear指令会执行EraseType.All，在其他的终端软件里top指令不会清空已经存在的行，而是把已经存在的行往上移动
                            // 所以EraseType.All的动作和Scrollback一样执行
                            case XTerminal.Parser.EraseType.All:
                                {
                                    VTextLine next = activeDocument.FirstLine;
                                    while (next != null)
                                    {
                                        next.EraseAll();

                                        next = next.NextLine;
                                    }

                                    break;
                                }

                            case XTerminal.Parser.EraseType.Scrollback:
                                {
                                    if (this.IsAlternate)
                                    {
                                        // xterm-256color类型的终端VIM程序的PageDown和PageUp会执行EraseType.All
                                        // 备用缓冲区是没有滚动数据的，只能清空当前显示的所有内容
                                        // 所以这里把备用缓冲区和主缓冲区分开处理
                                        VTextLine next = activeDocument.FirstLine;
                                        while (next != null)
                                        {
                                            next.EraseAll();

                                            next = next.NextLine;
                                        }
                                    }
                                    else
                                    {
                                        // 把当前鼠标所在行之前的所有行移动到可视区域外，注意当前行不移动

                                        // 相关命令：
                                        // MainDocument：xterm-256color类型的终端clear程序
                                        // AlternateDocument：暂无

                                        // Erase Saved Lines
                                        // 模拟xshell的操作，把当前行（光标所在行，就是最后一行）移动到可视区域的第一行

                                        // 一共要移动这么多行
                                        int rows = this.activeDocument.Cursor.Row;

                                        for (int i = 0; i < rows; i++)
                                        {
                                            VTextLine firstLine = this.activeDocument.FirstLine;
                                            VTextLine lastLine = this.activeDocument.LastLine;

                                            this.activeDocument.SwapLine(firstLine, lastLine);

                                            VTHistoryLine historyLine = new VTHistoryLine();
                                            firstLine.SetHistory(historyLine);
                                            this.activeDocument.History.AddHistory(historyLine);
                                        }

                                        #region 更新滚动条的值

                                        // 滚动条滚动到底
                                        // 计算滚动条可以滚动的最大值

                                        int scrollMax = this.activeDocument.History.Lines - this.activeDocument.ViewportRow;
                                        if (scrollMax > 0)
                                        {
                                            this.ScrollInfo.ScrollMax = Math.Min(scrollMax, this.activeDocument.ScrollMax);
                                            this.ScrollInfo.ScrollValue = this.ScrollInfo.ScrollMax;
                                        }

                                        #endregion

                                        //this.activeDocument.SetCursor(0, this.Cursor.Column);
                                    }
                                    break;
                                }

                            default:
                                {
                                    activeDocument.EraseDisplay(CursorCol, (Document.Enumerations.EraseType)eraseType);
                                    break;
                                }
                        }

                        break;
                    }

                #endregion

                #region 光标操作

                // 下面的光标移动指令不能进行VTDocument的滚动
                // 光标的移动坐标是相对于可视区域内的坐标
                // 服务器发送过来的光标原点是从(1,1)开始的，我们程序里的是(0,0)开始的，所以要减1

                case VTActions.BS:
                    {
                        VTDebug.Context.WriteInteractive(action, "{0},{1}", CursorRow, CursorCol);
                        activeDocument.SetCursor(CursorRow, CursorCol - 1);
                        break;
                    }

                case VTActions.CursorBackward:
                    {
                        List<int> parameters = parameter as List<int>;
                        int n = VTParameter.GetParameter(parameters, 0, 1);
                        VTDebug.Context.WriteInteractive(action, "{0},{1},{2}", CursorRow, CursorCol, n);
                        activeDocument.SetCursor(CursorRow, CursorCol - n);
                        break;
                    }

                case VTActions.CUF_CursorForward:
                    {
                        List<int> parameters = parameter as List<int>;
                        int n = VTParameter.GetParameter(parameters, 0, 1);
                        VTDebug.Context.WriteInteractive(action, "{0},{1},{2}", CursorRow, CursorCol, n);
                        activeDocument.SetCursor(CursorRow, CursorCol + n);
                        break;
                    }

                case VTActions.CUU_CursorUp:
                    {
                        List<int> parameters = parameter as List<int>;
                        int n = VTParameter.GetParameter(parameters, 0, 1);
                        VTDebug.Context.WriteInteractive(action, "{0},{1},{2}", CursorRow, CursorCol, n);
                        activeDocument.SetCursor(CursorRow - n, CursorCol);
                        break;
                    }

                case VTActions.CUD_CursorDown:
                    {
                        List<int> parameters = parameter as List<int>;
                        int n = VTParameter.GetParameter(parameters, 0, 1);
                        VTDebug.Context.WriteInteractive(action, "{0},{1},{2}", CursorRow, CursorCol, n);
                        activeDocument.SetCursor(CursorRow + n, CursorCol);
                        break;
                    }

                case VTActions.VPA_VerticalLinePositionAbsolute:
                    {
                        // 绝对垂直行位置 光标在当前列中垂直移动到第 <n> 个位置
                        // 保持列不变，把光标移动到指定的行处
                        List<int> parameters = parameter as List<int>;
                        int row = VTParameter.GetParameter(parameters, 0, 1);
                        row = Math.Max(0, row - 1);
                        VTDebug.Context.WriteInteractive(action, "{0},{1},{2}", CursorRow, CursorCol, row);
                        activeDocument.SetCursor(row, CursorCol);
                        break;
                    }

                case VTActions.HVP_HorizontalVerticalPosition:
                case VTActions.CUP_CursorPosition:
                    {
                        List<int> parameters = parameter as List<int>;

                        int row = 0, col = 0;
                        if (parameters.Count == 2)
                        {
                            // VT的光标原点是(1,1)，我们程序里的是(0,0)，所以要减1
                            int newrow = parameters[0];
                            int newcol = parameters[1];

                            // 测试中发现在ubuntu系统上执行apt install或者apt remove命令，HVP会发送0列过来，这里处理一下，如果遇到参数是0，那么就直接变成0
                            row = newrow == 0 ? 0 : newrow - 1;
                            col = newcol == 0 ? 0 : newcol - 1;

                            int viewportRow = this.activeDocument.ViewportRow;
                            int viewportColumn = this.activeDocument.ViewportColumn;

                            // 对行和列做限制
                            if (row >= viewportRow)
                            {
                                row = viewportRow - 1;
                            }

                            if (col >= viewportColumn)
                            {
                                col = viewportColumn - 1;
                            }
                        }
                        else
                        {
                            // 如果没有参数，那么说明就是定位到原点(0,0)
                        }

                        // 打开vim，输入i，然后按tab，虽然第一行的字符列数小于要移动到的col，但是vim还是会移动，所以这里把不足的列数使用空格补齐
                        if (this.ActiveLine.Columns < col)
                        {
                            this.ActiveLine.PadColumns(col);
                        }

                        VTDebug.Context.WriteInteractive(action, "{0},{1},{2},{3},{4}", CursorRow, CursorCol, row, col, this.ActiveLine.Characters.Count);
                        activeDocument.SetCursor(row, col);
                        break;
                    }

                case VTActions.CHA_CursorHorizontalAbsolute:
                    {
                        List<int> parameters = parameter as List<int>;

                        // 将光标移动到当前行中的第n列
                        int n = VTParameter.GetParameter(parameters, 0, -1);
                        if (n == -1)
                        {
                            break;
                        }

                        VTDebug.Context.WriteInteractive(action, "{0},{1},{2}", CursorRow, CursorCol, n);

                        ActiveLine.PadColumns(n - 1);
                        activeDocument.SetCursor(CursorRow, n - 1);
                        break;
                    }

                case VTActions.DECSC_CursorSave:
                    {
                        VTDebug.Context.WriteInteractive(action, "{0},{1}", CursorRow, CursorCol);

                        // 收到这个指令的时候把光标状态保存一下，等下次收到DECRC_CursorRestore再还原保存了的光标状态
                        activeDocument.CursorSave();
                        break;
                    }

                case VTActions.DECRC_CursorRestore:
                    {
                        VTDebug.Context.WriteInteractive(action, "{0},{1},{2},{3}", CursorRow, CursorCol, activeDocument.CursorState.Row, activeDocument.CursorState.Column);
                        activeDocument.CursorRestore();
                        break;
                    }

                #endregion

                #region 文本特效

                case VTActions.UnsetAll:
                    {
                        VTDebug.Context.WriteInteractive(action, string.Empty);

                        // 重置所有文本装饰
                        activeDocument.ClearAttribute();
                        break;
                    }

                case VTActions.Bold:
                case VTActions.BoldUnset:
                case VTActions.Underline:
                case VTActions.UnderlineUnset:
                case VTActions.Italics:
                case VTActions.ItalicsUnset:
                case VTActions.DoublyUnderlined:
                case VTActions.DoublyUnderlinedUnset:
                case VTActions.Foreground:
                case VTActions.ForegroundUnset:
                case VTActions.Background:
                case VTActions.BackgroundUnset:
                case VTActions.ReverseVideo:
                case VTActions.ReverseVideoUnset:
                    {
                        VTDebug.Context.WriteInteractive(action, "{0},{1},{2}", CursorRow, CursorCol, parameter == null ? string.Empty : parameter.ToString());

                        // 打开VIM的时候，VIM会在打印第一行的~号的时候设置验色，然后把剩余的行全部打印，也就是说设置一次颜色可以对多行都生效
                        // 所以这里要记录下如果当前有文本特效被设置了，那么在行改变的时候也需要设置文本特效
                        // 缓存下来，每次打印字符的时候都要对ActiveLine Apply一下

                        switch (action)
                        {
                            case VTActions.ReverseVideo:
                                {
                                    VTColor foreColor = VTColor.CreateFromRgbKey(backgroundColor);
                                    VTColor backColor = VTColor.CreateFromRgbKey(foregroundColor);
                                    activeDocument.SetAttribute(VTextAttributes.Background, true, backColor);
                                    activeDocument.SetAttribute(VTextAttributes.Foreground, true, foreColor);
                                    break;
                                }

                            case VTActions.ReverseVideoUnset:
                                {
                                    activeDocument.SetAttribute(VTextAttributes.Background, false, null);
                                    activeDocument.SetAttribute(VTextAttributes.Foreground, false, null);
                                    break;
                                }

                            default:
                                {
                                    bool enabled;
                                    VTextAttributes attribute = TermUtils.VTAction2TextAttribute(action, out enabled);
                                    activeDocument.SetAttribute(attribute, enabled, parameter);
                                    break;
                                }
                        }

                        break;
                    }

                case VTActions.Faint:
                case VTActions.FaintUnset:
                case VTActions.CrossedOut:
                case VTActions.CrossedOutUnset:
                    {
                        logger.ErrorFormat(string.Format("未执行的VTAction, {0}", action));
                        break;
                    }

                #endregion

                #region DECPrivateMode

                case VTActions.DECANM_AnsiMode:
                    {
                        bool enable = Convert.ToBoolean(parameter);
                        VTDebug.Context.WriteInteractive(action, "{0}", enable);
                        keyboard.SetAnsiMode(enable);
                        break;
                    }

                case VTActions.DECCKM_CursorKeysMode:
                    {
                        bool enable = Convert.ToBoolean(parameter);
                        VTDebug.Context.WriteInteractive(action, "{0}", enable);
                        keyboard.SetCursorKeyMode(enable);
                        break;
                    }

                case VTActions.DECKPAM_KeypadApplicationMode:
                    {
                        VTDebug.Context.WriteInteractive(action, string.Empty);
                        keyboard.SetKeypadMode(true);
                        break;
                    }

                case VTActions.DECKPNM_KeypadNumericMode:
                    {
                        VTDebug.Context.WriteInteractive(action, string.Empty);
                        keyboard.SetKeypadMode(false);
                        break;
                    }

                case VTActions.DECAWM_AutoWrapMode:
                    {
                        bool enable = Convert.ToBoolean(parameter);
                        VTDebug.Context.WriteInteractive(action, "{0}", enable);
                        autoWrapMode = enable;
                        activeDocument.AutoWrapMode = enable;
                        break;
                    }

                case VTActions.XTERM_BracketedPasteMode:
                    {
                        xtermBracketedPasteMode = Convert.ToBoolean(parameter);
                        VTDebug.Context.WriteInteractive(action, "{0}", xtermBracketedPasteMode);
                        break;
                    }

                case VTActions.ATT610_StartCursorBlink:
                    {
                        bool enable = Convert.ToBoolean(parameter);
                        VTDebug.Context.WriteInteractive(action, "{0}", enable);
                        Cursor.AllowBlink = enable;
                        break;
                    }

                case VTActions.DECTCEM_TextCursorEnableMode:
                    {
                        bool enable = Convert.ToBoolean(parameter);
                        VTDebug.Context.WriteInteractive(action, "{0}", enable);
                        Cursor.IsVisible = enable;
                        break;
                    }

                #endregion

                #region 文本操作

                case VTActions.DCH_DeleteCharacter:
                    {
                        // 从指定位置删除n个字符，删除后的字符串要左对齐，默认删除1个字符
                        List<int> parameters = parameter as List<int>;
                        int count = VTParameter.GetParameter(parameters, 0, 1);
                        VTDebug.Context.WriteInteractive(action, "{0},{1},{2}", CursorRow, CursorCol, count);
                        activeDocument.DeleteCharacter(CursorCol, count);
                        break;
                    }

                case VTActions.ICH_InsertCharacter:
                    {
                        // 相关命令：
                        // MainDocument：sudo apt install pstat，然后回车，最后按方向键上回到历史命令
                        // AlternateDocument：暂无

                        // Insert Ps (Blank) Character(s) (default = 1) (ICH).
                        // 在当前光标处插入N个空白字符,这会将所有现有文本移到右侧。 向右溢出屏幕的文本会被删除
                        // 目前没发现这个操作对终端显示有什么影响，所以暂时不实现
                        List<int> parameters = parameter as List<int>;
                        int count = VTParameter.GetParameter(parameters, 0, 1);
                        activeDocument.InsertCharacters(CursorCol, count);
                        break;
                    }

                case VTActions.ECH_EraseCharacters:
                    {
                        // 从当前光标处用空格填充n个字符
                        // Erase Characters from the current cursor position, by replacing them with a space
                        List<int> parameters = parameter as List<int>;
                        int count = VTParameter.GetParameter(parameters, 0, 1);
                        ActiveLine.EraseRange(CursorCol, count);
                        break;
                    }

                case VTActions.IL_InsertLine:
                    {
                        // 将 <n> 行插入光标位置的缓冲区。 光标所在的行及其下方的行将向下移动。
                        List<int> parameters = parameter as List<int>;
                        int lines = VTParameter.GetParameter(parameters, 0, 1);
                        VTDebug.Context.WriteInteractive(action, "{0}", lines);
                        if (lines > 0)
                        {
                            activeDocument.InsertLines(lines);
                        }
                        break;
                    }

                case VTActions.DL_DeleteLine:
                    {
                        // 从缓冲区中删除<n> 行，从光标所在的行开始。
                        List<int> parameters = parameter as List<int>;
                        int lines = VTParameter.GetParameter(parameters, 0, 1);
                        VTDebug.Context.WriteInteractive(action, "{0}", lines);
                        if (lines > 0)
                        {
                            activeDocument.DeleteLines(lines);
                        }
                        break;
                    }

                #endregion

                #region 上下滚动

                case VTActions.SD_ScrollDown:
                    {
                        // Scroll down Ps lines (default = 1) (SD), VT420.

                        break;
                    }

                case VTActions.SU_ScrollUp:
                    {
                        break;
                    }

                #endregion

                case VTActions.UseAlternateScreenBuffer:
                    {
                        VTDebug.Context.WriteInteractive(action, string.Empty);

                        uiSyncContext.Send(new SendOrPostCallback((o) =>
                        {
                            mainDocument.SetVisible(false);
                            alternateDocument.SetVisible(true);
                        }), null);

                        activeDocument = alternateDocument;

                        DocumentChanged?.Invoke(this, mainDocument, alternateDocument);
                        break;
                    }

                case VTActions.UseMainScreenBuffer:
                    {
                        VTDebug.Context.WriteInteractive(action, string.Empty);

                        uiSyncContext.Send(new SendOrPostCallback((o) =>
                        {
                            mainDocument.SetVisible(true);
                            alternateDocument.SetVisible(false);
                        }), null);

                        alternateDocument.SetScrollMargin(0, 0);
                        alternateDocument.EraseAll();
                        alternateDocument.SetCursor(0, 0);
                        alternateDocument.ClearAttribute();
                        alternateDocument.Selection.Clear();

                        activeDocument = mainDocument;

                        DocumentChanged?.Invoke(this, alternateDocument, mainDocument);
                        break;
                    }

                case VTActions.DSR_DeviceStatusReport:
                    {
                        // DSR，参考https://invisible-island.net/xterm/ctlseqs/ctlseqs.html

                        List<int> parameters = parameter as List<int>;
                        StatusType statusType = (StatusType)Convert.ToInt32(parameters[0]);
                        PerformDeviceStatusReport(statusType);
                        break;
                    }

                case VTActions.DA_DeviceAttributes:
                    {
                        VTDebug.Context.WriteInteractive(action, string.Empty);
                        VTDebug.Context.WriteInteractive(VTSendTypeEnum.DA_DeviceAttributes, DA_DeviceAttributesData);
                        sessionTransport.Write(DA_DeviceAttributesData);
                        break;
                    }

                case VTActions.DECSTBM_SetScrollingRegion:
                    {
                        // 设置可滚动区域
                        // 不可以操作滚动区域以外的行，只能对滚动区域内的行进行操作
                        // 对于滚动区域的作用的解释，举个例子说明
                        // 比方说marginTop是1，marginBottom也是1
                        // 那么在执行LineFeed动作的时候，默认情况下，是把第一行挂到最后一行的后面，有了margin之后，就要把第二行挂到倒数第二行的后面
                        // ScrollMargin会对很多动作产生影响：LF，RI_ReverseLineFeed，DeleteLine，InsertLine

                        // 视频终端的规范里说，如果topMargin等于bottomMargin，或者bottomMargin大于屏幕高度，那么忽略这个指令
                        // 边距还会影响插入行 (IL) 和删除行 (DL)、向上滚动 (SU) 和向下滚动 (SD) 修改的行。

                        // Notes on DECSTBM
                        // * The value of the top margin (Pt) must be less than the bottom margin (Pb).
                        // * The maximum size of the scrolling region is the page size
                        // * DECSTBM moves the cursor to column 1, line 1 of the page
                        // * https://github.com/microsoft/terminal/issues/1849

                        // 当前终端屏幕可显示的行数量
                        int lines = this.activeDocument.ViewportRow;

                        List<int> parameters = parameter as List<int>;
                        int topMargin = VTParameter.GetParameter(parameters, 0, 1);
                        int bottomMargin = VTParameter.GetParameter(parameters, 1, lines);

                        if (bottomMargin < 0 || topMargin < 0)
                        {
                            logger.ErrorFormat("DECSTBM_SetScrollingRegion参数不正确，忽略本次设置, topMargin = {0}, bottomMargin = {1}", topMargin, bottomMargin);
                            return;
                        }
                        if (topMargin >= bottomMargin)
                        {
                            logger.ErrorFormat("DECSTBM_SetScrollingRegion参数不正确，topMargin大于等bottomMargin，忽略本次设置, topMargin = {0}, bottomMargin = {1}", topMargin, bottomMargin);
                            return;
                        }
                        if (bottomMargin > lines)
                        {
                            logger.ErrorFormat("DECSTBM_SetScrollingRegion参数不正确，bottomMargin大于当前屏幕总行数, bottomMargin = {0}, lines = {1}", bottomMargin, lines);
                            return;
                        }

                        // 如果topMargin等于1，那么就表示使用默认值，也就是没有marginTop，所以当topMargin == 1的时候，marginTop改为0
                        int marginTop = topMargin == 1 ? 0 : topMargin;
                        // 如果bottomMargin等于控制台高度，那么就表示使用默认值，也就是没有marginBottom，所以当bottomMargin == 控制台高度的时候，marginBottom改为0
                        int marginBottom = lines - bottomMargin;
                        VTDebug.Context.WriteInteractive(action, "topMargin1 = {0}, bottomMargin1 = {1}, topMargin2 = {2}, bottomMargin2 = {3}", topMargin, bottomMargin, marginTop, marginBottom);
                        activeDocument.SetScrollMargin(marginTop, marginBottom);
                        break;
                    }

                case VTActions.DECSLRM_SetLeftRightMargins:
                    {
                        List<int> parameters = parameter as List<int>;
                        int leftMargin = VTParameter.GetParameter(parameters, 0, 0);
                        int rightMargin = VTParameter.GetParameter(parameters, 1, 0);

                        VTDebug.Context.WriteInteractive(action, "leftMargin = {0}, rightMargin = {1}", leftMargin, rightMargin);
                        logger.ErrorFormat("未实现DECSLRM_SetLeftRightMargins");
                        break;
                    }

                default:
                    {
                        throw new NotImplementedException(string.Format("未执行的VTAction, {0}", action));
                    }
            }
        }

        #endregion
    }
}