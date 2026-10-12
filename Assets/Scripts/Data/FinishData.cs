using UnityEngine;

// 마감을 적용할 부위를 구분하여 잘못된 종류의 제품 적용을 막는다.
public enum FinishType
{
    // 기존 에셋의 분류 숫자가 바뀌지 않도록 각 값을 지정한다. 조명에 쓰던 3은 사용하지 않는다.
    Wall = 0,
    Floor = 1,
    Ceiling = 2,
    Blind = 4,
    Curtain = 5,
    // 블라인드와 커튼 중 하나를 같은 창에서 순환 선택하는 화면 카테고리이다.
    WindowCovering = 6
}

// 제품 설정을 여러 대상에서 공유한다. 실행 중 선택 상태는 FinishTarget에 보관한다.
[CreateAssetMenu(fileName = "NewFinishData", menuName = "IFEA/Data/Finish")]
public class FinishData : ScriptableObject
{
    // CSV 제품과 에셋을 구분하는 고유 이름이다.
    public string productId;
    // 툴팁과 견적 목록에 표시할 제품명이다.
    public string productName;
    // FinishTarget이 제품과 대상의 부위가 같은지 검사하기 위한 분류이다.
    public FinishType type;
    // UIManager가 제품 사진을 표시할 때 사용하는 Sprite이다.
    public Sprite productImage;
    // USD/m² 또는 USD/개 단가이다. -1은 가격 자료가 없다는 뜻이다.
    public float unitPrice = -1f;
    // 기존 수집 시공비를 보관한다. 현재 제품 견적과 회수기간에는 포함하지 않으며 -1은 미입력이다.
    public float installationCost = -1f;
    // 제조사 선언 열저항(m²K/W)이다. -1은 자료 없음이고 0과 구분한다.
    public float thermalResistance = -1f;
    // 열저항의 출처와 시험 조건을 확인하기 위해 보관한다.
    public string thermalSource;
    // FinishTarget이 벽/바닥/천장의 외관을 교체할 때 적용할 재질이다.
    public Material material;
    // 원본 텍스처 누락처럼 제품 외관을 확인할 때 필요한 안내를 UIManager에 전달한다.
    public string appearanceNote;
    // FinishTarget이 블라인드와 커튼의 실제 표시 모델을 생성하도록 전달하는 Prefab이다.
    public GameObject visualPrefab;

    // UIManager가 제품 이름을 표시하도록 저장값을 전달한다.
    public string GetProductName() { return productName; }
    // UIManager가 사진을 표시하도록 Sprite 참조를 전달한다.
    public Sprite GetProductImage() { return productImage; }
    // AnalysisManager가 제품 비용을 계산하도록 단가를 전달한다.
    public float GetUnitPrice() { return unitPrice; }
    // 기존 시공비 자료를 확인할 때 저장값을 읽도록 제공한다. 현재 견적에는 사용하지 않는다.
    public float GetInstallationCost() { return installationCost; }
    // FinishTarget이 외관을 바꾸도록 적용 재질을 전달한다.
    public Material GetMaterial() { return material; }
    // AnalysisManager가 두께로 다시 계산하지 않고 제조사 선언값을 사용하도록 반환한다.
    public float GetThermalResistance() { return thermalResistance; }
    // 제품 가격만 계산하므로 단가의 입력 여부를 검사한다. 보관한 시공비는 유효성 조건에 넣지 않는다.
    public bool HasPriceData() { return unitPrice >= 0f; }
    // 미입력 열저항을 개선값으로 계산하지 않도록 유효 여부를 반환한다.
    public bool HasThermalData() { return thermalResistance >= 0f; }
}
