@startuml
actor User as user
participant "Giao diện đăng ký" as ui
participant "AuthController" as ctrl
participant "UserService" as service
participant "Database" as db

user -> ui: Nhập email, password, tên
ui -> ctrl: POST /api/register
ctrl -> service: RegisterAsync(request)
service -> db: SELECT * FROM users WHERE email = ?
db --> service: email chưa tồn tại
service -> service: bcrypt.hash(password)
service -> db: INSERT INTO users(...)
db --> service: success
service --> ctrl: user object
ctrl --> ui: 201 Created
ui --> user: Thông báo đăng ký thành công
@enduml