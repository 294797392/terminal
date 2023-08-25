﻿using Microsoft.Win32;
using ModengTerm.Rendering;
using ModengTerm.VideoTerminal;
using ModengTerm.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using XTerminal.Base;
using XTerminal.Base.DataModels;
using XTerminal.Document;
using XTerminal.Document.Rendering;
using XTerminal.Enumerations;
using XTerminal.Session;
using XTerminal.ViewModels;

namespace XTerminal.UserControls
{
    /// <summary>
    /// TerminalContentUserControl.xaml 的交互逻辑
    /// </summary>
    public partial class TerminalContentUserControl : SessionContent, IVideoTerminal
    {
        #region 类变量

        private static log4net.ILog logger = log4net.LogManager.GetLogger("TerminalUserControl");

        #endregion

        #region 实例变量

        private UserInput userInput;

        private VideoTerminal videoTerminal;

        private MouseDownHandler mouseDownDlg;
        private MouseMoveHandler mouseMoveDlg;
        private MouseUpHandler mouseUpDlg;
        private MouseWheelHandler mouseWheelDlg;
        private SizeChangedHandler sizeChangedDlg;

        #endregion

        #region 属性

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
            this.userInput = new UserInput();
            this.Background = Brushes.Transparent;
            this.Focusable = true;
        }

