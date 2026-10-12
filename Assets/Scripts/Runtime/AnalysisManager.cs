using System.Collections.Generic;
using UnityEngine;

// 현재 선택에서 제품 견적과 연간 냉난방 에너지 비교 결과를 계산한다.
public class AnalysisManager : MonoBehaviour
{
    // 현재 방의 면적 자료를 읽기 위해 RoomController 클래스를 참조한다.
    [SerializeField] private RoomController roomController;
    // 냉난방 시간, 효율, 에너지 단가를 한 곳에서 읽기 위해 EnergyStandardData 클래스를 참조한다.
    [SerializeField] private EnergyStandardData energyStandard;
    // 이번 계산에서 사용하는 방의 면적과 이름을 보관한다.
    private RoomData roomData;
    // 선택한 제품의 구매비 합계를 보관한다.
    private float totalCost;
    // 공사비를 제외한 제품 구매비를 회수기간 계산에 사용하도록 보관한다.
    private float additionalInvestment;
    // 각 부위의 첫 제품으로 계산한 열손실 계수 합계를 보관한다.
    private float baselineHeatLoss;
    // 사용자가 선택한 제품으로 계산한 열손실 계수 합계를 보관한다.
    private float selectedHeatLoss;
    // 비교 제품의 연간 난방과 냉방 에너지 합계를 보관한다.
    private float baselineAnnualEnergyUsage;
    // 선택 제품의 연간 난방과 냉방 에너지 합계를 보관한다.
    private float annualEnergyUsage;
    // 비교 제품의 연간 난방과 냉방 비용 합계를 보관한다.
    private float baselineAnnualEnergyCost;
    // 선택 제품의 연간 난방과 냉방 비용 합계를 보관한다.
    private float annualEnergyCost;
    // 비교 제품 비용에서 선택 제품 비용을 뺀 연간 절감액을 보관한다.
    private float annualSaving;
    // 제품 구매비를 연간 절감액으로 나눈 회수기간을 보관한다.
    private float paybackPeriod;
    // 가격이 빠진 제품이 있을 때 완성된 견적으로 표시하지 않도록 상태를 보관한다.
    private bool estimateComplete;
    // 열성능이나 환경 자료가 빠졌을 때 계산값을 숨기도록 상태를 보관한다.
    private bool analysisComplete;
    // 절감액이 양수이고 가격이 완성된 경우만 회수기간을 표시하도록 상태를 보관한다.
    private bool paybackAvailable;
    // 누락 값과 잘못된 설정을 UIManager 클래스에 전달하도록 안내문을 보관한다.
    private string statusMessage = "";
    // UIManager 클래스가 항목별 견적을 표시하도록 계산된 문자열을 보관한다.
    private string estimateDetails = "";
    // UIManager 클래스가 선택 이름을 표시하도록 마지막 계산 대상들을 보관한다.
    private FinishTarget[] currentTargets;

    // IFEAManager 클래스가 방 선택이나 제품 변경 후 모든 결과를 다시 계산할 때 호출한다.
    public void CalculateAll(GameObject room)
    {
        ClearResults();
        // 선택된 방이 없으면 이전 방의 결과를 남기지 않고 안내만 표시한다.
        if (room == null)
        {
            statusMessage = "Please select a room first.";
            return;
        }
        // 선택 제품과 실제 적용 면적을 읽기 위해 현재 방의 FinishTarget 클래스들을 가져온다.
        FinishTarget[] targets = room.GetComponentsInChildren<FinishTarget>();
        currentTargets = targets;
        CalculateEstimate(targets);
        // 다른 방의 RoomData를 잘못 읽지 않도록 RoomController 연결과 현재 방을 확인한다.
        if (roomController == null || roomController.GetCurrentRoom() != room)
        {
            AddMessage("Check the RoomController reference in AnalysisManager.");
            return;
        }
        // 욕실, 화장실, 발코니 제외 면적이 저장된 현재 RoomData를 가져온다.
        roomData = roomController.GetCurrentRoomData();
        // 면적이나 냉난방 조건이 없으면 임의의 결과를 만들지 않고 계산을 중단한다.
        if (roomData == null || !roomData.HasAreaData() || energyStandard == null || !energyStandard.HasEnvironmentData())
        {
            AddMessage("Set room areas and heating and cooling values.");
            return;
        }
        analysisComplete = true;
        CalculateThermalPerformance(targets);
        // 제품 R값이나 대상 면적 검사에 실패하면 이후 비용 계산도 중단한다.
        if (!analysisComplete) { return; }
        CalculateEnergyUsage();
        CalculateEnergyCost();
        CalculateSaving();
        CalculatePayback();
    }

