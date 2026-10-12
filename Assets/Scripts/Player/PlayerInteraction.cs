using UnityEngine;
using UnityEngine.EventSystems;

// 강의의 Transform/Input/Raycast로 평평한 Room 이동과 중앙 마감 선택을 담당한다.
public class PlayerInteraction : MonoBehaviour
{
    // 화면 중앙에서 Raycast를 쏘고 상하 시점을 회전하기 위해 Camera를 참조한다.
    [SerializeField] private Camera playerCamera;
    // 선택 가능한 마감 대상까지의 최대 거리(m)이다.
    [SerializeField] private float interactionDistance = 3f;
    // Player가 1초에 이동할 거리(m)이다.
    [SerializeField] private float moveSpeed = 3f;
    // Mouse X/Y 입력을 시점 각도로 바꾸는 배율이다.
    [SerializeField] private float mouseSensitivity = 2f;
    // 마감 대상만 탐지하여 UI/Player Collider를 선택하지 않도록 지정한다.
    [SerializeField] private LayerMask interactionMask = -1;
    // 이동을 막는 벽/가구 Collider만 검사하기 위한 LayerMask이다.
    [SerializeField] private LayerMask collisionMask = -1;
    // 벽 바로 앞에서 몸이 겹치지 않도록 남길 거리(m)이다.
    [SerializeField] private float wallPadding = 0.25f;
    // 제품 변경 뒤 견적/분석 갱신과 툴팁 표시를 요청하기 위해 전체 흐름 Manager를 참조한다.
    [SerializeField] private IFEAManager ifeaManager;
    // 같은 대상을 바라보는 동안 툴팁을 매 프레임 다시 그리지 않도록 보관한다.
    private FinishTarget currentTarget;
    // IFEAManager가 전달한 현재 화면 흐름 상태이다.
    private IFEAState currentState = IFEAState.RoomSelection;
    // 위아래 회전각을 제한하여 Camera가 뒤집히지 않게 한다.
    private float pitch;

    // 시작 시 필요한 Camera/Manager 연결을 확인하고 입력은 Room 선택 전까지 막는다.
    private void Awake()
    {
        // Camera 없이는 시점과 Raycast가 불가능하므로 연결 오류를 알린다.
        if (playerCamera == null || ifeaManager == null)
        {
            Debug.LogError("Connect Camera and IFEAManager in PlayerInteraction.", this);
        }
        SetState(IFEAState.RoomSelection);
    }

