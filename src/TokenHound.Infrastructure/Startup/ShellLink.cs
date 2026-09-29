using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Versioning;
using System.Text;

namespace TokenHound.Infrastructure.Startup;

/// <summary>
/// Creates and reads Windows shell shortcuts (<c>.lnk</c>) through the built-in <c>ShellLink</c> COM object.
/// </summary>
[SupportedOSPlatform("windows")]
public static class ShellLink
{
    private const int MAX_PATH_LENGTH = 32767;
    private const int STGM_READ = 0;

    private static readonly Guid CLSID_SHELL_LINK = new("00021401-0000-0000-C000-000000000046");

    /// <summary>
    /// Writes a shortcut, replacing any file already at <paramref name="linkPath"/>.
    /// </summary>
    /// <param name="linkPath">The full path of the <c>.lnk</c> file to write.</param>
    /// <param name="targetPath">The executable the shortcut starts.</param>
    /// <param name="workingDirectory">The working directory for the started process.</param>
    /// <param name="description">The shortcut description shown by the shell.</param>
    /// <exception cref="COMException">The shell could not create or save the shortcut.</exception>
    public static void Save(string linkPath, string targetPath, string workingDirectory, string description)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(linkPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);

        var instance = CreateInstance();

        try
        {

            var link = (IShellLinkW)instance;

            link.SetPath(targetPath);
            link.SetWorkingDirectory(workingDirectory);
            link.SetDescription(description);
            ((IPersistFile)instance).Save(linkPath, true);
        }
        finally
        {

            Marshal.FinalReleaseComObject(instance);
        }
    }

    /// <summary>
    /// Reads the target path of an existing shortcut.
    /// </summary>
    /// <param name="linkPath">The full path of the <c>.lnk</c> file to read.</param>
    /// <returns>The target path stored in the shortcut.</returns>
    /// <exception cref="COMException">The shell could not load the shortcut.</exception>
    public static string ReadTarget(string linkPath)
        => Read(
            linkPath,
            static (link, buffer) => link.GetPath(
                buffer,
                buffer.Capacity,
                IntPtr.Zero,
                0
            )
        );

    /// <summary>
    /// Reads the working directory of an existing shortcut.
    /// </summary>
    /// <param name="linkPath">The full path of the <c>.lnk</c> file to read.</param>
    /// <returns>The working directory stored in the shortcut.</returns>
    /// <exception cref="COMException">The shell could not load the shortcut.</exception>
    public static string ReadWorkingDirectory(string linkPath)
        => Read(linkPath, static (link, buffer) => link.GetWorkingDirectory(buffer, buffer.Capacity));

    private static string Read(string linkPath, Action<IShellLinkW, StringBuilder> readField)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(linkPath);

        var instance = CreateInstance();

        try
        {

            ((IPersistFile)instance).Load(linkPath, STGM_READ);

            var buffer = new StringBuilder(MAX_PATH_LENGTH);

            readField((IShellLinkW)instance, buffer);

            return buffer.ToString();
        }
        finally
        {

            Marshal.FinalReleaseComObject(instance);
        }
    }

    private static object CreateInstance()
    {

        var type = Type.GetTypeFromCLSID(CLSID_SHELL_LINK, throwOnError: true)!;

        return Activator.CreateInstance(type)
            ?? throw new COMException("The ShellLink COM object could not be created.");
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cch, IntPtr pfd, uint fFlags);

        void GetIDList(out IntPtr ppidl);

        void SetIDList(IntPtr pidl);

        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cch);

        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);

        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cch);

        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);

        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cch);

        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);

        void GetHotkey(out short pwHotkey);

        void SetHotkey(short wHotkey);

        void GetShowCmd(out int piShowCmd);

        void SetShowCmd(int iShowCmd);

        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cch, out int piIcon);

        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);

        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);

        void Resolve(IntPtr hwnd, uint fFlags);

        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }
}
