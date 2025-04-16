# Speaker Sleep Guard

Speaker Sleep Guard는는 스피커를 항상 활성 상태로 유지하는 Windows 애플리케이션입니다. 일부 오디오 장치가 일정 시간 동안 오디오 출력이 없을 때 자동으로 비활성화되는 문제를 해결하기 위해 설계되었습니다.

## 기능

- 무음을 지속적으로 출력하여 스피커 연결 유지
- 시스템 트레이에서 실행되어 사용자 방해 최소화
- 윈도우 시작 시 자동 실행 옵션
- 쉬운 재생/중지 토글

## 설치

1. [최신 릴리스](https://github.com/Kamilake/speaker-sleep-guard/releases)에서 .exe 파일을 다운로드합니다.
2. 다운로드한 파일을 실행합니다.

또는 소스에서 직접 빌드할 수 있습니다:

```bash
git clone https://github.com/Kamilake/speaker-sleep-guard.git
cd speaker-sleep-guard
dotnet build
dotnet run
```

## 사용법

- 애플리케이션이 실행되면 시스템 트레이에 아이콘이 표시됩니다.
- 트레이 아이콘을 더블 클릭하여 재생/중지를 토글할 수 있습니다.
- 트레이 아이콘을 우클릭하여 메뉴에서 다음 옵션을 선택할 수 있습니다:
  - 재생 시작/중지: 무음 재생을 시작하거나 중지합니다.
  - 시작 시 자동 실행: Windows 시작 시 자동 실행을 설정합니다.
  - 종료: 애플리케이션을 종료합니다.

## 요구 사항

- Windows 10 이상
- .NET 6.0 이상

## 라이선스

이 프로젝트는 MIT 라이선스 하에 배포됩니다. 자세한 내용은 [LICENSE](LICENSE) 파일을 참조하세요.
