# 원룸 모델링

Unity trillion_IFCE 프로젝트에 이미지 기반 원룸 모델을 저장했습니다. 컨피규레이터 컴포넌트, 교체 UI, 런타임 교체 스크립트는 제거했습니다.

장면: Assets/TrillionStudio/Scenes/StudioApartment.unity
프리팹: Assets/TrillionStudio/Prefabs/StudioApartment.prefab

## 계층
StudioApartment_4.6m_x_9.0m
  00_Structure
  01_MainRoom
    01_Finishes / Walls_Independent_MainRoom / Floor_Independent_MainRoom / Ceiling_Independent_MainRoom
    02_Fixed_Trim
    03_Furniture / Bed / Window_Lounge / Media_Console
    04_Kitchen_Fixed_Furniture
    05_Storage_Entry
    06_Doors_and_Windows
    07_Lighting
    08_Blinds
  02_Bathroom
    01_Finishes / Walls_Independent_Bathroom / Floor_Independent_Bathroom / Ceiling_Independent_Bathroom
    02_Sanitary_Fixtures / Vanity / Toilet / Shower_Enclosure
    03_Doors_and_Windows
    07_Lighting
    08_Blinds

방과 화장실의 마감 메시와 조명·블라인드 모델은 각각 별도의 부모 그룹에 있습니다. 사용자가 이후 오브젝트와 교체 스크립트를 직접 연결할 수 있습니다.

프리팹에는 전체 외벽, 창문, 천장이 포함되어 있습니다. 편집 장면에서만 내부 확인을 위해 전면/동측의 Renderer와 천장 그룹을 숨겼습니다. 전체 외관이 필요하면 프리팹을 새 장면에 배치하세요.

단위 1 = 1m. 외곽 4.6 × 9.0m, 천장 2.65m는 사진 비율을 기반으로 한 추정치입니다. 실측 도면으로 치수를 보정해야 합니다.

실제 bevel 메시, 이불 주름, 베개, 수전, 손잡이, 싱크, 샤워 유리, 배수구, 창틀, 블라인드 등 세부 부품을 분리했습니다. 목재·타일·직물·석재 텍스처는 로컬 절차 생성입니다. 실시간 조명이며 베이크는 하지 않았습니다. 거울은 환경 반사 재질이며 평면 반사는 아닙니다.

모델 계층 오브젝트: 545. 루트와 자식에 사용자 동작 스크립트 없음.

전면 벽·창문·롤러 블라인드 표시 완료. 창문 전체 약 4.06m × 2.02m, 4분할 슬라이딩 창틀·유리·손잡이·창턱이 별도 자식 모델입니다. 블라인드는 상단 레일·직물·하단 무게추·줄로 분리했습니다. 전면 벽은 하부 벽·양쪽 기둥·상부 벽·실내 마감으로 분리됩니다.

주방과 TV 뒤쪽 동측 벽·실내 벽지 마감·걸레받이를 표시하고 저장했습니다.

방·주방·현관 천장과 화장실 천장을 표시했습니다. 메인룸과 화장실 천장 부모가 독립되어 별도로 교체할 수 있습니다.

천장 등기구 5개를 본체·테두리·발광 커버·Light 부모 자식 구조로 정리했습니다. 방 3개, 화장실 2개이며 기존 펜던트도 유지합니다.

천장 등기구 5개를 본체·테두리·발광 커버·Light 부모 자식 구조로 정리했습니다. 방 3개, 화장실 2개이며 기존 펜던트도 유지합니다.

가정용 밀착형 LED 천장등으로 변경: 거실 방등, 긴 주방등, 현관등, 화장실 방습 커버형 등. 각각 본체·커버·Light를 자식으로 분리했습니다.
