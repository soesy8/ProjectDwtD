# 젖은 복도 — 이야기형 프로토타입

2026-10-18 기획을 반영한 첫 스테이지입니다. 친구와 교실에 가려는 상황에서 주변을 관찰하고, 속마음 말풍선으로 단서를 이해한 뒤 신호를 보냅니다. 선택의 결과를 확인하고 안전한 행동을 실천하면 칭찬 도장 1개를 받습니다.

## 현재 씬 구성 — 2026-10-19 오후 콘셉트 반영

`WetCorridor_Test.unity`를 새 복도 콘셉트의 구도로 재구성했습니다. 중앙의 친구를 가까이 배치하고, 왼쪽에 큰 물웅덩이와 높은 창문, 오른쪽에 교실 문·게시판을 배치했습니다. 오른쪽 우산과 우산꽂이는 제거하고 물웅덩이 뒤에 양동이와 대걸레를 배치했습니다. 친구는 가방끈을 쥔 자세와 반바지로 바뀌었으며, 크림·민트·청록·주황 팔레트와 셀 명암을 유지합니다.

- 왼쪽 위에는 큰 `속마음` 구름, 오른쪽 아래에는 두 손 아이콘의 `행동 선택` 버튼을 표시합니다. 기본 상황 안내판은 숨기고, 수첩은 보상·완료 때 표시합니다. 선택지와 보상 UI가 열리면 전경 손을 숨깁니다.
- 물웅덩이 뒤의 `Cleaning_Tools`에 속이 빈 청록 양동이와 걸레 술이 달린 대걸레를 배치했습니다. 양동이는 물웅덩이 뒤에 두고 대걸레는 왼쪽 벽의 창문 사이에 기대어 배치했습니다. 각각 레이저로 살펴보면 청소 중인 상황을 설명하며, 행동이나 도장 획득으로 이어지지 않습니다. 물기·친구 관찰과 선택·재시도 흐름은 그대로 작동합니다.
- `CorridorComposition`이 친구의 시작점, 물기, 안전·위험 이동 위치를 공유합니다. 새 구도에 맞춰 실제 관찰 콜라이더와 마른 경로도 이동했습니다.
- `CorridorWorldPanel`은 화면 비율이 바뀔 때 초기 위치를 기준으로 UI를 다시 배치합니다. 고개를 돌려 조준할 때는 버튼이 움직이지 않습니다. 속마음만 시선을 따라갑니다.
- `CorridorFirstPersonHands`는 데스크톱 구도용 손입니다. XR이 활성화되면 숨기며 실제 컨트롤러 추적과 구분합니다.
- 물웅덩이의 창문 반사는 `IllustratedWater.shader`로 그린 고정된 색면입니다. 실시간 반사 카메라는 사용하지 않습니다.
- 새 환경은 `Reference_Corridor`에 저장했습니다. 이전 `SchoolCorridor`, `Storybook_Art`는 비활성 상태로 보존했습니다. 새 자산은 `Assets/Test/ReferenceArt`에 있습니다.

실제 Unity 화면: [기본 구도](Reference_Composition.png), [청소 도구 관찰](Reference_CleaningTools.png), [행동 선택](Reference_Choices.png), [마른 길](Reference_DryPath.png), [도착·보상](Reference_Reward.png).

```powershell
# 현재 씬에서 Play 종료 후 새 배경과 구도를 재생성하고 저장
unity command run_script --file Test/Tools/BuildReferenceCorridor.cs --entry BuildReferenceCorridor.Run --timeout_ms 55000 --caller plugin --skill unity-scene-setup --json
# 새 Play 세션에서 청소 도구 관찰·화면 범위·레이저·도착 구도 검사 및 캡처
unity command run_script --file Test/Tools/ReviewReferenceCorridor.cs --entry ReviewReferenceCorridor.Run --timeout_ms 55000 --caller plugin --skill ui-ugui --json
```

새 구도와 청소 도구의 검증 결과는 `ReferenceReview.json`에 기록합니다. 앞선 UI·레이저 29개와 이야기 흐름 70개 항목의 결과는 `UIReview.json`, `StoryValidation.json`에 있습니다. 16:9·16:10·4:3에서 버튼 범위와 말풍선 겹침, 실제 중앙 레이저 선택, 재활성화 후 선·점 복원, 안전 이동과 두 위험 분기·재시도·도장을 확인했습니다. 물리 헤드셋·컨트롤러와 플레이어 빌드는 별도 검증이 필요합니다.

