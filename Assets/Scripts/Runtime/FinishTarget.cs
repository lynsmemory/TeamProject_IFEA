using System.Collections.Generic;
using UnityEngine;

// 마감 부위 한 곳의 제품 선택 상태와 재질 변경을 담당한다.
// 벽·바닥·천장 또는 창 가림 부위 하나의 제품 선택 상태를 관리한다.
// 벽·바닥·천장은 Renderer의 지정 재질 칸을 교체하고,
// 블라인드·커튼은 Visual Anchor 아래에 제품 프리팹을 생성한다.
// 방 전체 변경 범위는 IFEAManager가, 견적·에너지 계산은 AnalysisManager가 담당한다.
public class FinishTarget : MonoBehaviour
{
    // 이 대상에 적용할 제품의 종류를 구분한다.
    [SerializeField] private FinishType type;
    // FinishData의 재질을 실제 모델에 적용하기 위해 Renderer를 참조한다.
    [SerializeField] private Renderer targetRenderer;
    // 여러 재질 중 교체할 칸을 지정한다.
    [SerializeField] private int materialIndex;
    // 견적/분석에 사용할 면적(m²) 또는 수량(개)이다. -1은 미입력이다.
    [SerializeField] private float area = -1f;
    // 이 마감 조각 중 실제 분석 경계에 해당하는 면적(m²)이다. -1이면 기존 area를 사용한다.
    [SerializeField] private float thermalArea = -1f;
    // 외기에 접한 부위만 열손실 계산에 포함하기 위한 값이다.
    [SerializeField] private bool isThermalSurface;
    // 마감 뒤 노출되는 구조 패널은 외관만 함께 바꾸고 시공 면적을 중복 견적에 넣지 않도록 구분한다.
    [SerializeField] private bool includeInEstimate = true;
    // 같은 창의 Blind/Curtain 면적을 한 번만 계산하기 위한 창 ID이다.
    [SerializeField] private string windowId;
    // 기본 상태의 제품 설명을 툴팁에 보여주기 위한 데이터이다.
    [SerializeField] private FinishData defaultFinish;
    // 이 대상에서 선택할 제품 목록이다. 설정값은 FinishData에 보관한다.
    [SerializeField] private List<FinishData> finishList = new List<FinishData>();
    // -1은 추가 제품 없이 원래 재질을 사용하는 상태이다.
    private int currentIndex = -1;
    // 초기화할 때 원래 외관을 복원하기 위한 Material이다.
    private Material defaultMaterial;
    // 준비되지 않은 재질을 복원하지 않도록 초기화 성공 여부를 보관한다.
    private bool hasDefaultMaterial;
    // PlayerInteraction이 실패 이유를 IFEAManager에 전달하도록 보관한다.
    private string statusMessage = "";
    // 창 크기에 맞춘 위치와 크기로 새 블라인드/커튼 모델을 생성하기 위한 부모이다.
    [SerializeField] private Transform visualAnchor;
    // 제품 선택 전의 블라인드 표시를 복원하기 위한 기본 모델이다. 커튼은 기본 모델 없이 시작할 수 있다.
    [SerializeField] private GameObject defaultVisual;
    // 교체하거나 제거할 때 현재 생성된 제품 모델을 찾기 위해 보관한다.
    private GameObject currentVisual;

    // 방 생성 시 원래 재질을 저장하여 초기화 기능을 준비한다.
    private void Awake()
    {
        // 블라인드와 커튼은 재질 칸 대신 모델을 교체하므로 생성 위치 연결만 확인한다.
        if (IsWindowCovering())
        {
            // 생성 위치가 없으면 창에 모델을 배치할 수 없어 연결 안내를 남긴다.
            if (visualAnchor == null) 
            { 
                statusMessage = name + ": Connect Visual Anchor.";
            }
            return;
        }
        // Renderer가 없으면 재질을 읽을 수 없어 연결 안내를 남기고 중단한다.
        if (targetRenderer == null)
        {
            statusMessage = name + ": Connect Renderer.";
            return;
        }
        // 원본 에셋을 수정하지 않고 특정 재질 칸을 읽기 위한 배열이다.
        Material[] materials = targetRenderer.sharedMaterials;
        // 범위 밖 인덱스로 접근하면 오류가 나므로 준비를 중단한다.
        if (materialIndex < 0 || materialIndex >= materials.Length)
        {
            statusMessage = name + ": Material Index is out of range.";
            return;
        }
        defaultMaterial = materials[materialIndex];
        hasDefaultMaterial = true;
        // 화면의 기본 외관과 에너지 비교 기준을 같게 만들기 위해 현재 대상의 비교 제품을 가져온다.
        FinishData comparisonFinish = GetComparisonFinish();
        // 비교 제품과 재질이 모두 준비된 경우에만 원래 재질 대신 비교 제품 재질을 기본 외관으로 사용한다.
        if (comparisonFinish != null && comparisonFinish.GetMaterial() != null)
        {
            // Reset에서도 같은 비교 제품 외관으로 돌아가도록 기본 재질을 비교 제품 재질로 바꾼다.
            defaultMaterial = comparisonFinish.GetMaterial();
            materials[materialIndex] = defaultMaterial;
            targetRenderer.sharedMaterials = materials;
        }
    }

