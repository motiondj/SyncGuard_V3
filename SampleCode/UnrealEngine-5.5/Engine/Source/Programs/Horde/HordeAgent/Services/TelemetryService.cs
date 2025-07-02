// Copyright Epic Games, Inc. All Rights Reserved.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using EpicGames.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Management.Infrastructure;

namespace HordeAgent.Services;

/// <summary>
/// Metrics for CPU usage
/// </summary>
public class CpuMetrics
{
	/// <summary>
	/// Percentage of time the CPU was busy executing code in user space
	/// </summary>
	public float User { get; set; }

	/// <summary>
	/// Percentage of time the CPU was busy executing code in kernel space
	/// </summary>
	public float System { get; set; }

	/// <summary>
	/// Percentage of time the CPU was idling
	/// </summary>
	public float Idle { get; set; }

	/// <inheritdoc />
	public override string ToString()
	{
		return $"User={User,5:F1}% System={System,5:F1}% Idle={Idle,5:F1}%";
	}
}

/// <summary>
/// Metrics for memory usage
/// </summary>
public class MemoryMetrics
{
	/// <summary>
	/// Total memory installed (kibibytes)
	/// </summary>
	public uint Total { get; set; }

	/// <summary>
	/// Available memory (kibibytes)
	/// </summary>
	public uint Available { get; set; }

	/// <summary>
	/// Used memory (kibibytes)
	/// </summary>
	public uint Used { get; set; }

	/// <summary>
	/// Used memory (percentage)
	/// </summary>
	public float UsedPercentage { get; set; }

	/// <inheritdoc />
	public override string ToString()
	{
		return $"Total={Total} kB, Available={Available} kB, Used={Used} kB, Used={UsedPercentage * 100.0:F1} %";
	}
}

/// <summary>
/// Metrics for free disk space
/// </summary>
public class DiskMetrics
{
	/// <summary>
	/// Amount of free space on the drive
	/// </summary>
	public long FreeSpace { get; set; }

	/// <summary>
	/// Total size of the drive
	/// </summary>
	public long TotalSize { get; set; }
}

/// <summary>
/// OS agnostic interface for retrieving system metrics (CPU, memory etc)
/// </summary>
public interface ISystemMetrics
{
	/// <summary>
	/// Get CPU usage metrics
	/// </summary>
	/// <returns>An object with CPU usage metrics</returns>
	CpuMetrics? GetCpu();

	/// <summary>
	/// Get memory usage metrics
	/// </summary>
	/// <returns>An object with memory usage metrics</returns>
	MemoryMetrics? GetMemory();

	/// <summary>
	/// Gets HDD metrics
	/// </summary>
	/// <returns>An object with disk usage metrics</returns>
	DiskMetrics? GetDisk();
}

/// <summary>
/// Default implementation of <see cref="ISystemMetrics"/>
/// </summary>
public sealed class DefaultSystemMetrics : ISystemMetrics
{
	/// <inheritdoc/>
	public CpuMetrics? GetCpu()
		=> null;

	/// <inheritdoc/>
	public MemoryMetrics? GetMemory()
		=> null;

	/// <inheritdoc/>
	public DiskMetrics? GetDisk()
		=> null;
}

