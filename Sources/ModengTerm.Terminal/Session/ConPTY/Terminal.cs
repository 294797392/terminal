using Microsoft.Win32.SafeHandles;
using ModengTerm.Terminal.Session.ConPTY.Processes;
using System;
using System.IO;
using System.Threading;
using static ModengTerm.Terminal.Session.ConPTY.Native.PseudoConsoleApi;
using static ModengTerm.Terminal.Session.ConPTY.Native.ConsoleApi;

namespace ModengTerm.Terminal.Session.ConPTY
{
    /// <summary>
    /// Class for managing communication with the underlying console, and communicating with its pseudoconsole.
    /// </summary>
    public sealed class Terminal
    {
        private const string ExitCommand = "exit\r";
        private const string CtrlC_Command = "\x3";

        private PseudoConsolePipe inputPipe;
        private PseudoConsolePipe outputPipe;
        private PseudoConsole pseudoConsole;
        private Process process;

        /// <summary>
        /// A stream of VT-100-enabled output from the console.
        /// </summary>
        public FileStream ConsoleOutStream { get; private set; }

        public FileStream ConsoleInStream { get; private set; }

        public Terminal()
        {

        }

        /// <summary>
        /// Start the pseudoconsole and run the process as shown in 
        /// https://docs.microsoft.com/en-us/windows/console/creating-a-pseudoconsole-session#creating-the-pseudoconsole
        /// </summary>
        /// <param name="command">the command to run, e.g. cmd.exe</param>
        /// <param name="consoleHeight">The height (in characters) to start the pseudoconsole with. Defaults to 80.</param>
        /// <param name="consoleWidth">The width (in characters) to start the pseudoconsole with. Defaults to 30.</param>
        public void Start(string command, int consoleWidth = 80, int consoleHeight = 30)
        {
            this.inputPipe = new PseudoConsolePipe();
            this.outputPipe = new PseudoConsolePipe();
            this.pseudoConsole = PseudoConsole.Create(inputPipe.ReadSide, outputPipe.WriteSide, consoleWidth, consoleHeight);
            this.process = ProcessFactory.Start(command, PseudoConsole.PseudoConsoleThreadAttribute, pseudoConsole.Handle);

            // copy all pseudoconsole output to a FileStream and expose it to the rest of the app
            this.ConsoleOutStream = new FileStream(outputPipe.ReadSide, FileAccess.Read);
            //OutputReady.Invoke(this, EventArgs.Empty);

            // Store input pipe handle, and a writer for later reuse
            this.ConsoleInStream = new FileStream(inputPipe.WriteSide, FileAccess.Write);

            // free resources in case the console is ungracefully closed (e.g. by the 'x' in the window titlebar)
            //OnClose(() => DisposeResources(process, pseudoConsole, outputPipe, inputPipe, _consoleInputWriter));

            //WaitForExit(process).WaitOne(Timeout.Infinite);
        }

        public void Stop()
        {
            this.process.Dispose();
            this.pseudoConsole.Dispose();
            this.outputPipe.Dispose();
            this.inputPipe.Dispose();
        }

        public void Resize(int row, int col) 
        {
            this.pseudoConsole.Resize(row, col);
        }

        /// <summary>
        /// Set a callback for when the terminal is closed (e.g. via the "X" window decoration button).
        /// Intended for resource cleanup logic.
        /// </summary>
        private static void OnClose(Action handler)
        {
            SetConsoleCtrlHandler(eventType =>
            {
                if (eventType == CtrlTypes.CTRL_CLOSE_EVENT)
                {
                    handler();
                }
                return false;
            }, true);
        }

        private void DisposeResources(params IDisposable[] disposables)
        {
            foreach (var disposable in disposables)
            {
                disposable.Dispose();
            }
        }
    }
}