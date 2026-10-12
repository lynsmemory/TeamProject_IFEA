using UnityEngine;

// AnalysisManager가 모든 제품을 같은 냉난방 조건으로 비교하도록 환경 데이터를 보관한다.
[CreateAssetMenu(fileName = "NewEnergyStandard", menuName = "IFEA/Data/Energy Standard")]
public class EnergyStandardData : ScriptableObject
{
    // 실내온도 24도를 기준으로 계산한 연간 난방 등가 온도차이다.
    public float temperatureDifference = -1f;
    // 난방 등가 온도차와 곱하여 연간 난방 부하를 만드는 시간이다.
    public float annualOperatingHours = -1f;
    // 난방에 사용한 에너지가 실내 열로 바뀌는 비율이다.
    public float heatingCoolingEfficiency = -1f;
    // 견적과 에너지 비용에 함께 표시할 통화 이름이다.
    public string currencyLabel = "USD";
    // 난방 에너지 1kWh의 가격이며 currencyLabel과 같은 통화를 사용한다.
    public float energyPrice = -1f;
    // 여름 냉방도일을 연간 시간으로 나눈 냉방 등가 온도차이다.
    public float coolingTemperatureDifference = -1f;
    // 냉방 등가 온도차와 곱하여 연간 냉방 부하를 만드는 시간이다.
    public float coolingOperatingHours = -1f;
    // 냉방기가 전기 1kWh로 옮길 수 있는 열량의 비율이다.
    public float coolingEfficiency = -1f;
    // 냉방에 사용하는 전기 1kWh의 가격이며 currencyLabel과 같은 통화를 사용한다.
    public float coolingEnergyPrice = -1f;

    // AnalysisManager가 빠진 환경 값이나 0으로 나누는 계산을 막도록 입력 상태를 반환한다.
    public bool HasEnvironmentData()
    {
        // 난방과 냉방을 연간 합계로 표시하므로 두 조건이 모두 입력된 경우에만 계산한다.
        return temperatureDifference >= 0f && annualOperatingHours >= 0f
            && heatingCoolingEfficiency > 0f && energyPrice >= 0f
            && coolingTemperatureDifference >= 0f && coolingOperatingHours >= 0f
            && coolingEfficiency > 0f && coolingEnergyPrice >= 0f;
    }

    // AnalysisManager가 열손실 계수에 곱할 연간 난방 에너지 배율을 계산한다.
    public float GetHeatingEnergyFactor()
    {
        return temperatureDifference * annualOperatingHours / 1000f / heatingCoolingEfficiency;
    }

    // AnalysisManager가 열손실 계수에 곱할 연간 냉방 에너지 배율을 계산한다.
    public float GetCoolingEnergyFactor()
    {
        return coolingTemperatureDifference * coolingOperatingHours / 1000f / coolingEfficiency;
    }
}
