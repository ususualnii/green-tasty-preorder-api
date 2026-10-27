# Green & Tasty — Group Pre-Order API (Owner)

Домашнє завдання до воркшопу **Backend (.NET) #5**: функціонал групового pre-order для Owner.
Усі три рівні складності. ASP.NET Core Web API (Controllers), дані — in-memory з сідом.

## Запуск

```bash
cd green-tasty-preorder-api
dotnet run
```

- API: <http://localhost:5000>
- Swagger UI: <http://localhost:5000/swagger> (поле `X-User-Id` вже додано до owner-only запитів)
- Health check: <http://localhost:5000/health>
- Дані скидаються при кожному рестарті (сховище in-memory).

## Деплой на Render

Проєкт готовий до Render: є `Dockerfile`, `render.yaml` і endpoint `/health`.

1. Репозиторій: <https://github.com/ususualnii/green-tasty-preorder-api>
2. Render Dashboard → **New → Blueprint** → підключити репозиторій → **Deploy Blueprint**
   (або **New → Web Service** → Connect repo → Runtime **Docker**, Root Directory `.`, Plan **Free**)
3. Після деплою відкриється публічний URL на кшталт `https://green-tasty-preorder-api.onrender.com`

Нюанси:

- Render ін'єктить `PORT=10000` — `Program.cs` читає його і біндить `0.0.0.0` (обов'язкова умова Render, без цього деплой падає).
- **Free-план засинає** після 15 хв без запитів: перший запит після простою може йти ~30–60 с, далі — нормально.
- Оновлення: пуш у `main` → Render автоматично перебудовує сервіс.

## Авторизація (лише Owner)

| Користувач | Роль |
|------------|-------|
| `owner-1` (Andrii) | **Owner** — за замовчуванням |
| `guest-1` (Olena)  | Guest  |

Хто викликає, визначається так (перший знайдений варіант):

1. заголовок `X-User-Id: owner-1`
2. `Authorization: Bearer owner-1`
3. query-параметр `?userId=owner-1`
4. **нічого не вказано → `owner-1`** — тому звичайні посилання в браузері працюють одразу

- невідомий id → **401**
- не Owner (`guest-1`) → **403**
- не власник конкретної групи/резервації → **403**

## Endpoint-и

| Рівень | Метод | Шлях |
|--------|-------|------|
| 🟢 1 | GET | `/guest/groups/{groupReservationId}/sessions` |
| 🔵 2 | GET | `/reservations/{reservationId}/guest-session-id` |
| 🔵 2 | POST | `/group-reservations/{groupReservationId}/submit` |
| 🟣 3 | DELETE | `/guest/groups/{groupReservationId}/sessions/{guestSessionId}` |

### 🟢 1. Список сесій учасників групи

```bash
curl -H "X-User-Id: owner-1" http://localhost:5000/guest/groups/group-1/sessions
```

```json
{
  "participants": [
    {
      "id": "owner-session-123",
      "type": "Owner",
      "name": "Andrii",
      "items": [{ "dish": { "id": "dish1", "name": "Caesar Salad", "price": 180 }, "quantity": 2 }],
      "total": 360,
      "isSubmitted": true
    }
  ],
  "total": 1300
}
```

`total` учасника = сума `price × quantity` по його стравах, `total` групи = сума по всіх учасниках.

### 🔵 2a. guestSessionId власної owner session

```bash
curl -H "X-User-Id: owner-1" http://localhost:5000/reservations/reservation-123/guest-session-id
# { "guestSessionId": "owner-session-123" }
```

### 🔵 2b. Підтвердження консолідованого pre-order

```bash
curl -X POST -H "X-User-Id: owner-1" -H "Content-Type: application/json" \
  -d '{"reservationId":"reservation-123"}' \
  http://localhost:5000/group-reservations/group-1/submit
```

```json
[
  { "dishId": "dish1", "quantity": 3 },
  { "dishId": "dish2", "quantity": 2 }
]
```

Що відбувається під час submit:

1. беруться **лише submitted** сесії групи (draft-сесія `session-3` з `dish3` ігнорується);
2. страви об'єднуються по `dishId` (2 + 1 = `dish1:3`, 1 + 1 = `dish2:2`);
3. результат додається в order резервації;
4. `order.status` → `SubmittedByCustomer`;
5. `groupReservation.status` → `Submitted`.

### 🟣 3. Видалення guest session із групи

```bash
curl -X DELETE -H "X-User-Id: owner-1" \
  http://localhost:5000/guest/groups/group-1/sessions/session-2
```

У відповіді — оновлений snapshot групи (3 учасники, `total: 1080`).

## Коди помилок

| Умова | Код |
|-------|-----|
| Невідомий/відсутній користувач | 401 |
| Не Owner (або не власник групи/резервації) | 403 |
| Група або резервація не знайдені | 404 |
| Guest session не знайдена (DELETE) | 404 |
| Guest session належить іншій групі (DELETE) | 400 |
| Немає жодної submitted session (submit) | 400 |
| У submitted sessions немає страв (submit) | 400 |
| Order не знайдено (submit) | 400 |

Перевірити ці випадки (сід):

```bash
# 401 (невідомий id) / 403 (не Owner)
curl "http://localhost:5000/guest/groups/group-1/sessions?userId=nobody"
curl "http://localhost:5000/guest/groups/group-1/sessions?userId=guest-1"

# 404
curl -H "X-User-Id: owner-1" http://localhost:5000/guest/groups/nope/sessions

# 400 "немає submitted sessions"  (group-2: уся сесія draft)
curl -X POST -H "X-User-Id: owner-1" -H "Content-Type: application/json" \
  -d '{"reservationId":"reservation-999"}' http://localhost:5000/group-reservations/group-2/submit

# 400 "немає страв"  (group-3: submitted, але порожня)
curl -X POST -H "X-User-Id: owner-1" -H "Content-Type: application/json" \
  -d '{"reservationId":"reservation-g1"}' http://localhost:5000/group-reservations/group-3/submit

# 400 "order не знайдено"  (reservation-888 без order)
curl -X POST -H "X-User-Id: owner-1" -H "Content-Type: application/json" \
  -d '{"reservationId":"reservation-888"}' http://localhost:5000/group-reservations/group-1/submit
```

## Сід-дані

- **Страви:** `dish1` Caesar Salad — 180, `dish2` Tom Yum — 220, `dish3` Grilled Salmon — 320
- **group-1** (Owner `owner-1`): `owner-session-123` (Owner, submitted, dish1×2), `session-1` Olena (submitted, dish1×1 + dish2×1), `session-2` Petro (submitted, dish2×1), `session-3` Iryna (**draft**, dish3×1) → сума 1300
- **group-2**: `owner-session-999` (draft) → для помилки «немає submitted sessions»
- **group-3**: `session-4` (submitted, без страв) → для помилки «немає страв»
- **резервації:** `reservation-123` (з order `order-123`), `reservation-888` (без order), `reservation-999`, `reservation-g1`

## Структура

```
green-tasty-preorder-api/
├── Program.cs                 # DI, Swagger, маршрутизація
├── Domain.cs                  # User, Dish, GuestSession, Reservation, GroupReservation, Order + статуси
├── Dtos.cs                    # DTO відповідей/запитів
├── Store.cs                   # in-memory сховище + сід + побудова snapshot
├── OwnerOnly.cs               # фільтр авторизації (лише Owner) + Swagger-параметр
└── Controllers/
    ├── GuestGroupsController.cs       # рівень 1 + рівень 3
    ├── ReservationsController.cs      # рівень 2 (guest-session-id)
    └── GroupReservationsController.cs # рівень 2 (submit)
```
