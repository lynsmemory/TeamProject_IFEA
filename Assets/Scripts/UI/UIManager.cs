using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 화면 전환과 결과 표시를 담당한다. 가격과 에너지 계산은 AnalysisManager에서 받는다.
public class UIManager : MonoBehaviour
{
    // 계산을 중복 구현하지 않고 검증된 견적/분석 결과를 읽기 위해 참조한다.
    [SerializeField] private AnalysisManager analysisManager;
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

    // UI-01을 표시하고 다른 화면을 숨겨 Room 선택을 준비한다.
    public void ShowUI01()
    {
        SetPanel(roomSelectUI, true);
        SetPanel(guideUI, false);
        SetPanel(tooltipUI, false);
        SetPanel(estimateUI, false);
        SetPanel(analysisUI, false);
        SetPanel(resultUI, false);
        ShowMessage("");
    }
    // finish의 이름, 비용, 선언 열저항과 사진을 표시한다. null이면 기본 상태를 안내한다.
    public void ShowUI03Tooltip(FinishData finish, FinishType targetType)
    {
        SetPanel(tooltipUI, true);
        // 벽, 바닥, 천장의 전체 변경과 창별 장치 변경을 구분하여 조작을 안내한다.
        string controls = "Left Click: Change All / Right Click: Reset All";
        // 창마다 독립적으로 선택하는 블라인드와 커튼에는 개별 적용 안내를 사용한다.
        if (targetType == FinishType.WindowCovering || targetType == FinishType.Blind || targetType == FinishType.Curtain)
        {
            controls = "Left Click: Next Covering / Right Click: Reset";
        }
        // 통합 카테고리를 화면에서 공백이 있는 영어 이름으로 읽기 쉽게 표시한다.
        string category = targetType == FinishType.WindowCovering ? "Window Covering" : targetType.ToString();
        // 기본 상태는 제품 데이터가 없어도 안내를 표시할 수 있게 처리한다.
        if (finish == null)
        {
            SetText(tooltipText, category + "\nDefault\n" + controls);
            // 이전 제품 사진이 기본 상태에 남지 않도록 숨긴다.
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
        // 열저항 미입력과 실제 0 열저항을 구분하는 설명이다.
        string thermal = "Thermal Resistance: Not Set";
        // 선언값이 입력되어 있을 때만 수치를 표시한다.
        if (finish.HasThermalData()) { thermal = "Thermal Resistance: " + finish.GetThermalResistance().ToString("0.####") + " m²K/W"; }
        // 실제 제품 사진과 기본 색상 미리보기를 구분하기 위해 FinishData의 외관 안내를 읽는다.
        string appearance = "";
        // 외관 안내가 입력된 제품에만 표시하여 불필요한 빈 줄을 만들지 않는다.
        if (finish.appearanceNote != "" && finish.appearanceNote != null) { appearance = "\n" + finish.appearanceNote; }
        SetText(tooltipText, finish.GetProductName() + "\n" + category + "\n" + price + "\n" + thermal
            + appearance + "\n" + controls);
        // 사진 컴포넌트가 연결된 경우에만 상태를 바꾼다.
        if (productImage != null)
        {
            productImage.sprite = finish.GetProductImage();
            productImage.gameObject.SetActive(productImage.sprite != null);
        }
    }
    // Room 진입 후 UI-02/04/05를 표시하고 선택/최종 결과 화면을 숨긴다.
    public void ShowUI04_05()
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
    public void ShowUI06()
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
        // 분석 클래스가 있어야 실제 선택 목록을 읽을 수 있으므로 연결된 경우에만 갱신한다.
        if (analysisManager != null)
        {
            // 제품 탐색은 분석 클래스가 담당하고 UI는 전달받은 이름만 카드에 표시한다.
            SetText(wallSelectionText, analysisManager.GetSelectionLabel(FinishType.Wall));
            SetText(floorSelectionText, analysisManager.GetSelectionLabel(FinishType.Floor));
            SetText(ceilingSelectionText, analysisManager.GetSelectionLabel(FinishType.Ceiling));
            SetText(windowCoveringSelectionText, analysisManager.GetSelectionLabel(FinishType.WindowCovering));
        }
    }
    // 계산 담당 클래스에서 받은 결과 또는 데이터 누락 안내를 UI-05에 표시한다.
    public void UpdateAnalysis() { SetText(analysisText, GetAnalysisText()); }
    // roomName과 같은 계산 결과를 UI-06에 표시한다. 여기서는 계산을 다시 하지 않는다.
    public void UpdateResult(string roomName)
    {
        SetText(resultText, GetResultDetailsText(roomName));
        SetText(resultCostText, "Cost Incomplete");
        SetText(resultEnergyText, "Product Data Needed");
        SetText(resultAnnualCostText, "Product Data Needed");
        SetText(resultSavingText, "Product Data Needed");
        SetText(resultPaybackText, "Unavailable");
        // 분석 클래스가 없으면 미완료 안내만 남기고 값 조회를 중단한다.
        if (analysisManager == null) { return; }
        // 가격이 모두 입력된 경우만 완성된 총 견적을 표시한다.
        if (analysisManager.IsEstimateComplete()) { SetText(resultCostText, analysisManager.GetTotalCost().ToString("N2") + " " + GetCurrency()); }
        // 분석 기준이 유효할 때만 사용량/비용/절감액을 숫자로 표시한다.
        if (analysisManager.IsAnalysisComplete())
        {
            SetText(resultEnergyText, analysisManager.GetAnnualEnergyUsage().ToString("N1") + " kWh");
            SetText(resultAnnualCostText, analysisManager.GetAnnualEnergyCost().ToString("N2") + " " + GetCurrency());
            SetText(resultSavingText, analysisManager.GetAnnualSaving().ToString("N2") + " " + GetCurrency() + "/year");
        }
        // 절감액과 가격 조건이 유효할 때만 회수기간을 숫자로 표시한다.
        if (analysisManager.IsPaybackAvailable()) { SetText(resultPaybackText, analysisManager.GetPaybackPeriod().ToString("0.00") + " years"); }
    }
    // 평소 이동 중에는 분석 패널이 3D 시야를 가리지 않도록 조작 안내만 표시한다.
    public void ShowRoomUI()
    {
        ShowUI01();
        SetPanel(roomSelectUI, false);
        SetPanel(guideUI, true);
    }
    // 마감 대상을 바라보지 않을 때 UI-03을 숨긴다.
    public void HideTooltip() { SetPanel(tooltipUI, false); }
    // Manager가 전달한 오류/누락 안내를 공통 텍스트에 표시한다.
    public void ShowMessage(string message) { SetText(messageText, message); }
    // IFEAManager가 화면 전환 전에 UI-06 연결을 확인하도록 반환한다.
    public bool HasResultUI() { return resultUI != null && resultText != null; }

