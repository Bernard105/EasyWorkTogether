@startuml
actor User as user
participant "Giao diện" as ui
participant "AuthController" as ctrl
participant "AuthService" as auth
participant "Database" as db
participant "TokenService" as token

user -> ui: Nhập email, password
ui -> ctrl: POST /api/login
ctrl -> auth: Authenticate(email, password)
auth -> db: SELECT * FROM users WHERE email = ?
db --> auth: user record
auth -> auth: bcrypt.verify(password)
auth --> ctrl: user
ctrl -> token: GenerateTokens(user)
token -> db: INSERT INTO user_sessions(...)
db --> token: success
token --> ctrl: access_token, refresh_token
ctrl --> ui: 200 OK { tokens }
ui --> user: Chuyển đến Dashboard

note right: Nếu sai password 3 lần,\nhệ thống gửi captcha\n(extend)
@enduml