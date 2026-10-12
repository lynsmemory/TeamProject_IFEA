using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 화면 전환과 결과 표시를 담당한다. 계산 결과는 IFEAManager가 전달한다.
public class UIManager : MonoBehaviour
{
    // IFEAManager가 전달한 최신 견적/분석 결과이다.
    private AnalysisResult analysisResult;
    // 방 종류를 선택하는 UI-01 화면이다.
    [SerializeField] private GameObject roomSelectUI;
    // 이동 안내와 중앙 표시점이 있는 UI-02 화면이다.
    [SerializeField] private GameObject guideUI;
    // 바라보는 제품을 보여주는 UI-03 화면이다.
    [SerializeField] private GameObject tooltipUI;
    // 견적과 항목별 금액을 보여주는 UI-04 화면이다.
    [SerializeField] private GameObject estimateUI;
    // 연간 난방과 냉방 에너지, 비용을 보여주는 UI-05 화면이다.
    [SerializeField] private GameObject analysisUI;
    // 최종 견적과 분석을 보여주는 UI-06 화면이다.
    [SerializeField] private GameObject resultUI;
    // 제품 이름과 스펙을 표시하기 위해 TMP 컴포넌트를 참조한다.
    [SerializeField] private TMP_Text tooltipText;
    // 제품 Sprite를 표시하기 위해 Image 컴포넌트를 참조한다.
    [SerializeField] private Image productImage;
    // 항목별 견적을 표시할 TMP 컴포넌트이다.
    [SerializeField] private TMP_Text estimateText;
    // 연간 냉난방 분석을 표시할 TMP 컴포넌트이다.
    [SerializeField] private TMP_Text analysisText;
    // 최종 요약 내용을 표시할 TMP 컴포넌트이다.
    [SerializeField] private TMP_Text resultText;
    // 연결 오류나 자료 누락을 사용자에게 안내할 TMP 컴포넌트이다.
    [SerializeField] private TMP_Text messageText;
    // UI-04의 벽 선택 이름을 AnalysisManager에서 받아 표시한다.
    [SerializeField] private TMP_Text wallSelectionText;
    // UI-04의 바닥 선택 이름을 AnalysisManager에서 받아 표시한다.
    [SerializeField] private TMP_Text floorSelectionText;
    // UI-04의 천장 선택 이름을 AnalysisManager에서 받아 표시한다.
    [SerializeField] private TMP_Text ceilingSelectionText;
    // 같은 창 가림 카테고리의 실제 선택 이름을 표시하기 위해 통합 카드의 TMP 텍스트를 참조한다.
    [SerializeField] private TMP_Text windowCoveringSelectionText;
    // UI-06의 총 견적 카드에 계산값을 표시한다.
    [SerializeField] private TMP_Text resultCostText;
    // UI-06의 연간 냉난방 사용량 카드에 계산값을 표시한다.
    [SerializeField] private TMP_Text resultEnergyText;
    // UI-06의 연간 비용 카드에 계산값을 표시한다.
    [SerializeField] private TMP_Text resultAnnualCostText;
    // UI-06의 절감액 카드에 계산값을 표시한다.
    [SerializeField] private TMP_Text resultSavingText;
    // UI-06의 회수기간 카드에 계산값 또는 계산 불가를 표시한다.
    [SerializeField] private TMP_Text resultPaybackText;
    // UI-06 그래프에서 비교 기준과 선택 결과의 연간 에너지 사용량을 표시한다.
    [SerializeField] private Image energyComparisonBar;
    [SerializeField] private Image energySelectedBar;
    // UI-06 그래프에서 비교 기준과 선택 결과의 연간 냉난방 비용을 표시한다.
    [SerializeField] private Image costComparisonBar;
    [SerializeField] private Image costSelectedBar;
    // 비교 비용 중 절감된 비율을 UI-06 단일 막대로 표시한다.
    [SerializeField] private Image savingsBar;
    // UI-06 그래프 제목과 각 막대의 비교 비율을 한 TMP에 표시한다.
    [SerializeField] private TMP_Text resultGraphText;

