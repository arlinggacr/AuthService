namespace PipelineService.Utils.Handler
{
    public class PageModelHandler
    {
        public Dictionary<string, object>? KeyValues { get; set; }
        public int Total { get; set; }
        public int Page { get; set; }
        public int TotalPage { get; set; }
    }
}
