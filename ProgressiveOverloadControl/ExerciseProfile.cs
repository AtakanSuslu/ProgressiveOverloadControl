using System;
using System.Collections.Generic;
using System.Text;

namespace ProgressiveOverloadControl
{
    public class ExerciseProfile
    {
        public (int Min, int Max) WeeklySets { get; init; } = (8, 16); // haftalık hedef set aralığı
        public (int Min, int Max) RepsPerSet { get; init; } = (5, 12); // tipik tekrar aralığı
        public (double Min, double Max) TargetRIR { get; init; } = (1, 3);
        public double ProgressionPct { get; init; } = 0.03; // haftalık hedef artış (hacim/1RM)
        public double AddSetIfBelowPct { get; init; } = 0.90; // hacim hedefinin %90 altıysa set ekle
        public double DeloadTriggerPct { get; init; } = 0.08; // 3 hafta üst üste > %8 artış + RIR düşüş → deload
        public double MinRirForLoadIncrease { get; init; } = 2.0; // bu RIR ve üzerindeyse kilo/tekrar arttır
        public double MaxRirBeforeAddSet { get; init; } = 3.5; // çok rahat → set eklemeyi düşün
        public double MinRirForSafety { get; init; } = 0.5; // çok zorlanma (failure e yakın)
        public double UpperBodyKgStep { get; init; } = 2.5;
        public double LowerBodyKgStep { get; init; } = 5.0;
        public bool IsLower { get; init; } = false;
    }
}
