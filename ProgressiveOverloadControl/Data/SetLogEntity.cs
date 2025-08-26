using System;
using System.Collections.Generic;
using System.Text;

namespace ProgressiveOverloadControl.Data
{
    public class SetLogEntity
    {
        public int Id { get; set; }              // PK
        public DateTime Date { get; set; }
        public string Exercise { get; set; } = "";
        public int SetNo { get; set; }
        public int Reps { get; set; }
        public double WeightKg { get; set; }
        public int? RIR { get; set; }
    }
}
