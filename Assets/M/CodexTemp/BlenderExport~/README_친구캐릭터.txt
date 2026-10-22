친구 캐릭터 — Blender 모델링 시작 파일

원본: Assets/Test/Scenes/WetCorridor_Test.unity / Friend_Milo
저장된 현재 프로토타입의 자세, 부위별 메시, UV0, 노멀, 색상과 부모 계층을 추출했습니다.
Unity 원본 씬·자산에는 변경을 가하지 않았습니다.

01_Character_Editable: 직접 수정할 캐릭터 부품과 팔/다리 부모 오브젝트.
02_Alternate_Parts_Hidden: 숨겨진 앞머리 3개, 눈 흰자 2개와 놀란 입. 숨김 상태로 보존.
03_Preview_Studio: 정면/사선 미리보기 카메라. 모델링 시 숨겨도 됩니다.

미터 단위, Blender Z축 위 / -Y방향 앞. 캐릭터 루트는 원점에 있습니다.
Unity의 배치 위치와 루트 회전을 제거하고, 원래 1.1배 크기는 유지했습니다.
각 메시 데이터는 독립적이어서 한쪽 팔을 편집해도 다른 부품이 함께 변하지 않습니다.
원본 삼각형 구조를 유지했습니다. 리토폴로지, 결합, 리깅과 스키닝은 적용하지 않았습니다.
원본에는 뼈대 리깅이 없으며 팔·다리를 부모 Transform으로 움직이는 구조입니다.
주머니 스티치는 Unity LineRenderer를 편집 가능한 두께 있는 커브로 바꿨습니다.

뷰포트는 재질 색상으로 표시됩니다. Material Preview/Rendered 또는 F12에서
3단계 셀 명암을 확인할 수 있으며, F12 렌더에는 갈색 Freestyle 외곽선이 포함됩니다.
Blender의 미리보기 셰이더와 외곽선은 Unity URP 셰이더의 근사 재현입니다.
Unity 셰이더 속성 원본은 각 머티리얼의 Custom Properties에 JSON으로 보존했습니다.
외부 텍스처 의존성은 없습니다.

권장: 이 파일을 열어 다른 이름으로 저장한 뒤 필요한 부위부터 모델링을 진행하세요.
