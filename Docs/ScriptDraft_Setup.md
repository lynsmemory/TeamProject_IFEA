# IFEA 스크립트 초안 설정 가이드

SRS의 FR-01~09와 클래스 다이어그램을 기준으로 만든 첫 통합용 초안이다.

## 1. 데이터 에셋

`Assets > Create > IFEA > Data`에서 다음 에셋을 만든다.

1. `Room` 3개: 원룸, 투룸, 쓰리룸의 ID, 면적, 체적, Prefab을 입력한다.
2. `Finish`: 벽, 바닥, 천장, 조명, 블라인드, 커튼 제품별로 만든다.
3. `Energy Standard`: 온도차, 연간 운전시간, 냉난방 효율, 전력 단가, 기준 U값을 입력한다.

열저항 단위는 **m²K/W**, 가격 단위는 **원**으로 통일한다.

### 시험 열 데이터 가져오기

`Docs/Testdata/ThermalDataCatalog.csv`에는 Ceiling, Floor, Wall별 2개 제품의 제조사 선언 열저항이 정리되어 있다.

1. Unity 메뉴에서 `Tools > IFEA > Import Thermal Test Data`를 실행한다.
2. 생성된 에셋은 `Assets/Data/FinishData/TestData`에서 확인한다.
3. PDF에 가격 정보가 없으므로 단가와 시공비는 별도로 입력한다.
4. 제품 이미지와 Material도 프로젝트 에셋을 준비한 뒤 각 FinishData에 연결한다.

열성능 계산에는 제조사가 선언한 열저항을 직접 사용한다.

## 2. Room Prefab

- Player 시작 위치에 빈 오브젝트를 만들고 `RoomSpawnPoint`를 추가한다.
- 변경할 벽, 바닥, 천장 또는 설비 오브젝트에 Collider와 `FinishTarget`을 추가한다.
- `FinishTarget`의 Type, Renderer, 면적/수량, 선택 가능한 FinishData 목록을 연결한다.
- 조명, 블라인드, 커튼처럼 교체 모델이 필요한 제품은 FinishData의 Visual Prefab을 사용한다.

## 3. Scene

- Player에 `CharacterController`, `FirstPersonController`, `PlayerInteraction`을 추가한다.
- 빈 오브젝트에 `RoomController`, `AnalysisManager`, `IFEAManager`를 추가한다.
- Canvas에 `UIManager`를 추가하고 패널과 Text/Image 참조를 연결한다.
- EventSystem은 Input System UI Input Module을 사용한다.
- Room 선택 버튼은 각각 `IFEAManager.StartOneRoom`, `StartTwoRoom`, `StartThreeRoom`에 연결한다.
- 초기화 버튼은 `IFEAManager.ResetProject`, 결과 버튼은 `IFEAManager.ShowResult`에 연결한다.

## 4. 현재 상호작용

- WASD: 이동
- 마우스: 시점 회전
- Escape: 커서 잠금 전환
- 마감 대상 좌클릭: 다음 제품 적용
- 마감 대상 우클릭: 기본 상태로 제거/초기화

## 5. 계산 초안의 전제

- 견적 = `(제품 단가 + 시공비) × 면적 또는 수량`
- 열저항 = `FinishData에 저장된 제조사 선언값`
- 적용 후 열관류율 = `1 / (기준 외피 열저항 + 마감재 열저항)`
- 연간 에너지 = `U값 × 외피면적 × 온도차 × 연간운전시간 / 1000 / 효율`
- 투자 회수기간 = `총 견적 / 기준안 대비 연간 절감액`

이는 SRS의 “단순화된 계산식 기반 추정값”에 맞춘 초안이다. 팀에서 기준 외피 구성과 월별 계산식을 확정하면 `AnalysisManager`의 계산식만 교체한다.
