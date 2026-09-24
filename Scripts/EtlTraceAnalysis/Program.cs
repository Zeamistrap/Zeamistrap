using System.Globalization;
using System.Text;
using Microsoft.Diagnostics.Tracing;


if (args.Length < 1 || args.Length > 3)
{
    Console.Error.WriteLine("Usage: EtlTraceAnalysis <trace.etl> [output.csv] [process-id]");
    return 2;
}

string etlPath = Path.GetFullPath(args[0]);
string outputPath = Path.GetFullPath(args.Length >= 2
    ? args[1]
    : Path.Combine(Path.GetDirectoryName(etlPath)!, "gpu-event-inventory.csv"));

int? targetProcessId = args.Length == 3 ? int.Parse(args[2]) : null;

if (!File.Exists(etlPath))
{
    Console.Error.WriteLine($"ETL file was not found: {etlPath}");
    return 2;
}

var summaries = new Dictionary<string, EventSummary>(StringComparer.OrdinalIgnoreCase);
long processedEvents = 0;

Console.WriteLine($"Reading {etlPath}...");
if (targetProcessId.HasValue)
    Console.WriteLine($"Filtering process ID {targetProcessId.Value}...");

using (var source = new ETWTraceEventSource(etlPath))
{
    source.AllEvents += data =>
    {
        processedEvents++;

        if (targetProcessId.HasValue && data.ProcessID != targetProcessId.Value)
            return;

        string provider = data.ProviderName ?? string.Empty;
        string eventName = data.EventName ?? string.Empty;
        string key = $"{data.ProviderGuid}\t{data.TaskGuid}\t{data.Opcode}\t{data.ID}\t{data.ProcessID}\t{eventName}";

        if (!summaries.TryGetValue(key, out EventSummary? summary))
        {
            string samplePayload = data.PayloadNames.Length > 0
                ? string.Join("; ", data.PayloadNames.Select((name, index) => $"{name}={data.PayloadValue(index)}"))
                : string.Empty;

            byte[] eventData = data.EventData();
            int sampleLength = Math.Min(128, eventData.Length);
            string sampleHex = Convert.ToHexString(eventData.AsSpan(0, sampleLength));

            summary = new EventSummary(
                data.ProviderGuid,
                data.TaskGuid,
                data.Opcode.ToString(),
                data.OpcodeName,
                data.ID.ToString(),
                data.ProcessID,
                data.ProcessName,
                provider,
                eventName,
                data.TimeStampRelativeMSec,
                samplePayload,
                sampleHex);
            summaries.Add(key, summary);
        }

        summary.Count++;

        if ((processedEvents & 0xFFFFF) == 0)
            Console.WriteLine($"  processed {processedEvents:N0} events...");
    };

    source.Process();
}

var outputDirectory = Path.GetDirectoryName(outputPath);
if (!string.IsNullOrWhiteSpace(outputDirectory))
    Directory.CreateDirectory(outputDirectory);

using (var writer = new StreamWriter(outputPath, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)))
{
    writer.WriteLine("Count,ProviderGuid,TaskGuid,Opcode,OpcodeName,EventId,ProcessId,ProcessName,Provider,EventName,FirstTimestampMs,SamplePayload,SampleEventDataHex");

    foreach (EventSummary summary in summaries.Values.OrderByDescending(x => x.Count))
    {
        writer.WriteLine(string.Join(",", new string[]
        {
            summary.Count.ToString(CultureInfo.InvariantCulture),
            summary.ProviderGuid.ToString(),
            summary.TaskGuid.ToString(),
            summary.Opcode,
            Csv(summary.OpcodeName),
            summary.EventId,
            summary.ProcessId.ToString(CultureInfo.InvariantCulture),
            Csv(summary.ProcessName),
            Csv(summary.Provider),
            Csv(summary.EventName),
            summary.FirstTimestampMs.ToString("F3", CultureInfo.InvariantCulture),
            Csv(summary.SamplePayload),
            summary.SampleEventDataHex
        }));
    }
}

Console.WriteLine($"Processed {processedEvents:N0} total events.");
Console.WriteLine($"Found {summaries.Count:N0} provider/event combinations.");
Console.WriteLine($"Inventory: {outputPath}");

foreach (EventSummary summary in summaries.Values.OrderByDescending(x => x.Count).Take(30))
    Console.WriteLine($"{summary.Count,12:N0}  PID {summary.ProcessId}  {summary.Provider} / {summary.EventName}");

return 0;

static string Csv(string value)
{
    return $"\"{value.Replace("\"", "\"\"")}\"";
}

internal sealed class EventSummary(
    Guid providerGuid,
    Guid taskGuid,
    string opcode,
    string opcodeName,
    string eventId,
    int processId,
    string processName,
    string provider,
    string eventName,
    double firstTimestampMs,
    string samplePayload,
    string sampleEventDataHex)
{
    public Guid ProviderGuid { get; } = providerGuid;
    public Guid TaskGuid { get; } = taskGuid;
    public string Opcode { get; } = opcode;
    public string OpcodeName { get; } = opcodeName;
    public string EventId { get; } = eventId;
    public int ProcessId { get; } = processId;
    public string ProcessName { get; } = processName;
    public string Provider { get; } = provider;
    public string EventName { get; } = eventName;
    public double FirstTimestampMs { get; } = firstTimestampMs;
    public string SamplePayload { get; } = samplePayload;
    public string SampleEventDataHex { get; } = sampleEventDataHex;
    public long Count { get; set; }
}
