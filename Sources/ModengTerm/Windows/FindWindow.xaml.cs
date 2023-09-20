﻿using ModengTerm.Terminal.Document;
using ModengTerm.Terminal.ViewModels;
using ModengTerm.ViewModels;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using XTerminal.Document;

namespace ModengTerm.Windows
{
    /// <summary>
    /// FindWindow.xaml 的交互逻辑
    /// </summary>
    public partial class FindWindow : Window
    {
        public event Action<VTHistoryLine> FindCompleted;

        private FindWindowVM viewModel;

        public FindWindow(FindWindowVM vm)
        {
            InitializeComponent();

            this.InitializeWindow(vm);
        }

        private void InitializeWindow(FindWindowVM vm)
        {
            this.viewModel = vm;
            base.DataContext = vm;
        }

        private void ButtonFind_Click(object sender, RoutedEventArgs e)
        {
            this.viewModel.Find();
        }

        private void ButtonFindAll_Click(object sender, RoutedEventArgs e)
        {
            this.viewModel.Find();
        }
    }
}
