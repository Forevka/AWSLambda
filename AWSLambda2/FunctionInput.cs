using System;
using System.Collections.Generic;
using System.Text;

namespace AWSLambda2
{
    public class FunctionInput
    {
        public string Action { get; set; }
        public SongDescription Value { get; set; }
    }

    public class SongDescription
    {
        public string Artist { get; set; }
        public string SongTitle { get; set; }
    }
}