아래 두 아트·UI 항목은 초기 개선 기록입니다. 현재 구도와 재생성 도구는 위 항목을 기준으로 합니다.

## 초기 아트 개선 — 2026-10-19

최신 기획서와 복도·주방·보행로 콘셉트 아트를 참고해, 현재 젖은 복도 씬을 크림색·민트·청록 배경과 주황색 캐릭터의 3D 카툰 스타일로 개선했습니다. 이 씬을 열고 Play하면 적용된 아트를 바로 볼 수 있습니다.

- `Assets/Test/Shaders/StorybookToon.shader`: 3단계 셀 명암, 갈색 실루엣 윤곽선, 그림자 생성·수신. 머티리얼에서 색상, 명암 경계, 선 두께를 조정할 수 있습니다. XR 인스턴싱·스테레오 매크로를 포함합니다.
- `Assets/Test/Art/Materials`, `Meshes`: 씬이 직접 참조하는 머티리얼과 메시입니다. Play 때 환경을 새로 생성하거나 외부 이미지를 불러올 필요가 없습니다.
- 친구의 큰 머리·짧은 팔다리, 앞머리·볼·눈동자·웃는 입, 후드 주머니와 신발을 정리했습니다. 기존 팔·다리 움직임과 놀란 표정 참조를 유지합니다.
- 둥근 사물함, 청록 조명 프레임, 창밖 하늘·나뭇잎, 화분, 타일 줄눈과 큰 햇빛 형태를 추가했습니다. 물기는 이어진 불규칙 실루엣·푸른 테두리·반짝임으로 표현합니다.
- 말풍선과 버튼의 색을 종이·갈색 선으로 통일하고 기존 정적 한글 아틀라스를 공유하는 `StorybookKoreanInk` 머티리얼로 글자를 보강했습니다. 기본 아트 윤곽선과 관찰 시 금색 강조는 별개입니다.
- `Test/Art_Observation.png`, `Art_Environment.png`, `Art_Character.png`, `Art_Title.png`는 실제 Unity 렌더링 미리보기입니다. 캐릭터 확대 컷과 환경 컷은 아트 확인용으로 UI를 숨겨 촬영했습니다.

에디터 재적용 도구는 `Test/Tools/BuildStorybookArt.cs`, 화면·자산 확인 도구는 `Test/Tools/ReviewStorybookArt.cs`에 있습니다. 재적용은 현재 씬을 저장하므로 별도 아트 편집 후에는 아래 명령을 실행할 필요가 없습니다.

```powershell
# WetCorridor_Test 씬을 열고 Play를 종료한 상태
unity command run_script --file Test/Tools/BuildStorybookArt.cs --entry BuildStorybookArt.Run --timeout_ms 55000 --project-path D:\project\ProjectWait --json
# Play를 시작한 직후 아트 미리보기와 검사 결과 저장
unity command run_script --file Test/Tools/ReviewStorybookArt.cs --entry ReviewStorybookArt.Run --project-path D:\project\ProjectWait --json
```

2026-10-19 데스크톱 통합 검증 72개 항목을 통과했습니다(`StoryValidation.json`). 셰이더 오류·누락 메시·잘못된 머티리얼 참조는 없었습니다(`ArtReview.json`). 실제 VR 헤드셋의 양안 표현·착용 가독성·기기 성능과 Android 플레이어 빌드는 아직 검증하지 않았습니다.

## 초기 UI 디자인 개선 — 2026-10-19

안내판·수첩·NPC 대사·속마음·선택 카드·힌트·도장·선택 학습 피드백에 같은 크림·민트·청록 팔레트와 갈색 윤곽선을 적용했습니다. UI 앞면은 밝은 윗부분, 중간색, 아래쪽 음영의 세 영역으로 나뉘며, 비스듬한 모서리와 아랫면 두께·그림자로 셀 셰이딩 같은 입체감을 표현합니다.

