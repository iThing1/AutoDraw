# AutoDraw

파스텔 화가가 검은 잉크 보스와 싸우는 2D 보스전 연출 샘플입니다.
맞은 자리마다 물감 자국이 남고, 보스를 쓰러뜨리면 검은 잉크만 걷혀 그림이 완성됩니다.

> 🎬 시연 영상: [YouTube](https://youtu.be/nhz6yQkhps4)
>
> 🚧 진행 중인 프로젝트입니다 (2026.10.12 ~). 현재 상태와 다음 할 일은 [아래](#진행-상황)에 정리했습니다.
>
> 📝 문서 작성일: 2026.10.14

| | |
|---|---|
| 장르 | 2D 보스전 (자동 공격 + 직접 이동 · 막기) |
| 엔진 | Unity 6000.3.13f1 · URP 2D |
| 사용 기술 | C# · UniTask · DOTween · Cinemachine 3 · Input System |
| 인원 | 1인 |

## 실행 방법

1. Unity 6000.3.13f1로 프로젝트를 엽니다.
2. `Assets/01_Scenes/BattleScene.unity`를 열고 플레이합니다.

> 타격 이펙트에 쓴 Cartoon FX Remaster Free는 에셋 스토어 라이선스라 저장소에 포함하지 않았습니다.
> 에셋 스토어에서 받아 `Assets/00_ThirdParty/` 아래에 넣으면 이펙트가 보입니다. 없어도 컴파일과 실행은 됩니다.

## 조작

| 키 | 동작 |
|---|---|
| WASD / 방향키 | 이동 |
| 1 · 2 | 기본 공격 선택 (공격은 자동으로 나감) |
| 3 | 특수 스킬 — 게이지가 가득 찼을 때 |
| Space | 막기 — 게이지 25를 써서 0.25초 동안 방패 |

## 흐름

```
접근(직접 이동) → 보스 등장 컷신 → 전투 → 처치 슬로모션 → 조각 분해 → 검은 잉크 걷힘 · STAGE CLEAR
```

## 구조

- **GameManager** 하나가 `UIManager` · `PoolManager` · `TimeScaleManager` · `SoundManager`를 일반 클래스로 들고 있습니다.
- **BattleManager**(스테이지 프리팹)가 위 흐름을 UniTask로 순서대로 진행합니다.
- **유닛**은 컴포넌트 조합입니다: `BattleUnit`(상태) · `Health` · `AutoAttacker` · `UnitMover` · `PlayerGuard`
- **연출 수치는 전부 ScriptableObject**입니다. 캐싱하지 않고 매번 읽어 플레이 중 조정이 바로 반영됩니다.

```
Assets/02_Scripts
├─ Battle       전투 흐름, 유닛, 공격, 이동, 막기
│  └─ AttackStyles   공격 방식 (전략 패턴)
├─ Camera       전투 · 시네마틱 카메라
├─ Common       연출 공통(Effects), 유틸(Utilities)
├─ Data         ScriptableObject와 설정 클래스
├─ Enums
├─ Manager
└─ UI
```

## 설계 포인트

- **공격 = 전략 패턴** — `AttackStyle` 추상 SO를 상속해 패턴을 추가합니다. 새 패턴은 클래스 하나로 끝나고 기존 코드는 그대로입니다.
- **유닛 행동 = 상태 패턴** — `UnitState`(Idle / Move / Action / Dead). 이동과 공격 패턴이 동시에 위치를 움직이지 않습니다.
- **명중 판정은 한 곳** — `AttackTarget`이 조준 · 명중 반경 · 막기 · 근접 접근 지점을 모두 맡습니다. 공격 패턴은 판정을 모릅니다.
- **시간 배율은 한 곳** — `TimeScaleManager`만 `Time.timeScale`을 바꿉니다. 슬로모션과 막기는 `using` 블록으로 걸고 풀어 해제 누락이 없습니다.
- **풀링과 미리 생성** — 투사체 · 이펙트 · 물감 자국을 재사용하고, 패턴 전용 프리팹은 전투 전에 만들어 둡니다.

## 문제와 해결

| 문제 | 원인 | 해결 |
|---|---|---|
| 보스 패턴을 추가할 때마다 기존 코드를 고쳐야 함 | 발사 방식을 enum + switch로 분기 | 추상 SO + 하위 클래스로 분리 ([d41cfc9](https://github.com/iThing1/AutoDraw/commit/d41cfc9)) |
| 패턴 묶음끼리 서로를 넣으면 멈춤 | 묶음 안의 묶음을 끝없이 따라감 | 깊이 제한 + 인스펙터에서 즉시 경고 ([343c8b5](https://github.com/iThing1/AutoDraw/commit/343c8b5)) |
| 패턴 도중 보스가 다시 배회함 | 멈춤을 bool 하나로 관리해 겹치면 먼저 끝난 쪽이 해제 | 유닛 상태(UnitState) 도입 ([ac0ad23](https://github.com/iThing1/AutoDraw/commit/ac0ad23)) |
| 돌진 베기 중 보스가 쓰러지면 엔딩 슬로모션이 풀림 | 늦게 끝난 쪽이 남이 건 슬로모션까지 해제 | 번호표 + `using` 블록으로 건 쪽만 해제 ([0684a3f](https://github.com/iThing1/AutoDraw/commit/0684a3f)) |
| 막기 방패를 탄이 그냥 통과함 | 도착 순간에만 판정, 방패 그림과 판정 크기도 다름 | 날아오는 동안 매 프레임 판정, 방패 크기를 판정 반경에서 계산 ([cb28bee](https://github.com/iThing1/AutoDraw/commit/cb28bee)) |
| 피하는 순간 탄이 홱 꺾임 | 추적 보정이 목표 이동량을 그대로 더함 | 보정 속도를 탄 속도 × 추적 강도로 제한, 빗나간 탄은 이어서 날아감 ([1eb5ef3](https://github.com/iThing1/AutoDraw/commit/1eb5ef3)) |
| 첫 공격 때 프레임이 튐 | 포대 · 칼끝 · 투사체를 처음 쓸 때 생성 | 전투 전 풀에 미리 생성 ([0d8cdd1](https://github.com/iThing1/AutoDraw/commit/0d8cdd1)) |
| 물감 자국이 보스 · 투사체를 덮음 | 자국마다 정렬 순서가 계속 올라감 | 정렬 레이어 분리 (바닥 · 물감 · 유닛 · 투사체 · 이펙트) |

## 진행 상황

- [x] 전투 흐름 (접근 · 보스 등장 · 전투 · 엔딩)
- [x] 자동 공격과 보스 패턴 3종 (기본 탄 · 측면 포대 일제사격 · 돌진 베기)
- [x] 직접 이동 · 타이밍 막기
- [ ] 공격 키를 입력 에셋으로 옮기기 (지금은 키보드 직접 입력)
- [ ] 잉크가 녹아 사라지는 셰이더
- [ ] 보스 추가

## 리소스

| 리소스 | 출처 | 라이선스 |
|---|---|---|
| 타격 이펙트 | Cartoon FX Remaster Free (Jean Moreno) | Unity Asset Store EULA (저장소 미포함) |
| 효과음 | Kenney — Impact · Interface · Sci-fi Sounds | CC0 |
| 글꼴 | 나눔손글씨 붓 (NAVER) | SIL OFL 1.1 |
| 라이브러리 | UniTask (Cysharp) · DOTween (Demigiant) | MIT · DOTween 무료 라이선스 |