    // 키보드/마우스 입력만 매 프레임 검사한다. 분석과 UI 전체 갱신은 Manager가 담당한다.
    private void Update()
    {
        // 전체 화면 흐름은 IFEAManager가 결정하므로 연결되지 않은 상태에서는 입력을 처리하지 않는다.
        if (ifeaManager == null) { return; }
        // 방 화면에서는 견적·에너지 메뉴를 열고, 열린 메뉴에서는 다시 방으로 돌아간다.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ifeaManager.HandleEscapeInput();
            return;
        }
        // 방 선택 전 또는 결과 화면에서는 3D 조작을 하지 않도록 중단한다.
        if (currentState != IFEAState.RoomInteraction || playerCamera == null)
        {
            return;
        }
        // UI 버튼 위에서 이동/회전/마감 클릭이 실행되지 않도록 조작을 막는다.
        if (IsPointerOverUI())
        {
            ClearTarget();
            return;
        }
        // 커서가 풀렸으면 이동을 멈추고 빈 3D 화면 클릭으로만 다시 잠글 수 있게 한다.
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            ClearTarget();
            // UI가 아닌 곳에서 좌클릭하면 시점 조작을 재개한다. 제품 변경은 다음 클릭부터 한다.
            if (Input.GetMouseButtonDown(0))
            {
                SetCursorLocked(true);
            }
            return;
        }
        Move();
        Look();
        DetectTarget();
        // 좌클릭은 다음 제품 선택을 요청한다.
        if (Input.GetMouseButtonDown(0)) { ChangeNextFinish(); }
        // 우클릭은 현재 부위의 추가 제품을 제거한다.
        if (Input.GetMouseButtonDown(1)) { ResetFinish(); }
    }

    // WASD 입력을 수평 이동으로 바꾸고 이동 방향의 벽/가구를 Raycast로 검사한다.
    private void Move()
    {
        // 대각선 이동도 같은 속도가 되도록 방향을 정규화한다.
        Vector3 direction = transform.right * Input.GetAxisRaw("Horizontal")
            + transform.forward * Input.GetAxisRaw("Vertical");
        direction.y = 0f;
        direction = direction.normalized;
        // 이번 프레임에 이동할 거리이다. 프레임 수와 관계없이 일정 속도로 움직인다.
        float distance = Mathf.Max(0f, moveSpeed) * Time.deltaTime;
        // 입력이 없으면 불필요한 충돌 검사를 생략한다.
        if (direction.sqrMagnitude == 0f) { return; }
        // 발/몸통/눈높이에서 검사하여 낮은 가구와 벽을 지나가지 않도록 한다.
        for (int heightIndex = 0; heightIndex < 3; heightIndex++)
        {
            // Player 발 위치를 기준으로 검사할 높이이다.
            Vector3 origin = transform.position + Vector3.up * (0.3f + heightIndex * 0.6f);
            // 이동 거리와 몸 여유 거리 안에 Collider가 있으면 그 방향으로 이동하지 않는다.
            if (Physics.Raycast(origin, direction, distance + Mathf.Max(0f, wallPadding), collisionMask))
            {
                return;
            }
        }
        transform.position += direction * distance;
    }

    // 마우스 입력으로 몸은 좌우, Camera는 상하로 회전시킨다.
    private void Look()
    {
        // 현재 프레임의 수평 시점 변화량이다.
        float yaw = Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -80f, 80f);
        transform.Rotate(0f, yaw, 0f);
        playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    // 화면 중앙에서 가장 먼저 맞은 Collider의 FinishTarget을 찾아 툴팁에 전달한다.
    private void DetectTarget()
    {
        // 이번 프레임에 발견한 마감 대상이다.
        FinishTarget nextTarget = null;

        // 화면 중앙 기준으로 정면 광선을 만든다.
        Ray ray = playerCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        // 광선에 맞은 모든 Collider를 가져온다.
        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            Mathf.Max(0f, interactionDistance),
            interactionMask
        );

        // 가까운 Collider부터 검사한다.
        System.Array.Sort(
            hits,
            (left, right) => left.distance.CompareTo(right.distance)
        );

        // FinishTarget이 없는 충돌용 벽은 건너뛴다.
        foreach (RaycastHit hit in hits)
        {
            FinishTarget target =
                hit.collider.GetComponentInParent<FinishTarget>();

            if (target != null)
            {
                nextTarget = target;
                break;
            }
        }

        // 같은 대상이면 UI를 다시 갱신하지 않는다.
        if (nextTarget == currentTarget) { return; }

        currentTarget = nextTarget;

        // 탐지한 대상을 UI 흐름에 전달한다.
        ifeaManager.ShowTarget(currentTarget);
    }

    // 탐지한 부위의 종류 전체를 다음 제품으로 변경하도록 전체 흐름 Manager에 요청한다.
    private void ChangeNextFinish()
    {
        // 마감 대상이 없으면 아무 제품도 변경하지 않는다.
        if (currentTarget == null) { return; }
        // 개별 표면이 아니라 같은 종류 전체를 변경해야 하므로 IFEAManager에 범위 처리를 맡긴다.
        ifeaManager.SelectNextFinish(currentTarget);
    }
    // 탐지한 부위와 같은 종류 전체의 기본 재질 복원을 Manager에 요청한다.
    private void ResetFinish()
    {
        // 대상이 없으면 초기화할 부위가 없으므로 중단한다.
        if (currentTarget == null) { return; }
        // 같은 종류의 복원과 재계산이 한 번에 이루어지도록 IFEAManager를 호출한다.
        ifeaManager.ResetFinishSelection(currentTarget);
    }
    // Room 선택/결과 전환 시 3D 조작 허용 상태와 커서 상태를 함께 바꾼다.
    public void SetState(IFEAState state)
    {
        currentState = state;
        ClearTarget();
        SetCursorLocked(state == IFEAState.RoomInteraction);
    }
    // 다른 방으로 이동할 때 이전 방의 상하 시점 각도를 제거한다.
    public void ResetViewRotation()
    {
        pitch = 0f;
        // Camera가 연결된 경우에만 시점을 초기화하여 null 오류를 막는다.
        if (playerCamera != null) { playerCamera.transform.localRotation = Quaternion.identity; }
    }
    // UI 포인터 검사로 화면 버튼 입력과 3D 입력이 동시에 실행되는 것을 막는다.
    private bool IsPointerOverUI()
    {
        // 잠긴 커서의 이전 UI 위치가 룸 조작을 막지 않도록 실제 UI를 누르는 해제 상태에서만 검사한다.
        return Cursor.lockState != CursorLockMode.Locked
            && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
    // 툴팁을 남기지 않도록 현재 대상을 지우고 Manager에 숨김을 요청한다.
    private void ClearTarget()
    {
        currentTarget = null;
        // 초기 Awake 시 Manager가 없어도 오류 없이 연결을 기다린다.
        if (ifeaManager != null) { ifeaManager.ShowTarget(null); }
    }
    // 잠금 상태를 바꿔 3D 조작 중에는 커서를 숨기고 UI 조작 중에는 표시한다.
    private void SetCursorLocked(bool locked)
    {
        // 입력 허용 상태에 따라 Unity의 잠금 모드를 선택한다.
        if (locked) { Cursor.lockState = CursorLockMode.Locked; }
        else { Cursor.lockState = CursorLockMode.None; }
        Cursor.visible = !locked;
    }
    // Player가 꺼질 때 커서가 계속 잠겨 UI를 못 누르는 상황을 막는다.
    private void OnDisable() { SetCursorLocked(false); }
}
