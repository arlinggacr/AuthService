public class RequestBody
{
    public string? Database { get; set; }
    public string? SourceTableName { get; set; }
    public string? TargetTableName { get; set; }
    public bool? IsDownload { get; set; }
}