    // 선택 제품의 단가와 적용 면적을 곱하여 제품 구매비만 계산한다.
    private void CalculateEstimate(FinishTarget[] targets)
    {
        estimateComplete = true;
        // 벽, 바닥, 천장, 창 가림 제품의 비용을 따로 모으기 위한 배열이다.
        float[] categoryCosts = new float[4];
        // 가격이 0인 제품도 선택 항목으로 표시하기 위한 상태 배열이다.
        bool[] categorySelected = new bool[4];
        // 비용 배열과 같은 순서로 화면에 표시할 영어 이름이다.
        string[] categoryNames = { "Wall", "Floor", "Ceiling", "Window Covering" };
        // 현재 방에 실제로 선택된 제품만 찾아 비용을 더한다.
        foreach (FinishTarget target in targets)
        {
            // 장식용 보조 패널과 삭제된 대상은 제품 견적에 포함하지 않는다.
            if (target == null || !target.IsEstimateSurface()) { continue; }
            // 선택 상태를 읽기 위해 FinishTarget 클래스에서 FinishData를 가져온다.
            FinishData finish = target.GetCurrentFinish();
            // 제품을 선택하지 않은 기본 상태에는 구매비를 더하지 않는다.
            if (finish == null) { continue; }
            // 제품 단가를 곱할 시공 면적이나 수량을 가져온다.
            float amount = target.GetArea();
            // 가격, 적용량, 제품 종류가 잘못되면 부분 합계를 전체 견적으로 표시하지 않는다.
            if (!finish.HasPriceData() || amount <= 0f || !target.MatchesFinishType(finish))
            {
                estimateComplete = false;
                AddMessage(target.name + ": Check unit price, area or quantity and category.");
                continue;
            }
            // 사용자가 공사비 제외를 결정했으므로 제품 단가와 적용량만 곱한다.
            float cost = finish.GetUnitPrice() * amount;
            totalCost += cost;
            // 벽 비용을 첫 번째 칸에 모으기 위한 기본 위치이다.
            int categoryIndex = 0;
            // 바닥 비용이 벽 비용과 섞이지 않도록 두 번째 칸을 사용한다.
            if (target.GetFinishType() == FinishType.Floor) { categoryIndex = 1; }
            // 천장 비용이 다른 부위와 섞이지 않도록 세 번째 칸을 사용한다.
            else if (target.GetFinishType() == FinishType.Ceiling) { categoryIndex = 2; }
            // 블라인드와 커튼은 같은 카테고리이므로 마지막 칸에 함께 모은다.
            else if (target.IsWindowCovering()) { categoryIndex = 3; }
            categoryCosts[categoryIndex] += cost;
            categorySelected[categoryIndex] = true;
        }
        // 선택된 부위만 한 줄씩 표시하여 긴 대상 이름이 화면을 덮지 않게 한다.
        for (int index = 0; index < categoryCosts.Length; index++)
        {
            // 제품을 선택하지 않은 카테고리는 비용 목록에 표시하지 않는다.
            if (!categorySelected[index]) { continue; }
            estimateDetails += categoryNames[index] + ": " + categoryCosts[index].ToString("N2")
                + " " + GetCurrencyLabel() + "\n";
        }
        // 가격이나 면적이 빠진 경우 현재 금액이 부분 합계임을 화면에 알린다.
        if (!estimateComplete) { estimateDetails += "Some product prices or areas are missing.\n"; }
        additionalInvestment = totalCost;
    }

