using RestSharp;
using System.Net;

namespace TwintApi
{
    public class TwintException : Exception
    {
        public TwintException(RestResponse response) 
            : base(response.StatusDescription)
        {
            StatusCode = response.StatusCode;
            StatusDescription = response.StatusDescription;
            Response = response.Content;
        }

        public TwintException(HttpStatusCode statusCode, string statusDescription, string response)
            : base(statusDescription)
        { 
            StatusCode = statusCode;
            StatusDescription = statusDescription;
            Response = response;
        }

        public HttpStatusCode StatusCode { get; init; }
        public string StatusDescription { get; init; }
        public string Response { get; init; }
    }
}
