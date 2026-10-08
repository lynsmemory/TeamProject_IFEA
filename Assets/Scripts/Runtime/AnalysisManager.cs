using UnityEngine;

public sealed class AnalysisManager : MonoBehaviour
{
    // 견적 및 에너지 분석 매니저
    // 변수
    // - temperatureDifference: float
    // - annualOperatingHours: float
    // - heatingCoolingEfficiency: float
    // - energyPrice: float
    // - totalCost: float
    // - additionalInvestment: float
    // - baselineAnnualEnergyUsage: float
    // - annualEnergyUsage: float
    // - baselineAnnualEnergyCost: float
    // - annualEnergyCost: float
    // - annualSaving: float
    // - paybackPeriod: float

    // 함수
    // + CalculateAll(room: GameObject): void
    // - CalculateEstimate(targets: FinishTarget[]): void
    // - CalculateRoomThermalPerformance(targets: FinishTarget[]): float
    // - CalculateEnergyUsage(targets: FinishTarget[]): void
    // - CalculateEnergyCost(): void
    // - CalculateSaving(): void
    // - CalculatePayback(): void
    // + GetTotalCost(): float
    // + GetAnnualEnergyUsage(): float
    // + GetAnnualEnergyCost(): float
    // + GetAnnualSaving(): float
    // + GetPaybackPeriod(): float
}