    // 외피 기준 R값을 사용하지 않고 제품 R값과 실제 분석 면적으로 열손실 계수를 계산한다.
    private void CalculateThermalPerformance(FinishTarget[] targets)
    {
        // RoomData 전체 면적을 넘는 중복 대상을 찾기 위한 벽 면적 합계이다.
        float usedWallArea = 0f;
        // RoomData 전체 면적을 넘는 중복 대상을 찾기 위한 바닥 면적 합계이다.
        float usedFloorArea = 0f;
        // RoomData 전체 면적을 넘는 중복 대상을 찾기 위한 천장 면적 합계이다.
        float usedCeilingArea = 0f;
        // 창은 같은 ID를 한 번만 계산하므로 일반 부위 반복문에서 제외한다.
        foreach (FinishTarget target in targets)
        {
            // 외기에 접하지 않는 실내 칸막이와 삭제된 대상은 에너지 계산에서 제외한다.
            if (target == null || !target.IsThermalSurface() || target.IsWindowCovering()) { continue; }
            // 욕실, 화장실, 발코니처럼 열 계산에서 뺀 조각은 열면적이 0이므로 건너뛴다.
            float area = target.GetThermalArea();
            if (area == 0f) { continue; }
            // 음수 면적은 입력되지 않은 값이므로 잘못된 계산을 막기 위해 중단한다.
            if (area < 0f)
            {
                FailAnalysis(target.name + ": Set a valid thermal area.");
                return;
            }
            // 외피 기준값 대신 제품 목록의 첫 유효 제품을 비교 기준으로 가져온다.
            FinishData comparisonFinish = target.GetComparisonFinish();
            // 사용자가 고른 제품이 없을 때 비교 제품과 같은 상태로 계산하기 위한 변수이다.
            FinishData selectedFinish = target.GetCurrentFinish();
            // 선택 전 상태에서는 비교안과 선택안이 같아야 하므로 비교 제품을 사용한다.
            if (selectedFinish == null) { selectedFinish = comparisonFinish; }
            // 제품이 없거나 R값이 0이면 U값을 계산할 수 없으므로 분석을 중단한다.
            if (!HasUsableThermalData(target, comparisonFinish)
                || !HasUsableThermalData(target, selectedFinish))
            {
                FailAnalysis(target.name + ": Set a product with thermal resistance above zero.");
                return;
            }
            // 부위별 중복 면적을 확인하기 위해 대상 종류를 읽는다.
            FinishType type = target.GetFinishType();
            // 벽 대상은 벽 전체 면적과 비교할 수 있도록 따로 합산한다.
            if (type == FinishType.Wall) { usedWallArea += area; }
            // 바닥 대상은 바닥 전체 면적과 비교할 수 있도록 따로 합산한다.
            else if (type == FinishType.Floor) { usedFloorArea += area; }
            // 천장 대상은 천장 전체 면적과 비교할 수 있도록 따로 합산한다.
            else if (type == FinishType.Ceiling) { usedCeilingArea += area; }
            // 열손실 계수는 면적을 제품 열저항으로 나누어 계산한다.
            baselineHeatLoss += area / comparisonFinish.GetThermalResistance();
            selectedHeatLoss += area / selectedFinish.GetThermalResistance();
        }
        CalculateWindowThermalPerformance(targets);
        // 창 계산에서 오류가 발생했으면 추가 면적 검사를 하지 않고 중단한다.
        if (!analysisComplete) { return; }
        // 같은 외피 조각이 중복 등록되어 RoomData보다 큰 에너지가 계산되는 것을 막는다.
        if (usedWallArea > roomData.wallArea + 0.001f
            || usedFloorArea > roomData.floorArea + 0.001f
            || usedCeilingArea > roomData.ceilingArea + 0.001f)
        {
            FailAnalysis("Selected surface area exceeds the RoomData total.");
        }
    }