    // 목록의 다음 유효한 제품을 적용한다. 목록 끝에서는 처음 제품으로 돌아간다.
    public void NextFinish()
    {
        statusMessage = "";
        // 블라인드와 커튼은 제품 데이터의 모델을 생성하는 별도 흐름을 사용한다.
        if (IsWindowCovering())
        {
            NextVisual();
            return;
        }
        // 원본 재질이 준비되지 않았으면 초기화할 수 없어 변경도 막는다.
        if (!hasDefaultMaterial || targetRenderer == null)
        {
            statusMessage = name + ": Check Renderer and Material Index.";
            return;
        }
        // 목록이 없으면 다음 제품을 선택할 수 없어 안내한다.
        if (finishList == null || finishList.Count == 0)
        {
            statusMessage = name + ": No selectable FinishData.";
            return;
        }
        // 기본 화면에는 비교 제품의 재질이 이미 적용되어 있으므로 첫 클릭은 그 다음 제품부터 시작한다.
        int startIndex = currentIndex;
        if (startIndex < 0)
        {
            FinishData comparisonFinish = GetComparisonFinish();
            int comparisonIndex = finishList.IndexOf(comparisonFinish);
            if (comparisonIndex >= 0) { startIndex = comparisonIndex; }
        }
        // 목록을 한 바퀴만 검사하여 잘못된 제품 때문에 무한 반복하는 것을 막는다.
        for (int step = 1; step <= finishList.Count; step++)
        {
            // 이번 제품의 위치를 목록 크기 안에서 순환시키는 인덱스이다.
            int nextIndex = (startIndex + step) % finishList.Count;
            // 분류와 재질이 유효한지 확인하기 위해 해당 제품을 읽는다.
            FinishData finish = finishList[nextIndex];
            // 누락 제품/다른 종류/재질 미입력은 적용할 수 없어 다음 제품을 검사한다.
            if (finish == null || finish.type != type || finish.GetMaterial() == null)
            {
                continue;
            }
            // 원본 Material 에셋을 수정하지 않고 Renderer의 지정 칸만 바꾼다.
            Material[] materials = targetRenderer.sharedMaterials;
            // 재질 배열이 바뀌었을 수 있으므로 적용 직전에도 범위를 검사한다.
            if (materialIndex < 0 || materialIndex >= materials.Length)
            {
                statusMessage = name + ": No material slot available.";
                return;
            }
            // 제품에 지정한 재질을 가져와 현재 모델의 외관을 바꾼다.
            materials[materialIndex] = finish.GetMaterial();
            targetRenderer.sharedMaterials = materials;
            currentIndex = nextIndex;
            return;
        }
        statusMessage = name + ": No product with a matching category and Material.";
    }

    // 추가 제품을 제거하고 최초에 저장한 재질로 복원한다.
    public void ResetFinish()
    {
        statusMessage = "";
        // 창 장치는 생성한 제품을 제거하고 기존 모델 표시 상태를 복원한다.
        if (IsWindowCovering())
        {
            ClearVisual();
            // 기본 모델이 있는 블라인드만 다시 표시하고 커튼은 빈 상태로 돌아간다.
            if (defaultVisual != null) { defaultVisual.SetActive(true); }
            currentIndex = -1;
            return;
        }
        // 기본 재질을 확보한 대상만 Renderer 복원을 시도한다.
        if (hasDefaultMaterial && targetRenderer != null)
        {
            // 다른 재질 칸을 유지하면서 선택한 칸만 복원하기 위한 배열이다.
            Material[] materials = targetRenderer.sharedMaterials;
            // 잘못된 칸을 덮어쓰지 않도록 배열 범위를 확인한다.
            if (materialIndex < 0 || materialIndex >= materials.Length)
            {
                statusMessage = name + ": No slot for the default Material.";
                return;
            }
            materials[materialIndex] = defaultMaterial;
            targetRenderer.sharedMaterials = materials;
        }
        currentIndex = -1;
    }

    // 블라인드 또는 커튼의 다음 유효한 제품 모델을 창 위치에 생성한다.
    private void NextVisual()
    {
        // 생성 위치나 목록이 없으면 제품을 놓을 수 없어 안내한다.
        if (visualAnchor == null || finishList == null || finishList.Count == 0)
        {
            statusMessage = name + ": Connect Visual Anchor and FinishData.";
            return;
        }
        // 목록 한 바퀴 안에서 현재 종류와 모델이 준비된 제품을 찾는다.
        for (int step = 1; step <= finishList.Count; step++)
        {
            // 선택 순서를 목록 범위 안에서 순환시키기 위한 인덱스이다.
            int nextIndex = (currentIndex + step) % finishList.Count;
            // 모델과 제품 설명을 함께 읽기 위해 FinishData를 참조한다.
            FinishData finish = finishList[nextIndex];
            // 다른 종류나 모델 누락 데이터는 생성할 수 없어 건너뛴다.
            if (finish == null || !MatchesFinishType(finish) || finish.visualPrefab == null) { continue; }
            ClearVisual();
            // 기본 블라인드가 제품 모델과 겹치지 않도록 숨긴다.
            if (defaultVisual != null) { defaultVisual.SetActive(false); }
            currentVisual = Instantiate(finish.visualPrefab, visualAnchor);
            currentVisual.transform.localPosition = Vector3.zero;
            currentVisual.transform.localRotation = Quaternion.identity;
            currentIndex = nextIndex;
            return;
        }
        statusMessage = name + ": No matching product Visual Prefab.";
    }
    // 이전 제품 모델을 즉시 숨긴 뒤 제거해 한 프레임 동안 새 모델과 겹치지 않게 한다.
    private void ClearVisual()
    {
        // 처음 선택할 때는 생성 모델이 없으므로 존재할 때만 제거한다.
        if (currentVisual != null)
        {
            currentVisual.SetActive(false);
            Destroy(currentVisual);
            currentVisual = null;
        }
    }