    // 화면용 견적 설명을 만든다. 금액은 AnalysisManager에서 읽기만 한다.
    private string GetEstimateText()
    {
        // 분석 클래스가 없으면 잘못된 0원 대신 연결 안내를 표시한다.
        if (analysisManager == null) { return "Connect AnalysisManager in UIManager."; }
        // 견적 완성 여부를 사용자에게 알려주는 제목이다.
        string title = "Product Cost";
        // 가격이 빠졌으면 금액이 알려진 항목만의 합계임을 알린다.
        if (!analysisManager.IsEstimateComplete()) { title = "Known Product Subtotal (Incomplete)"; }
        // 항목별 계산 결과를 분석 클래스에서 받아 UI에 표시한다.
        string details = analysisManager.GetEstimateDetails();
        // 기본 상태에서 목록이 비어 있는 이유를 안내한다.
        if (details == "") { details = "No Selected Products"; }
        return title + ": " + analysisManager.GetTotalCost().ToString("N2") + " " + GetCurrency() + "\nInstallation excluded\n" + details;
    }
    // 연간 난방과 냉방 분석 설명을 만든다. 제품값이 누락되면 숫자를 숨긴다.
    private string GetAnalysisText()
    {
        // 분석 클래스 없이는 결과를 읽을 수 없으므로 안내한다.
        if (analysisManager == null) { return "Connect AnalysisManager in UIManager."; }
        // 누락 데이터로 계산한 0이나 이전 결과를 정상값처럼 표시하지 않는다.
        if (!analysisManager.IsAnalysisComplete())
        {
            return "Heating and Cooling Analysis Incomplete\n" + analysisManager.GetStatusMessage();
        }
        // 회수기간이 계산 불가능할 때 사용할 설명이다.
        string payback = "Payback: Unavailable (Missing Price or No Positive Savings)";
        // 견적이 완전하고 절감액이 양수일 때만 계산된 기간을 표시한다.
        if (analysisManager.IsPaybackAvailable())
        {
            payback = "Payback: " + analysisManager.GetPaybackPeriod().ToString("0.00") + " years";
        }
        return "Annual Heating and Cooling Estimate, Indoor 24 C\nComparison Product: "
            + analysisManager.GetBaselineAnnualEnergyUsage().ToString("N1") + " kWh/year\nSelected: "
            + analysisManager.GetAnnualEnergyUsage().ToString("N1") + " kWh/year\nComparison Cost: "
            + analysisManager.GetBaselineAnnualEnergyCost().ToString("N2") + " " + GetCurrency() + "/year\nSelected Cost: "
            + analysisManager.GetAnnualEnergyCost().ToString("N2") + " " + GetCurrency() + "/year\nAnnual Savings: "
            + analysisManager.GetAnnualSaving().ToString("N2") + " " + GetCurrency() + "\n" + payback
            + "\nComparison uses the first valid product in each category. Ventilation is excluded.";
    }
    // UI-06 상세 카드에 선택 제품과 비교 기준을 짧게 정리하여 버튼 영역을 넘지 않도록 한다.
    private string GetResultDetailsText(string roomName)
    {
        // AnalysisManager 클래스가 없으면 제품별 비용과 비교 기준을 읽을 수 없으므로 연결 안내만 반환한다.
        if (analysisManager == null) { return roomName + "\n\nConnect AnalysisManager in UIManager."; }
        // AnalysisManager 클래스가 확인한 네 카테고리의 현재 제품 이름을 결과 상세 카드에 표시하기 위해 가져온다.
        string selectedProducts = GetResultProductText();
        // 열성능 자료가 빠진 경우 잘못된 비교 숫자를 표시하지 않고 상태 안내를 반환한다.
        if (!analysisManager.IsAnalysisComplete())
        {
            return roomName + "\n\nSelected Products\n" + selectedProducts
                + "\nHeating and Cooling Analysis Incomplete\n" + analysisManager.GetStatusMessage();
        }
        return roomName + "\n\nSelected Products\n" + selectedProducts
            + "\nComparison HVAC Use: " + analysisManager.GetBaselineAnnualEnergyUsage().ToString("N1") + " kWh/year"
            + "\nComparison HVAC Cost: " + analysisManager.GetBaselineAnnualEnergyCost().ToString("N2") + " " + GetCurrency() + "/year"
            + "\nComparison products are labeled above."
            + "\nVentilation excluded.";
    }
    // UI-06에서 벽, 바닥, 천장과 창 가림의 현재 제품을 한 줄씩 표시한다.
    private string GetResultProductText()
    {
        // AnalysisManager 클래스를 통해 실제 룸 전체에 적용된 벽 제품 이름을 가져온다.
        string wallProduct = analysisManager.GetSelectionLabel(FinishType.Wall);
        // AnalysisManager 클래스를 통해 실제 룸 전체에 적용된 바닥 제품 이름을 가져온다.
        string floorProduct = analysisManager.GetSelectionLabel(FinishType.Floor);
        // AnalysisManager 클래스를 통해 실제 룸 전체에 적용된 천장 제품 이름을 가져온다.
        string ceilingProduct = analysisManager.GetSelectionLabel(FinishType.Ceiling);
        // AnalysisManager 클래스를 통해 각 창에 적용된 블라인드와 커튼 상태를 통합해서 가져온다.
        string windowProduct = analysisManager.GetSelectionLabel(FinishType.WindowCovering);
        return "Wall: " + wallProduct
            + "\nFloor: " + floorProduct
            + "\nCeiling: " + ceilingProduct
            + "\nWindow Covering: " + windowProduct;
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
        if (analysisManager == null) { return ""; }
        return analysisManager.GetCurrencyLabel();
    }

    // TMP 컴포넌트에 텍스트를 넣는다. 아직 연결 전인 필드도 오류 없이 처리한다.
    private void SetText(TMP_Text target, string value)
    {
        // 연결된 텍스트에만 문자열을 적용한다.
        if (target != null) { target.text = value; }
    }
}