    // UI-01을 표시하고 다른 화면을 숨겨 Room 선택을 준비한다.
    public void ShowUIRoomSelect()
    {
        SetPanel(roomSelectUI, true);
        SetPanel(guideUI, false);
        SetPanel(tooltipUI, false);
        SetPanel(estimateUI, false);
        SetPanel(analysisUI, false);
        SetPanel(resultUI, false);
        ShowMessage("");
    }
    // 실제 선택 제품의 이름, 비용, 선언 열저항과 사진을 표시한다. null이면 미선택 상태만 안내한다.
    public void ShowUITooltip(FinishData finish, FinishType targetType)
    {
        SetPanel(tooltipUI, true);
        // 통합 카테고리를 화면에서 공백이 있는 영어 이름으로 읽기 쉽게 표시한다.
        string category = targetType == FinishType.WindowCovering ? "Window Covering" : targetType.ToString();
        // 미선택 상태에는 비교 제품의 가격과 열저항을 노출하지 않는다.
        if (finish == null)
        {
            SetText(tooltipText, category + "\nNot Selected");
            // 이전에 선택했던 제품 사진이 미선택 상태에 남지 않도록 숨긴다.
            if (productImage != null) { productImage.gameObject.SetActive(false); }
            return;
        }
        // 가격 입력 여부에 따라 보여줄 설명을 보관한다.
        string price = "Product Price: Not Set";
        // 제품 단가가 입력된 경우만 구매비 기준을 표시한다. 보관한 공사비는 화면에 포함하지 않는다.
        if (finish.HasPriceData())
        {
            price = "Product Price: " + finish.GetUnitPrice().ToString("N2") + " " + GetCurrency() + "/m²\nInstallation excluded";
        }
        // 열저항 미입력과 계산 불가능한 값을 구분하는 설명이다.
        string thermal = "Thermal Resistance: Not Set";
        ThermalDataState thermalState = finish.GetThermalDataState();
        // 0이나 비정상 값은 미입력과 구분하여 수정할 대상을 알린다.
        if (thermalState == ThermalDataState.Invalid) { thermal = "Thermal Resistance: Invalid (must be above zero)"; }
        // 계산 가능한 선언값만 실제 수치로 표시한다.
        else if (thermalState == ThermalDataState.Usable) { thermal = "Thermal Resistance: " + finish.GetThermalResistance().ToString("0.####") + " m²K/W"; }
        SetText(tooltipText, finish.GetProductName() + "\n" + category + "\n" + price + "\n" + thermal);
        // 사진 컴포넌트가 연결된 경우에만 상태를 바꾼다.
        if (productImage != null)
        {
            productImage.sprite = finish.GetProductImage();
            productImage.gameObject.SetActive(productImage.sprite != null);
        }
    }
    // Room 진입 후 UI-02/04/05를 표시하고 선택/최종 결과 화면을 숨긴다.
    public void ShowUIEstimateAndAnalysis()
    {
        SetPanel(roomSelectUI, false);
        SetPanel(resultUI, false);
        // 이동 안내가 분석 화면의 복귀/결과 버튼을 가리지 않도록 숨긴다.
        SetPanel(guideUI, false);
        SetPanel(estimateUI, true);
        SetPanel(analysisUI, true);
        HideTooltip();
    }
    // 결과 화면만 표시하여 선택 중인 화면과 요약 화면이 겹치지 않게 한다.
    public void ShowUIResult()
    {
        SetPanel(roomSelectUI, false);
        SetPanel(guideUI, false);
        SetPanel(tooltipUI, false);
        SetPanel(estimateUI, false);
        SetPanel(analysisUI, false);
        SetPanel(resultUI, true);
    }
    // 계산 담당 클래스에서 받은 항목별 비용과 총 견적을 UI-04에 표시한다.
    public void UpdateEstimate()
    {
        SetText(estimateText, GetEstimateText());
        // 계산 결과가 있어야 실제 선택 목록을 표시할 수 있다.
        if (analysisResult != null)
        {
            // UI는 IFEAManager가 전달한 이름만 카드에 표시한다.
            SetText(wallSelectionText, analysisResult.GetSelectionLabel(FinishType.Wall));
            SetText(floorSelectionText, analysisResult.GetSelectionLabel(FinishType.Floor));
            SetText(ceilingSelectionText, analysisResult.GetSelectionLabel(FinishType.Ceiling));
            SetText(windowCoveringSelectionText, analysisResult.GetSelectionLabel(FinishType.WindowCovering));
        }
    }
    // 계산 담당 클래스에서 받은 결과 또는 데이터 누락 안내를 UI-05에 표시한다.
    public void UpdateAnalysis() { SetText(analysisText, GetAnalysisText()); }
    // roomName과 같은 계산 결과를 UI-06에 표시한다. 여기서는 계산을 다시 하지 않는다.
    public void UpdateResult(string roomName)
    {
        // 이전 결과의 막대 길이가 미완성 분석 화면에 남지 않도록 먼저 초기화한다.
        ResetResultGraph();
        SetText(resultText, GetResultDetailsText(roomName));
        SetText(resultCostText, "Cost Incomplete");
        SetText(resultEnergyText, "Product Data Needed");
        SetText(resultAnnualCostText, "Product Data Needed");
        SetText(resultSavingText, "Product Data Needed");
        SetText(resultPaybackText, "Unavailable");
        // 분석 클래스가 없으면 미완료 안내만 남기고 값 조회를 중단한다.
        if (analysisResult == null) { return; }
        // 가격이 모두 입력된 경우만 완성된 총 견적을 표시한다.
        if (analysisResult.estimateComplete) { SetText(resultCostText, analysisResult.totalCost.ToString("N2") + " " + GetCurrency()); }
        // 분석 기준이 유효할 때만 사용량/비용/절감액을 숫자로 표시한다.
        if (analysisResult.analysisComplete)
        {
            SetText(resultEnergyText, analysisResult.annualEnergyUsage.ToString("N1") + " kWh");
            SetText(resultAnnualCostText, analysisResult.annualEnergyCost.ToString("N2") + " " + GetCurrency());
            SetText(resultSavingText, analysisResult.annualSaving.ToString("N2") + " " + GetCurrency() + "/year");
            UpdateResultGraph();
        }
        // 절감액과 가격 조건이 유효할 때만 회수기간을 숫자로 표시한다.
        if (analysisResult.paybackAvailable) { SetText(resultPaybackText, analysisResult.paybackPeriod.ToString("0.00") + " years"); }
    }
    // 평소 이동 중에는 분석 패널이 3D 시야를 가리지 않도록 조작 안내만 표시한다.
    public void ShowRoomUI()
    {
        ShowUIRoomSelect();
        SetPanel(roomSelectUI, false);
        SetPanel(guideUI, true);
    }
    // 마감 대상을 바라보지 않을 때 UI-03을 숨긴다.
    public void HideTooltip() { SetPanel(tooltipUI, false); }
    // Manager가 전달한 오류/누락 안내를 공통 텍스트에 표시한다.
    public void ShowMessage(string message) { SetText(messageText, message); }
    // IFEAManager가 화면 전환 전에 UI-06 연결을 확인하도록 반환한다.
    public bool HasResultUI() { return resultUI != null && resultText != null; }
    // IFEAManager가 계산 직후 최신 결과를 전달한다.
    public void SetAnalysisResult(AnalysisResult result) { analysisResult = result; }

