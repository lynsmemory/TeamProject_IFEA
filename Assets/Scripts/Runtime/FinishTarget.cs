using UnityEngine;

public sealed class FinishTarget : MonoBehaviour
{
    // 마감재 적용 대상
    // 변수
    // - type: FinishType
    // - targetRenderer: Renderer
    // - area: float
    // - currentIndex: int
    // - finishList: List<FinishData>

    // 함수
    // + NextFinish(): void
    // + ResetFinish(): void
    // + GetCurrentFinish(): FinishData
    // + GetDefaultFinish(): FinishData
    // + GetArea(): float
}
