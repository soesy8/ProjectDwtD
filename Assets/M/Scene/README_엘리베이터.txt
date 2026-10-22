Wait! 엘리베이터 안전교육 — 임시 프로토타입
작성·검증: 2026-10-22 (KST)

실행
1. Unity 6000.3.18f1에서 Assets/M/Scene/Elevator_Safety_Prototype.unity를 엽니다.
2. Play를 누르고 Game 뷰를 한 번 클릭해 입력 포커스를 줍니다.
3. 관찰 → 제안 → 친구 점프·착지 직후 고장 → 도움 요청 → 대기 → 구조 완료로 진행합니다.
   모든 대상을 관찰할 필요는 없습니다. 친구에게 먼저 이야기할 수도 있습니다.
   ‘더 살펴본다’ 뒤에는 자동으로 점프하지 않습니다. ‘친구와 이야기하기’로 제안을 다시 엽니다.

PC 조작
- 마우스 이동: 테스트 씬과 같은 자동 시점 회전입니다. 오른쪽 버튼을 누를 필요가 없습니다.
- 화면 중앙 레이저: 청록 테두리의 친구·문·층 표시·비상통화·안전 안내·위치 안내를 조준해 관찰합니다.
- 왼쪽 클릭: 중앙 레이저가 가리키는 대상이나 선택지를 실행합니다. 커서 위치로 선택하지 않습니다.
- Esc: 시점 회전·레이저를 끄고 커서를 풉니다. 다시 Esc 또는 클릭하면 시점 회전으로 돌아갑니다.
- 숫자 1~4: 현재 화면의 같은 번호 선택지. R: 처음부터 다시 시작. F8: 개발 도구 표시/닫기.
- 도움 요청 전 아무것도 하지 않아도 실패하지 않습니다. 20초·40초에 순서대로 힌트가 나옵니다.
- 플레이어 눈높이는 친구의 눈 메시에서 측정한 Y=1.1064756m에 맞췄습니다. PC_Test_Controls의 Eye Height에서 조정할 수 있습니다.
- 관찰 중 큰 설명 카드를 숨겼습니다. 대사는 위쪽 말풍선, 선택지는 아래쪽 아이콘 카드로 표시합니다.
  생각 말풍선은 시선을 따라오고 대상을 벗어나면 숨겨집니다. 선택 카드는 월드에 고정되어 조준 시 도망가지 않습니다.

VR 조작과 확인 범위
- 기존 Stationary_XR_Origin의 OpenXR/XR Interaction Toolkit 입력을 복제해 사용했습니다.
- 컨트롤러 레이로 대상을 가리켜 관찰하고 선택 입력으로 장치·월드 공간 UI를 선택하도록 연결했습니다.
- 시선 추적 장치는 필수가 아닙니다. 카메라 흔들림이나 강제 회전, 플레이어 점프는 없습니다.
- 물리적인 VR 헤드셋·컨트롤러·손 추적, 양안 가독성과 실제 착용감은 검증하지 않았습니다.
- PC 검증을 VR 검증 완료로 간주하지 않습니다.

개발 도구
- 화면 오른쪽 위 ‘개발 도구’ 또는 F8을 사용합니다.
- 처음부터 / 제안 / 점프 / 고장·대처 / 통화 / 구조 대기 / 완료로 이동합니다.
- 단계 이동은 이전 선택 기록, 자막·생각, 관찰 강조, 바닥 위치, 층 표시, 경고음, 연결·대기 타이머를 초기화합니다.
- 생략된 앞 단계는 ‘개발용 단계 이동’으로 기록합니다. 실제 학습 결과와 구분됩니다.
- ‘비상통화 응답/무응답 전환’은 다음 연결에 적용됩니다. 현재 선택한 개발 설정은 재시작에도 유지됩니다.
- Inspector의 Elevator_Safety_Lesson > Simulate No Response로도 설정할 수 있습니다.
- 무응답 후 휴대폰 119 선택은 모의 신고이며 전화·네트워크 서비스에 연결하지 않습니다.

시간·문구 조정
- Assets/M/Scene/ElevatorSettings.asset에서 제안 지연, 친구 대사 시간, 점프 시간, 연결 시간,
  1·2단계 힌트, 구조 대기, 구조대원 자막 시간, 건물명·식별 정보와 핵심 대사를 바꿉니다.
- 기본값: 제안 18초 / 대사 2.8초 / 점프 0.85초 / 연결 3초 / 힌트 20·40초 / 구조 대기 18초 / 구조대원 안내 5초.
- 관찰·대화·읽기 시간을 포함한 3~5분 체험을 설계 목표로 삼았습니다. 플레이어 시간 제한은 없습니다.
  빠르게 선택하면 더 짧게 끝납니다. 실제 아동 사용자 체험 시간은 측정하지 않았습니다.
- 나머지 화면 문구는 ElevatorView.cs, 관찰 문구는 ElevatorLesson.cs에 모아져 있습니다.
  상태 전이는 ElevatorFlow.cs에 분리되어 있으며 범용 시나리오 제작 도구는 만들지 않았습니다.

친구 모델 저장
- 공유용 프리팹: Assets/Shared Assets/Models/Friend/Wait_Friend.prefab
- FBX: Assets/Shared Assets/Models/Friend/Wait_Friend_Edited.fbx
- Blender 원본: Assets/Shared Assets/Models/Friend/Source~/Wait_Friend_Edited.blend
- 같은 폴더의 Materials/, Animations/, FriendStorybookToon.shader를 함께 사용합니다.
- 기존에 제작된 친구 모델을 복제·연결했습니다. 기존 Assets/Test의 모델·씬은 수정하거나 이동하지 않았습니다.
- 새 GUID를 사용하며 공유용 친구 프리팹은 Assets/M 또는 Assets/Test 자산에 의존하지 않습니다.

