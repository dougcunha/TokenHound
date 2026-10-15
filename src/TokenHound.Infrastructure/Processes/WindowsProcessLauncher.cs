using System.ComponentModel;
using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace TokenHound.Infrastructure.Processes;

[SupportedOSPlatform("windows")]
internal static class WindowsProcessLauncher
{
    internal static OwnedProcess Start(string executable, string arguments)
    {

        var job = WindowsJob.Create();
        var pipes = new List<AnonymousPipeServerStream>();

        try
        {

            pipes.Add(new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable));
            pipes.Add(new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable));
            pipes.Add(new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable));
            var handles = pipes.Select(static pipe => pipe.ClientSafePipeHandle.DangerousGetHandle()).ToArray();
            var process = Create((executable, arguments), job, handles);

            return Connect(process, job, pipes);
        }
        catch
        {

            job.Dispose();

            foreach (var pipe in pipes)
                pipe.Dispose();

            throw;
        }
    }

    private static OwnedProcess Connect(Process process, WindowsJob job, List<AnonymousPipeServerStream> pipes)
    {

        foreach (var pipe in pipes)
            pipe.DisposeLocalCopyOfClientHandle();

        return new OwnedProcess(
            process,
            new StreamWriter(pipes[0], new UTF8Encoding(false)),
            new StreamReader(pipes[1], Encoding.UTF8),
            new StreamReader(pipes[2], Encoding.UTF8),
            job
        );
    }

    private static Process Create((string Executable, string Arguments) command, WindowsJob job, IntPtr[] streams)
    {

        using var attributes = new WindowsProcessAttributes();
        attributes.SetHandles(job, streams);
        var startup = new WindowsProcessNative.StartupInfoEx
        {
            Startup = new WindowsProcessNative.StartupInfo
            {
                Size = Marshal.SizeOf<WindowsProcessNative.StartupInfoEx>(),
                Flags = WindowsProcessNative.USE_STD_HANDLES,
                StandardInput = streams[0],
                StandardOutput = streams[1],
                StandardError = streams[2]
            },
            Attributes = attributes.DangerousGetHandle()
        };

        return CreateProcess(command.Executable, command.Arguments, ref startup);
    }

    private static Process CreateProcess(string executable, string arguments, ref WindowsProcessNative.StartupInfoEx startup)
    {

        if (!WindowsProcessNative.CreateProcessW(
            null,
            new StringBuilder($"\"{executable}\" {arguments}"),
            IntPtr.Zero,
            IntPtr.Zero,
            true,
            WindowsProcessNative.CREATION_FLAGS,
            IntPtr.Zero,
            null,
            ref startup,
            out var information
        ))
            throw new Win32Exception(Marshal.GetLastPInvokeError());

        return OpenProcess(information);
    }

    private static Process OpenProcess(WindowsProcessNative.ProcessInformation information)
    {

        try
        {

            var process = Process.GetProcessById(information.ProcessId);
            _ = process.SafeHandle;

            if (WindowsProcessNative.ResumeThread(information.Thread) == uint.MaxValue)
            {

                var error = Marshal.GetLastPInvokeError();
                process.Dispose();
                throw new Win32Exception(error);
            }

            return process;
        }
        finally
        {

            WindowsProcessNative.CloseHandle(information.Thread);
            WindowsProcessNative.CloseHandle(information.Process);
        }
    }
}
