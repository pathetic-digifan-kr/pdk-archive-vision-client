# OCR 결과 계약서

버전: `1.0`

이 문서는 `pdk-archive-vision-client`와 향후 로컬 JSON 처리기(`digimon_db_manager`)가 공유하는 OCR 결과 형식을 정의한다.

## 1. 책임 범위

### Client의 책임

- 이미지와 ROI를 선택하고 관리한다.
- OCR 엔진에 이미지를 전달한다.
- OCR 응답을 표준 결과 JSON으로 저장한다.
- 원본 이미지와 ROI의 위치 정보를 보존한다.

### JSON 처리기 또는 DB Manager의 책임

- 결과 JSON의 형식과 필수 필드를 검증한다.
- `label` 또는 `fieldKey`를 DB 필드로 매핑한다.
- 언어·도메인별 값 변환을 수행한다.
- 중복과 무결성을 검사한 뒤 DB에 저장한다.

Client는 DB 테이블 구조를 알지 않아야 하며, DB Manager는 Avalonia UI에 의존하지 않아야 한다.

## 2. 식별자와 이름

| 필드 | 용도 | 규칙 |
|---|---|---|
| `regionId` | ROI의 시스템 식별자 | UUID 문자열. 템플릿과 결과에서 유지한다. |
| `label` | 사용자가 보는 ROI 이름 | 한글, 공백을 허용한다. JSON 키로 사용하지 않는다. |
| `fieldKey` | 선택적인 DB 매핑 키 | 영문 `snake_case`를 권장한다. 예: `dex_number` |

`label`은 사용자가 변경할 수 있는 표시명이고, `regionId`는 변경하지 않는 식별자다. DB Manager는 가능하면 `fieldKey`를 우선 사용하고, 없을 때만 별도의 매핑 설정으로 `label`을 변환한다.

## 3. 저장 파일 형식

Client가 저장하는 OCR 결과 파일의 최상위 구조는 다음과 같다.

```json
{
  "schemaVersion": 1,
  "resultId": "6c67f3f0-5e1f-4d2e-b5fa-4f4f0bb6b1c0",
  "createdAt": "2026-09-21T12:34:56+09:00",
  "sourceImage": {
    "path": "/data/digimon/screenshots/terriermon_assistant.png",
    "fileName": "terriermon_assistant.png"
  },
  "template": {
    "id": "a4b93a0e-2a9e-4f56-bc77-4d0efc606e3b",
    "name": "디지몬 도감 화면"
  },
  "regions": [
    {
      "regionId": "2bf8a7fb-37c9-41c4-a8b1-0b45e3246422",
      "label": "이름",
      "fieldKey": "name_ko",
      "x": 0.12,
      "y": 0.30,
      "width": 0.42,
      "height": 0.10
    },
    {
      "regionId": "9b4bdc40-f2f3-4d4c-8bc9-8e64d3e7ec21",
      "label": "도감번호",
      "fieldKey": "dex_number",
      "x": 0.12,
      "y": 0.18,
      "width": 0.16,
      "height": 0.07
    }
  ],
  "results": [
    {
      "regionId": "2bf8a7fb-37c9-41c4-a8b1-0b45e3246422",
      "text": "테리어몬 조수",
      "confidence": 0.99,
      "status": "recognized",
      "errorMessage": null
    },
    {
      "regionId": "9b4bdc40-f2f3-4d4c-8bc9-8e64d3e7ec21",
      "text": "476",
      "confidence": 0.98,
      "status": "recognized",
      "errorMessage": null
    }
  ]
}
```

## 4. 필드 규칙

### 최상위 필드

- `schemaVersion`: 필수. 현재 값은 `1`이다.
- `resultId`: 필수 UUID. 결과 파일 하나를 식별한다.
- `createdAt`: 필수. ISO 8601 형식의 생성 시각이다.
- `sourceImage`: 필수. OCR 대상 이미지 정보다.
- `template`: 선택. 템플릿 없이 검사한 경우 `null`일 수 있다.
- `regions`: 필수 배열. OCR에 사용한 ROI 정의다.
- `results`: 필수 배열. ROI별 OCR 결과다.

### 좌표

`x`, `y`, `width`, `height`는 이미지 크기에 종속되지 않는 정규화 좌표다.

- 유효 범위: `0.0` 이상 `1.0` 이하
- `x + width`는 `1.0` 이하
- `y + height`는 `1.0` 이하

### OCR 상태

`status`는 다음 값 중 하나를 사용한다.

- `recognized`: 텍스트를 인식했다.
- `empty`: 응답은 있었지만 텍스트가 비어 있다.
- `missing`: 해당 ROI에 대한 응답이 없다.
- `error`: OCR 또는 이미지 처리 중 오류가 발생했다.

`confidence`는 `recognized` 상태에서만 기록하며 `0.0`~`1.0` 범위의 값으로 한다. 그 외 상태에서는 `null`을 사용한다.

## 5. OCR 서버 요청 호환 규칙

현재 OCR 엔드포인트의 `zones_json` 요청 형식은 다음과 같이 유지한다.

```json
[
  {
    "label": "2bf8a7fb-37c9-41c4-a8b1-0b45e3246422",
    "x": 0.12,
    "y": 0.30,
    "width": 0.42,
    "height": 0.10
  }
]
```

현재 서버 계약에서는 `label`에 `regionId`를 넣어 응답의 `parsed_data[].id`와 매칭한다. 따라서 `RoiModel`은 당장 변경하지 않아도 된다.

향후 OCR 서버가 명시적인 ID 필드를 지원하면 다음 형식으로 확장할 수 있다.

```json
{
  "id": "2bf8a7fb-37c9-41c4-a8b1-0b45e3246422",
  "label": "이름",
  "x": 0.12,
  "y": 0.30,
  "width": 0.42,
  "height": 0.10
}
```

## 6. 배치 결과

디렉터리 검사 결과도 이미지별 동일한 결과 파일 구조를 사용한다. 여러 이미지의 결과를 하나의 파일로 저장해야 하는 경우에만 최상위에 `items` 배열을 둔다.

```json
{
  "schemaVersion": 1,
  "createdAt": "2026-09-21T12:34:56+09:00",
  "items": [
    {
      "resultId": "...",
      "sourceImage": { "path": "/data/images/a.png", "fileName": "a.png" },
      "regions": [],
      "results": []
    }
  ]
}
```

`db_manager`는 단일 결과 파일과 배치 결과 파일을 구분해 처리해야 한다. 단일 결과를 임의로 객체의 동적 키로 변환하거나, ROI 이름을 JSON 속성명으로 사용하지 않는다.

## 7. 하위 호환과 변경 원칙

- 필드 추가는 가능한 한 하위 호환 방식으로 한다.
- 필드 의미를 변경할 때는 `schemaVersion`을 증가시킨다.
- `regionId`는 템플릿 저장·로드 과정에서 유지한다.
- 사용자가 보는 `label` 변경은 기존 결과의 `regionId` 매칭에 영향을 주지 않아야 한다.
- DB 저장 실패가 OCR 결과 파일 생성 실패로 이어지지 않도록 두 단계를 분리한다.

## 8. 구현 순서

1. Client에서 이 계약에 맞는 단일 이미지 결과 저장을 구현한다.
2. 이미지 경로와 ROI UUID 매칭을 검증한다.
3. 디렉터리 검사 결과를 `items` 배열로 저장한다.
4. `digimon_db_manager`에 JSON 검증 및 DB 매핑 Parser를 추가한다.
5. 계약 예제와 실제 Client 출력물을 이용한 양쪽 통합 테스트를 추가한다.
