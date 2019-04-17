using System;
using System.Collections.Generic;
using System.Text;

namespace AWSLambda2
{
    class Response
    {
        public int Status { get; set; }
        public string Description { get; set; }
    }

    public class LambdaRequest
    {
        public string body { get; set; }
    }
}