- `CorridorBubbleGraphic`: 구름·생각 방울·대사 꼬리·둥근 표면을 벡터 메시로 그립니다. UI 평면에 명암을 고정해 장면 조명이 바뀌어도 글자 대비를 유지합니다. `Extrusion`, `Bevel Width`, `Shade Strength`, `Shadow Offset`으로 모양을 조정합니다.
- `CorridorUITheme`: 공통 색상, 안내판의 장 표시, 제본 고리가 달린 수첩, 도장 자리, 아이콘 배지와 카드 배치를 담당합니다. 세 행동 선택 카드는 같은 스타일을 사용합니다.
- `CorridorButtonDepth`: 포인터를 올리면 앞면이 조금 올라오고 누르면 내려갑니다. 클릭 영역과 콜백은 기존 버튼을 유지하며, 포인터 이탈·선택 해제·비활성화 때 위치를 복원합니다. 데스크톱과 XR 포인터가 같은 이벤트 경로를 사용합니다.
- 기본 UI는 씬에도 저장되어 Play 전 확인할 수 있습니다. 관찰 진행·힌트·PC 조작 안내는 기존처럼 Play 시작 시 생성하며 같은 테마를 적용합니다. 기존 한글 아틀라스와 머티리얼을 재사용합니다.

`Test/UI_Observation.png`, `UI_Choices.png`, `UI_Hover.png`, `UI_Pressed.png`, `UI_Reward.png`, `UI_Question.png` 등은 Unity에서 촬영한 UI 미리보기입니다. `UIReview.json`에는 포인터 효과·복원, 동일한 선택 카드 색, 보상·비활성 상태와 오류 여부의 검증 결과가 저장됩니다.

```powershell
# Play 종료 후 현재 복도 씬에 공통 UI 디자인을 재적용하고 저장
unity command run_script --file Test/Tools/ApplyCorridorUI.cs --entry ApplyCorridorUI.Run --caller plugin --skill ui-ugui --json
# 새 Play 실행 직후 화면·포인터 검증
unity command run_script --file Test/Tools/ReviewCorridorUI.cs --entry ReviewCorridorUI.Run --timeout_ms 55000 --caller plugin --skill ui-ugui --json
```

개별 UI를 별도로 수정한 뒤에는 재적용 명령을 실행할 필요가 없습니다. 테마의 자동 적용은 `CorridorStoryPresentation`에서 처리합니다. 실제 VR 헤드셋의 양안 가독성과 성능은 별도 확인이 필요합니다.

레이저는 데스크톱 컨트롤러의 재활성화 때 선·점을 함께 복원합니다. 기본 대기색은 주황, 선택 가능한 대상은 청록이며 `Laser Width`로 두께를 조정합니다. 컨트롤러 비활성화와 XR 모드에서는 데스크톱 레이저를 숨깁니다. 상황 안내는 위쪽으로 이동하고 축소했으며, PC 조작 안내는 아래쪽으로 옮겼습니다. NPC 대사는 거리에 따라 화면상 크기를 유지하며 실제로 상황 안내나 속마음과 겹칠 때만 위치를 조정합니다. `UI_Laser.png`는 복원된 레이저가 실제 선택 카드에 닿는 화면입니다. `UIReview.json`에는 세 화면 비율과 NPC 거리 세 곳의 겹침 검사도 포함됩니다.

최종 수정본에서 기존 스토리 72개 항목과 UI·레이저 29개 항목이 모두 통과했습니다. 힌트·선택 카드의 실제 중앙 레이저 도달, 비활성화 후 선·점 복원, 버튼 눌림·복원, 16:9·16:10·4:3에서 대화와 상황 안내·속마음의 겹침을 확인했습니다. 검증 중 런타임 오류는 없었습니다.

## 이번에 추가한 체험

- 시작 후 바로 답을 고르지 않고 친구와 바닥을 살피는 관찰 단계가 열립니다.
- 물기는 은은하게 반짝입니다. 물기나 친구를 조준하면 같은 색의 테두리가 생기고, 구름 모양 말풍선에 주인공의 생각이 나타납니다.
- 속마음 말풍선은 플레이어가 시선을 돌려도 화면 왼쪽 위의 같은 위치를 따라갑니다. 카메라 회전과 XR 추적 이후 위치를 갱신합니다.
- `행동 선택`을 선택하면 세 행동을 고를 수 있습니다. 관찰 여부와 관계없이 직접 진행할 수 있습니다.
- `힌트 보기`는 단계적으로 안내하며, 행동을 대신 선택하거나 도장을 지급하지 않습니다.
- 안전한 첫 선택, 두 위험 분기와 재시도, 도장은 유지합니다. 도장 이후 원인·응용 질문과 완료 설명은 별도 오브젝트에 보존하고 기본 비활성화했습니다.
- 세 선택 버튼에 같은 색과 형태를 적용해 색상으로 정답을 미리 알려주지 않습니다.

## 실행