    // UIManager/AnalysisManager가 현재 추가 선택 제품을 읽도록 반환한다.
    public FinishData GetCurrentFinish()
    {
        // 기본 상태나 잘못된 인덱스는 추가 제품 없음으로 처리하여 배열 오류를 막는다.
        if (finishList == null || currentIndex < 0 || currentIndex >= finishList.Count)
        {
            return null;
        }
        return finishList[currentIndex];
    }
    // AnalysisManager가 외피 기준값 대신 제품 목록 안의 비교 제품을 사용하도록 반환한다.
    public FinishData GetComparisonFinish()
    {
        // Inspector에 기본 제품이 연결되어 있고 대상 종류와 맞으면 그 제품을 비교 기준으로 사용한다.
        if (defaultFinish != null && MatchesFinishType(defaultFinish) && defaultFinish.GetThermalResistance() > 0f)
        {
            return defaultFinish;
        }
        // 기본 제품 연결 전에도 현재 제품 자료만으로 비교할 수 있도록 목록의 첫 유효 제품을 찾는다.
        if (finishList != null)
        {
            // 목록 순서는 Inspector에서 정한 비교 순서이므로 첫 제품을 기본 비교 제품으로 사용한다.
            foreach (FinishData finish in finishList)
            {
                // 종류가 다르거나 R값이 없는 제품은 열성능 비교 기준으로 사용할 수 없어 건너뛴다.
                if (finish == null || !MatchesFinishType(finish) || finish.GetThermalResistance() <= 0f) { continue; }
                return finish;
            }
        }
        // 사용할 수 있는 제품이 없으면 AnalysisManager가 누락 안내를 표시하도록 null을 반환한다.
        return null;
    }
    // PlayerInteraction이 화면에 보이는 비교 제품의 설명을 UI에 전달하도록 반환한다.
    public FinishData GetDefaultFinish() 
    {
        return GetComparisonFinish();
    }
    // AnalysisManager가 적용 면적 또는 수량을 사용하도록 반환한다.
    public float GetArea() 
    {
        return area;
    }
    // AnalysisManager가 내부 칸막이를 빼고 실제 경계 부분만 열손실에 반영하도록 면적을 반환한다.
    public float GetThermalArea()
    {
        // 이전 데이터나 창 대상에는 별도 열면적이 없을 수 있어 기존 면적으로 계산을 이어간다.
        if (thermalArea < 0f) 
        {
            return area;
        }
        return thermalArea;
    }
    // AnalysisManager가 적용 부위를 구분하도록 반환한다.
    public FinishType GetFinishType() 
    { 
        return type;
    }
    // AnalysisManager가 창 가림 카테고리와 기존 단일 장치 대상을 같은 창 분석 흐름으로 구분하도록 전달한다.
    public bool IsWindowCovering()
    {
        return type == FinishType.WindowCovering || type == FinishType.Blind || type == FinishType.Curtain;
    }
    // 통합 창 가림 대상은 블라인드와 커튼 제품을 모두 허용하고 일반 마감은 같은 종류만 허용한다.
    public bool MatchesFinishType(FinishData finish)
    {
        // 제품이 없으면 종류를 검사할 수 없어 적용 불가로 처리한다.
        if (finish == null) 
        {
            return false;
        }
        // 같은 창에 순환 적용할 두 제품 종류만 통합 목록에 허용한다.
        if (type == FinishType.WindowCovering)
        {
            return finish.type == FinishType.Blind || finish.type == FinishType.Curtain;
        }
        return finish.type == type;
    }
    // AnalysisManager가 외기 접촉 부위만 열 계산에 포함하도록 반환한다.
    public bool IsThermalSurface() { return isThermalSurface; }
    // AnalysisManager가 외관 보조 패널을 시공비 합산에서 제외하도록 전달한다.
    public bool IsEstimateSurface() { return includeInEstimate; }
    // AnalysisManager가 같은 창을 묶어 면적 중복을 막도록 반환한다.
    public string GetWindowId() { return windowId; }
    // PlayerInteraction이 실패 이유를 UI로 전달하도록 반환한다.
    public string GetStatusMessage() { return statusMessage; }
}
