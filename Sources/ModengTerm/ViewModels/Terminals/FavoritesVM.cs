﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPFToolkit.MVVM;

namespace ModengTerm.Terminal.ViewModels
{
    public class FavoritesVM : ParagraphsVM
    {
        public FavoritesVM(ParagraphSource source, ShellSessionVM videoTerminal) : 
            base(source, videoTerminal)
        {
        }
    }
}
