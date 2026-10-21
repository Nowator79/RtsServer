# RtsServer

Это основной многопользовательский сервер RTS проекта.

## Авторизация и MySQL

По умолчанию сервер использует **реальную MySQL** (`localhost`, `root`/`root`, база `rts_server`).

1. Подними MySQL и примени схему:

```bash
mysql -uroot -proot < RtsServer/Docs/mysql_init.sql
```

2. Запусти сервер. При старте он проверяет БД; если таблица `Users` пуста — создаёт пользователя `test` / `test`.

3. Клиент: логин/пароль → сервер выдаёт `Token` → клиент сохраняет в PlayerPrefs. Следующий вход идёт по токену (`/auth/login/` с полем `Token`).

Переменные окружения:

| Переменная | Значение |
|---|---|
| `RTS_MYSQL` | строка подключения MySQL |
| `RTS_USE_MOCK_DB=1` | временно вернуться на in-memory мок |

Дополнительная информация:
<ul>
  <li><a href="https://www.figma.com/file/TLmRB55QXtSxZnQT2TbPbv?type=design&node-id=52-2&mode=design&t=zpLGJnOP5OQWExPC-0">Дизайн проект</a></li>
  <li>Разработчик:<a href="mailto:dfgbolshenet@gmail.com">dfgbolshenet@gmail.com</a></li>
</ul>

Клиентский проект разработан на Unity и будет доступен здесь по ссылке позже.
