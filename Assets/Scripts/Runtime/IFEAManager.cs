using UnityEngine;

// 전체 화면 흐름과 3D 조작 가능 상태를 같은 값으로 관리한다.
public enum IFEAState
{
    RoomSelection,
    RoomInteraction,
    EstimateAnalysis,
    Result
}

// UI, 방, Player, 계산 클래스의 실행 순서를 조정한다. 기능 자체는 각 담당 클래스에 맡긴다.
public class IFEAManager : MonoBehaviour
{
    // 방을 생성하고 현재 모델을 얻기 위해 RoomController를 참조한다.
    [SerializeField] private RoomController roomController;
    // 선택/결과 화면에서 3D 입력을 막고 Room에서는 허용하기 위해 참조한다.
    [SerializeField] private PlayerInteraction playerInteraction;
    // 제품 변경 후 현재 방의 견적/에너지를 다시 계산하도록 요청하기 위해 참조한다.
    [SerializeField] private AnalysisManager analysisManager;
    // 계산 결과를 표시하고 UI-01~06을 전환하도록 요청하기 위해 참조한다.
    [SerializeField] private UIManager uiManager;
    // ESC 입력과 화면 전환이 현재 흐름에 맞게 동작하도록 상태를 보관한다.
    private IFEAState currentState = IFEAState.RoomSelection;

