using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace SimpleZmq.Native
{
    /// <summary>
    /// Static class to manage libzmq.
    /// </summary>
    internal static partial class LibZmq
    {
        static LibZmq()
        {
            LoadLibZmq();
        }

        private static bool TryLoadLibZmq(string libzmqFilePath)
        {
            return File.Exists(libzmqFilePath) && Win32.LoadLibrary(libzmqFilePath) != IntPtr.Zero;
        }

        private static void LoadLibZmq()
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            var bitnessString = Environment.Is64BitProcess ? "x64" : "x86";
            var libzmqDirectory = String.Format("{0}-{1}-{2}", executingAssembly.GetName().Name, executingAssembly.GetName().Version, bitnessString);
            const string libzmqFileName = "libzmq.dll";

            var libzmqAssemblyDirectory = Path.GetDirectoryName(executingAssembly.Location);
            if (libzmqAssemblyDirectory != null)
            {
                var libzmqAssemblyPath = Path.Combine(libzmqAssemblyDirectory, libzmqDirectory);
                var libzmqAssemblyFilePath = Path.Combine(libzmqAssemblyPath, libzmqFileName);

                // First trying to load it from this assembly's folder's sub-folder: e.g .\SimpleZmq-4.0.2-x64\libzmq.dll
                // It's useful if someone wants to package libzmq.dlls next to their application and don't rely on the extract-to-temp-folder behavior.
                if (TryLoadLibZmq(libzmqAssemblyFilePath))
                {
                    return;
                }
            }

            // we need to extract the embedded libzmq.dll and load that
            var libzmqTempPath = Path.Combine(Path.GetTempPath(), libzmqDirectory);
            var libzmqTempFilePath = Path.Combine(libzmqTempPath, libzmqFileName);

            // making sure that another process doing the same is not racing with us (creating a temp file into the same folder can't be done concurrently or loading a half-created file)
            using (var tempFolderMutex = new Mutex(false, @"Global\" + libzmqDirectory))
            {
                if (tempFolderMutex.WaitOne())
                {
                    try
                    {
                        if (TryLoadLibZmq(libzmqTempFilePath))
                        {
                            // it already exists at the temp location
                            return;
                        }

                        Directory.CreateDirectory(libzmqTempPath);
                        // Copying the libzmq dll from the embedded resource into a temporary file.
                        var libzmqResourceName = String.Format("SimpleZmq.lib.{0}.libzmq.dll", bitnessString);
                        using (var libzmqResourceStream = executingAssembly.GetManifestResourceStream(libzmqResourceName))
                        {
                            if (libzmqResourceStream == null)
                            {
                                throw new InvalidOperationException(String.Format("Couldn't load {0} from the embedded resource '{1}'.", libzmqFileName, libzmqResourceName));
                            }

                            try
                            {
                                using (var libzmqFile = File.Create(libzmqTempFilePath))
                                {
                                    libzmqResourceStream.CopyTo(libzmqFile);
                                }
                            }
                            catch (UnauthorizedAccessException ex)
                            {
                                throw new InvalidOperationException(String.Format("Couldn't copy {0} into a temporary file: {1}.", libzmqFileName, libzmqTempFilePath), ex);
                            }
                            catch (IOException ex)
                            {
                                throw new InvalidOperationException(String.Format("Couldn't copy {0} into a temporary file: {1}.", libzmqFileName, libzmqTempFilePath), ex);
                            }
                        }

                        if (!TryLoadLibZmq(libzmqTempFilePath))
                        {
                            throw new InvalidOperationException(String.Format("Couldn't load {0} from {1}.", libzmqFileName, libzmqTempFilePath));
                        }
                    }
                    finally
                    {
                        tempFolderMutex.ReleaseMutex();
                    }
                }
            }
        }

        #region Misc
        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zmq_errno();

        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr zmq_strerror(int errnum);
        #endregion

        #region Context
        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr zmq_ctx_new();

        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zmq_ctx_term(IntPtr contextPtr);

        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zmq_ctx_shutdown(IntPtr contextPtr);

        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zmq_ctx_set(IntPtr contextPtr, int option, int value);

        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zmq_ctx_get(IntPtr contextPtr, int option);
        #endregion

        #region Sockets
        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr zmq_socket(IntPtr context, int type);

        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zmq_bind(IntPtr socket, [MarshalAs(UnmanagedType.LPStr)] string endpoint);

        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zmq_connect(IntPtr socket, [MarshalAs(UnmanagedType.LPStr)] string endpoint);

        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zmq_send(IntPtr socket, byte[] buf, int len, int flags);

        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zmq_setsockopt(IntPtr socket, int option_name, IntPtr option_value, int option_len);

        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zmq_getsockopt(IntPtr socket, int option_name, IntPtr option_value, IntPtr option_len);

        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zmq_msg_init(IntPtr msg);

        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zmq_msg_close(IntPtr msg);

        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zmq_msg_size(IntPtr msg);

        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr zmq_msg_data(IntPtr msg);

        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zmq_msg_recv(IntPtr msg, IntPtr socket, int flags);

        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zmq_poll([In] [Out] ZmqPollItem[] items, int nitems, long timeout);

        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zmq_socket_monitor(IntPtr socket, [MarshalAs(UnmanagedType.LPStr)] string addr, int events);

        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zmq_close(IntPtr socket);

        // pre-created delegate instances so that we can pass zmq functions as delegates without allocating on the managed heap
        public static readonly Func<IntPtr, byte[], int, int, int> zmq_send_func = LibZmq.zmq_send;
        public static readonly Func<IntPtr, IntPtr, int, int> zmq_msg_recv_func = LibZmq.zmq_msg_recv;
        public static readonly Func<IntPtr, int, IntPtr, int, int> zmq_setsockopt_func = LibZmq.zmq_setsockopt;
        public static readonly Func<IntPtr, int, IntPtr, IntPtr, int> zmq_getsockopt_func = LibZmq.zmq_getsockopt;
        public static readonly Func<ZmqPollItem[], int, long, int> zmq_poll_func = LibZmq.zmq_poll;
        #endregion
    }
}