    // 같은 창 ID에 연결된 블라인드와 커튼을 하나의 Window Covering 제품으로 계산한다.
    private void CalculateWindowThermalPerformance(FinishTarget[] targets)
    {
        // 이미 계산한 창 ID를 보관하여 같은 창 면적을 두 번 더하지 않게 한다.
        List<string> checkedWindows = new List<string>();
        // RoomData의 창 전체 면적을 넘는지 확인하기 위한 계산 면적 합계이다.
        float usedWindowArea = 0f;
        // 창 가림 제품이 연결된 모든 대상을 확인한다.
        foreach (FinishTarget target in targets)
        {
            // 외기에 접한 Window Covering 대상만 창 에너지 계산에 사용한다.
            if (target == null || !target.IsThermalSurface() || !target.IsWindowCovering()) { continue; }
            // 같은 창의 대상을 찾기 위해 Inspector에 저장한 창 ID를 읽는다.
            string windowId = target.GetWindowId();
            // ID가 없으면 같은 창을 구분할 수 없으므로 분석을 중단한다.
            if (string.IsNullOrEmpty(windowId))
            {
                FailAnalysis(target.name + ": Set Window Id.");
                return;
            }
            // 이미 계산한 창은 면적이 중복되지 않도록 건너뛴다.
            if (checkedWindows.Contains(windowId)) { continue; }
            checkedWindows.Add(windowId);
            // 창 가림 제품이 실제 적용되는 창 면적을 가져온다.
            float area = target.GetThermalArea();
            // 0면적은 분석 대상에서 제외된 창이므로 계산하지 않는다.
            if (area == 0f) { continue; }
            // 음수 면적은 입력 누락이므로 결과를 만들지 않는다.
            if (area < 0f)
            {
                FailAnalysis(target.name + ": Set a valid window area.");
                return;
            }
            // 외피 기준값 대신 창 제품 목록의 첫 유효 제품을 비교 기준으로 가져온다.
            FinishData comparisonFinish = target.GetComparisonFinish();
            // 같은 창에 선택된 실제 제품을 찾기 위한 변수이다.
            FinishData selectedFinish = null;
            // 같은 창에 서로 다른 제품이 중복 선택되었는지 확인하기 위한 개수이다.
            int selectedCount = 0;
            // 같은 창 ID를 가진 대상에서 실제 선택 제품을 찾는다.
            foreach (FinishTarget other in targets)
            {
                // 다른 창이나 일반 마감 대상은 현재 창의 선택 제품이 아니다.
                if (other == null || !other.IsWindowCovering() || other.GetWindowId() != windowId) { continue; }
                // 같은 창 대상들의 면적이 다르면 어느 면적이 맞는지 알 수 없어 분석을 중단한다.
                if (Mathf.Abs(other.GetThermalArea() - area) > 0.001f)
                {
                    FailAnalysis(windowId + ": Window covering areas do not match.");
                    return;
                }
                // 사용자가 실제로 선택한 제품만 중복 검사와 선택안 계산에 사용한다.
                FinishData currentFinish = other.GetCurrentFinish();
                if (currentFinish == null) { continue; }
                selectedFinish = currentFinish;
                selectedCount++;
            }
            // 블라인드와 커튼은 같은 카테고리이므로 한 창에 한 제품만 선택할 수 있다.
            if (selectedCount > 1)
            {
                FailAnalysis(windowId + ": Select one window covering product.");
                return;
            }
            // 선택 전 상태에서는 비교 제품과 같은 열성능을 사용한다.
            if (selectedFinish == null) { selectedFinish = comparisonFinish; }
            // 비교 제품이나 선택 제품의 R값이 없으면 창 U값을 계산할 수 없다.
            if (!HasUsableThermalData(target, comparisonFinish)
                || !HasUsableThermalData(target, selectedFinish))
            {
                FailAnalysis(windowId + ": Set a window product with thermal resistance above zero.");
                return;
            }
            usedWindowArea += area;
            baselineHeatLoss += area / comparisonFinish.GetThermalResistance();
            selectedHeatLoss += area / selectedFinish.GetThermalResistance();
        }
        // 등록한 창 면적이 RoomData보다 크면 중복 대상이 있으므로 결과를 막는다.
        if (usedWindowArea > roomData.windowArea + 0.001f)
        {
            FailAnalysis("Selected window area exceeds the RoomData total.");
        }
    }

    // 제품 종류와 양수 R값을 함께 검사하여 잘못된 제품으로 나누는 계산을 막는다.
    private bool HasUsableThermalData(FinishTarget target, FinishData finish)
    {
        // FinishTarget 클래스의 대상 종류와 FinishData 클래스의 제품 종류가 맞아야 해당 부위에 사용할 수 있다.
        return target != null && finish != null && target.MatchesFinishType(finish)
            && finish.HasThermalData() && finish.GetThermalResistance() > 0f;
    }