    // 모든 Awake 뒤 최초 화면을 준비하여 Room 선택 전의 3D 입력을 막는다.
    private void Start()
    {
        // 필요한 클래스가 빠졌으면 연결 안내 후 시작을 중단한다.
        if (!HasRequiredReferences()) { return; }
        SetState(IFEAState.RoomSelection);
        uiManager.ShowUIRoomSelect();
        analysisManager.CalculateAll(null, null);
        RefreshUI();
        // 처음 방을 고르는 것은 정상 시작 흐름이므로 빈 분석의 선택 안내를 화면 오류처럼 남기지 않는다.
        uiManager.ShowMessage("");
    }
    // roomType에 맞는 방을 요청하고 분석/UI/Player 입력을 순서대로 준비한다.
    public void StartRoom(RoomType roomType)
    {
        // 연결이 빠진 클래스를 호출하지 않도록 검사한다.
        if (!HasRequiredReferences()) { return; }
        SetState(IFEAState.RoomSelection);
        // Prefab 생성과 Player 배치는 RoomController의 책임이므로 해당 클래스에 요청한다.
        roomController.LoadRoom(roomType);
        // 생성에 실패하면 빈 방을 분석하지 않고 문제를 안내한다.
        if (roomController.GetStatusMessage() != "" || roomController.GetCurrentRoom() == null)
        {
            uiManager.ShowUIRoomSelect();
            ShowMessage(roomController.GetStatusMessage());
            return;
        }
        // Player 시점 초기화도 전체 흐름을 아는 IFEAManager가 요청한다.
        playerInteraction.ResetViewRotation();
        // 화면 표시 상태를 바꾸는 기능은 UIManager에 요청한다.
        uiManager.ShowRoomUI();
        OnFinishChanged();
        SetState(IFEAState.RoomInteraction);
    }
    // UI Button에서 enum 인수를 고르지 않고 원룸을 선택하도록 제공한다.
    public void StartOneRoom() { StartRoom(RoomType.OneRoom); }
    // 투룸 버튼이 같은 방 생성 흐름을 사용하도록 제공한다.
    public void StartTwoRoom() { StartRoom(RoomType.TwoRoom); }
    // 쓰리룸 버튼이 같은 방 생성 흐름을 사용하도록 제공한다.
    public void StartThreeRoom() { StartRoom(RoomType.ThreeRoom); }
    // 클릭한 부위에 다음 제품을 적용하도록 요청한다.
    public void SelectNextFinish(FinishTarget clickedTarget)
    {
        ChangeFinish(clickedTarget, false);
    }
    // 클릭한 부위를 기본 제품 상태로 복원하도록 요청한다.
    public void ResetFinishSelection(FinishTarget clickedTarget)
    {
        ChangeFinish(clickedTarget, true);
    }
    // 일반 마감은 같은 종류 전체, 창 가림은 클릭한 대상 하나에 변경 동작을 적용한다.
    private void ChangeFinish(FinishTarget clickedTarget, bool reset)
    {
        // 방이나 클릭 대상이 없으면 변경할 마감 목록을 읽을 수 없어 중단한다.
        if (!HasRequiredReferences() || clickedTarget == null || roomController.GetCurrentRoom() == null) { return; }
        // 클릭한 부위의 종류를 FinishTarget에서 읽어 다른 종류의 마감은 유지한다.
        FinishType selectedType = clickedTarget.GetFinishType();
        // 창 장치는 창마다 제품을 선택해야 하므로 벽/바닥/천장 전체 변경과 구분한다.
        if (clickedTarget.IsWindowCovering())
        {
            // 우클릭은 해당 창 장치만 기본 상태로 복원한다.
            if (reset) { clickedTarget.ResetFinish(); }
            // 좌클릭은 해당 창 장치의 다음 제품 모델을 생성한다.
            else { clickedTarget.NextFinish(); }
            OnFinishChanged();
            ShowTarget(clickedTarget);
            // 모델 연결 실패를 정상 선택으로 표시하지 않도록 안내한다.
            if (clickedTarget.GetStatusMessage() != "") { ShowMessage(clickedTarget.GetStatusMessage()); }
            return;
        }
        // 방 전체의 동일 종류 마감을 변경하기 위해 RoomController가 보관한 모델에서 FinishTarget을 가져온다.
        FinishTarget[] targets = roomController.GetCurrentFinishTargets();
        // 하나라도 변경에 실패하면 성공 안내 대신 연결 오류를 표시하기 위한 문자열이다.
        string errors = "";
        // 각 표면의 실제 재질 교체는 FinishTarget의 책임이므로 해당 클래스에 요청한다.
        foreach (FinishTarget target in targets)
        {
            // 다른 종류의 표면은 이번 클릭의 변경 범위에 포함되지 않아 건너뛴다.
            if (target.GetFinishType() != selectedType) { continue; }
            // 우클릭이면 같은 종류 전체의 원래 재질을 복원한다.
            if (reset) { target.ResetFinish(); }
            // 좌클릭이면 같은 목록과 선택 순서를 사용하는 전체 표면을 다음 제품으로 바꾼다.
            else { target.NextFinish(); }
            // 연결 오류가 있는 표면을 숨기지 않도록 실패 이유를 모은다.
            if (target.GetStatusMessage() != "") { errors += target.GetStatusMessage() + "\n"; }
        }
        // 표면마다 계산하지 않고, 전체 변경이 끝난 뒤 AnalysisManager와 UIManager에 한 번 요청한다.
        OnFinishChanged();
        ShowTarget(clickedTarget);
        // 부분 적용을 정상 완료로 오해하지 않도록 실패가 있을 때 추가 안내한다.
        if (errors != "") { ShowMessage(errors); }
    }
    // 현재 Room을 계산 클래스에 전달하고 결과를 UI에 갱신한다.
    public void OnFinishChanged()
    {
        // 연결이 없으면 계산과 표시를 호출하지 않는다.
        if (!HasRequiredReferences()) { return; }
        // 전체 계산을 중복 구현하지 않고 AnalysisManager에 요청한다.
        analysisManager.CalculateAll(roomController.GetCurrentRoomData(), roomController.GetCurrentFinishTargets());
        RefreshUI();
    }
    // 방을 제거하고 추가 선택과 계산값을 초기 상태로 돌려 Room 선택 화면을 표시한다.
    public void ResetProject()
    {
        // 연결이 없으면 초기화를 안전하게 수행할 수 없어 중단한다.
        if (!HasRequiredReferences()) { return; }
        SetState(IFEAState.RoomSelection);
        roomController.ClearRoom();
        analysisManager.CalculateAll(null, null);
        uiManager.ShowUIRoomSelect();
        RefreshUI();
        uiManager.ShowMessage("");
    }
    // 최종 선택을 다시 계산한 뒤 결과 화면으로 전달하고 3D 조작을 막는다.
    public void ShowResult()
    {
        // 연결되지 않은 클래스 호출을 막는다.
        if (!HasRequiredReferences()) { return; }
        // Room 선택 전에는 구성 결과가 없으므로 안내한다.
        if (roomController.GetCurrentRoom() == null)
        {
            ShowMessage("Please select a room first.");
            return;
        }
        // 결과 패널이 없을 때 기존 화면을 숨겨버리지 않도록 먼저 검사한다.
        if (!uiManager.HasResultUI())
        {
            ShowMessage("Connect Result UI and Result Text in UIManager.");
            return;
        }
        OnFinishChanged();
        SetState(IFEAState.Result);
        // 화면 내용 구성은 UIManager의 책임이므로 방 이름을 전달한다.
        uiManager.UpdateResult(roomController.GetCurrentRoomData().roomName);
        uiManager.ShowUIResult();
    }
    // UI-06의 돌아가기 버튼에서 기존 Room 조작을 다시 시작한다.
    public void ReturnToRoom()
    {
        // 방이 없거나 연결이 빠지면 조작을 재개할 수 없어 중단한다.
        if (!HasRequiredReferences() || roomController.GetCurrentRoom() == null) { return; }
        uiManager.ShowRoomUI();
        RefreshUI();
        SetState(IFEAState.RoomInteraction);
    }
    // 견적/분석 버튼에서 전체 화면을 열고 마우스로 UI를 누를 수 있도록 이동 입력을 막는다.
    public void ShowEstimateAnalysis()
    {
        // 방 선택이 끝나야 표시할 선택 내용이 있으므로 연결과 방 존재 여부를 검사한다.
        if (!HasRequiredReferences() || roomController.GetCurrentRoom() == null)
        {
            ShowMessage("Please select a room first.");
            return;
        }
        // 최신 선택을 계산에 반영하고 UI에 전달하도록 전체 갱신 함수를 호출한다.
        OnFinishChanged();
        SetState(IFEAState.EstimateAnalysis);
        uiManager.ShowUIEstimateAndAnalysis();
    }
    // 현재 화면 상태에 따라 ESC 입력으로 메뉴를 열거나 방 화면으로 돌아간다.
    public void HandleEscapeInput()
    {
        if (currentState == IFEAState.RoomInteraction)
        {
            ShowEstimateAnalysis();
        }
        else if (currentState == IFEAState.EstimateAnalysis || currentState == IFEAState.Result)
        {
            ReturnToRoom();
        }
    }
    // target의 제품을 UIManager에 전달한다. 대상이 없으면 툴팁을 숨긴다.
    public void ShowTarget(FinishTarget target)
    {
        // Player의 Awake 시 UI 연결이 없어도 오류를 막는다.
        if (uiManager == null) { return; }
        // 중앙에 마감 대상이 없으면 이전 제품 설명을 숨긴다.
        if (target == null) { uiManager.HideTooltip(); return; }
        // 사용자가 실제로 선택한 제품만 UI-03에 표시하기 위해 대상 데이터를 읽는다.
        FinishData finish = target.GetCurrentFinish();
        uiManager.ShowUITooltip(finish, target.GetFinishType());
    }
    // 실패 이유를 화면과 Console에 남겨 사용자가 연결 문제를 확인하도록 한다.
    public void ShowMessage(string message)
    {
        // UI가 있으면 화면으로 안내한다.
        if (uiManager != null) { uiManager.ShowMessage(message); }
        // 빈 문자열은 정상 상태이므로 경고를 남기지 않는다.
        if (!string.IsNullOrEmpty(message)) { Debug.LogWarning(message, this); }
    }
    // 값이 바뀐 시점에만 UI에 견적/분석/누락 안내를 갱신하도록 요청한다.
    private void RefreshUI()
    {
        // UIManager가 AnalysisManager를 직접 참조하지 않도록 계산 결과를 중재해 전달한다.
        uiManager.SetAnalysisResult(analysisManager.GetResult());
        uiManager.UpdateEstimate();
        uiManager.UpdateAnalysis();
        uiManager.ShowMessage(analysisManager.GetStatusMessage());
    }
    // Player가 bool의 의미를 추측하지 않고 현재 애플리케이션 상태로 입력 여부를 판단하게 한다.
    private void SetState(IFEAState state)
    {
        currentState = state;
        if (playerInteraction != null) { playerInteraction.SetState(state); }
    }
    // 네 담당 클래스가 연결되어야 전체 흐름을 실행할 수 있으므로 연결 여부를 반환한다.
    private bool HasRequiredReferences()
    {
        // 누락된 참조를 호출하면 null 오류가 나므로 안내하고 false를 반환한다.
        if (roomController == null || playerInteraction == null || analysisManager == null || uiManager == null)
        {
            ShowMessage("Connect RoomController, PlayerInteraction, AnalysisManager and UIManager in IFEAManager.");
            return false;
        }
        return true;
    }
}
