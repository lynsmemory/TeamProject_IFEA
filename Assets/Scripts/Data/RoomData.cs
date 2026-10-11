using UnityEngine;

// Room 선택 버튼이 원룸/투룸/쓰리룸을 구분하기 위한 값이다.
public enum RoomType
{
    OneRoom,
    TwoRoom,
    ThreeRoom
}

// RoomController의 방 생성과 AnalysisManager의 외피 면적 계산에 필요한 데이터를 공유한다.
[CreateAssetMenu(fileName = "NewRoomData", menuName = "IFEA/Data/Room")]
public class RoomData : ScriptableObject
{
    // 중복 방 데이터를 구분하는 고유 ID이다.
    public string roomId;
    // 결과 화면에 표시할 방 이름이다.
    public string roomName;
    // 선택 버튼이 요청한 방 종류와 이 데이터가 같은지 검사한다.
    public RoomType roomType;
    // RoomController가 Instantiate로 생성할 전체 방 모델이다.
    public GameObject roomPrefab;
    // 난방 열손실에 포함하는 분석 경계 벽 면적(m²)이다. 욕실, 화장실, 발코니 바닥 범위와 실내 칸막이는 제외한다.
    public float wallArea = -1f;
    // 욕실, 화장실, 발코니를 제외하고 열손실 계산에 포함하는 바닥 면적이다. -1은 미입력, 0은 계산 대상 없음이다.
    public float floorArea = -1f;
    // 욕실, 화장실, 발코니를 제외하고 열손실 계산에 포함하는 천장 면적이다.
    public float ceilingArea = -1f;
    // 실제 창의 총 면적이다. 같은 창의 Blind/Curtain 면적을 중복해서 넣지 않는다.
    public float windowArea = -1f;
    // 방 체적(m³)이다. 현재 외피 열손실 식에는 사용하지 않고 자료로 보관한다.
    public float volume = -1f;

    // 면적 누락 및 외피 면적이 전부 0인 방의 분석을 막기 위해 유효 여부를 반환한다.
    public bool HasAreaData()
    {
        return wallArea >= 0f && floorArea >= 0f && ceilingArea >= 0f
            && windowArea >= 0f && wallArea + floorArea + ceilingArea + windowArea > 0f;
    }
}
