@startuml
actor User as user
participant "Giao diện" as ui
participant "AuthController" as ctrl
participant "AuthService" as auth
participant "Database" as db

user -> ui: Nhấn "Đăng xuất"
ui -> ctrl: POST /api/logout (Bearer token)
ctrl -> auth: Logout(token)
auth -> db: UPDATE user_sessions SET status='revoked' WHERE access_token=?
db --> auth: success
auth --> ctrl: OK
ctrl --> ui: 204 No Content
ui --> user: Đã đăng xuất
@enduml