/// <summary>
/// Windows specific implementation for gathering system metrics
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsSystemMetrics : ISystemMetrics, IDisposable
{
	private const string ProcessorInfo = "Processor Information"; // Prefer this over "Processor" as it's more modern
	private const string Memory = "Memory";
	private const string Total = "_Total";

	private readonly PerformanceCounter _procIdleTime = new(ProcessorInfo, "% Idle Time", Total);
	private readonly PerformanceCounter _procUserTime = new(ProcessorInfo, "% User Time", Total);
	private readonly PerformanceCounter _procPrivilegedTime = new(ProcessorInfo, "% Privileged Time", Total);

	private readonly uint _totalPhysicalMemory = GetPhysicalMemory();
	private readonly PerformanceCounter _memAvailableBytes = new(Memory, "Available Bytes");

	private readonly DirectoryReference _workingDir;
	private readonly Stopwatch _driveInfoUpdateTimer = Stopwatch.StartNew();
	private DriveInfo? _driveInfo;

	/// <summary>
	/// Constructor
	/// </summary>
	public WindowsSystemMetrics(DirectoryReference workingDir)
	{
		_workingDir = workingDir;
		_driveInfo = GetDriveInfo();

		GetCpu(); // Trigger this to ensure performance counter has a fetched value. Avoids an initial zero result when called later.
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		_memAvailableBytes.Dispose();
		_procIdleTime.Dispose();
		_procUserTime.Dispose();
		_procPrivilegedTime.Dispose();
	}

	/// <inheritdoc />
	public CpuMetrics GetCpu()
	{
		return new CpuMetrics
		{
			User = _procUserTime.NextValue(),
			System = _procPrivilegedTime.NextValue(),
			Idle = _procIdleTime.NextValue()
		};
	}

	/// <inheritdoc />
	public MemoryMetrics GetMemory()
	{
		uint available = (uint)(_memAvailableBytes.NextValue() / 1024);
		uint used = _totalPhysicalMemory - available;
		return new MemoryMetrics
		{
			Total = _totalPhysicalMemory,
			Available = available,
			Used = used,
			UsedPercentage = used / (float)_totalPhysicalMemory,
		};
	}

	/// <inheritdoc/>
	public DiskMetrics? GetDisk()
	{
		// Update the metrics if we don't have a recent sample
		if (_driveInfoUpdateTimer.Elapsed > TimeSpan.FromSeconds(20))
		{
			_driveInfo = GetDriveInfo();
			_driveInfoUpdateTimer.Restart();
		}

		if (_driveInfo == null)
		{
			return null;
		}

		return new DiskMetrics { FreeSpace = _driveInfo.AvailableFreeSpace, TotalSize = _driveInfo.TotalSize };
	}

	DriveInfo? GetDriveInfo()
	{
		string? rootDir = Path.GetPathRoot(_workingDir.FullName);
		if (rootDir == null)
		{
			return null;
		}
		return new DriveInfo(rootDir);
	}

	private static uint GetPhysicalMemory()
	{
		using CimSession session = CimSession.Create(null);
		const string QueryNamespace = @"root\cimv2";
		const string QueryDialect = "WQL";
		ulong totalCapacity = 0;

		foreach (CimInstance instance in session.QueryInstances(QueryNamespace, QueryDialect, "select Capacity from Win32_PhysicalMemory"))
		{
			foreach (CimProperty property in instance.CimInstanceProperties)
			{
				if (property.Name.Equals("Capacity", StringComparison.OrdinalIgnoreCase) && property.Value is ulong capacity)
				{
					totalCapacity += capacity;
				}
			}
		}

		return (uint)(totalCapacity / 1024); // as kibibytes
	}
}

#pragma warning restore CA1416

/// <summary>
/// Send telemetry events back to server at regular intervals
/// </summary>
class TelemetryService : IHostedService, IDisposable
{
	private readonly TimeSpan _heartbeatInterval = TimeSpan.FromSeconds(60);
	private readonly TimeSpan _heartbeatMaxAllowedDiff = TimeSpan.FromSeconds(5);

	private readonly ILogger<TelemetryService> _logger;

	private CancellationTokenSource? _eventLoopHeartbeatCts;
	private Task? _eventLoopTask;
	internal Func<DateTime> GetUtcNow { get; set; } = () => DateTime.UtcNow;

	/// <summary>
	/// Constructor
	/// </summary>
	public TelemetryService(ILogger<TelemetryService> logger)
	{
		_logger = logger;
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		_eventLoopHeartbeatCts?.Dispose();
	}

	/// <inheritdoc />
	public Task StartAsync(CancellationToken cancellationToken)
	{
		_eventLoopHeartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		_eventLoopTask = EventLoopHeartbeatAsync(_eventLoopHeartbeatCts.Token);
		return Task.CompletedTask;
	}

	public async Task StopAsync(CancellationToken cancellationToken)
	{
		if (_eventLoopHeartbeatCts != null)
		{
			await _eventLoopHeartbeatCts.CancelAsync();
		}

		if (_eventLoopTask != null)
		{
			try
			{
				await _eventLoopTask;
			}
			catch (OperationCanceledException)
			{
				// Ignore cancellation exceptions
			}
		}
	}