    // 화면용 견적 설명을 만든다. 금액은 AnalysisManager에서 읽기만 한다.
    private string GetEstimateText()
    {
        // 분석 클래스가 없으면 잘못된 0원 대신 연결 안내를 표시한다.
        if (analysisResult == null) { return "Analysis result is not ready."; }
        // 견적 완성 여부를 사용자에게 알려주는 제목이다.
        string title = "Product Cost";
        // 가격이 빠졌으면 금액이 알려진 항목만의 합계임을 알린다.
        if (!analysisResult.estimateComplete) { title = "Known Product Subtotal (Incomplete)"; }
        // 항목별 계산 결과를 분석 클래스에서 받아 UI에 표시한다.
        string details = analysisResult.estimateDetails;
        // 기본 상태에서 목록이 비어 있는 이유를 안내한다.
        if (details == "") { details = "No Selected Products"; }
        return title + ": " + analysisResult.totalCost.ToString("N2") + " " + GetCurrency() + "\nInstallation excluded\n" + details;
    }
    // 연간 난방과 냉방 분석 설명을 만든다. 제품값이 누락되면 숫자를 숨긴다.
    private string GetAnalysisText()
    {
        // 분석 클래스 없이는 결과를 읽을 수 없으므로 안내한다.
        if (analysisResult == null) { return "Analysis result is not ready."; }
        // 누락 데이터로 계산한 0이나 이전 결과를 정상값처럼 표시하지 않는다.
        if (!analysisResult.analysisComplete)
        {
            return "Heating and Cooling Analysis Incomplete\n" + analysisResult.statusMessage;
        }
        // 회수기간이 계산 불가능할 때 사용할 설명이다.
        string payback = "Payback: Unavailable (Missing Price or No Positive Savings)";
        // 견적이 완전하고 절감액이 양수일 때만 계산된 기간을 표시한다.
        if (analysisResult.paybackAvailable)
        {
            payback = "Payback: " + analysisResult.paybackPeriod.ToString("0.00") + " years";
        }
        return "Annual Heating and Cooling Estimate, Indoor 24 C\nComparison Product: "
            + analysisResult.baselineAnnualEnergyUsage.ToString("N1") + " kWh/year\nSelected: "
            + analysisResult.annualEnergyUsage.ToString("N1") + " kWh/year\nComparison Cost: "
            + analysisResult.baselineAnnualEnergyCost.ToString("N2") + " " + GetCurrency() + "/year\nSelected Cost: "
            + analysisResult.annualEnergyCost.ToString("N2") + " " + GetCurrency() + "/year\nAnnual Savings: "
            + analysisResult.annualSaving.ToString("N2") + " " + GetCurrency() + "\n" + payback;
    }
    // UI-06 상세 카드에 선택 제품과 비교 기준을 짧게 정리하여 버튼 영역을 넘지 않도록 한다.
    private string GetResultDetailsText(string roomName)
    {
        // AnalysisManager 클래스가 없으면 제품별 비용과 비교 기준을 읽을 수 없으므로 연결 안내만 반환한다.
        if (analysisResult == null) { return roomName + "\n\nAnalysis result is not ready."; }
        // AnalysisManager 클래스가 확인한 네 카테고리의 현재 제품 이름을 결과 상세 카드에 표시하기 위해 가져온다.
        string selectedProducts = GetResultProductText();
        // 열성능 자료가 빠진 경우 잘못된 비교 숫자를 표시하지 않고 상태 안내를 반환한다.
        if (!analysisResult.analysisComplete)
        {
            return roomName + "\n\nSelected Products\n" + selectedProducts
                + "\nHeating and Cooling Analysis Incomplete\n" + analysisResult.statusMessage;
        }
        return roomName + "\n\nSelected Products\n" + selectedProducts
            + "\nComparison HVAC Use: " + analysisResult.baselineAnnualEnergyUsage.ToString("N1") + " kWh/year"
            + "\nComparison HVAC Cost: " + analysisResult.baselineAnnualEnergyCost.ToString("N2") + " " + GetCurrency() + "/year";
    }
    // UI-06에서 실제로 선택한 제품만 한 줄씩 표시한다.
    private string GetResultProductText()
    {
        System.Collections.Generic.List<string> selections = new System.Collections.Generic.List<string>();
        AddResultSelection(selections, "Wall", FinishType.Wall);
        AddResultSelection(selections, "Floor", FinishType.Floor);
        AddResultSelection(selections, "Ceiling", FinishType.Ceiling);
        AddResultSelection(selections, "Window Covering", FinishType.WindowCovering);
        if (selections.Count == 0) { return "No Selected Products"; }
        return string.Join("\n", selections);
    }