실행 확인
- Unity Editor Play Mode에서 시나리오 41개 항목 통과: 말리기·동조, 더 관찰하기, 착지 후 고장,
  힌트, 중복 입력, 위험 행동 중단·재도전, 관리실 연결, 119 무응답 대체, 구조 완료,
  재시작·단계 이동 초기화와 첫 선택 기록 보존을 확인했습니다.
- Input System의 키보드·마우스 이동·클릭 이벤트로 눈높이, 자동 회전, 중앙 레이저 조준·선택,
  시선에 따른 생각 표시·숨김, Esc 커서 전환, 개발용 단계 버튼, 실제 시간 점프·정지를 검증했습니다.
  최신 통과 항목 수와 내용은 ElevatorQA/InputVerification.json에 기록됩니다.
- 입력 자동 검증 동안에는 포커스 문제를 피하기 위한 메모리상의 임시 InputSettings 복제본을 사용하고
  검증 후 원본을 복구했습니다. ProjectSettings나 기존 InputSettings 자산은 변경하지 않았습니다.
- Windows SendInput 검증은 Computer Use 도구 연결 실패로 실행하지 못했습니다.
- 상세 결과와 화면: Assets/M/CodexTemp/ElevatorQA/의 JSON·PNG.
- 소리의 재생·정지는 AudioSource 상태로 확인했습니다. 실제 출력 장치의 청감은 확인하지 않았습니다.
- Windows/Android 실행 파일 빌드, VR 실기기, 실제 전화·음성 인식·물리 승강기·구조 조작은 이번 범위에 없습니다.

공식 안전 안내 확인
- 행정안전부 국민재난안전포털 ‘승강기 안전’ 국민행동요령에서 뛰기·문 기대기 금지,
  무리한 탈출 금지, 비상버튼 구조 요청, 연결이 안 될 때 휴대폰 119 신고, 침착한 대기를 확인했습니다.
  https://www.safekorea.go.kr/idsiSFK/neo/sfk/cs/contents/prevent/SDIJK15043.html?cd1=43&cd2=99&menuSeq=128&pagecd=SDIJK150.43
- 행정안전부 안전한TV ‘승강기 유형별 사고 대처방법_엘리베이터’의 승강기 고유번호 전달 안내도 참고했습니다.
  https://www.safetv.go.kr/base/video/view?idx=458
- 건물명 ‘햇살배움센터’와 식별 정보 ‘교육용 M-01’은 가상입니다. 현재 층을 맞히게 하지 않습니다.
- 친구의 착지 뒤 고장이 나는 연결은 요청된 교육용 시나리오 연출입니다. 실제 승강기 고장을 물리적으로 재현한 것은 아닙니다.

변경 파일·기존 프로젝트 영향
- 새 씬·설정·이 안내: Assets/M/Scene/Elevator_Safety_Prototype.unity, ElevatorSettings.asset, README_엘리베이터.txt
- 새 런타임 코드: Assets/M/Scripts/Elevator/의 ElevatorSettings.cs, ElevatorFlow.cs, ElevatorLesson.cs,
  ElevatorObservation.cs, ElevatorDesktopControls.cs, ElevatorView.cs
- 씬 생성 도구: Assets/M/Scripts/Editor/ElevatorPrototypeBuilder.cs
- 기존 씬의 시점·레이저·UI 수정 도구: Assets/M/Scripts/Editor/ElevatorPresentationSetup.cs
- M 전용 자산: Assets/M/Arts/Shaders/ElevatorStorybookToon.shader,
  Assets/M/Arts/Materials/Elevator/의 머티리얼·합성 WAV, Assets/M/Arts/Fonts/ElevatorKorean.asset
- 사용자 요청 예외 경로: Assets/Shared Assets/Models/Friend/ 및 Unity 생성 .meta
- 검증 도구: Assets/M/CodexTemp/ElevatorTools~/의 VerifyElevator.cs, VerifyInput.cs, ValidateAssets.cs
- 검증 결과·캡처: Assets/M/CodexTemp/ElevatorQA/
- 이번 시점·UI 수정 캡처: Assets/M/CodexTemp/ElevatorQA/Presentation/
- 기존 한글 폰트, CorridorBubbleGraphic, XR 리그·입력 자산을 읽기 전용 참조로 재사용했습니다.
  기존 원본 씬, Packages, ProjectSettings, 공용 UI 코드와 기존 사용자 변경은 보존했습니다.
- 테스트 씬의 이동·회전·점프 Locomotion 컴포넌트는 비활성입니다. 기존 리그 원본에는 영향을 주지 않습니다.
- 정식 씬·런타임 코드는 CodexTemp에 의존하지 않습니다. 빌드 씬 목록은 변경하지 않았습니다.
- 씬 재생성 메뉴는 Wait/M/Build Elevator Prototype입니다. 저장하지 않은 씬이 있으면 생성을 중단합니다.
  이 메뉴는 M 테스트 씬과 이번 공유용 친구 자산만을 대상으로 합니다.
- AGENTS.md의 로컬 Git 제외는 git check-ignore -v AGENTS.md로 확인했습니다.
