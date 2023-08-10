﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPFToolkit.MVVM;

namespace XTerminal.ViewModels
{
    public class OptionTreeNodeVM : TreeNodeViewModel
    {
        public string EntryClass { get; set; }

        public OptionTreeNodeVM(TreeViewModelContext context, object data = null) :
            base(context, data)
        {

        }
    }
}
