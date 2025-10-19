using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreOne.DOMAIN.Models
{
    public class LogHttpErrorRequest
    {
        public string UserID { get; set; }
        public string RequestPath { get; set; }
        public int StatusCode { get; set; }
        public string Headers { get; set; }

       
    }

    public class LogExceptionRequest
    {
        public string UserID { get; set; }
        public string ErrorMessage { get; set; }
        public string StackTrace { get; set; }
        public string RequestPath { get; set; }
        public string Headers { get; set; }

        
    }

}