	/// <summary>
	/// Continuously run the heartbeat for the event loop
	/// </summary>
	/// <param name="cancellationToken">Cancellation token</param>
	private async Task EventLoopHeartbeatAsync(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			(bool onTime, TimeSpan diff) = await IsEventLoopOnTimeAsync(_heartbeatInterval, _heartbeatMaxAllowedDiff, cancellationToken);
			if (!onTime)
			{
				_logger.LogWarning("Event loop heartbeat was not on time. Diff {Diff} ms", diff.TotalMilliseconds);
			}
		}
	}

	/// <summary>
	/// Checks if the async event loop is on time
	/// </summary>
	/// <param name="wait">Time to wait</param>
	/// <param name="maxDiff">Max allowed diff</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>A tuple with the result</returns>
	public async Task<(bool onTime, TimeSpan diff)> IsEventLoopOnTimeAsync(TimeSpan wait, TimeSpan maxDiff, CancellationToken cancellationToken)
	{
		try
		{
			DateTime before = GetUtcNow();
			await Task.Delay(wait, cancellationToken);
			DateTime after = GetUtcNow();
			TimeSpan diff = after - before - wait;
			return (diff.Duration() < maxDiff, diff);
		}
		catch (TaskCanceledException)
		{
			// Ignore
			return (true, TimeSpan.Zero);
		}
	}

	/// <summary>
	/// Get list of any filter drivers known to be problematic for builds
	/// </summary>
	/// <param name="logger">Logger to use</param>
	/// <param name="cancellationToken">Cancellation token for the call</param>
	public static async Task<List<string>> GetProblematicFilterDriversAsync(ILogger logger, CancellationToken cancellationToken)
	{
		try
		{
			List<string> problematicDrivers = new()
			{
				// PlayStation SDK related filter drivers known to have negative effects on file system performance
				"cbfilter",
				"cbfsfilter",
				"cbfsconnect",
				"sie-filemon",

				"csagent", // CrowdStrike
			};

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				string output = await ReadFltMcOutputAsync(cancellationToken);
				List<string>? drivers = ParseFltMcOutput(output);
				if (drivers == null)
				{
					logger.LogWarning("Unable to get loaded filter drivers");
					return new List<string>();
				}

				List<string> loadedDrivers = drivers
					.Where(x =>
					{
						foreach (string probDriverName in problematicDrivers)
						{
							if (x.Contains(probDriverName, StringComparison.OrdinalIgnoreCase))
							{
								return true;
							}
						}
						return false;
					})
					.ToList();

				return loadedDrivers;
			}
		}
		catch (Exception e)
		{
			logger.LogError(e, "Error logging filter drivers");
		}

		return new List<string>();
	}

	/// <summary>
	/// Log any filter drivers known to be problematic for builds
	/// </summary>
	/// <param name="logger">Logger to use</param>
	/// <param name="cancellationToken">Cancellation token for the call</param>
	public static async Task LogProblematicFilterDriversAsync(ILogger logger, CancellationToken cancellationToken)
	{
		List<string> loadedDrivers = await GetProblematicFilterDriversAsync(logger, cancellationToken);
		if (loadedDrivers.Count > 0)
		{
			logger.LogWarning("Agent has problematic filter drivers loaded: {FilterDrivers}", String.Join(',', loadedDrivers));
		}
	}

	internal static async Task<string> ReadFltMcOutputAsync(CancellationToken cancellationToken)
	{
		string fltmcExePath = Path.Combine(Environment.SystemDirectory, "fltmc.exe");
		using CancellationTokenSource cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		cancellationSource.CancelAfter(10000);
		using ManagedProcess process = new(null, fltmcExePath, "filters", null, null, null, ProcessPriorityClass.Normal);
		StringBuilder sb = new(1000);

		while (!cancellationToken.IsCancellationRequested)
		{
			string? line = await process.ReadLineAsync(cancellationToken);
			if (line == null)
			{
				break;
			}

			sb.AppendLine(line);
		}

		await process.WaitForExitAsync(cancellationToken);
		return sb.ToString();
	}

	internal static List<string>? ParseFltMcOutput(string output)
	{
		if (output.Contains("access is denied", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}

		List<string> filters = new();
		string[] lines = output.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

		foreach (string line in lines)
		{
			if (line.Length < 5)
			{
				continue;
			}
			if (line.StartsWith("---", StringComparison.Ordinal))
			{
				continue;
			}
			if (line.StartsWith("Filter", StringComparison.Ordinal))
			{
				continue;
			}

			string[] parts = line.Split("   ", StringSplitOptions.RemoveEmptyEntries);
			string filterName = parts[0];
			filters.Add(filterName);
		}

		return filters;
	}
}