    // 선택한 카테고리만 결과 목록에 추가하여 비교 기준 제품이 선택 제품처럼 보이지 않게 한다.
    private void AddResultSelection(System.Collections.Generic.List<string> selections, string category, FinishType type)
    {
        if (!analysisResult.HasSelection(type)) { return; }
        selections.Add(category + ": " + analysisResult.GetSelectionLabel(type));
    }
    // 비교값과 선택값 중 큰 값을 기준으로 같은 항목의 두 막대 길이를 정규화한다.
    private void UpdateResultGraph()
    {
        SetComparisonBars(
            energyComparisonBar,
            energySelectedBar,
            analysisResult.baselineAnnualEnergyUsage,
            analysisResult.annualEnergyUsage);

        SetComparisonBars(
            costComparisonBar,
            costSelectedBar,
            analysisResult.baselineAnnualEnergyCost,
            analysisResult.annualEnergyCost);

        // 절감액은 비교 비용에서 차지하는 비율로 표시하며 손실이 발생하면 0으로 처리한다.
        float savingRate = 0f;
        if (analysisResult.baselineAnnualEnergyCost > 0f)
        {
            savingRate = analysisResult.annualSaving / analysisResult.baselineAnnualEnergyCost;
        }
        SetBarFill(savingsBar, savingRate);

        // 별도 텍스트 오브젝트를 늘리지 않고 기존 GraphText에 비교 비율을 함께 표시한다.
        string energyComparisonPercent = GetComparisonPercent(
            analysisResult.baselineAnnualEnergyUsage,
            analysisResult.baselineAnnualEnergyUsage);
        string energySelectedPercent = GetComparisonPercent(
            analysisResult.annualEnergyUsage,
            analysisResult.baselineAnnualEnergyUsage);
        string costComparisonPercent = GetComparisonPercent(
            analysisResult.baselineAnnualEnergyCost,
            analysisResult.baselineAnnualEnergyCost);
        string costSelectedPercent = GetComparisonPercent(
            analysisResult.annualEnergyCost,
            analysisResult.baselineAnnualEnergyCost);
        string savingPercent = analysisResult.baselineAnnualEnergyCost > 0f
            ? (savingRate * 100f).ToString("0.0") + "%"
            : "N/A";

        SetText(resultGraphText,
            "<size=24><b>Energy and Cost Comparison</b></size>\n\n"
            + "<size=20><b>Annual HVAC Use</b></size>\n"
            + "<color=#777777>Comparison (" + energyComparisonPercent + ")</color>\n"
            + "<color=#A97148>Selected (" + energySelectedPercent + ")</color>\n\n"
            + "<size=20><b>Annual HVAC Cost</b></size>\n"
            + "<color=#777777>Comparison (" + costComparisonPercent + ")</color>\n"
            + "<color=#A97148>Selected (" + costSelectedPercent + ")</color>\n\n"
            + "<size=20><b>Estimated Savings</b></size>\n"
            + "<color=#6E9E72>Savings Rate (" + savingPercent + ")</color>");
    }
    // 비교 기준값을 100%로 보았을 때 해당 값이 차지하는 비율을 화면 문자열로 만든다.
    private string GetComparisonPercent(float value, float comparisonValue)
    {
        if (comparisonValue <= 0f) { return "N/A"; }
        return (value / comparisonValue * 100f).ToString("0.0") + "%";
    }
    // 서로 같은 단위인 두 값을 한 쌍으로 비교할 수 있도록 0~1 범위로 변환한다.
    private void SetComparisonBars(Image comparisonBar, Image selectedBar, float comparisonValue, float selectedValue)
    {
        float maxValue = Mathf.Max(0f, comparisonValue, selectedValue);
        if (maxValue <= 0f)
        {
            SetBarFill(comparisonBar, 0f);
            SetBarFill(selectedBar, 0f);
            return;
        }
        SetBarFill(comparisonBar, comparisonValue / maxValue);
        SetBarFill(selectedBar, selectedValue / maxValue);
    }
    // Image가 연결된 경우에만 Filled Image의 표시 비율을 안전한 범위로 설정한다.
    private void SetBarFill(Image bar, float value)
    {
        if (bar != null) { bar.fillAmount = Mathf.Clamp01(value); }
    }
    // 결과가 없거나 불완전할 때 이전 그래프가 정상값처럼 남지 않도록 모든 막대를 비운다.
    private void ResetResultGraph()
    {
        SetBarFill(energyComparisonBar, 0f);
        SetBarFill(energySelectedBar, 0f);
        SetBarFill(costComparisonBar, 0f);
        SetBarFill(costSelectedBar, 0f);
        SetBarFill(savingsBar, 0f);
        SetText(resultGraphText,
            "<size=24><b>Energy and Cost Comparison</b></size>\n\n"
            + "<size=20><b>Annual HVAC Use</b></size>\nComparison (--%)\nSelected (--%)\n\n"
            + "<size=20><b>Annual HVAC Cost</b></size>\nComparison (--%)\nSelected (--%)\n\n"
            + "<size=20><b>Estimated Savings</b></size>\nSavings Rate (--%)");
    }
    // 패널이 아직 연결되지 않았어도 오류 없이 연결된 화면의 표시 상태만 바꾼다.
    private void SetPanel(GameObject panel, bool visible)
    {
        // 연결된 패널에만 상태를 적용한다.
        if (panel != null) { panel.SetActive(visible); }
    }
    // 가격 단위 문자열을 코드에 고정하지 않고 AnalysisManager가 읽은 기준 데이터의 통화 표기를 사용한다.
    private string GetCurrency()
    {
        // AnalysisManager가 연결되지 않은 상태에서도 오류 없이 통화 표기 없이 표시하기 위한 검사이다.
        if (analysisResult == null) { return ""; }
        return analysisResult.currencyLabel;
    }

    // TMP 컴포넌트에 텍스트를 넣는다. 아직 연결 전인 필드도 오류 없이 처리한다.
    private void SetText(TMP_Text target, string value)
    {
        // 연결된 텍스트에만 문자열을 적용한다.
        if (target != null) { target.text = value; }
    }
}
