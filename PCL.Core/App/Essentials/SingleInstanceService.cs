using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using PCL.Core.App.IoC;
using PCL.Core.IO;
using PCL.Core.Utils;

namespace PCL.Core.App.Essentials;

[LifecycleService(LifecycleState.BeforeLoading, Priority = -2134567890)]
[LifecycleScope("single-instance", "单例", false)]
public sealed partial class SingleInstanceService
{
    private static FileStream? _lockStream;
    private static readonly string _LockFilePath = Path.Combine(Paths.SharedLocalData, "instance.lock");

    private static void _TryRpc(string processId, string content)
    {
        var pipeName = $"{RpcService.PipePrefix}@{processId}";
        using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
        pipe.Connect(1000);
        using var sw = new StreamWriter(pipe, PipeComm.PipeEncoding);
        sw.WriteLine(content);
        sw.Write(PipeComm.PipeEndingChar);
        sw.Flush();
    }

    [LifecycleStart]
    private static void _Start()
    {
        bool shouldExit = false;
        try
        {
            var stream = File.Open(_LockFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            Context.Debug("未发现重复实例，正在向单例锁写入信息");
            using var sw = new StreamWriter(stream, Encoding.ASCII, 8, true);
            sw.Write(Basics.CurrentProcessId);
            sw.Flush();
            _lockStream = stream;
        }
        catch (Exception)
        {
            try
            {
                using var stream = File.Open(_LockFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                var pidText = reader.ReadToEnd().Trim();

                // 自愈：检查记录的 PID 是否还活着；死进程残留锁时接管
                if (int.TryParse(pidText, out var oldPid) && oldPid != Environment.ProcessId)
                {
                    bool alive;
                    try { _ = System.Diagnostics.Process.GetProcessById(oldPid); alive = true; }
                    catch { alive = false; }

                    if (!alive)
                    {
                        Context.Warn($"PID {oldPid} 已不存在，清理残留锁并接管");
                        try { File.Delete(_LockFilePath); } catch { /* 文件可能仍被 OS 句柄占用，忽略 */ }
                        // 直接以覆盖方式写回自己的 PID
                        try
                        {
                            var takeover = File.Open(_LockFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
                            using var sw = new StreamWriter(takeover, Encoding.ASCII, 8, true);
                            sw.Write(Basics.CurrentProcessId);
                            sw.Flush();
                            _lockStream = takeover;
                            Context.Debug("已接管单例锁");
                            return; // 正常继续，不退出
                        }
                        catch (Exception ex)
                        {
                            Context.Warn("接管单例锁失败，忽略锁继续启动", ex);
                            return; // 不退出，继续启动
                        }
                    }
                }

                Context.Info($"发现重复实例 {pidText}，尝试传递参数并拉起主窗口");
                try
                {
                    _TryRpc(pidText, "REQ cli\n" + JsonSerializer.Serialize(StartupService.UnhandledCommands, JsonCompat.SerializerOptions));
                    _TryRpc(pidText, "REQ activate");
                }
                catch (Exception ex) { Context.Warn("RPC 通信失败", ex); }
                shouldExit = true;
            }
            catch (Exception ex)
            {
                Context.Error("读取单例锁出错", ex);
                shouldExit = true;
            }
            if (shouldExit) Context.RequestExit(1);
        }
    }

    [LifecycleStop]
    private static void _Stop()
    {
        if (_lockStream is null) return;
        Context.Debug("正在删除单例锁");
        _lockStream.Dispose();
        File.Delete(_LockFilePath);
    }
}
