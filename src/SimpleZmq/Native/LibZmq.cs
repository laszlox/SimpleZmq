using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace SimpleZmq.Native
{
    public static partial class LibZmq
    {
        static LibZmq()
        {
            LoadLibZmq();
        }

        private static void LoadLibZmq()
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            var bitnessString = Environment.Is64BitProcess ? "x64" : "x86";
            var libzmqPath = Path.Combine(Path.GetTempPath(), String.Format("{0}-{1}-{2}", executingAssembly.GetName().Name, executingAssembly.GetName().Version, bitnessString));
            var libzmqFilePath = Path.Combine(libzmqPath, "libzmq.dll");

            Directory.CreateDirectory(libzmqPath);
            if (!File.Exists(libzmqFilePath))
            {
                // Copying the libzmq dll from the embedded resource into a temporary file. If the file already exists, we just use it (the path contains the version number too).
                var libzmqResourceName = String.Format("SimpleZmq.lib.{0}.libzmq.dll", bitnessString);
                using (var libzmqResourceStream = executingAssembly.GetManifestResourceStream(libzmqResourceName))
                {
                    if (libzmqResourceStream == null)
                    {
                        throw new InvalidOperationException(String.Format("Couldn't load libzmq from the embedded resource '{0}'", libzmqResourceName));
                    }

                    try
                    {
                        using (var libzmqFile = File.Create(libzmqFilePath))
                        {
                            libzmqResourceStream.CopyTo(libzmqFile);
                        }
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        throw new InvalidOperationException("Couldn't copy libzmq.dll into a temporary file.", ex);
                    }
                    catch (IOException ex)
                    {
                        throw new InvalidOperationException("Couldn't copy libzmq.dll into a temporary file.", ex);
                    }
                }
            }

            if (Win32.LoadLibrary(libzmqFilePath) == IntPtr.Zero)
            {
                throw new InvalidOperationException(String.Format("Couldn't load library '{0}'", libzmqFilePath));
            }
        }

        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zmq_errno();

        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr zmq_strerror(int errnum);

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

    }
}