    // 제품 열손실 계수에 난방과 냉방 배율을 각각 적용한 뒤 연간 에너지로 합산한다.
    private void CalculateEnergyUsage()
    {
        // 실내온도 24도 난방 조건을 연간 사용량으로 바꾸는 배율이다.
        float heatingFactor = energyStandard.GetHeatingEnergyFactor();
        // 여름 냉방 조건을 연간 사용량으로 바꾸는 배율이다.
        float coolingFactor = energyStandard.GetCoolingEnergyFactor();
        baselineAnnualEnergyUsage = baselineHeatLoss * heatingFactor + baselineHeatLoss * coolingFactor;
        annualEnergyUsage = selectedHeatLoss * heatingFactor + selectedHeatLoss * coolingFactor;
    }

    // 난방과 냉방의 서로 다른 에너지 단가를 적용하여 연간 비용 합계를 계산한다.
    private void CalculateEnergyCost()
    {
        // 난방 열손실 계수에 난방 사용량 배율과 난방 단가를 적용한다.
        float baselineHeatingCost = baselineHeatLoss * energyStandard.GetHeatingEnergyFactor() * energyStandard.energyPrice;
        // 냉방 열손실 계수에 냉방 사용량 배율과 전기 단가를 적용한다.
        float baselineCoolingCost = baselineHeatLoss * energyStandard.GetCoolingEnergyFactor() * energyStandard.coolingEnergyPrice;
        // 선택 제품의 난방 비용을 계산한다.
        float selectedHeatingCost = selectedHeatLoss * energyStandard.GetHeatingEnergyFactor() * energyStandard.energyPrice;
        // 선택 제품의 냉방 비용을 계산한다.
        float selectedCoolingCost = selectedHeatLoss * energyStandard.GetCoolingEnergyFactor() * energyStandard.coolingEnergyPrice;
        baselineAnnualEnergyCost = baselineHeatingCost + baselineCoolingCost;
        annualEnergyCost = selectedHeatingCost + selectedCoolingCost;
    }

    // 비교 제품보다 줄어든 연간 냉난방 비용을 절감액으로 계산한다.
    private void CalculateSaving()
    {
        annualSaving = baselineAnnualEnergyCost - annualEnergyCost;
    }

    // 제품 구매비가 모두 입력되고 절감액이 양수일 때만 회수기간을 계산한다.
    private void CalculatePayback()
    {
        // 미완성 견적이나 0 이하 절감액은 회수기간을 만들 수 없어 숫자를 표시하지 않는다.
        if (!estimateComplete || annualSaving <= 0f) { return; }
        paybackPeriod = additionalInvestment / annualSaving;
        paybackAvailable = true;
    }

    // 새 방을 계산할 때 이전 방의 결과와 상태가 남지 않도록 모든 값을 초기화한다.
    private void ClearResults()
    {
        roomData = null;
        currentTargets = null;
        totalCost = 0f;
        additionalInvestment = 0f;
        baselineHeatLoss = 0f;
        selectedHeatLoss = 0f;
        baselineAnnualEnergyUsage = 0f;
        annualEnergyUsage = 0f;
        baselineAnnualEnergyCost = 0f;
        annualEnergyCost = 0f;
        annualSaving = 0f;
        paybackPeriod = 0f;
        estimateComplete = false;
        analysisComplete = false;
        paybackAvailable = false;
        statusMessage = "";
        estimateDetails = "";
    }

    // 열 데이터 오류를 기록하여 후속 계산과 UI 숫자 표시를 막는다.
    private void FailAnalysis(string message)
    {
        analysisComplete = false;
        AddMessage(message);
    }

    // 여러 누락 항목을 덮어쓰지 않고 UIManager 클래스에 모두 전달하도록 안내문을 추가한다.
    private void AddMessage(string message)
    {
        statusMessage += message + "\n";
    }

    // UIManager 클래스가 하드코딩하지 않고 EnergyStandardData의 통화 이름을 사용하도록 반환한다.
    public string GetCurrencyLabel()
    {
        // EnergyStandardData가 연결되지 않은 상태에서는 빈 문자열을 반환하여 접근 오류를 막는다.
        if (energyStandard == null) { return ""; }
        return energyStandard.currencyLabel;
    }

