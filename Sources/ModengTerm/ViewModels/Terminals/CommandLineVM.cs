﻿using ModengTerm.Base;
using ModengTerm.Document;
using ModengTerm.Document.Drawing;
using ModengTerm.Terminal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ModengTerm.ViewModels.Terminals
{
    public class CommandLineVM : InputSessionVM
    {
        private VTDocument document;

        public IDocumentRenderer DrawingDocument { get; set; }

        public CommandLineVM() : 
            base(null)
        { 
        }

        protected override int OnOpen()
        {
            VTDocumentOptions options = new VTDocumentOptions() 
            {
                Controller = this.DrawingDocument
            };

            this.document = new VTDocument(options);
            this.document.Initialize();

            return ResponseCode.SUCCESS;
        }

        protected override void OnClose()
        {
            this.document.Release();
        }

        public override void SendInput(UserInput userInput)
        {
        }
    }
}
