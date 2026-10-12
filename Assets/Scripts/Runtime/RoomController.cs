using UnityEngine;

// 선택한 방 Prefab 생성/제거와 Player의 시작 위치 배치를 담당한다.
public class RoomController : MonoBehaviour
{
    // 원룸 모델과 분석 면적을 함께 읽기 위해 RoomData를 참조한다.
    [SerializeField] private RoomData oneRoomData;
    // 투룸 선택 시 사용할 Prefab과 분석 면적을 보관한 데이터이다.
    [SerializeField] private RoomData twoRoomData;
    // 쓰리룸 선택 시 사용할 Prefab과 분석 면적을 보관한 데이터이다.
    [SerializeField] private RoomData threeRoomData;
    // 생성된 방과 Player를 지정 위치에 놓기 위한 부모 오브젝트이다. 비워도 된다.
    [SerializeField] private Transform roomRoot;
    // RoomSpawnPoint 위치로 이동시킬 Player의 Transform이다.
    [SerializeField] private Transform player;
    // 생성한 방을 나중에 제거하고 IFEAManager에 전달하기 위해 보관한다.
    private GameObject currentRoom;
    // 현재 방의 이름/외피 면적을 IFEAManager에 전달하는 데이터이다.
    private RoomData currentRoomData;
    // 방 생성 시 한 번 찾은 마감 대상들을 변경과 분석에서 재사용한다.
    private FinishTarget[] currentFinishTargets = new FinishTarget[0];
    // IFEAManager가 생성 실패를 사용자에게 안내하도록 이유를 보관한다.
    private string statusMessage = "";

    // 선택한 roomType의 데이터에서 Prefab을 생성하고 Player 시작 위치를 배치한다.
    public void LoadRoom(RoomType roomType)
    {
        statusMessage = "";
        // 요청한 방 종류에 맞는 데이터 참조를 선택하기 위한 변수이다.
        RoomData selectedData = null;
        // 방 버튼의 종류에 따라 읽어야 할 RoomData가 다르므로 분기한다.
        switch (roomType)
        {
            case RoomType.OneRoom: selectedData = oneRoomData; break;
            case RoomType.TwoRoom: selectedData = twoRoomData; break;
            case RoomType.ThreeRoom: selectedData = threeRoomData; break;
        }
        // 새 방 데이터가 없을 때 기존 방을 먼저 삭제하면 복구가 어려우므로 검사부터 한다.
        if (selectedData == null || selectedData.roomPrefab == null || selectedData.roomType != roomType)
        {
            statusMessage = "Check the selected RoomData type and Room Prefab reference.";
            return;
        }
        // Player가 없으면 시작 위치를 배치할 수 없으므로 방 생성도 중단한다.
        if (player == null)
        {
            statusMessage = "Connect Player in RoomController.";
            return;
        }
        ClearRoom();
        currentRoomData = selectedData;
        currentRoom = Instantiate(selectedData.roomPrefab, roomRoot);
        // 원본 이름을 써서 결과 화면 및 Hierarchy에서 선택한 방을 알아보기 쉽게 한다.
        currentRoom.name = selectedData.roomPrefab.name;
        // 클릭할 때마다 Hierarchy를 다시 탐색하지 않도록 생성 직후 한 번만 보관한다.
        currentFinishTargets = currentRoom.GetComponentsInChildren<FinishTarget>();
        SpawnPlayer();
        // 시작 위치가 잘못된 방을 플레이 가능 상태로 남기지 않도록 제거한다.
        if (statusMessage != "")
        {
            ClearRoom();
        }
    }

    // 생성된 방 안의 RoomSpawnPoint를 읽어 Player의 위치/방향을 설정한다.
    public void SpawnPlayer()
    {
        // 방 또는 Player가 없으면 컴포넌트를 찾을 수 없어 안내 후 중단한다.
        if (currentRoom == null || player == null)
        {
            statusMessage = "Room and Player are not ready.";
            return;
        }
        // 정확한 시작 지점을 얻기 위해 방의 RoomSpawnPoint 컴포넌트를 가져온다.
        RoomSpawnPoint[] spawnPoints = currentRoom.GetComponentsInChildren<RoomSpawnPoint>();
        // 임의 위치에 Player를 놓지 않도록 방마다 활성 시작 지점 하나를 요구한다.
        if (spawnPoints.Length != 1)
        {
            statusMessage = "Add exactly one active RoomSpawnPoint to the Room Prefab.";
            return;
        }
        player.position = spawnPoints[0].GetPosition();
        player.rotation = spawnPoints[0].GetRotation();
    }

    // 이전 방을 즉시 비활성화하고 제거하여 Collider가 새 방과 겹치지 않게 한다.
    public void ClearRoom()
    {
        // 처음 실행할 때는 제거할 방이 없으므로 존재하는 경우에만 제거한다.
        if (currentRoom != null)
        {
            currentRoom.SetActive(false);
            Destroy(currentRoom);
        }
        currentRoom = null;
        currentRoomData = null;
        currentFinishTargets = new FinishTarget[0];
    }
    // IFEAManager/AnalysisManager가 선택된 방의 마감 대상을 읽도록 반환한다.
    public GameObject GetCurrentRoom() { return currentRoom; }
    // AnalysisManager와 UIManager가 현재 방의 면적/이름을 읽도록 반환한다.
    public RoomData GetCurrentRoomData() { return currentRoomData; }
    // IFEAManager가 생성 시 보관한 마감 대상을 변경과 분석에 전달하도록 반환한다.
    public FinishTarget[] GetCurrentFinishTargets() { return currentFinishTargets; }
    // IFEAManager가 방 생성 실패 이유를 화면에 표시하도록 반환한다.
    public string GetStatusMessage() { return statusMessage; }
}