        private void SaveToFile(SaveModeEnum saveMode, SaveFormatEnum format)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            if ((bool)saveFileDialog.ShowDialog())
            {
                this.videoTerminal.SaveToFile(saveMode, format, saveFileDialog.FileName);
            }
        }

        #endregion

        #region 公开接口

        #endregion

        #region 事件处理器

        /// <summary>
        /// 输入中文的时候会触发该事件
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            base.OnPreviewTextInput(e);

            this.userInput.CapsLock = Console.CapsLock;
            this.userInput.Key = VTKeys.None;
            this.userInput.Text = e.Text;
            this.userInput.Modifiers = VTModifierKeys.None;

            this.videoTerminal.HandleInput(this.userInput);

            e.Handled = true;

            //Console.WriteLine(e.Text);
        }

        /// <summary>
        /// 从键盘上按下按键的时候会触发
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            //Console.WriteLine(e.Key);

            if (e.Key == Key.ImeProcessed)
            {
                // 这些字符交给输入法处理了
            }
            else
            {
                switch (e.Key)
                {
                    case Key.Tab:
                    case Key.Up:
                    case Key.Down:
                    case Key.Left:
                    case Key.Right:
                    case Key.Space:
                        {
                            // 防止焦点移动到其他控件上了
                            e.Handled = true;
                            break;
                        }
                }

                if (e.Key != Key.ImeProcessed)
                {
                    e.Handled = true;
                }

                VTKeys vtKey = DrawingUtils.ConvertToVTKey(e.Key);
                this.userInput.CapsLock = Console.CapsLock;
                this.userInput.Key = vtKey;
                this.userInput.Text = null;
                this.userInput.Modifiers = (VTModifierKeys)e.KeyboardDevice.Modifiers;
                this.videoTerminal.HandleInput(this.userInput);
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            this.Focus();
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            this.mouseWheelDlg(this, e.Delta > 0);
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


        private void SliderScrolbar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.videoTerminal.ScrollTo((int)e.NewValue);
        }



        private void GridContent_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(this);

            GridContent.CaptureMouse();
            this.mouseDownDlg(this, new VTPoint(p.X, p.Y), e.ClickCount);
        }

        private void GridContent_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (GridContent.IsMouseCaptured)
            {
                Point p = e.GetPosition(this);
                this.mouseMoveDlg(this, new VTPoint(p.X, p.Y));
            }
        }

        private void GridContent_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(this);
            this.mouseUpDlg(this, new VTPoint(p.X, p.Y));
            GridContent.ReleaseMouseCapture();
        }



        private void ContentControlSurface_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // 每次大小改变的时候重新计算下渲染区域的边界框
            Point leftTop = ContentControlSurface.PointToScreen(new Point(0, 0));
            VTRect vtRect = new VTRect(leftTop.X, leftTop.Y, ContentControlSurface.ActualWidth, ContentControlSurface.ActualHeight);
            this.sizeChangedDlg(this, vtRect);
        }



        private void MenuItemCopy_Click(object sender, RoutedEventArgs e)
        {
            this.videoTerminal.CopySelection();
        }

        private void MenuItemPaste_Click(object sender, RoutedEventArgs e)
        {
            this.videoTerminal.Paste();
        }

        private void MenuItemSelectAll_Click(object sender, RoutedEventArgs e)
        {
            this.videoTerminal.SelectAll();
        }

        private void MenuItemSaveScreenToTextFile_Click(object sender, RoutedEventArgs e)
        {
            this.SaveToFile(SaveModeEnum.SaveScreen, SaveFormatEnum.TextFormat);
        }

        private void MenuItemSaveScreenToHtmlFile_Click(object sender, RoutedEventArgs e)
        {
            this.SaveToFile(SaveModeEnum.SaveScreen, SaveFormatEnum.HtmlFormat);
        }

        private void MenuItemSaveSelectedToTextFile_Click(object sender, RoutedEventArgs e)
        {
            this.SaveToFile(SaveModeEnum.SaveSelected, SaveFormatEnum.TextFormat);
        }

        private void MenuItemSaveSelectedToHtmlFile_Click(object sender, RoutedEventArgs e)
        {
            this.SaveToFile(SaveModeEnum.SaveSelected, SaveFormatEnum.HtmlFormat);
        }

        private void MenuItemSaveAllToTextFile_Click(object sender, RoutedEventArgs e)
        {
            this.SaveToFile(SaveModeEnum.SaveAll, SaveFormatEnum.TextFormat);
        }

        private void MenuItemSaveAllToHtmlFile_Click(object sender, RoutedEventArgs e)
        {
            this.SaveToFile(SaveModeEnum.SaveAll, SaveFormatEnum.HtmlFormat);
        }

        private void MenuItemDrawingMode_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (MenuItemDrawingMode.IsChecked)
            {
                // 绘图模式
                this.mouseDownDlg = this.videoTerminal.OnDrawingMouseDown;
                this.mouseMoveDlg = this.videoTerminal.OnDrawingMouseMove;
                this.mouseUpDlg = this.videoTerminal.OnDrawingMouseUp;
                this.mouseWheelDlg = this.videoTerminal.OnDrawingMouseWheel;
                this.sizeChangedDlg = this.videoTerminal.OnDrawingSizeChanged;
            }
            else
            {
                // 非绘图模式
                this.mouseDownDlg = this.videoTerminal.OnMouseDown;
                this.mouseMoveDlg = this.videoTerminal.OnMouseMove;
                this.mouseUpDlg = this.videoTerminal.OnMouseUp;
                this.mouseWheelDlg = this.videoTerminal.OnMouseWheel;
                this.sizeChangedDlg = this.videoTerminal.OnSizeChanged;
            }
        }

        #endregion

        #region IVideoTerminal

        public IDrawingCanvas CreateCanvas()
        {
            DrawingSurface canvas = new DrawingSurface();
            return canvas;
        }

        public void AddCanvas(IDrawingCanvas canvas)
        {
            this.Dispatcher.Invoke(() => 
            {
                ContentControlSurface.Children.Add(canvas as UIElement);
            });
        }

        public void RemoveCanvas(IDrawingCanvas canvas)
        {
            base.Dispatcher.Invoke(() => 
            {
                ContentControlSurface.Children.Remove(canvas as UIElement);
            });
        }

        public void GetScrollInfo(out int maximum, out int scrollValue)
        {
            maximum = (int)SliderScrolbar.Maximum;
            scrollValue = (int)SliderScrolbar.Value;
        }

        /// <summary>
        /// 更新滚动信息
        /// </summary>
        /// <param name="maximum">滚动条的最大值</param>
        public void SetScrollInfo(int maximum, int scrollValue)
        {
            if (SliderScrolbar.Maximum != maximum)
            {
                SliderScrolbar.Maximum = maximum;
            }

            if (SliderScrolbar.Value != scrollValue)
            {
                SliderScrolbar.Value = scrollValue;
            }
        }

        public VTextMetrics MeasureText(string text, double fontSize, string fontFamily)
        {
            Typeface typeface = new Typeface(new FontFamily(fontFamily), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            FormattedText formattedText = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface,
                fontSize, Brushes.Black, null, TextFormattingMode.Display, App.PixelsPerDip);

            VTextMetrics metrics = new VTextMetrics()
            {
                Height = formattedText.Height,
                Width = formattedText.WidthIncludingTrailingWhitespace
            };

            return metrics;
        }

        #endregion

        #region SessionContent

        public override int Open(OpenedSessionVM session)
        {
            this.videoTerminal = session as VideoTerminal;

            this.mouseDownDlg = this.videoTerminal.OnMouseDown;
            this.mouseMoveDlg = this.videoTerminal.OnMouseMove;
            this.mouseUpDlg = this.videoTerminal.OnMouseUp;
            this.mouseWheelDlg = this.videoTerminal.OnMouseWheel;
            this.sizeChangedDlg = this.videoTerminal.OnSizeChanged;

            return ResponseCode.SUCCESS;
        }

        public override void Close()
        {
            
        }

        #endregion
    }
}