1. 현재 프로젝트에서 `Assets/Test/Scenes/WetCorridor_Test.unity`를 엽니다.
2. Unity의 Play 버튼을 누르고 Game 창을 클릭해 입력 포커스를 줍니다.
3. 마우스로 시점을 움직여 중앙 레이저가 대상을 가리키게 하고 좌클릭합니다.

마우스를 다시 활성화하는 첫 클릭은 포커스 복귀에 사용합니다. 버튼을 선택하려면 대상을 조준하고 한 번 더 클릭합니다.

## 독립 기능 오브젝트

Hierarchy 루트에 아래 오브젝트를 저장했습니다. Play 전에도 확인하고 활성 상태를 바꿀 수 있습니다.

| 오브젝트 | 기본 상태 | 역할 |
|---|---|---|
| `Final_Feedback_Controller` | 비활성 | `CorridorFinalFeedback`이 도장 이후 질문, 응답 해설, 완료 설명을 담당합니다. 기존 `LearningCheck`, `StageEpilogue` UI가 자식으로 들어 있습니다. |
| `Player_Thought_Controller` | 활성 | `CorridorThoughtBubble`이 자식 `My_Thought_Cloud`와 카메라 추적을 담당합니다. 끄면 속마음 UI만 숨겨지고 관찰·선택 흐름은 계속됩니다. |

피드백을 다시 사용하려면 `Final_Feedback_Controller`를 켜고 스테이지를 플레이하면 됩니다. 꺼져 있을 때는 도장 획득 후 질문 없이 종료하며 `다시 둘러보기` 또는 `R`로 복습합니다. 플레이 중 피드백을 끄더라도 도장을 유지하고 복습 가능한 상태로 돌아옵니다.

말풍선의 `View Camera`, `Viewport Anchor`(현재 0.20, 0.74), `View Distance`, `Viewport Height`는 Inspector에서 확인할 수 있습니다. 현재 테마는 초기화 때 콘셉트 구도에 맞춰 위치와 크기를 적용합니다. 말풍선 그래픽은 입력을 가로채지 않습니다. 친구의 대사 말풍선은 친구 위에 표시하며 화면 밖으로 나가지 않게 조정합니다.

## 마우스 테스트 오브젝트

Hierarchy의 루트에 빈 오브젝트 `Desktop_Test_Controller`가 있습니다. `DesktopTestControls` 컴포넌트가 카메라 회전과 중앙 레이저 선택을 담당하며, 자식에는 레이저와 점 표시가 있습니다.

| 입력 | 동작 |
|---|---|
| 마우스 이동 | 버튼을 누르지 않고 카메라 회전 |
| 좌클릭 | 중앙 레이저가 가리키는 버튼·마른 바닥 경로 선택, 관찰 대상 설명 다시 확인 |
| Esc | 마우스 해제 및 시점 조작 중지 |
| 해제 후 Game 창 클릭 | 마우스 시점 조작 재개 |
| 해제 상태에서 Esc | Game 창에 입력 포커스가 있을 때 시점 조작 재개 |

관찰·선택 가능한 대상을 조준하면 레이저 점이 청록색으로 바뀝니다. 물기와 친구에는 금색 윤곽선이 추가됩니다. 관찰 중에는 친구가 위험 분기로 진행하지 않습니다. 카메라는 위치를 고정한 채 회전하며, 위아래 회전 범위는 제한합니다. Inspector에서 `Sensitivity`, `Pitch Limit`, `Laser Range`를 조정할 수 있습니다.

헤드셋이 활성화되면 이 데스크톱 컨트롤러는 마우스 조작과 중앙 레이저를 끄고 XR 추적에 시점을 맡깁니다. 씬에는 프로젝트의 기존 XR 리그와 컨트롤러 UI 입력을 연결했습니다. 실제 헤드셋 착용 검증은 별도로 필요합니다.

## 보조 키보드 입력

| 입력 | 사용 시점과 동작 |
|---|---|
| Space | 타이틀에서 이야기 시작, 관찰 단계에서 행동 선택으로 이동 |
| H | 관찰·행동 선택·경로 선택 중 힌트 보기 |
| 1 | 행동 선택에서 빨리 오라는 신호 |
| 2 | 행동 선택에서 멈춤 신호 |
| 3 | 행동 선택에서 바로 오라는 신호 |
| 4 | 친구가 멈춘 뒤 마른 길 알려주기 |
| R | 위험 결과에서 재시도, 완료 화면에서 복습 |
| Enter | 도장 기록. 마지막 피드백을 켰을 때는 해설 확인 후 다음 활동 |
| 1 또는 2 | 마지막 피드백을 켰을 때 학습 확인 질문의 첫 번째 또는 두 번째 답 |

