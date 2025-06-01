using System;
using System.Collections.Generic;

namespace app.Models
{
    public class ApiInfoViewModel
    {
        public List<ApiEndpointInfo> ApiEndpoints { get; set; }
        public string ApiDocumentation { get; set; }
    }

    public class ApiEndpointInfo
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string HttpMethod { get; set; }
        public string Description { get; set; }
    }
}