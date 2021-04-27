using System;
using System.IO;
using System.Runtime.InteropServices;

namespace GetMsiDetails
{
    public sealed class MsiPackage : IDisposable
    {
        [DllImport("msi.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
        private static extern int MsiOpenPackageW(string szPackagePath, out IntPtr hProduct);

        [DllImport("msi.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
        private static extern int MsiCloseHandle(IntPtr hAny);

        [DllImport("msi.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
        private static extern int MsiGetPropertyW(IntPtr hAny, string name, System.Text.StringBuilder buffer, ref int bufferLength);

        public IntPtr Handle { get; private set; }

        public MsiPackage(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (!File.Exists(path))
            {
                throw new ArgumentException($"File not found: {path}", nameof(path));
            }

            int result = MsiOpenPackageW(path, out IntPtr handle);
            ThrowIfFailureResult(result, "Error opening msi package.");
            Handle = handle;
        }

        public string GetPackageProperty(string property)
        {
            int length = 256;
            var buffer = new System.Text.StringBuilder(length);
            int result = MsiGetPropertyW(Handle, property, buffer, ref length);
            ThrowIfFailureResult(result, "Error reading property from msi package.");
            return buffer.ToString();
        }

        private void ThrowIfFailureResult(int result, string message)
        {
            if (result != 0)
            {
                Exception innerException = Marshal.GetExceptionForHR(result);
                throw new InvalidOperationException($"{message}. HRESULT = {result}.", innerException);
            }
        }

        private void ReleaseUnmanagedResources()
        {
            if (Handle != IntPtr.Zero)
            {
                MsiCloseHandle(Handle);
                Handle = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~MsiPackage()
        {
            ReleaseUnmanagedResources();
        }
    }
}
