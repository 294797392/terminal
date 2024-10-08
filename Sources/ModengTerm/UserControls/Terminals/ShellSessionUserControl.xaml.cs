﻿using ModengTerm.Base;
using ModengTerm.Base.DataModels;
using ModengTerm.Base.Enumerations;
using ModengTerm.Controls;
using ModengTerm.Document;
using ModengTerm.Document.Rendering;
using ModengTerm.Terminal.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ModengTerm.Terminal.UserControls
{
    /// <summary>
    /// TerminalContentUserControl.xaml 的交互逻辑
    /// </summary>
    public partial class TerminalContentUserControl : UserControl, ISessionContent
    {
        #region 类变量

        private static log4net.ILog logger = log4net.LogManager.GetLogger("TerminalUserControl");

        #endregion

        #region 实例变量

        private ShellSessionVM shellSession;
        private IVideoTerminal videoTerminal;
        private VTKeyInput userInput;

        #endregion

        #region 属性

        public XTermSession Session { get; set; }

        public IVideoTerminal VideoTerminal { get; set; }

        #endregion

        #region 构造方法

        public TerminalContentUserControl()
        {
            InitializeComponent();

            this.InitializeUserControl();
        }

        #endregion

        #region 实例方法

        private void InitializeUserControl()
        {
            // 必须设置Focusable=true，调用Focus才生效
            GridDocument.Focusable = true;
            this.Background = Brushes.Transparent;
            this.userInput = new VTKeyInput();
        }

        /// <summary>
        /// 获取当前正在显示的文档的事件输入器
        /// </summary>
        /// <returns></returns>
        private VTEventInput GetActiveEventInput()
        {
            return this.videoTerminal.ActiveDocument.EventInput;
        }

        private WPFDocument GetActiveDocument()
        {
            return this.videoTerminal.IsAlternate ?
                DocumentAlternate : DocumentMain;
        }

        private MouseData GetMouseData(object sender, MouseButtonEventArgs e)
        {
            WPFDocument document = this.GetActiveDocument();
            Point mousePosition = e.GetPosition(document);
            MouseData mouseData = new MouseData(mousePosition.X, mousePosition.Y, e.ClickCount, (sender as FrameworkElement).IsMouseCaptured);

            return mouseData;
        }

        private MouseData GetMouseData(object sender, MouseEventArgs e)
        {
            WPFDocument document = this.GetActiveDocument();
            WPFDocumentCanvas canvas = document.Content;
            Point mousePosition = e.GetPosition(canvas);
            MouseData mouseData = new MouseData(mousePosition.X, mousePosition.Y, 0, (sender as FrameworkElement).IsMouseCaptured);

            return mouseData;
        }

        private void HandleCaptureAction(object sender, MouseData mouseData)
        {
            switch (mouseData.CaptureAction)
            {
                case MouseData.CaptureActions.None:
                    {
                        break;
                    }

                case MouseData.CaptureActions.Capture:
                    {
                        (sender as FrameworkElement).CaptureMouse();
                        break;
                    }

                case MouseData.CaptureActions.ReleaseCapture:
                    {
                        (sender as FrameworkElement).ReleaseMouseCapture();
                        break;
                    }

                default:
                    throw new NotImplementedException();
            }
        }

        #endregion

        #region 事件处理器

        private void ContentCanvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            MouseData mouseData = this.GetMouseData(sender, e);
            VTEventInput eventInput = this.GetActiveEventInput();
            eventInput.OnMouseMove(mouseData);
            this.HandleCaptureAction(sender, mouseData);
        }

        private void ContentCanvas_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            MouseData mouseData = this.GetMouseData(sender, e);
            VTEventInput eventInput = this.GetActiveEventInput();
            eventInput.OnMouseUp(mouseData);
            this.HandleCaptureAction(sender, mouseData);
        }

        private void ContentCanvas_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MouseData mouseData = this.GetMouseData(sender, e);
            VTEventInput eventInput = this.GetActiveEventInput();
            eventInput.OnMouseDown(mouseData);
            this.HandleCaptureAction(sender, mouseData);
        }

        private void ContentCanvas_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            BehaviorRightClicks brc = this.Session.GetOption<BehaviorRightClicks>(OptionKeyEnum.BEHAVIOR_RIGHT_CLICK);
            if (brc == BehaviorRightClicks.FastCopyPaste)
            {
                VTDocument activeDocument = this.videoTerminal.ActiveDocument;

                if (activeDocument.Selection.IsEmpty)
                {
                    // 粘贴剪贴板里的内容
                    string text = Clipboard.GetText();
                    if (string.IsNullOrEmpty(text))
                    {
                        return;
                    }

                    this.shellSession.SendText(text);
                }
                else
                {
                    this.shellSession.CopySelection();
                    activeDocument.ClearSelection();
                }
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            VTEventInput eventInput = this.GetActiveEventInput();
            eventInput.OnMouseWheel(e.Delta > 0);

            e.Handled = true;
        }

        private void TerminalContentUserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            WPFDocument document = this.GetActiveDocument();

            this.videoTerminal.Resize(document.ContentSize);
        }


        /// <summary>
        /// 重写了这个事件后，就会触发鼠标相关的事件
        /// </summary>
        /// 参考AvalonEdit
        /// <param name="hitTestParameters"></param>
        /// <returns></returns>
        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            return new PointHitTestResult(this, hitTestParameters.HitPoint);
        }

        private void ContextMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = e.OriginalSource as MenuItem;
            ShellContextMenu functionMenu = menuItem.DataContext as ShellContextMenu;
            if (functionMenu == null)
            {
                return;
            }

            if (functionMenu.Execute == null)
            {
                return;
            }

            functionMenu.Execute();
        }


        private void GridDocument_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Tab:
                case Key.Up:
                case Key.Down:
                case Key.Left:
                case Key.Right:
                    {
                        this.userInput.CapsLock = Console.CapsLock;
                        this.userInput.Key = VTermUtils.ConvertToVTKey(e.Key);
                        this.userInput.Modifiers = (VTModifierKeys)e.KeyboardDevice.Modifiers;
                        this.shellSession.SendInput(this.userInput);

                        // 防止焦点移动到其他控件上
                        e.Handled = true;
                        break;
                    }

                default:
                    {
                        break;
                    }
            }
        }

        private void GridDocument_TextInput(object sender, TextCompositionEventArgs e)
        {
            this.shellSession.SendText(e.Text);
        }

        private void GridDocument_Loaded(object sender, RoutedEventArgs e)
        {
            // Loaded事件里让控件获取焦点，这样才能触发OnKeyDown和OnTextInput事件
            GridDocument.Focus();
        }

        private void GridDocument_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // 获取焦点，才能收到OnKeyDown和OnTextInput回调
            GridDocument.Focus();
        }



        private void ButtonOptions_Checked(object sender, RoutedEventArgs e)
        {
            ButtonOptions.ContextMenu.IsOpen = true;
        }

        private void ButtonSend_Click(object sender, RoutedEventArgs e)
        {
            ShellSessionVM shellSession = this.shellSession;
            if (shellSession == null)
            {
                return;
            }

            string text = ComboBoxHistoryCommands.Text;
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (MenuItemHexInput.IsChecked)
            {
                byte[] bytes;
                if (!MTermUtils.TryParseHexString(text, out bytes))
                {
                    MTMessageBox.Info("请输入正确的十六进制数据");
                    return;
                }

                shellSession.SendRawData(bytes);
            }
            else
            {
                if (MenuItemSendCRLF.IsChecked)
                {
                    text = string.Format("{0}\r\n", text);
                }

                shellSession.SendText(text);
            }

            shellSession.HistoryCommands.Add(text);
        }

        private void ButtonClear_Click(object sender, RoutedEventArgs e)
        {
            ComboBoxHistoryCommands.Text = string.Empty;
        }



        private void VideoTerminal_DocumentChanged(IVideoTerminal arg1, VTDocument oldDocument, VTDocument newDocument)
        {
            newDocument.EventInput.OnLoaded();
        }

        private void VideoTerminal_RequestChangeWindowSize(IVideoTerminal arg1, double deltaX, double deltaY)
        {
            Window window = Window.GetWindow(this);

            window.Width += deltaX;
            window.Height += deltaY;

            logger.InfoFormat("修改窗口大小");
        }

        #endregion

        #region ISessionContent

        public int Open()
        {
            string background = this.Session.GetOption<string>(OptionKeyEnum.THEME_BACKGROUND_COLOR);
            BorderBackground.Background = DrawingUtils.GetBrush(background);

            double margin = this.Session.GetOption<double>(OptionKeyEnum.SSH_THEME_CONTENT_MARGIN);
            DocumentAlternate.ContentMargin = margin;
            DocumentAlternate.Content.PreviewMouseLeftButtonDown += ContentCanvas_PreviewMouseLeftButtonDown;
            DocumentAlternate.Content.PreviewMouseLeftButtonUp += ContentCanvas_PreviewMouseLeftButtonUp;
            DocumentAlternate.Content.PreviewMouseMove += ContentCanvas_PreviewMouseMove;
            DocumentAlternate.Content.PreviewMouseRightButtonDown += ContentCanvas_PreviewMouseRightButtonDown;
            DocumentMain.ContentMargin = margin;
            DocumentMain.Content.PreviewMouseLeftButtonDown += ContentCanvas_PreviewMouseLeftButtonDown;
            DocumentMain.Content.PreviewMouseLeftButtonUp += ContentCanvas_PreviewMouseLeftButtonUp;
            DocumentMain.Content.PreviewMouseMove += ContentCanvas_PreviewMouseMove;
            DocumentMain.Content.PreviewMouseRightButtonDown += ContentCanvas_PreviewMouseRightButtonDown;

            // 设置了ContentMargin，等待界面加载完毕
            // TODO：
            // 设置了ContentMargin之后，不会立即更新ActualWidth和ActualHeight，要等Loaded之后才能获取到
            // 暂时没找到更好的办法去获取到设置了Margin之后的ContentSize
            base.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);

            this.shellSession = this.DataContext as ShellSessionVM;
            this.shellSession.MainDocument = DocumentMain;
            this.shellSession.AlternateDocument = DocumentAlternate;
            this.shellSession.Open();

            this.videoTerminal = this.shellSession.VideoTerminal;
            this.videoTerminal.DocumentChanged += VideoTerminal_DocumentChanged;
            this.videoTerminal.RequestChangeWindowSize += VideoTerminal_RequestChangeWindowSize;
            this.videoTerminal.ActiveDocument.EventInput.OnLoaded();

            this.SizeChanged += TerminalContentUserControl_SizeChanged;

            return ResponseCode.SUCCESS;
        }

        public void Close()
        {
            this.SizeChanged -= TerminalContentUserControl_SizeChanged;

            DocumentAlternate.Content.PreviewMouseLeftButtonDown -= ContentCanvas_PreviewMouseLeftButtonDown;
            DocumentAlternate.Content.PreviewMouseLeftButtonUp -= ContentCanvas_PreviewMouseLeftButtonUp;
            DocumentAlternate.Content.PreviewMouseMove -= ContentCanvas_PreviewMouseMove;
            DocumentMain.Content.PreviewMouseLeftButtonDown -= ContentCanvas_PreviewMouseLeftButtonDown;
            DocumentMain.Content.PreviewMouseLeftButtonUp -= ContentCanvas_PreviewMouseLeftButtonUp;
            DocumentMain.Content.PreviewMouseMove -= ContentCanvas_PreviewMouseMove;

            this.shellSession.Close();

            this.videoTerminal.DocumentChanged -= VideoTerminal_DocumentChanged;
            this.videoTerminal.RequestChangeWindowSize -= VideoTerminal_RequestChangeWindowSize;
            this.videoTerminal = null;
        }

        #endregion
    }
}
