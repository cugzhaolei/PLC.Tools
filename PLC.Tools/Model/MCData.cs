using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLC.Tools.Model
{
    public class MCData
    {
        public required string Name { get; set; }
        public required string Address {  get; set; }
        public required string DateType {  get; set; }
        public int Length {  get; set; }
        public string? StringEncode { get; set; }
    }
}
