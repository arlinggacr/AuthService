namespace PipelineService.Utils.Handler
{
    public class ResponseHandler
    {
        public int Code { get; set; }
        public bool Success { get; set; }
        public object? Data { get; set; }
        public PageModelHandler? Page { get; set; }
        public List<string>? Errors { get; set; }

        public static ResponseHandler ToResponse(
            int code,
            bool success,
            object? data,
            List<string> errors
        )
        {
            var resultResponse = new ResponseHandler
            {
                Code = code,
                Success = success,
                Data = data,
                Errors = errors
            };

            return resultResponse;
        }

        public static ResponseHandler ToResponsePagination(
            int code,
            bool success,
            object? data,
            PageModelHandler pagination
        )
        {
            var resultResponse = new ResponseHandler
            {
                Code = code,
                Success = success,
                Data = data,
                Errors = new List<string>(),
                Page = pagination
            };

            return resultResponse;
        }
    }
}