    // UIManager 클래스가 직접 다시 계산하지 않고 총 제품 구매비를 표시하도록 반환한다.
    public float GetTotalCost() { return totalCost; }
    // UIManager 클래스가 비교 제품의 연간 냉난방 에너지를 표시하도록 반환한다.
    public float GetBaselineAnnualEnergyUsage() { return baselineAnnualEnergyUsage; }
    // UIManager 클래스가 선택 제품의 연간 냉난방 에너지를 표시하도록 반환한다.
    public float GetAnnualEnergyUsage() { return annualEnergyUsage; }
    // UIManager 클래스가 비교 제품의 연간 냉난방 비용을 표시하도록 반환한다.
    public float GetBaselineAnnualEnergyCost() { return baselineAnnualEnergyCost; }
    // UIManager 클래스가 선택 제품의 연간 냉난방 비용을 표시하도록 반환한다.
    public float GetAnnualEnergyCost() { return annualEnergyCost; }
    // UIManager 클래스가 연간 냉난방 비용 절감액을 표시하도록 반환한다.
    public float GetAnnualSaving() { return annualSaving; }
    // UIManager 클래스가 검증된 투자 회수기간을 표시하도록 반환한다.
    public float GetPaybackPeriod() { return paybackPeriod; }
    // UIManager 클래스가 부분 합계를 완성된 견적과 구분하도록 반환한다.
    public bool IsEstimateComplete() { return estimateComplete; }
    // UIManager 클래스가 누락 자료가 있는 계산 결과를 숨기도록 반환한다.
    public bool IsAnalysisComplete() { return analysisComplete; }
    // UIManager 클래스가 회수 불가 상태를 0년과 구분하도록 반환한다.
    public bool IsPaybackAvailable() { return paybackAvailable; }
    // UIManager 클래스가 계산을 중단한 이유를 화면에 표시하도록 반환한다.
    public string GetStatusMessage() { return statusMessage; }
    // UIManager 클래스가 항목별 제품 비용을 표시하도록 계산된 문자열을 반환한다.
    public string GetEstimateDetails() { return estimateDetails; }

    // UIManager 클래스가 카드에 실제 선택 제품 이름을 표시하도록 해당 분류의 이름을 반환한다.
    public string GetSelectionLabel(FinishType type)
    {
        // 계산 대상이 없으면 선택 상태를 알 수 없으므로 기본 표기를 반환한다.
        if (currentTargets == null) { return "Default"; }
        // 같은 분류에서 처음 찾은 현재 표시 제품 이름을 비교하기 위해 보관한다.
        string displayedName = "";
        // 사용자가 비교 제품이 아닌 다른 제품을 선택했는지 구분하기 위한 값이다.
        bool hasSelectedProduct = false;
        // 요청한 분류의 현재 표시 제품을 찾는다.
        foreach (FinishTarget target in currentTargets)
        {
            // Window Covering 카드는 Blind와 Curtain을 같은 분류로 찾아야 한다.
            bool matchesWindowCovering = type == FinishType.WindowCovering && target != null && target.IsWindowCovering();
            // 일반 카드는 정확히 같은 종류만 표시하며 삭제된 대상은 건너뛴다.
            if (target == null || (!matchesWindowCovering && target.GetFinishType() != type)) { continue; }
            // 선택 이름을 읽기 위해 FinishTarget 클래스에서 현재 FinishData를 가져온다.
            FinishData finish = target.GetCurrentFinish();
            // 추가 선택 제품이 있으면 결과에서 비교 상태와 선택 상태를 구분할 수 있도록 기록한다.
            if (finish != null) { hasSelectedProduct = true; }
            // 추가 선택이 없는 대상은 실제 계산에 사용하는 비교 제품 이름을 표시한다.
            if (finish == null) { finish = target.GetComparisonFinish(); }
            // 비교 제품 자료도 없으면 이름을 만들 수 없어 다음 대상을 확인한다.
            if (finish == null) { continue; }
            // 같은 분류에 서로 다른 제품이 적용되면 한 제품명 대신 혼합 상태를 알린다.
            if (displayedName != "" && displayedName != finish.GetProductName()) { return "Multiple Products"; }
            displayedName = finish.GetProductName();
        }
        // 표시할 제품 자료가 없으면 빈 문자열 대신 기본 상태를 표시한다.
        if (displayedName == "") { return "Default"; }
        // 추가 선택 전에는 해당 이름이 에너지 비교 기준 제품이라는 사실을 함께 표시한다.
        if (!hasSelectedProduct) { return "Comparison, " + displayedName; }
        return displayedName;
    }
}
