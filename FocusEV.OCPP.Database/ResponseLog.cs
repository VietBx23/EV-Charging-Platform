using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FocusEV.OCPP.Database
{
    public class ResponseLog
    {
        public int ResponseLogId { get; set; }
        public string isType { get; set; }
        public string isResponse { get; set; }
        public DateTime CreateDate { get; set; }
    }
}
