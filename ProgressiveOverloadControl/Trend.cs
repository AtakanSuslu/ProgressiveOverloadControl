using System;
using System.Collections.Generic;
using System.Text;

namespace ProgressiveOverloadControl
{
    public sealed class Trend
    {
        public double VolMA3;   // son 3 haftanın hacim ortalaması
        public double RmMA3;    // son 3 haftanın 1RM ortalaması
        public double? RirMA3;  // son 3 haftanın RIR ortalaması (varsa)
        public double VolDelta; // son hafta vs önceki hafta %
        public double RmDelta;  // son hafta vs önceki hafta %
        public double? RirDelta;// son hafta vs önceki hafta (negatif → zorlanma arttı)
    }
}