## 체험 흐름

- 이야기 시작: 친구의 부탁 → 주변 관찰 → 대상의 테두리와 속마음 확인 → 직접 행동 선택으로 진행.
- 위험한 신호: 친구가 다가오다가 물기를 밟음 → 친구의 움직임을 느리게 표현 → 원인 강조 → 다시 시도.
- 안전한 신호: 친구에게 멈춤 요청 → 마른 경로 선택 → 친구가 돌아서 무사히 도착 → 감사 반응 → 도장 찍기.
- 도장 이후(현재 기본): 질문 없이 마무리 → 필요하면 다시 둘러보기.
- 도장 이후(피드백 오브젝트를 켠 경우): 원인 확인 → 조건이 바뀐 상황에 적용 → 완료 설명.

처음부터 안전하게 선택하면 사고를 강제로 보여주지 않습니다. 재시도나 학습 확인의 오답으로 획득한 도장이 사라지지 않으며, 복습해도 같은 도장이 중복 지급되지 않습니다. 슬로우모션은 친구의 동작에만 적용해 카메라 회전과 입력 속도를 유지합니다. 실제 힌트 사용과 정상적인 경로 선택을 구분해 기록합니다.

이번 결과물은 스테이지 1개와 도장 1개까지의 프로토타입입니다. 수첩의 나머지 2칸과 도장 3개 수집 후 전체 이야기 엔딩은 기획상의 후속 제작 범위입니다.

## 파일 위치

| 위치 | 내용 |
|---|---|
| `Assets/Test/Scenes/WetCorridor_Test.unity` | Unity에서 열어 실행하는 씬 |
| `Assets/Test/Scripts` | 씬 실행과 테스트 컨트롤러에 필요한 C# 결과물 |
| `Assets/Test/Materials`, `Assets/Test/Fonts` | 씬 머티리얼, 한글 폰트와 라이선스 |
| `Test/Story_*.png` | 갱신된 실행 화면 미리보기 |
| `Test/StoryValidation.json` | 갱신된 프로토타입 실행 검증 결과 |
| `Test/Tools/ValidateStory.cs` | Unity Pipeline에서 실행하는 흐름·중앙 레이저 통합 검증 |
| `Test/Tools/BakeStoryFont.cs` | 대사에 쓰인 한글을 기존 정적 TMP 폰트에 추가하는 에디터 도구 |
| `Test/Validation.json`, 기존 이름의 PNG | 2026-10-16 이전 프로토타입 검증·화면 |

씬을 실행하려면 `Assets/Test`의 스크립트와 자산을 함께 유지해야 합니다. 속마음과 마지막 피드백은 씬에 저장된 독립 오브젝트이며, 메인 컨트롤러에 참조가 연결되어 있습니다. 관찰 대상·VFX·관찰 진행 버튼은 Play 시 생성합니다. 검증 도구는 실행에 필요하지 않습니다.

## 개발 검증

씬을 새로 Play한 뒤 다음 명령으로 검증합니다. 검증은 실제 데스크톱 중앙 레이저 선택 경로를 통해 UI와 바닥 경로를 조작하고 결과를 기록합니다.

```powershell
unity command run_script --file Test/Tools/ValidateStory.cs --entry ValidateStory.Run --timeout_ms 55000 --project-path D:\project\ProjectWait --caller plugin --skill unity-cli --format json
```

2026-10-18 실행 검증에서 관찰, 중앙 레이저 선택, 안전한 첫 선택, 두 위험 분기, 재시도, 힌트, 도장 중복 방지, 한글 표시, 피드백 비활성 상태와 재활성 후 기존 흐름, 말풍선의 카메라 추적과 독립 토글을 확인합니다. 실제 결과는 `StoryValidation.json`에 저장됩니다. 검증 중 켜는 마지막 피드백은 종료 시 다시 비활성화합니다.

실물 VR 헤드셋·컨트롤러 검증은 별도입니다. XR 샘플의 시작 검사가 패키지 목록을 동기적으로 기다리며 에디터를 멈추는 문제는, Starter Assets와 Hands Interaction Demo 검증 코드가 이미 등록된 패키지 정보를 읽도록 수정했습니다.

## 폰트

한글에는 [Google Fonts의 Noto Sans KR](https://github.com/google/fonts/tree/main/ofl/notosanskr)을 사용했습니다. 라이선스 원문은 `Assets/Test/Fonts/OFL.txt`에 포함했습니다.
